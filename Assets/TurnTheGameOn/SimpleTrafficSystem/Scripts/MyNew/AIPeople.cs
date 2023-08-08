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
        #region Params
        public int assignedIndex { get; private set; }

        #region ��ײ������
        [Header("Sensor Detector")]
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Transform frontSensorTransform;//ǰ����ͼ�������
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Vector3 frontSensorSize = new Vector3(15f, 1f, 0.001f);//ǰ����ͼ������С
        [Tooltip("Front Sensor Length")]
        public float frontSensorLength;//ǰ����ͼ��������
        //������ͼ����
        public Transform leftSensorTransform;
        public Transform rightSensorTransform;
        public Vector3 sideSensorSize = new Vector3(15f, 1f, 0.001f);
        public float sideSensorLength;

        //�Ų���ײ�����Ҫ��Ϊ�˼��̨��
        [Tooltip("Control point to orient/position the foot detection sensor. ")]
        public Transform footSensorTransform;//�Ų���ײ��������
        [Tooltip("Foot Sensor Length")]
        public float footSensorLength;//�Ų���ײ���������
        #endregion

        #region ��������
        [Header("Base Info")]
        [Tooltip("PlayerHead,��Ҫ��ģ�͵Ĳ��ӹؽڼ�һ���յĸ��ڵ�")]
        public Transform playerHead;
        [Tooltip("tick when this is a riding model")]
        public bool isRiding;//�ǻ�����ģ����Ҫ��ѡ
        [HideInInspector]
        public Animator animator;
        [HideInInspector]
        public float speed;
        [HideInInspector]
        public float maxSpeed;
        [HideInInspector]
        public Quaternion originHeadRotation;//��ʼ��ͷ����ת
        #endregion

        #region ���Բ���
        [Header("use for test")]//���ڲ��ԣ���ʽ���ɾ
        public Transform car;//���ѳ���
        public AITrafficWaypoint nextPoint;//��һ���н���
        #endregion

        #region ˽�б���
        private bool isFrontHit;
        private bool isFootHit;
        private RaycastHit hitInfo;
        private RaycastHit fHitInfo;
        private int randomIndex;//���ѡ����һ��·��
        #endregion

        #endregion

        #region Register
        public void RegisterPerson(AITrafficWaypointRoute route)
        {
            //���������ٶ��Լ�����ٶ�
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
            //��ȡ��ʼֵ
            animator = GetComponent<Animator>();
            originHeadRotation = playerHead.rotation;
            assignedIndex = AIPeopleController.Instance.RegisterPeopleAI(this, route);
        }//����ע������
        #endregion

        #region public api method
        //ʹ�ø����˿�ʼ�ƶ�
        public void StartMoving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, true);
        }

        //ֹͣ�ƶ���ʹ�úͳ���һ���ķ���������������AITrafficWaypoint.cs���е���
        public void StopDriving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, false);
            AIPeopleController.Instance.Set_IsLastPoint(assignedIndex, true);
        }//��ʱ�ﵽ·�߽�β���ĵ�ʱʹ���������

        //���ǰ���Ƿ����ϰ�
        public bool FrontSensorDetecting()
        {
            //ǰ�����߼��
            isFrontHit = Physics.Raycast(frontSensorTransform.position, transform.forward, out hitInfo, frontSensorLength, ~(1<<1));
            
            if (!isFrontHit)
                return false;           
            else
            {
                //����ϰ��ǳ��ӣ����ҳ���ͣ������λ�������˿��Լ�����ǰ
                if (hitInfo.transform.GetComponent<AITrafficCar>()&&hitInfo.transform.GetComponent<Rigidbody>().velocity == Vector3.zero)
                    return false;
                return true;
            }
        }
        //�Ų�����Ƿ�����̨��
        public bool FootSensorDetecting()
        {
            isFootHit = Physics.Raycast(footSensorTransform.position, transform.forward, out fHitInfo, footSensorLength, AIPeopleController.Instance.footLayerMask.value);
            return isFootHit;
        }
        //����
        public void ChangeToRouteWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            onReachWaypointSettings.OnReachWaypointEvent.Invoke();

            AIPeopleController.Instance.Set_WaypointDataListCountArray(assignedIndex);
            AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.parentRoute);//����·��
            AIPeopleController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.parentRoute.routeInfo);//����·����Ϣ
            AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);//����·�ߵ�
            AIPeopleController.Instance.Set_CurrentRoutePointIndexArray//���õ�ǰ·����
                (
                    assignedIndex,
                    onReachWaypointSettings.waypointIndexnumber - 1,
                    onReachWaypointSettings.waypoint
                );
            if (onReachWaypointSettings.waypoint)
            {
                //��������NavigationAgent��AI destination
                AIPeopleController.Instance.Set_AIDestination(assignedIndex, onReachWaypointSettings.waypoint.transform.position);

            }

            AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);//����·����λ��
        }

        #endregion

        #region Emergencies
        /// <summary>
        /// �������ѵ�ʱ��Ӧ����һ����Χ��⣬��⵽�����˵��ø÷��������ѳ�����Ϣ������
        /// </summary>
        [ContextMenu("Car Horn")]
        public void HeardCarHorn(/*AITrafficCar car*/)//������������ʱ���ø÷���
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
                //�����ܶ�����
                if (carPos.z > 0)
                    AIPeopleController.Instance.Set_runDirection(assignedIndex, -1);
                else
                    AIPeopleController.Instance.Set_runDirection(assignedIndex, 1);
            }
            else
            {
                //������Ϊ����ֹͣ��ʱ��
                AIPeopleController.Instance.Set_stopForHornCoolDownTimer(assignedIndex, AIPeopleController.Instance.waitingTime);
            }

        }
        /// <summary>
        /// �����������ʱ���ø÷���
        /// </summary>
        [ContextMenu("After Car Horn")]
        public void AfterCarHorn()
        {
            AIPeopleController.Instance.Set_StopForHorn(assignedIndex, false);
            AIPeopleController.Instance.Set_stopForHornCoolDownTimer(assignedIndex, 0);
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, true);
            //�����г���Ҫ���ö���
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
            if (onReachWaypointSettings.parentRoute == AIPeopleController.Instance.GetPeopleRoute(assignedIndex))//����������ߵ�·��
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                //����routeProgressNL
                AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
                //����waypointDataListCountNL
                AIPeopleController.Instance.Set_WaypointDataListCountArray(assignedIndex);
                //��ȡ��һ����
                AITrafficWaypoint newpoint = onReachWaypointSettings.waypoint;
                nextPoint = newpoint;

                //��Ҫ������·��
                if (onReachWaypointSettings.newRoutePoints.Length > 0)
                {
                    //��ȡһ�����Index�������һ��·��
                    randomIndex = Random.Range(0, onReachWaypointSettings.newRoutePoints.Length);
                    //����newpointΪ��һ��Ŀ���
                    newpoint = onReachWaypointSettings.newRoutePoints[randomIndex];
                    //����peopleRouteList
                    AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, newpoint.onReachWaypointSettings.parentRoute);//����·��
                    //����peopleAIWaypointRouteInfo
                    AIPeopleController.Instance.Set_RouteInfo(assignedIndex, newpoint.onReachWaypointSettings.parentRoute.routeInfo);//����·����Ϣ
                    //��������routeProgressNL
                    AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, newpoint.onReachWaypointSettings.waypointIndexnumber - 1);//����·�ߵ�
                    //����currentRoutePointIndexNL currentWaypointList isChangingLanesNL
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                        (
                            assignedIndex,
                            newpoint.onReachWaypointSettings.waypointIndexnumber - 1,
                            newpoint
                        );
                }
                //û����·����Ҫ����
                else if (onReachWaypointSettings.waypointIndexnumber < onReachWaypointSettings.parentRoute.waypointDataList.Count)//������·��
                {
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                    (
                         assignedIndex,
                         onReachWaypointSettings.waypointIndexnumber,
                         onReachWaypointSettings.waypoint
                    );
                    //����newpointΪ��һ��Ŀ���
                    newpoint = onReachWaypointSettings.nextPointInRoute;
                }
                //ʹ���˳����µ�λ��
                if (newpoint)
                {
                    //����navigation agent ��Ŀ���
                    AIPeopleController.Instance.Set_AIDestination(assignedIndex, newpoint.transform.position);
                }
                //����·��position
                AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);
            }
        }
        #endregion

    }
}
