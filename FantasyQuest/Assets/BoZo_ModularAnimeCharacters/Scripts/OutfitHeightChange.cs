using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Bozo.AnimeCharacters
{
    public class OutfitHeightChange : MonoBehaviour
    {
        [SerializeField] float HeightOffset;
        private void OnEnable()
        {
            var System = GetComponentInParent<OutfitSystem>();
            if (System == null) return;

            System.SetHeight(HeightOffset);
        }

        private void OnDisable()
        {
            var System = GetComponentInParent<OutfitSystem>();
            if (System == null) return;

            System.SetHeight(0);
        }
    }
}
