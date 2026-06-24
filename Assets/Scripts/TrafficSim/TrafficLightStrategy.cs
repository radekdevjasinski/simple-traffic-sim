using UnityEngine;

namespace TrafficSim
{
    public class TrafficLightStrategy : IIntersectionStrategy
    {
        public enum LightPhase { VerticalGreen, Clearance, HorizontalGreen }

        public LightPhase CurrentPhase { get; private set; } = LightPhase.VerticalGreen;
        private LightPhase _nextPhase = LightPhase.HorizontalGreen;

        // Configurable timings
        public float GreenDuration = 8f;
        public float ClearanceDuration = 2.5f;
        private float _timer = 0f;

        private LineRenderer _line1;
        private LineRenderer _line2;

        public void Initialize(IntersectionController intersection)
        {
            _line1 = CreateLineRenderer(intersection.transform, "TrafficLightVis_1");
            _line2 = CreateLineRenderer(intersection.transform, "TrafficLightVis_2");
        }

        public void Cleanup(IntersectionController intersection)
        {
            if (_line1 != null)
            {
                Object.Destroy(_line1.material);
                Object.Destroy(_line1.gameObject);
            }
            if (_line2 != null)
            {
                Object.Destroy(_line2.material);
                Object.Destroy(_line2.gameObject);
            }
        }

        public void UpdateStrategy(IntersectionController intersection)
        {
            _timer += Time.deltaTime;

            // --- STATE MACHINE LOGIC ---
            if (CurrentPhase == LightPhase.Clearance && _timer >= ClearanceDuration)
            {
                CurrentPhase = _nextPhase;
                _timer = 0f;
            }
            else if (CurrentPhase != LightPhase.Clearance && _timer >= GreenDuration)
            {
                _nextPhase = CurrentPhase == LightPhase.VerticalGreen ? LightPhase.HorizontalGreen : LightPhase.VerticalGreen;
                CurrentPhase = LightPhase.Clearance;
                _timer = 0f;
            }

            UpdateVisualization(intersection);

            // If we are in the All-Red clearance phase, nobody is allowed to enter.
            if (CurrentPhase == LightPhase.Clearance) return;

            // --- TRAFFIC RELEASE LOGIC ---
            VehicleController vehicleToRelease = null;

            foreach (var v in intersection.WaitingVehicles)
            {
                // 1. Check if the vehicle is on the currently active Green axis
                bool isVertical = Mathf.Abs(Vector2.Dot(v.transform.up, Vector2.up)) > 0.5f;
                if (CurrentPhase == LightPhase.VerticalGreen && !isVertical) continue;
                if (CurrentPhase == LightPhase.HorizontalGreen && isVertical) continue;

                bool canProceed = true;

                // 2. Must not conflict with anyone currently finishing their crossing
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

                // 3. Must yield to other Green-light vehicles (Left turns yield to oncoming straight traffic)
                foreach (var other in intersection.WaitingVehicles)
                {
                    if (v == other) continue;

                    bool isSameDirection = Vector2.Dot(v.transform.up, other.transform.up) > 0.5f;

                    // Queue Enforcement: Cannot bypass a car waiting in front of you in the same lane.
                    if (isSameDirection && intersection.WaitingVehicles.IndexOf(other) < myIndex)
                    {
                        canProceed = false;
                        break;
                    }

                    // Only evaluate conflicts with other vehicles that also have a green light
                    bool otherIsVertical = Mathf.Abs(Vector2.Dot(other.transform.up, Vector2.up)) > 0.5f;
                    if (CurrentPhase == LightPhase.VerticalGreen && !otherIsVertical) continue;
                    if (CurrentPhase == LightPhase.HorizontalGreen && otherIsVertical) continue;

                    if (!PathConflicts(v, other)) continue;

                    bool isTurningLeft = v.CurrentRoute.gameObject.name.Contains("_Left");
                    bool isOncoming = Vector2.Dot(v.transform.up, other.transform.up) < -0.5f;

                    // Yield if turning left across oncoming traffic
                    if (isTurningLeft && isOncoming)
                    {
                        canProceed = false;
                        break; // I must wait for the oncoming car to go first
                    }
                }

                if (canProceed)
                {
                    vehicleToRelease = v;
                    break; // Release one car per frame
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
            => defaultTarget;

        private bool PathConflicts(VehicleController a, VehicleController b)
        {
            bool isOncoming = Vector2.Dot(a.transform.up, b.transform.up) < -0.5f;
            bool isSameDirection = Vector2.Dot(a.transform.up, b.transform.up) > 0.5f;

            // 1. Same direction: No intersection conflict.
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

            // 3. Cross-traffic: Always assume their paths cross for safety.
            return true;
        }

        private LineRenderer CreateLineRenderer(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = Vector3.zero;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.startWidth = 0.2f;
            lr.endWidth = 0.2f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.sortingOrder = 10;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            return lr;
        }

        private void UpdateVisualization(IntersectionController intersection)
        {
            if (_line1 == null || _line2 == null) return;

            Vector3 c = intersection.transform.position;

            if (CurrentPhase == LightPhase.VerticalGreen)
            {
                SetLine(_line1, c + Vector3.up * 2, c + Vector3.down * 2, Color.green);
                SetLine(_line2, c, c, Color.green); // hide by placing both points at the same spot
            }
            else if (CurrentPhase == LightPhase.HorizontalGreen)
            {
                SetLine(_line1, c + Vector3.left * 2, c + Vector3.right * 2, Color.green);
                SetLine(_line2, c, c, Color.green); // hide
            }
            else
            {
                SetLine(_line1, c + new Vector3(-1, 1, 0), c + new Vector3(1, -1, 0), Color.red);
                SetLine(_line2, c + new Vector3(-1, -1, 0), c + new Vector3(1, 1, 0), Color.red);
            }
        }

        private void SetLine(LineRenderer lr, Vector3 start, Vector3 end, Color color)
        {
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.startColor = color;
            lr.endColor = color;
        }
    }
}
