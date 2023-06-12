namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Jobs;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Jobs;
    //��AITrafficController�������ڿ�������
    public class AIPeopleController : MonoBehaviour
    {
        public static AIPeopleController Instance;
        #region Params
        private bool isInitialized;
        [Header("Speed")]
        public float runningSpeed;
        public float walkingSpeed;
        public float accelerateSpeed;
        [Header("Ray Detect")]
        [Tooltip("Physics layers the detection sensors can detect.")]
        public LayerMask layerMask;
        [Tooltip("Physics layers the foot detection sensors can detect.")]
        public LayerMask footLayerMask;
        [Tooltip("Enables the processing of Lane Changing logic.")]
        public bool useLaneChanging;
        [Tooltip("Minimum time required after changing lanes before allowed to change lanes again.")]
        public float changeLaneCooldown = 20f;

        public int peopleCount { get; private set; }
        private List<AIPeople> peopleList = new List<AIPeople>();
        private List<AITrafficWaypointRoute> peopleRouteList = new List<AITrafficWaypointRoute>();
        private List<Rigidbody> rigidbodyList = new List<Rigidbody>();
        private List<AITrafficWaypointRouteInfo> peopleAIWaypointRouteInfo = new List<AITrafficWaypointRouteInfo>();
        private List<float> changeLaneCooldownTimer = new List<float>();
        private List<AITrafficWaypoint> currentWaypointList = new List<AITrafficWaypoint>();
        private AIPeopleJob peopleAITrafficJob;
        private JobHandle jobHandle;

        #region NativeList
        private NativeList<bool> isWalkingNL;

        private NativeList<int> waypointDataListCountNL;
        private NativeList<float3> routePointPositionNL;
        private NativeList<int> currentRoutePointIndexNL;
        private NativeList<float3> finalRoutePointPositionNL;

        private NativeList<bool> stopForTrafficLightNL;//�Ƿ���Ҫ�����źŵ�ͣ��
        private NativeList<float> routeProgressNL;//��·����
        private NativeList<bool> isFrontHitNL;//ǰ���Ƿ����ϰ�
        private NativeList<bool> isLastPointNL;
        private NativeList<bool> isFootHitNL;
        //ת��
        private NativeList<Quaternion> targetRotationNL;
        //���
        private NativeList<bool> canChangeLanesNL;
        private NativeList<bool> isChangingLanesNL;
        private NativeList<bool> needChangeLanesNL;
        //���߼��
        NativeArray<BoxcastCommand> frontBoxcastCommands;
        NativeArray<RaycastHit> frontBoxcastResults;

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
            }
            else
            {
                Debug.LogWarning("Multiple AIPeopleController Instances found in scene, this is not allowed. Destroying this duplicate AITrafficController.");
                Destroy(this);
            }
        }
        private void Start()
        {
            StartCoroutine(Initialize());
        }
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
        private void FixedUpdate()
        {
            if (isInitialized)
            {                
                for(int i=0;i<peopleCount;i++)
                {
                    stopForTrafficLightNL[i] = peopleAIWaypointRouteInfo[i].stopForTrafficLight;
                    isFrontHitNL[i]= peopleList[i].FrontSensorDetecting();
                    isFootHitNL[i] = peopleList[i].FootSensorDetecting();
                    peopleList[i].animator.SetFloat("Speed",rigidbodyList[i].velocity.magnitude/runningSpeed);
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
                    isFrontHitNA= isFrontHitNL,
                    isLastPointNA= isLastPointNL,
                    isFootHitNA= isFootHitNL,
                    targetRotationNA= targetRotationNL,
                    needChangeLanesNA= needChangeLanesNL,
                    deltaTime =Time.deltaTime
                };

                jobHandle = peopleAITrafficJob.Schedule(peopleTAA);
                jobHandle.Complete();

                for (int i = 0; i < peopleCount; i++) // operate on results
                {
                    //���
                    if (needChangeLanesNL[i])
                    {
                        if(currentWaypointList[i].onReachWaypointSettings.laneChangePoints.Count==0)//û�е�·���Ա�
                        {
                            isWalkingNL[i] = false;
                        }
                        else if (!canChangeLanesNL[i]&&!isChangingLanesNL[i])
                        {
                            changeLaneCooldownTimer[i] += Time.deltaTime;
                            if (changeLaneCooldownTimer[i] > changeLaneCooldown)
                            {
                                canChangeLanesNL[i] = true;
                                changeLaneCooldownTimer[i] = 0f;
                            }
                            isWalkingNL[i] = false;//���ڱ����ȴʱ�䣬���˱��ֲ���
                        }
                        else if (!isChangingLanesNL[i])//���ڱ��
                        {
                            isChangingLanesNL[i] = true;
                            canChangeLanesNL[i] = false;
                            peopleList[i].ChangeToRouteWaypoint(currentWaypointList[i].onReachWaypointSettings.laneChangePoints[0].onReachWaypointSettings);
                        }
                    }
                    //�������߻���ͣ
                    if (isWalkingNL[i])
                        rigidbodyList[i].velocity = rigidbodyList[i].transform.forward * walkingSpeed * Time.timeScale;//��aiǰ��
                    else
                        rigidbodyList[i].velocity = Vector3.zero;
                    //���ײ����̨��
                    if(isFootHitNL[i])
                    {
                        rigidbodyList[i].transform.position += new Vector3(0,0.3f*Time.deltaTime,0);//����΢΢����
                    }
                    
                }
            }
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
            }
            moveTargetTAA.Dispose();
            peopleTAA.Dispose();
        }
        #endregion

        #region Register
        public int RegisterPeopleAI(AIPeople peopleAI, AITrafficWaypointRoute route)
        {
            peopleList.Add(peopleAI);
            peopleRouteList.Add(route);
            Rigidbody rigidbody = peopleAI.GetComponent<Rigidbody>();
            rigidbodyList.Add(rigidbody);

            Transform moveTarget = new GameObject("MoveTarget").transform;//�ƶ�Ŀ��
            moveTarget.SetParent(peopleAI.transform);
            TransformAccessArray temp_moveTargetTAA = new TransformAccessArray(peopleCount);
            for (int i = 0; i < peopleCount; i++)
            {
                temp_moveTargetTAA.Add(moveTargetTAA[i]);
            }
            temp_moveTargetTAA.Add(moveTarget);

            peopleCount = peopleList.Count;
            if(peopleCount>=2)
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
            isChangingLanesNL.Add(false);
            needChangeLanesNL.Add(false);
            currentWaypointList.Add(null);
            #endregion

            waypointDataListCountNL[peopleCount - 1] = peopleRouteList[peopleCount - 1].waypointDataList.Count;//��i��������routeһ���м�����
            peopleAIWaypointRouteInfo[peopleCount - 1] = peopleRouteList[peopleCount - 1].routeInfo;
            for (int i = 0; i < peopleCount; i++)
            {
                moveTargetTAA.Add(temp_moveTargetTAA[i]);
                peopleTAA.Add(peopleList[i].transform);
            }

            return peopleCount - 1;
        }
        #endregion

        #region set array data
        public void Set_IsWalkingArray(int _index, bool _value)
        {
            if(isWalkingNL[_index]!=_value)
            {
                isWalkingNL[_index] = _value;
                if (_value == false)
                {
                    rigidbodyList[_index].velocity = Vector3.zero;
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
        public AITrafficWaypointRoute GetPeopleRoute(int _index)
        {
            return peopleRouteList[_index];
        }//��ȡ�����ڵ�·��
        public bool GetChangeLaneInfo(int _index)
        {
            return isChangingLanesNL[_index];
        }//��ȡ�����Ϣ
        #endregion

    }
}
