using UnityEngine;

namespace Project.Core.Inventory
{
    public enum ItemType
    {
        Consumable, // 回復薬などの消費アイテム
        Weapon,     // 武器（装備品）
        Material,   // クラフト素材
        KeyItem     // だいじなもの
    }

    /// <summary>
    /// ゲーム内に存在する「アイテムの種類」を定義するScriptableObject。
    /// 各種アイテムのマスターデータとして機能します。
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Project/Inventory/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string ItemID;             // セーブデータなどで識別するための一意のID
        public string ItemName;           // 表示名
        [TextArea(3, 5)]
        public string Description;        // 説明文
        public Sprite Icon;               // UI表示用のアイコン
        public ItemType Type;

        [Header("Parameters (Consumable)")]
        [Tooltip("回復アイテムなどを使用した場合のHP回復量")]
        public float HealAmount;

        [Header("Parameters (Weapon)")]
        [Tooltip("武器として装備した場合の攻撃力ボーナス")]
        public float AttackBonus;

        [Header("Settings")]
        public bool IsStackable = true;   // 複数個を1枠にスタックできるか
        public int MaxStack = 99;         // 最大スタック数
        
        // ---
        // 以下に「アイテムを使った時の実処理（パーティクルを出す等）」を定義する拡張なども可能です
    }
}
