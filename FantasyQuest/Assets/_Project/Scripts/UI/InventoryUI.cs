using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Project.Core.Inventory;
using Project.Core.Stats;

namespace Project.UI
{
    /// <summary>
    /// プレイヤーのインベントリ（所持アイテム一覧）を画面に表示し、ボタン入力などを制御するUIマネージャー
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private CharacterStats _playerStats;
        [SerializeField] private GameObject _inventoryPanel;    // インベントリUIの親パネル（On/Off切り替え用）
        [SerializeField] private Transform _slotContainer;      // アイコンを並べるコンテナ
        
        // ※実際は専用のSlotPrefab（Icon Image, Count Text, Button等を内包）を使う想定
        [SerializeField] private GameObject _slotPrefab; 

        private bool _isInventoryOpen = false;

        private void Start()
        {
            if (_inventoryPanel != null) _inventoryPanel.SetActive(false);

            if (_inventoryManager != null)
            {
                // インベントリの中身が変化した際、自動的にUIを再描画する
                _inventoryManager.OnInventoryUpdated += RefreshUI;
            }
        }

        private void OnDestroy()
        {
            if (_inventoryManager != null)
            {
                _inventoryManager.OnInventoryUpdated -= RefreshUI;
            }
        }

        private void Update()
        {
            // 仮入力として I キーでインベントリを開閉する
            if (Input.GetKeyDown(KeyCode.I))
            {
                ToggleInventory();
            }
        }

        public void ToggleInventory()
        {
            _isInventoryOpen = !_isInventoryOpen;
            if (_inventoryPanel != null) _inventoryPanel.SetActive(_isInventoryOpen);

            if (_isInventoryOpen)
            {
                // インベントリを開いた時はカーソルを表示して操作可能にする（必要に応じて）
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                RefreshUI(_inventoryManager.GetSlots());
            }
            else
            {
                // 閉じた場合はアクションに戻る
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>
        /// 最新のアイテムリストに合わせてUIのアイコンを作り直す
        /// </summary>
        private void RefreshUI(List<InventoryManager.InventorySlot> slots)
        {
            if (_slotContainer == null || _slotPrefab == null) return;

            // まず古いUI枠を全削除
            foreach (Transform child in _slotContainer)
            {
                Destroy(child.gameObject);
            }

            // アイテム数だけ新しい枠を生成
            for (int i = 0; i < slots.Count; i++)
            {
                var slotData = slots[i];
                GameObject newSlotObj = Instantiate(_slotPrefab, _slotContainer);

                // --- 本来はここでPrefab内部のButtonやImageにアクセスして情報をセットする ---
                // 例：
                // Text countText = newSlotObj.GetComponentInChildren<Text>();
                // if (countText != null) countText.text = slotData.Amount.ToString();
                // 
                // Button btn = newSlotObj.GetComponent<Button>();
                // btn.onClick.AddListener(() => OnSlotClicked(slotData.Item));
                
                // ※ ここでは基盤実装のため省略
            }
        }

        /// <summary>
        /// （UIから呼び出される想定の関数）アイテムアイコンがクリックされた
        /// </summary>
        public void OnSlotClicked(ItemData clickedItem)
        {
            if (_inventoryManager != null && _playerStats != null)
            {
                _inventoryManager.UseItem(clickedItem, _playerStats);
            }
        }
    }
}
