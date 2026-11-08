/// <summary>
/// Oyun içi gelir hızını (para/saniye) üstel hareketli ortalama ile tutar.
/// Çevrimdışı kazanç bu değeri kullanır. Update içinde allocation yok.
/// </summary>
public sealed class IncomeTracker
{
    private const float WindowSeconds = 30f;
    private const float Smoothing = 0.3f;

    private readonly SaveData _data;
    private double _windowEarnings;
    private float _windowTime;

    public IncomeTracker(SaveData data) => _data = data;

    public double IncomePerSecond => _data.incomePerSecond;

    public void AddEarnings(long amount)
    {
        if (amount > 0) _windowEarnings += amount;
    }

    public void Tick(float deltaTime)
    {
        _windowTime += deltaTime;
        if (_windowTime < WindowSeconds) return;

        double rate = _windowEarnings / _windowTime;
        _data.incomePerSecond = _data.incomePerSecond <= 0 ? rate : _data.incomePerSecond + (rate - _data.incomePerSecond) * Smoothing;
        _windowEarnings = 0;
        _windowTime = 0;
    }
}
