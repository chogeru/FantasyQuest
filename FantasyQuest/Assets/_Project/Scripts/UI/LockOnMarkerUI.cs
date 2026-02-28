using UnityEngine;
using Project.Core.CameraSystem;

namespace Project.UI
{
    /// <summary>
    /// TargetLockOnシステムと連動し、現在ロックオンしている敵めがけて
    /// 画面上に照準（マーカー）アイコンのUIを表示します。
    /// （※CanvasのRender Modeは「Screen Space - Overlay」を想定）
    /// </summary>
    public class LockOnMarkerUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("ターゲット情報を取得する大元のTargetLockOnスクリプト")]
        [SerializeField] private TargetLockOn _targetLockOn;
        [Tooltip("画面上に表示する照準UI（Imageなど）")]
        [SerializeField] private RectTransform _markerRect;

        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;

            if (_markerRect != null)
            {
                _markerRect.gameObject.SetActive(false); // 初期は消す
            }
        }

        private void LateUpdate()
        {
            if (_targetLockOn == null || _markerRect == null || _mainCamera == null) return;

            // ロックオン中でない場合、またはターゲットが存在しない場合は消す
            if (!_targetLockOn.IsLockedOn || _targetLockOn.GetTarget() == null)
            {
                if (_markerRect.gameObject.activeSelf) _markerRect.gameObject.SetActive(false);
                return;
            }

            // ロックオン中の場合、UIを表示
            if (!_markerRect.gameObject.activeSelf) _markerRect.gameObject.SetActive(true);

            // 敵の中心（ワールド座標）をスクリーン座標（画面上のピクセル位置）に変換する
            Vector3 targetWorldPosition = _targetLockOn.GetTarget().position;
            
            // Note: 敵の少し上（または胸の高さ）あたりにマーカーを出したい場合はオフセットする
            targetWorldPosition += Vector3.up * 1.5f;

            Vector3 screenPosition = _mainCamera.WorldToScreenPoint(targetWorldPosition);

            // 画面の裏側に居る時（Zがマイナス）は描画しない（カメラの仕組み上、背後も映ってしまうため）
            if (screenPosition.z < 0)
            {
                _markerRect.gameObject.SetActive(false);
            }
            else
            {
                // マーカーの位置を更新
                _markerRect.position = screenPosition;
            }

            // オプション: ロックオンマーカーを常にクルクル回す等のアニメーションもここに入れられます
            // _markerRect.Rotate(0, 0, 90f * Time.deltaTime);
        }
    }
}
