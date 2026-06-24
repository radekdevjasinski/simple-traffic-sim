using UnityEngine;

namespace TrafficSim
{
    public class UnmarkedStrategy : IIntersectionStrategy
    {
        public void Initialize(IntersectionController intersection) { }

        public void Cleanup(IntersectionController intersection) { }

        public void UpdateStrategy(IntersectionController intersection)
        {
            // If the physical intersection is clear and someone is waiting...
            if (intersection.VehiclesInIntersection.Count == 0 && intersection.WaitingVehicles.Count > 0)
            {
                // Grab the first car and grant it clearance to proceed
                VehicleController nextVehicle = intersection.WaitingVehicles[0];
                intersection.WaitingVehicles.RemoveAt(0);

                // Immediately mark the intersection as occupied to prevent other vehicles from being dequeued 
                // before this vehicle physically enters the trigger zone.
                intersection.VehiclesInIntersection.Add(nextVehicle);

                nextVehicle.GrantClearance(intersection);
            }
        }

        public Vector2 GetSteeringTarget(IntersectionController intersection, VehicleController vehicle, Vector2 defaultTarget)
            => defaultTarget;
    }
}
