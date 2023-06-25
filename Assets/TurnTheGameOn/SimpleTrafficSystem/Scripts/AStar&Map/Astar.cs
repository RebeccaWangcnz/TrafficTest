using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using TurnTheGameOn.SimpleTrafficSystem;

public class Astar : MonoBehaviour
{
    //控制粗寻路的变量
    [Tooltip("查找路径点的范围")]
    public float range;//单次查找范围,单次查找范围越小，最终遍历次数越多，行驶路径的精度越高
    [Tooltip("Unity内置寻路组件，需要先渲染导航网格表面才能使用")]
    public NavMeshAgent m_agent;//控制细寻路的组件
    public Transform m_Target;//最终的追逐目标
    private GameObject[] Waypoints;//将所有的waypoint打包成数组
    private List<GameObject> Waypointslist = new List<GameObject>();//将waypoint数组打包成列表
    private AstarPoint[] MapPoint;//这是一个寻路节点类，另有脚本定义
    private List<AstarPoint> MapPointlist = new List<AstarPoint>();//将waypoint制作成A*寻路专用的一个类，并构造数字地图
    private AstarPoint startpoint;//粗寻路起点
    private AstarPoint endpoint;//粗寻路终点
    private Transform m_transform;//导航车的当前位置
    private Ray pointray;//用来探测真实墙的射线检测
    //控制动画的变量
    [Tooltip("车身转动允许的最大角速度")]
    public float maxAugularSpeed;//最大车身转动角速度
    private Rigidbody m_rigidbody;
    private float m_bodyangularspeed;//车身转动角速度
    private float w_angularSpeedx;//轮胎转动角速度
    private float w_angularSpeedy;//前轮舵角速度
    private float w_angularSpeedycache;//上一帧前轮舵角速度
    public Transform[] wheels;
    //控制避障检测的变量
    [Tooltip("所有车的图层")]
    public LayerMask layerMask;
    public Transform frontsensor;
    public Transform leftsensor;
    public Transform righttsensor;
    private bool frontBoxcast;
    private bool leftBoxcast;
    private bool rightBoxcast;
    private bool iscasting = false;
    private RaycastHit frontHit;
    private RaycastHit leftHit;
    private RaycastHit rightHit;
    private Transform waypointcache;//保存前方waypoint，以确保在不满足追逐条件的情况下能使用waypoint作为导航目标
    //控制转向灯（还没写）
    private LineRenderer line;
    void Awake()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        line = GetComponent<LineRenderer>();
        Waypoints = GameObject.FindGameObjectsWithTag("Waypoint");//获取所有的waypoint，打包进入数组
    }
    void Start()
    {
        //剔除没有带WayPoint脚本的错误点，并使用list保存结果
        foreach(GameObject p in Waypoints)
        {
            if(p.GetComponent<AITrafficWaypoint>()!=null)
            {
                Waypointslist.Add(p);
            }
        }
        //提取waypoint坐标信息，存入继承的Astarpoint类，并构建地图数组
        foreach (GameObject Waypoint in Waypointslist)
        {
            AstarPoint pointa = new AstarPoint(Waypoint.transform);
            MapPointlist.Add(pointa);
        }
        Debug.Log(MapPointlist.Count);
        MapPoint = MapPointlist.ToArray();//List转数组，这里没把List释放掉，不知道为什么，总觉得有用
        //设置起终点
        m_transform = this.transform;
        SetStart(m_transform);
        SetEnd(m_Target);
        m_agent.destination = m_Target.position;
    }

    void FixedUpdate()//运动动画及纠正导航
    {
        BodyAngularSpeed();
        WheelMove();
        BoxCastTask();
        Interactive();//空函数预留位，以在特定场景下执行不同动作
        if(m_agent.destination == m_transform.position)//当导航代理不存在目标时（目标坐标等于自己），设置目标为最终追逐的目标
        {
            m_agent.destination = m_Target.position;
            m_agent.speed = 15f;
        }
    }

    void WheelMove()//轮子运动的函数
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
    void BodyAngularSpeed()//车身转动的函数
    {
        if (m_agent.angularSpeed < maxAugularSpeed)
        {
            m_agent.angularSpeed += m_rigidbody.velocity.magnitude * 0.04f;//车身旋转角速度从一个很小的值逐渐加到最大值，模拟方向盘从0开始转，让转向稍微不那么生硬
        }
    }
    void BoxCastTask()
    {
        //前传感器探测,控制速度，避免追尾
        frontBoxcast = Physics.BoxCast(m_transform.position, new Vector3(1.5f, 0.5f, 0.001f), m_transform.forward, out frontHit, m_transform.transform.rotation, 15f, layerMask);
        if (frontBoxcast)
        {
            float fspeed = frontHit.collider.GetComponent<Rigidbody>().velocity.magnitude;
            float distance = frontHit.distance;
            if (m_agent.speed > fspeed) m_agent.speed = fspeed;
            if (distance < 5f)
            {
                m_agent.speed = 0;
            }
        }
        //侧传感器探测，如有侧方来车则取消追踪避免侧击
        leftBoxcast = Physics.BoxCast(m_transform.position, new Vector3(0.001f, 0.5f, 5f), m_transform.right * -1f, out leftHit, m_transform.rotation, 3f, layerMask);
        rightBoxcast = Physics.BoxCast(m_transform.position, new Vector3(0.001f, 0.5f, 5f), m_transform.right, out rightHit, m_transform.rotation, 3f, layerMask);
        if (leftBoxcast | rightBoxcast)
        {
            Debug.Log("侧向来车");
            iscasting = true;
            m_agent.destination = waypointcache.position;
        }
    }
    public void OnReachedWaypoint(AITrafficWaypointSettings onReachWaypointSettings)//粗寻路主函数，函数使用的是Waypoint的触发函数，刚好可以链接到waypoint的系统
    {
        waypointcache = onReachWaypointSettings.nextPointInRoute.transform;//保存前方waypoint，以确保在不满足追逐条件的情况下能使用waypoint作为导航目标
        //当前方向盘转角（车身转向角速度）较大时，重置角速度
        if (m_agent.angularSpeed>=maxAugularSpeed/2f)
        {
            m_agent.angularSpeed = 5f;//重置角速度（设0会更生硬所以设的5）
        }
        //避障检测通过，执行寻路函数
        if (!iscasting)
        {            
            m_agent.speed = onReachWaypointSettings.speedLimit / 3.6f;//导航速度=waypoint限速
            SetStart(m_transform);
            SetEnd(m_Target);//因为是动态寻路，每次查找都需要重置一下起终点
            List<AstarPoint> openList = new List<AstarPoint>();//开列表（查找后合格，待选择的点）
            List<AstarPoint> closeList = new List<AstarPoint>();//关列表（所有走过或被舍弃的点都不可被再次查找）
            List<AstarPoint> parentList = new List<AstarPoint>();//父节点列表（路线由父节点逆溯得到）
            openList.Add(startpoint);
            while (openList.Count > 0)//只要开放列表还存在元素就继续
            {
                AstarPoint point = GetMinFOfList(openList);//选出open集合中F值最小的点
                openList.Remove(point);
                closeList.Add(point);//从open把point移入close（走过或被舍弃）
                List<AstarPoint> SurroundPoints = GetSurroundPoint(point);//查找point附近的点
                foreach (AstarPoint p in closeList)//在周围点中把已经在关闭列表的点删除
                {
                    if (SurroundPoints.Contains(p))
                    {
                        SurroundPoints.Remove(p);
                    }
                }//不再查找关列表
                for (int i = 0; i < openList.Count; i++)
                {
                    if (!SurroundPoints.Contains(openList[i]))
                    {
                        openList.Remove((openList[i]));
                    }
                }//因为查找区域是一个范围，部分点在openlist里但没有在SurroungPoint里就没有被再计算，这里需要舍弃掉
                for (int i=0; i< SurroundPoints.Count;i++)//遍历周围的点
                {
                    if (openList.Contains(SurroundPoints[i]))//周围点已经在开放列表中
                    {
                        //重新计算G,如果比原来的G更小,就更改这个点的父亲
                        float newG = (SurroundPoints[i].Transform.position - point.Transform.position).magnitude + point.G;
                        if (newG < SurroundPoints[i].G)
                        {
                            SurroundPoints[i].SetParent(point,newG);
                        }
                    }
                    else
                    {
                        //设置父亲和F并加入开放列表
                        SurroundPoints[i].parent = point;
                        GetF(SurroundPoints[i]);
                        openList.Add(SurroundPoints[i]);
                    }
                }
                if ((point.Transform.position - endpoint.Transform.position).magnitude<100f)//只要出现终点就结束
                {
                    break;
                }
            }
            parentList.Add(closeList[closeList.Count - 1]);//将最后的查找点（伪目标点）加入父节点列表
            //逆溯父节点，直到起点
            while (parentList[parentList.Count - 1].parent != null)
            {
                parentList.Add(parentList[parentList.Count - 1].parent);
            }
            m_agent.destination = parentList[parentList.Count - 2].Transform.position;//倒数第二个点就是下一步的waypoint
            //画线
            line.positionCount = parentList.Count;
            for(int i = 0;i< line.positionCount;i++)
            {
                Vector3 linepoint = parentList[i].Transform.position;
                line.SetPosition(i, linepoint);
                //Debug.Log(i+"G="+ parentList[i].G+","+ parentList[i].Transform.position);
            }
        }
    }
    public void SetStart(Transform startpos)//设置起点
    {
        startpoint = new AstarPoint(startpos);
    }
    public void SetEnd(Transform endpos)//设置终点
    {
        endpoint = new AstarPoint(endpos);
    }
    public List<AstarPoint> GetSurroundPoint(AstarPoint Point)//寻找周围点
    {
        List<AstarPoint> SurroundPoint = new List<AstarPoint>();
        for (int i = 0; i < MapPoint.Length; i++)
        {
            MapPoint[i].Dis = (MapPoint[i].Transform.position - Point.Transform.position).magnitude;
            //查找条件1：在查找范围内
            if (MapPoint[i].Dis < range)
            {
                Vector3 relativePoint = Point.Transform.InverseTransformPoint(MapPoint[i].Transform.position);
                //查找条件2：至少在前方10m外
                if (relativePoint.z>10f)
                {
                    SurroundPoint.Add(MapPoint[i]);
                }
            }
        }
        return SurroundPoint;
    }
    public void GetF(AstarPoint Point)//设置代价
    {
        float G = 0;
        Vector3 vDis = Point.Transform.position - Point.parent.Transform.position;
        Vector3 forward = Point.parent.Transform.forward;
        if (Point.parent != null)
        {
            Ray pointray = new Ray(Point.parent.Transform.position, vDis);
            RaycastHit hit;
            //寻找墙点和跳跃点，不让走
            if (Point.Transform.position.y - Point.parent.Transform.position.y>3f|| Physics.Raycast(pointray, out hit, vDis.magnitude, 1 << LayerMask.NameToLayer("wall")))
            {
                G =9999;
            }
            //设置单步代价：父点代价+两点间欧拉距离*路线偏移系数（减少不必要的换道且能一定程度提高在弯道区域的路径精度）
            else
            {
                G = (Point.Transform.position - Point.parent.Transform.position).magnitude + Point.parent.G*(1+Vector3.Angle(vDis, forward)/45f);
            }
        }
        float H = (endpoint.Transform.position - Point.Transform.position).magnitude;//预估代价等于到终点的欧拉距离
        float F = H + G;
        Point.H = H;
        Point.G = G;
        Point.F = F;
    }
    public AstarPoint GetMinFOfList(List<AstarPoint> list)//得到一个集合中F值最小的点
    {
        float min = float.MaxValue;
        AstarPoint point = null;
        foreach (AstarPoint p in list)
        {
            if (p.F < min)
            {
                min = p.F;
                point = p;
            }
        }
        return point;
    }
    void Interactive()//需要与驾驶车在什么情况下交互
    {
        //距离100m时跟车
        float distance = (m_Target.position - m_transform.position).magnitude;
        if(distance<100f)
        {
            m_agent.destination = m_Target.position;
        }
    }
}
