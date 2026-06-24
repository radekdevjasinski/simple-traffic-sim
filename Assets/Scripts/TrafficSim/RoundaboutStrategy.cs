using UnityEngine;

namespace TrafficSim
{
    public class RoundaboutStrategy : IIntersectionStrategy
    {
        public void Initialize(IntersectionController intersection) { }

        public void Cleanup(IntersectionController intersection) { }

        public void UpdateStrategy(IntersectionController intersection)
        {
            if (intersection.WaitingVehicles.Count == 0)
                return;

            VehicleController vehicleToRelease = null;

            foreach (var v in intersection.WaitingVehicles)
            {
                bool canProceed = true;

                int myIndex = intersection.WaitingVehicles.IndexOf(v);

                // Queue Enforcement: You cannot bypass a car waiting in front of you in the same lane.
                foreach (var other in intersection.WaitingVehicles)
                {
                    if (v == other) continue;
                    bool isSameDirection = Vector2.Dot(v.transform.up, other.transform.up) > 0.5f;
                    if (isSameDirection && intersection.WaitingVehicles.IndexOf(other) < myIndex)
                    {
                        canProceed = false;
                        break;
                    }
                }
                if (!canProceed) continue;

                // Virtual Sensor: "Is anyone coming towards my entry point?"
                foreach (var crossing in intersection.VehiclesInIntersection)
                {
                    Vector2 toCrossing = crossing.transform.position - v.transform.position;
                    float distance = toCrossing.magnitude;

                    if (distance < 7f) // Safely covers the immediate conflict zone
                    {
                        // Strict spatial gap: Prevent cars from the same lane from tailgating into the roundabout.
                        // If a car just entered right in front of us, we wait until it creates a safe physical buffer.
                        if (distance < 3.0f)
                        {
                            canProceed = false;
                            Debug.DrawLine(v.transform.position, crossing.transform.position, Color.red);
                            break;
                        }

                        // Using physics vectors: If the car's forward direction aligns with the direction AWAY from us,
                        // it has already safely passed our entry point and is no longer a threat.
                        bool isMovingAway = Vector2.Dot(crossing.transform.up, toCrossing.normalized) > 0.3f;

                        if (!isMovingAway)
                        {
                            canProceed = false;
                            Debug.DrawLine(v.transform.position, crossing.transform.position, Color.magenta);
                            break;
                        }
                    }
                }

                if (canProceed)
                {
                    vehicleToRelease = v;
                    break; // Only release one vehicle per frame to prevent clumped entries
                }
            }

            if (vehicleToRelease != null)
            {
                intersection.WaitingVehicles.Remove(vehicleToRelease);
                intersection.VehiclesInIntersection.Add(vehicleToRelease);
                vehicleToRelease.GrantClearance(intersection);
            }
        }

        public Vector2 GetSteeringTarget(IntersectionController intersection, VehicleController vehicle, Vector2 defaultTarget)
        {
            Vector2 center = intersection.transform.position;

            // If the vehicle's target waypoint is far from the center (like the final exit node), 
            // it has successfully finished navigating. We completely release it from the swirl physics.
            if (Vector2.Distance(center, defaultTarget) > 3.0f)
                return defaultTarget;

            Vector2 toCenter = center - (Vector2)vehicle.transform.position;
            float distanceToCenter = toCenter.magnitude;

            // If we are far from the center, just head to the default target (exit)
            if (distanceToCenter > 3.0f) return defaultTarget;

            Vector2 defaultDirection = (defaultTarget - (Vector2)vehicle.transform.position).normalized;

            // The ideal radius of the circular lane around the island
            float laneRadius = 1.2f;

            // CCW Tangent vector (perpendicular to the center)
            Vector2 ccwTangent = new Vector2(toCenter.y, -toCenter.x).normalized;

            // Radial correction (acts as a spring pushing cars into the ideal lane radius if they drift)
            Vector2 radialCorrection = toCenter.normalized * (distanceToCenter - laneRadius);

            // Check if the car naturally aligns with its intended exit
            float exitAlignment = Vector2.Dot(ccwTangent, defaultDirection);

            // If aligned with exit and safely navigating the outer edge of the island, break orbit and drive straight to the exit
            if (exitAlignment > 0.80f && distanceToCenter > laneRadius - 0.3f)
            {
                return defaultTarget;
            }

            // Otherwise, strictly enforce the circular path (Tangent + Radial Correction)
            Vector2 swirlDirection = (ccwTangent + radialCorrection * 2.0f).normalized;
            return (Vector2)vehicle.transform.position + swirlDirection;
        }
    }
}
