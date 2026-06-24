using UnityEngine;

namespace TrafficSim
{
    public struct SimulationStats
    {
        public int TotalVehicles;
        public float AverageSpeed;
        public float AverageWaitTime;
        public float MaxWaitTime;
    }

    public class SimulationManager : MonoBehaviour
    {
        public static SimulationManager Instance { get; private set; }

        public bool IsRunning { get; private set; }
        public float SpawnChance { get; private set; } = 0.5f;

        private float _timer;
        private float _duration;

        // Data Aggregation
        private int _totalVehicles;
        private float _sumAverageSpeeds;
        private float _sumWaitTimes;
        private float _maxWaitTime;

        public event System.Action OnSimulationStarted;
        public event System.Action<SimulationStats> OnSimulationEnded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartSimulation(float duration, float timeScale, float spawnChance)
        {
            // Clear any lingering vehicles from previous runs
            var vehicles = FindObjectsByType<VehicleController>(FindObjectsSortMode.None);
            foreach (var v in vehicles) Destroy(v.gameObject);

            _duration = duration;
            SpawnChance = Mathf.Clamp01(spawnChance);
            Time.timeScale = timeScale;

            _timer = 0f;
            _totalVehicles = 0;
            _sumAverageSpeeds = 0f;
            _sumWaitTimes = 0f;
            _maxWaitTime = 0f;

            IsRunning = true;
            OnSimulationStarted?.Invoke();
        }

        private void Update()
        {
            if (!IsRunning) return;

            _timer += Time.deltaTime;

            if (_timer >= _duration)
            {
                EndSimulation();
            }
            Debug.Log($"Simulation Time: {_timer:F2} / {_duration} seconds");
        }

        public void RecordVehicle(float avgSpeed, float waitTime)
        {
            if (!IsRunning) return;

            _totalVehicles++;
            _sumAverageSpeeds += avgSpeed;
            _sumWaitTimes += waitTime;

            if (waitTime > _maxWaitTime)
                _maxWaitTime = waitTime;
        }

        private void EndSimulation()
        {
            IsRunning = false;
            Time.timeScale = 0f;

            SimulationStats stats = new SimulationStats
            {
                TotalVehicles = _totalVehicles,
                AverageSpeed = _totalVehicles > 0 ? (_sumAverageSpeeds / _totalVehicles) : 0f,
                AverageWaitTime = _totalVehicles > 0 ? (_sumWaitTimes / _totalVehicles) : 0f,
                MaxWaitTime = _maxWaitTime
            };

            OnSimulationEnded?.Invoke(stats);
        }
    }
}
