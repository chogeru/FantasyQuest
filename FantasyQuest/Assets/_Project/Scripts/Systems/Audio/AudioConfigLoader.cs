using UnityEngine;
using Sirenix.OdinInspector;

namespace Project.Systems.Audio
{
    /// <summary>
    /// 特定のシーンやオブジェクトごとに固有のAudioDataConfigをAudioManagerへ
    /// 動的に登録(Register)・解除(Unregister)するためのヘルパーコンポーネント。
    /// これにより、そのシーンでしか使わないBGM/SEを節約して運用できます。
    /// </summary>
    public class AudioConfigLoader : MonoBehaviour
    {
        [InfoBox("このシーン(またはオブジェクト)で追加読み込みしたいサウンド設定をアタッチします。")]
        [SerializeField, Required]
        private AudioDataConfig _sceneAudioConfig;

        [Tooltip("OnEnable/OnDisableで自動的に登録・解除を行うフラグ")]
        [SerializeField] private bool _autoRegisterOnEnable = true;

        private void OnEnable()
        {
            if (_autoRegisterOnEnable)
            {
                Register();
            }
        }

        private void OnDisable()
        {
            if (_autoRegisterOnEnable)
            {
                Unregister();
            }
        }

        [Button("Register Config manually", ButtonSizes.Large)]
        public void Register()
        {
            if (AudioManager.Instance != null && _sceneAudioConfig != null)
            {
                AudioManager.Instance.RegisterConfig(_sceneAudioConfig);
            }
        }

        [Button("Unregister Config manually", ButtonSizes.Large)]
        public void Unregister()
        {
            if (AudioManager.Instance != null && _sceneAudioConfig != null)
            {
                AudioManager.Instance.UnregisterConfig(_sceneAudioConfig);
            }
        }
    }
}
