namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Jobs;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Jobs;
    using UnityEngine.AI;
    //与AITrafficController类似用于控制行人
    public class AIPeopleController : MonoBehaviour
    {
        public static AIPeopleController Instance;//实例化，方便获取
        #region Params
        private bool isInitialized;//是否被初始化

        #region 速度设置
        [Tooltip("the speed for people running")]
        public float runningSpeed;
        [Tooltip("the walking speed range for people")]
        public Vector2 walkingSpeedRange;
        [Tooltip("the riding speed range for bycicle")]
        public Vector2 ridingSpeedRange;
        [Tooltip("the fastest riding speed range for bycicle")]
        public float fastestRidingSpeed;
        #endregion

        #region 射线检测层级
        [Tooltip("Physics layers the detection sensors can detect.")]
        public LayerMask layerMask;
        [Tooltip("Physics layers the foot detection sensors can detect.")]
        public LayerMask footLayerMask;
        #endregion

        #region 行人路线设置
        [Tooltip("Enables the processing of Lane Changing logic.")]
        public bool useLaneChanging;
        [Tooltip("Minimum time required after changing lanes before allowed to change lanes again.")]
        public float changeLaneCooldown = 20f;

        //pool :this params is related to AITrafficController
        [Tooltip("Array of AITrafficCar prefabs to spawn.")]
        public AIPeople[] peoplePrefabs;
        [Tooltip("Max amount of people the pooling system is allowed to spawn, must be equal or lower than people in pool.")]
        public int density = 200;
        [Tooltip("Max amount of people placed in the pooling system on scene start.")]
        public int peopleInPool = 200;
        [Tooltip("The position that people are sent to when being disabled.")]
        public Vector3 disabledPosition = new Vector3(0, -2000, 0);
        [Tooltip("Frequency at which pooling spawn is performed.")]
        public float spawnRate = 2;
        [Tooltip("waiting time before walk back when stop for the car horn")]
        public float waitingTime;//用于汽车鸣笛特殊事件

        public int peopleCount { get; private set; }//行人数量
        public int currentDensity { get; private set; }//当前密度
        #endregion

        #region 私有变量
        //job
        private AIPeopleDistanceJob _AIPeopleDistanceJob;//Job
        private AIPeopleJob peopleAITrafficJob;
        private JobHandle jobHandle;
        private AIPeoplePoolEntry newPeoplePoolEntry = new AIPeoplePoolEntry();
        //spawn
        private float3 centerPosition;//交通流中心点，与AITrafficController一致
        private float spawnZone;//生成区域
        private bool usePool;//是否使用pool，与AITraffciController一致
        private float spawnTimer;//生成倒计时
        private int randomSpawnPointIndex;//随机生成点Index
        private Vector3 spawnPosition;//生成位置
        private Vector3 spawnOffset = new Vector3(0, -4, 0);//生成偏移，一般就是半个人高
        private float distanceToSpawnPoint;
        private int currentAmountToSpawn;//待生成数量
        private AIPeople spawnpeople;//生成人
        private AIPeople loadPeople;
        //List
        private List<AIPeople> peopleList = new List<AIPeople>();//行人AIPeople列表
        private List<AITrafficWaypointRoute> peopleRouteList = new List<AITrafficWaypointRoute>();//行人路线列表
        private List<Rigidbody> rigidbodyList = new List<Rigidbody>();//刚体列表
        private List<NavMeshAgent> agents = new List<NavMeshAgent>();//AI agent列表
        private List<AITrafficWaypointRouteInfo> peopleAIWaypointRouteInfo = new List<AITrafficWaypointRouteInfo>();//路线info列表
        private List<float> changeLaneCooldownTimer = new List<float>();//换道冷却倒计时
        private List<float> stopForHornCooldownTimer = new List<float>();//鸣笛停止倒计时
        private List<AITrafficWaypoint> currentWaypointList = new List<AITrafficWaypoint>();//当前路线点列表
        private List<Vector3> targetsList = new List<Vector3>();//目标点列表
        private List<bool> runForTrafficLightNL = new List<bool>();//是否需要根据信号灯跑步，这个变量主要是用来控制行人在路中央信号灯变红或者变黄加速通过
        private List<AITrafficSpawnPoint> peopleSpawnPoint = new List<AITrafficSpawnPoint>();
        private List<AITrafficSpawnPoint> availableSpawnPoints = new List<AITrafficSpawnPoint>();
        private List<AIPeoplePoolEntry> peoplePool = new List<AIPeoplePoolEntry>();

        #endregion

        #region NativeList
        //以下list均是保存对应index的行人的路线信息
        private NativeList<bool> isWalkingNL;//是否行走

        private NativeList<int> waypointDataListCountNL;//路径上的路径点数量
        private NativeList<float3> routePointPositionNL;//路径点位置
        private NativeList<int> currentRoutePointIndexNL;//当前路径点索引
        private NativeList<float3> finalRoutePointPositionNL;//路径最后一个路径点位置
        private NativeList<bool> stopForTrafficLightNL;//是否需要根据信号灯停车

        private NativeList<float> routeProgressNL;//道路进程
        private NativeList<bool> isFrontHitNL;//前方是否有障碍
        private NativeList<bool> isLeftHitNL;//前方是否有障碍
        private NativeList<bool> isRighttHitNL;//前方是否有障碍
        private NativeList<bool> isLastPointNL;//是否是最后一个点
        private NativeList<bool> isFootHitNL;//脚部是否检测到台阶
        //转向
        private NativeList<Quaternion> targetRotationNL;
        //变道
        private NativeList<bool> canChangeLanesNL;
        private NativeList<bool> isChangingLanesNL;
        private NativeList<bool> needChangeLanesNL;
        //pool
        private NativeList<bool> isDisabledNL;//是否是未激活状态
        private NativeList<bool> isActiveNL;
        private NativeList<bool> outOfBoundsNL;//是否在边界外       
        private NativeList<float> distanceToPlayerNL;//与player距离
        //特殊事件
        private NativeList<bool> stopForHornNL;//停止鸣笛
        private NativeList<int> runDirectionNL;//避开方向
        private NativeList<bool> crossRoadNL;//是否要强硬过马路
        private Vector3 direction;//避开方向
        //ray cast
        private NativeArray<RaycastCommand> frontRaycastCommands;
        private NativeArray<RaycastHit> frontRaycastResults;
        private NativeArray<BoxcastCommand> leftBoxcastCommands;
        private NativeArray<RaycastHit> leftBoxcastResults;
        private NativeArray<BoxcastCommand> rightBoxcastCommands;
        private NativeArray<RaycastHit> rightBoxcastResults;
        private NativeArray<RaycastCommand> footRaycastCommands;
        private NativeArray<RaycastHit> footRaycastResults;
        //Gizmos
        private Vector3 gizmoOffset;
        private Matrix4x4 cubeTransform;
        private Matrix4x4 oldGizmosMatrix;
        //TAA
        private TransformAccessArray moveTargetTAA;
        private TransformAccessArray peopleTAA;
        #endregion
        #endregion

        #region Main Methods
        private void OnEnable()
        {
            //初始化 分配空间
            if (Instance == null)
            {
                Instance = this;
                isWalkingNL = new NativeList<bool>(Allocator.Persistent);
                routePointPositionNL = new NativeList<float3>(Allocator.Persistent);
                currentRoutePointIndexNL = new NativeList<int>(Allocator.Persistent);
                finalRoutePointPositionNL = new NativeList<float3>(Allocator.Persistent);
                waypointDataListCountNL = new NativeList<int>(Allocator.Persistent);
                routeProgressNL = new NativeList<float>(Allocator.Persistent);
                stopForTrafficLightNL = new NativeList<bool>(Allocator.Persistent);
                isFrontHitNL = new NativeList<bool>(Allocator.Persistent);
                isLeftHitNL = new NativeList<bool>(Allocator.Persistent);
                isRighttHitNL = new NativeList<bool>(Allocator.Persistent);
                isLastPointNL = new NativeList<bool>(Allocator.Persistent);
                isFootHitNL = new NativeList<bool>(Allocator.Persistent);
                targetRotationNL = new NativeList<Quaternion>(Allocator.Persistent);
                canChangeLanesNL = new NativeList<bool>(Allocator.Persistent);
                isChangingLanesNL = new NativeList<bool>(Allocator.Persistent);
                needChangeLanesNL = new NativeList<bool>(Allocator.Persistent);
                isDisabledNL = new NativeList<bool>(Allocator.Persistent);
                isActiveNL = new NativeList<bool>(Allocator.Persistent);
                outOfBoundsNL = new NativeList<bool>(Allocator.Persistent);
                stopForHornNL = new NativeList<bool>(Allocator.Persistent);
                crossRoadNL = new NativeList<bool>(Allocator.Persistent);
                distanceToPlayerNL = new NativeList<float>(Allocator.Persistent);
                runDirectionNL = new NativeList<int>(Allocator.Persistent);
            }
            else
            {
                Debug.LogWarning("Multiple AIPeopleController Instances found in scene, this is not allowed. Destroying this duplicate AITrafficController.");
                Destroy(this);
            }
        }
        private void Start()
        {
            //获取AITrafficController中pooling信息
            spawnZone = AITrafficController.Instance.spawnZone;
            usePool = AITrafficController.Instance.usePooling;
            if (usePool)
            {
                StartCoroutine(SpawnStartupTrafficCoroutine());
            }
            else
                StartCoroutine(Initialize());
        }
        //不适用pooling时的初始化
        IEnumerator Initialize()
        {
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < peopleCount; i++)
            {
                routePointPositionNL[i] = peopleRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;//出生点位置
                finalRoutePointPositionNL[i] = peopleRouteList[i].waypointDataList[peopleRouteList[i].waypointDataList.Count - 1]._transform.position;//路径终点位置
                //peopleList[i].StartMoving();
            }
            isInitialized = true;
        }
        //使用pooling时的初始化
        IEnumerator SpawnStartupTrafficCoroutine()
        {
            yield return new WaitForEndOfFrame();
            availableSpawnPoints.Clear();
            currentDensity = 0;
            currentAmountToSpawn = density - currentDensity;

            //获取所有的生成点
            for (int i = 0; i < peopleSpawnPoint.Count; i++) 
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, peopleSpawnPoint[i].transformCached.position);//车辆生成位置与中心点位置的距离
                if (peopleSpawnPoint[i].isTrigger == false)//当前点没有被占据
                {
                    availableSpawnPoints.Add(peopleSpawnPoint[i]);//添加到可以生成的路径点列表中
                }
            }

            //生成行人
            for (int i = 0; i < density; i++)
            {
                for (int j = 0; j < peoplePrefabs.Length; j++)
                {
                    if (availableSpawnPoints.Count == 0 || currentAmountToSpawn == 0) break;//不需要生成则结束循环

                    //获取随机生成点index
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    //生成位置
                    spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                    
                    //生成
                    GameObject spawnedTrafficVehicle = Instantiate(peoplePrefabs[j].gameObject, spawnPosition, availableSpawnPoints[randomSpawnPointIndex].transformCached.rotation);
                    spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);
                    spawnedTrafficVehicle.transform.LookAt(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                    availableSpawnPoints.RemoveAt(randomSpawnPointIndex);
                    currentAmountToSpawn -= 1;
                }
            }

            //生成pool里的行人
            for (int i = 0; i < peopleInPool; i++)
            {
                //需求人数超过pool人数，不需要在pool中生成行人
                if (peopleCount >= peopleInPool) break;
                for (int j = 0; j < peoplePrefabs.Length; j++)
                {
                    if (peopleCount >= peopleInPool) break;
                    GameObject spawnedTrafficVehicle = Instantiate(peoplePrefabs[j].gameObject, Vector3.zero, Quaternion.identity);
                    spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(peopleRouteList[0]);
                    //将生成的 行人移到pool中
                    MovePeopleToPool(spawnedTrafficVehicle.GetComponent<AIPeople>().assignedIndex);
                }
            }
            for (int i = 0; i < peopleCount; i++)
            {
                //设置当前位置和终点位置
                routePointPositionNL[i] = peopleRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;
                finalRoutePointPositionNL[i] = peopleRouteList[i].waypointDataList[peopleRouteList[i].waypointDataList.Count - 1]._transform.position;
            }
            for (int i = 0; i < peopleCount; i++)
            {
                //设置父物体
                peopleList[i].transform.SetParent(transform);
            }
            isInitialized = true;
        }
        private void FixedUpdate()
        {
            if (isInitialized)
            {
                for (int i = 0; i < peopleCount; i++)
                {
                    stopForTrafficLightNL[i] = peopleAIWaypointRouteInfo[i].stopForTrafficLight;
                    runForTrafficLightNL[i] = peopleAIWaypointRouteInfo[i].runForTrafficLight;
                    //射线检测
                    frontRaycastCommands[i] = new RaycastCommand(peopleList[i].frontSensorTransform.position,peopleList[i].transform.forward, peopleList[i].frontSensorLength,layerMask);
                    leftBoxcastCommands[i] = new BoxcastCommand(peopleList[i].leftSensorTransform.position,peopleList[i].sideSensorSize, peopleList[i].transform.rotation,peopleList[i].transform.forward, peopleList[i].sideSensorLength, layerMask);
                    rightBoxcastCommands[i] = new BoxcastCommand(peopleList[i].rightSensorTransform.position,peopleList[i].sideSensorSize, peopleList[i].transform.rotation,peopleList[i].transform.forward, peopleList[i].sideSensorLength, layerMask);
                    footRaycastCommands[i] = new RaycastCommand(peopleList[i].footSensorTransform.position, peopleList[i].transform.forward, peopleList[i].footSensorLength, footLayerMask);

                    //设置速度和动画对应
                    peopleList[i].animator.SetFloat("speedWithoutBT",agents[i].velocity.magnitude);
                    //根据速度设置避让等级 0808改了一下优先级计算方法，行人和非机动车除数应该一样的
                    agents[i].avoidancePriority = (int)(100*agents[i].speed/ fastestRidingSpeed);
                }
                //进行射线检测批处理
                var handle = RaycastCommand.ScheduleBatch(frontRaycastCommands, frontRaycastResults, 1, default);
                handle.Complete();
                handle = BoxcastCommand.ScheduleBatch(leftBoxcastCommands, leftBoxcastResults, 1, default);
                handle.Complete();
                handle = BoxcastCommand.ScheduleBatch(rightBoxcastCommands, rightBoxcastResults, 1, default);
                handle.Complete();
                handle = RaycastCommand.ScheduleBatch(footRaycastCommands, footRaycastResults, 1, default);
                handle.Complete();
                //根据碰撞结果设置布尔值
                for (int i = 0; i < peopleCount; i++)
                {
                    isRighttHitNL[i] = rightBoxcastResults[i].collider == null ? false : true;
                    isLeftHitNL[i] = leftBoxcastResults[i].collider == null ? false : true;
                    if (!frontRaycastResults[i].collider)
                    {
                        isFrontHitNL[i] = false;
                    }
                    else
                    {
                        AIPeople hitPeople = frontRaycastResults[i].collider.GetComponent<AIPeople>();
                        AITrafficCar hitCar = frontRaycastResults[i].collider.GetComponent<AITrafficCar>();
                        if (hitPeople && Vector3.Dot(hitPeople.transform.forward, peopleList[i].transform.forward) < 0.5f)
                        {//如果两个人迎面相撞 要绕道
                            isFrontHitNL[i] = false;
                        }
                        else if(hitPeople && agents[i].avoidancePriority> agents[hitPeople.assignedIndex].avoidancePriority && agents[hitPeople.assignedIndex].avoidancePriority!=0)
                        {
                            isFrontHitNL[i] = false;
                        }
                        //else if(!frontRaycastResults[i].collider.GetComponent<AITrafficCar>()&&(!isLeftHitNL[i]||!isRighttHitNL[i]))
                        //{//如果两侧没有撞到东西
                        //    Vector3 pointPos = peopleList[i].transform.InverseTransformPoint(targetsList[i]);
                        //    if((pointPos.x>=0&&!isRighttHitNL[i])|| (pointPos.x<= 0 && !isLeftHitNL[i]))
                        //        isFrontHitNL[i] = false;
                        //}
                        else if(hitCar && hitCar.IsBraking())
                        {//前面碰到的是车，且车是停止的，则行人绕行
                            isFrontHitNL[i] = false;
                        }
                        else
                            isFrontHitNL[i] = true;
                    }
                    isFootHitNL[i] = footRaycastResults[i].collider == null ? false : true;
                }

                //AIPeopleJob
                peopleAITrafficJob = new AIPeopleJob
                {
                    //set NA=NL
                    isWalkingNA = isWalkingNL,
                    currentRoutePointIndexNA = currentRoutePointIndexNL,
                    waypointDataListCountNA = waypointDataListCountNL,
                    routeProgressNA = routeProgressNL,
                    stopForTrafficLightNA = stopForTrafficLightNL,
                    isFrontHitNA = isFrontHitNL,
                    isLefttHitNA = isLeftHitNL,
                    isRightHitNA = isRighttHitNL,
                    isLastPointNA = isLastPointNL,
                    isFootHitNA = isFootHitNL,
                    targetRotationNA = targetRotationNL,
                    needChangeLanesNA = needChangeLanesNL,
                    useLaneChanging = useLaneChanging,
                    stopForHornNA = stopForHornNL,
                    runDirectionNA = runDirectionNL,
                    crossRoadNA = crossRoadNL,
                    deltaTime = Time.deltaTime
                };
                //开始job
                jobHandle = peopleAITrafficJob.Schedule(peopleTAA);
                jobHandle.Complete();

                // operate on results
                for (int i = 0; i < peopleCount; i++) 
                {
                    //变道
                    if (needChangeLanesNL[i])
                    {
                        if (!currentWaypointList[i] || currentWaypointList[i].onReachWaypointSettings.laneChangePoints.Count == 0)//没有道路可以变
                        {
                            isWalkingNL[i] = false;
                        }
                        else if (!canChangeLanesNL[i] && !isChangingLanesNL[i])//处于变道冷却时间，行人保持不动
                        {
                            changeLaneCooldownTimer[i] += Time.deltaTime;
                            if (changeLaneCooldownTimer[i] > changeLaneCooldown)
                            {
                                canChangeLanesNL[i] = true;
                                changeLaneCooldownTimer[i] = 0f;
                            }
                            isWalkingNL[i] = false;
                        }
                        else if (!isChangingLanesNL[i])//不在变道，此时可以变道
                        {
                            isChangingLanesNL[i] = true;
                            canChangeLanesNL[i] = false;
                            peopleList[i].ChangeToRouteWaypoint(currentWaypointList[i].onReachWaypointSettings.laneChangePoints[0].onReachWaypointSettings);
                        }
                    }

                    //强制过马路的特殊事件
                    if (crossRoadNL[i])
                        agents[i].speed = peopleList[i].maxSpeed;
                    //正常情况
                    else
                    {
                        if(isWalkingNL[i])//行走状态
                        {
                            if (!runForTrafficLightNL[i])//没有走到路中间变红灯
                                agents[i].speed = peopleList[i].speed;
                            else//走到路中央变红，则改为最大速度行走
                                agents[i].speed = peopleList[i].maxSpeed;
                        }
                        else//停止状态
                        {
                            agents[i].speed = 0;
                            if (stopForHornNL[i])//如果属于被车笛声干扰
                            {                                
                                //一段时间后后退或前进
                                if (stopForHornCooldownTimer[i] < waitingTime)
                                {
                                    stopForHornCooldownTimer[i] += Time.deltaTime;
                                    direction = agents[i].transform.forward;
                                }
                                else
                                {
                                    //头部旋转
                                    peopleList[i].playerHead.LookAt(new Vector3( peopleList[i].frontSensorTransform.position.x, peopleList[i].playerHead.position.y, peopleList[i].frontSensorTransform.position.z));
                                    //设置躲避方向
                                   if (runDirectionNL[i]==1)
                                   {
                                        agents[i].speed = peopleList[i].maxSpeed;
                                    }
                                    else
                                    {
                                        agents[i].velocity = -direction * peopleList[i].maxSpeed;                                        
                                    }
                                    peopleList[i].animator.SetInteger("RunDirection", 1);
                                    //到达台阶上
                                    if (isFootHitNL[i])
                                    {
                                        peopleList[i].AfterCarHorn();
                                    }
                                }
                            }
                        }
                    }

                }

                if (usePool)
                {
                    centerPosition = AITrafficController.Instance.centerPoint.position;
                    //AIPeopleDistanceJob
                    _AIPeopleDistanceJob = new AIPeopleDistanceJob
                    {
                        isDisabledNA = isDisabledNL,
                        outOfBoundsNA = outOfBoundsNL,
                        playerPosition = centerPosition,
                        spawnZone = spawnZone,
                        distanceToPlayerNA = distanceToPlayerNL
                    };
                    jobHandle = _AIPeopleDistanceJob.Schedule(peopleTAA);
                    jobHandle.Complete();
                    //根据结果刷新密度
                    for (int i = 0; i < peopleCount; i++)
                    {
                        if (isDisabledNL[i] == false)
                        {
                            peopleRouteList[i].currentDensity += 1;
                            if (outOfBoundsNL[i])//在pool边界外
                            {
                                MovePeopleToPool(peopleList[i].assignedIndex);
                            }
                        }
                    }
                    if (spawnTimer >= spawnRate) SpawnPeople();
                    else spawnTimer += Time.deltaTime;
                }
            }
        }
        private void OnDestroy()
        {
            DisposeArrays(true);
        }
        void DisposeArrays(bool _isQuit)
        {
            if (_isQuit)
            {
                isWalkingNL.Dispose();
                routePointPositionNL.Dispose();
                currentRoutePointIndexNL.Dispose();
                finalRoutePointPositionNL.Dispose();
                waypointDataListCountNL.Dispose();
                routeProgressNL.Dispose();
                stopForTrafficLightNL.Dispose();
                isFrontHitNL.Dispose();
                isLeftHitNL.Dispose();
                isRighttHitNL.Dispose();
                isLastPointNL.Dispose();
                isFootHitNL.Dispose();
                targetRotationNL.Dispose();
                isChangingLanesNL.Dispose();
                canChangeLanesNL.Dispose();
                needChangeLanesNL.Dispose();
                isDisabledNL.Dispose();
                isActiveNL.Dispose();
                outOfBoundsNL.Dispose();
                distanceToPlayerNL.Dispose();
                stopForHornNL.Dispose();
                runDirectionNL.Dispose();
                crossRoadNL.Dispose();              
            }
            moveTargetTAA.Dispose();
            peopleTAA.Dispose();
            frontRaycastResults.Dispose();
            frontRaycastCommands.Dispose();
            leftBoxcastCommands.Dispose();
            leftBoxcastResults.Dispose();
            rightBoxcastCommands.Dispose();
            rightBoxcastResults.Dispose();
            footRaycastResults.Dispose();
            footRaycastCommands.Dispose();
        }
        #endregion

        #region Register
        public int RegisterPeopleAI(AIPeople peopleAI, AITrafficWaypointRoute route)
        {
            peopleList.Add(peopleAI);
            peopleRouteList.Add(route);
            Rigidbody rigidbody = peopleAI.GetComponent<Rigidbody>();
            rigidbodyList.Add(rigidbody);
            agents.Add(peopleAI.GetComponent<NavMeshAgent>());

            Transform moveTarget = new GameObject("MoveTarget").transform;//移动目标
            moveTarget.SetParent(peopleAI.transform);
            TransformAccessArray temp_moveTargetTAA = new TransformAccessArray(peopleCount);
            for (int i = 0; i < peopleCount; i++)
            {
                temp_moveTargetTAA.Add(moveTargetTAA[i]);
            }
            temp_moveTargetTAA.Add(moveTarget);

            peopleCount = peopleList.Count;
            if (peopleCount >= 2)
            {
                DisposeArrays(false);
            }

            #region allocation
            isWalkingNL.Add(true);
            routePointPositionNL.Add(float3.zero);
            currentRoutePointIndexNL.Add(0);
            finalRoutePointPositionNL.Add(float3.zero);
            waypointDataListCountNL.Add(0);
            peopleAIWaypointRouteInfo.Add(null);
            routeProgressNL.Add(0);
            stopForTrafficLightNL.Add(false);
            isFrontHitNL.Add(false);
            isRighttHitNL.Add(false);
            isLeftHitNL.Add(false);
            isLastPointNL.Add(false);
            isFootHitNL.Add(false);
            targetRotationNL.Add(Quaternion.identity);
            moveTargetTAA = new TransformAccessArray(peopleCount);
            peopleTAA = new TransformAccessArray(peopleCount);
            canChangeLanesNL.Add(true);
            changeLaneCooldownTimer.Add(0);
            stopForHornCooldownTimer.Add(0);
            isChangingLanesNL.Add(false);
            needChangeLanesNL.Add(false);
            currentWaypointList.Add(null);
            isDisabledNL.Add(false);
            isActiveNL.Add(true);
            outOfBoundsNL.Add(false);
            distanceToPlayerNL.Add(0);
            stopForHornNL.Add(false);
            runDirectionNL.Add(1);
            crossRoadNL.Add(false);
            runForTrafficLightNL.Add(false);
            frontRaycastResults = new NativeArray<RaycastHit>(peopleCount, Allocator.Persistent);
            frontRaycastCommands = new NativeArray<RaycastCommand>(peopleCount, Allocator.Persistent);
            leftBoxcastCommands = new NativeArray<BoxcastCommand>(peopleCount, Allocator.Persistent);
            leftBoxcastResults = new NativeArray<RaycastHit>(peopleCount, Allocator.Persistent);
            rightBoxcastCommands = new NativeArray<BoxcastCommand>(peopleCount, Allocator.Persistent);
            rightBoxcastResults = new NativeArray<RaycastHit>(peopleCount, Allocator.Persistent);
            footRaycastResults = new NativeArray<RaycastHit>(peopleCount, Allocator.Persistent);
            footRaycastCommands = new NativeArray<RaycastCommand>(peopleCount, Allocator.Persistent);
            targetsList.Add(Vector3.zero);
            #endregion

            waypointDataListCountNL[peopleCount - 1] = peopleRouteList[peopleCount - 1].waypointDataList.Count;//第i个人所在route一共有几个点
            peopleAIWaypointRouteInfo[peopleCount - 1] = peopleRouteList[peopleCount - 1].routeInfo;
            for (int i = 0; i < peopleCount; i++)
            {
                moveTargetTAA.Add(temp_moveTargetTAA[i]);
                peopleTAA.Add(peopleList[i].transform);
            }
            temp_moveTargetTAA.Dispose();
            return peopleCount - 1;
        }
        #endregion

        #region set array data
        public void Set_IsWalkingArray(int _index, bool _value)
        {
            if (isWalkingNL[_index] != _value)
            {
                isWalkingNL[_index] = _value;
                if (_value == false)
                {
                    //rigidbodyList[_index].velocity = Vector3.zero;
                    agents[_index].speed = 0;
                }
            }
        }
        public void Set_IsLastPoint(int _index, bool _value)
        {
            isLastPointNL[_index] = _value;
        }
        public void Set_CurrentRoutePointIndexArray(int _index, int _value, AITrafficWaypoint _nextWaypoint)
        {
            currentRoutePointIndexNL[_index] = _value;
            currentWaypointList[_index] = _nextWaypoint;
            isChangingLanesNL[_index] = false;
        }
        public void Set_TargetRotation(int _index, Quaternion _value)
        {
            targetRotationNL[_index] = _value;
        }
        public void Set_RouteProgressArray(int _index, float _value)
        {
            routeProgressNL[_index] = _value;
        }
        public void Set_WaypointDataListCountArray(int _index)
        {
            waypointDataListCountNL[_index] = peopleRouteList[_index].waypointDataList.Count;
        }

        public void Set_WaypointRoute(int _index, AITrafficWaypointRoute _route)
        {
            peopleRouteList[_index] = _route;
        }
        public void Set_RouteInfo(int _index, AITrafficWaypointRouteInfo routeInfo)
        {
            peopleAIWaypointRouteInfo[_index] = routeInfo;
        }
        public void Set_RoutePointPositionArray(int _index)
        {
            routePointPositionNL[_index] = peopleRouteList[_index].waypointDataList[currentRoutePointIndexNL[_index]]._transform.position;
            finalRoutePointPositionNL[_index] = peopleRouteList[_index].waypointDataList[peopleRouteList[_index].waypointDataList.Count - 1]._transform.position;
        }
        public void Set_StopForHorn(int _index, bool _value)
        {
            stopForHornNL[_index] = _value;
        }
        public void Set_runDirection(int _index, int _value)
        {
            runDirectionNL[_index] = _value;
        }
        public int Get_runDirection(int _index)
        {
            return runDirectionNL[_index];
        }
        public void Set_stopForHornCoolDownTimer(int _index, float _value)
        {
            stopForHornCooldownTimer[_index] = _value;
        }
        public void Set_CrossRoad(int _index, bool _value)
        {
            crossRoadNL[_index] = _value;
        }
        public AITrafficWaypointRoute GetPeopleRoute(int _index)
        {
            return peopleRouteList[_index];
        }//获取人所在的路径
        public bool GetChangeLaneInfo(int _index)
        {
            return isChangingLanesNL[_index];
        }//获取变道信息
        public void Set_AIDestination(int _index,Vector3 _destination)
        {
            agents[_index].SetDestination(_destination);
            targetsList[_index] = _destination;
        }
        public Vector3 Get_AIDestination(int _index)
        {
            return targetsList[_index];
        }
        public Vector3 Get_trueDestination(int _index)
        {
            return agents[_index].destination;
        }
        //public void Add_PeopleSpawnPoint(AITrafficWaypoint _point)
        //{
        //    peopleSpawnPoint.Add(_point);
        //}
        #endregion

        #region pool
        //将人移动至pool
        public void MovePeopleToPool(int _index)
        {
            canChangeLanesNL[_index] = false;
            isChangingLanesNL[_index] = false;
            isDisabledNL[_index] = true;
            isActiveNL[_index] = false;
            //peopleList[_index].StopDriving();
            isWalkingNL[_index] = false;
            peopleList[_index].transform.position = disabledPosition;
            agents[_index].enabled = false;
            StartCoroutine(MovePeopleToPoolCoroutine(_index));
        }
        IEnumerator MovePeopleToPoolCoroutine(int _index)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            peopleList[_index].gameObject.SetActive(false);
            newPeoplePoolEntry = new AIPeoplePoolEntry();
            newPeoplePoolEntry.assignedIndex = _index;
            newPeoplePoolEntry.peoplePrefab = peopleList[_index];
            peoplePool.Add(newPeoplePoolEntry);//添加到pool列表中
        }
        void SpawnPeople()//生成人物
        {
            spawnTimer = 0f;
            availableSpawnPoints.Clear();
            //获取可能的生成位置
            for (int i = 0; i < peopleSpawnPoint.Count; i++) 
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, peopleSpawnPoint[i].transformCached.position);
                if ((distanceToSpawnPoint > AITrafficController.Instance.actizeZone || (distanceToSpawnPoint > AITrafficController.Instance.minSpawnZone && peopleSpawnPoint[i].isVisible == false))
                    && distanceToSpawnPoint < spawnZone && peopleSpawnPoint[i].isTrigger == false)//在生成zone范围内且不可见且不能是被占据的状态
                {
                    availableSpawnPoints.Add(peopleSpawnPoint[i]);
                }
            }
            
            currentDensity = peopleList.Count - peoplePool.Count;//获取密度
            if (currentDensity < density) //Spawn Traffic
            {
                //得到需要生成的数量
                currentAmountToSpawn = density - currentDensity;
                
                for (int i = 0; i < currentAmountToSpawn; i++)
                {
                    if (availableSpawnPoints.Count == 0 || peoplePool.Count == 0) break;
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    if (availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.currentDensity < availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.maxDensity)
                    {
                        spawnpeople = GetPeopleFromPool(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);//从pool中获取人物进行生成
                        if (spawnpeople != null)
                        {
                            availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.currentDensity += 1;
                            spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                            spawnpeople.transform.SetPositionAndRotation(
                                spawnPosition,
                                availableSpawnPoints[randomSpawnPointIndex].transformCached.rotation
                                );
                            spawnpeople.transform.LookAt(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                            availableSpawnPoints.RemoveAt(randomSpawnPointIndex);
                            spawnpeople.GetComponent<NavMeshAgent>().enabled = true;
                        }
                    }
                }
            }
        }
        public int RegisterSpawnPoint(AITrafficSpawnPoint _TrafficSpawnPoint)
        {
            int index = peopleSpawnPoint.Count;
            peopleSpawnPoint.Add(_TrafficSpawnPoint);
            return index;
        }
        public AIPeople GetPeopleFromPool(AITrafficWaypointRoute parentRoute)
        {
            loadPeople = null;
            for (int i = 0; i < peoplePool.Count; i++)
            {
                for (int j = 0; j < parentRoute.vehicleTypes.Length; j++)
                {
                    loadPeople = peoplePool[i].peoplePrefab;
                    isDisabledNL[peoplePool[i].assignedIndex] = false;
                   // 激活pool中的行人
                    EnablePeople(peopleList[peoplePool[i].assignedIndex].assignedIndex, parentRoute);
                    peoplePool.RemoveAt(i);
                    return loadPeople;
                }
            }
            return loadPeople;
        }
        //行人从pool中激活
        public void EnablePeople(int _index, AITrafficWaypointRoute parentRoute)
        {
            isActiveNL[_index] = true;
            peopleList[_index].gameObject.SetActive(true);
            peopleRouteList[_index] = parentRoute;
            peopleAIWaypointRouteInfo[_index] = parentRoute.routeInfo;
            peopleList[_index].StartMoving();
            needChangeLanesNL[_index] = false;
        }
        #endregion

        #region Gizmo
        private void OnDrawGizmos()
        {
            for(int i=0;i<peopleCount;i++)
            {
                //前向射线检测显示
                if (frontRaycastResults[i].collider)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(peopleList[i].frontSensorTransform.position, frontRaycastResults[i].collider.transform.position);
                }
                else
                {
                    // 如果射线没有击中物体，将射线的末端位置绘制为绿色的 Gizmos 线条
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(peopleList[i].frontSensorTransform.position, peopleList[i].frontSensorTransform.position + peopleList[i].transform.forward * peopleList[i].frontSensorLength);
                }

                //侧向射线检测显示
                Gizmos.color = !isRighttHitNL[i] ? STSPrefs.normalColor : STSPrefs.detectColor;
                gizmoOffset = new Vector3(peopleList[i].sideSensorSize.x * 2.0f, peopleList[i].sideSensorSize.y * 2.0f, peopleList[i].sideSensorLength);
                DrawCube(peopleList[i].leftSensorTransform.position + peopleList[i].transform.forward * (peopleList[i].sideSensorLength / 2), peopleList[i].transform.rotation, gizmoOffset);
                DrawCube(peopleList[i].rightSensorTransform.position + peopleList[i].transform.forward * (peopleList[i].sideSensorLength / 2), peopleList[i].transform.rotation, gizmoOffset);

                //脚部射线检测显示
                if (!isFootHitNL[i])
                {
                    // 如果射线没有击中物体，将射线的末端位置绘制为绿色的 Gizmos 线条
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(peopleList[i].footSensorTransform.position, peopleList[i].footSensorTransform.position + peopleList[i].transform.forward * peopleList[i].footSensorLength);
                }
            }
        }
        void DrawCube(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            cubeTransform = Matrix4x4.TRS(position, rotation, scale);
            oldGizmosMatrix = Gizmos.matrix;
            Gizmos.matrix *= cubeTransform;
            Gizmos.DrawCube(Vector3.zero, Vector3.one);
            Gizmos.matrix = oldGizmosMatrix;
        }
        #endregion

    }
}
