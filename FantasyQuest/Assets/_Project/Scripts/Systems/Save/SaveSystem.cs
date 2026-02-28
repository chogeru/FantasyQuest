using System;
using System.IO;
using UnityEngine;
using Project.Core.Stats;
using Project.Core.Inventory;
using System.Collections.Generic;

namespace Project.Systems.Save
{
    /// <summary>
    /// セーブデータとしてJSONに書き出すためのデータ構造
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public float PlayerPositionX;
        public float PlayerPositionY;
        public float PlayerPositionZ;
        
        public float PlayerHealth;

        // インベントリの保存用（ScriptableObjectであるItemDataはID文字列として保存する）
        [Serializable]
        public class SavedItemData
        {
            public string ItemID;
            public int Amount;
        }
        public List<SavedItemData> InventoryItems = new List<SavedItemData>();
        
        // 他にも「発見した焚き火のリスト」「クリアしたクエスト」等を追加可能
    }

    /// <summary>
    /// プレイヤーの状態（位置、HP、インベントリ）をローカルストレージにJSONとして保存・読み込みを行うシステム。
    /// （※実践では暗号化等も考慮しますが、ここでは基盤となるJSONセーブを構築します）
    /// </summary>
    public class SaveSystem : MonoBehaviour
    {
        private string SaveFilePath => Path.Combine(Application.persistentDataPath, "savedata.json");

        [Header("References to Save")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private CharacterStats _playerStats;
        [SerializeField] private InventoryManager _playerInventory;
        
        [Header("Item Database")]
        [Tooltip("ロード時にItemIDからItemDataを復元するための全アイテムリスト")]
        [SerializeField] private List<ItemData> _itemDatabase;

        private void Update()
        {
            // 仮入力：テスト用に F5 でセーブ、F9 でロード
            if (UnityEngine.Input.GetKeyDown(KeyCode.F5)) SaveGame();
            if (UnityEngine.Input.GetKeyDown(KeyCode.F9)) LoadGame();
        }

        public void SaveGame()
        {
            GameSaveData data = new GameSaveData();

            // 1. 位置の保存
            if (_playerTransform != null)
            {
                data.PlayerPositionX = _playerTransform.position.x;
                data.PlayerPositionY = _playerTransform.position.y;
                data.PlayerPositionZ = _playerTransform.position.z;
            }

            // 2. HP（ステータス）の保存
            // ※CharacterStatsから現在HPを取れるように、本来はCurrentHealthのGetterを追加します
            // 仮置きとして100固定にせず、本来の値を保存する想定（後でStatsの拡充が必要）
            // data.PlayerHealth = _playerStats.CurrentHealth; 
            data.PlayerHealth = 100f; // ★仮置き

            // 3. インベントリの保存
            if (_playerInventory != null)
            {
                foreach (var slot in _playerInventory.GetSlots())
                {
                    data.InventoryItems.Add(new GameSaveData.SavedItemData 
                    { 
                        ItemID = slot.Item.ItemID, 
                        Amount = slot.Amount 
                    });
                }
            }

            // JSONに変換して書き込み
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);

            Debug.Log($"<color=cyan>[SaveSystem]</color> Game Saved! Path: {SaveFilePath}\n{json}");
        }

        public void LoadGame()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning("[SaveSystem] サブデータファイルが見つかりません。");
                return;
            }

            string json = File.ReadAllText(SaveFilePath);
            GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

            // 1. 位置の復元
            if (_playerTransform != null)
            {
                // CharacterControllerが着いている場合、直接positionを弄るとバグる事があるため一時無効化する
                var cc = _playerTransform.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                
                _playerTransform.position = new Vector3(data.PlayerPositionX, data.PlayerPositionY, data.PlayerPositionZ);
                
                if (cc != null) cc.enabled = true;
            }

            // 2. HPの復元
            // if (_playerStats != null) _playerStats.SetHealth(data.PlayerHealth);

            // 3. インベントリの復元（IDから実態を取得する）
            if (_playerInventory != null && _itemDatabase != null)
            {
                _playerInventory.GetSlots().Clear(); // 一旦空にする
                
                foreach (var savedItem in data.InventoryItems)
                {
                    var itemData = _itemDatabase.Find(i => i.ItemID == savedItem.ItemID);
                    if (itemData != null)
                    {
                        _playerInventory.AddItem(itemData, savedItem.Amount);
                    }
                }
            }

            Debug.Log($"<color=cyan>[SaveSystem]</color> Game Loaded!");
        }
    }
}
