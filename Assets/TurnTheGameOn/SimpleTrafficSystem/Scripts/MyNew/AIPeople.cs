namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.AI;
    //Rebe：该类主要用于控制行人的行为逻辑
    [RequireComponent(typeof(NavMeshAgent))]
    public class AIPeople : MonoBehaviour
    {
        #region Params
        public int assignedIndex { get; private set; }

        #region 碰撞检测参数
        [Header("Sensor Detector")]
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Transform frontSensorTransform;//前向盒型检测器起点
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Vector3 frontSensorSize = new Vector3(15f, 1f, 0.001f);//前向盒型检测器大小
        [Tooltip("Front Sensor Length")]
        public float frontSensorLength;//前向盒型检测器长度
        //侧向盒型检测器
        public Transform leftSensorTransform;
        public Transform rightSensorTransform;
        public Vector3 sideSensorSize = new Vector3(15f, 1f, 0.001f);
        public float sideSensorLength;

        //脚部碰撞检测主要是为了检测台阶
        [Tooltip("Control point to orient/position the foot detection sensor. ")]
        public Transform footSensorTransform;//脚部碰撞检测器起点
        [Tooltip("Foot Sensor Length")]
        public float footSensorLength;//脚部碰撞检测器长度
        #endregion

        #region 基本参数
        [Header("Base Info")]
        [Tooltip("PlayerHead,需要给模型的脖子关节加一个空的父节点")]
        public Transform playerHead;
        [Tooltip("tick when this is a riding model")]
        public bool isRiding;//非机动车模型需要勾选
        [HideInInspector]
        public Animator animator;
        [HideInInspector]
        public float speed;
        [HideInInspector]
        public float maxSpeed;
        [HideInInspector]
        public Quaternion originHeadRotation;//初始的头部旋转
        #endregion

        #region 测试参数
        [Header("use for test")]//用于测试，正式版可删
        public Transform car;//鸣笛车辆
        public AITrafficWaypoint nextPoint;//下一个行进点
        #endregion

        #region 私有变量
        private bool isFrontHit;
        private bool isFootHit;
        private RaycastHit hitInfo;
        private RaycastHit fHitInfo;
        private int randomIndex;//随机选择下一条路径
        #endregion

        #endregion

        #region Register
        public void RegisterPerson(AITrafficWaypointRoute route)
        {
            //设置正常速度以及最快速度
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
            //获取初始值
            animator = GetComponent<Animator>();
            originHeadRotation = playerHead.rotation;
            assignedIndex = AIPeopleController.Instance.RegisterPeopleAI(this, route);
        }//用于注册行人
        #endregion

        #region public api method
        //使得该行人开始移动
        public void StartMoving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, true);
        }

        //停止移动，使用和车辆一样的方法命名，方便在AITrafficWaypoint.cs进行调用
        public void StopDriving()
        {
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, false);
            AIPeopleController.Instance.Set_IsLastPoint(assignedIndex, true);
        }//当时达到路线结尾处的点时使用这个方法

        //检测前面是否有障碍
        public bool FrontSensorDetecting()
        {
            //前向射线检测
            isFrontHit = Physics.Raycast(frontSensorTransform.position, transform.forward, out hitInfo, frontSensorLength, ~(1<<1));
            
            if (!isFrontHit)
                return false;           
            else
            {
                //如果障碍是车子，并且车子停下来让位，则行人可以继续向前
                if (hitInfo.transform.GetComponent<AITrafficCar>()&&hitInfo.transform.GetComponent<Rigidbody>().velocity == Vector3.zero)
                    return false;
                return true;
            }
        }
        //脚部检测是否碰到台阶
        public bool FootSensorDetecting()
        {
            isFootHit = Physics.Raycast(footSensorTransform.position, transform.forward, out fHitInfo, footSensorLength, AIPeopleController.Instance.footLayerMask.value);
            return isFootHit;
        }
        //换道
        public void ChangeToRouteWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            onReachWaypointSettings.OnReachWaypointEvent.Invoke();

            AIPeopleController.Instance.Set_WaypointDataListCountArray(assignedIndex);
            AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.parentRoute);//更新路线
            AIPeopleController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.parentRoute.routeInfo);//更新路线信息
            AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);//更新路线点
            AIPeopleController.Instance.Set_CurrentRoutePointIndexArray//设置当前路径点
                (
                    assignedIndex,
                    onReachWaypointSettings.waypointIndexnumber - 1,
                    onReachWaypointSettings.waypoint
                );
            if (onReachWaypointSettings.waypoint)
            {
                //更新行人NavigationAgent的AI destination
                AIPeopleController.Instance.Set_AIDestination(assignedIndex, onReachWaypointSettings.waypoint.transform.position);

            }

            AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);//设置路径点位置
        }

        #endregion

        #region Emergencies
        /// <summary>
        /// 车辆鸣笛的时候应该有一个范围检测，检测到的行人调用该方法，并把车的信息传过来
        /// </summary>
        [ContextMenu("Car Horn")]
        public void HeardCarHorn(/*AITrafficCar car*/)//听到汽车鸣笛时调用该方法
        {
            AIPeopleController.Instance.Set_StopForHorn(assignedIndex, true);
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, false);//停止移动
            if(!isRiding)
            {
                //左右张望动画
                playerHead.LookAt(car);//看向车子
                animator.SetBool("HeardHorn", true);//播放看向动画
                //计算方位
                Vector3 carPos = transform.InverseTransformPoint(car.position);//将车子转化为以行人的坐标系                                                              //Debug.Log(carPos.z);
                //设置跑动方向
                if (carPos.z > 0)
                    AIPeopleController.Instance.Set_runDirection(assignedIndex, -1);
                else
                    AIPeopleController.Instance.Set_runDirection(assignedIndex, 1);
            }
            else
            {
                //设置因为鸣笛停止的时间
                AIPeopleController.Instance.Set_stopForHornCoolDownTimer(assignedIndex, AIPeopleController.Instance.waitingTime);
            }

        }
        /// <summary>
        /// 脱离汽车检测时调用该方法
        /// </summary>
        [ContextMenu("After Car Horn")]
        public void AfterCarHorn()
        {
            AIPeopleController.Instance.Set_StopForHorn(assignedIndex, false);
            AIPeopleController.Instance.Set_stopForHornCoolDownTimer(assignedIndex, 0);
            AIPeopleController.Instance.Set_IsWalkingArray(assignedIndex, true);
            //非自行车需要设置动画
            if (!isRiding)
            {
                animator.SetBool("HeardHorn", false);
                animator.SetInteger("RunDirection", 0);
            }               
        }
        /// <summary>
        /// 调用该方法行人将无视红绿灯并且跑着穿马路
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
            if (onReachWaypointSettings.parentRoute == AIPeopleController.Instance.GetPeopleRoute(assignedIndex))//如果是正在走的路径
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                //设置routeProgressNL
                AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
                //设置waypointDataListCountNL
                AIPeopleController.Instance.Set_WaypointDataListCountArray(assignedIndex);
                //获取下一个点
                AITrafficWaypoint newpoint = onReachWaypointSettings.waypoint;
                nextPoint = newpoint;

                //需要更换新路线
                if (onReachWaypointSettings.newRoutePoints.Length > 0)
                {
                    //获取一个随机Index，随机下一条路线
                    randomIndex = Random.Range(0, onReachWaypointSettings.newRoutePoints.Length);
                    //设置newpoint为下一个目标点
                    newpoint = onReachWaypointSettings.newRoutePoints[randomIndex];
                    //更改peopleRouteList
                    AIPeopleController.Instance.Set_WaypointRoute(assignedIndex, newpoint.onReachWaypointSettings.parentRoute);//更新路线
                    //更改peopleAIWaypointRouteInfo
                    AIPeopleController.Instance.Set_RouteInfo(assignedIndex, newpoint.onReachWaypointSettings.parentRoute.routeInfo);//更新路线信息
                    //更新设置routeProgressNL
                    AIPeopleController.Instance.Set_RouteProgressArray(assignedIndex, newpoint.onReachWaypointSettings.waypointIndexnumber - 1);//更新路线点
                    //设置currentRoutePointIndexNL currentWaypointList isChangingLanesNL
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                        (
                            assignedIndex,
                            newpoint.onReachWaypointSettings.waypointIndexnumber - 1,
                            newpoint
                        );
                }
                //没有新路线需要更换
                else if (onReachWaypointSettings.waypointIndexnumber < onReachWaypointSettings.parentRoute.waypointDataList.Count)//不更新路线
                {
                    AIPeopleController.Instance.Set_CurrentRoutePointIndexArray
                    (
                         assignedIndex,
                         onReachWaypointSettings.waypointIndexnumber,
                         onReachWaypointSettings.waypoint
                    );
                    //设置newpoint为下一个目标点
                    newpoint = onReachWaypointSettings.nextPointInRoute;
                }
                //使行人朝向新的位置
                if (newpoint)
                {
                    //更新navigation agent 的目标点
                    AIPeopleController.Instance.Set_AIDestination(assignedIndex, newpoint.transform.position);
                }
                //设置路线position
                AIPeopleController.Instance.Set_RoutePointPositionArray(assignedIndex);
            }
        }
        #endregion

    }
}
