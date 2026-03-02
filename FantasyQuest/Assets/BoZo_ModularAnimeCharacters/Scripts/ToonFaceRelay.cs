using UnityEngine;

namespace Bozo.AnimeCharacters
{
    public class ToonFaceRelay : MonoBehaviour
    {
        private ToonFacialAnimator animator;


        private void Start()
        {
            animator = GetComponentInParent<ToonFacialAnimator>();
        }
        public void SetFace(int preset)
        {
            if (animator == null) return;
            animator.SetFace(preset);
        }
    }
}
