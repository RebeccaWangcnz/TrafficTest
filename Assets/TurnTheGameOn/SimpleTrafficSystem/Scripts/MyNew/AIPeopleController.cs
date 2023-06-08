namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections.Generic;
    using System.Collections;
    using UnityEngine;
    using UnityEngine.Jobs;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Jobs;
    //与AITrafficController类似用于控制行人
    public class AIPeopleController : MonoBehaviour
    {
        public static AIPeopleController Instance;
        #region params
        private bool isInitialized;
        public int peopleCount { get; private set; }
        private List<AIPeople> peopleList = new List<AIPeople>();
        private List<AITrafficWaypointRoute> peopleRouteList = new List<AITrafficWaypointRoute>();
        private List<Rigidbody> rigidbodyList = new List<Rigidbody>();
        private List<AITrafficWaypointRouteInfo> peopleAIWaypointRouteInfo = new List<AITrafficWaypointRouteInfo>();
        private float deltaTime;
        private AIPeopleJob peopleAITrafficJob;
        private JobHandle jobHandle;


        private NativeList<bool> isWalkingNL;
        //private NativeList<float> targetSpeedNL;
        //private NativeList<float> topSpeedNL;
        //public NativeList<float> speedNL;//当前速度
        //public NativeList<float> accelNL;//加速度
        //public NativeList<float> accelerationInputNL;//加速度输入

        //碰撞
        NativeArray<BoxcastCommand> frontBoxcastCommands;
        //public NativeList<bool> frontHitNL;//是否前方存在碰撞
        //public NativeList<float> frontSensorLengthNL;
        //public NativeList<float> frontHitDistanceNL;

        private NativeList<int> waypointDataListCountNL;
        private NativeList<float3> routePointPositionNL;
        private NativeList<int> currentRoutePointIndexNL;
        private NativeList<float3> finalRoutePointPositionNL;

        //private TransformAccessArray moveTargetTAA;
        //private TransformAccessArray peopleTAA;
        #endregion

        #region Main Methods
        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
                isWalkingNL = new NativeList<bool>(Allocator.Persistent);
                //targetSpeedNL = new NativeList<float>(Allocator.Persistent);
                //topSpeedNL = new NativeList<float>(Allocator.Persistent);
                //speedNL = new NativeList<float>(Allocator.Persistent);
                //accelNL = new NativeList<float>(Allocator.Persistent);
                //accelerationInputNL = new NativeList<float>(Allocator.Persistent);
                //frontHitNL = new NativeList<bool>(Allocator.Persistent);
                //frontSensorLengthNL = new NativeList<float>(Allocator.Persistent);
                //frontHitDistanceNL = new NativeList<float>(Allocator.Persistent);
                frontBoxcastCommands = new NativeArray<BoxcastCommand>(peopleCount, Allocator.Persistent);
                routePointPositionNL = new NativeList<float3>(Allocator.Persistent);
                currentRoutePointIndexNL = new NativeList<int>(Allocator.Persistent);
                finalRoutePointPositionNL = new NativeList<float3>(Allocator.Persistent);
                waypointDataListCountNL = new NativeList<int>(Allocator.Persistent);
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
                routePointPositionNL[i] = peopleRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;//出生点位置
                finalRoutePointPositionNL[i] = peopleRouteList[i].waypointDataList[peopleRouteList[i].waypointDataList.Count - 1]._transform.position;//路径终点位置
                //peopleList[i].StartMoving();
            }
            isInitialized = true;
        }
        private void FixedUpdate()
        {
            if (isInitialized)
            {
                //deltaTime = Time.deltaTime;
                //peopleAITrafficJob = new AIPeopleJob
                //{
                //    //set NA=NL
                //    isWalkingNA = isWalkingNL,
                //    targetSpeedNA = targetSpeedNL,
                //    topSpeedNA = topSpeedNL,
                //    speedNA = speedNL,
                //    accelNA = accelNL,
                //    accelerationInputNA = accelerationInputNL,
                //    frontHitNA = frontHitNL,
                //    frontSensorLengthNA = frontSensorLengthNL,
                //    frontHitDistanceNA = frontHitDistanceNL,
                //    routePointPositionNA = routePointPositionNL,
                //    currentRoutePointIndexNA = currentRoutePointIndexNL,
                //    finalRoutePointPositionNA = finalRoutePointPositionNL,
                //    waypointDataListCountNA = waypointDataListCountNL
                //};
                //jobHandle = peopleAITrafficJob.Schedule(moveTargetTAA);
                //jobHandle.Complete();
                for (int i = 0; i < peopleCount; i++) // operate on results
                {
                    if (isWalkingNL[i])
                        rigidbodyList[i].velocity = rigidbodyList[i].transform.forward * peopleList[i].moveSpeed * Time.timeScale;//让ai前进
                    frontBoxcastCommands[i] = new BoxcastCommand(frontSensorTransformPositionNL[i], frontSensorSizeNL[i], frontRotationList[i], frontDirectionList[i], frontSensorLengthNL[i], layerMask);
                }
                // do sensor jobs
                var handle = BoxcastCommand.ScheduleBatch(frontBoxcastCommands, frontBoxcastResults, 1, default);
                handle.Complete();

            }
        }
        void DisposeArrays(bool _isQuit)
        {
            if (_isQuit)
            {
                isWalkingNL.Dispose();
                frontBoxcastCommands.Dispose();
                routePointPositionNL.Dispose();
                currentRoutePointIndexNL.Dispose();
                finalRoutePointPositionNL.Dispose();
                waypointDataListCountNL.Dispose();
            }
        }
        #endregion

        #region Register
        public int RegisterPeopleAI(AIPeople peopleAI, AITrafficWaypointRoute route)
        {
            peopleList.Add(peopleAI);
            peopleRouteList.Add(route);
            Rigidbody rigidbody = peopleAI.GetComponent<Rigidbody>();
            rigidbodyList.Add(rigidbody);

            Transform moveTarget = new GameObject("MoveTarget").transform;//移动目标
            //moveTarget.SetParent(peopleAI.transform);
            //TransformAccessArray temp_moveTargetTAA = new TransformAccessArray(peopleCount);
            //for (int i = 0; i < peopleCount; i++)
            //{
            //    temp_moveTargetTAA.Add(moveTargetTAA[i]);
            //}
            //temp_moveTargetTAA.Add(moveTarget);

            peopleCount = peopleList.Count;

            #region allocation
            isWalkingNL.Add(true);
            //targetSpeedNL.Add(0);
            //topSpeedNL.Add(0);
            //speedNL.Add(0);
            //accelNL.Add(0);
            //accelerationInputNL.Add(0);
            //frontHitNL.Add(false);
            //frontSensorLengthNL.Add(0);
            //frontHitDistanceNL.Add(0);
            routePointPositionNL.Add(float3.zero);
            currentRoutePointIndexNL.Add(0);
            finalRoutePointPositionNL.Add(float3.zero);
            waypointDataListCountNL.Add(0);
            peopleAIWaypointRouteInfo.Add(null);
            //moveTargetTAA = new TransformAccessArray(peopleCount);
            //peopleTAA = new TransformAccessArray(peopleCount);
            #endregion

            waypointDataListCountNL[peopleCount - 1] = peopleRouteList[peopleCount - 1].waypointDataList.Count;//第i个人所在route一共有几个点
            peopleAIWaypointRouteInfo[peopleCount - 1] = peopleRouteList[peopleCount - 1].routeInfo;
            for (int i = 0; i < peopleCount; i++)
            {
                //moveTargetTAA.Add(temp_moveTargetTAA[i]);
                //peopleTAA.Add(peopleList[i].transform);
            }

            return peopleCount - 1;
        }
        #endregion
        #region set array data
        public void Set_IsDrivingArray(int _index, bool _value)
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
        public void Set_CurrentRoutePointIndexArray(int _index, int _value/*, AITrafficWaypoint _nextWaypoint*/)
        {
            currentRoutePointIndexNL[_index] = _value;
            //currentWaypointList[_index] = _nextWaypoint;
            //isChangingLanesNL[_index] = false;
        }
        public void Set_RoutePointPositionArray(int _index)
        {
            routePointPositionNL[_index] = peopleRouteList[_index].waypointDataList[currentRoutePointIndexNL[_index]]._transform.position;
            finalRoutePointPositionNL[_index] = peopleRouteList[_index].waypointDataList[peopleRouteList[_index].waypointDataList.Count - 1]._transform.position;
        }
        public AITrafficWaypointRoute GetPeopleRoute(int _index)
        {
            return peopleRouteList[_index];
        }//获取人所在的路径
        #endregion

    }
}
