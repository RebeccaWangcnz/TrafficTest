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
        public static AIPeopleController Instance;
        #region Params
        private bool isInitialized;
        [Header("Speed")]
        public float runningSpeed;
        public Vector2 walkingSpeedRange;
        public float accelerateSpeed;
        public Vector2 ridingSpeedRange;
        public float fastestRidingSpeed;
        [Header("Ray Detect")]
        [Tooltip("Physics layers the detection sensors can detect.")]
        public LayerMask layerMask;
        [Tooltip("Physics layers the foot detection sensors can detect.")]
        public LayerMask footLayerMask;
        [Tooltip("Enables the processing of Lane Changing logic.")]
        public bool useLaneChanging;
        [Tooltip("Minimum time required after changing lanes before allowed to change lanes again.")]
        public float changeLaneCooldown = 20f;
        [Tooltip("car width")]
        public float carWidth = 1.5f;

        //pool :this params is related to AITrafficController
        [Header("Pool")]
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

        [Header("Emergencies")]
        [Tooltip("waiting time before walk back when stop for the car horn")]
        public float waitingTime;
        private float3 centerPosition;
        private float spawnZone;
        private bool usePool;
        private AIPeopleDistanceJob _AIPeopleDistanceJob;
        private float spawnTimer;
        private int randomSpawnPointIndex;
        private Vector3 spawnPosition;
        private Vector3 spawnOffset = new Vector3(0, -4, 0);

        public int peopleCount { get; private set; }
        public int currentDensity { get; private set; }
        private List<AIPeople> peopleList = new List<AIPeople>();
        private List<AITrafficWaypointRoute> peopleRouteList = new List<AITrafficWaypointRoute>();
        private List<Rigidbody> rigidbodyList = new List<Rigidbody>();
        private List<NavMeshAgent> agents = new List<NavMeshAgent>();
        private List<AITrafficWaypointRouteInfo> peopleAIWaypointRouteInfo = new List<AITrafficWaypointRouteInfo>();
        private List<float> changeLaneCooldownTimer = new List<float>();
        private List<float> stopForHornCooldownTimer = new List<float>();
        private List<AITrafficWaypoint> currentWaypointList = new List<AITrafficWaypoint>();
        private AIPeopleJob peopleAITrafficJob;
        private JobHandle jobHandle;

        #region NativeList
        private NativeList<bool> isWalkingNL;

        private NativeList<int> waypointDataListCountNL;
        private NativeList<float3> routePointPositionNL;
        private NativeList<int> currentRoutePointIndexNL;
        private NativeList<float3> finalRoutePointPositionNL;

        private NativeList<bool> stopForTrafficLightNL;//是否需要根据信号灯停车
        private List<bool> runForTrafficLightNL=new List<bool>();//是否需要根据信号灯停车
        private NativeList<float> routeProgressNL;//道路进程
        private NativeList<bool> isFrontHitNL;//前方是否有障碍
        private NativeList<bool> isLastPointNL;
        private NativeList<bool> isFootHitNL;
        //转向
        private NativeList<Quaternion> targetRotationNL;
        //变道
        private NativeList<bool> canChangeLanesNL;
        private NativeList<bool> isChangingLanesNL;
        private NativeList<bool> needChangeLanesNL;
        //pool
        private NativeList<bool> isDisabledNL;
        private NativeList<bool> isActiveNL;
        private NativeList<bool> outOfBoundsNL;
        private AIPeoplePoolEntry newPeoplePoolEntry = new AIPeoplePoolEntry();
        private NativeList<float> distanceToPlayerNL;
        private List<AIPeoplePoolEntry> peoplePool = new List<AIPeoplePoolEntry>();
        private List<AITrafficSpawnPoint> peopleSpawnPoint = new List<AITrafficSpawnPoint>();
        private List<AITrafficSpawnPoint> availableSpawnPoints = new List<AITrafficSpawnPoint>();
        private float distanceToSpawnPoint;
        private int currentAmountToSpawn;
        private AIPeople spawnpeople;
        private AIPeople loadPeople;
        //特殊事件
        private NativeList<bool> stopForHornNL;
        private NativeList<int> runDirectionNL;
        private NativeList<bool> crossRoadNL;
        //ray cast
        private NativeArray<RaycastCommand> frontRaycastCommands;
        private NativeArray<RaycastHit> frontRaycastResults;
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
                //pool get value
                //usePool = AITrafficController.Instance.usePooling;
            }
            else
            {
                Debug.LogWarning("Multiple AIPeopleController Instances found in scene, this is not allowed. Destroying this duplicate AITrafficController.");
                Destroy(this);
            }
        }
        private void Start()
        {
            spawnZone = AITrafficController.Instance.spawnZone;
            usePool = AITrafficController.Instance.usePooling;
            if (usePool)
            {
                StartCoroutine(SpawnStartupTrafficCoroutine());
            }
            else
                StartCoroutine(Initialize());
        }
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
        IEnumerator SpawnStartupTrafficCoroutine()
        {
            yield return new WaitForEndOfFrame();
            availableSpawnPoints.Clear();
            currentDensity = 0;
            currentAmountToSpawn = density - currentDensity;
            for (int i = 0; i < peopleSpawnPoint.Count; i++) //获取所有的生成点
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, peopleSpawnPoint[i].transformCached.position);//车辆生成位置与中心点位置的距离
                if (peopleSpawnPoint[i].isTrigger == false)
                {
                    availableSpawnPoints.Add(peopleSpawnPoint[i]);
                }
            }
            for (int i = 0; i < density; i++) // 生成行人
            {
                for (int j = 0; j < peoplePrefabs.Length; j++)
                {
                    if (availableSpawnPoints.Count == 0 || currentAmountToSpawn == 0) break;
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                    GameObject spawnedTrafficVehicle = Instantiate(peoplePrefabs[j].gameObject, spawnPosition, availableSpawnPoints[randomSpawnPointIndex].transformCached.rotation);
                    spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);
                    spawnedTrafficVehicle.transform.LookAt(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                    availableSpawnPoints.RemoveAt(randomSpawnPointIndex);
                    currentAmountToSpawn -= 1;
                }
            }
            for (int i = 0; i < peopleInPool; i++)
            {
                if (peopleCount >= peopleInPool) break;
                for (int j = 0; j < peoplePrefabs.Length; j++)
                {
                    if (peopleCount >= peopleInPool) break;
                    GameObject spawnedTrafficVehicle = Instantiate(peoplePrefabs[j].gameObject, Vector3.zero, Quaternion.identity);
                    spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(peopleRouteList[0]);
                    MovePeopleToPool(spawnedTrafficVehicle.GetComponent<AIPeople>().assignedIndex);
                }
            }
            for (int i = 0; i < peopleCount; i++)
            {
                routePointPositionNL[i] = peopleRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;
                finalRoutePointPositionNL[i] = peopleRouteList[i].waypointDataList[peopleRouteList[i].waypointDataList.Count - 1]._transform.position;
                //peopleList[i].StartMoving();
            }
            for (int i = 0; i < peopleCount; i++)
            {
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
                    //射线检测尝试
                    frontRaycastCommands[i] = new RaycastCommand(peopleList[i].frontSensorTransform.position,peopleList[i].transform.forward, peopleList[i].frontSensorLength,layerMask);
                    footRaycastCommands[i] = new RaycastCommand(peopleList[i].footSensorTransform.position, peopleList[i].transform.forward, peopleList[i].footSensorLength, footLayerMask);
                    //peopleList[i].animator.SetFloat("Speed", rigidbodyList[i].velocity.magnitude / runningSpeed);
                    peopleList[i].animator.SetFloat("speedWithoutBT",agents[i].velocity.magnitude);
                }
                var handle = RaycastCommand.ScheduleBatch(frontRaycastCommands, frontRaycastResults, 1, default);
                handle.Complete();
                handle = RaycastCommand.ScheduleBatch(footRaycastCommands, footRaycastResults, 1, default);
                handle.Complete();
                for (int i = 0; i < peopleCount; i++)
                {
                    //isFrontHitNL[i] = frontRaycastResults[i].collider == null ? false : true;
                    if (!frontRaycastResults[i].collider)
                    {
                        isFrontHitNL[i] = false;
                    }
                    else
                    {                                               
                        if (frontRaycastResults[i].collider.GetComponent<AIPeople>() && Vector3.Dot(frontRaycastResults[i].collider.transform.forward, peopleList[i].transform.forward) < 0)
                        {//如果两个人或车迎面相撞 要绕道
                            isFrontHitNL[i] = false;
                        }
                        else if (frontRaycastResults[i].collider.GetComponent<AITrafficCar>())
                        {//如果撞到车 但是位置比较偏
                            Vector3 carPos = peopleList[i].transform.InverseTransformPoint(frontRaycastResults[i].collider.transform.position);
                            if (Mathf.Abs( carPos.x) > carWidth)
                                isFrontHitNL[i] = false;
                        }
                        else
                            isFrontHitNL[i] = true;
                    }
                    isFootHitNL[i] = footRaycastResults[i].collider == null ? false : true;
                }

                //deltaTime = Time.deltaTime;
                peopleAITrafficJob = new AIPeopleJob
                {
                    //set NA=NL
                    isWalkingNA = isWalkingNL,
                    currentRoutePointIndexNA = currentRoutePointIndexNL,
                    waypointDataListCountNA = waypointDataListCountNL,
                    routeProgressNA = routeProgressNL,
                    stopForTrafficLightNA = stopForTrafficLightNL,
                    isFrontHitNA = isFrontHitNL,
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

                jobHandle = peopleAITrafficJob.Schedule(peopleTAA);
                jobHandle.Complete();

                for (int i = 0; i < peopleCount; i++) // operate on results
                {
                    //变道
                    if (needChangeLanesNL[i])
                    {
                        if (!currentWaypointList[i] || currentWaypointList[i].onReachWaypointSettings.laneChangePoints.Count == 0)//没有道路可以变
                        {
                            isWalkingNL[i] = false;
                        }
                        else if (!canChangeLanesNL[i] && !isChangingLanesNL[i])
                        {
                            changeLaneCooldownTimer[i] += Time.deltaTime;
                            if (changeLaneCooldownTimer[i] > changeLaneCooldown)
                            {
                                canChangeLanesNL[i] = true;
                                changeLaneCooldownTimer[i] = 0f;
                            }
                            isWalkingNL[i] = false;//处于变道冷却时间，行人保持不动
                        }
                        else if (!isChangingLanesNL[i])//不在变道
                        {
                            isChangingLanesNL[i] = true;
                            canChangeLanesNL[i] = false;
                            peopleList[i].ChangeToRouteWaypoint(currentWaypointList[i].onReachWaypointSettings.laneChangePoints[0].onReachWaypointSettings);
                        }
                    }
                    //控制行走或暂停
                    if (crossRoadNL[i])
                        agents[i].speed = peopleList[i].maxSpeed;
                    else
                    {
                        if(isWalkingNL[i])
                        {
                            if (!runForTrafficLightNL[i])//没有走到路中间变红灯
                                agents[i].speed = peopleList[i].speed;
                            else
                                agents[i].speed = peopleList[i].maxSpeed;
                        }
                        else
                        {
                            agents[i].speed = 0;
                            if (stopForHornNL[i])//如果属于被车笛声干扰
                            {
                                //一段时间后后退或前进
                                if (stopForHornCooldownTimer[i] < waitingTime)
                                {
                                    stopForHornCooldownTimer[i] += Time.deltaTime;
                                }
                                else
                                {
                                    //设置躲避方向
                                    agents[i].speed = peopleList[i].maxSpeed;
                                    peopleList[i].animator.SetInteger("RunDirection", runDirectionNL[i]);
                                }
                            }
                        }
                    }

                }
                if (usePool)
                {
                    centerPosition = AITrafficController.Instance.centerPoint.position;
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
                    for (int i = 0; i < peopleCount; i++)
                    {
                        if (isDisabledNL[i] == false)
                        {
                            peopleRouteList[i].currentDensity += 1;
                            if (outOfBoundsNL[i])//在pool边界外
                            {
                                //Debug.Log(peopleList[i].assignedIndex);
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
            footRaycastResults = new NativeArray<RaycastHit>(peopleCount, Allocator.Persistent);
            footRaycastCommands = new NativeArray<RaycastCommand>(peopleCount, Allocator.Persistent);
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
        }
        //public void Add_PeopleSpawnPoint(AITrafficWaypoint _point)
        //{
        //    peopleSpawnPoint.Add(_point);
        //}
        #endregion

        #region pool
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
            peoplePool.Add(newPeoplePoolEntry);
        }
        void SpawnPeople()
        {
            spawnTimer = 0f;
            availableSpawnPoints.Clear();
            for (int i = 0; i < peopleSpawnPoint.Count; i++) // Get Available Spawn Points From All Zones
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, peopleSpawnPoint[i].transformCached.position);
                if ((distanceToSpawnPoint > AITrafficController.Instance.actizeZone || (distanceToSpawnPoint > AITrafficController.Instance.minSpawnZone && peopleSpawnPoint[i].isVisible == false))
                    && distanceToSpawnPoint < spawnZone && peopleSpawnPoint[i].isTrigger == false)
                {
                    availableSpawnPoints.Add(peopleSpawnPoint[i]);
                }
            }
            
            currentDensity = peopleList.Count - peoplePool.Count;
            if (currentDensity < density) //Spawn Traffic
            {
                currentAmountToSpawn = density - currentDensity;
                for (int i = 0; i < currentAmountToSpawn; i++)
                {
                    if (availableSpawnPoints.Count == 0 || peoplePool.Count == 0) break;
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    if (availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.currentDensity < availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.maxDensity)
                    {
                        //Debug.Log("spawn");
                        spawnpeople = GetPeopleFromPool(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);
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
                   // rigidbodyList[peoplePool[i].assignedIndex].isKinematic = false;
                    EnablePeople(peopleList[peoplePool[i].assignedIndex].assignedIndex, parentRoute);
                    peoplePool.RemoveAt(i);
                    return loadPeople;
                }
            }
            return loadPeople;
        }
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
                if (isFrontHitNL[i])
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
                //Gizmos.color = !isFrontHitNL[i]  ? STSPrefs.normalColor : STSPrefs.detectColor;
                //gizmoOffset = new Vector3(peopleList[i].frontSensorSize.x * 2.0f, peopleList[i].frontSensorSize.y * 2.0f, peopleList[i].frontSensorLength);
                //DrawCube(peopleList[i].frontSensorTransform.position + peopleList[i].transform.forward * (peopleList[i].frontSensorLength / 2), peopleList[i].transform.rotation, gizmoOffset);

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
