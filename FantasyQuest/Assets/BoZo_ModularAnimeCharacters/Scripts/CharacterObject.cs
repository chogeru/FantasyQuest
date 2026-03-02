using UnityEngine;

namespace Bozo.AnimeCharacters
{

    [CreateAssetMenu(fileName = "BMAC_CharacterObject", menuName = "BoZo/BMAC_CharacterObject")]
    public class CharacterObject : DataObject
    {
        public Texture2D icon;
        public CharacterData data;

        public override CharacterData GetCharacterData()
        {
            return data;
        }

        public override Texture2D GetCharacterIcon()
        {
            return icon;
        }
    }
}
