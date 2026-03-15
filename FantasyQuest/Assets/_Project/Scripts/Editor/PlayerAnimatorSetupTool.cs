using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace Project.Editor
{
    /// <summary>
    /// Player用のAnimatorControllerをアニメーションクリップ付きで自動生成・セットアップするウィザード
    /// </summary>
    public class PlayerAnimatorSetupWizard : ScriptableWizard
    {
        [Header("Locomotion (移動・待機)")]
        [Tooltip("待機モーション (Speed = 0)")]
        public AnimationClip idleClip;
        [Tooltip("歩きモーション (Speed = 3)")]
        public AnimationClip walkClip;
        [Tooltip("走りモーション (Speed = 6.5)")]
        public AnimationClip sprintClip;

        [Header("Air (ジャンプ・落下)")]
        [Tooltip("落下モーション (VerticalVelocity = -5)")]
        public AnimationClip fallClip;
        [Tooltip("ジャンプ上昇モーション (VerticalVelocity = 5)")]
        public AnimationClip jumpRisingClip;

        [Header("Swimming (泳ぎ)")]
        [Tooltip("泳ぎモーション")]
        public AnimationClip swimClip;

        [Header("Combat (攻撃)")]
        [Tooltip("基本の攻撃モーション")]
        public AnimationClip attackClip;

        [MenuItem("FantasyQuest/Tools/Create Player Animator Controller")]
        public static void CreateWizard()
        {
            // ウィザードウィンドウを表示する
            ScriptableWizard.DisplayWizard<PlayerAnimatorSetupWizard>(
                "Setup Player Animator", 
                "Create Controller" // 設定用ボタン名
            );
        }

        // Createボタンが押された時の処理
        private void OnWizardCreate()
        {
            string defaultName = "PlayerAnimatorController";
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Animator Controller",
                defaultName,
                "controller",
                "作成するAnimatorControllerの保存先を選んでください"
            );

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            // AnimatorControllerの新規作成
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // 必要なパラメータを追加
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
            controller.AddParameter("VerticalVelocity", AnimatorControllerParameterType.Float);
            controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsSwimming", AnimatorControllerParameterType.Bool);
            controller.AddParameter("ComboStep", AnimatorControllerParameterType.Int);

            // ルートステートマシンを取得
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;

            // === 1. Locomotion (移動・待機) ステートの作成 ===
            AnimatorState locomotionState = rootStateMachine.AddState("Locomotion");
            // ブレンドツリーを手動作成してアセットに追加
            BlendTree locomotionBlendTree = new BlendTree();
            locomotionBlendTree.name = "LocomotionBlendTree";
            locomotionBlendTree.blendType = BlendTreeType.Simple1D;
            locomotionBlendTree.blendParameter = "Speed";
            locomotionBlendTree.useAutomaticThresholds = false; // しきい値を自前で制御

            // インスペクターで設定された各アニメーションクリップをBlendTreeに登録
            locomotionBlendTree.AddChild(idleClip, 0f);
            locomotionBlendTree.AddChild(walkClip, 3f);
            locomotionBlendTree.AddChild(sprintClip, 6.5f);
            
            // コントローラー内部にブレンドツリーのデータを保存
            AssetDatabase.AddObjectToAsset(locomotionBlendTree, controller);
            locomotionState.motion = locomotionBlendTree;

            // === 2. Air (ジャンプ・落下) ステートの作成 ===
            AnimatorState airState = rootStateMachine.AddState("Air");
            BlendTree airBlendTree = new BlendTree();
            airBlendTree.name = "AirBlendTree";
            airBlendTree.blendType = BlendTreeType.Simple1D;
            airBlendTree.blendParameter = "VerticalVelocity";
            airBlendTree.useAutomaticThresholds = false;

            // 落下・上昇モーションを割り当て (しきい値を狭くして最高地点でのブレンドを素早く切り替える)
            airBlendTree.AddChild(fallClip, -0.1f);
            airBlendTree.AddChild(jumpRisingClip, 0.1f);

            AssetDatabase.AddObjectToAsset(airBlendTree, controller);
            airState.motion = airBlendTree;

            // === 3. Attack (攻撃) ステートの作成 ===
            AnimatorState attackState = rootStateMachine.AddState("Attack");
            if (attackClip != null)
            {
                attackState.motion = attackClip;
            }

            // === 4. Swim (泳ぎ) ステートの作成 ===
            AnimatorState swimState = rootStateMachine.AddState("Swim");
            if (swimClip != null)
            {
                swimState.motion = swimClip;
            }

            // デフォルトステートをLocomotionに設定
            rootStateMachine.defaultState = locomotionState;

            // === トランジション（遷移）の作成 ===

            // 1. Locomotion -> Air (ジャンプ入力時)
            var jumpTransition = locomotionState.AddTransition(airState);
            jumpTransition.AddCondition(AnimatorConditionMode.If, 0, "Jump");
            jumpTransition.hasExitTime = false;
            jumpTransition.duration = 0.1f;

            // 2. Locomotion -> Air (崖から落ちた時など)
            var fallTransition = locomotionState.AddTransition(airState);
            fallTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
            fallTransition.hasExitTime = false;
            fallTransition.duration = 0.1f;

            // 3. Air -> Locomotion (着地した時)
            var landTransition = airState.AddTransition(locomotionState);
            landTransition.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
            landTransition.hasExitTime = false;
            landTransition.duration = 0.1f;

            // 4. Any -> Attack (攻撃入力時)
            var attackTransition = rootStateMachine.AddAnyStateTransition(attackState);
            attackTransition.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackTransition.hasExitTime = false;
            attackTransition.duration = 0.1f;
            attackTransition.canTransitionToSelf = false; // 同じ状態への連続遷移を防ぐ

            // 5. Attack -> Locomotion (攻撃終了後)
            var finishAttackTransition = attackState.AddTransition(locomotionState);
            finishAttackTransition.hasExitTime = true; // クリップ終了まで待機
            finishAttackTransition.exitTime = 1f;  // アニメーションが100%終わった時点
            finishAttackTransition.duration = 0.2f; // 0.2秒かけてLocomotionにスッと戻る

            // 6. Any -> Swim (水に入った時)
            var swimTransition = rootStateMachine.AddAnyStateTransition(swimState);
            swimTransition.AddCondition(AnimatorConditionMode.If, 0, "IsSwimming");
            swimTransition.hasExitTime = false;
            swimTransition.duration = 0.2f;
            swimTransition.canTransitionToSelf = false;

            // 7. Swim -> Locomotion (水から出た時)
            var exitSwimTransition = swimState.AddTransition(locomotionState);
            exitSwimTransition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsSwimming");
            exitSwimTransition.hasExitTime = false;
            exitSwimTransition.duration = 0.2f;

            Debug.Log($"<color=green>[PlayerAnimatorSetupWizard] Animator Controllerが正常に生成されました: {path}</color>");
            
            // 作成したアセットを選択してハイライトする
            Selection.activeObject = controller;
            EditorGUIUtility.PingObject(controller);
        }
    }
}
