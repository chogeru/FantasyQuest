using UnityEngine;

namespace Project.Systems.Spawning
{
    /// <summary>
    /// 個別の敵のスポーン位置を定義し、エディタ上で分かりやすく可視化するコンポーネント。
    /// 接地（スナップ）機能により、空中に浮いたり地面に埋まったりするのを防ぎます。
    /// </summary>
    public class EnemySpawnPoint : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("時間・天候などで柔軟に出現敵が変わるスポーンテーブル（こちらが優先）")]
        public SpawnTable DynamicSpawnTable;

        [Tooltip("テーブルを利用しない場合の固定の敵プレハブ")]
        public GameObject EnemyPrefab;
        
        [Tooltip("スポーン時に自動で地面にスナップさせるか（水生・飛行エネミーの場合はオフを推奨）")]
        public bool SnapToGroundOnSpawn = true;
        
        [Tooltip("地面と判定するレイヤー")]
        public LayerMask GroundLayer = ~0; // 全レイヤーを含める（必要に応じて変更）

        [Header("Editor Visualization")]
        [Tooltip("エディタ上で表示するギズモの色")]
        public Color GizmoColor = new Color(1f, 0.2f, 0.2f, 0.7f);
        public float GizmoRadius = 0.5f;

        // 実行時に呼ばれるスポーン処理
        public GameObject Spawn()
        {
            GameObject prefabToSpawn = EnemyPrefab;

            // テーブルが設定されていれば、現在の環境要因を使って動的にプレハブを決定する
            if (DynamicSpawnTable != null)
            {
                if (Project.Systems.Environment.EnvironmentManager.Instance != null)
                {
                    var env = Project.Systems.Environment.EnvironmentManager.Instance;
                    prefabToSpawn = DynamicSpawnTable.GetRandomEnemy(env.CurrentTime, env.CurrentWeather);
                }
                else
                {
                    // 環境マネージャーがシーンにない場合は、デフォルト（昼・晴れ）のリストから抽選
                    prefabToSpawn = DynamicSpawnTable.GetRandomEnemy(Project.Systems.Environment.TimeOfDay.Day, Project.Systems.Environment.WeatherCondition.Clear);
                }
            }

            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"[EnemySpawnPoint] {gameObject.name} にスポーン対象のプレハブがありません。テーブルまたは固定Prefabの設定を確認してください。");
                return null;
            }

            Vector3 spawnPos = transform.position;
            if (SnapToGroundOnSpawn)
            {
                spawnPos = GetSnappedPosition();
            }

            return Instantiate(prefabToSpawn, spawnPos, transform.rotation);
        }

        // 接地位置を計算して返す
        public Vector3 GetSnappedPosition()
        {
            // 少し上からレイを飛ばして地面を探す（めり込み防止）
            Vector3 origin = transform.position + Vector3.up * 2f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f, GroundLayer))
            {
                return hit.point;
            }
            return transform.position; // 見つからなければ元の位置をそのまま使う
        }

        // エディタのコンテキストメニュー（歯車アイコン）から手動で接地させる
        [ContextMenu("Snap To Ground Now (今すぐ接地させる)")]
        public void SnapNow()
        {
            transform.position = GetSnappedPosition();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[EnemySpawnPoint] {gameObject.name} を地面にスナップしました。");
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = GizmoColor;
            
            // スポーン位置を球で表示
            Vector3 center = transform.position + Vector3.up * GizmoRadius;
            Gizmos.DrawSphere(center, GizmoRadius);
            
            // 向いている方向（前方）を矢印のように線で描画
            Gizmos.color = Color.white;
            Vector3 forward = transform.forward * 1.5f;
            Gizmos.DrawRay(center, forward);
            // 矢印の先端の羽
            Gizmos.DrawRay(center + forward, -transform.forward * 0.3f + transform.right * 0.3f);
            Gizmos.DrawRay(center + forward, -transform.forward * 0.3f - transform.right * 0.3f);

            // 床までのレイを可視化（接地オンの場合）
            if (SnapToGroundOnSpawn)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // 半透明の黄色
                Vector3 snappedPos = GetSnappedPosition();
                Gizmos.DrawLine(center, snappedPos);
                Gizmos.DrawWireCube(snappedPos, new Vector3(GizmoRadius * 2, 0.05f, GizmoRadius * 2)); // 接地点の目印
            }
        }
#endif
    }
}
