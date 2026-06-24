using UnityEngine;

namespace TrafficSim
{
    public class VehicleController : MonoBehaviour
    {
        [Header("Movement Settings")]
        public float MaxSpeed = 5f;
        public float Acceleration = 3f;
        public float Deceleration = 6f;
        public float TurnSpeed = 200f;
        public float WaypointThreshold = 0.5f;

        [Header("Failsafes")]
        [Tooltip("How long a vehicle can stay completely still before being despawned (fixes deadlocks)")]
        public float MaxStuckTime = 20f;

        [Header("Pathfinding")]
        public Route CurrentRoute;
        private int _currentWaypointIndex = 0;

        [Header("Sensors")]
        public float SafeDistance = 1.0f;
        public float ObstacleDistance { get; private set; } = float.MaxValue;
        public VehicleController VehicleAhead { get; private set; }

        [Header("Intersection Sensors")]
        public float IntersectionApproachDistance = 3.0f;
        public bool HasRequestedClearance { get; private set; }
        public bool IsClearedToProceed { get; private set; }
        public bool IsWaitingAtIntersection => HasRequestedClearance && !IsClearedToProceed;

        public float CurrentSpeed { get; set; }
        private IVehicleState _currentState;
        public IntersectionController ActiveIntersection { get; set; }

        public WaypointNode CurrentWaypoint => (CurrentRoute != null && _currentWaypointIndex < CurrentRoute.Nodes.Count) ? CurrentRoute.Nodes[_currentWaypointIndex] : null;

        public readonly CruisingState CruisingState = new CruisingState();
        public readonly BrakingState BrakingState = new BrakingState();
        public readonly WaitingState WaitingState = new WaitingState();

        // Tracking Data
        private float _lifetime;
        private float _totalDistance;
        private float _totalWaitTime;
        private float _stuckTimer;
        private Vector3 _lastPosition;

        private void Start()
        {
            ChangeState(CruisingState);
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (CurrentWaypoint == null) return;
            _currentState?.Update(this);

            // Collect Frame Analytics
            _lifetime += Time.deltaTime;
            _totalDistance += Vector3.Distance(transform.position, _lastPosition);
            _lastPosition = transform.position;

            // Any speed close to 0 indicates the vehicle is stuck in traffic or yielding
            if (CurrentSpeed < 0.05f)
            {
                _totalWaitTime += Time.deltaTime;
                _stuckTimer += Time.deltaTime;

                if (_stuckTimer >= MaxStuckTime)
                {
                    Despawn();
                    return; // Stop updating this frame to prevent errors after despawning
                }
            }
            else
            {
                _stuckTimer = 0f;
            }
        }

        public void ChangeState(IVehicleState newState)
        {
            _currentState = newState;
            _currentState.Enter(this);
        }

        public void SetObstacle(float distance, VehicleController ahead)
        {
            ObstacleDistance = distance;
            VehicleAhead = ahead;
            if (_currentState == CruisingState)
                ChangeState(BrakingState);
        }

        public void ClearObstacle()
        {
            ObstacleDistance = float.MaxValue;
            VehicleAhead = null;
            if (_currentState == BrakingState)
                ChangeState(CruisingState);
        }

        public void MoveTowardsWaypoint()
        {
            if (CurrentWaypoint == null) return;

            // Move forward
            transform.Translate(Vector3.up * (CurrentSpeed * Time.deltaTime), Space.Self);

            Vector2 targetPosition = CurrentWaypoint.transform.position;
            if (ActiveIntersection != null)
            {
                targetPosition = ActiveIntersection.GetSteeringTarget(this, targetPosition);
            }

            // Rotate towards the waypoint
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
                Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, TurnSpeed * Time.deltaTime);
            }

            float distanceToWaypoint = Vector2.Distance(transform.position, CurrentWaypoint.transform.position);

            if (CurrentWaypoint.IsIntersectionEntry)
            {
                // 1. Request clearance early as we approach
                if (distanceToWaypoint < IntersectionApproachDistance && !HasRequestedClearance)
                {
                    HasRequestedClearance = true;
                    CurrentWaypoint.Intersection.EnqueueVehicle(this);
                }

                // 2. Reached the physical stop line
                if (distanceToWaypoint < WaypointThreshold)
                {
                    if (!IsClearedToProceed)
                    {
                        if (_currentState != WaitingState)
                            ChangeState(WaitingState); // Force a stop if clearance was denied
                    }
                    else
                    {
                        // We have clearance, cruise seamlessly through!
                        HasRequestedClearance = false;
                        IsClearedToProceed = false;
                        _currentWaypointIndex++;
                        if (CurrentWaypoint == null) Despawn();
                    }
                }
            }
            else
            {
                // Normal non-intersection waypoint behavior
                if (distanceToWaypoint < WaypointThreshold)
                {
                    _currentWaypointIndex++;
                    if (CurrentWaypoint == null) Despawn();
                }
            }
        }

        public void GrantClearance(IntersectionController intersection)
        {
            IsClearedToProceed = true;
            ActiveIntersection = intersection;

            // If the vehicle had fully stopped at the line, wake it up and advance its path
            if (_currentState == WaitingState)
            {
                HasRequestedClearance = false;
                IsClearedToProceed = false;

                _currentWaypointIndex++;
                if (CurrentWaypoint != null)
                    ChangeState(CruisingState);
                else
                    Despawn();
            }
        }

        private void Despawn()
        {
            if (SimulationManager.Instance != null && SimulationManager.Instance.IsRunning)
            {
                float averageSpeed = _lifetime > 0f ? (_totalDistance / _lifetime) : 0f;
                SimulationManager.Instance.RecordVehicle(averageSpeed, _totalWaitTime);
            }

            // Clean up from any intersection queues to prevent NullReferenceExceptions in the Strategy loops
            if (CurrentWaypoint != null && CurrentWaypoint.Intersection != null)
            {
                CurrentWaypoint.Intersection.WaitingVehicles.Remove(this);
                CurrentWaypoint.Intersection.VehiclesInIntersection.Remove(this);
            }
            if (ActiveIntersection != null)
            {
                ActiveIntersection.WaitingVehicles.Remove(this);
                ActiveIntersection.VehiclesInIntersection.Remove(this);
            }

            Destroy(gameObject);
        }
    }
}
