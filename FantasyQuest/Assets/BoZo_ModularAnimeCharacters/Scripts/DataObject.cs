using UnityEngine;


namespace Bozo.AnimeCharacters
{
    public abstract class DataObject : ScriptableObject
    {

        public virtual CharacterData GetCharacterData()
        {
            return null;
        }

        public virtual Texture2D GetCharacterIcon()
        {
            return null;
        }
    }
}
