namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using System.Collections.Generic;
    using System.Collections;

    [HelpURL("https://simpletrafficsystem.turnthegameon.com/documentation/api/aitrafficcar")]
    public class AITrafficCar : MonoBehaviour
    {
        public int assignedIndex { get; private set; }//车的标号
        [Tooltip("Vehicles will only spawn, and merge onto routes with matching vehicle types.")]
        public AITrafficVehicleType vehicleType = AITrafficVehicleType.Default;
        [Tooltip("Amount of torque that is passed to car Wheel Colliders when not braking.")]
        public float accelerationPower = 1500;
        [Tooltip("Respawn the car to the first route point on it's spawn route when the car comes to a stop.")]
        public bool goToStartOnStop;
        [Tooltip("Car max speed, assigned to AITrafficController when car is registered.")]
        public float topSpeed = 25f;
        [Tooltip("Minimum amount of drag applied to car Rigidbody when not braking.")]
        public float minDrag = 0.3f;
        [Tooltip("Minimum amount of angular drag applied to car Rigidbody when not braking.")]
        public float minAngularDrag = 0.3f;

        [Tooltip("Size of the front detection sensor BoxCast.")]
        public Vector3 frontSensorSize = new Vector3(1.3f, 1f, 0.001f);
        [Tooltip("Length of the front detection sensor BoxCast.")]
        public float frontSensorLength = 10f;
        [Tooltip("Length of the front detection sensor BoxCast for turn light.")]
        public float frontSensorLengthForTurnLight = 100f;
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
        //打开转向灯射线检测
        RaycastHit m_Hit;
        bool m_HitDetect;

        public void RegisterCar(AITrafficWaypointRoute route)
        {
            leftturnlight.materials[0].EnableKeyword("_EMISSION");
            rightturnlight.materials[0].EnableKeyword("_EMISSION");
            if (brakeMaterial == null && brakeMaterialMesh != null)
            {
                brakeMaterial = brakeMaterialMesh.materials[brakeMaterialIndex];
            }
            assignedIndex = AITrafficController.Instance.RegisterCarAI(this, route);
            //Debug.Log(transform.name+","+assignedIndex);
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
        }
        /// <summary>
        /// Rebe：是否需要打开转向灯
        /// </summary>
        public void NeedTurnLight(int direction)
        {
            if (m_HitDetect= Physics.BoxCast(frontSensorTransform.position,frontSensorSize,transform.forward,out m_Hit,transform.rotation,frontSensorLengthForTurnLight,AITrafficController.Instance.layerMask))
            {
                turnlight.isturning = direction;
                if(direction!=0)
                    Debug.Log("打开转向灯");
            }
        }
        //用来显示打开转向灯的射线检测
        //void OnDrawGizmos()
        //{
        //    Gizmos.color = Color.red;

        //    //Check if there has been a hit yet
        //    if (m_HitDetect)
        //    {
        //        //Draw a Ray forward from GameObject toward the hit
        //        Gizmos.DrawRay(frontSensorTransform.position, transform.forward * m_Hit.distance);
        //        //Draw a cube that extends to where the hit exists
        //        Gizmos.DrawWireCube(frontSensorTransform.position + transform.forward * m_Hit.distance, frontSensorSize);
        //    }
        //    //If there hasn't been a hit yet, draw the ray at the maximum distance
        //    else
        //    {
        //        //Draw a Ray forward from GameObject toward the maximum distance
        //        Gizmos.DrawRay(transform.position, transform.forward * frontSensorLengthForTurnLight);
        //        //Draw a cube at the maximum distance
        //        Gizmos.DrawWireCube(transform.position + transform.forward * frontSensorLengthForTurnLight, frontSensorSize);
        //    }
        //}
        #endregion

        #region Waypoint Trigger Methods
        /// <summary>
        /// Callback triggered when the AITrafficCar reaches a waypoint.
        /// </summary>
        /// <param name="onReachWaypointSettings"></param>
        public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)//把点的信息发送给车，处理到达某waypoint的各种时间
        {
            if (onReachWaypointSettings.parentRoute == AITrafficController.Instance.GetCarRoute(assignedIndex))//这条路刚好是车正在走的路
            {
                onReachWaypointSettings.OnReachWaypointEvent.Invoke();
                AITrafficController.Instance.Set_SpeedLimitArray(assignedIndex, onReachWaypointSettings.speedLimit);
                AITrafficController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.waypointIndexnumber - 1);
                AITrafficController.Instance.Set_WaypointDataListCountArray(assignedIndex);
                if (onReachWaypointSettings.newRoutePoints.Length > 0)
                {
                    newRoutePointsMatchingType.Clear();
                    for (int i = 0; i < onReachWaypointSettings.newRoutePoints.Length; i++)
                    {
                        for (int j = 0; j < onReachWaypointSettings.newRoutePoints[i].onReachWaypointSettings.parentRoute.vehicleTypes.Length; j++)
                        {
                            if (onReachWaypointSettings.newRoutePoints[i].onReachWaypointSettings.parentRoute.vehicleTypes[j] == vehicleType)
                            {
                                newRoutePointsMatchingType.Add(i);//newRoutesPointMatchingType存储可以进入的新routes
                                break;
                            }
                        }
                    }
                    if (newRoutePointsMatchingType.Count > 0 && onReachWaypointSettings.waypointIndexnumber != onReachWaypointSettings.parentRoute.waypointDataList.Count)
                    {
                        randomIndex = UnityEngine.Random.Range(0, newRoutePointsMatchingType.Count);//随机一个新路线
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
                            );//上述全为变道操作
                        //if (onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute != onReachWaypointSettings.parentRoute)
                        //{
                        //    turnlight.isturning = AITrafficController.Instance.GetPossibleDirection(this.frontSensorTransform, onReachWaypointSettings.newRoutePoints[randomIndex].transform);
                        //}
                    }
                    else if (onReachWaypointSettings.waypointIndexnumber == onReachWaypointSettings.parentRoute.waypointDataList.Count)
                    {
                        randomIndex = UnityEngine.Random.Range(0, onReachWaypointSettings.newRoutePoints.Length);
                        if (randomIndex == onReachWaypointSettings.newRoutePoints.Length) randomIndex -= 1;
                        AITrafficController.Instance.Set_WaypointRoute(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute);
                        AITrafficController.Instance.Set_RouteInfo(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.parentRoute.routeInfo);
                        AITrafficController.Instance.Set_RouteProgressArray(assignedIndex, onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1);
                        AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                            (
                            assignedIndex,
                            onReachWaypointSettings.newRoutePoints[randomIndex].onReachWaypointSettings.waypointIndexnumber - 1,
                            onReachWaypointSettings.newRoutePoints[randomIndex]
                            );
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
                }
                else if (onReachWaypointSettings.waypointIndexnumber < onReachWaypointSettings.parentRoute.waypointDataList.Count)//没有走完
                {
                    AITrafficController.Instance.Set_CurrentRoutePointIndexArray
                        (
                        assignedIndex,
                        onReachWaypointSettings.waypointIndexnumber,
                        onReachWaypointSettings.waypoint
                        );
                }
                AITrafficController.Instance.Set_RoutePointPositionArray(assignedIndex);
                if (onReachWaypointSettings.stopDriving)//如果要停下来
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
        }
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
        }
    }
}