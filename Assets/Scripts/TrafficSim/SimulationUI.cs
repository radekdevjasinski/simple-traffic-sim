using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

namespace TrafficSim
{
    public class SimulationUI : MonoBehaviour
    {
        [Header("Config Inputs")]
        public TMP_InputField DurationInput;
        public TMP_InputField SpawnChanceInput;
        public TMP_InputField TimeScaleInput;
        public Button StartButton;

        [Header("Results Panel")]
        public GameObject ResultsPanel;
        public TextMeshProUGUI TotalVehiclesText;
        public TextMeshProUGUI AverageSpeedText;
        public TextMeshProUGUI AverageWaitText;
        public TextMeshProUGUI MaxWaitText;

        private void Start()
        {
            StartButton.onClick.AddListener(OnStartClicked);

            SimulationManager.Instance.OnSimulationStarted += () => ResultsPanel.SetActive(false);
            SimulationManager.Instance.OnSimulationEnded += ShowResults;

            ResultsPanel.SetActive(false);
        }

        private void OnStartClicked()
        {
            // Replace commas with dots to ensure parsing works regardless of the user's OS culture
            string parseDuration = DurationInput.text.Replace(',', '.');
            string parseChance = SpawnChanceInput.text.Replace(',', '.');
            string parseScale = TimeScaleInput.text.Replace(',', '.');

            float duration = float.TryParse(parseDuration, NumberStyles.Float, CultureInfo.InvariantCulture, out float d) ? d : 60f;
            float chance = float.TryParse(parseChance, NumberStyles.Float, CultureInfo.InvariantCulture, out float c) ? c : 0.5f;
            float scale = float.TryParse(parseScale, NumberStyles.Float, CultureInfo.InvariantCulture, out float s) ? s : 1f;

            SimulationManager.Instance.StartSimulation(duration, scale, chance);
        }

        private void ShowResults(SimulationStats stats)
        {
            ResultsPanel.SetActive(true);
            TotalVehiclesText.text = stats.TotalVehicles.ToString();
            AverageSpeedText.text = $"{stats.AverageSpeed:F2} m/s";
            AverageWaitText.text = $"{stats.AverageWaitTime:F2} s";
            MaxWaitText.text = $"{stats.MaxWaitTime:F2} s";
        }
    }
}
