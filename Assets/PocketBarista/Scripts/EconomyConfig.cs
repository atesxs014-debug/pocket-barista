using UnityEngine;

/// <summary>
/// Ekonomi ayarları. Inspector'dan düzenlenir, kod içinde sabit değer tutulmaz.
/// Create > Tycoon > Economy Config ile oluşturun.
/// </summary>
[CreateAssetMenu(fileName = "EconomyConfig", menuName = "Tycoon/Economy Config")]
public sealed class EconomyConfig : ScriptableObject
{
    [SerializeField, Tooltip("Yeni oyuncunun başlangıç bakiyesi")]
    private long startingMoney = 150;

    [SerializeField, Tooltip("İlk değişiklikten kaç saniye sonra diske yazılsın (debounce)")]
    private float saveDebounceSeconds = 2f;

    [SerializeField, Tooltip("persistentDataPath altındaki kayıt dosyası adı")]
    private string saveFileName = "tycoon_save.json";

    public long StartingMoney => startingMoney;
    public float SaveDebounceSeconds => saveDebounceSeconds;
    public string SaveFileName => saveFileName;

    private void OnValidate()
    {
        if (startingMoney < 0) startingMoney = 0;
        if (saveDebounceSeconds < 0f) saveDebounceSeconds = 0f;
        if (string.IsNullOrWhiteSpace(saveFileName)) saveFileName = "tycoon_save.json";
    }
}
