# Pocket Barista

3D low-poly kafe idle tycoon (Unity 6, dikey, Android + iOS).

## Derleme (GitHub Actions)
Her `main` push'unda **Actions** sekmesinde iki çıktı oluşur:
- `PocketBarista-Android-APK`: telefona doğrudan kurulur.
- `PocketBarista-iOS-IPA-unsigned`: imzasız IPA. Sideloadly / AltStore ile kendi Apple hesabınla kurulur.

Gerekli gizli anahtarlar (Settings → Secrets and variables → Actions):
`UNITY_EMAIL`, `UNITY_PASSWORD`, `UNITY_LICENSE`.

## Yerelde açma
Unity 6000.4.8f1 ile projeyi açın, menüden **Pocket Barista → Her Şeyi Kur** çalıştırın, Play'e basın.
Detaylar: `Docs/KURULUM.md`.

## Klasörler
- `Assets/PocketBarista/Scripts`: oyun kodu (Core, Data, Gameplay, UI, Feedback, Editor)
- `Assets/PocketBarista/Models`: 74 low-poly OBJ model
- `tools/`: modelleri üreten Python araçları
