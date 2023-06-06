using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VehiclePhysics;
using TurnTheGameOn.SimpleTrafficSystem;
//�����ڼ�ʻ���ϣ�����Ҫ���VehicleReplaceһ��ʹ�ã���������VPP����ģ��ѡ�еĽ�ͨ�����˶�
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
        AITrafficCar_Im = ImitatedVehicle.GetComponent<AITrafficCar>();//����VehicleChoose�ű����øýű�֮��ImitatedVehicle����ֵ����Awake����ִ��������FixedUpdate()֮ǰ����ȡ����ֵд��Awake������ͻ�õ���ֵ
        rigidbody_Im = ImitatedVehicle.GetComponent<Rigidbody>();
        float speed_m = rigidbody_m.velocity.magnitude;
        float speed_Im = rigidbody_Im.velocity.magnitude;
        float acclpencentage = (speed_Im - speed_m) / speed_Im;
        float steer = AITrafficCar_Im.SteeringInput();
        float throttle = acclpencentage * acclpencentage + (1 - acclpencentage) * AITrafficCar_Im.AccelerationInput();//Խ�ӽ�AI���ٶȣ�ʵ�ʶ�������ռ��Խ��ԽԶ��AI���ٶȣ�ʵ���ٶ�����ռ��Խ��
        //m_VPVehicleToolkit.SetSteering(steer);
        if (acclpencentage > 0.1 == true)
        {
           // m_VPVehicleToolkit.SetThrottle(1);//�տ�ʼʱ��������׷
        }
        if (acclpencentage>0 && acclpencentage <= 0.1)
        {
           // m_VPVehicleToolkit.SetThrottle(throttle);//�ٶȽӽ�����ģ�¶���
        }
        if (acclpencentage <= 0 == true)
        {
            //m_VPVehicleToolkit.SetThrottle(0);
            //m_VPVehicleToolkit.SetBrake((float)0.1);
        }
    }
}