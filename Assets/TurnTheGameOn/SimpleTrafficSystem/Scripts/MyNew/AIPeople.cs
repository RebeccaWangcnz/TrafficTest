namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    //Rebe：该类主要用于控制行人的行为逻辑
    public class AIPeople : MonoBehaviour
    {
        public int assignedIndex { get; private set; }
        //[Tooltip("person's move speed")]
        //public float moveSpeed;
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Transform frontSensorTransform;
        [Tooltip("Front Sensor Length")]
        public float frontSensorLength;
        [Tooltip("Control point to orient/position the foot detection sensor. ")]
        public Transform footSensorTransform;
        [Tooltip("Foot Sensor Length")]
        public float footSensorLength;

        private bool isFrontHit;
        private bool isFootHit;
        private RaycastHit hitInfo;
        private RaycastHit fHitInfo;

        private int randomIndex;
        [HideInInspector]
        public Animator animator;
        public void RegisterPerson(AITrafficWaypointRoute route)
        {
            animator = GetComponent<Animator>();
            assignedIndex = AIPeopleController.Instance.RegisterPeopleAI(this, route);
        }//用于注册行人

        #region public api method
        public void StartMoving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, true);
        }
        /// <summary>
        /// The AIPeople will stop moving,use the same name as car to easily gotten by AITrafficWaypoint.cs
        /// </summary>
        public void StopDriving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, false);
            AIPeopleController.Instance.Set_IsLastPoint(assignedIndex, true);
        }//当时达到路线结尾处的点时使用这个方法
        
        public bool FrontSensorDetecting()
        {
            isFrontHit = Physics.Raycast(frontSensorTransform.position, transform.forward, out hitInfo, frontSensorLength, ~(1<<1));
            if (!isFrontHit)
                return false;           
            else
            {
                if (hitInfo.transform.GetComponent<AITrafficCar>()&&hitInfo.transform.GetComponent<Rigidbody>().velocity == Vector3.zero)//如果障碍是车子，并且车子停下来让位，则行人可以继续向前
                    return false;
                return true;
            }

        }//检测前面是否有障碍
        public bool FootSensorDetecting()
        {
            isFootHit = Physics.Raycast(footSensorTransform.position, transform.forward, out fHitInfo, footSensorLength, AIPeopleController.Instance.footLayerMask.value);
            return isFootHit;
        }
        /// <summary>
        /// change lane
        /// </summary>
        /// <param name="onReachWaypointSettings"></param>
        public void ChangeToRouteWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            onReachWaypointSettings.OnReachWaypointEvent.Invoke();

            AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.parentRoute);//更新路线
            AIPeopleController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.parentRoute.routeInfo);//更新路线信息
            AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);//更新路线点
            AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                (
                    assignedIndex,
                    onReachWaypointSettings.waypointIndexnumber - 1,
                    onReachWaypointSettings.waypoint
                );
            if (onReachWaypointSettings.waypoint)
            {
                Vector3 targetDirection = onReachWaypointSettings.waypoint.transform.position - transform.position;
                targetDirection.y = 0;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                AIPeopleController.Instance.Set_TargetRotation(assignedIndex, targetRotation);
            }

            AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);
        }

        #endregion

        #region waypoint trigger method
        public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            if (onReachWaypointSettings.parentRoute == AIPeopleController.Instance.GetPeopleRoute(assignedIndex))
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
                AITrafficWaypoint newpoint = onReachWaypointSettings.waypoint;
                if (onReachWaypointSettings.newRoutePoints.Length > 0)//更换新路线
                {
                    randomIndex = Random.Range(0, onReachWaypointSettings.newRoutePoints.Length);
                    newpoint = onReachWaypointSettings.newRoutePoints[randomIndex];
                    AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, newpoint.onReachWaypointSettings.parentRoute);//更新路线
                    AIPeopleController.Instance.Set_RouteInfo(assignedIndex, newpoint.onReachWaypointSettings.parentRoute.routeInfo);//更新路线信息
                    AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, newpoint.onReachWaypointSettings.waypointIndexnumber - 1);//更新路线点
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                        (
                            assignedIndex,
                            newpoint.onReachWaypointSettings.waypointIndexnumber - 1,
                            newpoint
                        );
                }
                else if (onReachWaypointSettings.waypointIndexnumber < onReachWaypointSettings.parentRoute.waypointDataList.Count)//不更新路线
                {
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                    (
                         assignedIndex,
                         onReachWaypointSettings.waypointIndexnumber,
                         onReachWaypointSettings.waypoint
                    );
                    newpoint = onReachWaypointSettings.nextPointInRoute;
                }
                //使行人朝向新的位置
                if (newpoint)
                {
                    Vector3 targetDirection = newpoint.transform.position - transform.position;
                    targetDirection.y = 0;
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    AIPeopleController.Instance.Set_TargetRotation(assignedIndex, targetRotation);
                }

                AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);
            }
        }
        #endregion
        #region gizmo
        private void OnDrawGizmos()
        {
            if (isFrontHit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(frontSensorTransform.position, hitInfo.point);
            }
            else
            {
                // 如果射线没有击中物体，将射线的末端位置绘制为绿色的 Gizmos 线条
                Gizmos.color = Color.green;
                Gizmos.DrawLine(frontSensorTransform.position, frontSensorTransform.position + transform.forward * frontSensorLength);
            }
            if (isFootHit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(footSensorTransform.position, fHitInfo.point);
            }
            else
            {
                // 如果射线没有击中物体，将射线的末端位置绘制为绿色的 Gizmos 线条
                Gizmos.color = Color.green;
                Gizmos.DrawLine(footSensorTransform.position, footSensorTransform.position + transform.forward * footSensorLength);
            }
        }
        #endregion
    }
}
