using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Diagnostics;



namespace ThornyDevtudio.RuntimeDebuggerToolkit
{
    public class FPSGraph : MonoBehaviour
    {
        public TextMeshProUGUI fpsText;
        public TextMeshProUGUI ramText;

        public RectTransform graphContainer;
        public int maxPoints = 100;
        public float graphHeight = 215f;
        public float graphWidth = 325f;

        [SerializeField] private GameObject tickLabelPrefab;
        [SerializeField] private int yTickCount = 4;
        [SerializeField] private float maxFps = 120f;
        [SerializeField] private bool useDynamicMaxFps = false;

        private readonly System.Collections.Generic.List<float> fpsValues = new();
        private float timePassed;
        private float updateRate = 0.2f;

        public TextMeshProUGUI systemInfoText;

        void Start()
        {
            string os = SystemInfo.operatingSystem;
            string cpu = SystemInfo.processorType;
            int cpuCores = SystemInfo.processorCount;
            string gpu = SystemInfo.graphicsDeviceName;
            string gpuVendor = SystemInfo.graphicsDeviceVendor;
            int ram = SystemInfo.systemMemorySize;
            int vram = SystemInfo.graphicsMemorySize;

            Resolution screenRes = Screen.currentResolution;
            int windowWidth = Screen.width;
            int windowHeight = Screen.height;

            systemInfoText.text =
                $"<size=16><b>System Info</b>\n" +
                $"<size=12><color=#D3D3D3>OS: {os}\n" +
                $"CPU: {cpu} ({cpuCores} cores)\n" +
                $"GPU: {gpu} ({gpuVendor})\n" +
                $"RAM: {ram} MB\n" +
                $"VRAM: {vram} MB\n" +
                $"Screen: {screenRes.width}x{screenRes.height} @{screenRes.refreshRateRatio.value:F2}Hz\n" +
                $"Window: {windowWidth}x{windowHeight}";
        }


        void Update()
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            timePassed += Time.unscaledDeltaTime;

            if (timePassed > updateRate)
            {
                timePassed = 0;

                fpsValues.Add(currentFPS);
                if (fpsValues.Count > maxPoints)
                    fpsValues.RemoveAt(0);

                float minFPS = fpsValues.Min();
                float maxFPS = fpsValues.Max();
                float avgFPS = fpsValues.Average();

                fpsText.text =
                    $"<size=28><b>FPS :</b> <color=#43F63D>{Mathf.RoundToInt(currentFPS)}</color></size>\n\n" +
                    $"<size=14>Min :</size> <color=#FFFFFF>{Mathf.RoundToInt(minFPS)}</color>\n" +
                    $"<size=14>Max :</size> <color=#FFFFFF>{Mathf.RoundToInt(maxFPS)}</color>\n" +
                    $"<size=14>Avg :</size> <color=#FFFFFF>{avgFPS:F1}</color>";



                float totalAllocated = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
                float totalReserved = UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f);
                float monoUsed = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);

                ramText.text =
                    $"<size=28><b> </b></size>\n\n" +
                    $"<size=14> RAM :\n" +
                    $"<size=14><color=#D3D3D3> Mono: {monoUsed:F1} MB</size></color>\n" +
                    $"<size=14><color=#D3D3D3> Allocated: {totalAllocated:F1} MB</size></color>\n" +
                    $"<size=14><color=#D3D3D3> Reserved: {totalReserved:F1} MB</size></color>";


                DrawGraph();
                //DrawAxes();
                DrawYAxisTicks();
            }
        }


        void DrawGraph()
        {
            foreach (Transform child in graphContainer)
                Destroy(child.gameObject);

            float xSpacing = graphWidth / maxPoints;

            float currentMaxFps = useDynamicMaxFps && fpsValues.Count > 0
                ? Mathf.Max(maxFps, Mathf.Ceil(fpsValues.Max()))
                : maxFps;

            for (int i = 0; i < fpsValues.Count - 1; i++)
            {
                float x1 = i * xSpacing;
                float y1 = Mathf.Clamp(fpsValues[i] / currentMaxFps, 0, 1) * graphHeight;
                float x2 = (i + 1) * xSpacing;
                float y2 = Mathf.Clamp(fpsValues[i + 1] / currentMaxFps, 0, 1) * graphHeight;

                CreateLine(new Vector2(x1, y1), new Vector2(x2, y2));
            }
        }

        void DrawAxes()
        {
            CreateAxisLine(Vector2.zero, new Vector2(graphWidth, 0)); // Axe X
            CreateAxisLine(Vector2.zero, new Vector2(0, graphHeight)); // Axe Y
        }

        void DrawYAxisTicks()
        {
            float currentMaxFps = useDynamicMaxFps && fpsValues.Count > 0
                ? Mathf.Max(maxFps, Mathf.Ceil(fpsValues.Max()))
                : maxFps;

            for (int i = 0; i <= yTickCount; i++)
            {
                float normalizedValue = i / (float)yTickCount;
                float yPos = normalizedValue * graphHeight;

                CreateAxisLine(new Vector2(0, yPos), new Vector2(graphWidth, yPos));

                GameObject labelObj = Instantiate(tickLabelPrefab, graphContainer);
                RectTransform labelRect = labelObj.GetComponent<RectTransform>();
                labelRect.anchoredPosition = new Vector2(-30f, yPos);
                TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
                tmp.text = Mathf.RoundToInt(normalizedValue * currentMaxFps).ToString();
                tmp.fontSize = 14;
                tmp.alignment = TextAlignmentOptions.Right;
            }
        }

        void CreateLine(Vector2 start, Vector2 end)
        {
            GameObject lineObj = new GameObject("Line", typeof(Image));
            lineObj.transform.SetParent(graphContainer, false);
            Image lineImage = lineObj.GetComponent<Image>();
            lineImage.color = Color.green;

            RectTransform rectTransform = lineImage.rectTransform;
            Vector2 dir = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(distance, 2f);
            rectTransform.anchoredPosition = start + dir * distance / 2;
            rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }

        void CreateAxisLine(Vector2 start, Vector2 end)
        {
            GameObject axisObj = new GameObject("Axis", typeof(Image));
            axisObj.transform.SetParent(graphContainer, false);
            Image axisImage = axisObj.GetComponent<Image>();
            axisImage.color = Color.grey;

            RectTransform rectTransform = axisImage.rectTransform;
            Vector2 dir = (end - start).normalized;
            float distance = Vector2.Distance(start, end);

            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.sizeDelta = new Vector2(distance, 2f);
            rectTransform.anchoredPosition = start + dir * distance / 2;
            rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
    }
}
