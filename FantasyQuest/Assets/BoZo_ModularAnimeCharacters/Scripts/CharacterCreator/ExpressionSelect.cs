using UnityEngine;

namespace Bozo.AnimeCharacters
{
    public class ExpressionSelect : MonoBehaviour
    {
        public OutfitSystem outfitSystem;
        public Animator animator;

        public string parameterID;

        private void OnEnable()
        {
            outfitSystem.OnOutfitChanged += GetHead;
        }

        private void GetHead(Outfit outfit)
        {
            var head = outfitSystem.GetOutfit("Head");
            if (head) animator = head.GetComponentInChildren<Animator>();
            else animator = outfitSystem.animator;
        }

        private void OnDisable()
        {
            outfitSystem.OnOutfitChanged -= GetHead;
        }

        public void SetExpression(float value)
        {
            if(animator == null)
            {
                var head = outfitSystem.GetOutfit("Head");
                if (head) animator = head.GetComponentInChildren<Animator>();
                else animator = outfitSystem.animator;
            }

            animator.SetFloat(parameterID, value);
        }
    }
}