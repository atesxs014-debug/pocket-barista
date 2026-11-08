using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Sahneye yerleştirilmiş bir eşyanın kaydı.</summary>
[Serializable]
public sealed class PlacedItemData
{
    public string itemId;      // BuildableItemSO.Id
    public float x;
    public float z;
    public int rotationSteps;  // 0..3 → 0°, 90°, 180°, 270°
}

[Serializable]
public sealed class IdLevel
{
    public string id;
    public int level;
}

/// <summary>Diske yazılan veri. Alan eklerseniz CurrentVersion'ı artırın ve Upgrade() içinde varsayılan verin.</summary>
[Serializable]
public sealed class SaveData
{
    public const int CurrentVersion = 4;

    public int version = CurrentVersion;
    public long money;
    public long xp;

    // Dükkan düzeni
    public bool layoutInitialized;
    public List<PlacedItemData> placedItems = new List<PlacedItemData>();

    // İlerleme
    public List<IdLevel> upgrades = new List<IdLevel>();
    public List<string> unlockedProducts = new List<string>();
    public List<string> hiredStaff = new List<string>();
    public long stars;                       // Prestij yıldızları (kalıcı)
    public int prestigeCount;

    // İstatistikler (run = bu prestij turu, lifetime = tüm zamanlar)
    public double runEarnings;
    public double lifetimeEarnings;
    public long customersServed;
    public long customersLost;
    public long tipsCollected;
    public long itemsPlaced;
    public long upgradesBought;
    public long staffHired;
    public long productsUnlocked;

    // Görev, günlük ödül, boost, çevrimdışı
    public int questIndex;
    public long questBaseline;
    public int dailyStreak;
    public long lastDailyClaimDay = -1;      // UTC gün numarası
    public long boostEndUtcTicks;
    public long lastSaveUtcTicks;
    public double incomePerSecond;
    // v4: mekanlar ve alan açma
    public int venueIndex;                                   // Şu anki mekan (Content.Venues sırası)
    public List<string> unlockedAreas = new List<string>();  // Bu mekanda açılmış bölgeler           // Çevrimdışı kazanç için son gelir hızı
}

/// <summary>
/// JSON tabanlı kayıt. JSON ana thread'de üretilir, dosya yazımı arka plan thread'inde yapılır
/// (ana thread'i bloklamaz). Önce .tmp dosyasına yazılır, sonra yer değiştirilir:
/// yazım sırasında uygulama ölürse eski kayıt bozulmaz.
/// Not: WebGL'de thread yoktur; orada SaveBlocking kullanın.
/// </summary>
public sealed class SaveSystem
{
    private const string LegacyMoneyKey = "Tycoon_Money";
    private const string LegacyXPKey = "Tycoon_XP";

    private readonly string _path;
    private readonly string _tmpPath;
    private readonly object _fileLock = new object();
    private Task _writeChain = Task.CompletedTask;

    public SaveSystem(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Dosya adı boş olamaz", nameof(fileName));
        _path = Path.Combine(Application.persistentDataPath, fileName);
        _tmpPath = _path + ".tmp";
    }

    public SaveData Load(long defaultMoney)
    {
        // Ana dosya yoksa, yarım kalmış bir yer değiştirmeden kalan .tmp'yi dene
        string source = File.Exists(_path) ? _path : (File.Exists(_tmpPath) ? _tmpPath : null);

        if (source != null)
        {
            try
            {
                SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(source));
                if (data != null)
                {
                    Upgrade(data);
                    Debug.Log($"[Save] Yüklendi: {source} (money={data.money}, xp={data.xp})");
                    return data;
                }
                Debug.LogWarning("[Save] Kayıt dosyası boş, varsayılana dönülüyor.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Kayıt okunamadı, varsayılana dönülüyor: {e.Message}");
            }
        }

        return MigrateLegacyOrDefault(defaultMoney);
    }

    /// <summary>Ana thread'i bloklamadan kaydeder. Yazımlar sıraya girer, üst üste binmez.</summary>
    public void SaveAsync(SaveData data)
    {
        if (data == null) return;
        string json = JsonUtility.ToJson(data); // Unity API: ana thread'de kalmalı
        _writeChain = _writeChain.ContinueWith(_ => WriteAtomic(json), TaskScheduler.Default);
    }

    /// <summary>Pause/Quit için: uygulama kapanmadan önce yazımın bittiğinden emin olur.</summary>
    public void SaveBlocking(SaveData data)
    {
        if (data == null) return;
        string json = JsonUtility.ToJson(data);
        try { _writeChain.Wait(1000); }
        catch (Exception e) { Debug.LogWarning($"[Save] Önceki yazım beklenirken hata: {e.Message}"); }
        WriteAtomic(json);
    }

    private void WriteAtomic(string json)
    {
        lock (_fileLock)
        {
            try
            {
                File.WriteAllText(_tmpPath, json);
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(_tmpPath, _path);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Yazım başarısız: {e.Message}");
            }
        }
    }

    // Eski sürüm kayıtlarını güncel şemaya getirir
    private static void Upgrade(SaveData data)
    {
        if (data.placedItems == null) data.placedItems = new List<PlacedItemData>();
        if (data.upgrades == null) data.upgrades = new List<IdLevel>();
        if (data.unlockedProducts == null) data.unlockedProducts = new List<string>();
        if (data.hiredStaff == null) data.hiredStaff = new List<string>();
        if (data.unlockedAreas == null) data.unlockedAreas = new List<string>();
        data.unlockedAreas.RemoveAll(string.IsNullOrEmpty);
        if (data.venueIndex < 0) data.venueIndex = 0;
        data.placedItems.RemoveAll(p => p == null || string.IsNullOrEmpty(p.itemId));
        data.upgrades.RemoveAll(u => u == null || string.IsNullOrEmpty(u.id));
        data.unlockedProducts.RemoveAll(string.IsNullOrEmpty);
        data.hiredStaff.RemoveAll(string.IsNullOrEmpty);
        if (data.version < 3) data.lastDailyClaimDay = -1;

        if (data.version < SaveData.CurrentVersion)
        {
            Debug.Log($"[Save] Kayıt v{data.version} → v{SaveData.CurrentVersion} güncellendi.");
            data.version = SaveData.CurrentVersion;
        }
    }

    // Eski sürümün PlayerPrefs kaydı varsa oyuncunun parası kaybolmasın.
    private static SaveData MigrateLegacyOrDefault(long defaultMoney)
    {
        var data = new SaveData { money = defaultMoney, xp = 0 };

        if (PlayerPrefs.HasKey(LegacyMoneyKey) || PlayerPrefs.HasKey(LegacyXPKey))
        {
            data.money = PlayerPrefs.GetInt(LegacyMoneyKey, (int)Math.Min(defaultMoney, int.MaxValue));
            data.xp = PlayerPrefs.GetInt(LegacyXPKey, 0);
            Debug.Log($"[Save] Eski PlayerPrefs kaydı taşındı (money={data.money}, xp={data.xp}).");
        }

        return data;
    }
}
