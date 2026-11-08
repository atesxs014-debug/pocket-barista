using System;

/// <summary>Sıralı görevler. Görev başladığında istatistiğin o anki değeri "baseline" olarak saklanır.</summary>
public sealed class QuestService : IDisposable
{
    public event Action QuestChanged;      // Yeni görev veya ilerleme
    public event Action<QuestSO> QuestClaimed;

    private readonly GameContentSO _content;
    private readonly SaveData _data;
    private readonly GameStats _stats;
    private readonly LevelSystem _levels;
    private readonly Action<QuestSO> _grantReward;

    public QuestService(GameContentSO content, SaveData data, GameStats stats, LevelSystem levels, Action<QuestSO> grantReward)
    {
        _content = content;
        _data = data;
        _stats = stats;
        _levels = levels;
        _grantReward = grantReward;
        _stats.Changed += HandleStatChanged;
        _levels.LevelChanged += HandleLevelChanged;
    }

    public void Dispose()
    {
        _stats.Changed -= HandleStatChanged;
        _levels.LevelChanged -= HandleLevelChanged;
    }

    public QuestSO Current =>
        _data.questIndex >= 0 && _data.questIndex < _content.Quests.Count ? _content.Quests[_data.questIndex] : null;

    public bool AllCompleted => Current == null;

    public long Progress
    {
        get
        {
            QuestSO q = Current;
            if (q == null) return 0;
            if (q.Type == QuestType.ReachLevel) return _levels.Level;
            return Math.Max(0, _stats.GetValue(q.Type) - _data.questBaseline);
        }
    }

    public bool IsComplete => Current != null && Progress >= Current.Target;

    public bool TryClaim()
    {
        QuestSO q = Current;
        if (q == null || !IsComplete) return false;

        _grantReward?.Invoke(q);
        _data.questIndex++;
        QuestSO next = Current;
        _data.questBaseline = next != null && next.Type != QuestType.ReachLevel ? _stats.GetValue(next.Type) : 0;

        UnityEngine.Debug.Log($"[Quest] Tamamlandı: {q.Id}");
        QuestClaimed?.Invoke(q);
        QuestChanged?.Invoke();
        return true;
    }

    private void HandleStatChanged(QuestType type)
    {
        QuestSO q = Current;
        if (q != null && q.Type == type) QuestChanged?.Invoke();
    }

    private void HandleLevelChanged(int _)
    {
        QuestSO q = Current;
        if (q != null && q.Type == QuestType.ReachLevel) QuestChanged?.Invoke();
    }
}
