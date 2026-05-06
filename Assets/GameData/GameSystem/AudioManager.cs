using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;

namespace GameSystem
{
    [System.Serializable]
    public struct SceneMusicSetting
    {
        public string SceneName;
        [Range(0, 2)] public int MusicIndex;

        public SceneMusicSetting(string sceneName, int musicIndex)
        {
            SceneName = sceneName;
            MusicIndex = musicIndex;
        }
    }

    public class AudioManager : Singleton<AudioManager>
    {
        private const float MinMixerVolumeDb = -80f;
        private const string MusicVolumePrefsKey = "AudioManager.MusicVolume";
        private const string SfxVolumePrefsKey = "AudioManager.SFXVolume";

        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private string musicVolumeParameter = "MusicVolume";
        [SerializeField] private string sfxVolumeParameter = "SFXVolume";

        [Header("Sources")]
        [SerializeField] private AudioSource musicSourceA;
        [SerializeField] private AudioSource musicSourceB;
        [SerializeField] private int sfxPoolSize = 8;
        [SerializeField] private int maxSfxPoolSize = 24;

        [Header("Music Playlist")]
        [SerializeField] private AudioClip[] musicClips = new AudioClip[3];
        [SerializeField, Range(0, 2)] private int defaultMusicIndex = 0;
        [SerializeField] private bool playDefaultMusicOnStart = false;

        [Header("Scene Music")]
        [SerializeField] private bool controlMusicByScene = true;
        [SerializeField] private SceneTransitionManager sceneTransitionManager;
        [SerializeField] private float sceneMusicFadeOutDuration = 0.5f;
        [SerializeField] private float sceneMusicSilenceDuration = 0.25f;
        [SerializeField] private float sceneMusicFadeInDuration = 1f;
        [SerializeField]
        private SceneMusicSetting[] sceneMusicSettings =
        {
            new SceneMusicSetting(SceneTransitionManager.SCENE_MAIN_MENU, 0),
            new SceneMusicSetting(SceneTransitionManager.SCENE_HUMAN, 1),
            new SceneMusicSetting(SceneTransitionManager.SCENE_MONSTER, 2)
        };

        [Header("Auction Music")]
        [FormerlySerializedAs("auctionMusicClip")]
        [SerializeField] private AudioClip auctionMusic;
        [SerializeField] private float auctionMusicFadeDuration = 1f;

        [Header("Defaults")]
        [SerializeField, Range(0f, 1f)] private float initialMusicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float initialSfxVolume = 1f;
        [SerializeField] private float defaultMusicFadeDuration = 1f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool saveVolumeSettings = true;

        private readonly List<AudioSource> _sfxSources = new List<AudioSource>();
        private readonly HashSet<AudioSource> _loopingSfxSources = new HashSet<AudioSource>();
        private AudioSource _activeMusicSource;
        private AudioSource _inactiveMusicSource;
        private Coroutine _musicFadeCoroutine;
        private float _musicVolume;
        private float _sfxVolume;
        private float _musicSourceAFade;
        private float _musicSourceBFade;
        private bool _musicMixerVolumeAvailable;
        private bool _sfxMixerVolumeAvailable;
        private bool _warnedMissingMusicParameter;
        private bool _warnedMissingSfxParameter;
        private int _currentMusicIndex = -1;
        private int _pendingSceneMusicIndex = -1;
        private bool _pendingSceneMusicChange;
        private bool _pendingSceneMusicFadeOutStarted;
        private float _sceneMusicFadeOutStartedAt = -1f;
        private float _activeSceneMusicFadeOutDuration;
        private Coroutine _sceneMusicRoutine;
        private AudioClip _preAuctionMusicClip;
        private int _preAuctionMusicIndex = -1;
        private AudioClip _activeAuctionMusicClip;
        private bool _auctionMusicOverrideActive;

        public float MusicVolume => _musicVolume;
        public float SfxVolume => _sfxVolume;
        public AudioMixer AudioMixer => audioMixer;
        public AudioMixerGroup MusicMixerGroup => musicMixerGroup;
        public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;
        public IReadOnlyList<AudioClip> MusicClips => musicClips;
        public int CurrentMusicIndex => _currentMusicIndex;
        public bool IsMusicPlaying => _activeMusicSource != null && _activeMusicSource.isPlaying;
        public AudioClip CurrentMusicClip => _activeMusicSource != null ? _activeMusicSource.clip : null;

        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
                return;

            EnsureAudioSources();
            LoadVolumeSettings();
            ApplyMusicVolume();
            ApplySfxVolume();
        }

        private void Start()
        {
            BindSceneTransitionManager();

            if (playDefaultMusicOnStart)
                PlayMusicByIndex(defaultMusicIndex);
        }

        protected override void OnDestroy()
        {
            UnbindSceneTransitionManager();
            base.OnDestroy();
        }

        private void OnValidate()
        {
            if (musicClips == null || musicClips.Length != 3)
            {
                AudioClip[] resizedClips = new AudioClip[3];
                if (musicClips != null)
                {
                    int copyCount = Mathf.Min(musicClips.Length, resizedClips.Length);
                    for (int i = 0; i < copyCount; i++)
                        resizedClips[i] = musicClips[i];
                }

                musicClips = resizedClips;
            }

            if (sceneMusicSettings == null || sceneMusicSettings.Length == 0)
            {
                sceneMusicSettings = new[]
                {
                    new SceneMusicSetting(SceneTransitionManager.SCENE_MAIN_MENU, 0),
                    new SceneMusicSetting(SceneTransitionManager.SCENE_HUMAN, 1),
                    new SceneMusicSetting(SceneTransitionManager.SCENE_MONSTER, 2)
                };
            }

            sfxPoolSize = Mathf.Max(1, sfxPoolSize);
            maxSfxPoolSize = Mathf.Max(sfxPoolSize, maxSfxPoolSize);
        }

        public void ConfigureMixer(AudioMixer mixer, AudioMixerGroup musicGroup, AudioMixerGroup sfxGroup)
        {
            audioMixer = mixer;
            musicMixerGroup = musicGroup;
            sfxMixerGroup = sfxGroup;
            _warnedMissingMusicParameter = false;
            _warnedMissingSfxParameter = false;
            AssignMixerGroups();
            ApplyMusicVolume();
            ApplySfxVolume();
        }

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeDuration = -1f, bool restartIfSameClip = false)
        {
            _currentMusicIndex = GetMusicClipIndex(clip);

            if (clip == null)
            {
                StopMusic(fadeDuration);
                return;
            }

            EnsureAudioSources();

            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(FadeToMusicRoutine(clip, loop, ResolveFadeDuration(fadeDuration), restartIfSameClip));
        }

        public void PlayMusicByIndex(int index, bool loop = true, float fadeDuration = -1f, bool restartIfSameClip = false)
        {
            if (!TryGetMusicClip(index, out AudioClip clip))
                return;

            _currentMusicIndex = index;
            PlayMusic(clip, loop, fadeDuration, restartIfSameClip);
        }

        public void SwitchMusic(int index, float fadeDuration = -1f)
        {
            PlayMusicByIndex(index, true, fadeDuration);
        }

        public void PlayAuctionMusic(float fadeDuration = -1f)
        {
            if (auctionMusic == null)
                return;

            if (!_auctionMusicOverrideActive)
            {
                _preAuctionMusicClip = CurrentMusicClip;
                _preAuctionMusicIndex = _currentMusicIndex;
            }

            _activeAuctionMusicClip = auctionMusic;
            _auctionMusicOverrideActive = true;
            PlayMusic(auctionMusic, true, ResolveAuctionFadeDuration(fadeDuration));
        }

        public void StopAuctionMusic(float fadeDuration = -1f)
        {
            if (!_auctionMusicOverrideActive)
                return;

            AudioClip restoreClip = _preAuctionMusicClip;
            int restoreIndex = _preAuctionMusicIndex;
            AudioClip auctionClip = _activeAuctionMusicClip;

            _auctionMusicOverrideActive = false;
            _preAuctionMusicClip = null;
            _preAuctionMusicIndex = -1;
            _activeAuctionMusicClip = null;

            float resolvedFadeDuration = ResolveAuctionFadeDuration(fadeDuration);
            if (IsValidMusicIndex(restoreIndex))
            {
                PlayMusicByIndex(restoreIndex, true, resolvedFadeDuration);
                return;
            }

            if (restoreClip != null && restoreClip != auctionClip)
                PlayMusic(restoreClip, true, resolvedFadeDuration);
            else
                StopMusic(resolvedFadeDuration);
        }

        public void PlayNextMusic(float fadeDuration = -1f)
        {
            PlayMusicByIndex(GetWrappedMusicIndex(_currentMusicIndex + 1), true, fadeDuration);
        }

        public void PlayPreviousMusic(float fadeDuration = -1f)
        {
            PlayMusicByIndex(GetWrappedMusicIndex(_currentMusicIndex - 1), true, fadeDuration);
        }

        public void StopMusic(float fadeDuration = -1f)
        {
            EnsureAudioSources();

            if (_musicFadeCoroutine != null)
                StopCoroutine(_musicFadeCoroutine);

            _musicFadeCoroutine = StartCoroutine(FadeOutMusicRoutine(ResolveFadeDuration(fadeDuration)));
        }

        public void SetMusicClip(int index, AudioClip clip)
        {
            if (!IsValidMusicIndex(index))
            {
                Debug.LogWarning($"[AudioManager] Music index out of range: {index}");
                return;
            }

            musicClips[index] = clip;
        }

        public void PauseMusic()
        {
            musicSourceA?.Pause();
            musicSourceB?.Pause();
        }

        public void ResumeMusic()
        {
            if (_activeMusicSource != null)
                _activeMusicSource.UnPause();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            if (saveVolumeSettings)
                PlayerPrefs.SetFloat(MusicVolumePrefsKey, _musicVolume);

            ApplyMusicVolume();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            if (saveVolumeSettings)
                PlayerPrefs.SetFloat(SfxVolumePrefsKey, _sfxVolume);

            ApplySfxVolume();
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null)
                return;

            EnsureAudioSources();

            AudioSource source = GetAvailableSfxSource();
            if (source == null)
                return;

            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            source.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            PlaySfx(clip, volumeScale, pitch);
        }

        public AudioSource PlayLoopingSfx(AudioClip clip, float volumeScale = 1f, float pitch = 1f)
        {
            if (clip == null)
                return null;

            EnsureAudioSources();

            AudioSource source = GetAvailableSfxSource();
            if (source == null)
                return null;

            source.clip = clip;
            source.loop = true;
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            source.volume = GetSfxSourceVolume() * Mathf.Clamp01(volumeScale);
            source.Play();
            _loopingSfxSources.Add(source);
            return source;
        }

        public void StopLoopingSfx(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.loop = false;
            source.clip = null;
            source.pitch = 1f;
            source.volume = GetSfxSourceVolume();
            _loopingSfxSources.Remove(source);
        }

        private void BindSceneTransitionManager()
        {
            if (!controlMusicByScene)
                return;

            if (sceneTransitionManager == null)
                sceneTransitionManager = GetComponent<SceneTransitionManager>();

            if (sceneTransitionManager == null)
                sceneTransitionManager = SceneTransitionManager.Instance;

            if (sceneTransitionManager == null)
                return;

            sceneTransitionManager.OnSceneLoadStart -= OnSceneLoadStart;
            sceneTransitionManager.OnSceneLoadComplete -= OnSceneLoadComplete;
            sceneTransitionManager.OnSceneLoadStart += OnSceneLoadStart;
            sceneTransitionManager.OnSceneLoadComplete += OnSceneLoadComplete;
        }

        private void UnbindSceneTransitionManager()
        {
            if (sceneTransitionManager == null)
                return;

            sceneTransitionManager.OnSceneLoadStart -= OnSceneLoadStart;
            sceneTransitionManager.OnSceneLoadComplete -= OnSceneLoadComplete;
        }

        private void OnSceneLoadStart(string sceneName)
        {
            if (_sceneMusicRoutine != null)
            {
                StopCoroutine(_sceneMusicRoutine);
                _sceneMusicRoutine = null;
            }

            _pendingSceneMusicChange = false;
            _pendingSceneMusicFadeOutStarted = false;
            _pendingSceneMusicIndex = -1;
            _sceneMusicFadeOutStartedAt = -1f;
            _activeSceneMusicFadeOutDuration = 0f;

            if (!controlMusicByScene || !TryGetSceneMusicIndex(sceneName, out int targetMusicIndex))
                return;

            _pendingSceneMusicIndex = targetMusicIndex;
            _pendingSceneMusicChange = ShouldSwitchSceneMusic(targetMusicIndex);

            if (_pendingSceneMusicChange && IsMusicPlaying)
            {
                _pendingSceneMusicFadeOutStarted = true;
                _sceneMusicFadeOutStartedAt = GetAudioTime();
                _activeSceneMusicFadeOutDuration = Mathf.Max(0f, sceneMusicFadeOutDuration);
                StopMusic(sceneMusicFadeOutDuration);
            }
        }

        private void OnSceneLoadComplete(string sceneName)
        {
            if (!controlMusicByScene)
                return;

            int targetMusicIndex = _pendingSceneMusicIndex;
            bool shouldSwitch = _pendingSceneMusicChange;

            if (targetMusicIndex < 0 && TryGetSceneMusicIndex(sceneName, out int resolvedIndex))
            {
                targetMusicIndex = resolvedIndex;
                shouldSwitch = ShouldSwitchSceneMusic(targetMusicIndex);
            }

            _pendingSceneMusicIndex = -1;
            _pendingSceneMusicChange = false;
            bool waitForFadeOut = _pendingSceneMusicFadeOutStarted;
            _pendingSceneMusicFadeOutStarted = false;

            if (shouldSwitch)
            {
                if (_sceneMusicRoutine != null)
                    StopCoroutine(_sceneMusicRoutine);

                _sceneMusicRoutine = StartCoroutine(PlaySceneMusicAfterSilenceRoutine(targetMusicIndex, waitForFadeOut));
            }
        }

        private IEnumerator PlaySceneMusicAfterSilenceRoutine(int targetMusicIndex, bool waitForFadeOut)
        {
            if (waitForFadeOut && _sceneMusicFadeOutStartedAt >= 0f)
            {
                float elapsedFadeOut = GetAudioTime() - _sceneMusicFadeOutStartedAt;
                float remainingFadeOut = Mathf.Max(0f, _activeSceneMusicFadeOutDuration - elapsedFadeOut);
                yield return WaitForAudioSeconds(remainingFadeOut);
            }

            yield return WaitForAudioSeconds(Mathf.Max(0f, sceneMusicSilenceDuration));
            PlayMusicByIndex(targetMusicIndex, true, sceneMusicFadeInDuration);
            _sceneMusicRoutine = null;
        }

        private IEnumerator FadeToMusicRoutine(AudioClip clip, bool loop, float duration, bool restartIfSameClip)
        {
            AudioSource from = _activeMusicSource;

            if (from != null && from.clip == clip && from.isPlaying && !restartIfSameClip)
            {
                from.loop = loop;
                SetMusicFade(from, 1f);
                ApplyMusicSourceVolumes();
                _musicFadeCoroutine = null;
                yield break;
            }

            AudioSource to = from == musicSourceA ? musicSourceB : musicSourceA;
            if (to == null)
                yield break;

            to.Stop();
            to.clip = clip;
            to.loop = loop;
            to.pitch = 1f;
            SetMusicFade(to, 0f);
            ApplyMusicSourceVolumes();
            to.Play();

            float fromStartFade = from != null ? GetMusicFade(from) : 0f;
            float time = 0f;

            if (duration <= 0f)
            {
                if (from != null)
                {
                    from.Stop();
                    from.clip = null;
                    SetMusicFade(from, 0f);
                }

                SetMusicFade(to, 1f);
                _activeMusicSource = to;
                _inactiveMusicSource = from;
                ApplyMusicSourceVolumes();
                _musicFadeCoroutine = null;
                yield break;
            }

            while (time < duration)
            {
                time += GetDeltaTime();
                float t = Mathf.Clamp01(time / duration);

                if (from != null)
                    SetMusicFade(from, Mathf.Lerp(fromStartFade, 0f, t));

                SetMusicFade(to, Mathf.Lerp(0f, 1f, t));
                ApplyMusicSourceVolumes();
                yield return null;
            }

            if (from != null)
            {
                from.Stop();
                from.clip = null;
                SetMusicFade(from, 0f);
            }

            SetMusicFade(to, 1f);
            _activeMusicSource = to;
            _inactiveMusicSource = from;
            ApplyMusicSourceVolumes();
            _musicFadeCoroutine = null;
        }

        private IEnumerator FadeOutMusicRoutine(float duration)
        {
            AudioSource source = _activeMusicSource;
            if (source == null || !source.isPlaying)
            {
                _musicFadeCoroutine = null;
                yield break;
            }

            float startFade = GetMusicFade(source);
            float time = 0f;

            if (duration <= 0f)
            {
                StopMusicSource(source);
                _musicFadeCoroutine = null;
                yield break;
            }

            while (time < duration)
            {
                time += GetDeltaTime();
                SetMusicFade(source, Mathf.Lerp(startFade, 0f, Mathf.Clamp01(time / duration)));
                ApplyMusicSourceVolumes();
                yield return null;
            }

            StopMusicSource(source);
            _musicFadeCoroutine = null;
        }

        private void StopMusicSource(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            SetMusicFade(source, 0f);
            _inactiveMusicSource = source;

            if (_activeMusicSource == source)
                _activeMusicSource = null;

            ApplyMusicSourceVolumes();
        }

        private void EnsureAudioSources()
        {
            _sfxSources.RemoveAll(source => source == null);
            _loopingSfxSources.RemoveWhere(source => source == null);

            musicSourceA = EnsureChildAudioSource(musicSourceA, "Music Source A", musicMixerGroup, true);
            musicSourceB = EnsureChildAudioSource(musicSourceB, "Music Source B", musicMixerGroup, true);

            if (_activeMusicSource == null)
                _activeMusicSource = musicSourceA;

            if (_inactiveMusicSource == null)
                _inactiveMusicSource = musicSourceB;

            while (_sfxSources.Count < Mathf.Max(1, sfxPoolSize))
            {
                AudioSource source = CreateSfxSource();
                _sfxSources.Add(source);
            }

            AssignMixerGroups();
        }

        private AudioSource EnsureChildAudioSource(AudioSource source, string sourceName, AudioMixerGroup mixerGroup, bool loop)
        {
            if (source == null)
            {
                Transform existing = transform.Find(sourceName);
                if (existing != null)
                    source = existing.GetComponent<AudioSource>();
            }

            if (source == null)
            {
                GameObject sourceObject = new GameObject(sourceName);
                sourceObject.transform.SetParent(transform);
                source = sourceObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f;
            source.outputAudioMixerGroup = mixerGroup;
            return source;
        }

        private AudioSource CreateSfxSource()
        {
            return EnsureChildAudioSource(null, $"SFX Source {_sfxSources.Count + 1}", sfxMixerGroup, false);
        }

        private void AssignMixerGroups()
        {
            if (musicSourceA != null)
                musicSourceA.outputAudioMixerGroup = musicMixerGroup;

            if (musicSourceB != null)
                musicSourceB.outputAudioMixerGroup = musicMixerGroup;

            foreach (AudioSource source in _sfxSources)
            {
                if (source != null)
                    source.outputAudioMixerGroup = sfxMixerGroup;
            }
        }

        private AudioSource GetAvailableSfxSource()
        {
            _sfxSources.RemoveAll(source => source == null);
            _loopingSfxSources.RemoveWhere(source => source == null);

            foreach (AudioSource source in _sfxSources)
            {
                if (!_loopingSfxSources.Contains(source) && !source.isPlaying)
                {
                    source.pitch = 1f;
                    source.loop = false;
                    source.clip = null;
                    source.volume = GetSfxSourceVolume();
                    return source;
                }
            }

            int maxPoolSize = Mathf.Max(Mathf.Max(1, sfxPoolSize), maxSfxPoolSize);
            if (_sfxSources.Count < maxPoolSize)
            {
                AudioSource source = CreateSfxSource();
                _sfxSources.Add(source);
                source.volume = GetSfxSourceVolume();
                AssignMixerGroups();
                return source;
            }

            AudioSource fallback = null;
            foreach (AudioSource source in _sfxSources)
            {
                if (!_loopingSfxSources.Contains(source))
                {
                    fallback = source;
                    break;
                }
            }

            if (fallback == null)
                return null;

            fallback.Stop();
            fallback.pitch = 1f;
            fallback.loop = false;
            fallback.clip = null;
            fallback.volume = GetSfxSourceVolume();
            return fallback;
        }

        private bool TryGetMusicClip(int index, out AudioClip clip)
        {
            clip = null;

            if (!IsValidMusicIndex(index))
            {
                Debug.LogWarning($"[AudioManager] Music index out of range: {index}");
                return false;
            }

            clip = musicClips[index];
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] Music clip at index {index} is not assigned.");
                return false;
            }

            return true;
        }

        private bool IsValidMusicIndex(int index)
        {
            return musicClips != null && index >= 0 && index < musicClips.Length;
        }

        private int GetWrappedMusicIndex(int index)
        {
            if (musicClips == null || musicClips.Length == 0)
                return 0;

            return (index % musicClips.Length + musicClips.Length) % musicClips.Length;
        }

        private int GetMusicClipIndex(AudioClip clip)
        {
            if (clip == null || musicClips == null)
                return -1;

            for (int i = 0; i < musicClips.Length; i++)
            {
                if (musicClips[i] == clip)
                    return i;
            }

            return -1;
        }

        private bool TryGetSceneMusicIndex(string sceneName, out int musicIndex)
        {
            musicIndex = -1;

            if (string.IsNullOrEmpty(sceneName) || sceneMusicSettings == null)
                return false;

            foreach (SceneMusicSetting setting in sceneMusicSettings)
            {
                if (string.Equals(setting.SceneName, sceneName, System.StringComparison.Ordinal))
                {
                    musicIndex = setting.MusicIndex;
                    return IsValidMusicIndex(musicIndex);
                }
            }

            return false;
        }

        private bool ShouldSwitchSceneMusic(int targetMusicIndex)
        {
            if (!IsValidMusicIndex(targetMusicIndex))
                return false;

            AudioClip targetClip = musicClips[targetMusicIndex];
            if (targetClip == null)
                return false;

            return !IsMusicPlaying || CurrentMusicClip != targetClip;
        }

        private void LoadVolumeSettings()
        {
            _musicVolume = saveVolumeSettings ? PlayerPrefs.GetFloat(MusicVolumePrefsKey, initialMusicVolume) : initialMusicVolume;
            _sfxVolume = saveVolumeSettings ? PlayerPrefs.GetFloat(SfxVolumePrefsKey, initialSfxVolume) : initialSfxVolume;
            _musicVolume = Mathf.Clamp01(_musicVolume);
            _sfxVolume = Mathf.Clamp01(_sfxVolume);
        }

        private void ApplyMusicVolume()
        {
            _musicMixerVolumeAvailable = false;

            if (audioMixer != null && !string.IsNullOrWhiteSpace(musicVolumeParameter))
            {
                _musicMixerVolumeAvailable = audioMixer.SetFloat(musicVolumeParameter, LinearToDecibel(_musicVolume));
                if (!_musicMixerVolumeAvailable && !_warnedMissingMusicParameter)
                {
                    Debug.LogWarning($"[AudioManager] AudioMixer parameter not found or not exposed: {musicVolumeParameter}");
                    _warnedMissingMusicParameter = true;
                }
            }

            ApplyMusicSourceVolumes();
        }

        private void ApplySfxVolume()
        {
            _sfxMixerVolumeAvailable = false;

            if (audioMixer != null && !string.IsNullOrWhiteSpace(sfxVolumeParameter))
            {
                _sfxMixerVolumeAvailable = audioMixer.SetFloat(sfxVolumeParameter, LinearToDecibel(_sfxVolume));
                if (!_sfxMixerVolumeAvailable && !_warnedMissingSfxParameter)
                {
                    Debug.LogWarning($"[AudioManager] AudioMixer parameter not found or not exposed: {sfxVolumeParameter}");
                    _warnedMissingSfxParameter = true;
                }
            }

            foreach (AudioSource source in _sfxSources)
            {
                if (source != null)
                    source.volume = GetSfxSourceVolume();
            }
        }

        private void ApplyMusicSourceVolumes()
        {
            if (musicSourceA != null)
                musicSourceA.volume = GetMusicSourceVolume(_musicSourceAFade);

            if (musicSourceB != null)
                musicSourceB.volume = GetMusicSourceVolume(_musicSourceBFade);
        }

        private float GetMusicSourceVolume(float fade)
        {
            return Mathf.Clamp01(fade) * (_musicMixerVolumeAvailable ? 1f : _musicVolume);
        }

        private float GetSfxSourceVolume()
        {
            return _sfxMixerVolumeAvailable ? 1f : _sfxVolume;
        }

        private void SetMusicFade(AudioSource source, float fade)
        {
            if (source == musicSourceA)
                _musicSourceAFade = Mathf.Clamp01(fade);
            else if (source == musicSourceB)
                _musicSourceBFade = Mathf.Clamp01(fade);
        }

        private float GetMusicFade(AudioSource source)
        {
            if (source == musicSourceA)
                return _musicSourceAFade;

            if (source == musicSourceB)
                return _musicSourceBFade;

            return 0f;
        }

        private float ResolveFadeDuration(float fadeDuration)
        {
            return fadeDuration >= 0f ? fadeDuration : Mathf.Max(0f, defaultMusicFadeDuration);
        }

        private float ResolveAuctionFadeDuration(float fadeDuration)
        {
            return fadeDuration >= 0f ? fadeDuration : Mathf.Max(0f, auctionMusicFadeDuration);
        }

        private float GetDeltaTime()
        {
            return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        }

        private float GetAudioTime()
        {
            return useUnscaledTime ? Time.unscaledTime : Time.time;
        }

        private IEnumerator WaitForAudioSeconds(float duration)
        {
            if (duration <= 0f)
                yield break;

            float time = 0f;
            while (time < duration)
            {
                time += GetDeltaTime();
                yield return null;
            }
        }

        private static float LinearToDecibel(float volume)
        {
            if (volume <= 0.0001f)
                return MinMixerVolumeDb;

            return Mathf.Log10(volume) * 20f;
        }
    }
}
