using UnityEngine;

namespace TrafficSim
{
    public class CruisingState : IVehicleState
    {
        public void Enter(VehicleController vehicle) { }

        public void Update(VehicleController vehicle)
        {
            float targetSpeed = vehicle.MaxSpeed;

            // Smoothly decelerate to a stop when approaching an intersection entry (UNLESS we have early clearance)
            if (vehicle.CurrentWaypoint != null && vehicle.CurrentWaypoint.IsIntersectionEntry && !vehicle.IsClearedToProceed)
            {
                float distanceToWaypoint = Vector2.Distance(vehicle.transform.position, vehicle.CurrentWaypoint.transform.position);
                float distanceToStop = Mathf.Max(0f, distanceToWaypoint - vehicle.WaypointThreshold);

                if (distanceToStop <= 0.05f)
                    targetSpeed = 0f;
                else
                {
                    float idealSpeed = Mathf.Sqrt(2f * vehicle.Deceleration * distanceToStop);
                    targetSpeed = Mathf.Min(targetSpeed, idealSpeed);
                }
            }

            if (vehicle.CurrentSpeed < targetSpeed)
                vehicle.CurrentSpeed = Mathf.MoveTowards(vehicle.CurrentSpeed, targetSpeed, vehicle.Acceleration * Time.deltaTime);
            else
                vehicle.CurrentSpeed = Mathf.MoveTowards(vehicle.CurrentSpeed, targetSpeed, vehicle.Deceleration * Time.deltaTime);

            vehicle.MoveTowardsWaypoint();
        }
    }

    public class BrakingState : IVehicleState
    {
        public void Enter(VehicleController vehicle) { }

        public void Update(VehicleController vehicle)
        {
            float distanceToStop = vehicle.ObstacleDistance - vehicle.SafeDistance;

            if (distanceToStop <= 0.05f)
            {
                // We are at or closer than the safe gap, stop completely
                vehicle.CurrentSpeed = Mathf.MoveTowards(vehicle.CurrentSpeed, 0f, vehicle.Deceleration * Time.deltaTime);
            }
            else
            {
                // Smooth pursuit kinematic calculation (v = sqrt(2 * a * d))
                float idealSpeed = Mathf.Sqrt(2f * vehicle.Deceleration * Mathf.Max(0f, distanceToStop));

                // Add the speed of the vehicle ahead so we match its pace smoothly
                if (vehicle.VehicleAhead != null)
                    idealSpeed += vehicle.VehicleAhead.CurrentSpeed;

                idealSpeed = Mathf.Clamp(idealSpeed, 0f, vehicle.MaxSpeed);

                if (vehicle.CurrentSpeed < idealSpeed)
                    vehicle.CurrentSpeed = Mathf.MoveTowards(vehicle.CurrentSpeed, idealSpeed, vehicle.Acceleration * Time.deltaTime);
                else
                    vehicle.CurrentSpeed = Mathf.MoveTowards(vehicle.CurrentSpeed, idealSpeed, vehicle.Deceleration * Time.deltaTime);
            }

            vehicle.MoveTowardsWaypoint();
        }
    }

    public class WaitingState : IVehicleState
    {
        public void Enter(VehicleController vehicle) { vehicle.CurrentSpeed = 0f; }
        public void Update(VehicleController vehicle) { /* Stand still */ }
    }
}
