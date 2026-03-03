using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Project.Systems.Audio
{
    /// <summary>
    /// シーン間でBGM/SEを管理するシングルトンクラス (Odin対応版)
    /// 必要な時だけシーン固有のAudioConfigを登録(RegisterConfig) / 解除(UnregisterConfig)し、
    /// 不要なメモリ消費を抑えることができる設計。
    /// </summary>
    public class AudioManager : SerializedMonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [TitleGroup("Loaded Configurations", "現在ロードされているAudio Configのリスト")]
        [InfoBox("Common Configはアプリ起動直後から常に保持される共通サウンドです。")]
        [SerializeField, Required]
        private AudioDataConfig _commonConfig;

        [ShowInInspector, ReadOnly, ListDrawerSettings(Expanded = true, IsReadOnly = true)]
        private List<AudioDataConfig> _activeConfigs = new List<AudioDataConfig>();

        [TitleGroup("Audio Sources")]
        [SerializeField] private AudioSource _bgmSource1;
        [SerializeField] private AudioSource _bgmSource2;
        [SerializeField] private AudioSource _seSourcePrefab;
        [SerializeField] private int _initialSEPoolSize = 10;

        private bool _isBgmSource1Playing = true;
        private Coroutine _fadeCoroutine;

        [ShowInInspector, ReadOnly, FoldoutGroup("Debug Info")]
        private List<AudioSource> _sePool = new List<AudioSource>();

        [TitleGroup("Global Volume Settings")]
        [PropertyRange(0f, 1f), OnValueChanged("OnVolumeChanged")] 
        public float masterVolume = 1f;
        
        [PropertyRange(0f, 1f), OnValueChanged("OnVolumeChanged")] 
        public float bgmVolume = 1f;

        [PropertyRange(0f, 1f), OnValueChanged("OnVolumeChanged")] 
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

            if (_commonConfig != null && !_activeConfigs.Contains(_commonConfig))
            {
                _activeConfigs.Add(_commonConfig);
            }

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            if (_bgmSource1 == null) _bgmSource1 = gameObject.AddComponent<AudioSource>();
            if (_bgmSource2 == null) _bgmSource2 = gameObject.AddComponent<AudioSource>();

            _bgmSource1.loop = true;
            _bgmSource2.loop = true;
            _bgmSource1.playOnAwake = false;
            _bgmSource2.playOnAwake = false;

            if (_seSourcePrefab == null)
            {
                GameObject go = new GameObject("SE_Prefab");
                go.transform.SetParent(transform);
                var audioSrc = go.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
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

        /// <summary>
        /// 新しいオーディオ設定情報を登録します(シーン遷移時など)。
        /// </summary>
        [Button("Register Config", ButtonSizes.Medium), FoldoutGroup("Config Management")]
        public void RegisterConfig(AudioDataConfig config)
        {
            if (config != null && !_activeConfigs.Contains(config))
            {
                _activeConfigs.Add(config);
            }
        }

        /// <summary>
        /// オーディオ設定情報を解除し、メモリ参照を外します。
        /// </summary>
        [Button("Unregister Config", ButtonSizes.Medium), FoldoutGroup("Config Management")]
        public void UnregisterConfig(AudioDataConfig config)
        {
            if (config != null && _activeConfigs.Contains(config) && config != _commonConfig)
            {
                _activeConfigs.Remove(config);
            }
        }

        [Button("Clear All Temp Configs", ButtonSizes.Medium), FoldoutGroup("Config Management")]
        public void ClearTemporaryConfigs()
        {
            _activeConfigs.Clear();
            if (_commonConfig != null)
            {
                _activeConfigs.Add(_commonConfig);
            }
        }

        // 登録されている全てのConfigから対象のBGMを検索
        private AudioClip FindBGMClip(string id, out float volume)
        {
            volume = 1f;
            // 後から追加されたConfig（インデックスが大きい方）を優先する
            for (int i = _activeConfigs.Count - 1; i >= 0; i--)
            {
                if (_activeConfigs[i] == null) continue;
                
                AudioClip clip = _activeConfigs[i].GetBGMClip(id, out float confVol);
                if (clip != null)
                {
                    volume = confVol;
                    return clip;
                }
            }
            return null;
        }

        // 登録されている全てのConfigから対象のSEを検索
        private AudioClip FindSEClip(string id, out float volume)
        {
            volume = 1f;
            for (int i = _activeConfigs.Count - 1; i >= 0; i--)
            {
                if (_activeConfigs[i] == null) continue;

                AudioClip clip = _activeConfigs[i].GetSEClip(id, out float confVol);
                if (clip != null)
                {
                    volume = confVol;
                    return clip;
                }
            }
            return null;
        }

        // --- Playback Controls ---

        public void PlayBGM(string id, float fadeDuration = 1.0f)
        {
            AudioClip clip = FindBGMClip(id, out float clipVolume);
            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: BGM ID '{id}' not found in any active configs.");
                return;
            }

            AudioSource activeSource = _isBgmSource1Playing ? _bgmSource1 : _bgmSource2;
            if (activeSource.clip == clip && activeSource.isPlaying) return;

            AudioSource nextSource = _isBgmSource1Playing ? _bgmSource2 : _bgmSource1;
            nextSource.clip = clip;
            nextSource.volume = 0f;
            nextSource.Play();

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossFadeBGM(activeSource, nextSource, fadeDuration, clipVolume));

            _isBgmSource1Playing = !_isBgmSource1Playing;
        }

        public void StopBGM(float fadeDuration = 1.0f)
        {
            AudioSource activeSource = _isBgmSource1Playing ? _bgmSource1 : _bgmSource2;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutBGM(activeSource, fadeDuration));
        }

        public void PlaySE(string id)
        {
            AudioClip clip = FindSEClip(id, out float clipVolume);
            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: SE ID '{id}' not found in any active configs.");
                return;
            }

            AudioSource source = GetSESource();
            source.clip = clip;
            source.volume = clipVolume * seVolume * masterVolume;
            source.Play();

            StartCoroutine(ReturnSESourceToPool(source, clip.length));
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
            float finalVolume = targetVolume * bgmVolume * masterVolume;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float normalizedTime = timer / duration;

                if (fadeOutSource.isPlaying)
                {
                    fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, normalizedTime);
                }
                
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
            AudioSource activeSource = _isBgmSource1Playing ? _bgmSource1 : _bgmSource2;
            if (activeSource != null && activeSource.isPlaying)
            {
                // To keep it simple, we just apply the master & bgm ratio
                activeSource.volume = masterVolume * bgmVolume; 
            }
        }
    }
}
