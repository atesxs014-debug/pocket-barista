using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Composition root: kaydı yükler, ekonomiyi ve tüm oyun servislerini (GameServices) kurar,
/// kaydetme zamanlamasını, çevrimdışı kazancı ve prestij sıfırlamasını yönetir.
/// Execution order -1000: diğer bileşenlerin Awake/OnEnable'ından önce hazır olur.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private EconomyConfig config;
    [SerializeField] private GameContentSO content;
    [SerializeField] private ProgressionConfigSO progression;
    [SerializeField, Tooltip("TR/EN metin tablosu")] private LocalizationTableSO localizationTable;

    public PlayerEconomy Economy { get; private set; }
    public GameServices Services { get; private set; }

    /// <summary>Servisler yeniden kurulduğunda (prestij sonrası). UI yeniden abone olmalı.</summary>
    public event System.Action ServicesRebuilt;

    private SaveSystem _saveSystem;
    private SaveData _saveData;
    private bool _dataDirty;
    private bool _saveScheduled;
    private float _saveDueTime;
    private bool _offlineEvaluated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Sahne yeniden yüklendiğinde (prestij) sahnedeki kopya yok edilir; kalıcı olan kalır.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (config == null)
        {
            Debug.LogError("[GameManager] EconomyConfig atanmamış! Varsayılan değerler kullanılıyor.");
            config = ScriptableObject.CreateInstance<EconomyConfig>();
        }
        if (content == null || progression == null)
        {
            Debug.LogError("[GameManager] GameContentSO veya ProgressionConfigSO atanmamış! Oyun başlatılamaz.");
            enabled = false;
            return;
        }

        Localization.Initialize(localizationTable);

        _saveSystem = new SaveSystem(config.SaveFileName);
        _saveData = _saveSystem.Load(config.StartingMoney);
        BuildServices();
    }

    private void Start()
    {
        // Oyuncu kayıtlı mekandan farklı bir sahnede açtıysa (ör. Boot sahnesi) doğru mekana git
        VenueSO venue = Services?.Venue.Current;
        if (venue != null && !string.IsNullOrEmpty(venue.SceneName) && SceneManager.GetActiveScene().name != venue.SceneName
            && Application.CanStreamedLevelBeLoaded(venue.SceneName))
        {
            SceneManager.sceneLoaded += HandleFirstVenueLoaded; // Popup yeni sahnede olduğu için orada değerlendir
            SceneManager.LoadScene(venue.SceneName);
            return;
        }
        // UI popup'ları Awake/OnEnable'da abone olduktan sonra değerlendir
        EvaluateOffline();
    }

    private void HandleFirstVenueLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= HandleFirstVenueLoaded;
        EvaluateOffline();
    }

    private void BuildServices()
    {
        Economy = new PlayerEconomy(_saveData.money, _saveData.xp);
        Economy.MoneyChanged += HandleEconomyChanged;
        Economy.XPChanged += HandleEconomyChanged;
        Services = new GameServices(_saveData, Economy, content, progression, MarkDirty);
        Debug.Log($"[GameManager] Servisler hazır. Para={Economy.Money} Seviye={Services.Levels.Level} Yıldız={Services.Prestige.Stars}");
    }

    private void TearDownServices()
    {
        if (Economy != null)
        {
            Economy.MoneyChanged -= HandleEconomyChanged;
            Economy.XPChanged -= HandleEconomyChanged;
        }
        Services?.Dispose();
        Services = null;
        Economy = null;
    }

    private void EvaluateOffline()
    {
        if (_offlineEvaluated || Services == null) return;
        _offlineEvaluated = true;
        Services.Offline.Evaluate();
    }

    private void Update()
    {
        Services?.Income.Tick(Time.unscaledDeltaTime);

        if (_saveScheduled && Time.unscaledTime >= _saveDueTime)
            Flush(blocking: false);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            Flush(blocking: true, force: true);
        }
        else
        {
            // Arka plandan dönüş: çevrimdışı kazancı tekrar hesapla
            _offlineEvaluated = false;
            EvaluateOffline();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) Flush(blocking: true, force: true);
    }

    private void OnApplicationQuit() => Flush(blocking: true, force: true);

    private void OnDestroy()
    {
        if (Instance != this) return;
        TearDownServices();
        Instance = null;
    }

    // --- Geriye dönük uyumluluk ---
    public void AddMoney(long amount) => Economy?.AddMoney(amount);
    public bool SpendMoney(long amount) => Economy != null && Economy.TrySpendMoney(amount);
    public void AddXP(long amount) => Economy?.AddXP(amount);
    public void SaveNow() => Flush(blocking: false, force: true);

    // --- Dükkan düzeni (ShopLayoutService kullanır) ---
    public IReadOnlyList<PlacedItemData> PlacedItems => _saveData.placedItems;
    public bool IsLayoutInitialized => _saveData.layoutInitialized;

    public void AddPlacedItem(PlacedItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.itemId))
        {
            Debug.LogWarning("[GameManager] Geçersiz PlacedItemData yok sayıldı.");
            return;
        }
        _saveData.placedItems.Add(item);
        MarkDirty();
    }

    public void MarkLayoutInitialized()
    {
        if (_saveData.layoutInitialized) return;
        _saveData.layoutInitialized = true;
        MarkDirty();
    }

    // --- Prestij ---

    /// <summary>
    /// Yeniden doğuş: yıldızları ekler; para, XP, dükkan, yükseltmeler, menü, personel ve tur istatistiklerini sıfırlar,
    /// kaydeder ve sahneyi yeniden yükler. Görev zinciri, günlük seri, tüm zamanlar istatistikleri korunur.
    /// </summary>
    public bool Prestige()
    {
        if (Services == null || !Services.Prestige.CanPrestige)
        {
            Debug.LogWarning("[GameManager] Prestij şartı sağlanmadı.");
            return false;
        }

        long gained = Services.Prestige.CommitStars();
        Debug.Log($"[GameManager] PRESTİJ #{_saveData.prestigeCount}: +{gained} yıldız (toplam {_saveData.stars}).");

        Services.Venue.AdvanceForPrestige();
        VenueSO venue = Services.Venue.Current;
        _saveData.money = venue != null ? venue.StartingMoney : config.StartingMoney;
        _saveData.xp = 0;
        _saveData.layoutInitialized = false;
        _saveData.placedItems.Clear();
        _saveData.upgrades.Clear();
        _saveData.hiredStaff.Clear();
        _saveData.unlockedProducts.Clear();
        _saveData.runEarnings = 0;
        _saveData.incomePerSecond = 0;

        TearDownServices();
        BuildServices(); // Menü varsayılan ürünleri burada geri ekler
        _offlineEvaluated = true; // Prestij sonrası çevrimdışı popup gösterme

        Flush(blocking: true, force: true);
        ServicesRebuilt?.Invoke();
        LoadCurrentVenueScene();
        return true;
    }

    /// <summary>Mevcut mekanın sahnesini yükler (sahne adı boşsa aktif sahneyi yeniden yükler).</summary>
    public void LoadCurrentVenueScene()
    {
        VenueSO venue = Services?.Venue.Current;
        string scene = venue != null ? venue.SceneName : null;
        if (!string.IsNullOrEmpty(scene) && Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.Log($"[GameManager] Mekan sahnesi yükleniyor: {scene}");
            SceneManager.LoadScene(scene);
        }
        else
        {
            if (!string.IsNullOrEmpty(scene)) Debug.LogWarning($"[GameManager] '{scene}' Build Settings'te yok, aktif sahne yeniden yükleniyor.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // --- Kayıt ---

    /// <summary>Herhangi bir servis kayıt verisini değiştirdiğinde çağırır.</summary>
    public void MarkDirty()
    {
        _dataDirty = true;
        ScheduleSave();
    }

    private void HandleEconomyChanged(long _) => ScheduleSave();

    private void ScheduleSave()
    {
        // "Max gecikme" debounce: sürekli gelir akışı kaydı sonsuza dek ertelemez
        if (_saveScheduled) return;
        _saveScheduled = true;
        _saveDueTime = Time.unscaledTime + config.SaveDebounceSeconds;
    }

    private void Flush(bool blocking, bool force = false)
    {
        _saveScheduled = false;
        if (Economy == null || _saveData == null) return;
        if (!force && !Economy.IsDirty && !_dataDirty) return;

        Economy.WriteTo(_saveData);
        Economy.ClearDirty();
        _dataDirty = false;
        _saveData.lastSaveUtcTicks = GameClock.UtcTicks; // Çevrimdışı kazanç referansı

        if (blocking) _saveSystem.SaveBlocking(_saveData);
        else _saveSystem.SaveAsync(_saveData);
    }
}
