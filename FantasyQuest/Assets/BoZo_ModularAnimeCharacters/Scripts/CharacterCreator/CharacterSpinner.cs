using UnityEngine;
using UnityEngine.EventSystems;


namespace Bozo.AnimeCharacters
{
    public class CharacterSpinner : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {


        public float spinDir;
        public Transform character;
        private Animator anim;
        float dizzyTimer = 1;

        bool spinning;

        public void SetCharacter(Transform character)
        {
            this.character = character;
            anim = character.GetComponentInChildren<Animator>();
        }

        public void OnDrag(PointerEventData eventData)
        {
            spinDir = -eventData.delta.x * 0.1f;
            character.Rotate(0, spinDir, 0);
            dizzyTimer = 0.5f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            spinning = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            spinning = false;
        }

        private void Start()
        {
            SetCharacter(character);
        }

        private void Update()
        {
            if (dizzyTimer <= 0)
            {
                if (spinDir >= 5 || spinDir <= -5)
                {
                    anim.SetBool("Dizzy", true);
                }
                else
                {
                    anim.SetBool("Dizzy", false);
                }
            }

            if (!spinning) 
            {
                character.Rotate(0, spinDir, 0);
                spinDir = Mathf.Lerp(spinDir, 0, Time.deltaTime);
                dizzyTimer -= Time.deltaTime;
            }



        }


    }
}
