using UnityEngine;

/// <summary>Sıralı görev. Create > Tycoon > Quest.</summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "Tycoon/Quest")]
public sealed class QuestSO : ScriptableObject
{
    [SerializeField] private string id;
    [SerializeField, Tooltip("Localization anahtarı; metinde {0} hedef sayıdır")] private string descriptionKey;
    [SerializeField] private QuestType type = QuestType.ServeCustomers;
    [SerializeField] private long target = 10;
    [SerializeField] private RewardType rewardType = RewardType.Money;
    [SerializeField] private long rewardAmount = 100;

    public string Id => id;
    public string DescriptionKey => descriptionKey;
    public QuestType Type => type;
    public long Target => target;
    public RewardType RewardType => rewardType;
    public long RewardAmount => rewardAmount;

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id)) id = name;
        if (string.IsNullOrWhiteSpace(descriptionKey)) descriptionKey = "quest." + type.ToString().ToLowerInvariant();
        if (target < 1) target = 1;
        if (rewardAmount < 0) rewardAmount = 0;
    }
}
