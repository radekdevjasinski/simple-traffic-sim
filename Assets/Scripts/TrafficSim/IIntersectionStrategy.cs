using UnityEngine;

namespace TrafficSim
{
    /// <summary>
    /// The Strategy interface for intersection logic.
    /// </summary>
    public interface IIntersectionStrategy
    {
        void Initialize(IntersectionController intersection);
        void Cleanup(IntersectionController intersection);

        // Evaluates the queue and intersection state to grant clearance to vehicles.
        void UpdateStrategy(IntersectionController intersection);

        // Provides dynamic steering targets for vehicles inside the intersection
        Vector2 GetSteeringTarget(IntersectionController intersection, VehicleController vehicle, Vector2 defaultTarget);
    }
}
