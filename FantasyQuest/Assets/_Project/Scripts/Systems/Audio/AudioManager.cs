using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;

namespace Project.Systems.Audio
{
    public class AudioManager : SerializedMonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string PrefsMasterVol = "Audio_MasterVolume";
        private const string PrefsBgmVol = "Audio_BgmVolume";
        private const string PrefsSeVol = "Audio_SeVolume";

        [TitleGroup("Loaded Configurations")]
        [SerializeField, Required]
        private AudioDataConfig _commonConfig;

        [ShowInInspector, ReadOnly, ListDrawerSettings(Expanded = true, IsReadOnly = true)]
        private List<AudioDataConfig> _activeConfigs = new List<AudioDataConfig>();

        [TitleGroup("Mixer Settings")]
        [Tooltip("全体を統括するメインのAudioMixer（オプション）")]
        [SerializeField] private AudioMixer _mainAudioMixer;
        [SerializeField] private string _mixerMasterParam = "MasterVolume";
        [SerializeField] private string _mixerBGMParam = "BGMVolume";
        [SerializeField] private string _mixerSEParam = "SEVolume";

        [TitleGroup("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource1;
        [SerializeField] private AudioSource _bgmSource2;
        [SerializeField] private AudioSource _seSourcePrefab;
        [SerializeField] private int _initialSEPoolSize = 10;

        private bool _isBgmSource1Playing = true;
        private Coroutine _fadeCoroutine;
        private Coroutine _bgmIntroRoutine; // イントロ→ループ用のコルーチン

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Info")]
        private List<AudioSource> _sePool = new List<AudioSource>();

        [TitleGroup("Global Volume Settings")]
        [PropertyRange(0.0001f, 1f), OnValueChanged("OnVolumeChanged")] 
        public float masterVolume = 1f;
        
        [PropertyRange(0.0001f, 1f), OnValueChanged("OnVolumeChanged")] 
        public float bgmVolume = 1f;

        [PropertyRange(0.0001f, 1f), OnValueChanged("OnVolumeChanged")] 
        public float seVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadVolumeSettings();

            if (_commonConfig != null && !_activeConfigs.Contains(_commonConfig))
            {
                _activeConfigs.Add(_commonConfig);
            }

            InitializeAudioSources();
            ApplyVolumeToMixer();
        }

        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(PrefsMasterVol, 1f);
            bgmVolume = PlayerPrefs.GetFloat(PrefsBgmVol, 1f);
            seVolume = PlayerPrefs.GetFloat(PrefsSeVol, 1f);
        }

        public void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(PrefsMasterVol, masterVolume);
            PlayerPrefs.SetFloat(PrefsBgmVol, bgmVolume);
            PlayerPrefs.SetFloat(PrefsSeVol, seVolume);
            PlayerPrefs.Save();
        }

        private void InitializeAudioSources()
        {
            if (_bgmSource1 == null) _bgmSource1 = gameObject.AddComponent<AudioSource>();
            if (_bgmSource2 == null) _bgmSource2 = gameObject.AddComponent<AudioSource>();

            _bgmSource1.loop = true;
            _bgmSource2.loop = true;
            _bgmSource1.playOnAwake = false;
            _bgmSource2.playOnAwake = false;

            if (_commonConfig != null && _commonConfig.bgmMixerGroup != null)
            {
                _bgmSource1.outputAudioMixerGroup = _commonConfig.bgmMixerGroup;
                _bgmSource2.outputAudioMixerGroup = _commonConfig.bgmMixerGroup;
            }

            if (_seSourcePrefab == null)
            {
                GameObject go = new GameObject("SE_Prefab");
                go.transform.SetParent(transform);
                var audioSrc = go.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
                audioSrc.spatialBlend = 0f; // デフォルトは2D
                _seSourcePrefab = audioSrc;
            }

            for (int i = 0; i < _initialSEPoolSize; i++)
            {
                CreateSESource();
            }
        }

        private AudioSource CreateSESource()
        {
            AudioSource newSource = Instantiate(_seSourcePrefab, transform);
            newSource.gameObject.SetActive(false);
            
            if (_commonConfig != null && _commonConfig.seMixerGroup != null)
            {
                newSource.outputAudioMixerGroup = _commonConfig.seMixerGroup;
            }

            _sePool.Add(newSource);
            return newSource;
        }

        private AudioSource GetSESource()
        {
            foreach (var source in _sePool)
            {
                if (!source.gameObject.activeInHierarchy || !source.isPlaying)
                {
                    source.gameObject.SetActive(true);
                    return source;
                }
            }
            return CreateSESource();
        }

        // --- Config Management ---
        public void RegisterConfig(AudioDataConfig config)
        {
            if (config != null && !_activeConfigs.Contains(config))
                _activeConfigs.Add(config);
        }

        public void UnregisterConfig(AudioDataConfig config)
        {
            if (config != null && _activeConfigs.Contains(config) && config != _commonConfig)
                _activeConfigs.Remove(config);
        }

        private AudioDataConfig.BGMEntry FindBGMEntry(string id, out AudioDataConfig sourceConfig)
        {
            sourceConfig = null;
            for (int i = _activeConfigs.Count - 1; i >= 0; i--)
            {
                if (_activeConfigs[i] == null) continue;
                
                var entry = _activeConfigs[i].GetBGMEntry(id);
                if (entry != null)
                {
                    sourceConfig = _activeConfigs[i];
                    return entry;
                }
            }
            return null;
        }

        private AudioDataConfig.SEEntry FindSEEntry(string id, out AudioDataConfig sourceConfig)
        {
            sourceConfig = null;
            for (int i = _activeConfigs.Count - 1; i >= 0; i--)
            {
                if (_activeConfigs[i] == null) continue;

                var entry = _activeConfigs[i].GetSEEntry(id);
                if (entry != null)
                {
                    sourceConfig = _activeConfigs[i];
                    return entry;
                }
            }
            return null;
        }

        // --- Playback Controls ---

        public void PlayBGM(string id, float fadeDuration = 1.0f)
        {
            var entry = FindBGMEntry(id, out AudioDataConfig config);
            if (entry == null || entry.mainLoopClip == null)
            {
                Debug.LogWarning($"AudioManager: BGM ID '{id}' not found or invalid.");
                return;
            }

            AudioSource activeSource = _isBgmSource1Playing ? _bgmSource1 : _bgmSource2;
            AudioSource nextSource = _isBgmSource1Playing ? _bgmSource2 : _bgmSource1;

            // Mixer Group Override
            if (config.bgmMixerGroup != null)
            {
                nextSource.outputAudioMixerGroup = config.bgmMixerGroup;
            }

            if (_bgmIntroRoutine != null) StopCoroutine(_bgmIntroRoutine);

            if (entry.introClip != null)
            {
                // Intro -> Loop
                if (activeSource.clip == entry.introClip && activeSource.isPlaying) return;
                
                nextSource.clip = entry.introClip;
                nextSource.loop = false;
                nextSource.volume = 0f;
                nextSource.Play();

                _bgmIntroRoutine = StartCoroutine(HandleBGMIntro(nextSource, entry.introClip, entry.mainLoopClip));
            }
            else
            {
                // Standard Loop
                if (activeSource.clip == entry.mainLoopClip && activeSource.isPlaying) return;

                nextSource.clip = entry.mainLoopClip;
                nextSource.loop = true;
                nextSource.volume = 0f;
                nextSource.Play();
            }

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossFadeBGM(activeSource, nextSource, fadeDuration, entry.defaultVolume));

            _isBgmSource1Playing = !_isBgmSource1Playing;
        }

        private IEnumerator HandleBGMIntro(AudioSource source, AudioClip intro, AudioClip mainLoop)
        {
            // WaitForSecondsRealtime is better to avoid timeScale issues, 
            // but for exact audio stitching PlayScheduled is technically superior. 
            // We'll use a simple yield for this implementation.
            yield return new WaitForSeconds(intro.length);
            source.clip = mainLoop;
            source.loop = true;
            source.Play();
        }

        public void StopBGM(float fadeDuration = 1.0f)
        {
            AudioSource activeSource = _isBgmSource1Playing ? _bgmSource1 : _bgmSource2;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            if (_bgmIntroRoutine != null) StopCoroutine(_bgmIntroRoutine);
            
            _fadeCoroutine = StartCoroutine(FadeOutBGM(activeSource, fadeDuration));
        }

        /// <summary>
        /// 通常のSE (2D)
        /// </summary>
        public void PlaySE(string id)
        {
            PlaySEInternal(id, Vector3.zero, false);
        }

        /// <summary>
        /// 3D空間上のSE
        /// </summary>
        public void PlaySE3D(string id, Vector3 position)
        {
            PlaySEInternal(id, position, true);
        }

        private void PlaySEInternal(string id, Vector3 pos, bool is3D)
        {
            var entry = FindSEEntry(id, out AudioDataConfig config);
            if (entry == null || entry.clip == null)
            {
                Debug.LogWarning($"AudioManager: SE ID '{id}' not found.");
                return;
            }

            AudioSource source = GetSESource();
            source.transform.position = pos;
            source.clip = entry.clip;
            source.spatialBlend = is3D ? 1f : 0f;

            if (config.seMixerGroup != null)
                source.outputAudioMixerGroup = config.seMixerGroup;

            source.pitch = entry.useRandomPitch 
                ? Random.Range(entry.pitchRange.x, entry.pitchRange.y) 
                : 1f;

            // Mixerを使わない場合の独自計算（Mixer利用時はそちらに任せるが後方互換で残す）
            source.volume = entry.defaultVolume * seVolume * masterVolume;
            
            source.Play();
            StartCoroutine(ReturnSESourceToPool(source, entry.clip.length / source.pitch));
        }

        // --- Coroutines ---

        private IEnumerator ReturnSESourceToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay);
            if(source != null) source.gameObject.SetActive(false);
        }

        private IEnumerator CrossFadeBGM(AudioSource fadeOutSource, AudioSource fadeInSource, float duration, float targetVolume)
        {
            float timer = 0f;
            float startVolume = fadeOutSource.volume;
            float finalVolume = targetVolume * (_mainAudioMixer == null ? bgmVolume * masterVolume : targetVolume); // MixerがあればMixerが音量制御

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float normalizedTime = timer / duration;

                if (fadeOutSource.isPlaying)
                    fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, normalizedTime);
                
                fadeInSource.volume = Mathf.Lerp(0f, finalVolume, normalizedTime);
                yield return null;
            }

            fadeOutSource.Stop();
            fadeOutSource.volume = 0f;
            fadeInSource.volume = finalVolume;
        }

        private IEnumerator FadeOutBGM(AudioSource fadeOutSource, float duration)
        {
            float timer = 0f;
            float startVolume = fadeOutSource.volume;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
                yield return null;
            }

            fadeOutSource.Stop();
            fadeOutSource.volume = 0f;
        }

        private void OnVolumeChanged()
        {
            ApplyVolumeToMixer();

            // Mixerがない場合のフォールバック
            if (_mainAudioMixer == null)
            {
                AudioSource activeSource = _isBgmSource1Playing ? _bgmSource1 : _bgmSource2;
                if (activeSource != null && activeSource.isPlaying)
                {
                    activeSource.volume = masterVolume * bgmVolume; 
                }
            }
            
            // 値の変更を即座にセーブ
            SaveVolumeSettings();
        }

        private void ApplyVolumeToMixer()
        {
            if (_mainAudioMixer == null) return;

            // Linear to Decibel conversion
            _mainAudioMixer.SetFloat(_mixerMasterParam, Mathf.Log10(Mathf.Max(0.0001f, masterVolume)) * 20f);
            _mainAudioMixer.SetFloat(_mixerBGMParam, Mathf.Log10(Mathf.Max(0.0001f, bgmVolume)) * 20f);
            _mainAudioMixer.SetFloat(_mixerSEParam, Mathf.Log10(Mathf.Max(0.0001f, seVolume)) * 20f);
        }
    }
}
