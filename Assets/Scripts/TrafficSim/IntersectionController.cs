using System.Collections.Generic;
using UnityEngine;

namespace TrafficSim
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class IntersectionController : MonoBehaviour
    {
        public enum IntersectionType { Unmarked, RightHand, TrafficLight, Roundabout }

        [Header("Intersection Settings")]
        public IntersectionType Type = IntersectionType.TrafficLight;

        public List<VehicleController> WaitingVehicles = new List<VehicleController>();
        public List<VehicleController> VehiclesInIntersection = new List<VehicleController>();

        private IIntersectionStrategy _strategy;

        private void Awake()
        {
            ApplySelectedStrategy();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
                ApplySelectedStrategy();
        }

        public void ApplySelectedStrategy()
        {
            switch (Type)
            {
                case IntersectionType.Unmarked: SetStrategy(new UnmarkedStrategy()); break;
                case IntersectionType.RightHand: SetStrategy(new RightHandStrategy()); break;
                case IntersectionType.TrafficLight: SetStrategy(new TrafficLightStrategy()); break;
                case IntersectionType.Roundabout: SetStrategy(new RoundaboutStrategy()); break;
            }
        }

        public void SetStrategy(IIntersectionStrategy newStrategy)
        {
            _strategy?.Cleanup(this);
            _strategy = newStrategy;
            _strategy?.Initialize(this);
        }

        public Vector2 GetSteeringTarget(VehicleController vehicle, Vector2 defaultTarget)
        {
            return _strategy != null ? _strategy.GetSteeringTarget(this, vehicle, defaultTarget) : defaultTarget;
        }

        private void Update()
        {
            _strategy?.UpdateStrategy(this);
        }

        public void EnqueueVehicle(VehicleController vehicle)
        {
            if (!WaitingVehicles.Contains(vehicle))
                WaitingVehicles.Add(vehicle);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out VehicleController vehicle))
            {
                VehiclesInIntersection.Remove(vehicle);
                if (vehicle.ActiveIntersection == this) vehicle.ActiveIntersection = null;
            }
        }

        private void OnDestroy()
        {
            _strategy?.Cleanup(this);
        }
    }
}
