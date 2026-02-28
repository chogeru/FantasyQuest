using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEditor.AI;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using Project.Core.Player;
using Project.Core.AI;
using Project.Core.Stats;
using Project.Systems.Input;
using Project.Systems.Combat;
using Project.Core.CameraSystem;
using Project.UI;

namespace Project.Editor
{
    public class GameSetupTool : EditorWindow
    {
        [MenuItem("Project/Setup Action Game Scene")]
        public static void SetupScene()
        {
            Debug.Log("Starting Scene Setup...");

            // 1. Create Input Actions
            var inputAsset = CreateInputActions();
            var inputReader = CreateInputReader(inputAsset);

            // 2. Create Character Data Assets
            var playerData = CreateCharacterData("PlayerData", 100f, 50f, 15f, 5f);
            var enemyData = CreateCharacterData("EnemyData", 50f, 30f, 5f, 0f);

            // 3. Create Managers (Hitstop, Debug Console)
            CreateManagers();

            // 4. Create Environment & NavMesh
            CreateEnvironment();

            // 5. Create Player
            var player = CreatePlayer(inputReader, playerData);

            // 6. Create Enemy
            CreateEnemy(enemyData);

            // 7. Create Camera
            CreateCamera(player.transform, inputReader);

            // 8. Create UI
            CreateUI(player.GetComponent<CharacterStats>());

            Debug.Log("<color=green>Scene Setup Complete!</color>");
        }

        private static InputActionAsset CreateInputActions()
        {
            string dir = "Assets/_Project/Input";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = dir + "/PlayerInputActions.inputactions";
            if (!File.Exists(path))
            {
                string json = @"{
                    ""name"": ""PlayerInputActions"",
                    ""maps"": [
                        {
                            ""name"": ""Player"",
                            ""id"": ""e78923a1-1234-4567-89ab-cdef01234567"",
                            ""actions"": [
                                { ""name"": ""Move"", ""type"": ""Value"", ""id"": ""a1"", ""expectedControlType"": ""Vector2"" },
                                { ""name"": ""Look"", ""type"": ""Value"", ""id"": ""a2"", ""expectedControlType"": ""Vector2"" },
                                { ""name"": ""Jump"", ""type"": ""Button"", ""id"": ""a3"", ""expectedControlType"": ""Button"" },
                                { ""name"": ""Attack"", ""type"": ""Button"", ""id"": ""a4"", ""expectedControlType"": ""Button"" },
                                { ""name"": ""Sprint"", ""type"": ""Button"", ""id"": ""a5"", ""expectedControlType"": ""Button"" }
                            ],
                            ""bindings"": [
                                { ""name"": ""WASD"", ""id"": ""b1"", ""path"": ""2DVector"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": true, ""isPartOfComposite"": false },
                                { ""name"": ""up"", ""id"": ""b2"", ""path"": ""<Keyboard>/w"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                                { ""name"": ""down"", ""id"": ""b3"", ""path"": ""<Keyboard>/s"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                                { ""name"": ""left"", ""id"": ""b4"", ""path"": ""<Keyboard>/a"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                                { ""name"": ""right"", ""id"": ""b5"", ""path"": ""<Keyboard>/d"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Move"", ""isComposite"": false, ""isPartOfComposite"": true },
                                { ""name"": """", ""id"": ""b6"", ""path"": ""<Pointer>/delta"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Look"", ""isComposite"": false, ""isPartOfComposite"": false },
                                { ""name"": """", ""id"": ""b7"", ""path"": ""<Keyboard>/space"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Jump"", ""isComposite"": false, ""isPartOfComposite"": false },
                                { ""name"": """", ""id"": ""b8"", ""path"": ""<Mouse>/leftButton"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Attack"", ""isComposite"": false, ""isPartOfComposite"": false },
                                { ""name"": """", ""id"": ""b9"", ""path"": ""<Keyboard>/leftShift"", ""interactions"": """", ""processors"": """", ""groups"": """", ""action"": ""Sprint"", ""isComposite"": false, ""isPartOfComposite"": false }
                            ]
                        }
                    ],
                    ""controlSchemes"": []
                }";
                File.WriteAllText(path, json);
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
        }

        private static InputReader CreateInputReader(InputActionAsset asset)
        {
            string dir = "Assets/_Project/Input";
            string path = dir + "/InputReader.asset";
            
            InputReader reader = AssetDatabase.LoadAssetAtPath<InputReader>(path);
            if (reader == null)
            {
                reader = ScriptableObject.CreateInstance<InputReader>();
                AssetDatabase.CreateAsset(reader, path);
            }
            
            SerializedObject serializedReader = new SerializedObject(reader);
            var prop = serializedReader.FindProperty("_inputActionAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = asset;
                serializedReader.ApplyModifiedProperties();
            }
            EditorUtility.SetDirty(reader);
            AssetDatabase.SaveAssets();

            return reader;
        }

        private static CharacterData CreateCharacterData(string fileName, float hp, float sp, float atk, float armor)
        {
            string dir = "Assets/_Project/Data";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = dir + $"/{fileName}.asset";
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CharacterData>();
                data.MaxHealth = hp;
                data.MaxStamina = sp;
                data.BaseAttackPower = atk;
                data.Armor = armor;
                
                AssetDatabase.CreateAsset(data, path);
                AssetDatabase.SaveAssets();
            }
            return data;
        }

        private static void CreateManagers()
        {
            if (GameObject.Find("GameManagers") != null) return;

            GameObject managersObj = new GameObject("GameManagers");
            
            // Hitstop Manager
            managersObj.AddComponent<HitstopManager>();

            // Debug Console
            managersObj.AddComponent<DebugConsole>();
        }

        private static void CreateEnvironment()
        {
            if (GameObject.Find("Environment") != null) return;

            GameObject env = new GameObject("Environment");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(env.transform);
            ground.transform.localScale = new Vector3(5, 1, 5);
            ground.GetComponent<Renderer>().sharedMaterial.color = Color.gray;

            // Obstacles
            GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube1.transform.position = new Vector3(3, 0.5f, 3);
            cube1.transform.SetParent(env.transform);

            GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube2.transform.position = new Vector3(-3, 0.5f, 5);
            cube2.transform.SetParent(env.transform);

            // Set static for NavMesh
            GameObjectUtility.SetStaticEditorFlags(ground, StaticEditorFlags.NavigationStatic);
            GameObjectUtility.SetStaticEditorFlags(cube1, StaticEditorFlags.NavigationStatic);
            GameObjectUtility.SetStaticEditorFlags(cube2, StaticEditorFlags.NavigationStatic);

            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }

        private static GameObject CreatePlayer(InputReader inputReader, CharacterData dataAsset)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                player.name = "Player";
                player.tag = "Player";
                player.transform.position = new Vector3(0, 1f, 0);
                player.GetComponent<Renderer>().sharedMaterial.color = Color.blue;

                // Add Components
                player.AddComponent<PlayerStateMachine>();
                var stats = player.AddComponent<CharacterStats>();
                player.AddComponent<Animator>(); // requires Animator for state machine

                var controller = player.AddComponent<PlayerController>();
                
                // Assign dependencies via SerializedObject
                SerializedObject serializedController = new SerializedObject(controller);
                serializedController.FindProperty("_inputReader").objectReferenceValue = inputReader;
                
                // Set LayerMask for Ground in PlayerController
                serializedController.FindProperty("_groundLayer").intValue = 1; // Default layer
                
                // Provide dummy GroundCheck transform
                GameObject groundCheck = new GameObject("GroundCheck");
                groundCheck.transform.SetParent(player.transform);
                groundCheck.transform.localPosition = new Vector3(0, -1f, 0);
                serializedController.FindProperty("_groundCheck").objectReferenceValue = groundCheck;

                // Assign CharacterData
                var serializedStats = new SerializedObject(stats);
                serializedStats.FindProperty("_characterData").objectReferenceValue = dataAsset;
                serializedStats.ApplyModifiedProperties();

                serializedController.ApplyModifiedProperties();
            }
            return player;
        }

        private static void CreateEnemy(CharacterData dataAsset)
        {
            if (GameObject.Find("Enemy") != null) return;

            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = "Enemy";
            enemy.transform.position = new Vector3(0, 1f, 8);
            enemy.GetComponent<Renderer>().sharedMaterial.color = Color.red;

            // Tag/Layer for Enemy
            enemy.layer = LayerMask.NameToLayer("Default");

            enemy.AddComponent<EnemyStateMachine>();
            var stats = enemy.AddComponent<CharacterStats>();
            enemy.AddComponent<Animator>();
            enemy.AddComponent<NavMeshAgent>();

            var controller = enemy.AddComponent<EnemyAIController>();

            var serializedStats = new SerializedObject(stats);
            serializedStats.FindProperty("_characterData").objectReferenceValue = dataAsset;
            serializedStats.ApplyModifiedProperties();
        }

        private static void CreateCamera(Transform playerTransform, InputReader inputReader)
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            var camManager = mainCam.gameObject.GetComponent<CameraManager>();
            if (camManager == null) camManager = mainCam.gameObject.AddComponent<CameraManager>();

            var targetLock = mainCam.gameObject.GetComponent<TargetLockOn>();
            if (targetLock == null) targetLock = mainCam.gameObject.AddComponent<TargetLockOn>();

            SerializedObject serializedCam = new SerializedObject(camManager);
            serializedCam.FindProperty("_inputReader").objectReferenceValue = inputReader;
            serializedCam.FindProperty("_target").objectReferenceValue = playerTransform;
            serializedCam.FindProperty("_camTransform").objectReferenceValue = mainCam.transform;
            serializedCam.FindProperty("_collisionLayer").intValue = 1; // Default layer
            serializedCam.ApplyModifiedProperties();

            SerializedObject serializedLock = new SerializedObject(targetLock);
            serializedLock.FindProperty("_inputReader").objectReferenceValue = inputReader;
            serializedLock.FindProperty("_cameraManager").objectReferenceValue = camManager;
            serializedLock.FindProperty("_enemyLayer").intValue = ~0; // Everything layer for testing lock on
            serializedLock.ApplyModifiedProperties();
        }

        private static void CreateUI(CharacterStats playerStats)
        {
            if (GameObject.Find("UI_Canvas") != null) return;

            GameObject canvasObj = new GameObject("UI_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // HUD Manager
            var hud = canvasObj.AddComponent<HUDManager>();
            SerializedObject serializedHud = new SerializedObject(hud);
            serializedHud.FindProperty("_targetStats").objectReferenceValue = playerStats;

            // Health Bar
            GameObject healthBarObj = new GameObject("HealthBar");
            healthBarObj.transform.SetParent(canvasObj.transform, false);
            var healthImage = healthBarObj.AddComponent<Image>();
            healthImage.color = Color.green;
            
            // To support Fill amount:
            // Unassign sprite, or load a default one. Without sprite, Image component can still use Fill method if it has one?
            // Wait, Unity's Image component requires a Sprite to use FillMethod. 
            // We can create a default texture sprite.
            var texture = new Texture2D(1,1);
            texture.SetPixel(0,0,Color.white);
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0,0,1,1), new Vector2(0.5f, 0.5f));
            healthImage.sprite = sprite;
            
            healthImage.type = Image.Type.Filled;
            healthImage.fillMethod = Image.FillMethod.Horizontal;
            
            RectTransform hpRect = healthBarObj.GetComponent<RectTransform>();
            hpRect.anchorMin = new Vector2(0, 1);
            hpRect.anchorMax = new Vector2(0, 1);
            hpRect.pivot = new Vector2(0, 1);
            hpRect.anchoredPosition = new Vector2(20, -20);
            hpRect.sizeDelta = new Vector2(200, 20);

            serializedHud.FindProperty("_healthFillImage").objectReferenceValue = healthImage;

            // Stamina Bar
            GameObject staminaBarObj = new GameObject("StaminaBar");
            staminaBarObj.transform.SetParent(canvasObj.transform, false);
            var staminaImage = staminaBarObj.AddComponent<Image>();
            staminaImage.sprite = sprite;
            staminaImage.color = Color.yellow;
            staminaImage.type = Image.Type.Filled;
            staminaImage.fillMethod = Image.FillMethod.Horizontal;

            RectTransform stRect = staminaBarObj.GetComponent<RectTransform>();
            stRect.anchorMin = new Vector2(0, 1);
            stRect.anchorMax = new Vector2(0, 1);
            stRect.pivot = new Vector2(0, 1);
            stRect.anchoredPosition = new Vector2(20, -50);
            stRect.sizeDelta = new Vector2(150, 15);

            serializedHud.FindProperty("_staminaFillImage").objectReferenceValue = staminaImage;

            serializedHud.ApplyModifiedProperties();

            // Event System
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // Damage Popup Prefab Generator Helper
            GameObject popupPrefab = CreateDamagePopupPrefab(sprite);
            serializedHud.FindProperty("_damagePopupPrefab").objectReferenceValue = popupPrefab;
            serializedHud.ApplyModifiedProperties();
        }

        private static GameObject CreateDamagePopupPrefab(Sprite defaultSprite)
        {
            string dir = "Assets/_Project/Prefabs";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = dir + "/DamagePopup.prefab";
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existingPrefab != null) return existingPrefab;

            // プレハブの元となるGameObjectを直接作成
            GameObject popupObj = new GameObject("DamagePopup");
            popupObj.AddComponent<RectTransform>(); // Canvasのワールドスペース配置用
            
            // Textコンポーネント用の子オブジェクト
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(popupObj.transform);
            Text textComp = textObj.AddComponent<Text>();
            textComp.text = "10";
            textComp.color = Color.white;
            textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.fontSize = 24;
            
            // Outline effect for better visibility
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;

            popupObj.AddComponent<DamagePopup>();

            // Prefab化
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(popupObj, path);
            DestroyImmediate(popupObj);
            
            return prefab;
        }
    }
}
