using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;
//using Enviro;
//可挂在任何位置
namespace VehiclePhysics
{
    public class VehicleReplace : MonoBehaviour
    {
        public bool useEnviro = false; 
        [SerializeField] private AITrafficController TrafficController;
        [SerializeField] private GameObject m_Vehicle;//要转换的驾驶车
        [SerializeField] private Camera camera_Fixed;//固定观测位置的镜头，也是选择车辆的镜头
        [SerializeField] private Camera m_camera;//要转换的相机
        //[SerializeField] private EnviroManager Enviromentcontroller;//天气系统
        [HideInInspector] public GameObject repVehicle;//被替换的AI车
        [HideInInspector] public Camera camera_rep;//AI车的相机
        private VPVehicleController m_vehicleController;
        private VPStandardInput m_StandardInput;
        private VPVehicleToolkit m_VPVehicleToolkit;
        private ImitateDriving m_ImitateDriving;
        private Rigidbody m_rigidbody;//获取必须组件
        private float duration = 0.1f;//延迟时间
        public Ray myRay;
        bool isCaching = false;
        bool isReplacing = false;
        private Vector3 velocity = new Vector3(0f, 0f, 0f);
        void Awake()
        {
            m_vehicleController = m_Vehicle.GetComponent<VPVehicleController>();
            m_VPVehicleToolkit = m_Vehicle.GetComponent<VPVehicleToolkit>();
            m_StandardInput = m_Vehicle.GetComponent<VPStandardInput>();
            m_ImitateDriving = m_Vehicle.GetComponent<ImitateDriving>();
            m_rigidbody = m_Vehicle.GetComponent<Rigidbody>();
        }
        void Start()
        {
            TrafficController.centerPoint = camera_Fixed.transform;
            camera_Fixed.enabled = true;
            m_camera.enabled = false;
            repVehicle = null;
            camera_rep = null;
            if (useEnviro)
            {
                //Enviromentcontroller.Camera = camera_Fixed;
            }
        }
        void FixedUpdate()
        {
            VelocityCache();
            if (Input.GetMouseButtonDown(0))
            {
                VehicleChoose();
                isCaching = true;
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                isCaching = false;
                isReplacing = true;
                if (isReplacing)
                {
                    m_rigidbody.velocity = velocity;
                }//继承AI车速度
                if (repVehicle == null)
                {
                    Debug.Log("Target Null");
                }
                else
                {
                    CarReplaced();//注意Car在被replace之后，写在awake里的函数会丢失引用，有关联的脚本需要写到start、OnEnable里，或者等动作完成后再激活受影响的脚本
                    FreezingRotation();
                    Invoke("ReleaseFreezingRotation", duration);//一定时间后执行解锁
                    Invoke("ReleaseFreezingPosition", duration);
                }
                if (camera_rep == null)
                {
                    Debug.Log("Target Null");
                }
                else
                {
                    CameraChange();
                }
            }
            if(!camera_Fixed.enabled && !camera_rep.enabled&&!m_camera.enabled)
            {
                m_camera.enabled = true;
                m_StandardInput.enabled = true;
            }//频繁的相机切换需要CPU上下文检索，有可能会丢失目标，这里检查如果目标丢失了就再次打开
        }
        Vector3 VelocityCache()
        {
            if(isCaching & repVehicle != null)
            {
                velocity = repVehicle.GetComponent<Rigidbody>().velocity;
            }
            return velocity;
        }//获取AI车的速度矢量
        void VehicleChoose()
        {
            Ray myRay = camera_Fixed.ScreenPointToRay(Input.mousePosition);//从固定相机发出射线
            RaycastHit hit;
            if (Physics.Raycast(myRay, out hit))//射线检测AI车（AI车需添加特定的tag）
            {
                if(hit.collider.gameObject.tag== "AITrafficCar")
                {
                    repVehicle = hit.collider.gameObject;
                    camera_rep = repVehicle.transform.Find("Camera").gameObject.GetComponent<Camera>(); //获取检测到的AI车及其子物体和组件
                    camera_Fixed.enabled = false;
                    camera_rep.enabled = true;
                    m_ImitateDriving.ImitatedVehicle = repVehicle;
                    if(useEnviro)
                    {
                        //Enviromentcontroller.Camera = camera_rep;
                    }
                }
            }
        }
        void FreezingRotation()//冻结旋转
        {
            m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;//冻结旋转，驾驶车带着速度传送会和路面碰撞，需要限制运动控制颠簸
        }
        void ReleaseFreezingRotation()//解冻旋转
        {
            m_rigidbody.constraints = RigidbodyConstraints.None;//解冻旋转
            m_rigidbody.ResetInertiaTensor();//锁定旋转后惯性张量将会失效，需要重新计算
        }
        void ReleaseFreezingPosition()//冻结位移
        {
            isCaching = false;
            isReplacing = false;
        }
        void CarReplaced()
        {
            m_VPVehicleToolkit.enabled = !m_VPVehicleToolkit.enabled;
            m_StandardInput.enabled = !m_StandardInput.enabled;
            m_ImitateDriving.enabled = !m_ImitateDriving.enabled;
            Vector3 position = repVehicle.transform.position;
            Quaternion rotation = repVehicle.transform.rotation;
            repVehicle.SetActive(false);//设置AI车辆失活
            m_vehicleController.Reposition(position, rotation);//驾驶车空间转移
        }
            void CameraChange()
        {
            camera_rep.enabled = !camera_rep.enabled;
            m_camera.enabled = !m_camera.enabled;
            TrafficController.centerPoint = m_Vehicle.transform;
            if (useEnviro)
            {
                //Enviromentcontroller.Camera = m_camera;
            }
        }
    }
}
