namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    //Rebe��������Ҫ���ڿ������˵���Ϊ�߼�
    public class AIPeople : MonoBehaviour
    {
        public int assignedIndex { get; private set; }
        [Tooltip("person's move speed")]
        public float moveSpeed;

        private AITrafficWaypointRoute startRoute;
        private Rigidbody rb;
        public void RegisterPerson(AITrafficWaypointRoute route)
        {
            assignedIndex = AIPeopleController.Instance.RegisterPeopleAI(this, route);
            startRoute = route;
            rb = GetComponent<Rigidbody>();
        }//����ע������
        #region public api method
        public void StartMoving()
        {
            
        }
        /// <summary>
        /// The AIPeople will stop moving,use the same name as car to easily gotten by AITrafficWaypoint.cs
        /// </summary>
        [ContextMenu("StopDriving")]
        public void StopDriving()
        {
            AIPeopleController.Instance.Set_IsDrivingArray(assignedIndex, false);
        }
        #endregion
        #region waypoint trigger method
        public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            if (onReachWaypointSettings.parentRoute == AIPeopleController.Instance.GetPeopleRoute(assignedIndex))
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                if (onReachWaypointSettings.waypointIndexnumber < onReachWaypointSettings.parentRoute.waypointDataList.Count)
                {
                       AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                       (
                            assignedIndex,
                            onReachWaypointSettings.waypointIndexnumber
                            //onReachWaypointSettings.waypoint
                       );
                    //ʹ���˳����µ�λ��
                    Vector3 targetDirection = onReachWaypointSettings.parentRoute.waypointDataList[onReachWaypointSettings.waypointIndexnumber]._waypoint.transform.position - transform.position;
                    targetDirection.y = 0;
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    transform.rotation = targetRotation;
                    //transform.LookAt(onReachWaypointSettings.parentRoute.waypointDataList[onReachWaypointSettings.waypointIndexnumber]._waypoint.transform);
                }
                AITrafficController.Instance.Set_RoutePointPositionArray(assignedIndex);
            }
        }
            #endregion
        }
}
