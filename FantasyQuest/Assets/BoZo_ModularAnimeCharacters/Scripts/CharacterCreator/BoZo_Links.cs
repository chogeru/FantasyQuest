using UnityEngine;


namespace Bozo.AnimeCharacters
{

public class BoZo_Links : MonoBehaviour
{

        private void Awake()
        {
    #if !UNITY_EDITOR
            Destroy(this.gameObject);
    #endif
        }
        public void Documentation()
        {
            Application.OpenURL("https://docs.google.com/document/d/1kf1KGT-OqV3Ecvnej0mpmgIOkc-_e7UJp-BkGuZfuN4/edit?usp=sharing");
        }

        public void Discord()
        {
            Application.OpenURL("https://discord.gg/UCbRjUy7m7");
        }

        public void Twitter()
        {

        }

        public void Youtube()
        {

        }
    }

}