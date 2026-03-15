using UnityEngine;
using System.Collections.Generic;

namespace Project.Systems.Spawning
{
    /// <summary>
    /// プレイヤーが近づいた時に、配下のEnemySpawnPointを一斉に発火させるスポーン管理者。
    /// （ウェーブ制など今後の拡張も考慮した設計）
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [Tooltip("プレイヤーがこの距離に入ったらスポーンを開始する")]
        public float TriggerRadius = 15f;
        
        [Tooltip("一度しかスポーンさせない場合はtrue（時間を空けて再湧きさせたいならfalse）")]
        public bool SpawnOnce = true;

        [Header("Spawn Points")]
        [Tooltip("空白の場合、起動時に子オブジェクトにくっついているSpawnPointを自動取得します")]
        public List<EnemySpawnPoint> SpawnPoints = new List<EnemySpawnPoint>();

        private bool _hasSpawned = false;
        private Transform _playerTransform;
        
        // 生成した敵の参照を保持する（ウェーブや再スポーン拡張用）
        private List<GameObject> _spawnedEnemies = new List<GameObject>();

        private void Start()
        {
            // 子要素からポイントを自動取得
            if (SpawnPoints.Count == 0)
            {
                SpawnPoints.AddRange(GetComponentsInChildren<EnemySpawnPoint>());
            }

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
        }

        private void Update()
        {
            // すでにスポーン済みで、再スポーン不可のワンタイムスポーナーなら処理しない
            if (SpawnOnce && _hasSpawned) return;
            
            if (_playerTransform == null) return;

            // プレイヤーとの距離判定
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            if (distance <= TriggerRadius)
            {
                if (!_hasSpawned)
                {
                    SpawnAll();
                }
                else if (!SpawnOnce)
                {
                    // 拡張用: 再スポーンが許可されている場合、全滅したら再湧きさせるなどの処理
                    CheckAndRespawnIfNeeded();
                }
            }
        }

        [ContextMenu("Force Spawn All")]
        public void SpawnAll()
        {
            _hasSpawned = true;
            _spawnedEnemies.Clear();

            foreach (var point in SpawnPoints)
            {
                if (point != null && point.gameObject.activeInHierarchy)
                {
                    GameObject enemy = point.Spawn();
                    if (enemy != null)
                    {
                        _spawnedEnemies.Add(enemy);
                    }
                }
            }
            
            Debug.Log($"<color=cyan>[EnemySpawner]</color> {gameObject.name} から {_spawnedEnemies.Count} 体の敵がスポーンしました。");
        }

        /// <summary>
        /// 全滅を検知したらフラグを戻し、プレイヤーが再び範囲内に入った時に再スポーンさせる
        /// （再湧きクールタイムを入れる場合はここにタイマーを追加）
        /// </summary>
        private void CheckAndRespawnIfNeeded()
        {
            // 破壊された敵（Nullになった参照）をリストから除外
            _spawnedEnemies.RemoveAll(e => e == null);
            
            if (_spawnedEnemies.Count == 0)
            {
                // ここに遅延（タイマー）を入れるとより自然になりますが、今回は即時フラグ変更
                _hasSpawned = false; 
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // スポーン検知範囲を可視化（緑のワイヤーフレーム）
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, TriggerRadius);
        }
#endif
    }
}
