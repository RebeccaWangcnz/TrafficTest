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
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Transform frontSensorTransform;
        [Tooltip("Front Sensor Length")]
        public float frontSensorLength;

        private bool isFrontHit;
        private RaycastHit hitInfo;
        private int randomIndex;
        public void RegisterPerson(AITrafficWaypointRoute route)
        {
            assignedIndex = AIPeopleController.Instance.RegisterPeopleAI(this, route);
        }//����ע������
        #region public api method
        public void StartMoving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, true);
        }
        /// <summary>
        /// The AIPeople will stop moving,use the same name as car to easily gotten by AITrafficWaypoint.cs
        /// </summary>
        [ContextMenu("StopDriving")]
        public void StopDriving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, false);
            AIPeopleController.Instance.Set_IsLastPoint(assignedIndex, true);
        }//��ʱ�ﵽ·�߽�β���ĵ�ʱʹ���������
        //���ǰ���Ƿ����ϰ�
        public bool FrontSensorDetecting()
        {
            isFrontHit = Physics.Raycast(frontSensorTransform.position, transform.forward, out hitInfo, frontSensorLength, AIPeopleController.Instance.layerMask.value);
            return isFrontHit;
        }
        private void OnDrawGizmos()
        {
            if (isFrontHit)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(frontSensorTransform.position, hitInfo.point);
            }
            else
            {
                // �������û�л������壬�����ߵ�ĩ��λ�û���Ϊ��ɫ�� Gizmos ����
                Gizmos.color = Color.green;
                Gizmos.DrawLine(frontSensorTransform.position, frontSensorTransform.position + transform.forward * frontSensorLength);
            }
        }
        #endregion

        #region waypoint trigger method
        public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            if (onReachWaypointSettings.parentRoute == AIPeopleController.Instance.GetPeopleRoute(assignedIndex))
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
                AITrafficWaypoint newpoint= onReachWaypointSettings.waypoint;
                if (onReachWaypointSettings.newRoutePoints.Length > 0)//������·��
                {
                    randomIndex = Random.Range(0, onReachWaypointSettings.newRoutePoints.Length);
                    newpoint = onReachWaypointSettings.newRoutePoints[randomIndex];
                    AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, newpoint.onReachWaypointSettings.parentRoute);//����·��
                    AIPeopleController.Instance.Set_RouteInfo(assignedIndex, newpoint.onReachWaypointSettings.parentRoute.routeInfo);//����·����Ϣ
                    AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, newpoint.onReachWaypointSettings.waypointIndexnumber - 1);//����·�ߵ�
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                        (
                            assignedIndex,
                            newpoint.onReachWaypointSettings.waypointIndexnumber - 1,
                            newpoint
                        );
                }
                else if (onReachWaypointSettings.waypointIndexnumber < onReachWaypointSettings.parentRoute.waypointDataList.Count)//������·��
                {
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                    (
                         assignedIndex,
                         onReachWaypointSettings.waypointIndexnumber,
                         onReachWaypointSettings.waypoint
                    );
                    newpoint = onReachWaypointSettings.nextPointInRoute;
                }
                //Debug.Log(newpoint);
                //ʹ���˳����µ�λ��
                if(newpoint)
                {
                    Vector3 targetDirection = newpoint.transform.position - transform.position;
                    targetDirection.y = 0;
                    Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    transform.rotation = targetRotation;
                }

                AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);
            }
        }
        #endregion
    }
}
