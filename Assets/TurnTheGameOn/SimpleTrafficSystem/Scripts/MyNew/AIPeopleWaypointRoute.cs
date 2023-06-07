namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    //Rebe:新建脚本，该脚本主要用于给行人添加路线
    public class AIPeopleWaypointRoute : MonoBehaviour
    {
//        public bool isRegistered { get; protected set; }
//        public List<CarAIWaypointInfo> waypointDataList;
//        [Tooltip("Reference to the route's AITrafficWaypointRouteInfo script.")]
//        public AIPeopleWaypointRouteInfo routeInfo;
//        [Tooltip("Array of traffic car prefabs instantiated to the route on startup.")]
//        public GameObject[] spawnPeople;

//        private void Awake()
//        {
//            routeInfo = GetComponent<AIPeopleWaypointRouteInfo>();
//        }
//        private void Start()
//        {
//            RegisterRoute();
//            if (AITrafficController.Instance.usePooling == false)
//            {
//                SpawnTrafficVehicles();
//            }
//        }
//        #region Traffic Control
//        public void SpawnTrafficVehicles()
//        {

//            for (int i = 0, j = 0; i < spawnPeople.Length && j < waypointDataList.Count - 1; i++, j++)
//            {
//                Vector3 spawnPosition = waypointDataList[j]._transform.position;
//                spawnPosition.y += 1;
//                GameObject spawnedPerson = Instantiate(spawnPeople[i], spawnPosition, waypointDataList[j]._transform.rotation);
//                spawnedPerson.GetComponent<AIPeople>().RegisterPerson(this);
//                spawnedPerson.transform.LookAt(waypointDataList[j + 1]._transform);
//                j += 1; // increase j again tospawn vehicles with more space between
//            }

//        }//生成行人的主逻辑
//        #endregion

//        #region Utility Methods
//        public void RegisterRoute()
//        {
//            if (isRegistered == false)
//            {
//                AITrafficController.Instance.RegisterAIPeopleWaypointRoute(this);
//                isRegistered = true;
//            }
//        }
//        public void RemoveRoute()
//        {
//            if (isRegistered)
//            {
//                AITrafficController.Instance.RemoveAIPeopleWaypointRoute(this);
//                isRegistered = false;
//            }
//        }
//        #endregion

//        #region Unity Editor Helper Methods
//        bool IsCBetweenAB(Vector3 A, Vector3 B, Vector3 C)
//        {
//            return (
//                Vector3.Dot((B - A).normalized, (C - B).normalized) < 0f && Vector3.Dot((A - B).normalized, (C - A).normalized) < 0f &&
//                Vector3.Distance(A, B) >= Vector3.Distance(A, C) &&
//                Vector3.Distance(A, B) >= Vector3.Distance(B, C)
//                );
//        }

//#if UNITY_EDITOR
//        public Transform ClickToSpawnNextWaypoint(Vector3 _position)
//        {
//            GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, gameObject.transform) as GameObject;
//            CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
//            newPoint._name = newWaypoint.name = "AIPeopleWaypoint " + (waypointDataList.Count + 1);
//            newPoint._transform = newWaypoint.transform;
//            newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
//            newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = waypointDataList.Count + 1;
//            newPoint._waypoint.onReachWaypointSettings.parentPeopleRoute = this;//更改parentroute的类型
//            newPoint._waypoint.onReachWaypointSettings.speedLimit = 25f;
//            waypointDataList.Add(newPoint);
//            return newPoint._transform;
//        }

//        public void ClickToInsertSpawnNextWaypoint(Vector3 _position)
//        {
//            bool isBetweenPoints = false;
//            int insertIndex = 0;
//            if (waypointDataList.Count >= 2)
//            {
//                for (int i = 0; i < waypointDataList.Count - 1; i++)
//                {
//                    Vector3 point_A = waypointDataList[i]._transform.position;
//                    Vector3 point_B = waypointDataList[i + 1]._transform.position;
//                    isBetweenPoints = IsCBetweenAB(point_A, point_B, _position);
//                    insertIndex = i + 1;
//                    if (isBetweenPoints) break;
//                }
//            }

//            GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, gameObject.transform) as GameObject;
//            CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
//            newPoint._transform = newWaypoint.transform;
//            newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
//            newPoint._waypoint.onReachWaypointSettings.parentPeopleRoute = this;
//            newPoint._waypoint.onReachWaypointSettings.speedLimit = 25f;
//            if (isBetweenPoints)
//            {
//                newPoint._transform.SetSiblingIndex(insertIndex);
//                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (insertIndex + 1);
//                waypointDataList.Insert(insertIndex, newPoint);
//                for (int i = 0; i < waypointDataList.Count; i++)
//                {
//                    int newIndexName = i + 1;
//                    waypointDataList[i]._transform.gameObject.name = "AITrafficWaypoint " + newIndexName;
//                    waypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber = i + 1;
//                }
//            }
//            else
//            {
//                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (waypointDataList.Count + 1);
//                newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = waypointDataList.Count + 1;
//                waypointDataList.Add(newPoint);
//            }
//        }
//#endif
//        #endregion

//        #region Gizmos
//        private void OnDrawGizmos() { if (STSPrefs.routeGizmos) DrawGizmos(false); }
//        private void OnDrawGizmosSelected() { if (STSPrefs.routeGizmos) DrawGizmos(true); }

//        [HideInInspector] Transform arrowPointer;
//        private Transform junctionPosition;
//        private Matrix4x4 previousMatrix;
//        private int lookAtIndex;

//        private void DrawGizmos(bool selected)
//        {
//            if (!arrowPointer)
//            {
//                arrowPointer = new GameObject("ARROWPOINTER").transform;
//                arrowPointer.gameObject.hideFlags = HideFlags.HideAndDontSave;
//            }

//            // Draw line to new route points
//            Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
//            for (int i = 0; i < waypointDataList.Count; i++)
//            {
//                if (waypointDataList[i]._waypoint != null)
//                {
//                    Gizmos.color = selected ? STSPrefs.selectedJunctionColor : STSPrefs.junctionColor;
//                    if (waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints.Length > 0)
//                    {
//                        for (int j = 0; j < waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints.Length; j++)
//                        {
//                            if (waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints[j] != null)
//                            {
//                                Gizmos.DrawLine(waypointDataList[i]._transform.position, waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints[j].transform.position);
//                            }
//                        }
//                    }
//                }
//                else
//                {
//                    break;
//                }
//            }

//            // Draw line to next waypoint and lane change points
//            if (waypointDataList.Count > 1)
//            {
//                for (int i = 1; i < waypointDataList.Count; i++)
//                {
//                    Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
//                    Gizmos.DrawLine(waypointDataList[i - 1]._transform.position, waypointDataList[i]._transform.position); /// Line to next waypoint
//                    if (waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints != null)
//                    {
//                        for (int j = 0; j < waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints.Count; j++) // lines to lane chane points
//                        {
//                            if (waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints[j] != null)
//                                Gizmos.DrawLine(waypointDataList[i - 1]._transform.position, waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints[j].transform.position);
//                        }
//                    }
//                }
//            }

//            // Draw Arrows to connecting waypoints
//            if (waypointDataList.Count > 1)
//            {
//                Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
//                for (int i = 0; i < waypointDataList.Count; i++)
//                {
//                    previousMatrix = Gizmos.matrix;
//                    if (waypointDataList[waypointDataList.Count - 2]._waypoint != null && waypointDataList[i]._waypoint != null)
//                    {
//                        arrowPointer.position = i == 0 ? waypointDataList[waypointDataList.Count - 2]._waypoint.transform.position : waypointDataList[i]._waypoint.transform.position;
//                        lookAtIndex = i == 0 ? waypointDataList.Count - 1 : i - 1;
//                        if (i == 0)
//                        {
//                            arrowPointer.LookAt(waypointDataList[waypointDataList.Count - 1]._waypoint.transform);
//                            arrowPointer.position = waypointDataList[i]._waypoint.transform.position;
//                            arrowPointer.Rotate(0, 180, 0);
//                        }
//                        else arrowPointer.LookAt(waypointDataList[lookAtIndex]._waypoint.transform);
//                        Gizmos.matrix = Matrix4x4.TRS(waypointDataList[lookAtIndex]._waypoint.transform.position, arrowPointer.rotation, STSPrefs.arrowScale); // x, x, scale
//                        Gizmos.DrawFrustum(Vector3.zero, 10f, 2f, 0f, 5f); // x, width, length, x, x
//                    }
//                    else
//                    {
//                        break;
//                    }
//                    previousMatrix = Gizmos.matrix;
//                }
//            }

//            // Draw Arrows to junctions
//            Gizmos.color = selected ? STSPrefs.selectedYieldTriggerColor : STSPrefs.yieldTriggerColor;
//            for (int i = 0; i < waypointDataList.Count; i++)
//            {
//                if (waypointDataList[i]._waypoint != null && waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints != null)
//                {
//                    for (int j = 0; j < waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints.Count; ++j)
//                    {
//                        if (waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints[j] != null)
//                        {
//                            Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
//                            previousMatrix = Gizmos.matrix;
//                            junctionPosition = waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints[j].transform;
//                            arrowPointer.position = waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints[j].transform.position; //waypointData [i]._transform.position;
//                            arrowPointer.LookAt(waypointDataList[i]._transform);
//                            Gizmos.matrix = Matrix4x4.TRS(junctionPosition.position, arrowPointer.rotation, STSPrefs.arrowScale); // x, x, scale
//                            Gizmos.DrawFrustum(Vector3.zero, 10f, 2f, 0f, 5f); // x, width, length, x, x
//                            Gizmos.matrix = previousMatrix;
//                        }
//                    }
//                }
//                else
//                {
//                    break;
//                }
//            }

//            if (routeInfo == null)
//            {
//                routeInfo = GetComponent<AIPeopleWaypointRouteInfo>();
//            }
//        }
//        #endregion

    }
}
