using UnityEngine;

namespace TrafficSim
{
    /// <summary>
    /// Represents a single point on a road. Links to the next point to form a lane.
    /// </summary>
    public class WaypointNode : MonoBehaviour
    {
        [Header("Intersection Data")]
        [Tooltip("Is this the node right before entering an intersection?")]
        public bool IsIntersectionEntry;
        [Tooltip("Reference to the intersection this node belongs to.")]
        public IntersectionController Intersection;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(transform.position, 0.2f);
        }
    }
}
