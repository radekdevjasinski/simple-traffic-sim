using System.Collections.Generic;
using UnityEngine;

namespace TrafficSim
{
    /// <summary>
    /// Automates the construction of a 4-way intersection using the Builder Pattern.
    /// </summary>
    public class IntersectionBuilder : MonoBehaviour
    {
        [Header("Intersection Dimensions")]
        public float LaneWidth = 1.2f;
        public float RoadLength = 6.0f;

        [Header("Visuals")]
        public Sprite RoadSprite;
        public Color RoadColor = new Color(0.15f, 0.15f, 0.15f); // Dark Asphalt Gray
        public int SortingOrder = -10; // Ensure roads render behind cars (0)

        // To hold generated nodes for route creation
        private readonly Dictionary<string, (WaypointNode start, WaypointNode end)> _approachRoads = new Dictionary<string, (WaypointNode, WaypointNode)>();
        private readonly Dictionary<string, (WaypointNode start, WaypointNode end)> _exitRoads = new Dictionary<string, (WaypointNode, WaypointNode)>();

        [ContextMenu("Generate 4-Way Intersection")]
        public void GenerateIntersection()
        {
            // Clean up any existing generated nodes before building new ones
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }

            _approachRoads.Clear();
            _exitRoads.Clear();

            BuildVisuals();

            // Add or get the Intersection Controller
            IntersectionController controller = gameObject.GetComponent<IntersectionController>();
            if (controller == null) controller = gameObject.AddComponent<IntersectionController>();

            // Configure the trigger collider that represents the physical center of the intersection
            BoxCollider2D col = gameObject.GetComponent<BoxCollider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(LaneWidth * 2f, LaneWidth * 2f);

            float halfLane = LaneWidth / 2f;
            float intersectionBoundary = LaneWidth; // How far from the center the intersection entry/exit nodes are.

            // We assume Right-Hand traffic. 
            // For each direction, we build an approach road and an exit road, then link them.

            // Northbound Traffic (travels South to North)
            _approachRoads["South"] = BuildRoadSegment("SouthRoad_Approach", new Vector2(halfLane, -RoadLength), new Vector2(halfLane, -intersectionBoundary), controller);
            _exitRoads["North"] = BuildRoadSegment("NorthRoad_Exit", new Vector2(halfLane, intersectionBoundary), new Vector2(halfLane, RoadLength));

            // Southbound Traffic (travels North to South)
            _approachRoads["North"] = BuildRoadSegment("NorthRoad_Approach", new Vector2(-halfLane, RoadLength), new Vector2(-halfLane, intersectionBoundary), controller);
            _exitRoads["South"] = BuildRoadSegment("SouthRoad_Exit", new Vector2(-halfLane, -intersectionBoundary), new Vector2(-halfLane, -RoadLength));

            // Eastbound Traffic (travels West to East)
            _approachRoads["West"] = BuildRoadSegment("WestRoad_Approach", new Vector2(-RoadLength, -halfLane), new Vector2(-intersectionBoundary, -halfLane), controller);
            _exitRoads["East"] = BuildRoadSegment("EastRoad_Exit", new Vector2(intersectionBoundary, -halfLane), new Vector2(RoadLength, -halfLane));

            // Westbound Traffic (travels East to West)
            _approachRoads["East"] = BuildRoadSegment("EastRoad_Approach", new Vector2(RoadLength, halfLane), new Vector2(intersectionBoundary, halfLane), controller);
            _exitRoads["West"] = BuildRoadSegment("WestRoad_Exit", new Vector2(-intersectionBoundary, halfLane), new Vector2(-RoadLength, halfLane));

            GenerateRoutes();

            Debug.Log("Intersection successfully generated!");
        }

        private void GenerateRoutes()
        {
            if (_approachRoads.Count == 0)
            {
                Debug.LogError("Please generate the intersection first to create spawn/exit nodes.");
                return;
            }

            GameObject routesParent = new GameObject("Routes");
            routesParent.transform.SetParent(this.transform);
            routesParent.transform.localPosition = Vector3.zero;

            // Southbound traffic (starts North)
            CreateRoute("North", "South", "Straight", routesParent.transform);
            CreateRoute("North", "East", "Left", routesParent.transform);
            CreateRoute("North", "West", "Right", routesParent.transform);

            // Northbound traffic (starts South)
            CreateRoute("South", "North", "Straight", routesParent.transform);
            CreateRoute("South", "West", "Left", routesParent.transform);
            CreateRoute("South", "East", "Right", routesParent.transform);

            // Westbound traffic (starts East)
            CreateRoute("East", "West", "Straight", routesParent.transform);
            CreateRoute("East", "South", "Left", routesParent.transform);
            CreateRoute("East", "North", "Right", routesParent.transform);

            // Eastbound traffic (starts West)
            CreateRoute("West", "East", "Straight", routesParent.transform);
            CreateRoute("West", "North", "Left", routesParent.transform);
            CreateRoute("West", "South", "Right", routesParent.transform);
        }

        private void CreateRoute(string from, string to, string turnType, Transform parent)
        {
            GameObject routeObj = new GameObject($"Route_{from}_to_{to}_{turnType}");
            routeObj.transform.SetParent(parent);
            Route newRoute = routeObj.AddComponent<Route>();

            var approach = _approachRoads[from];
            var exit = _exitRoads[to];

            newRoute.Nodes.Add(approach.start);
            newRoute.Nodes.Add(approach.end);
            newRoute.Nodes.Add(exit.start);
            newRoute.Nodes.Add(exit.end);
        }

        private void BuildVisuals()
        {
            if (RoadSprite == null)
            {
                Debug.LogWarning("RoadSprite is missing! Please assign a square sprite for road visualization.");
                return;
            }

            GameObject visualParent = new GameObject("Visuals");
            visualParent.transform.SetParent(this.transform);
            visualParent.transform.localPosition = Vector3.zero;

            // Two overlapping rectangles create a perfect 4-way cross
            CreateRoadQuad("Asphalt_Vertical", new Vector2(LaneWidth * 2f, RoadLength * 2f), visualParent.transform);
            CreateRoadQuad("Asphalt_Horizontal", new Vector2(RoadLength * 2f, LaneWidth * 2f), visualParent.transform);
        }

        private void CreateRoadQuad(string name, Vector2 scale, Transform parent)
        {
            GameObject quad = new GameObject(name);
            quad.transform.SetParent(parent);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale = scale;

            SpriteRenderer sr = quad.AddComponent<SpriteRenderer>();
            sr.sprite = RoadSprite;
            sr.color = RoadColor;
            sr.sortingOrder = SortingOrder;
        }

        private (WaypointNode startNode, WaypointNode endNode) BuildRoadSegment(string roadName, Vector2 startPoint, Vector2 endPoint, IntersectionController controller = null)
        {
            GameObject roadParent = new GameObject(roadName);
            roadParent.transform.SetParent(this.transform);
            roadParent.transform.localPosition = Vector3.zero;

            WaypointNode startNode = CreateNode($"{roadName}_Start", startPoint, roadParent.transform);
            WaypointNode endNode = CreateNode($"{roadName}_End", endPoint, roadParent.transform);

            // If a controller is passed, this is an approach road. Tag its end node as an intersection entry point.
            if (controller != null)
            {
                endNode.IsIntersectionEntry = true;
                endNode.Intersection = controller;
            }
            return (startNode, endNode);
        }

        private WaypointNode CreateNode(string nodeName, Vector2 localPosition, Transform parent)
        {
            GameObject nodeObj = new GameObject($"Node_{nodeName}");
            nodeObj.transform.SetParent(parent);

            // Apply the local position relative to the intersection's center
            nodeObj.transform.localPosition = localPosition;

            // Unity 2D uses the Z axis for rotation, but nodes are just points so rotation doesn't strictly matter here.
            // However, facing them towards their travel direction is good practice.
            return nodeObj.AddComponent<WaypointNode>();
        }
    }
}
