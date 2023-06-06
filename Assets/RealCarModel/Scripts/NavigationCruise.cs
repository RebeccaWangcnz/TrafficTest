using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TurnTheGameOn.SimpleTrafficSystem;

public class NavigationCruise : MonoBehaviour
{
    //����Ѱ·�ı���
    private Transform m_transform;
    private NavMeshAgent m_agent;
    public Transform m_target;
    public float m_speed = 15.0f;
    //����·�����ı���
    private bool ischecking = false;
    private float timer = 0;
    private Rigidbody m_rigidbody;
    private AITrafficWaypointRoute ParentRoute;
    private Transform waypointcache;
    //�����˶������ı���
    private float w_angularSpeedx;
    private float w_angularSpeedy;
    private float w_angularSpeedycache;
    public Transform[] wheels;
    //���Ʊ��ϼ��ı���
    public LayerMask layerMask;
    public Transform frontsensor;
    public Transform leftsensor;
    public Transform righttsensor;
    private bool frontBoxcast;
    private bool leftBoxcast;
    private bool rightBoxcast;
    private bool iscasting;
    private RaycastHit frontHit;
    private RaycastHit leftHit;
    private RaycastHit rightHit;
    void Start()
    {
        m_agent = GetComponent<NavMeshAgent>();
        m_agent.speed = m_speed;
        w_angularSpeedycache = 0;
        m_rigidbody = GetComponent<Rigidbody>();
    }
    void FixedUpdate()
    {
        m_transform = this.transform;
        if(FindDriver())
        {
            m_agent.destination = m_target.position;
            Debug.Log("׷��ģʽ");
        }
        else Debug.Log("ѭ��ģʽ");
        BoxCastTask();
        WheelMove();
    }
    void WheelMove()
    {
        w_angularSpeedx = (m_rigidbody.velocity.magnitude / 0.35f) * 360 * 0.2f;
        w_angularSpeedy = m_rigidbody.angularVelocity.magnitude;
        for (int i = 0; i < wheels.Length; i++)
        {
            wheels[i].Rotate(Vector3.right, w_angularSpeedx * Time.fixedDeltaTime);
            if (i < 2)
            {
                wheels[i].localRotation = Quaternion.AngleAxis((w_angularSpeedy - w_angularSpeedycache) * 2f, Vector3.up) * wheels[i].localRotation;
            }
        }
        w_angularSpeedycache = w_angularSpeedy;
    }
    void BoxCastTask()
    {
        //ǰ������̽��,�����ٶȣ�����׷β
        frontBoxcast = Physics.BoxCast(m_transform.position, new Vector3(1.5f,0.5f,0.001f), m_transform.forward, out frontHit, m_transform.transform.rotation, 15f, layerMask);
        if (frontBoxcast)
        {
            float fspeed = frontHit.collider.GetComponent<Rigidbody>().velocity.magnitude;
            float distance = frontHit.distance;
            if(m_agent.speed> fspeed) m_agent.speed = fspeed;
            if (distance<5f)
            {
                m_agent.speed = 0;
            }
        }
        //�ഫ����̽�⣬���в෽������ȡ��׷�ٱ�����
        leftBoxcast = Physics.BoxCast(m_transform.position, new Vector3(0.001f, 0.5f, 5f), m_transform.right*-1f, out leftHit, m_transform.rotation, 3f, layerMask);
        rightBoxcast = Physics.BoxCast(m_transform.position, new Vector3(0.001f, 0.5f, 5f), m_transform.right, out rightHit, m_transform.rotation, 3f, layerMask);
        if (leftBoxcast| rightBoxcast)
        {
            Debug.Log("��������");
            iscasting = true;
            m_agent.destination = waypointcache.position;
        }
    }
    public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)
    {
        onReachWaypointSettings.OnReachWaypointEvent.Invoke();
        m_agent.speed = onReachWaypointSettings.speedLimit/3.6f;
        m_agent.destination = onReachWaypointSettings.nextPointInRoute.transform.position;
        ParentRoute = onReachWaypointSettings.parentRoute;
        waypointcache = onReachWaypointSettings.nextPointInRoute.transform;
        //���waypoint�ڵ��Ƿ��ܱ�֤ͨ��Ŀ��λ��
        float dis = 99999;
        float min = 99999;
        for (int i = 0; i < ParentRoute.waypointDataList.Count; i++)
        {
            dis = (ParentRoute.waypointDataList[i]._transform.position - m_target.position).magnitude;
            if(dis<min)
            {
                min = dis;//����·���еĵ����Ŀ�����С����
            }//����һ���ڵ㣨��·���ܷ�ͨ��Ŀ��λ�ã�
            for(int j=0;j< ParentRoute.waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints.Length; j++)
            {
                for(int k= 0;k< ParentRoute.waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints[j].onReachWaypointSettings.parentRoute.waypointDataList.Count;k++)
                {
                    dis = (ParentRoute.waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints[j].onReachWaypointSettings.parentRoute.waypointDataList[k]._transform.position - m_target.position).magnitude;
                    if (dis < min)
                    {
                        min = dis;//����·������Ŀ��λ�õ���С����
                    }
                }
            }//���Ҷ����ڵ㣨��·���ķ�֧·���ܷ�ͨ��Ŀ��λ�ã������߼��Ĳ��Һķ���Դ̫��Ͳ��ٽ�����
        }
        if(min>25f)
        {
            ischecking = true;
        }
        else if (onReachWaypointSettings.newRoutePoints.Length > 0)
        {
            ischecking = true;
        }
        else ischecking = false;
    }//��������׷��drivercarʱ������waypointǰ��
    private bool FindDriver()
    {
        timer += Time.fixedDeltaTime;
        if (iscasting)
        {
            return false;
        }
        else if ((m_transform.position - m_target.position).magnitude < 100f)
        {
            return true;
        }
        else if(ischecking)
        {
            return true;
        }
        else if (m_agent.destination != m_target.position && timer < 10f)
        {
            return true;
        }
        else
        {
            return false;
        }
    }//��������ʱ����ʼ׷�ٳ���
}
