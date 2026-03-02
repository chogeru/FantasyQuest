namespace URPStylizedNatureEssentials.Rendering
{
    using UnityEngine;

[ExecuteAlways] // Ensures it runs in Editor and Play mode
public class SunRotationCopy : MonoBehaviour
{
    public Transform sun; // Reference to the sun transform (manual assignment)
    [SerializeField] private bool findSunInScene = false; // Checkbox to auto-find sun

    void Update()
    {
        // If no sun is assigned and findSunInScene is checked, try to find the main light
        if (sun == null && findSunInScene)
        {
            Light mainLight = FindMainLight();
            if (mainLight != null)
            {
                sun = mainLight.transform;
            }
        }

        if (sun != null)
        {
            // Directly copy the sun's rotation to this object
            transform.rotation = sun.rotation;
        }
    }

    // Helper method to find the main light source in the scene
    private Light FindMainLight()
    {
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            // Look for a Directional Light (common for sun)
            if (light.type == LightType.Directional)
            {
                return light; // Return the first directional light found
            }
        }
        return null; // Return null if no suitable light is found
    }

#if UNITY_EDITOR
    // This ensures the rotation updates in Editor even when not playing
    void OnValidate()
    {
        if (sun == null && findSunInScene)
        {
            Light mainLight = FindMainLight();
            if (mainLight != null)
            {
                sun = mainLight.transform;
            }
        }

        if (sun != null)
        {
            transform.rotation = sun.rotation;
        }
    }
#endif
}
}