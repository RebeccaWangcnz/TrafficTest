namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections.Generic;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using System;
    using UnityEngine;
    using UnityEngine.Jobs;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Jobs;

    [HelpURL("https://simpletrafficsystem.turnthegameon.com/documentation/api/aitrafficcontroller")]
    public class AITrafficController : MonoBehaviour
    {
        public static AITrafficController Instance;//声明一个“自己”以便调用
        
        #region Public Variables and Registers
        public int carCount { get; private set; }
        public int currentDensity { get; private set; }

        [Tooltip("Array of AITrafficCar prefabs to spawn.")]
        public AITrafficCar[] trafficPrefabs;

        #region Car Settings
        [Tooltip("Enables the processing of YieldTrigger logic.")]
        public bool useYieldTriggers;
        [Tooltip("Multiplier used for calculating speed; 2.23693629 by default for MPH.")]//默认速度英里
        public float speedMultiplier = 2.23693629f;//速度乘数
        [Tooltip("Multiplier used to control how quickly the car's front wheels turn toward the target direction.")]
        public float steerSensitivity = 0.02f;//方向盘敏感度
        [Tooltip("Maximum angle the car's front wheels are allowed to turn toward the target direction.")]
        public float maxSteerAngle = 37f;//最大前轮舵角
        [Tooltip("Front detection sensor distance at which a car will start braking.")]
        public float stopThreshold = 5f;//前方障碍物识别减速区域
        [Tooltip("急刹车时给rigid施加的drag力")]
        public float hardBrakePower;

        [Tooltip("Physics layers the detection sensors can detect.")]
        public LayerMask layerMask;//在监视器里有图层的多选选单

        [Tooltip("Rotates the front sensor to face the next waypoint.")]
        public bool frontSensorFacesTarget = false;

        public WheelFrictionCurve lowSidewaysWheelFrictionCurve = new WheelFrictionCurve();
        public WheelFrictionCurve highSidewaysWheelFrictionCurve = new WheelFrictionCurve();

        [Tooltip("Enables the processing of Lane Changing logic.")]
        public bool useLaneChanging;
        [Tooltip("Minimum amount of time until a car is allowed to change lanes once conditions are met.")]
        public float changeLaneTrigger = 3f;//变道延迟时间
        [Tooltip("Minimum speed required to change lanes.")]
        public float minSpeedToChangeLanes = 5f;//最小变道速度
        [Tooltip("Minimum time required after changing lanes before allowed to change lanes again.")]
        public float changeLaneCooldown = 20f;//两次变道最小间隔时间

        [Tooltip("Dummy material used for brake light emission logic when a car does not have an assigned brake variable.")]
        public Material unassignedBrakeMaterial;
        public float brakeOnIntensityURP = 1f;
        public float brakeOnIntensityHDRP = 10f;
        public float brakeOnIntensityDP = 10f;
        public float brakeOffIntensityURP = -3f;
        public float brakeOffIntensityHDRP = 0f;
        public float brakeOffIntensityDP = -3f;
        private Color brakeColor = Color.red;
        private Color brakeOnColor;
        private Color brakeOffColor;
        private float brakeIntensityFactor;
        private string emissionColorName;
        //刹车灯的一个定义出来的材质（当没有刹车变量时？）
        [Tooltip("AI Cars will be parented to the 'Car Parent' transform, this AITrafficController will be the parent if a parent is not assigned.")]
        public bool setCarParent;
        [Tooltip("If 'Set Car Parent' is enabled, AI Cars will be parented to this transform, this AITrafficController will be the parent if a parent is not defined.")]
        public Transform carParent;
        #endregion
        //车辆的公共变量
        #region Pooling
        [Tooltip("Toggle the inspector and debug warnings about how the scene camera can impact pooling behavior.")]
        public bool showPoolingWarning = true;
        [Tooltip("Enables the processing of Pooling logic.")]
        public bool usePooling;
        [Tooltip("Transform that pooling distances will be checked against.")]
        public Transform centerPoint;//交通流的中心点
        [Tooltip("When using pooling, cars will not spawn to a route if the route limit is met.")]
        public bool useRouteLimit;
        [Tooltip("Max amount of cars placed in the pooling system on scene start.")]
        public int carsInPool = 200;
        [Tooltip("Max amount of cars the pooling system is allowed to spawn, must be equal or lower than cars in pool.")]
        public int density = 200;
        [Tooltip("Frequency at which pooling spawn is performed.")]
        public float spawnRate = 2;//每秒最多生成几辆车
        [Tooltip("The position that cars are sent to when being disabled.")]
        public Vector3 disabledPosition = new Vector3(0, -2000, 0);
        [Tooltip("Cars can't spawn or despawn in this zone.")]
        public float minSpawnZone = 50;
        [Tooltip("Car headlights will be disabled outside of this zone.")]
        public float cullHeadLight = 100;
        [Tooltip("Cars only spawn if the spawn point is not visible by the camera.")]
        public float actizeZone = 225;
        [Tooltip("Cars can spawn anywhere in this zone, even if spawn point is visible by the camera. Cars outside of this zone will be despawned.")]
        public float spawnZone = 350;//这些区域相关逻辑脚本在DistanceJob里，最后功能执行还是回来调用Controller的API
        #endregion
        //交通池的公共变量        
        #region Set Array Data
        public void Set_IsDrivingArray(int _index, bool _value)//设置车辆行驶状态的队列
        {
            if (isDrivingNL[_index] != _value)//如果车辆状态和value不对应
            {
                isBrakingNL[_index] = _value == true ? false : true;//？：条件运算符（？前满足执行：前，否执行：后）
                isDrivingNL[_index] = _value;//这里一组互斥逻辑，让行驶参数和刹车参数必定相异
                if (_value == false)
                {
                    motorTorqueNL[_index] = 0;
                    brakeTorqueNL[_index] = brakePowerNL[_index];
                    moveHandBrakeNL[_index] = 1;
                    for (int j = 0; j < 4; j++) // 控制四个轮子的运动状态，update执行的区域还有一个一模一样的（为什么这里要写这个？）
                    {
                        if (j == 0)
                        {
                            currentWheelCollider = frontRightWheelColliderList[_index];
                            currentWheelCollider.steerAngle = steerAngleNL[_index];
                            currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                            FRwheelPositionNL[_index] = wheelPosition_Cached;
                            FRwheelRotationNL[_index] = wheelQuaternion_Cached;
                        }
                        else if (j == 1)
                        {
                            currentWheelCollider = frontLefttWheelColliderList[_index];
                            currentWheelCollider.steerAngle = steerAngleNL[_index];
                            currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                            FLwheelPositionNL[_index] = wheelPosition_Cached;
                            FLwheelRotationNL[_index] = wheelQuaternion_Cached;
                        }
                        else if (j == 2)
                        {
                            currentWheelCollider = backRighttWheelColliderList[_index];
                            currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                            BRwheelPositionNL[_index] = wheelPosition_Cached;
                            BRwheelRotationNL[_index] = wheelQuaternion_Cached;
                        }
                        else if (j == 3)
                        {
                            currentWheelCollider = backLeftWheelColliderList[_index];
                            currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                            BLwheelPositionNL[_index] = wheelPosition_Cached;
                            BLwheelRotationNL[_index] = wheelQuaternion_Cached;
                        }
                        currentWheelCollider.motorTorque = motorTorqueNL[_index];
                        currentWheelCollider.brakeTorque = brakeTorqueNL[_index];
                    }
                }
            }
        }
        //以下几个关于Waypoint的函数在AICar里被调用，用于将分配到各车上的道路信息（点位、限速等）发送给车辆
        //如果要在WayPoint里添加新变量，需要在此写一个新的Set函数，再在AICar里的相应位置引用
        //如果在AICar里添加新变量则不必要，有bug再加
        public void Set_RouteInfo(int _index, AITrafficWaypointRouteInfo routeInfo)
        {
            carAIWaypointRouteInfo[_index] = routeInfo;
        }//各车路线信息标记
        public void Set_CurrentRoutePointIndexArray(int _index, int _value, AITrafficWaypoint _nextWaypoint)
        {
            currentRoutePointIndexNL[_index] = _value;
            currentWaypointList[_index] = _nextWaypoint;
            isChangingLanesNL[_index] = false;
        }//设置各车当前通过路径点的队列
        public void Set_RouteProgressArray(int _index, float _value)
        {
            routeProgressNL[_index] = _value;
        }//
        public void Set_SpeedLimitArray(int _index, float _value)
        {
            speedLimitNL[_index] = _value;
        }//设置道路限速
        public void Set_AverageSpeed(int _index, float _value)
        {
            averagespeedNL[_index] = _value;
        }//设置平均速度（在AITrafficWayPoint里定义，在AICar里被调用）
        public void Set_Sigma(int _index, float _value)
        {
            sigmaNL[_index] = _value;
        }//设置速度方差
        public void Set_WaypointDataListCountArray(int _index)
        {
            waypointDataListCountNL[_index] = carRouteList[_index].waypointDataList.Count;
        }
        public void Set_RoutePointPositionArray(int _index)
        {
            routePointPositionNL[_index] = carRouteList[_index].waypointDataList[currentRoutePointIndexNL[_index]]._transform.position;
            finalRoutePointPositionNL[_index] = carRouteList[_index].waypointDataList[carRouteList[_index].waypointDataList.Count - 1]._transform.position;
        }
        public void SetVisibleState(int _index, bool _isVisible)
        {
            if (isVisibleNL.IsCreated) isVisibleNL[_index] = _isVisible;
        }
        public void Set_WaypointRoute(int _index, AITrafficWaypointRoute _route)
        {
            carRouteList[_index] = _route;
        }//设置路线，换道时调用
        public void Set_CanProcess(int _index, bool _value)
        {
            canProcessNL[_index] = _value;
        }
        public void SetTopSpeed(int _index, float _value)
        {
            topSpeedNL[_index] = _value;
        }
        public void SetForceLaneChange(int _index, bool _value)
        {
            forceChangeLanesNL[_index] = _value;
        }//强制换道
        public void SetChangeToRouteWaypoint(int _index, AITrafficWaypointSettings _onReachWaypointSettings)
        {
            carList[_index].ChangeToRouteWaypoint(_onReachWaypointSettings);
            isChangingLanesNL[_index] = true;
            canChangeLanesNL[_index] = false;
            forceChangeLanesNL[_index] = false;
            changeLaneTriggerTimer[_index] = 0f;
        }//设置换道状态
        #endregion
        //可以被调用的函数
        #region Get Array Data
        public float GetAccelerationInput(int _index)
        {
            return accelerationInputNL[_index];
        }
        public float GetSteeringInput(int _index)
        {
            return steerAngleNL[_index];
        }
        public float GetCurrentSpeed(int _index)
        {
            return speedNL[_index];
        }
        public bool GetIsBraking(int _index)
        {
            return isBrakingNL[_index];
        }
        public bool IsLeftSensor(int _index)
        {
            return leftHitNL[_index];
        }
        public bool IsRightSensor(int _index)
        {
            return rightHitNL[_index];
        }
        public bool IsFrontSensor(int _index)
        {
            return frontHitNL[_index];
        }
        public bool GetIsDisabled(int _index)
        {
            return isDisabledNL[_index];
        }
        public Vector3 GetFrontSensorPosition(int _index)
        {
            return frontSensorTransformPositionNL[_index];
        }
        public Vector3 GetCarPosition(int _index)
        {
            return carTransformPositionNL[_index];
        }
        public Vector3 GetCarTargetPosition(int _index)
        {
            return driveTargetTAA[_index].position;
        }
        public AITrafficWaypointRoute GetCarRoute(int _index)
        {
            return carRouteList[_index];
        }
        public AITrafficCar[] GetTrafficCars()
        {
            return carList.ToArray();
        }
        public AITrafficWaypointRoute[] GetRoutes()
        {
            return allWaypointRoutesList.ToArray();
        }
        public AITrafficSpawnPoint[] GetSpawnPoints()
        {
            return trafficSpawnPoints.ToArray();
        }
        public AITrafficWaypoint GetCurrentWaypoint(int _index)
        {
            return currentWaypointList[_index];
        }
        public AITrafficWaypoint GetNextWaypoint(int _index)
        {
            try
            {
                var next=currentWaypointList[_index].onReachWaypointSettings.nextPointInRoute;
            }
            catch(NullReferenceException e)
            {
                return null;
            }
            return currentWaypointList[_index].onReachWaypointSettings.nextPointInRoute;
        }
        public int GetPossibleDirection(Transform _from, Transform _to)
        {
            return PossibleTargetDirection(_from,_to);
        }//新加的，让外部函数可以访问PossibleTargetDirection（）
        #endregion
        //可被读取的数据
        #region Registers
        public int RegisterCarAI(AITrafficCar carAI, AITrafficWaypointRoute route)
        {
            //添加数组的元素，AICar里的变量需要有引用
            //所有关联脚本里添加的新变量都要在此构造数组
            carList.Add(carAI);
            carRouteList.Add(route);
            currentWaypointList.Add(null);
            changeLaneCooldownTimer.Add(0);
            changeLaneTriggerTimer.Add(0);
            frontDirectionList.Add(Vector3.zero);
            frontRotationList.Add(Quaternion.identity);
            frontTransformCached.Add(carAI.frontSensorTransform);
            frontHitTransform.Add(null);
            frontPreviousHitTransform.Add(null);
            leftOriginList.Add(Vector3.zero);
            leftDirectionList.Add(Vector3.zero);
            leftRotationList.Add(Quaternion.identity);
            leftTransformCached.Add(carAI.leftSensorTransform);
            leftHitTransform.Add(null);
            leftPreviousHitTransform.Add(null);
            rightOriginList.Add(Vector3.zero);
            rightDirectionList.Add(Vector3.zero);
            rightRotationList.Add(Quaternion.identity);
            rightTransformCached.Add(carAI.rightSensorTransform);
            rightHitTransform.Add(null);
            rightPreviousHitTransform.Add(null);
            carAIWaypointRouteInfo.Add(null);
            if (carAI.brakeMaterial == null)
            {
                brakeMaterial.Add(unassignedBrakeMaterial);
            }
            else
            {
                brakeMaterial.Add(carAI.brakeMaterial);
                carAI.brakeMaterial.EnableKeyword("_EMISSION");
            }
            frontRightWheelColliderList.Add(carAI._wheels[0].collider);
            frontLefttWheelColliderList.Add(carAI._wheels[1].collider);
            backRighttWheelColliderList.Add(carAI._wheels[2].collider);
            backLeftWheelColliderList.Add(carAI._wheels[3].collider);//车轮碰撞器添加顺序是固定的
            Rigidbody rigidbody = carAI.GetComponent<Rigidbody>();
            rigidbodyList.Add(rigidbody);
            headLight.Add(carAI.headLight);
            Transform driveTarget = new GameObject("DriveTarget").transform;
            driveTarget.SetParent(carAI.transform);
            TransformAccessArray temp_driveTargetTAA = new TransformAccessArray(carCount);
            for (int i = 0; i < carCount; i++)
            {
                temp_driveTargetTAA.Add(driveTargetTAA[i]);
            }
            temp_driveTargetTAA.Add(driveTarget);
            carCount = carList.Count;
            if (carCount >= 2)
            {
                DisposeArrays(false);
            }
            #region allocation
            //本地列表的数组构造，本地列表（NativeList）是在Job多线程作业中用于临时保存变量的容器
            //在Job多线程作业中，为了防止线程竞争及保护变量数据，主线程使用的是集中保存的原变量，分线程使用的变量是原变量的副本，分别装在本地列表（NativeList）和本地队列（NativeListArray）中
            //所有涉及到Job多线程的的新变量都要在此构造数组
            currentRoutePointIndexNL.Add(0);
            waypointDataListCountNL.Add(0);
            carTransformPreviousPositionNL.Add(Vector3.zero);
            carTransformPositionNL.Add(Vector3.zero);
            finalRoutePointPositionNL.Add(float3.zero);
            routePointPositionNL.Add(float3.zero);
            forceChangeLanesNL.Add(false);
            isChangingLanesNL.Add(false);
            canChangeLanesNL.Add(true);
            isDrivingNL.Add(true);
            isActiveNL.Add(true);
            speedNL.Add(0);
            routeProgressNL.Add(0);
            targetSpeedNL.Add(0);
            accelNL.Add(0);
            speedLimitNL.Add(0);
            averagespeedNL.Add(0);
            sigmaNL.Add(0);
            targetAngleNL.Add(0);
            dragNL.Add(0);
            angularDragNL.Add(0);
            overrideDragNL.Add(false);
            localTargetNL.Add(Vector3.zero);
            steerAngleNL.Add(0);
            motorTorqueNL.Add(0);
            accelerationInputNL.Add(0);
            brakeTorqueNL.Add(0);
            moveHandBrakeNL.Add(0);
            overrideInputNL.Add(false);
            distanceToEndPointNL.Add(999);
            overrideAccelerationPowerNL.Add(0);
            overrideBrakePowerNL.Add(0);
            frontspeedNL.Add(0);
            isBrakingNL.Add(false);
            FRwheelPositionNL.Add(float3.zero);
            FRwheelRotationNL.Add(Quaternion.identity);
            FLwheelPositionNL.Add(float3.zero);
            FLwheelRotationNL.Add(Quaternion.identity);
            BRwheelPositionNL.Add(float3.zero);
            BRwheelRotationNL.Add(Quaternion.identity);
            BLwheelPositionNL.Add(float3.zero);
            BLwheelRotationNL.Add(Quaternion.identity);
            frontSensorLengthNL.Add(carAI.frontSensorLength);
            frontSensorSizeNL.Add(carAI.frontSensorSize);
            sideSensorLengthNL.Add(carAI.sideSensorLength);
            sideSensorSizeNL.Add(carAI.sideSensorSize);
            frontSensorTransformPositionNL.Add(carAI.frontSensorTransform.position);
            previousFrameSpeedNL.Add(0f);
            brakeTimeNL.Add(0f);
            topSpeedNL.Add(carAI.topSpeed);
            minDragNL.Add(carAI.minDrag);
            minAngularDragNL.Add(carAI.minAngularDrag);
            frontHitDistanceNL.Add(carAI.frontSensorLength);
            leftHitDistanceNL.Add(carAI.sideSensorLength);
            rightHitDistanceNL.Add(carAI.sideSensorLength);
            frontHitNL.Add(false);
            leftHitNL.Add(false);
            rightHitNL.Add(false);
            stopForTrafficLightNL.Add(false);
            yieldForCrossTrafficNL.Add(false);
            routeIsActiveNL.Add(false);
            isVisibleNL.Add(false);
            isDisabledNL.Add(false);
            withinLimitNL.Add(false);
            distanceToPlayerNL.Add(0);
            accelerationPowerNL.Add(carAI.accelerationPower);
            brakePowerNL.Add(carAI.brakePower);
            isEnabledNL.Add(false);
            outOfBoundsNL.Add(false);
            lightIsActiveNL.Add(false);
            canProcessNL.Add(true);
            needHardBrakeNL.Add(false);
            driveTargetTAA = new TransformAccessArray(carCount);
            carTAA = new TransformAccessArray(carCount);
            frontRightWheelTAA = new TransformAccessArray(carCount);
            frontLeftWheelTAA = new TransformAccessArray(carCount);
            backRightWheelTAA = new TransformAccessArray(carCount);
            backLeftWheelTAA = new TransformAccessArray(carCount);
            frontBoxcastCommands = new NativeArray<BoxcastCommand>(carCount, Allocator.Persistent);
            leftBoxcastCommands = new NativeArray<BoxcastCommand>(carCount, Allocator.Persistent);
            rightBoxcastCommands = new NativeArray<BoxcastCommand>(carCount, Allocator.Persistent);
            frontBoxcastResults = new NativeArray<RaycastHit>(carCount, Allocator.Persistent);
            leftBoxcastResults = new NativeArray<RaycastHit>(carCount, Allocator.Persistent);
            rightBoxcastResults = new NativeArray<RaycastHit>(carCount, Allocator.Persistent);
           
            #endregion
            waypointDataListCountNL[carCount - 1] = carRouteList[carCount - 1].waypointDataList.Count;
            carAIWaypointRouteInfo[carCount - 1] = carRouteList[carCount - 1].routeInfo;
            for (int i = 0; i < carCount; i++)
            {
                driveTargetTAA.Add(temp_driveTargetTAA[i]);
                carTAA.Add(carList[i].transform);
                frontRightWheelTAA.Add(carList[i]._wheels[0].meshTransform);
                frontLeftWheelTAA.Add(carList[i]._wheels[1].meshTransform);
                backRightWheelTAA.Add(carList[i]._wheels[2].meshTransform);
                backLeftWheelTAA.Add(carList[i]._wheels[3].meshTransform);
            }//给车分配它的轮子（有点扯，但轮子自身有Job多线程，跟车身不同步，出Bug确实会自己走自己的......）
            temp_driveTargetTAA.Dispose();
            return carCount - 1;
        }
        public int RegisterSpawnPoint(AITrafficSpawnPoint _TrafficSpawnPoint)
        {
            int index = trafficSpawnPoints.Count;
            trafficSpawnPoints.Add(_TrafficSpawnPoint);
            return index;
        }
        public void RemoveSpawnPoint(AITrafficSpawnPoint _TrafficSpawnPoint)
        {
            trafficSpawnPoints.Remove(_TrafficSpawnPoint);
            availableSpawnPoints.Clear();
        }
        public int RegisterAITrafficWaypointRoute(AITrafficWaypointRoute _route)
        {
            int index = allWaypointRoutesList.Count;
            allWaypointRoutesList.Add(_route);
            return index;
        }
        public void RemoveAITrafficWaypointRoute(AITrafficWaypointRoute _route)
        {
            allWaypointRoutesList.Remove(_route);
        }
        #endregion
        //主要是变量数组添加元素的过程（还有路线和出生点的生成）
        #endregion
        //公有变量声明
        #region Private Variables
        private List<AITrafficCar> carList = new List<AITrafficCar>();
        private List<AITrafficWaypointRouteInfo> carAIWaypointRouteInfo = new List<AITrafficWaypointRouteInfo>();
        private List<AITrafficWaypointRoute> allWaypointRoutesList = new List<AITrafficWaypointRoute>();
        private List<AITrafficWaypointRoute> carRouteList = new List<AITrafficWaypointRoute>();
        private List<AITrafficWaypoint> currentWaypointList = new List<AITrafficWaypoint>();
        private List<AITrafficSpawnPoint> trafficSpawnPoints = new List<AITrafficSpawnPoint>();
        private List<AITrafficSpawnPoint> availableSpawnPoints = new List<AITrafficSpawnPoint>();
        private List<WheelCollider> frontRightWheelColliderList = new List<WheelCollider>();
        private List<WheelCollider> frontLefttWheelColliderList = new List<WheelCollider>();
        private List<WheelCollider> backRighttWheelColliderList = new List<WheelCollider>();
        private List<WheelCollider> backLeftWheelColliderList = new List<WheelCollider>();
        private List<Rigidbody> rigidbodyList = new List<Rigidbody>();
        private List<Transform> frontTransformCached = new List<Transform>();
        private List<Transform> frontHitTransform = new List<Transform>();
        private List<Transform> frontPreviousHitTransform = new List<Transform>();
        private List<Transform> leftTransformCached = new List<Transform>();
        private List<Transform> leftHitTransform = new List<Transform>();
        private List<Transform> leftPreviousHitTransform = new List<Transform>();
        private List<Transform> rightTransformCached = new List<Transform>();
        private List<Transform> rightHitTransform = new List<Transform>();
        private List<Transform> rightPreviousHitTransform = new List<Transform>();
        private List<Material> brakeMaterial = new List<Material>();
        private List<Light> headLight = new List<Light>();
        private List<float> changeLaneTriggerTimer = new List<float>();
        private List<float> changeLaneCooldownTimer = new List<float>();
        private List<Vector3> frontDirectionList = new List<Vector3>();
        private List<Vector3> leftOriginList = new List<Vector3>();
        private List<Vector3> leftDirectionList = new List<Vector3>();
        private List<Vector3> rightOriginList = new List<Vector3>();
        private List<Vector3> rightDirectionList = new List<Vector3>();
        private List<Quaternion> leftRotationList = new List<Quaternion>();
        private List<Quaternion> frontRotationList = new List<Quaternion>();
        private List<Quaternion> rightRotationList = new List<Quaternion>();
        private List<AITrafficPoolEntry> trafficPool = new List<AITrafficPoolEntry>();
        private NativeList<int> currentRoutePointIndexNL;
        private NativeList<int> waypointDataListCountNL;
        private NativeList<bool> canProcessNL;
        private NativeList<bool> forceChangeLanesNL;
        private NativeList<bool> isChangingLanesNL;
        private NativeList<bool> canChangeLanesNL;
        private NativeList<bool> frontHitNL;
        private NativeList<bool> leftHitNL;
        private NativeList<bool> rightHitNL;
        private NativeList<bool> needHardBrakeNL;
        private NativeList<bool> yieldForCrossTrafficNL;
        private NativeList<bool> stopForTrafficLightNL;
        private NativeList<bool> routeIsActiveNL;
        private NativeList<bool> isActiveNL;
        private NativeList<bool> isDrivingNL;
        private NativeList<bool> overrideDragNL;
        private NativeList<bool> overrideInputNL;
        private NativeList<bool> isBrakingNL;
        private NativeList<bool> withinLimitNL;
        private NativeList<bool> isEnabledNL;
        private NativeList<bool> outOfBoundsNL;
        private NativeList<bool> lightIsActiveNL;
        private NativeList<bool> isVisibleNL;
        private NativeList<bool> isDisabledNL;
        private NativeList<float> frontHitDistanceNL;
        private NativeList<float> leftHitDistanceNL;
        private NativeList<float> rightHitDistanceNL;
        private NativeList<Vector3> frontSensorTransformPositionNL;
        private NativeList<float> frontSensorLengthNL;
        private NativeList<Vector3> frontSensorSizeNL;
        private NativeList<float> sideSensorLengthNL;
        private NativeList<Vector3> sideSensorSizeNL;
        private NativeList<float> previousFrameSpeedNL;
        private NativeList<float> brakeTimeNL;
        private NativeList<float> topSpeedNL;
        private NativeList<float> minDragNL;
        private NativeList<float> minAngularDragNL;
        private NativeList<float> speedNL;
        private NativeList<float> routeProgressNL;
        private NativeList<float> targetSpeedNL;
        private NativeList<float> accelNL;
        private NativeList<float> speedLimitNL;
        private NativeList<float> averagespeedNL;
        private NativeList<float> sigmaNL;
        private NativeList<float> targetAngleNL;
        private NativeList<float> dragNL;
        private NativeList<float> angularDragNL;
        private NativeList<float> steerAngleNL;
        private NativeList<float> accelerationInputNL;
        private NativeList<float> motorTorqueNL;
        private NativeList<float> brakeTorqueNL;
        private NativeList<float> moveHandBrakeNL;
        private NativeList<float> overrideAccelerationPowerNL;
        private NativeList<float> overrideBrakePowerNL;
        private NativeList<float> distanceToPlayerNL;
        private NativeList<float> accelerationPowerNL;
        private NativeList<float> brakePowerNL;
        private NativeList<float> distanceToEndPointNL;
        private NativeList<float> frontspeedNL;
        private NativeList<float3> finalRoutePointPositionNL;
        private NativeList<float3> routePointPositionNL;
        private NativeList<float3> FRwheelPositionNL;
        private NativeList<float3> FLwheelPositionNL;
        private NativeList<float3> BRwheelPositionNL;
        private NativeList<float3> BLwheelPositionNL;
        private NativeList<Vector3> carTransformPreviousPositionNL;
        private NativeList<Vector3> localTargetNL;
        private NativeList<Vector3> carTransformPositionNL;
        private NativeList<quaternion> FRwheelRotationNL;
        private NativeList<quaternion> FLwheelRotationNL;
        private NativeList<quaternion> BRwheelRotationNL;
        private NativeList<quaternion> BLwheelRotationNL;
        private TransformAccessArray driveTargetTAA;
        private TransformAccessArray carTAA;
        private TransformAccessArray frontRightWheelTAA;
        private TransformAccessArray frontLeftWheelTAA;
        private TransformAccessArray backRightWheelTAA;
        private TransformAccessArray backLeftWheelTAA;
        private JobHandle jobHandle;
        private AITrafficCarJob carAITrafficJob;
        private AITrafficCarWheelJob frAITrafficCarWheelJob;
        private AITrafficCarWheelJob flAITrafficCarWheelJob;
        private AITrafficCarWheelJob brAITrafficCarWheelJob;
        private AITrafficCarWheelJob blAITrafficCarWheelJob;
        private AITrafficCarPositionJob carTransformpositionJob;
        private AITrafficDistanceJob _AITrafficDistanceJob;
        private float3 centerPosition;
        private float spawnTimer;
        private float distanceToSpawnPoint;
        private float startTime;
        private float deltaTime;
        private float dragToAdd;
        private int currentAmountToSpawn;
        private int randomSpawnPointIndex;
        private bool canTurnLeft, canTurnRight;
        private bool isInitialized;
        private Vector3 relativePoint;
        private Vector3 wheelPosition_Cached;
        private Vector3 spawnPosition;
        private Vector3 spawnOffset = new Vector3(0, -4, 0);
        private Vector3 frontSensorEulerAngles;
        private Quaternion wheelQuaternion_Cached;
        private RaycastHit boxHit;
        private WheelCollider currentWheelCollider;
        private AITrafficCar spawncar;
        private AITrafficCar loadCar;
        private AITrafficWaypoint nextWaypoint;
        private AITrafficPoolEntry newTrafficPoolEntry = new AITrafficPoolEntry();
        private Material[] materialsb;
        private Material[] materialsy;
        private Material[] materialsg;
        private GetLicense cars;
        private List<Material> materiallistb = new List<Material>();
        private List<Material> materiallisty = new List<Material>();
        private List<Material> materiallistg = new List<Material>();

        NativeArray<RaycastHit> frontBoxcastResults;
        NativeArray<RaycastHit> leftBoxcastResults;
        NativeArray<RaycastHit> rightBoxcastResults;
        NativeArray<BoxcastCommand> frontBoxcastCommands;
        NativeArray<BoxcastCommand> leftBoxcastCommands;
        NativeArray<BoxcastCommand> rightBoxcastCommands;

        private int PossibleTargetDirection(Transform _from, Transform _to)
        {
            relativePoint = _from.InverseTransformPoint(_to.position);//把to的坐标从世界坐标系变到from的本地坐标系下
            if (relativePoint.x < 0.0) return -1;//左边
            else if (relativePoint.x > 0.0) return 1;//右边
            else return 0;
        }//判断to在from的哪一侧，往该侧转向
        #endregion
        //私有变量声明，包括Unity物体、组件的实例化
        #region Main Methods
        private void OnEnable()//脚本启用时执行：（本地列表的）变量实例化
        {
            materialsg = Resources.LoadAll<Material>("LicenseMaterial/green");
            materialsy = Resources.LoadAll<Material>("LicenseMaterial/yellow");
            materialsb = Resources.LoadAll<Material>("LicenseMaterial/blue");//从特定文件夹里获取车牌贴图材质（Resources文件名不要改，这是unity自带的可以直接读取材质的文件夹），生成数组
            materialsb = GetDisruptedItems(materialsb);//打乱数组元素，使发放车牌过程成为伪随机过程
            materialsg = GetDisruptedItems(materialsg);
            materialsy = GetDisruptedItems(materialsy);
            if (Instance == null)
            {
                //启动分配器，保存在本地列表里的变量需要通过分配器调用
                Instance = this;
                currentRoutePointIndexNL = new NativeList<int>(Allocator.Persistent);
                waypointDataListCountNL = new NativeList<int>(Allocator.Persistent);
                carTransformPreviousPositionNL = new NativeList<Vector3>(Allocator.Persistent);
                carTransformPositionNL = new NativeList<Vector3>(Allocator.Persistent);
                finalRoutePointPositionNL = new NativeList<float3>(Allocator.Persistent);
                routePointPositionNL = new NativeList<float3>(Allocator.Persistent);
                forceChangeLanesNL = new NativeList<bool>(Allocator.Persistent);
                isChangingLanesNL = new NativeList<bool>(Allocator.Persistent);
                canChangeLanesNL = new NativeList<bool>(Allocator.Persistent);
                isDrivingNL = new NativeList<bool>(Allocator.Persistent);
                isActiveNL = new NativeList<bool>(Allocator.Persistent);
                canProcessNL = new NativeList<bool>(Allocator.Persistent);
                speedNL = new NativeList<float>(Allocator.Persistent);
                routeProgressNL = new NativeList<float>(Allocator.Persistent);
                targetSpeedNL = new NativeList<float>(Allocator.Persistent);
                accelNL = new NativeList<float>(Allocator.Persistent);
                speedLimitNL = new NativeList<float>(Allocator.Persistent);
                averagespeedNL = new NativeList<float>(Allocator.Persistent);
                sigmaNL = new NativeList<float>(Allocator.Persistent);
                targetAngleNL = new NativeList<float>(Allocator.Persistent);
                dragNL = new NativeList<float>(Allocator.Persistent);
                angularDragNL = new NativeList<float>(Allocator.Persistent);
                overrideDragNL = new NativeList<bool>(Allocator.Persistent);
                localTargetNL = new NativeList<Vector3>(Allocator.Persistent);
                steerAngleNL = new NativeList<float>(Allocator.Persistent);
                motorTorqueNL = new NativeList<float>(Allocator.Persistent);
                accelerationInputNL = new NativeList<float>(Allocator.Persistent);
                brakeTorqueNL = new NativeList<float>(Allocator.Persistent);
                moveHandBrakeNL = new NativeList<float>(Allocator.Persistent);
                overrideInputNL = new NativeList<bool>(Allocator.Persistent);
                distanceToEndPointNL = new NativeList<float>(Allocator.Persistent);
                overrideAccelerationPowerNL = new NativeList<float>(Allocator.Persistent);
                overrideBrakePowerNL = new NativeList<float>(Allocator.Persistent);
                isBrakingNL = new NativeList<bool>(Allocator.Persistent);
                FRwheelPositionNL = new NativeList<float3>(Allocator.Persistent);
                FRwheelRotationNL = new NativeList<quaternion>(Allocator.Persistent);
                FLwheelPositionNL = new NativeList<float3>(Allocator.Persistent);
                FLwheelRotationNL = new NativeList<quaternion>(Allocator.Persistent);
                BRwheelPositionNL = new NativeList<float3>(Allocator.Persistent);
                BRwheelRotationNL = new NativeList<quaternion>(Allocator.Persistent);
                BLwheelPositionNL = new NativeList<float3>(Allocator.Persistent);
                BLwheelRotationNL = new NativeList<quaternion>(Allocator.Persistent);
                previousFrameSpeedNL = new NativeList<float>(Allocator.Persistent);
                brakeTimeNL = new NativeList<float>(Allocator.Persistent);
                topSpeedNL = new NativeList<float>(Allocator.Persistent);
                frontSensorTransformPositionNL = new NativeList<Vector3>(Allocator.Persistent);
                frontSensorLengthNL = new NativeList<float>(Allocator.Persistent);
                frontSensorSizeNL = new NativeList<Vector3>(Allocator.Persistent);
                sideSensorLengthNL = new NativeList<float>(Allocator.Persistent);
                sideSensorSizeNL = new NativeList<Vector3>(Allocator.Persistent);
                minDragNL = new NativeList<float>(Allocator.Persistent);
                minAngularDragNL = new NativeList<float>(Allocator.Persistent);
                frontHitDistanceNL = new NativeList<float>(Allocator.Persistent);
                leftHitDistanceNL = new NativeList<float>(Allocator.Persistent);
                rightHitDistanceNL = new NativeList<float>(Allocator.Persistent);
                frontHitNL = new NativeList<bool>(Allocator.Persistent);
                leftHitNL = new NativeList<bool>(Allocator.Persistent);
                rightHitNL = new NativeList<bool>(Allocator.Persistent);
                stopForTrafficLightNL = new NativeList<bool>(Allocator.Persistent);
                yieldForCrossTrafficNL = new NativeList<bool>(Allocator.Persistent);
                routeIsActiveNL = new NativeList<bool>(Allocator.Persistent);
                isVisibleNL = new NativeList<bool>(Allocator.Persistent);
                isDisabledNL = new NativeList<bool>(Allocator.Persistent);
                withinLimitNL = new NativeList<bool>(Allocator.Persistent);
                distanceToPlayerNL = new NativeList<float>(Allocator.Persistent);
                accelerationPowerNL = new NativeList<float>(Allocator.Persistent);
                brakePowerNL = new NativeList<float>(Allocator.Persistent);
                isEnabledNL = new NativeList<bool>(Allocator.Persistent);
                outOfBoundsNL = new NativeList<bool>(Allocator.Persistent);
                lightIsActiveNL = new NativeList<bool>(Allocator.Persistent);
                frontspeedNL = new NativeList<float>(Allocator.Persistent);
                needHardBrakeNL = new NativeList<bool>(Allocator.Persistent);
            }
            else
            {
                Debug.LogWarning("Multiple AITrafficController Instances found in scene, this is not allowed. Destroying this duplicate AITrafficController.");
                Destroy(this);
            }
        }

        private void Start()//脚本开始时执行：一些基础逻辑变量和物理、渲染属性赋值；生成第一批车
        {
            if (usePooling)
            {
                StartCoroutine(SpawnStartupTrafficCoroutine());//开启协程，生成第一批Spawn车
                if (showPoolingWarning)
                {
                    Debug.LogWarning("NOTE: " +
                        "OnBecameVisible and OnBecameInvisible are used by cars and spawn points to determine if they are visible.\n" +
                        "These callbacks are also triggered by the editor scene camera.\n" +
                        "Hide the scene view while testing for the most accurate simulation, which is what the final build will be.\n" +
                        "Not hiding the scene view camera may cause objcets to register the wrong state, resulting in unproper behavior.");
                }
            }
            else
            {
                StartCoroutine(Initialize());//同上
            }
            
            lowSidewaysWheelFrictionCurve.extremumSlip = 0.2f;
            lowSidewaysWheelFrictionCurve.extremumValue = 1f;
            lowSidewaysWheelFrictionCurve.asymptoteSlip = 0.5f;
            lowSidewaysWheelFrictionCurve.asymptoteValue = 0.75f;
            lowSidewaysWheelFrictionCurve.stiffness = 1f;
            highSidewaysWheelFrictionCurve.extremumSlip = 0.2f;
            highSidewaysWheelFrictionCurve.extremumValue = 1f;
            highSidewaysWheelFrictionCurve.asymptoteSlip = 0.5f;
            highSidewaysWheelFrictionCurve.asymptoteValue = 0.75f;
            highSidewaysWheelFrictionCurve.stiffness = 5f;
            brakeIntensityFactor = Mathf.Pow(2, RenderPipeline.IsDefaultRP ? brakeOnIntensityDP : RenderPipeline.IsURP ? brakeOnIntensityURP : brakeOnIntensityHDRP);
            brakeOnColor = new Color(brakeColor.r * brakeIntensityFactor, brakeColor.g * brakeIntensityFactor, brakeColor.b * brakeIntensityFactor);
            brakeIntensityFactor = Mathf.Pow(2, RenderPipeline.IsDefaultRP ? brakeOffIntensityDP : RenderPipeline.IsURP ? brakeOffIntensityURP : brakeOffIntensityHDRP);
            brakeOffColor = new Color(brakeColor.r * brakeIntensityFactor, brakeColor.g * brakeIntensityFactor, brakeColor.b * brakeIntensityFactor);
            emissionColorName = RenderPipeline.IsDefaultRP || RenderPipeline.IsURP ? "_EmissionColor" : "_EmissiveColor";
            unassignedBrakeMaterial = new Material(unassignedBrakeMaterial);//摩擦和刹车相关,前面的部分是调整侧向摩擦滑移曲线，后面为了是支持不同版本的刹车灯渲染管线
        }

        IEnumerator Initialize()//协程：给车算起终点，发送启动命令
        {
            yield return new WaitForSeconds(1f);//等一秒再执行下面的语句
            for (int i = 0; i < carCount; i++)
            {
                routePointPositionNL[i] = carRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;//现在所在点的位置
                finalRoutePointPositionNL[i] = carRouteList[i].waypointDataList[carRouteList[i].waypointDataList.Count - 1]._transform.position;//最后一个点的位置
                carList[i].StartDriving();
            }
            if (setCarParent)
            {
                if (carParent == null) carParent = transform;
                for (int i = 0; i < carCount; i++)
                {
                    carList[i].transform.SetParent(carParent);
                }
            }
            isInitialized = true;
        }

        private void FixedUpdate()//每固定时间执行：主逻辑
        {
            if (isInitialized)
            {
                if (STSPrefs.debugProcessTime) startTime = Time.realtimeSinceStartup;
                deltaTime = Time.deltaTime;
                if (useYieldTriggers)
                {
                    for (int i = 0; i < carCount; i++)
                    {
                        yieldForCrossTrafficNL[i] = false;
                        if (currentWaypointList[i] != null)
                        {
                            if (currentWaypointList[i].onReachWaypointSettings.nextPointInRoute != null)
                            {
                                for (int j = 0; j < currentWaypointList[i].onReachWaypointSettings.nextPointInRoute.onReachWaypointSettings.yieldTriggers.Count; j++)
                                {
                                    if (currentWaypointList[i].onReachWaypointSettings.nextPointInRoute.onReachWaypointSettings.yieldTriggers[j].yieldForTrafficLight == true)
                                    {
                                        yieldForCrossTrafficNL[i] = true;
                                        break;
                                    }
                                }
                            }
                        }
                        stopForTrafficLightNL[i] = carAIWaypointRouteInfo[i].stopForTrafficLight;
                    }
                }
                else
                {
                    for (int i = 0; i < carCount; i++)
                    {
                        yieldForCrossTrafficNL[i] = false;
                        stopForTrafficLightNL[i] = carAIWaypointRouteInfo[i].stopForTrafficLight;
                        //frontSensorTransformPositionNL[i] = frontTransformCached[i].position; // make a job?
                    }
                }//yieldtrigger功能（还没用）

                carAITrafficJob = new AITrafficCarJob//多线程实例化，并把本地列表里的变量值赋给本地队列以供子线程使用
                {
                    frontSensorLengthNA = frontSensorLengthNL,
                    currentRoutePointIndexNA = currentRoutePointIndexNL,
                    waypointDataListCountNA = waypointDataListCountNL,
                    carTransformPreviousPositionNA = carTransformPreviousPositionNL,
                    carTransformPositionNA = carTransformPositionNL,
                    finalRoutePointPositionNA = finalRoutePointPositionNL,
                    routePointPositionNA = routePointPositionNL,
                    isDrivingNA = isDrivingNL,
                    isActiveNA = isActiveNL,
                    canProcessNA = canProcessNL,
                    speedNA = speedNL,
                    deltaTime = deltaTime,
                    routeProgressNA = routeProgressNL,
                    topSpeedNA = topSpeedNL,
                    targetSpeedNA = targetSpeedNL,
                    speedLimitNA = speedLimitNL,
                    averagespeedNA = averagespeedNL,
                    sigmaNA=sigmaNL,
                    accelNA = accelNL,
                    localTargetNA = localTargetNL,
                    targetAngleNA = targetAngleNL,
                    steerAngleNA = steerAngleNL,
                    motorTorqueNA = motorTorqueNL,
                    accelerationInputNA = accelerationInputNL,
                    brakeTorqueNA = brakeTorqueNL,
                    moveHandBrakeNA = moveHandBrakeNL,
                    maxSteerAngle = maxSteerAngle,
                    overrideInputNA = overrideInputNL,
                    distanceToEndPointNA = distanceToEndPointNL,
                    overrideAccelerationPowerNA = overrideAccelerationPowerNL,
                    overrideBrakePowerNA = overrideBrakePowerNL,
                    isBrakingNA = isBrakingNL,
                    speedMultiplier = speedMultiplier,
                    steerSensitivity = steerSensitivity,
                    stopThreshold = stopThreshold,
                    frontHitDistanceNA = frontHitDistanceNL,
                    frontHitNA = frontHitNL,
                    leftHitNA = leftHitNL,
                    rightHitNA = rightHitNL,
                    stopForTrafficLightNA = stopForTrafficLightNL,
                    yieldForCrossTrafficNA = yieldForCrossTrafficNL,
                    accelerationPowerNA = accelerationPowerNL,
                    brakePowerNA = brakePowerNL,
                    frontSensorTransformPositionNA = frontSensorTransformPositionNL,
                    needHardBrakeNA = needHardBrakeNL,
                    frontspeedNA = frontspeedNL,
                };
                jobHandle = carAITrafficJob.Schedule(driveTargetTAA);
                jobHandle.Complete();

                for (int i = 0; i < carCount; i++) // operate on results
                {
                    //正态化速度，写在这里能让实际运行的车辆速度正态分布，写在RandomSpeed只能正态化限速而不是真实速度
                    float checkNum;
                    float x;
                    float n;
                    float range = sigmaNL[i] * 3f;//剔除了3σ外的波动
                    do
                    {
                        x = UnityEngine.Random.Range(averagespeedNL[i] - range, averagespeedNL[i] + range);//在范围内取随机数
                        n = 1.0f / (Mathf.Sqrt(2f * Mathf.PI) * sigmaNL[i]) * Mathf.Exp(-1f * (x - averagespeedNL[i]) * (x - averagespeedNL[i]) / (2f * sigmaNL[i] * sigmaNL[i]));//所获得随机数的正态密度函数
                        checkNum = UnityEngine.Random.Range(0, 1.0f / (Mathf.Sqrt(2f * Mathf.PI) * sigmaNL[i]));//获得该正态分布的最大单位密度,在该区间内抽样
                    } while (checkNum > n);
                    targetSpeedNL[i] = x;//当抽样结果满足概率检验时，返回随机数
                    /// Front Sensor
                    if (frontSensorFacesTarget)
                    {
                        if (currentWaypointList[i])
                        {
                            frontTransformCached[i].LookAt(currentWaypointList[i].onReachWaypointSettings.nextPointInRoute.transform);
                            frontSensorEulerAngles = frontTransformCached[i].rotation.eulerAngles;
                            frontSensorEulerAngles.x = 0;
                            frontSensorEulerAngles.z = 0;
                            frontTransformCached[i].rotation = Quaternion.Euler(frontSensorEulerAngles);
                        }
                    }
                    frontSensorTransformPositionNL[i] = frontTransformCached[i].position;
                    frontDirectionList[i] = frontTransformCached[i].forward;
                    frontRotationList[i] = frontTransformCached[i].rotation;
                    frontBoxcastCommands[i] = new BoxcastCommand(frontSensorTransformPositionNL[i], frontSensorSizeNL[i], frontRotationList[i], frontDirectionList[i], frontSensorLengthNL[i], layerMask);
                    //射线盒检测，跟射线检测类似，但射出的是盒状体；BoxcastCommand用于一组射线盒检测，ScheduleBatch是用于采用多线程分批次处理
                    if (useLaneChanging)
                    {
                        if (speedNL[i] > minSpeedToChangeLanes)
                        {
                            /*if ((forceChangeLanesNL[i] == true || frontHitNL[i] == true) && canChangeLanesNL[i] && isChangingLanesNL[i] == false)
                            {

                            }*/
                            leftOriginList[i] = leftTransformCached[i].position;
                            leftDirectionList[i] = leftTransformCached[i].forward;
                            leftRotationList[i] = leftTransformCached[i].rotation;
                            leftBoxcastCommands[i] = new BoxcastCommand(leftOriginList[i], sideSensorSizeNL[i], leftRotationList[i], leftDirectionList[i], sideSensorLengthNL[i], layerMask);

                            rightOriginList[i] = rightTransformCached[i].position;
                            rightDirectionList[i] = rightTransformCached[i].forward;
                            rightRotationList[i] = rightTransformCached[i].rotation;
                            rightBoxcastCommands[i] = new BoxcastCommand(rightOriginList[i], sideSensorSizeNL[i], rightRotationList[i], rightDirectionList[i], sideSensorLengthNL[i], layerMask);
                        }
                    }//如果要换道，车两侧发起射线盒检测（射线盒的尺寸是影响换道的一个原因）
                }
                var handle = BoxcastCommand.ScheduleBatch(frontBoxcastCommands, frontBoxcastResults, 1, default);
                handle.Complete();
                handle = BoxcastCommand.ScheduleBatch(leftBoxcastCommands, leftBoxcastResults, 1, default);
                handle.Complete();
                handle = BoxcastCommand.ScheduleBatch(rightBoxcastCommands, rightBoxcastResults, 1, default);
                handle.Complete();//打开传感器的多线程
                for (int i = 0; i < carCount; i++) 
                {
                    // front
                    frontHitNL[i] = frontBoxcastResults[i].collider == null ? false : true;
                    if (frontHitNL[i])
                    {
                        frontHitTransform[i] = frontBoxcastResults[i].transform; 
                        if (frontHitTransform[i] != frontPreviousHitTransform[i])
                        {
                            frontPreviousHitTransform[i] = frontHitTransform[i];
                        }
                        frontHitDistanceNL[i] = frontBoxcastResults[i].distance;
                        if (frontBoxcastResults[i].collider.gameObject.GetComponent<Rigidbody>() != null)
                        {
                            frontspeedNL[i] = frontBoxcastResults[i].collider.gameObject.GetComponent<Rigidbody>().velocity.magnitude;
                        }
                        else frontspeedNL[i] = 0;
                    }//
                    else //ResetHitBox
                    {
                        frontHitDistanceNL[i] = frontSensorLengthNL[i];
                    }
                    // left
                    leftHitNL[i] = leftBoxcastResults[i].collider == null ? false : true;
                    if (leftHitNL[i])
                    {
                        leftHitTransform[i] = boxHit.transform; // cache transform lookup
                        if (leftHitTransform[i] != leftPreviousHitTransform[i])
                        {
                            leftPreviousHitTransform[i] = leftHitTransform[i];
                        }
                        leftHitDistanceNL[i] = boxHit.distance;
                        
                    }
                    else //ResetHitBox
                    {
                        leftHitDistanceNL[i] = sideSensorLengthNL[i];
                    }
                    // right
                    rightHitNL[i] = rightBoxcastResults[i].collider == null ? false : true;
                    if (rightHitNL[i])
                    {
                        rightHitTransform[i] = boxHit.transform; // cache transform lookup
                        if (rightHitTransform[i] != rightPreviousHitTransform[i])
                        {
                            rightPreviousHitTransform[i] = rightHitTransform[i];
                        }
                        rightHitDistanceNL[i] = boxHit.distance;
                    }
                    else //ResetHitBox
                    {
                        rightHitDistanceNL[i] = sideSensorLengthNL[i];
                    }
                }//这一段是设置射线盒检测检车前方障碍物，把检测结果存进分批执行的变量里（能不能顺便保存前车车速，然后根据速度差设置逻辑？）
                
                for (int i = 0; i < carCount; i++) // operate on results
                {
                    if (isActiveNL[i] && canProcessNL[i])
                    {
                        #region Lane Change
                        if (useLaneChanging && isDrivingNL[i])//换道判断1：是否采用换道策略及是否在驾驶
                        {
                            if (speedNL[i] > minSpeedToChangeLanes)//换道判断2：是否超过了最小换道速度
                            {
                                if (!canChangeLanesNL[i])//换道判断3：是否还在换道冷却时间内（不在就读秒，够了就发出能换道指令&重置冷却）
                                {
                                    changeLaneCooldownTimer[i] += deltaTime;
                                    if (changeLaneCooldownTimer[i] > changeLaneCooldown)
                                    {
                                        canChangeLanesNL[i] = true;
                                        changeLaneCooldownTimer[i] = 0f;
                                    }
                                }

                                if ((forceChangeLanesNL[i] == true || frontHitNL[i] == true) && canChangeLanesNL[i] && isChangingLanesNL[i] == false)//换道判断4：（强制换道或前方检测障碍物）且能换道且目前不在换道
                                {
                                    changeLaneTriggerTimer[i] += Time.deltaTime;
                                    canTurnLeft = leftHitNL[i] == true ? false : true;
                                    canTurnRight = rightHitNL[i] == true ? false : true;//判断换道是左转还是右转
                                    //Debug.Log("LaneChangeTrigger1"+","+i);
                                    if (changeLaneTriggerTimer[i] >= changeLaneTrigger || forceChangeLanesNL[i] == true)//换道判断5：强制换道或满足换道触发时间（换道触发时间条件实际上比较苛刻，非跟车换道建议设为0）
                                    {
                                        canChangeLanesNL[i] = false;
                                        nextWaypoint = currentWaypointList[i];
                                        //Debug.Log(i+"LaneChangeTrigger2" + "," + nextWaypoint.onReachWaypointSettings.parentRoute + "," + nextWaypoint.onReachWaypointSettings.waypointIndexnumber);
                                        if (nextWaypoint != null)//换道判断6：下一个路径点不为空
                                        {
                                            //Debug.Log(i+","+"LaneChangeTrigger3" + "," + nextWaypoint.onReachWaypointSettings.parentRoute + "," + nextWaypoint.onReachWaypointSettings.waypoint);
                                            if (nextWaypoint.onReachWaypointSettings.laneChangePoints.Count > 0)  //换道判断7：本路径点的换道点数量大于零
                                            {
                                                for (int j = 0; j < nextWaypoint.onReachWaypointSettings.laneChangePoints.Count; j++)
                                                {
                                                    Debug.Log(PossibleTargetDirection(carTAA[i], nextWaypoint.onReachWaypointSettings.laneChangePoints[j].transform) == -1 && canTurnLeft ||
                                                        PossibleTargetDirection(carTAA[i], nextWaypoint.onReachWaypointSettings.laneChangePoints[j].transform) == 1 && canTurnRight);
                                                    if (
                                                        PossibleTargetDirection(carTAA[i], nextWaypoint.onReachWaypointSettings.laneChangePoints[j].transform) == -1 && canTurnLeft ||
                                                        PossibleTargetDirection(carTAA[i], nextWaypoint.onReachWaypointSettings.laneChangePoints[j].transform) == 1 && canTurnRight
                                                        )//换道判断8：换道点位置一侧是否能能转向，PossibleTargetDirection用于判断换道点在哪一侧，函数在前面
                                                    {
                                                        for (int k = 0; k < nextWaypoint.onReachWaypointSettings.laneChangePoints[j].onReachWaypointSettings.parentRoute.vehicleTypes.Length; k++)
                                                        {
                                                            if (carList[i].vehicleType == nextWaypoint.onReachWaypointSettings.laneChangePoints[j].onReachWaypointSettings.parentRoute.vehicleTypes[k])//换道判断9：车型是否符合（controller里type的作用就是分类执行命令？）
                                                            {
                                                                carList[i].ChangeToRouteWaypoint(nextWaypoint.onReachWaypointSettings.laneChangePoints[j].onReachWaypointSettings);//换道执行语句，定位在AITrafficCar300行附近,内容其实就是把车分配给lanechangepoint那条路径了
                                                                isChangingLanesNL[i] = true;
                                                                canChangeLanesNL[i] = false;
                                                                forceChangeLanesNL[i] = false;
                                                                changeLaneTriggerTimer[i] = 0f;
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    changeLaneTriggerTimer[i] = 0f;
                                    leftHitNL[i] = false;
                                    rightHitNL[i] = false;
                                    leftHitDistanceNL[i] = sideSensorLengthNL[i];
                                    rightHitDistanceNL[i] = sideSensorLengthNL[i];
                                }
                            }
                        }
                        #endregion
                       // Debug.Log(isBrakingNL[i]);
                        if ((speedNL[i] == 0 || !overrideInputNL[i]))//如果速度等于0或没触发重写（重写：Job里的一个函数，用于在特殊情况下覆盖原输出，一般使用重写都是要刹车了）
                        {
                            rigidbodyList[i].drag = 0;//加速/匀速行驶时阻力设为0，才能保持加速过程是线性可计算的
                            rigidbodyList[i].angularDrag = minAngularDragNL[i];
                        }
                        else if (overrideInputNL[i])//如果触发重写
                        {
                            isBrakingNL[i] = true;
                            //Debug.Log(rigidbodyList[i].drag);
                            if (frontHitNL[i]&&frontHitDistanceNL[i] / (speedNL[i] / 3.6f - frontspeedNL[i]) > 0&& frontHitDistanceNL[i] / (speedNL[i] / 3.6f - frontspeedNL[i]) < 10f)//距前撞物体距离小于传感器探测范围，前车速度小于本车且有追尾风险
                            {
                                motorTorqueNL[i] = 0;
                                if (frontHitDistanceNL[i] / (speedNL[i]/3.6f - frontspeedNL[i])<5f)//减速
                                {
                                    rigidbodyList[i].drag = minDragNL[i];//加阻力
                                }
                                if (frontHitDistanceNL[i] / (speedNL[i] / 3.6f - frontspeedNL[i]) < 3f || frontHitDistanceNL[i] <= 3f)//距前撞物体距离小于停车阈值（TTC<3s（用于行驶）或距离<3m(用于排队)），刹车
                                {
                                    brakeTorqueNL[i] = brakePowerNL[i];
                                }
                                if (frontHitDistanceNL[i] / (speedNL[i] / 3.6f - frontspeedNL[i]) < 1f|| frontHitDistanceNL[i]<=1f)//快撞了，上帝之手摁住
                                {
                                    dragToAdd = Mathf.InverseLerp(0, frontSensorLengthNL[i], frontHitDistanceNL[i]) * (speedNL[i]) * 200f;
                                    rigidbodyList[i].drag = minDragNL[i] + (Mathf.InverseLerp(0, frontSensorLengthNL[i], frontHitDistanceNL[i]) * dragToAdd);
                                    rigidbodyList[i].angularDrag = minAngularDragNL[i] + Mathf.InverseLerp(0, frontSensorLengthNL[i], frontHitDistanceNL[i] * dragToAdd);
                                    brakeTorqueNL[i] = brakePowerNL[i];
                                }
                            }
                            if (frontHitNL[i] && (frontHitDistanceNL[i] / (speedNL[i] / 3.6f - frontspeedNL[i]) <= 0 || frontHitDistanceNL[i] / (speedNL[i] / 3.6f - frontspeedNL[i])>= 10f)&& frontHitDistanceNL[i]>=3f)//无追尾风险，小油门量行驶
                            //(同时也保证了堵塞和排队时跟车车距不会太长或太短,太长将导致很长的拥堵距离，太短则会导致触发器来不及连续反应)
                            {
                                motorTorqueNL[i] = 250f;
                            }
                            if (needHardBrakeNL[i])
                            {
                                rigidbodyList[i].drag = speedNL[i]*hardBrakePower;//Rebe0627:急刹车                                    
                            }
                            else if (!frontHitNL[i] & speedNL[i] > targetSpeedNL[i])//超速
                            {
                                rigidbodyList[i].drag = minDragNL[i];//加阻力减速
                            }
                            else
                            {
                                motorTorqueNL[i] = 0;
                                dragToAdd = Mathf.InverseLerp(5, 0, distanceToEndPointNL[i]/frontSensorLengthNL[i]);//Rebe0627:保证Lerp的Value在0-1之间
                                rigidbodyList[i].drag = 0.04f + dragToAdd;
                                rigidbodyList[i].angularDrag = dragToAdd;
                            }
                            changeLaneTriggerTimer[i] = 0;
                            
                        }

                        for (int j = 0; j < 4; j++) //调整车轮碰撞器输出（动力来源）
                        {
                            if (j == 0)
                            {
                                currentWheelCollider = frontRightWheelColliderList[i];
                                currentWheelCollider.steerAngle = steerAngleNL[i];//车轮舵角（前转四驱车）
                                currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                                FRwheelPositionNL[i] = wheelPosition_Cached;
                                FRwheelRotationNL[i] = wheelQuaternion_Cached;
                            }
                            else if (j == 1)
                            {
                                currentWheelCollider = frontLefttWheelColliderList[i];
                                currentWheelCollider.steerAngle = steerAngleNL[i];
                                currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                                FLwheelPositionNL[i] = wheelPosition_Cached;
                                FLwheelRotationNL[i] = wheelQuaternion_Cached;
                            }
                            else if (j == 2)
                            {
                                currentWheelCollider = backRighttWheelColliderList[i];
                                currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                                BRwheelPositionNL[i] = wheelPosition_Cached;
                                BRwheelRotationNL[i] = wheelQuaternion_Cached;
                            }
                            else if (j == 3)
                            {
                                currentWheelCollider = backLeftWheelColliderList[i];
                                currentWheelCollider.GetWorldPose(out wheelPosition_Cached, out wheelQuaternion_Cached);
                                BLwheelPositionNL[i] = wheelPosition_Cached;
                                BLwheelRotationNL[i] = wheelQuaternion_Cached;
                            }
                            currentWheelCollider.motorTorque = motorTorqueNL[i];//电机扭矩：单位牛米，固定扭矩运动不科学
                            currentWheelCollider.brakeTorque = brakeTorqueNL[i];//刹车扭矩：单位牛米
                            currentWheelCollider.sidewaysFriction = speedNL[i] < 1 ? lowSidewaysWheelFrictionCurve : highSidewaysWheelFrictionCurve;
                        }

                        if ((frontHitNL[i] && speedNL[i] < (previousFrameSpeedNL[i] + 5)) || overrideDragNL[i])
                            isBrakingNL[i] = true;

                        if (speedNL[i] + .5f > previousFrameSpeedNL[i] && speedNL[i] > 15 && frontHitNL[i])
                            isBrakingNL[i] = false;

                        if (isBrakingNL[i])//连续刹车时间大于一定时间才亮刹车灯
                        {
                            if (!RenderPipeline.IsDefaultRP && !RenderPipeline.IsURP)
                            {
                                brakeMaterial[i].SetFloat("_EmissiveExposureWeight", 0);
                            }
                            brakeTimeNL[i] += deltaTime;
                            if (brakeTimeNL[i] > 0.3f)
                            {
                                brakeMaterial[i].SetColor(emissionColorName, brakeOnColor); //brakeMaterial[i].EnableKeyword("EMISSION");
                            }
                        }
                        else
                        {
                            brakeTimeNL[i] = 0f;
                            brakeMaterial[i].SetColor(emissionColorName, brakeOffColor); //brakeMaterial[i].EnableKeyword("EMISSION");
                        }
                        previousFrameSpeedNL[i] = speedNL[i];
                    }
                }
                //下面几个是在实例化其它多线程
                carTransformpositionJob = new AITrafficCarPositionJob
                {
                    canProcessNA = canProcessNL,
                    carTransformPreviousPositionNA = carTransformPreviousPositionNL,
                    carTransformPositionNA = carTransformPositionNL,
                };
                jobHandle = carTransformpositionJob.Schedule(carTAA);
                jobHandle.Complete();//改变车身位置的多线程

                frAITrafficCarWheelJob = new AITrafficCarWheelJob
                {
                    canProcessNA = canProcessNL,
                    wheelPositionNA = FRwheelPositionNL,
                    wheelQuaternionNA = FRwheelRotationNL,
                    speedNA = speedNL,
                };
                jobHandle = frAITrafficCarWheelJob.Schedule(frontRightWheelTAA);
                jobHandle.Complete();//控制轮子旋转和位置的多线程

                flAITrafficCarWheelJob = new AITrafficCarWheelJob
                {
                    canProcessNA = canProcessNL,
                    wheelPositionNA = FLwheelPositionNL,
                    wheelQuaternionNA = FLwheelRotationNL,
                    speedNA = speedNL,
                };
                jobHandle = flAITrafficCarWheelJob.Schedule(frontLeftWheelTAA);
                jobHandle.Complete();

                brAITrafficCarWheelJob = new AITrafficCarWheelJob
                {
                    canProcessNA = canProcessNL,
                    wheelPositionNA = BRwheelPositionNL,
                    wheelQuaternionNA = BRwheelRotationNL,
                    speedNA = speedNL,
                };
                jobHandle = brAITrafficCarWheelJob.Schedule(backRightWheelTAA);
                jobHandle.Complete();

                blAITrafficCarWheelJob = new AITrafficCarWheelJob
                {
                    canProcessNA = canProcessNL,
                    wheelPositionNA = BLwheelPositionNL,
                    wheelQuaternionNA = BLwheelRotationNL,
                    speedNA = speedNL,
                };
                jobHandle = blAITrafficCarWheelJob.Schedule(backLeftWheelTAA);
                jobHandle.Complete();
                
                if (usePooling)
                {
                    centerPosition = centerPoint.position;
                    _AITrafficDistanceJob = new AITrafficDistanceJob//开启定位多线程，用于判断在交通流的哪个区域及应该执行什么动作
                    {
                        canProcessNA = canProcessNL,
                        playerPosition = centerPosition,
                        distanceToPlayerNA = distanceToPlayerNL,
                        isVisibleNA = isVisibleNL,
                        withinLimitNA = withinLimitNL,
                        cullDistance = cullHeadLight,
                        lightIsActiveNA = lightIsActiveNL,
                        outOfBoundsNA = outOfBoundsNL,
                        actizeZone = actizeZone,
                        spawnZone = spawnZone,
                        isDisabledNA = isDisabledNL,
                    };
                    jobHandle = _AITrafficDistanceJob.Schedule(carTAA);
                    jobHandle.Complete();
                    for (int i = 0; i < allWaypointRoutesList.Count; i++)
                    {
                        allWaypointRoutesList[i].previousDensity = allWaypointRoutesList[i].currentDensity;
                        allWaypointRoutesList[i].currentDensity = 0;
                    }//统计各条线路上的密度
                    for (int i = 0; i < carCount; i++)
                    {
                        if (canProcessNL[i])
                        {
                            if (isDisabledNL[i] == false)
                            {
                                carRouteList[i].currentDensity += 1;
                                if (outOfBoundsNL[i])//这个在DistanceJob里，由密度或距离控制
                                {
                                    MoveCarToPool(carList[i].assignedIndex);//超过Spawnzone范围，执行MoveCarToPool
                                }
                            }
                            else if (outOfBoundsNL[i] == false)
                            {
                                if (lightIsActiveNL[i])
                                {
                                    if (isEnabledNL[i] == false)
                                    {
                                        isEnabledNL[i] = true;
                                        headLight[i].enabled = true;
                                    }
                                }
                                else
                                {
                                    if (isEnabledNL[i])
                                    {
                                        isEnabledNL[i] = false;
                                        headLight[i].enabled = false;
                                    }
                                }
                            }
                        }
                    }
                    if (spawnTimer >= spawnRate) SpawnTraffic();
                    else spawnTimer += deltaTime;
                }

                if (STSPrefs.debugProcessTime) Debug.Log((("AI Update " + (Time.realtimeSinceStartup - startTime) * 1000f)) + "ms");
            }
        }

        private void OnDestroy()//当事件被销毁（物体被销毁或进程关闭）时执行：关闭本地容器，释放内存
        {
            DisposeArrays(true);
        }

        void DisposeArrays(bool _isQuit)
        {
            if (_isQuit)
            {
                currentRoutePointIndexNL.Dispose();
                waypointDataListCountNL.Dispose();
                carTransformPreviousPositionNL.Dispose();
                carTransformPositionNL.Dispose();
                finalRoutePointPositionNL.Dispose();
                routePointPositionNL.Dispose();
                forceChangeLanesNL.Dispose();
                isChangingLanesNL.Dispose();
                canChangeLanesNL.Dispose();
                isDrivingNL.Dispose();
                isActiveNL.Dispose();
                speedNL.Dispose();
                routeProgressNL.Dispose();
                targetSpeedNL.Dispose();
                accelNL.Dispose();
                speedLimitNL.Dispose();
                averagespeedNL.Dispose();
                sigmaNL.Dispose();
                targetAngleNL.Dispose();
                dragNL.Dispose();
                angularDragNL.Dispose();
                overrideDragNL.Dispose();
                localTargetNL.Dispose();
                steerAngleNL.Dispose();
                motorTorqueNL.Dispose();
                accelerationInputNL.Dispose();
                brakeTorqueNL.Dispose();
                moveHandBrakeNL.Dispose();
                overrideInputNL.Dispose();
                distanceToEndPointNL.Dispose();
                overrideAccelerationPowerNL.Dispose();
                overrideBrakePowerNL.Dispose();
                isBrakingNL.Dispose();
                FRwheelPositionNL.Dispose();
                FRwheelRotationNL.Dispose();
                FLwheelPositionNL.Dispose();
                FLwheelRotationNL.Dispose();
                BRwheelPositionNL.Dispose();
                BRwheelRotationNL.Dispose();
                BLwheelPositionNL.Dispose();
                BLwheelRotationNL.Dispose();
                previousFrameSpeedNL.Dispose();
                brakeTimeNL.Dispose();
                topSpeedNL.Dispose();
                frontSensorTransformPositionNL.Dispose();
                frontSensorLengthNL.Dispose();
                frontSensorSizeNL.Dispose();
                sideSensorLengthNL.Dispose();
                sideSensorSizeNL.Dispose();
                minDragNL.Dispose();
                minAngularDragNL.Dispose();
                frontHitDistanceNL.Dispose();
                leftHitDistanceNL.Dispose();
                rightHitDistanceNL.Dispose();
                frontHitNL.Dispose();
                leftHitNL.Dispose();
                rightHitNL.Dispose();
                stopForTrafficLightNL.Dispose();
                yieldForCrossTrafficNL.Dispose();
                routeIsActiveNL.Dispose();
                isVisibleNL.Dispose();
                isDisabledNL.Dispose();
                withinLimitNL.Dispose();
                distanceToPlayerNL.Dispose();
                accelerationPowerNL.Dispose();
                brakePowerNL.Dispose();
                isEnabledNL.Dispose();
                outOfBoundsNL.Dispose();
                lightIsActiveNL.Dispose();
                canProcessNL.Dispose();
                frontspeedNL.Dispose();
                needHardBrakeNL.Dispose();
            }
            driveTargetTAA.Dispose();
            carTAA.Dispose();
            frontRightWheelTAA.Dispose();
            frontLeftWheelTAA.Dispose();
            backRightWheelTAA.Dispose();
            backLeftWheelTAA.Dispose();
            frontBoxcastCommands.Dispose();
            leftBoxcastCommands.Dispose();
            rightBoxcastCommands.Dispose();
            frontBoxcastResults.Dispose();
            leftBoxcastResults.Dispose();
            rightBoxcastResults.Dispose();
        }
        #endregion
        //主逻辑
        #region Gizmos
        private bool spawnPointsAreHidden;
        private Vector3 gizmoOffset;
        private Matrix4x4 cubeTransform;
        private Matrix4x4 oldGizmosMatrix;

        void OnDrawGizmos()
        {
            if (STSPrefs.sensorGizmos && Application.isPlaying)
            {
                for (int i = 0; i < carTransformPositionNL.Length; i++)
                {
                    if (isActiveNL[i] && canProcessNL[i])
                    {
                        ///// Front Sensor Gizmo
                        Gizmos.color = frontHitDistanceNL[i] == frontSensorLengthNL[i] ? STSPrefs.normalColor : STSPrefs.detectColor;
                        gizmoOffset = new Vector3(frontSensorSizeNL[i].x * 2.0f, frontSensorSizeNL[i].y * 2.0f, frontHitDistanceNL[i]);
                        DrawCube(frontSensorTransformPositionNL[i] + frontDirectionList[i] * (frontHitDistanceNL[i] / 2), frontRotationList[i], gizmoOffset);
                        if (STSPrefs.sideSensorGizmos)
                        {
                            #region Left Sensor
                            /// Left Sensor
                            leftOriginList[i] = leftTransformCached[i].position;
                            leftDirectionList[i] = leftTransformCached[i].forward;
                            leftRotationList[i] = leftTransformCached[i].rotation;
                            if (Physics.BoxCast(
                                leftOriginList[i],
                                sideSensorSizeNL[i],
                                leftDirectionList[i],
                                out boxHit,
                                leftRotationList[i],
                                sideSensorLengthNL[i],
                                layerMask,
                                QueryTriggerInteraction.UseGlobal))
                            {
                                leftHitTransform[i] = boxHit.transform; // cache transform lookup
                                if (leftHitTransform[i] != leftPreviousHitTransform[i])
                                {
                                    leftPreviousHitTransform[i] = leftHitTransform[i];
                                }
                                leftHitDistanceNL[i] = boxHit.distance;
                                leftHitNL[i] = true;
                            }
                            else if (leftHitNL[i] != false) //ResetHitBox
                            {
                                leftHitDistanceNL[i] = sideSensorLengthNL[i];
                                leftHitNL[i] = false;
                            }
                            ///// Left Sensor Gizmo
                            Gizmos.color = leftHitDistanceNL[i] == sideSensorLengthNL[i] ? STSPrefs.normalColor : STSPrefs.detectColor;
                            gizmoOffset = new Vector3(sideSensorSizeNL[i].x * 2.0f, sideSensorSizeNL[i].y * 2.0f, leftHitDistanceNL[i]);
                            DrawCube(leftOriginList[i] + leftDirectionList[i] * (leftHitDistanceNL[i] / 2), leftRotationList[i], gizmoOffset);
                            #endregion

                            #region Right Sensor
                            /// Right Sensor
                            rightOriginList[i] = rightTransformCached[i].position;
                            rightDirectionList[i] = rightTransformCached[i].forward;
                            rightRotationList[i] = rightTransformCached[i].rotation;
                            if (Physics.BoxCast(
                                rightOriginList[i],
                                sideSensorSizeNL[i],
                                rightDirectionList[i],
                                out boxHit,
                                rightRotationList[i],
                                sideSensorLengthNL[i],
                                layerMask,
                                QueryTriggerInteraction.UseGlobal))
                            {
                                rightHitTransform[i] = boxHit.transform; // cache transform lookup
                                if (rightHitTransform[i] != rightPreviousHitTransform[i])
                                {
                                    rightPreviousHitTransform[i] = rightHitTransform[i];
                                }
                                rightHitDistanceNL[i] = boxHit.distance;
                                rightHitNL[i] = true;
                            }
                            else if (rightHitNL[i] != false) //ResetHitBox
                            {
                                rightHitDistanceNL[i] = sideSensorLengthNL[i];
                                rightHitNL[i] = false;
                            }
                            ///// Right Sensor Gizmo
                            Gizmos.color = rightHitDistanceNL[i] == sideSensorLengthNL[i] ? STSPrefs.normalColor : STSPrefs.detectColor;
                            gizmoOffset = new Vector3(sideSensorSizeNL[i].x * 2.0f, sideSensorSizeNL[i].y * 2.0f, rightHitDistanceNL[i]);
                            DrawCube(rightOriginList[i] + rightDirectionList[i] * (rightHitDistanceNL[i] / 2), rightRotationList[i], gizmoOffset);
                            #endregion
                        }
                        else
                        {
                            if (leftHitNL[i])//(isChangingLanesNL[i] == false && canChangeLanesNL[i]) || m_AITrafficDebug.alwaysSideSensorGizmos)
                            {
                                ///// Left Sensor Gizmo
                                Gizmos.color = leftHitDistanceNL[i] == sideSensorLengthNL[i] ? STSPrefs.normalColor : STSPrefs.detectColor;
                                gizmoOffset = new Vector3(sideSensorSizeNL[i].x * 2.0f, sideSensorSizeNL[i].y * 2.0f, leftHitDistanceNL[i]);
                                DrawCube(leftOriginList[i] + leftDirectionList[i] * (leftHitDistanceNL[i] / 2), leftRotationList[i], gizmoOffset);
                            }
                            else if (rightHitNL[i])
                            {
                                ///// Right Sensor Gizmo
                                Gizmos.color = rightHitDistanceNL[i] == sideSensorLengthNL[i] ? STSPrefs.normalColor : STSPrefs.detectColor;
                                gizmoOffset = new Vector3(sideSensorSizeNL[i].x * 2.0f, sideSensorSizeNL[i].y * 2.0f, rightHitDistanceNL[i]);
                                DrawCube(rightOriginList[i] + rightDirectionList[i] * (rightHitDistanceNL[i] / 2), rightRotationList[i], gizmoOffset);
                            }
                        }
                    }
                }
            }
            if (STSPrefs.hideSpawnPointsInEditMode && spawnPointsAreHidden == false)
            {
                spawnPointsAreHidden = true;
                AITrafficSpawnPoint[] spawnPoints = FindObjectsOfType<AITrafficSpawnPoint>();
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    spawnPoints[i].GetComponent<MeshRenderer>().enabled = false;
                }
            }
            else if (STSPrefs.hideSpawnPointsInEditMode == false && spawnPointsAreHidden)
            {
                spawnPointsAreHidden = false;
                AITrafficSpawnPoint[] spawnPoints = FindObjectsOfType<AITrafficSpawnPoint>();
                for (int i = 0; i < spawnPoints.Length; i++)
                {
                    spawnPoints[i].GetComponent<MeshRenderer>().enabled = true;
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
        //场景里画各种标识
        #region TrafficPool
        public AITrafficCar GetCarFromPool(AITrafficWaypointRoute parentRoute)
        {
            loadCar = null;
            for (int i = 0; i < trafficPool.Count; i++)
            {
                for (int j = 0; j < parentRoute.vehicleTypes.Length; j++)
                {
                    if (trafficPool[i].trafficPrefab.vehicleType == parentRoute.vehicleTypes[j])
                    {
                        loadCar = trafficPool[i].trafficPrefab;
                        isDisabledNL[trafficPool[i].assignedIndex] = false;
                        rigidbodyList[trafficPool[i].assignedIndex].isKinematic = false;
                        EnableCar(carList[trafficPool[i].assignedIndex].assignedIndex, parentRoute);//激活一辆车
                        trafficPool.RemoveAt(i);//暂存车辆的池里去掉这辆车
                        return loadCar;
                    }
                }
            }
            return loadCar;
        }//从池里获取车辆

        public AITrafficCar GetCarFromPool(AITrafficWaypointRoute parentRoute, AITrafficVehicleType vehicleType)
        {
            loadCar = null;
            for (int i = 0; i < trafficPool.Count; i++)
            {
                for (int j = 0; j < parentRoute.vehicleTypes.Length; j++)
                {
                    if (trafficPool[i].trafficPrefab.vehicleType == parentRoute.vehicleTypes[j] &&
                        trafficPool[i].trafficPrefab.vehicleType == vehicleType &&
                        canProcessNL[trafficPool[i].assignedIndex])
                    {
                        loadCar = trafficPool[i].trafficPrefab;
                        isDisabledNL[trafficPool[i].assignedIndex] = false;
                        rigidbodyList[trafficPool[i].assignedIndex].isKinematic = false;
                        EnableCar(carList[trafficPool[i].assignedIndex].assignedIndex, parentRoute);
                        trafficPool.RemoveAt(i);
                        return loadCar;
                    }
                }
            }
            return loadCar;
        }//从池里获取车辆（限定车辆类型）

        public void EnableCar(int _index, AITrafficWaypointRoute parentRoute)
        {
            isActiveNL[_index] = true;
            carList[_index].gameObject.SetActive(true);
            carRouteList[_index] = parentRoute;
            carAIWaypointRouteInfo[_index] = parentRoute.routeInfo;
            carList[_index].StartDriving();
        }//激活某辆车

        public void MoveCarToPool(int _index)
        {
            canChangeLanesNL[_index] = false;
            isChangingLanesNL[_index] = false;
            forceChangeLanesNL[_index] = false;
            isDisabledNL[_index] = true;
            isActiveNL[_index] = false;
            carList[_index].StopDriving();
            carList[_index].transform.position = disabledPosition;
            carList[_index].gameObject.SetActive(false);//本来写在下面的协程函数里，VPP模型（所有轮子和车身不在同一级的模型）会有轮子留在路面上闪，写在这就不会            
            StartCoroutine(MoveCarToPoolCoroutine(_index));
        }//把车扔回池子里，其实就是关闭激活，然后把车辆扔到一个很远的地方

        IEnumerator MoveCarToPoolCoroutine(int _index)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            newTrafficPoolEntry = new AITrafficPoolEntry();
            newTrafficPoolEntry.assignedIndex = _index;
            newTrafficPoolEntry.trafficPrefab = carList[_index];
            trafficPool.Add(newTrafficPoolEntry);
        }

        public void MoveAllCarsToPool()
        {
            for (int i = 0; i < isActiveNL.Length; i++)
            {
                if (isActiveNL[i])
                {
                    canChangeLanesNL[i] = false;
                    isChangingLanesNL[i] = false;
                    forceChangeLanesNL[i] = false;
                    isDisabledNL[i] = true;
                    isActiveNL[i] = false;
                    carList[i].StopDriving();
                    carList[i].gameObject.SetActive(false);
                    StartCoroutine(MoveCarToPoolCoroutine(i));
                }
            }
        }

        void SpawnTraffic()
        {
            spawnTimer = 0f;
            availableSpawnPoints.Clear();
            for (int i = 0; i < trafficSpawnPoints.Count; i++) // Get Available Spawn Points From All Zones
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, trafficSpawnPoints[i].transformCached.position);
                if ((distanceToSpawnPoint > actizeZone || (distanceToSpawnPoint > minSpawnZone && trafficSpawnPoints[i].isVisible == false))
                    && distanceToSpawnPoint < spawnZone && trafficSpawnPoints[i].isTrigger == false)
                {
                    availableSpawnPoints.Add(trafficSpawnPoints[i]);
                }
            }
            currentDensity = carList.Count - trafficPool.Count;
            if (currentDensity < density) //Spawn Traffic
            {
                currentAmountToSpawn = density - currentDensity;
                for (int i = 0; i < currentAmountToSpawn; i++)
                {
                    if (availableSpawnPoints.Count == 0 || trafficPool.Count == 0) break;
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    if (availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.currentDensity < availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.maxDensity)
                    {
                        spawncar = GetCarFromPool(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);
                        if (spawncar != null)
                        {
                            availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.currentDensity += 1;
                            spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                            spawncar.transform.SetPositionAndRotation(
                                spawnPosition,
                                availableSpawnPoints[randomSpawnPointIndex].transformCached.rotation
                                );
                            spawncar.transform.LookAt(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                            availableSpawnPoints.RemoveAt(randomSpawnPointIndex);
                        }
                    }
                }
            }
        }//生成车，函数内容是选择某辆车分配给某条指定线路，激活这辆车；再随机分配给某一可用的生成点，把车传送过来

        IEnumerator SpawnStartupTrafficCoroutine()
        {
            yield return new WaitForEndOfFrame();
            availableSpawnPoints.Clear();
            currentDensity = 0;
            currentAmountToSpawn = density - currentDensity;
            for (int i = 0; i < trafficSpawnPoints.Count; i++) // Get Available Spawn Points From All Zones
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, trafficSpawnPoints[i].transformCached.position);
                if (trafficSpawnPoints[i].isTrigger == false)
                {
                    availableSpawnPoints.Add(trafficSpawnPoints[i]);
                }
            }
            for (int i = 0; i < density; i++) // Spawn Traffic
            {
                for (int j = 0; j < trafficPrefabs.Length; j++)
                {
                    if (availableSpawnPoints.Count == 0) break;
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                    for (int k = 0; k < availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.vehicleTypes.Length; k++)
                    {
                        if (currentAmountToSpawn == 0) break;
                        if (availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.vehicleTypes[k] == trafficPrefabs[j].vehicleType)
                        {
                            GameObject spawnedTrafficVehicle = Instantiate(trafficPrefabs[j].gameObject, spawnPosition, availableSpawnPoints[randomSpawnPointIndex].transformCached.rotation);
                            spawnedTrafficVehicle.GetComponent<AITrafficCar>().RegisterCar(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);
                            spawnedTrafficVehicle.transform.LookAt(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                            availableSpawnPoints.RemoveAt(randomSpawnPointIndex);//这一段实例化预制件，很多功能都能做进来，其他段就不需要了
                            //发放车牌
                            List<Material> materiallistb = materialsb.ToList();
                            List<Material> materiallistg = materialsg.ToList();
                            List<Material> materiallisty = materialsy.ToList();
                            //存储材质的数组转为列表，数组用于回收材质以及乱序，列表用于匹配和发放车牌
                            cars = spawnedTrafficVehicle.GetComponent<GetLicense>();
                            if ((int)cars.licensetype == 0)
                            {
                                cars.getmaterial = materiallistb[0];
                                materiallistb.Remove(materiallistb[0]);
                                for (int h = 0; h < materiallistb.Count; h++)
                                {
                                    materialsb[h] = materiallistb[h];
                                }
                            }                          
                            if ((int)cars.licensetype == 1)
                            {
                                cars.getmaterial = materiallistg[0];
                                materiallistg.Remove(materiallistg[0]);
                                for (int h = 0; h < materiallistg.Count; h++)
                                {
                                    materialsg[h] = materiallistg[h];
                                }
                            }
                            if ((int)cars.licensetype == 2)
                            {
                                cars.getmaterial = materiallisty[0];
                                materiallisty.Remove(materiallisty[0]);
                                for (int h = 0; h < materiallisty.Count; h++)
                                {
                                    materialsy[h] = materiallisty[h];
                                }
                            }
                            currentAmountToSpawn -= 1;
                            break;
                        }
                    }//初始化要生成的车
                    if (currentAmountToSpawn <= 0) break;
                }
            }
            for (int i = 0; i < carsInPool; i++)
            {
                if (carCount >= carsInPool) break;
                for (int j = 0; j < trafficPrefabs.Length; j++)
                {
                    if (carCount >= carsInPool) break;
                    GameObject spawnedTrafficVehicle = Instantiate(trafficPrefabs[j].gameObject, Vector3.zero, Quaternion.identity);
                    //下面发车牌
                    List<Material> materiallistb = materialsb.ToList();
                    List<Material> materiallistg = materialsg.ToList();
                    List<Material> materiallisty = materialsy.ToList();
                    cars = spawnedTrafficVehicle.GetComponent<GetLicense>();
                    if ((int)cars.licensetype == 0)
                    {
                        cars.getmaterial = materiallistb[0];
                        materiallistb.Remove(materiallistb[0]);
                        for (int h = 0; h < materiallistb.Count; h++)
                        {
                            materialsb[h] = materiallistb[h];
                        }
                    }
                    if ((int)cars.licensetype == 1)
                    {
                        cars.getmaterial = materiallistg[0];
                        materiallistg.Remove(materiallistg[0]);
                        for (int h = 0; h < materiallistg.Count; h++)
                        {
                            materialsg[h] = materiallistg[h];
                        }
                    }
                    if ((int)cars.licensetype == 2)
                    {
                        cars.getmaterial = materiallisty[0];
                        materiallisty.Remove(materiallisty[0]);
                        for (int h = 0; h < materiallisty.Count; h++)
                        {
                            materialsy[h] = materiallisty[h];
                        }
                    }
                    spawnedTrafficVehicle.GetComponent<AITrafficCar>().RegisterCar(carRouteList[0]);
                    MoveCarToPool(spawnedTrafficVehicle.GetComponent<AITrafficCar>().assignedIndex);
                }//初始化pooling车
            }
            for (int i = 0; i < carCount; i++)
            {
                routePointPositionNL[i] = carRouteList[i].waypointDataList[currentRoutePointIndexNL[i]]._transform.position;
                finalRoutePointPositionNL[i] = carRouteList[i].waypointDataList[carRouteList[i].waypointDataList.Count - 1]._transform.position;
                carList[i].StartDriving();
            }
            if (setCarParent)
            {
                if (carParent == null) carParent = transform;
                for (int i = 0; i < carCount; i++)
                {
                    carList[i].transform.SetParent(carParent);
                }
            }
            isInitialized = true;
        }//生成一开始的车，并进行初始化赋值

        public void EnableRegisteredTrafficEverywhere()
        {
            availableSpawnPoints.Clear();
            for (int i = 0; i < trafficSpawnPoints.Count; i++) // Get Available Spawn Points From All Zones
            {
                distanceToSpawnPoint = Vector3.Distance(centerPosition, trafficSpawnPoints[i].transformCached.position);
                if (trafficSpawnPoints[i].isTrigger == false)
                {
                    availableSpawnPoints.Add(trafficSpawnPoints[i]);
                }
            }
            for (int i = 0; i < density; i++) // Spawn Traffic
            {
                for (int j = 0; j < trafficPrefabs.Length; j++)
                {
                    if (availableSpawnPoints.Count == 0) break;
                    randomSpawnPointIndex = UnityEngine.Random.Range(0, availableSpawnPoints.Count);
                    spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                    for (int k = 0; k < availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.vehicleTypes.Length; k++)
                    {
                        if (availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.vehicleTypes[k] == trafficPrefabs[j].vehicleType)
                        {
                            spawncar = GetCarFromPool(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute);
                            if (spawncar != null)
                            {
                                availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.currentDensity += 1;
                                spawnPosition = availableSpawnPoints[randomSpawnPointIndex].transformCached.position + spawnOffset;
                                spawncar.transform.SetPositionAndRotation(

                                    spawnPosition,
                                    availableSpawnPoints[randomSpawnPointIndex].transformCached.rotation
                                    );
                                spawncar.transform.LookAt(availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[availableSpawnPoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                                availableSpawnPoints.RemoveAt(randomSpawnPointIndex);
                            }
                            break;
                        }
                    }
                }
            }
        }//把生成点去掉，在哪都能生成车
        #endregion
        //Pool的各类方法（可作为API使用，但需要传参）
        #region Runtime API for Dynamic Content - Some Require Pooling
        /// <summary>
        /// Requires pooling, disables and moves all cars into the pool.
        /// </summary>
        public void DisableAllCars()
        {
            usePooling = false;
            for (int i = 0; i < carList.Count; i++)
            {
                MoveCarToPool(i);
                Set_CanProcess(i, false);
            }
        }

        /// <summary>
        /// Clears the spawn points list.
        /// </summary>
        public void RemoveSpawnPoints()
        {
            for (int i = trafficSpawnPoints.Count - 1; i < trafficSpawnPoints.Count - 1; i--)
            {
                trafficSpawnPoints[i].RemoveSpawnPoint();
            }
        }

        /// <summary>
        /// Clears the route list.
        /// </summary>
        public void RemoveRoutes()
        {
            for (int i = allWaypointRoutesList.Count - 1; i < allWaypointRoutesList.Count - 1; i--)
            {
                allWaypointRoutesList[i].RemoveRoute();
            }
        }

        /// <summary>
        /// Enables processing on all registered cars.
        /// </summary>
        public void EnableAllCars()
        {
            for (int i = 0; i < carList.Count; i++)
            {
                carList[i].EnableAIProcessing();
            }
            usePooling = true;
            EnableRegisteredTrafficEverywhere();
        }
        #endregion
        #region 额外加入的一些独立功能函数
        //专门独立出来的侧向检测判断逻辑，用来给NewRoutePoint的换道模式加侧向检测
        public bool EnabledNewPoint(GameObject Car, Transform NewPointTransform)
        {
            leftHitNL[Car.GetComponent<AITrafficCar>().assignedIndex] = leftBoxcastResults[Car.GetComponent<AITrafficCar>().assignedIndex].collider == null ? false : true;
            rightHitNL[Car.GetComponent<AITrafficCar>().assignedIndex] = rightBoxcastResults[Car.GetComponent<AITrafficCar>().assignedIndex].collider == null ? false : true;
            //Debug.Log("left"+leftHitNL[Car.GetComponent<AITrafficCar>().assignedIndex]);
            //Debug.Log("right"+rightHitNL[Car.GetComponent<AITrafficCar>().assignedIndex]);
            if ((PossibleTargetDirection(Car.transform, NewPointTransform) == -1 && leftHitNL[Car.GetComponent<AITrafficCar>().assignedIndex] == false)
                || (PossibleTargetDirection(Car.transform, NewPointTransform) == 1 && rightHitNL[Car.GetComponent<AITrafficCar>().assignedIndex] == false))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //让数组乱序
        private static Material[] GetDisruptedItems(Material[] Materials)
        {
            //生成一个新数组：用于在之上计算和返回
            Material[] temp;
            temp = new Material[Materials.Length];
            for (int i = 0; i < temp.Length; i++)
            {
                temp[i] = Materials[i];
            }
            //打乱数组中元素顺序
            for (int i = 0; i < temp.Length; i++)
            {
                int x, y; Material t;
                x = UnityEngine.Random.Range(0, temp.Length);
                do
                {
                    y = UnityEngine.Random.Range(0, temp.Length);
                } while (y == x);
                t = temp[x];
                temp[x] = temp[y];
                temp[y] = t;
            }
            return temp;
        }
        #endregion
    }
}