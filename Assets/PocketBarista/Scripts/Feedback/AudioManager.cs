using System;
using UnityEngine;

public enum SfxType
{
    Tap = 0,
    Coin = 1,
    Serve = 2,
    PrepDone = 3,
    Purchase = 4,
    LevelUp = 5,
    Error = 6,
    QuestComplete = 7,
    Place = 8
}

/// <summary>
/// Havuzlu ses kaynakları ile SFX + tek kaynakla müzik. Statik Play() her yerden çağrılabilir;
/// sahnede AudioManager yoksa sessizce geçer. Aynı sesin üst üste binmesi (ör. 10 coin) minInterval ile sınırlanır.
/// </summary>
[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    [Serializable]
    private struct SfxEntry
    {
        public SfxType type;
        public AudioClip[] clips;           // Varyasyon için birden fazla
        [Range(0f, 1f)] public float volume;
        public float minInterval;           // Aynı sesin tekrar çalması için en az süre
    }

    private static AudioManager _instance;

    [SerializeField] private SfxEntry[] sfx = Array.Empty<SfxEntry>();
    [SerializeField] private AudioClip music;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.4f;
    [SerializeField] private int voices = 8;

    private AudioSource[] _sources;
    private AudioSource _musicSource;
    private int _next;
    private float[] _lastPlayed;
    private int[] _entryIndexByType;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => _instance = null;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        _sources = new AudioSource[Mathf.Max(1, voices)];
        for (int i = 0; i < _sources.Length; i++)
        {
            _sources[i] = gameObject.AddComponent<AudioSource>();
            _sources[i].playOnAwake = false;
        }

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.clip = music;
        _musicSource.volume = musicVolume;

        int typeCount = Enum.GetValues(typeof(SfxType)).Length;
        _lastPlayed = new float[typeCount];
        _entryIndexByType = new int[typeCount];
        for (int i = 0; i < typeCount; i++) _entryIndexByType[i] = -1;
        for (int i = 0; i < sfx.Length; i++) _entryIndexByType[(int)sfx[i].type] = i;

        GameSettings.Changed += ApplySettings;
        ApplySettings();
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        GameSettings.Changed -= ApplySettings;
        _instance = null;
    }

    public static void Play(SfxType type)
    {
        if (_instance == null || !GameSettings.SfxOn) return;
        _instance.PlayInternal(type);
    }

    private void PlayInternal(SfxType type)
    {
        int idx = _entryIndexByType[(int)type];
        if (idx < 0) return;
        SfxEntry e = sfx[idx];
        if (e.clips == null || e.clips.Length == 0) return;
        if (Time.unscaledTime - _lastPlayed[(int)type] < e.minInterval) return;
        _lastPlayed[(int)type] = Time.unscaledTime;

        AudioSource src = _sources[_next];
        _next = (_next + 1) % _sources.Length;
        src.pitch = UnityEngine.Random.Range(0.95f, 1.05f); // Tekrarı yumuşat
        src.PlayOneShot(e.clips[UnityEngine.Random.Range(0, e.clips.Length)], e.volume <= 0f ? 1f : e.volume);
    }

    private void ApplySettings()
    {
        if (_musicSource == null) return;
        if (GameSettings.MusicOn && music != null)
        {
            if (!_musicSource.isPlaying) _musicSource.Play();
        }
        else
        {
            _musicSource.Stop();
        }
    }
}
