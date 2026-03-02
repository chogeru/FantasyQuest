using UnityEngine;

namespace Salvage.ClothingCuller.Components
{
    public class ModularClothingSpawner : MonoBehaviour
    {
        private Occludee[] spawnedOccludees;

        #region SerializeFields

        [SerializeField] private ClothingCuller clothingCuller;
        [SerializeField] private Occludee body;
        [SerializeField] private Occludee[] clothingPrefabs;

        #endregion

        #region MonoBehaviour Methods

        private void Awake()
        {
            spawnedOccludees = new Occludee[clothingPrefabs.Length];
        }

        private void Start()
        {
            clothingCuller.Register(body, false);
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();

            for (int i = 0; i < clothingPrefabs.Length; i++)
            {
                drawEquipOrUnEquipButton(i);
            }

            GUILayout.EndHorizontal();
        }

        #endregion

        #region Private Methods

        private void drawEquipOrUnEquipButton(int index)
        {
            float scale = Camera.main.pixelHeight / 1080f;
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = (int)(30 * scale),
                fixedHeight = 100f * scale,
                fixedWidth = 300f * scale
            };

            Occludee occludee = spawnedOccludees[index];
            Occludee prefab = clothingPrefabs[index];

            if (occludee != null)
            {
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button($"Unequip {occludee.name}", style))
                {
                    unequip(occludee);
                    spawnedOccludees[index] = null;
                }
                return;
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"Equip {prefab.name}", style))
            {
                spawnedOccludees[index] = equip(prefab);
            }
        }

        private Occludee equip(Occludee occludeePrefab)
        {
            Occludee occludee = Instantiate(occludeePrefab, clothingCuller.transform);
            occludee.name = occludeePrefab.name;

            clothingCuller.Register(occludee);

            return occludee;
        }

        private void unequip(Occludee occludee)
        {
            clothingCuller.Deregister(occludee);

            Destroy(occludee.gameObject);
        }

        #endregion

        #region Public Methods



        #endregion
    }
}