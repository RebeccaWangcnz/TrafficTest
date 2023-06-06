using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VehiclePhysics;
using TurnTheGameOn.SimpleTrafficSystem;
//挂载在驾驶车上，且需要配合VehicleReplace一起使用，功能是让VPP车能模仿选中的交通流车运动
[RequireComponent(typeof(VPVehicleToolkit))]
public class ImitateDriving : MonoBehaviour
{
    [SerializeField] private bool m_Driving = true;
    public GameObject ImitatedVehicle;
    private VPVehicleToolkit m_VPVehicleToolkit;
    private AITrafficCar AITrafficCar_Im;
    private Rigidbody rigidbody_Im;
    private Rigidbody rigidbody_m;
    void Awake()
    {
        m_VPVehicleToolkit = GetComponent<VPVehicleToolkit>();
        rigidbody_m = GetComponent<Rigidbody>();
    }
    void Start()
    {

    }
    void FixedUpdate()
    {
        if (ImitatedVehicle == null || !m_Driving)
        {
            m_VPVehicleToolkit.StopEngine();
        }
        else
        {
            m_VPVehicleToolkit.StartEngine();
            Move();
        }
    }

    public void Move()
    {
        AITrafficCar_Im = ImitatedVehicle.GetComponent<AITrafficCar>();//由于VehicleChoose脚本调用该脚本之后ImitatedVehicle才有值，而Awake（）执行在所有FixedUpdate()之前，获取对象赋值写在Awake（）里就会得到空值
        rigidbody_Im = ImitatedVehicle.GetComponent<Rigidbody>();
        float speed_m = rigidbody_m.velocity.magnitude;
        float speed_Im = rigidbody_Im.velocity.magnitude;
        float acclpencentage = (speed_Im - speed_m) / speed_Im;
        float steer = AITrafficCar_Im.SteeringInput();
        float throttle = acclpencentage * acclpencentage + (1 - acclpencentage) * AITrafficCar_Im.AccelerationInput();//越接近AI车速度，实际动力因素占比越大；越远离AI车速度，实际速度因素占比越大
        //m_VPVehicleToolkit.SetSteering(steer);
        if (acclpencentage > 0.1 == true)
        {
           // m_VPVehicleToolkit.SetThrottle(1);//刚开始时加足马力追
        }
        if (acclpencentage>0 && acclpencentage <= 0.1)
        {
           // m_VPVehicleToolkit.SetThrottle(throttle);//速度接近了再模仿动力
        }
        if (acclpencentage <= 0 == true)
        {
            //m_VPVehicleToolkit.SetThrottle(0);
            //m_VPVehicleToolkit.SetBrake((float)0.1);
        }
    }
}