using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using TurnTheGameOn.SimpleTrafficSystem;

public class MiniMap : MonoBehaviour
{
    //导航线变量
    [Tooltip("查找路径点的范围")]
    public float range;//单次查找范围,单次查找范围越小，最终遍历次数越多，行驶路径的精度越高
    public Transform m_Target;//最终的追逐目标
    public Transform Car;//车辆位置
    [Tooltip("选择一个在NavigationUI层下的线渲染器")]
    public LineRenderer line;//导航线渲染器
    private GameObject[] Waypoints;//将所有的waypoint打包成数组
    private List<GameObject> Waypointslist = new List<GameObject>();//将waypoint数组打包成列表
    private AstarPoint[] MapPoint;//这是一个寻路节点类，另有脚本定义
    private List<AstarPoint> MapPointlist = new List<AstarPoint>();//将waypoint制作成A*寻路专用的一个类，并构造数字地图
    private List<AstarPoint> parentList = new List<AstarPoint>();//父节点列表（路线由父节点逆溯得到）
    private AstarPoint startpoint;//粗寻路起点
    private AstarPoint endpoint;//粗寻路终点
    private Ray pointray;//用来探测真实墙的射线检测
    private float timer;
    //摄像头跟随变量
    public enum GuideMode
    {
        Vertical,
        Horizontal
    }
    [Tooltip("小地图显示模式，即渲染相机的视角")]
    public GuideMode mode = GuideMode.Vertical;
    [Tooltip("选择一个仅渲染NavigationUI和NavigationArea层的相机")]
    public Transform Camera;
    private Vector3 relativepoint;
    private Vector3 addposition;
    private Quaternion addrotation;
    void Awake()
    {
        Waypoints = GameObject.FindGameObjectsWithTag("Waypoint");//获取所有的waypoint，打包进入数组
    }
    void Start()
    {
        float timer = 0;
        //剔除没有带WayPoint脚本的错误点，并使用list保存结果
        foreach (GameObject p in Waypoints)
        {
            if (p.GetComponent<AITrafficWaypoint>() != null)
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
        MapPoint = MapPointlist.ToArray();
        FindPath();
        DrawLine();
        //摄像机参数
        switch (mode)
        {
            case GuideMode.Vertical:
                {
                    addposition = new Vector3(0, 80f, 25f);
                    addrotation = Quaternion.Euler(90f, 0, 0);
                    break;
                }
            case GuideMode.Horizontal:
                {
                    addposition = new Vector3(0, 60f, -50f);
                    addrotation = Quaternion.Euler(35f, 0, 0);
                    break;
                }
        }
        relativepoint = Car.InverseTransformPoint(Car.position) + addposition;
        Camera.transform.position = Car.TransformPoint(relativepoint);
    }
    void FixedUpdate()
    {
        //导航线刷新率
        timer += Time.fixedDeltaTime;
        if (timer > 10f)
        {
            FindPath();
            DrawLine();
            timer = 0;
        }
        Camera.transform.position = Car.TransformPoint(relativepoint);
        Camera.transform.rotation = Car.rotation * addrotation;
    }
    void DrawLine()
    {
        line.positionCount = parentList.Count;
        for (int i = 0; i < parentList.Count; i++)
        {
            Vector3 linepoint = parentList[i].Transform.position + new Vector3(0, 0.5f, 0);
            line.SetPosition(i, linepoint);
        }
    }
    void FindPath()
    {
        parentList.Clear();
        SetStart(Car);
        SetEnd(m_Target);
        List<AstarPoint> openList = new List<AstarPoint>();//开列表（查找后合格，待选择的点）
        List<AstarPoint> closeList = new List<AstarPoint>();//关列表（所有走过或被舍弃的点都不可被再次查找）
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
            for (int i = 0; i < SurroundPoints.Count; i++)//遍历周围的点
            {
                if (openList.Contains(SurroundPoints[i]))//周围点已经在开放列表中
                {
                    //重新计算G,如果比原来的G更小,就更改这个点的父亲
                    float newG = (SurroundPoints[i].Transform.position - point.Transform.position).magnitude + point.G;
                    if (newG < SurroundPoints[i].G)
                    {
                        SurroundPoints[i].SetParent(point, newG);
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
            if ((point.Transform.position - endpoint.Transform.position).magnitude < 100f)//只要出现终点就结束
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
                if (relativePoint.z > 10f)
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
            if (Point.Transform.position.y - Point.parent.Transform.position.y > 3f || Physics.Raycast(pointray, out hit, vDis.magnitude, 1 << LayerMask.NameToLayer("wall")))
            {
                G = 9999;
            }
            //设置单步代价：父点代价+两点间欧拉距离*路线偏移系数（减少不必要的换道且能一定程度提高在弯道区域的路径精度）
            else
            {
                G = (Point.Transform.position - Point.parent.Transform.position).magnitude + Point.parent.G * (1 + Vector3.Angle(vDis, forward) / 45f);
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
}
