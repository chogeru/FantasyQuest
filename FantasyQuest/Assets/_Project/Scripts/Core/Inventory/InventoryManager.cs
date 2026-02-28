using System;
using System.Collections.Generic;
using UnityEngine;
using Project.Core.Stats;

namespace Project.Core.Inventory
{
    /// <summary>
    /// プレイヤーが所持しているアイテムリスト（インベントリ）を管理するクラス
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        // インベントリの各スロット（枠）を表現するクラス
        [Serializable]
        public class InventorySlot
        {
            public ItemData Item;
            public int Amount;

            public InventorySlot(ItemData item, int amount)
            {
                Item = item;
                Amount = amount;
            }
        }

        [Header("Settings")]
        [SerializeField] private int _maxSlots = 20;

        [Header("State")]
        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        // イベント：UIを更新するために利用
        public event Action<List<InventorySlot>> OnInventoryUpdated;

        /// <summary>
        /// アイテムをインベントリに追加する
        /// </summary>
        public bool AddItem(ItemData itemToAdd, int amount = 1)
        {
            // まず既存のスタック可能なスロットを探す
            if (itemToAdd.IsStackable)
            {
                foreach (var slot in _slots)
                {
                    if (slot.Item == itemToAdd && slot.Amount < itemToAdd.MaxStack)
                    {
                        // 複数枠にまたがる追加の厳密な計算は省略し、ここでは簡易的に枠内に収まるかだけを判定
                        int spaceLeft = itemToAdd.MaxStack - slot.Amount;
                        if (amount <= spaceLeft)
                        {
                            slot.Amount += amount;
                            OnInventoryUpdated?.Invoke(_slots);
                            return true;
                        }
                    }
                }
            }

            // 新しいスロットが必要な場合
            if (_slots.Count < _maxSlots)
            {
                _slots.Add(new InventorySlot(itemToAdd, amount));
                OnInventoryUpdated?.Invoke(_slots);
                return true;
            }

            Debug.LogWarning("インベントリがいっぱいです！");
            return false;
        }

        /// <summary>
        /// アイテムを取り除く
        /// </summary>
        public void RemoveItem(ItemData itemToRemove, int amount = 1)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item == itemToRemove)
                {
                    _slots[i].Amount -= amount;
                    if (_slots[i].Amount <= 0)
                    {
                        _slots.RemoveAt(i);
                    }
                    OnInventoryUpdated?.Invoke(_slots);
                    return;
                }
            }
        }

        /// <summary>
        /// アイテムを使用する（回復など）
        /// </summary>
        public void UseItem(ItemData item, CharacterStats userStats)
        {
            if (item == null || userStats == null) return;

            if (item.Type == ItemType.Consumable)
            {
                // 回復処理
                userStats.Heal(item.HealAmount);
                Debug.Log($"{item.ItemName} を使用し、HPが {item.HealAmount} 回復した！");

                // 消費アイテムなら数を減らす
                RemoveItem(item, 1);
            }
        }

        public List<InventorySlot> GetSlots() => _slots;
    }
}
