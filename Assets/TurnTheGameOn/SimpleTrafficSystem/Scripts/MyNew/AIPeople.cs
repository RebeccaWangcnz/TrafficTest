namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;
    //Rebe��������Ҫ���ڿ������˵���Ϊ�߼�
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIPeople : MonoBehaviour
    {
        public int assignedIndex { get; private set; }
        //[Tooltip("person's move speed")]
        //public float moveSpeed;
        [Header("Sensor Detector")]
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Transform frontSensorTransform;
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Vector3 frontSensorSize = new Vector3(15f, 1f, 0.001f);
        [Tooltip("Front Sensor Length")]
        public float frontSensorLength;
        public Transform leftSensorTransform;
        public Transform rightSensorTransform;
        public Vector3 sideSensorSize = new Vector3(15f, 1f, 0.001f);
        public float sideSensorLength;
        [Tooltip("Control point to orient/position the foot detection sensor. ")]
        public Transform footSensorTransform;
        [Tooltip("Foot Sensor Length")]
        public float footSensorLength;

        [Header("Base Info")]
        [Tooltip("PlayerHead,��Ҫ��ģ�͵Ĳ��ӹؽڼ�һ���յĸ��ڵ�")]
        public Transform playerHead;
        [Tooltip("tick when this is a riding model")]
        public bool isRiding;

        [Header("use for test")]
        public Transform car;
        public AITrafficWaypoint nextPoint;

        private bool isFrontHit;
        private bool isFootHit;
        private RaycastHit hitInfo;
        private RaycastHit fHitInfo;
        [HideInInspector]
        public Quaternion originHeadRotation;//��ʼ��ͷ����ת
        private int randomIndex;
        [HideInInspector]
        public Animator animator;
        [HideInInspector]
        public float speed;
        [HideInInspector]
        public float maxSpeed;
        public void RegisterPerson(AITrafficWaypointRoute route)
        {
            if(isRiding)
            {
                speed = Random.Range(AIPeopleController.Instance.ridingSpeedRange.x, AIPeopleController.Instance.ridingSpeedRange.y);
                maxSpeed = AIPeopleController.Instance.fastestRidingSpeed;
            }
                
            else
            {
                speed = Random.Range(AIPeopleController.Instance.walkingSpeedRange.x, AIPeopleController.Instance.walkingSpeedRange.y);
                maxSpeed = AIPeopleController.Instance.runningSpeed;
            }                
            animator = GetComponent<Animator>();
            originHeadRotation = playerHead.rotation;
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
        public void StopDriving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, false);
            AIPeopleController.Instance.Set_IsLastPoint(assignedIndex, true);
        }//��ʱ�ﵽ·�߽�β���ĵ�ʱʹ���������
        
        public bool FrontSensorDetecting()
        {
            isFrontHit = Physics.Raycast(frontSensorTransform.position, transform.forward, out hitInfo, frontSensorLength, ~(1<<1));
            if (!isFrontHit)
                return false;           
            else
            {
                if (hitInfo.transform.GetComponent<AITrafficCar>()&&hitInfo.transform.GetComponent<Rigidbody>().velocity == Vector3.zero)//����ϰ��ǳ��ӣ����ҳ���ͣ������λ�������˿��Լ�����ǰ
                    return false;
                return true;
            }

        }//���ǰ���Ƿ����ϰ�
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

            AIPeopleController.Instance.Set_WaypointDataListCountArray(assignedIndex);
            AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.parentRoute);//����·��
            AIPeopleController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.parentRoute.routeInfo);//����·����Ϣ
            AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);//����·�ߵ�
            AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                (
                    assignedIndex,
                    onReachWaypointSettings.waypointIndexnumber - 1,
                    onReachWaypointSettings.waypoint
                );
            if (onReachWaypointSettings.waypoint)
            {
                //Vector3 targetDirection = onReachWaypointSettings.waypoint.transform.position - transform.position;
                //targetDirection.y = 0;
                //Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                //AIPeopleController.Instance.Set_TargetRotation(assignedIndex, targetRotation);
                AIPeopleController.Instance.Set_AIDestination(assignedIndex, onReachWaypointSettings.waypoint.transform.position);

            }

            AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);
        }

        #endregion

        #region Emergencies
        /// <summary>
        /// �������ѵ�ʱ��Ӧ����һ����Χ��⣬��⵽�����˵��ø÷��������ѳ�����Ϣ������
        /// </summary>
        [ContextMenu("Car Horn")]
        public void HeardCarHorn(/*AITrafficCar car*/)
        {
            AIPeopleController.Instance.Set_StopForHorn(assignedIndex, true);
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, false);//ֹͣ�ƶ�
            if(!isRiding)
            {
                //������������
                playerHead.LookAt(car);//������
                animator.SetBool("HeardHorn", true);//���ſ��򶯻�
                                                    //���㷽λ
                Vector3 carPos = transform.InverseTransformPoint(car.position);//������ת��Ϊ�����˵�����ϵ                                                              //Debug.Log(carPos.z);
                if (carPos.z > 0)
                    AIPeopleController.Instance.Set_runDirection(assignedIndex, -1);
                else
                    AIPeopleController.Instance.Set_runDirection(assignedIndex, 1);
            }
            else
            {
                AIPeopleController.Instance.Set_stopForHornCoolDownTimer(assignedIndex, AIPeopleController.Instance.waitingTime);
            }

        }//������������ʱ���ø÷���
        /// <summary>
        /// �����������ʱ���ø÷���
        /// </summary>
        [ContextMenu("After Car Horn")]
        public void AfterCarHorn()
        {
            AIPeopleController.Instance.Set_StopForHorn(assignedIndex, false);
            AIPeopleController.Instance.Set_stopForHornCoolDownTimer(assignedIndex, 0);
            //if (AIPeopleController.Instance.Get_runDirection(assignedIndex)==1)
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, true);
            if (!isRiding)
            {
                animator.SetBool("HeardHorn", false);
                animator.SetInteger("RunDirection", 0);
            }               
        }
        /// <summary>
        /// ���ø÷������˽����Ӻ��̵Ʋ������Ŵ���·
        /// </summary>
        [ContextMenu("Cross the road")]
        public void CrossRoad()
        {
            AIPeopleController.Instance.Set_CrossRoad(assignedIndex, true);
        }
        #endregion

        #region Waypoint Trigger Method
        public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            if (onReachWaypointSettings.parentRoute == AIPeopleController.Instance.GetPeopleRoute(assignedIndex))
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
                AIPeopleController.Instance.Set_WaypointDataListCountArray(assignedIndex);
                AITrafficWaypoint newpoint = onReachWaypointSettings.waypoint;
                nextPoint = newpoint;
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
                //ʹ���˳����µ�λ��
                if (newpoint)
                {
                    //Vector3 targetDirection = newpoint.transform.position - transform.position;
                    //targetDirection.y = 0;
                    //Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                    //AIPeopleController.Instance.Set_TargetRotation(assignedIndex, targetRotation);
                    AIPeopleController.Instance.Set_AIDestination(assignedIndex, newpoint.transform.position);
                }

                AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);
            }
        }
        #endregion

        //#region Gizmo
        //private void OnDrawGizmos()
        //{
        //    if (isFrontHit)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawLine(frontSensorTransform.position, hitInfo.point);
        //    }
        //    else
        //    {
        //        // �������û�л������壬�����ߵ�ĩ��λ�û���Ϊ��ɫ�� Gizmos ����
        //        Gizmos.color = Color.green;
        //        Gizmos.DrawLine(frontSensorTransform.position, frontSensorTransform.position + transform.forward * frontSensorLength);
        //    }
        //    if (isFootHit)
        //    {
        //        Gizmos.color = Color.red;
        //        Gizmos.DrawLine(footSensorTransform.position, fHitInfo.point);
        //    }
        //    else
        //    {
        //        // �������û�л������壬�����ߵ�ĩ��λ�û���Ϊ��ɫ�� Gizmos ����
        //        Gizmos.color = Color.green;
        //        Gizmos.DrawLine(footSensorTransform.position, footSensorTransform.position + transform.forward * footSensorLength);
        //    }
        //}
        //#endregion
    }
}
