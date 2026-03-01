using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

namespace ThornyDevtudio.RuntimeDebuggerToolkit
{

    public class InGameConsoleDisplay : MonoBehaviour
    {
        public RectTransform contentPanel;
        public GameObject logEntryPrefab;
        public int maxMessages = 50;

        public Button clearButton;
        public Toggle collapseToggle;

        private readonly Queue<GameObject> messageQueue = new();
        public Toggle logToggle;
        public Toggle warningToggle;
        public Toggle errorToggle;

        private readonly Queue<GameObject> entryPool = new();
        private readonly List<(string message, string stackTrace, LogType type, System.DateTime timestamp)> fullLogHistory = new();

        private readonly Dictionary<(string, LogType), (int count, TextMeshProUGUI tmp, System.DateTime lastTime)> collapsedMessages = new();



        private void Awake()
        {
            for (int i = 0; i < maxMessages; i++)
            {
                GameObject entry = Instantiate(logEntryPrefab);
                entry.SetActive(false);
                entryPool.Enqueue(entry);
            }

            clearButton.onClick.AddListener(ClearAll);
            if (logToggle != null) logToggle.onValueChanged.AddListener(_ => RefreshConsole());
            if (warningToggle != null) warningToggle.onValueChanged.AddListener(_ => RefreshConsole());
            if (errorToggle != null) errorToggle.onValueChanged.AddListener(_ => RefreshConsole());
            if (collapseToggle != null) collapseToggle.onValueChanged.AddListener(_ => RefreshConsole());
        }




        private void OnEnable()
        {
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleLog;
        }




        void HandleLog(string logString, string stackTrace, LogType type)
        {
            fullLogHistory.Add((logString, stackTrace, type, System.DateTime.Now));

            DisplayLog(logString, stackTrace, type, System.DateTime.Now);
        }



        public void ClearConsoleUIOnly()
        {
            foreach (var obj in messageQueue)
            {
                obj.SetActive(false);
                obj.transform.SetParent(null);
                entryPool.Enqueue(obj);
            }

            messageQueue.Clear();


            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);
            collapsedMessages.Clear();
        }

        public void ClearAll()
        {
            fullLogHistory.Clear();
            ClearConsoleUIOnly();
            collapsedMessages.Clear();
        }


        private IEnumerator ScrollToBottomAfterLayout()
        {
            yield return new WaitForEndOfFrame();
            Canvas.ForceUpdateCanvases();

            ScrollRect scroll = contentPanel.GetComponentInParent<ScrollRect>();
            if (scroll != null)
            {
                scroll.verticalNormalizedPosition = 0f;
            }
        }

        public void RefreshConsole()
        {
            StopAllCoroutines();
            StartCoroutine(RefreshConsoleCoroutine());
        }

        private IEnumerator RefreshConsoleCoroutine()
        {
            ClearConsoleUIOnly();

            var historyCopy = new List<(string message, string stackTrace, LogType type, System.DateTime timestamp)>(fullLogHistory);

            if (collapseToggle != null && collapseToggle.isOn)
            {
                Dictionary<(string, LogType), (int count, System.DateTime lastTimestamp)> collapsedCounts = new();

                foreach (var log in historyCopy)
                {
                    // Filter by toggle
                    if ((log.type == LogType.Log && (logToggle == null || !logToggle.isOn)) ||
                        (log.type == LogType.Warning && (warningToggle == null || !warningToggle.isOn)) ||
                        ((log.type == LogType.Error || log.type == LogType.Exception || log.type == LogType.Assert) &&
                         (errorToggle == null || !errorToggle.isOn)))
                    {
                        continue;
                    }

                    var key = (log.message, log.type);
                    if (collapsedCounts.TryGetValue(key, out var data))
                    {
                        collapsedCounts[key] = (data.count + 1, log.timestamp);
                    }
                    else
                    {
                        collapsedCounts[key] = (1, log.timestamp);
                    }
                }


                int batchSize = 10;
                int count = 0;

                foreach (var kvp in collapsedCounts)
                {
                    string msg = kvp.Key.Item1;
                    LogType type = kvp.Key.Item2;
                    int occurences = kvp.Value.count;
                    System.DateTime lastTime = kvp.Value.lastTimestamp;

                    GameObject entry = GetPooledLogEntry();
                    TextMeshProUGUI tmp = entry.GetComponent<TextMeshProUGUI>();

                    string timeStampNew = $"<color=#888>[{lastTime:HH:mm:ss}]</color>";
                    tmp.text = $"{timeStampNew} ({occurences}x) {msg}";
                    tmp.enableWordWrapping = true;

                    switch (type)
                    {
                        case LogType.Warning: tmp.color = Color.yellow; break;
                        case LogType.Error:
                        case LogType.Exception:
                        case LogType.Assert:
                            tmp.color = Color.red; break;
                        default: tmp.color = Color.white; break;
                    }

                    collapsedMessages[(msg, type)] = (occurences, tmp, lastTime);
                    messageQueue.Enqueue(entry);

                    if (++count >= batchSize)
                    {
                        count = 0;
                        yield return null;
                    }
                }
            }
            else
            {
                int batchSize = 10;
                int count = 0;

                foreach (var log in historyCopy)
                {
                    DisplayLog(log.message, log.stackTrace, log.type, log.timestamp);
                    if (++count >= batchSize)
                    {
                        count = 0;
                        yield return null;
                    }
                }
            }
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);
            yield return ScrollToBottomAfterLayout();
        }



        void DisplayLog(string logString, string stackTrace, LogType type, System.DateTime timestamp)
        {
            if ((type == LogType.Log && (logToggle == null || !logToggle.isOn)) ||
                (type == LogType.Warning && (warningToggle == null || !warningToggle.isOn)) ||
                ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) &&
                 (errorToggle == null || !errorToggle.isOn)))
            {
                return;
            }

            var key = (logString, type);

            if (collapseToggle != null && collapseToggle.isOn)
            {
                if (collapsedMessages.TryGetValue(key, out var entry))
                {
                    int newCount = entry.count + 1;
                    entry.tmp.text = $"<color=#888>[{timestamp:HH:mm:ss}]</color> ({newCount}x) {logString}";
                    collapsedMessages[key] = (newCount, entry.tmp, timestamp);
                    return;
                }
            }

            GameObject newEntry = GetPooledLogEntry();
            TextMeshProUGUI tmp = newEntry.GetComponent<TextMeshProUGUI>();

            string timeStampNew = $"<color=#888>[{timestamp:HH:mm:ss}]</color>";
            tmp.text = $"{timeStampNew} {logString}";
            tmp.enableWordWrapping = true;

            switch (type)
            {
                case LogType.Warning: tmp.color = Color.yellow; break;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    tmp.color = Color.red; break;
                default: tmp.color = Color.white; break;
            }

            if (collapseToggle != null && collapseToggle.isOn)
            {
                collapsedMessages[key] = (1, tmp, timestamp);
            }

            messageQueue.Enqueue(newEntry);

            if (messageQueue.Count > maxMessages)
            {
                GameObject oldEntry = messageQueue.Dequeue();
                oldEntry.SetActive(false);
                oldEntry.transform.SetParent(null);
                entryPool.Enqueue(oldEntry);
            }
        }


        private GameObject GetPooledLogEntry()
        {
            GameObject entry;

            if (entryPool.Count > 0)
            {
                entry = entryPool.Dequeue();
                entry.transform.SetParent(contentPanel, false);
                entry.SetActive(true);
            }
            else
            {
                entry = Instantiate(logEntryPrefab, contentPanel);
            }

            return entry;
        }
    }
}

