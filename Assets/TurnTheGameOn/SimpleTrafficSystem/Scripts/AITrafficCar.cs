namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Collections;

    [HelpURL("https://simpletrafficsystem.turnthegameon.com/documentation/api/aitrafficcar")]
    public class AITrafficCar : MonoBehaviour
    {
        public int assignedIndex { get; private set; }//分配索引，把车分配到路径上
        [Tooltip("Vehicles will only spawn, and merge onto routes with matching vehicle types.")]
        public AITrafficVehicleType vehicleType = AITrafficVehicleType.Default;
        [Tooltip("Amount of torque that is passed to car Wheel Colliders when not braking.")]
        public float accelerationPower = 1500;
        [Tooltip("Amount of torque that is passed to car Wheel Colliders when  braking.")]
        public float brakePower = 3000;//刹车扭矩定义了变量但不出现在检查器上？（解决：需要在编辑器脚本上加才会出现在检查器上）
        [Tooltip("Respawn the car to the first route point on it's spawn route when the car comes to a stop.")]
        public bool goToStartOnStop;
        [Tooltip("Car max speed, assigned to AITrafficController when car is registered.")]
        public float topSpeed = 25f;
        [Tooltip("Minimum amount of drag applied to car Rigidbody when not braking.")]
        public float minDrag = 0.2f;
        [Tooltip("Minimum amount of angular drag applied to car Rigidbody when not braking.")]
        public float minAngularDrag = 0.3f;

        [Tooltip("Size of the front detection sensor BoxCast.")]
        public Vector3 frontSensorSize = new Vector3(1.3f, 1f, 0.001f);
        [Tooltip("Length of the front detection sensor BoxCast.")]
        public float frontSensorLength = 10f;
        [Tooltip("Size of the side detection sensor BoxCasts.")]
        public Vector3 sideSensorSize = new Vector3(15f, 1f, 0.001f);
        [Tooltip("Length of the side detection sensor BoxCasts.")]
        public float sideSensorLength = 5f;

        [Tooltip("Material used for brake light emission. If unassigned, the material assigned to the brakeMaterialMesh will be used.")]
        public Material brakeMaterial;
        [Tooltip("If brakeMaterial is unassigned, the material assigned to the brakeMaterialIndex will be used.")]
        public MeshRenderer brakeMaterialMesh;
        [Tooltip("Mesh Renderer material array index to get brakeMaterial from.")]
        public int brakeMaterialIndex;
        [Tooltip("Control point to orient/position the front detection sensor. ")]
        public Transform frontSensorTransform;
        [Tooltip("Control point to orient/position the left detection sensor.")]
        public Transform leftSensorTransform;
        [Tooltip("Control point to orient/position the right detection sensor.")]
        public Transform rightSensorTransform;
        [Tooltip("Light toggled on/off based on pooling cullHeadLight zone.")]
        public Light headLight;
        [Tooltip("References to car wheel mesh object, transform, and collider.")]
        public AITrafficCarWheels[] _wheels;
        public TurnLight turnlight;
        public MeshRenderer leftturnlight;
        public MeshRenderer rightturnlight;
        public Material lightmaterial;
        [HideInInspector] public Material leftmaterial;
        [HideInInspector] public Material rightmaterial;
        private AITrafficWaypointRoute startRoute;
        private Vector3 goToPointWhenStoppedVector3;
        private Rigidbody rb;
        private List<int> newRoutePointsMatchingType = new List<int>();
        private int randomIndex;
        private AITrafficWaypointSettings CacheSetting;

        public void RegisterCar(AITrafficWaypointRoute route)
        {
            leftturnlight.materials[0].EnableKeyword("_EMISSIVE");
            rightturnlight.materials[0].EnableKeyword("_EMISSIVE");
            if (brakeMaterial == null && brakeMaterialMesh != null)
            {
                brakeMaterial = brakeMaterialMesh.materials[brakeMaterialIndex];
            }
            assignedIndex = AITrafficController.Instance.RegisterCarAI(this, route);
            startRoute = route;
            rb = GetComponent<Rigidbody>();
        }

        #region Public API Methods
        /// These methods can be used to get AITrafficCar variables and call functions
        /// intended to be used by other MonoBehaviours.

        /// <summary>
        /// Returns current acceleration input as a float 0-1.
        /// </summary>
        /// <returns></returns>
        public float AccelerationInput()
        {
            return AITrafficController.Instance.GetAccelerationInput(assignedIndex);
        }

        /// <summary>
        /// Returns current steering input as a float -1 to 1.
        /// </summary>
        /// <returns></returns>
        public float SteeringInput()
        {
            return AITrafficController.Instance.GetSteeringInput(assignedIndex);
        }

        /// <summary>
        /// Returns current speed as a float.
        /// </summary>
        /// <returns></returns>
        public float CurrentSpeed()
        {
            return AITrafficController.Instance.GetCurrentSpeed(assignedIndex);
        }

        /// <summary>
        /// Returns current breaking input state as a bool.
        /// </summary>
        /// <returns></returns>
        public bool IsBraking()
        {
            return AITrafficController.Instance.GetIsBraking(assignedIndex);
        }

        /// <summary>
        /// Returns true if left sensor is triggered.
        /// </summary>
        /// <returns></returns>
        public bool IsLeftSensor()
        {
            return AITrafficController.Instance.IsLeftSensor(assignedIndex);
        }

        /// <summary>
        /// Returns true if right sensor is triggered.
        /// </summary>
        /// <returns></returns>
        public bool IsRightSensor()
        {
            return AITrafficController.Instance.IsRightSensor(assignedIndex);
        }

        /// <summary>
        /// Returns true if front sensor is triggered.
        /// </summary>
        /// <returns></returns>
        public bool IsFrontSensor()
        {
            return AITrafficController.Instance.IsFrontSensor(assignedIndex);
        }

        /// <summary>
        /// The AITrafficCar will start driving.
        /// </summary>
        [ContextMenu("StartDriving")]
        public void StartDriving()
        {
            AITrafficController.Instance.Set_IsDrivingArray(assignedIndex, true);
        }

        /// <summary>
        /// The AITrafficCar will stop driving.
        /// </summary>
        [ContextMenu("StopDriving")]
        public void StopDriving()
        {
            if (goToStartOnStop)
            {
                ChangeToRouteWaypoint(startRoute.waypointDataList[0]._waypoint.onReachWaypointSettings);
                goToPointWhenStoppedVector3 = startRoute.waypointDataList[0]._transform.position;
                goToPointWhenStoppedVector3.y += 1;
                transform.position = goToPointWhenStoppedVector3;
                transform.LookAt(startRoute.waypointDataList[1]._transform);
                rb.velocity = Vector3.zero;
            }
            else
            {
                AITrafficController.Instance.Set_IsDrivingArray(assignedIndex, false);
            }
        }

        /// <summary>
        /// Disables the AITrafficCar and returns it to the AITrafficController pool.
        /// </summary>
        [ContextMenu("MoveCarToPool")]
        public void MoveCarToPool()
        {
            AITrafficController.Instance.MoveCarToPool(assignedIndex);
        }

        /// <summary>
        /// Disables the AITrafficCar and returns it to the AITrafficController pool.
        /// </summary>
        [ContextMenu("EnableAIProcessing")]
        public void EnableAIProcessing()
        {
            AITrafficController.Instance.Set_CanProcess(assignedIndex, true);
        }

        /// <summary>
        /// Disables the AITrafficCar and returns it to the AITrafficController pool.
        /// </summary>
        [ContextMenu("DisableAIProcessing")]
        public void DisableAIProcessing()
        {
            AITrafficController.Instance.Set_CanProcess(assignedIndex, false);
        }

        /// <summary>
        /// Updates the AITrafficController top speed value for this AITrafficCar.
        /// </summary>
        public void SetTopSpeed(float _value)
        {
            topSpeed = _value;
            AITrafficController.Instance.SetTopSpeed(assignedIndex, topSpeed);
        }

        /// <summary>
        /// Controls an override flag that requests the car to attempt a lane change when able.
        /// </summary>
        public void SetForceLaneChange(bool _value)
        {
            AITrafficController.Instance.SetForceLaneChange(assignedIndex, _value);
        }//强制换道
        #endregion
        
        #region Waypoint Trigger Methods
        /// <summary>
        /// Callback triggered when the AITrafficCar reaches a waypoint.
        /// </summary>
        /// <param name="onReachWaypointSettings"></param>
        public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)//把点的信息发送给车
        {
            //if (AITrafficController.Instance.GetNextWaypoint(assignedIndex)!= null && onReachWaypointSettings.waypoint != AITrafficController.Instance.GetNextWaypoint(assignedIndex))
            //    return;
            CacheSetting = onReachWaypointSettings;
            if (onReachWaypointSettings.parentRoute == AITrafficController.Instance.GetCarRoute(assignedIndex))
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                AITrafficController.Instance.Set_SpeedLimitArray(assignedIndex, onReachWaypointSettings.speedLimit);
                AITrafficController.Instance.Set_AverageSpeed(assignedIndex, onReachWaypointSettings.averagespeed);
                AITrafficController.Instance.Set_Sigma(assignedIndex, onReachWaypointSettings.sigma);
                AITrafficController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
                AITrafficController.Instance.Set_WaypointDataListCountArray(assignedIndex);
                if (onReachWaypointSettings.newRoutePoints.Length > 0)//如果有newroutepoint
                {
                    newRoutePointsMatchingType.Clear();//清理掉所有匹配newroutepoint
                    for (int i = 0; i < onReachWaypointSettings.newRoutePoints.Length; i++)
                    {
                        if(AITrafficController.Instance.EnabledNewPoint(this.gameObject, onReachWaypointSettings.newRoutePoints[i].transform))//插入逻辑：如果newPoint满足侧向检验，换道
                        {
                            for (int j = 0; j < onReachWaypointSettings.newRoutePoints[i].onReachWaypointSettings.parentRoute.vehicleTypes.Length; j++)
                            {
                                if (onReachWaypointSettings.newRoutePoints[i].onReachWaypointSettings.parentRoute.vehicleTypes[j] == vehicleType)
                                {
                                    newRoutePointsMatchingType.Add(i);
                                    break;
                                }
                            }//对所有newroutepoint和车的类型重新匹配
                        }
                    }
                    if (newRoutePointsMatchingType.Count > 0 && onReachWaypointSettings.waypointIndexnumber != onReachWaypointSettings.parentRoute.waypointDataList.Count)//如果匹配到了newroutepoint且现在的点不是最后一个点路径点
                    {
                        randomIndex = UnityEngine.Random.Range(0, newRoutePointsMatchingType.Count);//随机分配点
                        if (randomIndex == newRoutePointsMatchingType.Count) randomIndex -= 1;
                        randomIndex = newRoutePointsMatchingType[randomIndex];
                        AITrafficController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute);
                        AITrafficController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute.routeInfo);
                        AITrafficController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1);
                        AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                            (
                            assignedIndex,
                            onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1,
                            onReachWaypointSettings.newRoutePoints[randomIndex]
                            );
                        if (onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute != onReachWaypointSettings.parentRoute)
                        {
                            turnlight.isturning = AITrafficController.Instance.GetPossibleDirection(this.frontSensorTransform, onReachWaypointSettings.newRoutePoints[randomIndex].transform);
                        }
                    }
                    else if (onReachWaypointSettings.waypointIndexnumber == onReachWaypointSettings.parentRoute.waypointDataList.Count)//如果现在的点是最后一个点，有newpoint直接分配newpoint
                    {
                        //randomIndex = UnityEngine.Random.Range(0, onReachWaypointSettings.newRoutePoints.Length);
                        //if (randomIndex == onReachWaypointSettings.newRoutePoints.Length) randomIndex -= 1;
                        //AITrafficController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute);
                        //AITrafficController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute.routeInfo);
                        //AITrafficController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1);
                        //AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                        //    (
                        //    assignedIndex,
                        //    onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1,
                        //    onReachWaypointSettings.newRoutePoints[randomIndex]
                        //    );
                        if (onReachWaypointSettings.parentRoute.isCrossRoad|| AITrafficController.Instance.EnabledNewPoint(this.gameObject, onReachWaypointSettings.newRoutePoints[randomIndex].transform))//插入逻辑：如果newPoint满足侧向检验，换道
                        {//Rebe0627:添加了一个判断条件，区别路口和并道
                            AITrafficController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute);
                            AITrafficController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute.routeInfo);
                            AITrafficController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1);
                            AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                                (
                                assignedIndex,
                                onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1,
                                onReachWaypointSettings.newRoutePoints[randomIndex]
                                );
                            if (onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute != onReachWaypointSettings.parentRoute)
                            {
                                turnlight.isturning = AITrafficController.Instance.GetPossibleDirection(this.frontSensorTransform, onReachWaypointSettings.newRoutePoints[randomIndex].transform);
                            }//外加的转向灯控制逻辑：如果newpoint不在本来的路线上（即要变道/并线了），获取下个点方向，打开该侧转向灯
                        }
                        else
                        {
                            StopDriving();
                            StartCoroutine(ResumeDrivingTimer(2f));
                        }
                    }
                    else
                    {
                        AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                        (
                        assignedIndex,
                        onReachWaypointSettings.waypointIndexnumber,
                        onReachWaypointSettings.waypoint
                        );
                    }
                }//newroutepoint的逻辑
                else if (onReachWaypointSettings.waypointIndexnumber < onReachWaypointSettings.parentRoute.waypointDataList.Count)
                {
                    AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                        (
                        assignedIndex,
                        onReachWaypointSettings.waypointIndexnumber,
                        onReachWaypointSettings.waypoint
                        );
                }
                AITrafficController.Instance.Set_RoutePointPositionArray(assignedIndex);
                if (onReachWaypointSettings.stopDriving)
                {
                    StopDriving();
                    if (onReachWaypointSettings.stopTime > 0)
                    {
                        StartCoroutine(ResumeDrivingTimer(onReachWaypointSettings.stopTime));
                    }
                }
            }
        }

        /// <summary>
        /// Used by AITrafficController to instruct the AITrafficCar to change lanes.
        /// </summary>
        /// <param name="onReachWaypointSettings"></param>
        public void ChangeToRouteWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
        {
            onReachWaypointSettings.OnReachWaypointEvent.Invoke();
            AITrafficController.Instance.Set_SpeedLimitArray(assignedIndex, onReachWaypointSettings.speedLimit);
            AITrafficController.Instance.Set_WaypointDataListCountArray(assignedIndex);
            AITrafficController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.parentRoute);
            AITrafficController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.parentRoute.routeInfo);
            AITrafficController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
            AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                (
                assignedIndex,
                onReachWaypointSettings.waypointIndexnumber,
                onReachWaypointSettings.waypoint
                );
            AITrafficController.Instance.Set_RoutePointPositionArray(assignedIndex);
            turnlight.isturning = AITrafficController.Instance.GetPossibleDirection(this.frontSensorTransform, onReachWaypointSettings.waypoint.transform);
        }//换道的执行代码；换道过程就是把车分配到由onReachWaypointSettings控制的新路径点上，onReachWaypointSettings就是被分配WayPoint的AITrafficWaypointSettings
        #endregion
        #region Callbacks
        void OnBecameInvisible()
        {
#if UNITY_EDITOR
            if (Camera.current != null)
            {
                if (Camera.current.name == "SceneCamera")
                    return;
            }
#endif
            AITrafficController.Instance.SetVisibleState(assignedIndex, false);
        }

        void OnBecameVisible()
        {
#if UNITY_EDITOR
            if (Camera.current != null)
            {
                if (Camera.current.name == "SceneCamera")
                    return;
            }
#endif
            AITrafficController.Instance.SetVisibleState(assignedIndex, true);
        }
        #endregion

        IEnumerator ResumeDrivingTimer(float _stopTime)
        {
            yield return new WaitForSeconds(_stopTime);
            StartDriving();
            OnReachedWaypoint(CacheSetting);
        }
    }
}