# Pocket Barista — Idle Tycoon Sistemleri Kurulum Rehberi

## Mimari (2 cümle)
`GameManager` kaydı yükler ve tüm saf C# servisleri (`GameServices`: ekonomi, seviye, yükseltme, menü, personel, prestij, boost, çevrimdışı kazanç, görev, günlük ödül, istatistik) tek noktada kurar; sahnedeki her şey `GameManager.Instance.Services` üzerinden bu servislere erişir ve event'lerle haberleşir. Oyun döngüsü (müşteri → masa → sipariş → istasyon → servis → para yığını → toplama) havuzlu nesnelerle çalışır, Update içinde GC üretmez; UI sadece dinler ve çizer.

## Klasör yapısı
| Klasör | İçerik |
|---|---|
| `Scripts/` (kök) | GameManager, PlayerEconomy, SaveSystem (v3), yerleştirme, kamera, yerelleştirme |
| `Scripts/Data/` | ScriptableObject tanımları: ürün, müşteri tipi, personel, yükseltme, görev, ilerleme ayarları, içerik kataloğu |
| `Scripts/Core/` | Servisler + BigNumber (1.23K, 4.5M, aa, ab...) |
| `Scripts/Gameplay/` | Sipariş panosu, istasyon, masa, para yığını, müşteri AI, personel AI, spawner'lar, havuzlar |
| `Scripts/Feedback/` | Uçan "+$" yazıları, ses yöneticisi, ayarlar |
| `Scripts/UI/` | Tüm paneller ve HUD parçaları |
| `Models/` | 74 low-poly OBJ/MTL + katalog, sahne ve mekan önizlemeleri |
| `tools/` | Modelleri yeniden üreten Python araçları |

## Gereksinimler
Unity 2021.3+ (LTS), TextMeshPro, uGUI, Legacy Input (veya "Both"). Kod Unity 2021.3 kütüphanelerine karşı derlendi, hata yok.

## Kurulum adımları
1. `Scripts/` klasörünü `Assets/Scripts/` altına, `Models/` klasörünü `Assets/Models/` altına kopyalayın.
2. **ScriptableObject'leri oluşturun** (Project > Create > Tycoon):
   - `EconomyConfig` (başlangıç parası 150)
   - `ProgressionConfig` (seviye eğrisi, yıldız eşiği, günlük ödüller, boost, çevrimdışı limit)
   - Ürünler: Espresso, Latte, Çay, Kruvasan, Donut, Cupcake, Dilim Pasta, Sandviç, Smoothie (öneri tablosu aşağıda)
   - Müşteri tipleri: Öğrenci, İş insanı, Müdavim, Turist, Emekli
   - Personel: Barista, Garson, Kasiyer
   - Yükseltmeler: her `UpgradeStat` için bir tane
   - Görevler: localization tablosundaki `quest.*` anahtarlarıyla sıralı zincir
   - `GameContent`: hepsini listelere sürükleyin
3. **Bootstrap**: Boş bir GameObject'e `GameManager` ekleyin, EconomyConfig, GameContent, ProgressionConfig ve LocalizationTable'ı bağlayın.
4. **Sahne nesneleri**: `OrderBoard`, `CustomerSpawner` (giriş/çıkış noktaları), `StaffSpawner` (personel bekleme noktaları), `MoneyPilePool` (prefab: money_pile), `ServedItemPool`, `FloatingTextPool` (prefab: FloatingText + TextMeshPro), `AudioManager` (ses klipleri).
5. **Prefab'lar**:
   - Masa: table_round/table_square + `Table` bileşeni (Seat, ItemAnchor, Pile, Service noktaları) + Collider.
   - İstasyon: espresso_machine / oven / tea_station / blender_station + `Station` (StationType seçin) + Collider.
   - Müşteri: customer_* modelleri + `CustomerAI`. Personel: barista / waiter / cashier + `StaffAI`.
   - Ürün görselleri: coffee_cup, tea_cup, croissant, donut, cupcake, cake_slice, sandwich, smoothie_cup → ProductSO.ServedPrefab.
6. **Dokunma**: Ana kameraya `PhysicsRaycaster`, sahneye `EventSystem` ekleyin (masa/istasyon/para dokunuşu için).
7. **UI** (Canvas, Safe Area): HUD'a `CurrencyView` (Money / XP / Stars), `LevelView`, `IncomeRateView`, `QuestView`, `BoostView`, `Toast`, `UnlockNotifier`. Paneller: `BuildShopPanel`, `UpgradePanel`, `MenuPanel`, `StaffPanel`, `PrestigePanel`, `StatsPanel`, `SettingsPanel`, `OfflineEarningsPopup`, `DailyRewardPopup`. Her listeli panel için bir `ShopRowView` prefab'ı yapın.
   - Not: Paneller kapalıyken GameObject'i **aktif bırakın**; `UIPanel` CanvasGroup ile gizler.
8. `localization_starter.csv` dosyasını LocalizationTable'a içe aktarın (sağ tık > Import CSV).
9. Play: 150$ ile 1 masa ve tezgahla başlarsınız.

## Mekanlar (5 adet) ve alan açma
Her mekan ayrı bir Unity sahnesidir. Yeniden doğuş (Harita > Taşın) bir sonraki mekana geçirir: para, eşyalar, yükseltmeler, menü, personel ve açılan bölgeler sıfırlanır; **yıldızlar ve görev zinciri kalır**. Son mekanda yeniden doğuş aynı mekanda devam eder.

1. Her mekan için `Create > Tycoon > Venue` ile VenueSO oluşturun, `sceneName` alanına sahne adını yazın ve sahneyi Build Settings'e ekleyin. Hepsini sırayla `GameContent > Venues` listesine koyun.
2. Her mekan sahnesine aynı sahne nesnelerini koyun (OrderBoard, spawner'lar, havuzlar, UI). Sahnede bir GameManager olabilir; ikinci kopya kendini yok eder.
3. Kilitli bölge: bir GameObject'e `ExpansionArea` ekleyin. `lockedRoot` = wood_fence / rope_barrier + sale_sign (+ Collider, dokununca satın alınır), `unlockedRoot` = bölgenin zemini, masaları ve dekoru. `areaId` mekan içinde benzersiz olmalı.
4. HUD'a `VenueMapPanel` (harita) ve sahneye `VenueArrivalBanner` ekleyin.

| # | Mekan | Fiyat x | Yeniden doğuş eşiği x | Başlangıç $ | Müşteri ağırlığı | Bölgeler (öneri) |
|---|---|---|---|---|---|---|
| 1 | Mahalle Köşesi | 1 | 1 | 150 | Müdavim, Emekli, Öğrenci | Arka Oda 2K (Sv3), Teras 15K (Sv7) |
| 2 | Kampüs Kafesi | 3 | 4 | 1K | Öğrenci x3 | Çalışma Salonu 20K, Bahçe 120K |
| 3 | Sahil Kafesi | 8 | 15 | 10K | Turist x3 | Güneşlenme Alanı 200K, İskele 1.5M |
| 4 | Şehir Merkezi | 20 | 60 | 100K | İş insanı x3 | Üst Kat 3M, Lounge 25M |
| 5 | Neon Çatı Katı | 50 | 250 | 1M | Hepsi | Bar 60M, Gökyüzü Terası 500M |

Mekan modelleri: Kampüs (study_desk, chalkboard_stand, bulletin_board, beanbag, bike_rack), Sahil (sand_tile, deck_tile, parasol, lounger, palm_tree, surfboard, railing_white), Şehir (marble_tile, glass_wall, high_table, modern_chair, planter_box, coat_rack), Neon (concrete_tile, neon_bar, string_lights, fire_pit, neon_cup_sign, rooftop_railing), kilitli bölge (sale_sign, rope_barrier, wood_fence).

## Önerilen başlangıç değerleri
| Ürün | İstasyon | Fiyat | Açılış seviye / ücret |
|---|---|---|---|
| Espresso | Espresso | 5 | Başta açık |
| Kruvasan | Oven | 8 | Sv 2 / 150 |
| Çay | Tea | 6 | Sv 3 / 300 |
| Latte | Espresso | 12 | Sv 4 / 600 |
| Donut | Oven | 15 | Sv 6 / 1.5K |
| Smoothie | Blender | 25 | Sv 8 / 4K |
| Sandviç | Oven | 35 | Sv 10 / 10K |
| Cupcake | Oven | 50 | Sv 13 / 30K |
| Dilim Pasta | Oven | 80 | Sv 16 / 90K |

| Müşteri | Fiyat x | Sabır x | Bahşiş şansı | Açılış |
|---|---|---|---|---|
| Öğrenci | 0.8 | 0.7 | %10 | 1 |
| Müdavim | 1.0 | 1.3 | %30 | 1 |
| İş insanı | 1.5 | 0.8 | %40 | 4 |
| Turist | 1.3 | 1.0 | %50 | 7 |
| Emekli | 1.1 | 1.6 | %25 | 10 |

Yükseltme maliyeti: `taban × 1.15^seviye`. Prestij: tur kazancı ≥ 1M olunca yıldız; her yıldız +%10 gelir.

## Kontrol listesi
- Konsolda `[GameManager] Servisler hazır` görünmeli.
- Müşteri gelir, masaya oturur, sipariş verir; istasyona dokunun → ürün hazırlanır → masaya dokunun → servis → masada para yığını → dokunun → "+$" uçar.
- Uygulamayı 5+ dk kapatıp açın → "Sen yokken kazandın" popup'ı.
