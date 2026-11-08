using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Sol üstteki görev kartı: açıklama, ilerleme çubuğu, tamamlanınca "Al" butonu.</summary>
public sealed class QuestView : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text progressLabel;
    [SerializeField] private TMP_Text rewardLabel;
    [SerializeField] private Image progressFill;
    [SerializeField] private Button claimButton;
    [SerializeField] private GameObject claimGlow;

    private readonly char[] _progBuf = new char[32];
    private readonly char[] _rewardBuf = new char[32];
    private GameServices _services;
    private QuestSO _shown;

    private void Start()
    {
        _services = GameManager.Instance != null ? GameManager.Instance.Services : null;
        if (_services == null) { enabled = false; return; }
        _services.Quests.QuestChanged += Render;
        Localization.LanguageChanged += HandleLanguage;
        if (claimButton != null) claimButton.onClick.AddListener(Claim);
        Render();
    }

    private void OnDestroy()
    {
        if (_services == null) return;
        _services.Quests.QuestChanged -= Render;
        Localization.LanguageChanged -= HandleLanguage;
    }

    private void HandleLanguage(GameLanguage _) { _shown = null; Render(); }

    private void Render()
    {
        QuestService q = _services.Quests;
        QuestSO quest = q.Current;
        if (root != null) root.SetActive(quest != null);
        if (quest == null) return;

        if (quest != _shown)
        {
            _shown = quest;
            if (description != null) description.text = Localization.Get(quest.DescriptionKey);
            string prefix = quest.RewardType == RewardType.Money ? "$" : quest.RewardType == RewardType.XP ? "XP " : "x2 ";
            string suffix = quest.RewardType == RewardType.IncomeBoostMinutes ? " min" : null;
            UIText.SetNumber(rewardLabel, _rewardBuf, quest.RewardAmount, prefix, suffix);
        }

        long progress = q.Progress;
        UIText.SetProgress(progressLabel, _progBuf, progress, quest.Target);
        if (progressFill != null) progressFill.fillAmount = quest.Target <= 0 ? 1f : Mathf.Clamp01((float)progress / quest.Target);

        bool complete = q.IsComplete;
        if (claimButton != null) claimButton.interactable = complete;
        if (claimGlow != null) claimGlow.SetActive(complete);
    }

    private void Claim()
    {
        if (!_services.Quests.TryClaim()) return;
        AudioManager.Play(SfxType.QuestComplete);
        Toast.Show(ToastKind.QuestComplete, _services.Quests.Current != null ? _services.Quests.Progress : 0);
    }
}
