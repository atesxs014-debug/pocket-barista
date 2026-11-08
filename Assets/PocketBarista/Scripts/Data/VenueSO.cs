using System;
using UnityEngine;

/// <summary>
/// Bir mekan (kafe lokasyonu). Mekanlar yeniden doğuş (prestij) ile sırayla açılır:
/// Mahalle → Kampüs → Sahil → Şehir Merkezi → Neon Çatı Katı. Create > Tycoon > Venue.
/// </summary>
[CreateAssetMenu(fileName = "NewVenue", menuName = "Tycoon/Venue")]
public sealed class VenueSO : ScriptableObject
{
    [Serializable]
    public struct CustomerWeight
    {
        public CustomerTypeSO type;
        [Min(0f)] public float weightMultiplier; // 0 = bu mekana hiç gelmez
    }

    [SerializeField, Tooltip("Kayıt için sabit kimlik")] private string id;
    [SerializeField] private string nameKey;
    [SerializeField] private string descriptionKey;
    [SerializeField] private Sprite icon;
    [SerializeField, Tooltip("Build Settings'teki sahne adı")] private string sceneName;

    [Header("Ekonomi")]
    [SerializeField, Tooltip("Tüm satış fiyatlarına çarpan")] private float priceMultiplier = 1f;
    [SerializeField, Tooltip("Bu mekanda yeniden doğuş için gereken kazanç çarpanı")] private double prestigeThresholdMultiplier = 1;
    [SerializeField, Tooltip("Mekana gelişte başlangıç parası")] private long startingMoney = 150;

    [Header("Müşteri karışımı (boşsa hepsi normal ağırlıkta)")]
    [SerializeField] private CustomerWeight[] customerWeights = Array.Empty<CustomerWeight>();

    [Header("Atmosfer")]
    [SerializeField] private Color ambientColor = new Color(0.25f, 0.22f, 0.2f);
    [SerializeField] private AudioClip music;

    public string Id => id;
    public string NameKey => nameKey;
    public string DescriptionKey => descriptionKey;
    public Sprite Icon => icon;
    public string SceneName => sceneName;
    public float PriceMultiplier => priceMultiplier;
    public double PrestigeThresholdMultiplier => prestigeThresholdMultiplier;
    public long StartingMoney => startingMoney;
    public Color AmbientColor => ambientColor;
    public AudioClip Music => music;

    /// <summary>Müşteri tipinin bu mekandaki ağırlık çarpanı. Liste boşsa 1.</summary>
    public float WeightFor(CustomerTypeSO type)
    {
        if (customerWeights == null || customerWeights.Length == 0) return 1f;
        for (int i = 0; i < customerWeights.Length; i++)
            if (customerWeights[i].type == type) return customerWeights[i].weightMultiplier;
        return 0f; // Listede yoksa bu mekana gelmez
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        if (priceMultiplier <= 0f) priceMultiplier = 1f;
        if (prestigeThresholdMultiplier <= 0) prestigeThresholdMultiplier = 1;
        if (startingMoney < 0) startingMoney = 0;
    }
}
