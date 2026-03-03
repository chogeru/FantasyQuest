using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace Project.Systems.Audio
{
    [CreateAssetMenu(fileName = "AudioDataConfig", menuName = "Project/Audio/Audio Data Config")]
    public class AudioDataConfig : SerializedScriptableObject
    {
        [System.Serializable]
        public class AudioEntry
        {
            [HideLabel, HorizontalGroup("Entry")]
            public AudioClip clip;

            [LabelText("Vol"), Range(0f, 1f), HorizontalGroup("Entry", Width = 100)]
            public float defaultVolume = 1f;
        }

        [InfoBox("ID(文字列)をキーにして各オーディオを管理します。")]
        [TitleGroup("Background Music Settings")]
        [DictionaryDrawerSettings(KeyLabel = "BGM ID", ValueLabel = "Audio Data", DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        public Dictionary<string, AudioEntry> bgmDictionary = new Dictionary<string, AudioEntry>();

        [TitleGroup("Sound Effect Settings")]
        [DictionaryDrawerSettings(KeyLabel = "SE ID", ValueLabel = "Audio Data", DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
        public Dictionary<string, AudioEntry> seDictionary = new Dictionary<string, AudioEntry>();

        public AudioClip GetBGMClip(string id, out float volume)
        {
            volume = 1f;
            if (bgmDictionary.TryGetValue(id, out AudioEntry entry))
            {
                volume = entry.defaultVolume;
                return entry.clip;
            }
            return null;
        }

        public AudioClip GetSEClip(string id, out float volume)
        {
            volume = 1f;
            if (seDictionary.TryGetValue(id, out AudioEntry entry))
            {
                volume = entry.defaultVolume;
                return entry.clip;
            }
            return null;
        }
    }
}
