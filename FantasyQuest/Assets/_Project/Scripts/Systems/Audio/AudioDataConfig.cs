using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;

namespace Project.Systems.Audio
{
    [CreateAssetMenu(fileName = "AudioDataConfig", menuName = "Project/Audio/Audio Data Config")]
    public class AudioDataConfig : SerializedScriptableObject
    {
        [System.Serializable]
        public class BGMEntry
        {
            [LabelText("Intro Clip (Optional)")]
            [Tooltip("イントロ部分のクリップ。これが終わるとMain Loop Clipに移行します。")]
            public AudioClip introClip;

            [LabelText("Main Loop Clip")]
            [Required]
            public AudioClip mainLoopClip;

            [LabelText("Vol"), Range(0f, 1f)]
            public float defaultVolume = 1f;
        }

        [System.Serializable]
        public class SEEntry
        {
            [Required]
            public AudioClip clip;

            [LabelText("Vol"), Range(0f, 1f)]
            public float defaultVolume = 1f;

            [LabelText("Random Pitch")]
            [Tooltip("再生時にピッチをランダムに変動させます。足音などに有効です。")]
            public bool useRandomPitch = false;

            [ShowIf("useRandomPitch")]
            [MinMaxSlider(0.5f, 1.5f, true)]
            public Vector2 pitchRange = new Vector2(0.9f, 1.1f);
        }

        [TitleGroup("Mixer Settings")]
        [InfoBox("AudioMixerGroupを指定しない場合はデフォルト経路で再生されます。")]
        public AudioMixerGroup bgmMixerGroup;
        public AudioMixerGroup seMixerGroup;

        [TitleGroup("Background Music Settings")]
        [InfoBox("ID(文字列)をキーにして各オーディオを管理します。")]
        [DictionaryDrawerSettings(KeyLabel = "BGM ID", ValueLabel = "Audio Data", DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        public Dictionary<string, BGMEntry> bgmDictionary = new Dictionary<string, BGMEntry>();

        [TitleGroup("Sound Effect Settings")]
        [DictionaryDrawerSettings(KeyLabel = "SE ID", ValueLabel = "Audio Data", DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        public Dictionary<string, SEEntry> seDictionary = new Dictionary<string, SEEntry>();

        public BGMEntry GetBGMEntry(string id)
        {
            if (bgmDictionary.TryGetValue(id, out BGMEntry entry))
            {
                return entry;
            }
            return null;
        }

        public SEEntry GetSEEntry(string id)
        {
            if (seDictionary.TryGetValue(id, out SEEntry entry))
            {
                return entry;
            }
            return null;
        }
    }
}
