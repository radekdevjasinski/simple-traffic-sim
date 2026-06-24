using UnityEngine;

namespace TrafficSim
{
    public class RightHandStrategy : IIntersectionStrategy
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

                // 1. Must not conflict with anyone currently crossing the intersection.
                foreach (var crossing in intersection.VehiclesInIntersection)
                {
                    if (PathConflicts(v, crossing))
                    {
                        canProceed = false;
                        break;
                    }
                }

                if (!canProceed) continue;

                int myIndex = intersection.WaitingVehicles.IndexOf(v);

                // 2. Must yield to other waiting vehicles based on Right-Hand and Turn rules.
                foreach (var other in intersection.WaitingVehicles)
                {
                    if (v == other) continue;

                    bool isSameDirection = Vector2.Dot(v.transform.up, other.transform.up) > 0.5f;

                    // Queue Enforcement: Single lane means you cannot bypass a car waiting in front of you.
                    if (isSameDirection && intersection.WaitingVehicles.IndexOf(other) < myIndex)
                    {
                        canProceed = false;
                        break;
                    }

                    // Only yield if our paths actually cross.
                    if (!PathConflicts(v, other)) continue;

                    // Math: Dot product of 'My Right' and 'Their Forward'
                    // If < -0.5, they are traveling in a direction that opposes my right vector
                    // (meaning they are coming from my right side, crossing to my left).
                    bool isToRight = Vector2.Dot(v.transform.right, other.transform.up) < -0.5f;

                    // Math: Dot product of 'My Forward' and 'Their Forward'
                    // If < -0.5, they are facing opposite to me (oncoming traffic).
                    bool isTurningLeft = v.CurrentRoute.gameObject.name.Contains("_Left");
                    bool isOncoming = Vector2.Dot(v.transform.up, other.transform.up) < -0.5f;

                    if (isToRight || (isTurningLeft && isOncoming))
                    {
                        canProceed = false;

                        // DEBUG TOOL: Draw a red line in the Scene view showing who we are yielding to.
                        // This allows us to visually verify the logic in real-time.
                        Debug.DrawLine(v.transform.position, other.transform.position, Color.yellow);

                        break; // I must yield, evaluate the next vehicle in line.
                    }
                }

                if (canProceed)
                {
                    vehicleToRelease = v;
                    break;
                }
            }

            // Deadlock Resolution: If everyone is yielding, but the physical intersection is completely empty, break the gridlock.
            if (vehicleToRelease == null && intersection.VehiclesInIntersection.Count == 0)
                vehicleToRelease = intersection.WaitingVehicles[0];

            if (vehicleToRelease != null)
            {
                intersection.WaitingVehicles.Remove(vehicleToRelease);
                intersection.VehiclesInIntersection.Add(vehicleToRelease);
                vehicleToRelease.GrantClearance(intersection);
            }
        }

        public Vector2 GetSteeringTarget(IntersectionController intersection, VehicleController vehicle, Vector2 defaultTarget)
            => defaultTarget;

        private bool PathConflicts(VehicleController a, VehicleController b)
        {
            bool isOncoming = Vector2.Dot(a.transform.up, b.transform.up) < -0.5f;
            bool isSameDirection = Vector2.Dot(a.transform.up, b.transform.up) > 0.5f;

            // 1. Same direction: No intersection conflict (sensors handle rear-end avoidance)
            if (isSameDirection) return false;

            // 2. Oncoming: Conflict ONLY if one is turning left and the other is NOT.
            if (isOncoming)
            {
                bool aTurningLeft = a.CurrentRoute.gameObject.name.Contains("_Left");
                bool bTurningLeft = b.CurrentRoute.gameObject.name.Contains("_Left");

                // If both are turning left, their paths don't conflict.
                if (aTurningLeft && bTurningLeft) return false;

                // If one is turning left and the other isn't (straight/right), they conflict.
                return aTurningLeft || bTurningLeft;
            }

            // 3. Cross-traffic: Always assume their paths cross for safety
            return true;
        }
    }
}
