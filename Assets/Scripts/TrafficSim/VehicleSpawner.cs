using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TrafficSim
{
    public class VehicleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject vehiclePrefab;
        [SerializeField] private List<SpawnConfiguration> spawnConfigurations;
        [SerializeField] private LayerMask vehicleLayer;
        [SerializeField] private float spawnClearanceRadius = 1.5f;

        [Tooltip("How frequently (in seconds) the spawner evaluates its spawn chance")]
        [SerializeField] private float spawnEvaluationTick = 1.0f;

        private void Start()
        {
            foreach (var config in spawnConfigurations)
            {
                StartCoroutine(SpawnVehicleCoroutine(config));
            }
        }

        private IEnumerator SpawnVehicleCoroutine(SpawnConfiguration config)
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnEvaluationTick);

                if (SimulationManager.Instance != null)
                {
                    if (!SimulationManager.Instance.IsRunning) continue;
                    if (Random.value > SimulationManager.Instance.SpawnChance) continue;
                }

                if (config.StartNode == null || config.PossibleRoutes.Count == 0)
                {
                    Debug.LogWarning($"Spawner configuration for '{config.Name}' is incomplete.", this);
                    continue;
                }

                Vector2 startPos = config.StartNode.transform.position;

                // Check if the spawn location is currently occupied by another vehicle
                if (Physics2D.OverlapCircle(startPos, spawnClearanceRadius, vehicleLayer) != null)
                {
                    continue; // Skip spawning this cycle if the place is taken
                }

                // Select the route first so we can determine the starting direction
                Route selectedRoute = config.PossibleRoutes[Random.Range(0, config.PossibleRoutes.Count)];

                // Calculate initial rotation to face the second node in the route
                Vector2 nextPos = selectedRoute.Nodes[1].transform.position;
                Vector2 direction = (nextPos - startPos).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f; // -90f for Unity's 2D UP vector
                Quaternion startRotation = Quaternion.Euler(0, 0, angle);

                GameObject vehicleGO = Instantiate(vehiclePrefab, startPos, startRotation);
                VehicleController controller = vehicleGO.GetComponent<VehicleController>();

                controller.CurrentRoute = selectedRoute;
            }
        }
    }

    [System.Serializable]
    public class SpawnConfiguration
    {
        public string Name; // For editor clarity
        public WaypointNode StartNode;
        public List<Route> PossibleRoutes;
    }
}
