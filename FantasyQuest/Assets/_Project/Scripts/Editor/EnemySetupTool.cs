using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;
using System.IO;
using Project.Core.AI;
using Project.Core.Stats;
using Project.Systems.Combat;
using UnityEngine.AI;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor.Events;
#endif

namespace Project.Editor
{
    /// <summary>
    /// 敵キャラのセットアップを超簡単に行うための統合エディタツール。
    /// 水陸空の移動対応、およびアニメーションオーバーライドの基盤設定まで一括で行います。
    /// </summary>
    public class EnemySetupTool : EditorWindow
    {
        // === 1. 基本設定 ===
        private string _enemyName = "New Enemy";
        private EnemyMovementType _movementType = EnemyMovementType.Land;
        private GameObject _modelPrefab;

        // === 2. アニメーション設定 ===
        private AnimatorController _baseController;
        private AnimationClip _idleClip;
        private AnimationClip _moveClip;
        private AnimationClip _attackClip;
        private AnimationClip _staggerClip;
        private AnimationClip _dieClip;

        // スクロール用
        private Vector2 _scrollPos;

        [MenuItem("FantasyQuest/Tools/Enemy Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<EnemySetupTool>("Enemy Setup");
            window.minSize = new Vector2(400, 650);
            window.Show();
        }

        private void OnGUI()
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                margin = new RectOffset(0, 0, 10, 5)
            };

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // ==========================================
            // タイトル
            // ==========================================
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("⚔️ Enemy Setup Wizard ⚔️", new GUIStyle(EditorStyles.largeLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 18
            });
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox("水・陸・空の敵キャラのコンポーネントとアニメーション（オーバーライド）を一括でセットアップします。", MessageType.Info);

            // ==========================================
            // 1.基本設定
            // ==========================================
            EditorGUILayout.LabelField("1. Basic Settings", headerStyle);
            EditorGUILayout.BeginVertical("box");
            _enemyName = EditorGUILayout.TextField("Enemy Name", _enemyName);
            _movementType = (EnemyMovementType)EditorGUILayout.EnumPopup("Movement Type", _movementType);
            _modelPrefab = (GameObject)EditorGUILayout.ObjectField("Model Prefab (任意)", _modelPrefab, typeof(GameObject), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // ==========================================
            // 2.アニメーション基盤設定
            // ==========================================
            EditorGUILayout.LabelField("2. Animation Foundation (Base & Override)", headerStyle);
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.HelpBox("全ての敵で共通利用するBaseControllerを設定し、クリップを割り当てることで自動的にOverrideControllerを生成します。", MessageType.None);
            
            EditorGUILayout.BeginHorizontal();
            _baseController = (AnimatorController)EditorGUILayout.ObjectField("Base Controller", _baseController, typeof(AnimatorController), false);
            if (GUILayout.Button("自動生成", GUILayout.Width(80)))
            {
                GenerateBaseAnimatorController();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(_baseController == null);
            _idleClip = (AnimationClip)EditorGUILayout.ObjectField("Idle Clip", _idleClip, typeof(AnimationClip), false);
            _moveClip = (AnimationClip)EditorGUILayout.ObjectField("Move (Walk/Swim/Fly) Clip", _moveClip, typeof(AnimationClip), false);
            _attackClip = (AnimationClip)EditorGUILayout.ObjectField("Attack Clip", _attackClip, typeof(AnimationClip), false);
            _staggerClip = (AnimationClip)EditorGUILayout.ObjectField("Stagger (Hurt) Clip", _staggerClip, typeof(AnimationClip), false);
            _dieClip = (AnimationClip)EditorGUILayout.ObjectField("Die Clip", _dieClip, typeof(AnimationClip), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(25);

            // ==========================================
            // 実行ボタン
            // ==========================================
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("🚀 Create & Setup Enemy 🚀", GUILayout.Height(40)))
            {
                if (ValidateSetup())
                {
                    ExecuteSetup();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndScrollView();
        }

        private bool ValidateSetup()
        {
            if (string.IsNullOrEmpty(_enemyName))
            {
                EditorUtility.DisplayDialog("Error", "Enemy Nameを入力してください。", "OK");
                return false;
            }
            if (_baseController == null)
            {
                EditorUtility.DisplayDialog("Error", "Base Animator Controllerが設定されていません。「自動生成」ボタンから作成できます。", "OK");
                return false;
            }
            return true;
        }

        private void ExecuteSetup()
        {
            // 1. Animator Override Controller の作成
            string dirPath = "Assets/_Project/Animations/Enemies/" + _enemyName;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            string overridePath = $"{dirPath}/{_enemyName}_OverrideController.overrideController";
            AnimatorOverrideController overrideController = new AnimatorOverrideController(_baseController);
            
            // クリップの割り当て（プレースホルダー名と一致させる想定）
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(overrideController.overridesCount);
            overrideController.GetOverrides(overrides);

            for (int i = 0; i < overrides.Count; ++i)
            {
                string originalName = overrides[i].Key.name;
                if (originalName.Contains("Idle") && _idleClip != null) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, _idleClip);
                else if (originalName.Contains("Move") && _moveClip != null) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, _moveClip);
                else if (originalName.Contains("Attack") && _attackClip != null) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, _attackClip);
                else if (originalName.Contains("Stagger") && _staggerClip != null) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, _staggerClip);
                else if (originalName.Contains("Die") && _dieClip != null) overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, _dieClip);
            }
            
            overrideController.ApplyOverrides(overrides);
            AssetDatabase.CreateAsset(overrideController, overridePath);
            AssetDatabase.SaveAssets();

            // 2. プレハブ/ゲームオブジェクトの生成
            GameObject enemyObj = new GameObject(_enemyName);
            
            // モデルの追加
            GameObject modelObj = null;
            if (_modelPrefab != null)
            {
                modelObj = (GameObject)PrefabUtility.InstantiatePrefab(_modelPrefab, enemyObj.transform);
                modelObj.transform.localPosition = Vector3.zero;
            }

            // 3. コンポーネントのアタッチと設定
            Animator animator = enemyObj.AddComponent<Animator>();
            animator.runtimeAnimatorController = overrideController;
            if (modelObj != null && modelObj.GetComponent<Animator>() != null)
            {
                // 既存のモデルにAnimatorがある場合は削除してルートで管理するか、アバターを引き継ぐ
                animator.avatar = modelObj.GetComponent<Animator>().avatar;
                DestroyImmediate(modelObj.GetComponent<Animator>());
            }

            CharacterController cc = enemyObj.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1f, 0); // 仮設定
            
            NavMeshAgent agent = enemyObj.AddComponent<NavMeshAgent>();
            agent.enabled = (_movementType == EnemyMovementType.Land); // 陸上のみ有効化

            CharacterStats stats = enemyObj.AddComponent<CharacterStats>();
            enemyObj.AddComponent<EnemyStateMachine>();

            EnemyAIController aiController = enemyObj.AddComponent<EnemyAIController>();
            
            // AIControllerの private な SerializedField をReflectionで設定するか、あるいは新設したパブリックなセット用メソッドを使う
            SerializedObject so = new SerializedObject(aiController);
            so.FindProperty("_animator").objectReferenceValue = animator;
            so.FindProperty("_movementType").enumValueIndex = (int)_movementType;
            so.ApplyModifiedProperties();

            // === さらなる改良: 戦闘モジュールの自動設定 ===
            
            // 1. Hurtbox(被弾判定)の設定
            Hurtbox hurtbox = enemyObj.AddComponent<Hurtbox>();
            CapsuleCollider hurtCollider = enemyObj.GetComponent<CapsuleCollider>();
            if (hurtCollider == null) hurtCollider = enemyObj.AddComponent<CapsuleCollider>();
            hurtCollider.isTrigger = false; // Hurtbox側ではTriggerにしない場合もあるが、要件次第。デフォルトでCCの当たり判定と併用
            hurtCollider.center = new Vector3(0, 1f, 0);
            hurtCollider.height = 2f;
            
            // Hurtbox の OnTakeDamage イベントを CharacterStats の TakeDamage メソッドにバインド
#if UNITY_EDITOR
            UnityEventTools.AddPersistentListener(hurtbox.OnTakeDamage, new UnityAction<float>(stats.TakeDamage));
#endif

            // 2. Hitbox(攻撃判定)用の子オブジェクト作成
            GameObject hitboxObj = new GameObject("MeleeHitbox");
            hitboxObj.transform.SetParent(enemyObj.transform);
            hitboxObj.transform.localPosition = new Vector3(0, 1f, 1f); // 前方に配置
            Hitbox hitbox = hitboxObj.AddComponent<Hitbox>();
            SphereCollider hitCollider = hitboxObj.AddComponent<SphereCollider>();
            hitCollider.isTrigger = true;
            hitCollider.radius = 0.5f;

            // === プレハブ化 ===
            string prefabDirPath = "Assets/_Project/Prefabs/Enemies";
            if (!Directory.Exists(prefabDirPath))
            {
                Directory.CreateDirectory(prefabDirPath);
            }
            
            string prefabPath = $"{prefabDirPath}/{_enemyName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(enemyObj, prefabPath, InteractionMode.UserAction);
            
            // 完了メッセージ
            Selection.activeGameObject = prefab;
            EditorGUIUtility.PingObject(prefab);
            
            Debug.Log($"<color=cyan>[EnemySetupTool]</color> {_enemyName} ({_movementType}) のフルセットアップとPrefab化が完了しました！ Prefab: {prefabPath}");
            EditorUtility.DisplayDialog("Success", $"{_enemyName} のセットアップが完了し、Prefabとして保存されました。\nシーン上のオブジェクトを選択しています。", "OK");
        }

        // ==========================================
        // ベースコントローラー生成ロジック
        // ==========================================
        private void GenerateBaseAnimatorController()
        {
            string defaultPath = "Assets/_Project/Animations/Enemies/BaseEnemyController.controller";
            
            // ディレクトリ作成
            Directory.CreateDirectory(Path.GetDirectoryName(defaultPath));

            string path = EditorUtility.SaveFilePanelInProject("Save Base Controller", "BaseEnemyController", "controller", "保存先を選んでください", "Assets/_Project/Animations/Enemies");
            if (string.IsNullOrEmpty(path)) return;

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            // パラメータ
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsSwimming", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsFlying", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            // プレースホルダー用クリップの作成
            AnimationClip idleDummy = CreateDummyClip(controller, "Base_Idle");
            AnimationClip moveDummy = CreateDummyClip(controller, "Base_Move");
            AnimationClip attackDummy = CreateDummyClip(controller, "Base_Attack");
            AnimationClip staggerDummy = CreateDummyClip(controller, "Base_Stagger");
            AnimationClip dieDummy = CreateDummyClip(controller, "Base_Die");

            // Locomotion (BlendTree)
            AnimatorState locomotionState = sm.AddState("Locomotion");
            BlendTree blendTree = new BlendTree { name = "LocomotionBlend", blendType = BlendTreeType.Simple1D, blendParameter = "Speed" };
            blendTree.AddChild(idleDummy, 0f);
            blendTree.AddChild(moveDummy, 3f); // Walk or Swim or Fly speed
            
            AssetDatabase.AddObjectToAsset(blendTree, controller);
            locomotionState.motion = blendTree;

            // 各種ステート
            AnimatorState attackState = sm.AddState("Attack");
            attackState.motion = attackDummy;
            AnimatorState staggerState = sm.AddState("Stagger");
            staggerState.motion = staggerDummy;
            AnimatorState dieState = sm.AddState("Die");
            dieState.motion = dieDummy;

            sm.defaultState = locomotionState;

            // トランジション
            var attackTrans = sm.AddAnyStateTransition(attackState);
            attackTrans.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            attackTrans.duration = 0.1f;

            var finishAttackTrans = attackState.AddTransition(locomotionState);
            finishAttackTrans.hasExitTime = true;
            finishAttackTrans.exitTime = 1f;

            var hurtTrans = sm.AddAnyStateTransition(staggerState);
            hurtTrans.AddCondition(AnimatorConditionMode.If, 0, "Hurt");
            hurtTrans.duration = 0.1f;

            var finishHurtTrans = staggerState.AddTransition(locomotionState);
            finishHurtTrans.hasExitTime = true;
            finishHurtTrans.exitTime = 1f;

            var dieTrans = sm.AddAnyStateTransition(dieState);
            dieTrans.AddCondition(AnimatorConditionMode.If, 0, "Die");
            dieTrans.duration = 0.1f;

            _baseController = controller;
            AssetDatabase.SaveAssets();

            Debug.Log($"<color=green>[EnemySetupTool]</color> Base Animator Controller を作成しました: {path}");
        }

        private AnimationClip CreateDummyClip(AnimatorController controller, string clipName)
        {
            AnimationClip clip = new AnimationClip { name = clipName };
            AssetDatabase.AddObjectToAsset(clip, controller);
            return clip;
        }
    }
}
