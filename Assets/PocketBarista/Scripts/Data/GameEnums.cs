/// <summary>Oyun genelinde kullanılan sabit kümeler.</summary>
public enum StationType
{
    Espresso = 0,   // Kahveler
    Oven = 1,       // Kruvasan, kek, sandviç
    Tea = 2,        // Çaylar
    Blender = 3     // Smoothie, frappe
}

public enum StaffRole
{
    Barista = 0,    // İstasyonda sipariş hazırlar
    Waiter = 1,     // Hazır siparişi masaya götürür
    Cashier = 2     // Masalardaki para yığınlarını toplar
}

public enum UpgradeStat
{
    IncomeMultiplier = 0,     // Tüm satış fiyatları
    TipMultiplier = 1,        // Bahşiş miktarı
    PrepSpeed = 2,            // İstasyon hazırlama hızı
    StaffMoveSpeed = 3,       // Personel yürüme hızı
    CustomerPatience = 4,     // Müşteri sabrı
    CustomerSpawnRate = 5,    // Müşteri gelme sıklığı
    OfflineCapHours = 6,      // Çevrimdışı kazanç saat limiti (toplamsal)
    OfflineEarningRate = 7,   // Çevrimdışı kazanç oranı
    EatSpeed = 8              // Müşterinin yeme süresi (kısalır → masa çabuk boşalır)
}

public enum QuestType
{
    EarnMoney = 0,
    ServeCustomers = 1,
    PlaceItems = 2,
    ReachLevel = 3,
    BuyUpgrades = 4,
    HireStaff = 5,
    UnlockProducts = 6,
    CollectTips = 7
}

public enum RewardType
{
    Money = 0,
    XP = 1,
    IncomeBoostMinutes = 2
}
