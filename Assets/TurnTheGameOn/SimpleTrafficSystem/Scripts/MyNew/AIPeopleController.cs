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
    //��AITrafficController�������ڿ�������
    public class AIPeopleController : MonoBehaviour
    {
        public static AIPeopleController Instance;//ʵ�����������ȡ
        #region Params
        private bool isInitialized;//�Ƿ񱻳�ʼ��

        #region �ٶ�����
        [Tooltip("the speed for people running")]
        public float runningSpeed;
        [Tooltip("the walking speed range for people")]
        public Vector2 walkingSpeedRange;
        [Tooltip("the riding speed range for bycicle")]
        public Vector2 ridingSpeedRange;
        [Tooltip("the fastest riding speed range for bycicle")]
        public float fastestRidingSpeed;
        #endregion

        #region ���߼��㼶
        [Tooltip("Physics layers the detection sensors can detect.")]
        public LayerMask layerMask;
        [Tooltip("Physics layers the foot detection sensors can detect.")]
        public LayerMask footLayerMask;
        #endregion

        #region ����·������
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
        public float waitingTime;//�����������������¼�

        public int peopleCount { get; private set; }//��������
        public int currentDensity { get; private set; }//��ǰ�ܶ�
        #endregion

        #region ˽�б���
        //job
        private AIPeopleDistanceJob _AIPeopleDistanceJob;//Job
        private AIPeopleJob peopleAITrafficJob;
        private JobHandle jobHandle;
        private AIPeoplePoolEntry newPeoplePoolEntry = new AIPeoplePoolEntry();
        //spawn
        private float3 centerPosition;//��ͨ�����ĵ㣬��AITrafficControllerһ��
        private float spawnZone;//��������
        private bool usePool;//�Ƿ�ʹ��pool����AITraffciControllerһ��
        private float spawnTimer;//���ɵ���ʱ
        private int randomSpawnPointIndex;//������ɵ�Index
        private Vector3 spawnPosition;//����λ��
        private Vector3 spawnOffset = new Vector3(0, -4, 0);//����ƫ�ƣ�һ����ǰ���˸�
        private float distanceToSpawnPoint;
        private int currentAmountToSpawn;//����������
        private AIPeople spawnpeople;//������
        private AIPeople loadPeople;
        //List
        private List<AIPeople> peopleList = new List<AIPeople>();//����AIPeople�б�
        private List<AITrafficWaypointRoute> peopleRouteList = new List<AITrafficWaypointRoute>();//����·���б�
        private List<Rigidbody> rigidbodyList = new List<Rigidbody>();//�����б�
        private List<NavMeshAgent> agents = new List<NavMeshAgent>();//AI agent�б�
        private List<AITrafficWaypointRouteInfo> peopleAIWaypointRouteInfo = new List<AITrafficWaypointRouteInfo>();//·��info�б�
        private List<float> changeLaneCooldownTimer = new List<float>();//������ȴ����ʱ
        private List<float> stopForHornCooldownTimer = new List<float>();//����ֹͣ����ʱ
        private List<AITrafficWaypoint> currentWaypointList = new List<AITrafficWaypoint>();//��ǰ·�ߵ��б�
        private List<Vector3> targetsList = new List<Vector3>();//Ŀ����б�
        private List<bool> runForTrafficLightNL = new List<bool>();//�Ƿ���Ҫ�����źŵ��ܲ������������Ҫ����������������·�����źŵƱ����߱�Ƽ���ͨ��
        private List<AITrafficSpawnPoint> peopleSpawnPoint = new List<AITrafficSpawnPoint>();
        private List<AITrafficSpawnPoint> availableSpawnPoints = new List<AITrafficSpawnPoint>();
        private List<AIPeoplePoolEntry> peoplePool = new List<AIPeoplePoolEntry>();

        #endregion

        #region NativeList
        //����list���Ǳ����Ӧindex�����˵�·����Ϣ
        private NativeList<bool> isWalkingNL;//�Ƿ�����

        private NativeList<int> waypointDataListCountNL;//·���ϵ�·��������
        private NativeList<float3> routePointPositionNL;//·����λ��
        private NativeList<int> currentRoutePointIndexNL;//��ǰ·��������
        private NativeList<float3> finalRoutePointPositionNL;//·�����һ��·����λ��
        private NativeList<bool> stopForTrafficLightNL;//�Ƿ���Ҫ�����źŵ�ͣ��

        private NativeList<float> routeProgressNL;//��·����
        private NativeList<bool> isFrontHitNL;//ǰ���Ƿ����ϰ�
        private NativeList<bool> isLeftHitNL;//ǰ���Ƿ����ϰ�
        private NativeList<bool> isRighttHitNL;//ǰ���Ƿ����ϰ�
        private NativeList<bool> isLastPointNL;//�Ƿ������һ����
        private NativeList<bool> isFootHitNL;//�Ų��Ƿ��⵽̨��
        //ת��
        private NativeList<Quaternion> targetRotationNL;
        //���
        private NativeList<bool> canChangeLanesNL;
        private NativeList<bool> isChangingLanesNL;
        private NativeList<bool> needChangeLanesNL;
        //pool
        private NativeList<bool> isDisabledNL;//�Ƿ���δ����״̬
        private NativeList<bool> isActiveNL;
        private NativeList<bool> outOfBoundsNL;//�Ƿ��ڱ߽���       
        private NativeList<float> distanceToPlayerNL;//��player����
        //�����¼�
        private NativeList<bool> stopForHornNL;//ֹͣ����
        private NativeList<int> runDirectionNL;//�ܿ�����
        private NativeList<bool> crossRoadNL;//�Ƿ�ҪǿӲ����·
        private Vector3 direction;//�ܿ�����
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
            //��ʼ�� ����ռ�
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
            //��ȡAITrafficController��pooling��Ϣ
            spawnZone = AITrafficController.Instance.spawnZone;
            usePool = AITrafficController.Instance.usePooling;
            if (usePool)
            {
                StartCoroutine(SpawnStartupTrafficCoroutine());
            }
            else
                StartCoroutine(Initialize());
        }
        //������poolingʱ�ĳ�ʼ��
        IEnumerator Initialize()
        {
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < peopleCount; i++)
            {
                routePointPositionNL[i] = peopleRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;//������λ��
                finalRoutePointPositionNL[i] = peopleRouteList[i].waypointDataList[peopleRouteList[i].waypointDataList.Count - 1]._transform.position;//·���յ�λ��
                //peopleList[i].StartMoving();
            }
            isInitialized = true;
        }
        //ʹ��poolingʱ�ĳ�ʼ��
        IEnumerator SpawnStartupTrafficCoroutine()
        {
            yield return new WaitForEndOfFrame();
            availableSpawnPoints.Clear();
            currentDensity = 0;
            currentAmountToSpawn = density - currentDensity;

            //��ȡ���е����ɵ�
            for (int i = 0; i < peopleSpawnPoint.Count; i++) 
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, peopleSpawnPoint[i].transformCached.position);//��������λ�������ĵ�λ�õľ���
                if (peopleSpawnPoint[i].isTrigger == false)//��ǰ��û�б�ռ��
                {
                    availableSpawnPoints.Add(peopleSpawnPoint[i]);//��ӵ��������ɵ�·�����б���
                }
            }

            //��������
            for (int i = 0; i < density; i++)
            {
                for (int j = 0; j < peoplePrefabs.Length; j++)
                {
                    if (availableSpawnPoints.Count == 0 || currentAmountToSpawn == 0) break;//����Ҫ���������ѭ��

                    //��ȡ������ɵ�index
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    //����λ��
                    spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                    
                    //����
                    GameObject spawnedTrafficVehicle = Instantiate(peoplePrefabs[j].gameObject, spawnPosition, availableSpawnPoints[randomSpawnPointIndex].transformCached.rotation);
                    spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);
                    spawnedTrafficVehicle.transform.LookAt(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                    availableSpawnPoints.RemoveAt(randomSpawnPointIndex);
                    currentAmountToSpawn -= 1;
                }
            }

            //����pool�������
            for (int i = 0; i < peopleInPool; i++)
            {
                //������������pool����������Ҫ��pool����������
                if (peopleCount >= peopleInPool) break;
                for (int j = 0; j < peoplePrefabs.Length; j++)
                {
                    if (peopleCount >= peopleInPool) break;
                    GameObject spawnedTrafficVehicle = Instantiate(peoplePrefabs[j].gameObject, Vector3.zero, Quaternion.identity);
                    spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(peopleRouteList[0]);
                    //�����ɵ� �����Ƶ�pool��
                    MovePeopleToPool(spawnedTrafficVehicle.GetComponent<AIPeople>().assignedIndex);
                }
            }
            for (int i = 0; i < peopleCount; i++)
            {
                //���õ�ǰλ�ú��յ�λ��
                routePointPositionNL[i] = peopleRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;
                finalRoutePointPositionNL[i] = peopleRouteList[i].waypointDataList[peopleRouteList[i].waypointDataList.Count - 1]._transform.position;
            }
            for (int i = 0; i < peopleCount; i++)
            {
                //���ø�����
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
                    //���߼��
                    frontRaycastCommands[i] = new RaycastCommand(peopleList[i].frontSensorTransform.position,peopleList[i].transform.forward, peopleList[i].frontSensorLength,layerMask);
                    leftBoxcastCommands[i] = new BoxcastCommand(peopleList[i].leftSensorTransform.position,peopleList[i].sideSensorSize, peopleList[i].transform.rotation,peopleList[i].transform.forward, peopleList[i].sideSensorLength, layerMask);
                    rightBoxcastCommands[i] = new BoxcastCommand(peopleList[i].rightSensorTransform.position,peopleList[i].sideSensorSize, peopleList[i].transform.rotation,peopleList[i].transform.forward, peopleList[i].sideSensorLength, layerMask);
                    footRaycastCommands[i] = new RaycastCommand(peopleList[i].footSensorTransform.position, peopleList[i].transform.forward, peopleList[i].footSensorLength, footLayerMask);

                    //�����ٶȺͶ�����Ӧ
                    peopleList[i].animator.SetFloat("speedWithoutBT",agents[i].velocity.magnitude);
                    //�����ٶ����ñ��õȼ� 0808����һ�����ȼ����㷽�������˺ͷǻ���������Ӧ��һ����
                    agents[i].avoidancePriority = (int)(100*agents[i].speed/ fastestRidingSpeed);
                }
                //�������߼��������
                var handle = RaycastCommand.ScheduleBatch(frontRaycastCommands, frontRaycastResults, 1, default);
                handle.Complete();
                handle = BoxcastCommand.ScheduleBatch(leftBoxcastCommands, leftBoxcastResults, 1, default);
                handle.Complete();
                handle = BoxcastCommand.ScheduleBatch(rightBoxcastCommands, rightBoxcastResults, 1, default);
                handle.Complete();
                handle = RaycastCommand.ScheduleBatch(footRaycastCommands, footRaycastResults, 1, default);
                handle.Complete();
                //������ײ������ò���ֵ
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
                        {//���������ӭ����ײ Ҫ�Ƶ�
                            isFrontHitNL[i] = false;
                        }
                        else if(hitPeople && agents[i].avoidancePriority> agents[hitPeople.assignedIndex].avoidancePriority && agents[hitPeople.assignedIndex].avoidancePriority!=0)
                        {
                            isFrontHitNL[i] = false;
                        }
                        //else if(!frontRaycastResults[i].collider.GetComponent<AITrafficCar>()&&(!isLeftHitNL[i]||!isRighttHitNL[i]))
                        //{//�������û��ײ������
                        //    Vector3 pointPos = peopleList[i].transform.InverseTransformPoint(targetsList[i]);
                        //    if((pointPos.x>=0&&!isRighttHitNL[i])|| (pointPos.x<= 0 && !isLeftHitNL[i]))
                        //        isFrontHitNL[i] = false;
                        //}
                        else if(hitCar && hitCar.IsBraking())
                        {//ǰ���������ǳ����ҳ���ֹͣ�ģ�����������
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
                //��ʼjob
                jobHandle = peopleAITrafficJob.Schedule(peopleTAA);
                jobHandle.Complete();

                // operate on results
                for (int i = 0; i < peopleCount; i++) 
                {
                    //���
                    if (needChangeLanesNL[i])
                    {
                        if (!currentWaypointList[i] || currentWaypointList[i].onReachWaypointSettings.laneChangePoints.Count == 0)//û�е�·���Ա�
                        {
                            isWalkingNL[i] = false;
                        }
                        else if (!canChangeLanesNL[i] && !isChangingLanesNL[i])//���ڱ����ȴʱ�䣬���˱��ֲ���
                        {
                            changeLaneCooldownTimer[i] += Time.deltaTime;
                            if (changeLaneCooldownTimer[i] > changeLaneCooldown)
                            {
                                canChangeLanesNL[i] = true;
                                changeLaneCooldownTimer[i] = 0f;
                            }
                            isWalkingNL[i] = false;
                        }
                        else if (!isChangingLanesNL[i])//���ڱ������ʱ���Ա��
                        {
                            isChangingLanesNL[i] = true;
                            canChangeLanesNL[i] = false;
                            peopleList[i].ChangeToRouteWaypoint(currentWaypointList[i].onReachWaypointSettings.laneChangePoints[0].onReachWaypointSettings);
                        }
                    }

                    //ǿ�ƹ���·�������¼�
                    if (crossRoadNL[i])
                        agents[i].speed = peopleList[i].maxSpeed;
                    //�������
                    else
                    {
                        if(isWalkingNL[i])//����״̬
                        {
                            if (!runForTrafficLightNL[i])//û���ߵ�·�м����
                                agents[i].speed = peopleList[i].speed;
                            else//�ߵ�·�����죬���Ϊ����ٶ�����
                                agents[i].speed = peopleList[i].maxSpeed;
                        }
                        else//ֹͣ״̬
                        {
                            agents[i].speed = 0;
                            if (stopForHornNL[i])//������ڱ�����������
                            {                                
                                //һ��ʱ�����˻�ǰ��
                                if (stopForHornCooldownTimer[i] < waitingTime)
                                {
                                    stopForHornCooldownTimer[i] += Time.deltaTime;
                                    direction = agents[i].transform.forward;
                                }
                                else
                                {
                                    //ͷ����ת
                                    peopleList[i].playerHead.LookAt(new Vector3( peopleList[i].frontSensorTransform.position.x, peopleList[i].playerHead.position.y, peopleList[i].frontSensorTransform.position.z));
                                    //���ö�ܷ���
                                   if (runDirectionNL[i]==1)
                                   {
                                        agents[i].speed = peopleList[i].maxSpeed;
                                    }
                                    else
                                    {
                                        agents[i].velocity = -direction * peopleList[i].maxSpeed;                                        
                                    }
                                    peopleList[i].animator.SetInteger("RunDirection", 1);
                                    //����̨����
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
                    //���ݽ��ˢ���ܶ�
                    for (int i = 0; i < peopleCount; i++)
                    {
                        if (isDisabledNL[i] == false)
                        {
                            peopleRouteList[i].currentDensity += 1;
                            if (outOfBoundsNL[i])//��pool�߽���
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

            Transform moveTarget = new GameObject("MoveTarget").transform;//�ƶ�Ŀ��
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

            waypointDataListCountNL[peopleCount - 1] = peopleRouteList[peopleCount - 1].waypointDataList.Count;//��i��������routeһ���м�����
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
        }//��ȡ�����ڵ�·��
        public bool GetChangeLaneInfo(int _index)
        {
            return isChangingLanesNL[_index];
        }//��ȡ�����Ϣ
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
        //�����ƶ���pool
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
            peoplePool.Add(newPeoplePoolEntry);//��ӵ�pool�б���
        }
        void SpawnPeople()//��������
        {
            spawnTimer = 0f;
            availableSpawnPoints.Clear();
            //��ȡ���ܵ�����λ��
            for (int i = 0; i < peopleSpawnPoint.Count; i++) 
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, peopleSpawnPoint[i].transformCached.position);
                if ((distanceToSpawnPoint > AITrafficController.Instance.actizeZone || (distanceToSpawnPoint > AITrafficController.Instance.minSpawnZone && peopleSpawnPoint[i].isVisible == false))
                    && distanceToSpawnPoint < spawnZone && peopleSpawnPoint[i].isTrigger == false)//������zone��Χ���Ҳ��ɼ��Ҳ����Ǳ�ռ�ݵ�״̬
                {
                    availableSpawnPoints.Add(peopleSpawnPoint[i]);
                }
            }
            
            currentDensity = peopleList.Count - peoplePool.Count;//��ȡ�ܶ�
            if (currentDensity < density) //Spawn Traffic
            {
                //�õ���Ҫ���ɵ�����
                currentAmountToSpawn = density - currentDensity;
                
                for (int i = 0; i < currentAmountToSpawn; i++)
                {
                    if (availableSpawnPoints.Count == 0 || peoplePool.Count == 0) break;
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    if (availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.currentDensity < availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.maxDensity)
                    {
                        spawnpeople = GetPeopleFromPool(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);//��pool�л�ȡ�����������
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
                   // ����pool�е�����
                    EnablePeople(peopleList[peoplePool[i].assignedIndex].assignedIndex, parentRoute);
                    peoplePool.RemoveAt(i);
                    return loadPeople;
                }
            }
            return loadPeople;
        }
        //���˴�pool�м���
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
                //ǰ�����߼����ʾ
                if (frontRaycastResults[i].collider)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(peopleList[i].frontSensorTransform.position, frontRaycastResults[i].collider.transform.position);
                }
                else
                {
                    // �������û�л������壬�����ߵ�ĩ��λ�û���Ϊ��ɫ�� Gizmos ����
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(peopleList[i].frontSensorTransform.position, peopleList[i].frontSensorTransform.position + peopleList[i].transform.forward * peopleList[i].frontSensorLength);
                }

                //�������߼����ʾ
                Gizmos.color = !isRighttHitNL[i] ? STSPrefs.normalColor : STSPrefs.detectColor;
                gizmoOffset = new Vector3(peopleList[i].sideSensorSize.x * 2.0f, peopleList[i].sideSensorSize.y * 2.0f, peopleList[i].sideSensorLength);
                DrawCube(peopleList[i].leftSensorTransform.position + peopleList[i].transform.forward * (peopleList[i].sideSensorLength / 2), peopleList[i].transform.rotation, gizmoOffset);
                DrawCube(peopleList[i].rightSensorTransform.position + peopleList[i].transform.forward * (peopleList[i].sideSensorLength / 2), peopleList[i].transform.rotation, gizmoOffset);

                //�Ų����߼����ʾ
                if (!isFootHitNL[i])
                {
                    // �������û�л������壬�����ߵ�ĩ��λ�û���Ϊ��ɫ�� Gizmos ����
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
