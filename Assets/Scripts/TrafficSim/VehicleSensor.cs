using UnityEngine;

namespace TrafficSim
{
    [RequireComponent(typeof(VehicleController))]
    public class VehicleSensor : MonoBehaviour
    {
        public float SensorOffset = 0.6f;
        public float SensorLength = 2.5f;
        public LayerMask VehicleLayer;

        private VehicleController _controller;
        private bool _obstacleDetected = false;

        private void Awake()
        {
            _controller = GetComponent<VehicleController>();
        }

        private void Update()
        {
            Vector2 sensorOrigin = (Vector2)transform.position + ((Vector2)transform.up * SensorOffset);
            float sensorThickness = 0.30f;
            RaycastHit2D[] hits = Physics2D.CircleCastAll(sensorOrigin, sensorThickness, transform.up, SensorLength, VehicleLayer);

            Debug.DrawRay(sensorOrigin + (Vector2)transform.right * sensorThickness, transform.up * SensorLength, Color.red);
            Debug.DrawRay(sensorOrigin - (Vector2)transform.right * sensorThickness, transform.up * SensorLength, Color.red);

            bool foundObstacle = false;

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject == this.gameObject) continue;

                if (hit.collider.TryGetComponent(out VehicleController ahead))
                {
                    bool ignoreObstacle = false;

                    if (ahead.IsWaitingAtIntersection)
                    {
                        float directionSimilarity = Vector2.Dot(transform.up, hit.transform.up);
                        if (directionSimilarity <= 0.5f)
                            ignoreObstacle = true;
                    }

                    if (!ignoreObstacle)
                    {
                        _controller.SetObstacle(hit.distance, ahead);
                        _obstacleDetected = true;
                        foundObstacle = true;
                        break;
                    }
                }
            }

            if (!foundObstacle && _obstacleDetected)
            {
                _obstacleDetected = false;
                _controller.ClearObstacle();
            }
        }
    }
}
