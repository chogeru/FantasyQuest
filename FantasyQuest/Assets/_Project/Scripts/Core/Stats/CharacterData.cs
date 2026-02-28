using UnityEngine;

namespace Project.Core.Stats
{
    /// <summary>
    /// キャラクターの基礎ステータスを保持するデータアセット（ScriptableObject）。
    /// これにより、毎回シーンのPrefabに数値を手打ちする手間を省き、
    /// 「ゴブリン用」「重戦士用」といったデータをAssetとして一元管理できます。
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterData", menuName = "Project/Stats/Character Data")]
    public class CharacterData : ScriptableObject
    {
        [Header("Base Stats")]
        [Tooltip("最大体力")]
        public float MaxHealth = 100f;
        
        [Tooltip("最大スタミナ")]
        public float MaxStamina = 50f;
        
        [Tooltip("基本攻撃力")]
        public float BaseAttackPower = 10f;
        
        [Tooltip("防御力（ダメージ減算値）")]
        public float Armor = 5f;

        [Header("Stamina System")]
        [Tooltip("1秒間に自動回復するスタミナ量")]
        public float StaminaRegenRate = 10f;
        
        [Tooltip("スタミナ消費後、自動回復が始まるまでのディレイ（秒）")]
        public float StaminaRegenDelay = 2f;
    }
}
