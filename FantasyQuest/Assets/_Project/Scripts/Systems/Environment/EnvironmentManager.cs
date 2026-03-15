using UnityEngine;

namespace Project.Systems.Environment
{
    public enum TimeOfDay
    {
        Day,
        Night
    }

    public enum WeatherCondition
    {
        Clear,
        Rain,
        Fog
    }

    /// <summary>
    /// ゲーム内の環境（時間帯や天候）を管理するクラス。
    /// スポーンテーブルなどの条件判定に利用されます。
    /// ゆくゆくはここに「1日の時間経過」や「天候の変化」の処理を追加できます。
    /// </summary>
    public class EnvironmentManager : MonoBehaviour
    {
        public static EnvironmentManager Instance { get; private set; }

        [Header("Current Environment (テスト用にここから変更可能)")]
        public TimeOfDay CurrentTime = TimeOfDay.Day;
        public WeatherCondition CurrentWeather = WeatherCondition.Clear;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // シーンを跨いでも環境を維持したい場合は以下のコメントを外す
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
