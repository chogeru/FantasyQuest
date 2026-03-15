using System.Collections.Generic;
using UnityEngine;
using Project.Systems.Environment;

namespace Project.Systems.Spawning
{
    /// <summary>
    /// 環境要因（時間帯・天候）によって出現する敵の種類や確率を変えるための ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpawnTable", menuName = "FantasyQuest/Spawning/Spawn Table")]
    public class SpawnTable : ScriptableObject
    {
        [System.Serializable]
        public class SpawnEntry
        {
            [Tooltip("スポーンさせる敵のプレハブ")]
            public GameObject EnemyPrefab;
            
            [Tooltip("出やすさの重み（値が大きいほど出やすい）")]
            [Range(1, 100)] public int Weight = 10;
        }

        [System.Serializable]
        public class ConditionGroup
        {
            public string GroupName = "New Condition";
            
            [Tooltip("このテーブルラインナップが有効になる時間帯")]
            public List<TimeOfDay> ValidTimes = new List<TimeOfDay> { TimeOfDay.Day, TimeOfDay.Night };
            
            [Tooltip("このテーブルラインナップが有効になる天候")]
            public List<WeatherCondition> ValidWeathers = new List<WeatherCondition> { WeatherCondition.Clear, WeatherCondition.Rain, WeatherCondition.Fog };
            
            [Tooltip("この条件の時に出現・抽選される敵のリストとその確率")]
            public List<SpawnEntry> Entries = new List<SpawnEntry>();
        }

        [Header("Spawn Conditions")]
        [Tooltip("環境条件ごとのスポーン定義。上から順に判定され、最初に条件に合致したグループが決定されます。")]
        public List<ConditionGroup> ConditionGroups = new List<ConditionGroup>();
        
        [Header("Default Settings")]
        [Tooltip("上記のどの条件にも当てはまらなかった場合（または設定忘れ）のデフォルト枠")]
        public List<SpawnEntry> DefaultEntries = new List<SpawnEntry>();

        /// <summary>
        /// 現在の環境に基づいて、確率計算（ガチャ）を行い敵のプレハブを1つを決定して返す
        /// </summary>
        public GameObject GetRandomEnemy(TimeOfDay currentTime, WeatherCondition currentWeather)
        {
            List<SpawnEntry> targetEntries = DefaultEntries;

            // 上から順に条件をチェックし、合致するグループを探す
            foreach (var group in ConditionGroups)
            {
                if (group.ValidTimes.Contains(currentTime) && group.ValidWeathers.Contains(currentWeather))
                {
                    targetEntries = group.Entries;
                    break;
                }
            }

            if (targetEntries == null || targetEntries.Count == 0)
            {
                return null;
            }

            // 重み付け抽選（Weighted Random Gacha）
            int totalWeight = 0;
            foreach (var entry in targetEntries)
            {
                if (entry.EnemyPrefab != null) // nullは無視
                {
                    totalWeight += entry.Weight;
                }
            }
            
            if (totalWeight == 0) return null;

            int randomVal = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in targetEntries)
            {
                if (entry.EnemyPrefab == null) continue;
                
                currentWeight += entry.Weight;
                if (randomVal < currentWeight)
                {
                    return entry.EnemyPrefab;
                }
            }

            // 万が一のフォールバック
            return targetEntries[0].EnemyPrefab; 
        }
    }
}
