using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameLanguage
{
    Turkish = 0,
    English = 1
}

/// <summary>
/// Tüm oyun metinleri: anahtar → Türkçe / İngilizce. Create > Tycoon > Localization Table.
/// Yeni dil eklemek için enum'a değer ve Entry'ye alan eklenir.
/// </summary>
[CreateAssetMenu(fileName = "LocalizationTable", menuName = "Tycoon/Localization Table")]
public sealed class LocalizationTableSO : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string key;
        [TextArea(1, 3)] public string tr;
        [TextArea(1, 3)] public string en;
    }

    [SerializeField] private Entry[] entries = Array.Empty<Entry>();

    private Dictionary<string, int> _index;

    public bool TryGet(string key, GameLanguage language, out string value)
    {
        value = null;
        if (string.IsNullOrEmpty(key)) return false;
        EnsureIndex();
        if (!_index.TryGetValue(key, out int i)) return false;

        value = language == GameLanguage.English ? entries[i].en : entries[i].tr;
        if (string.IsNullOrEmpty(value)) value = entries[i].tr; // Çeviri eksikse Türkçeye düş
        return !string.IsNullOrEmpty(value);
    }

    private void OnEnable() => _index = null;

    private void OnValidate()
    {
        _index = null;
        EnsureIndex();
    }

#if UNITY_EDITOR
    /// <summary>Inspector'da sağ tık > "CSV'den içe aktar". Biçim: key,tr,en (ilk satır başlık; metinde virgül yok).</summary>
    [ContextMenu("CSV'den içe aktar")]
    private void ImportCsv()
    {
        string path = UnityEditor.EditorUtility.OpenFilePanel("Localization CSV", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(path)) return;

        string[] lines = System.IO.File.ReadAllLines(path, System.Text.Encoding.UTF8);
        var list = new List<Entry>(lines.Length);
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0) continue;
            string[] cols = line.Split(',');
            if (cols.Length < 3)
            {
                Debug.LogWarning($"[Localization] Satır {i + 1} atlandı (3 sütun bekleniyor): {line}", this);
                continue;
            }
            list.Add(new Entry { key = cols[0].Trim(), tr = cols[1].Trim(), en = cols[2].Trim() });
        }

        UnityEditor.Undo.RecordObject(this, "Import Localization CSV");
        entries = list.ToArray();
        _index = null;
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[Localization] {entries.Length} anahtar içe aktarıldı.", this);
    }
#endif

    private void EnsureIndex()
    {
        if (_index != null) return;
        _index = new Dictionary<string, int>(entries != null ? entries.Length : 0, StringComparer.Ordinal);
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            string key = entries[i].key;
            if (string.IsNullOrEmpty(key)) continue;
            if (_index.ContainsKey(key))
            {
                Debug.LogError($"[Localization] Tekrarlanan anahtar: '{key}'", this);
                continue;
            }
            _index.Add(key, i);
        }
    }
}
