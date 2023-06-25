using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using TurnTheGameOn.SimpleTrafficSystem;

public class MiniMap : MonoBehaviour
{
    //�����߱���
    [Tooltip("����·����ķ�Χ")]
    public float range;//���β��ҷ�Χ,���β��ҷ�ΧԽС�����ձ�������Խ�࣬��ʻ·���ľ���Խ��
    public Transform m_Target;//���յ�׷��Ŀ��
    public Transform Car;//����λ��
    [Tooltip("ѡ��һ����NavigationUI���µ�����Ⱦ��")]
    public LineRenderer line;//��������Ⱦ��
    private GameObject[] Waypoints;//�����е�waypoint���������
    private List<GameObject> Waypointslist = new List<GameObject>();//��waypoint���������б�
    private AstarPoint[] MapPoint;//����һ��Ѱ·�ڵ��࣬���нű�����
    private List<AstarPoint> MapPointlist = new List<AstarPoint>();//��waypoint������A*Ѱ·ר�õ�һ���࣬���������ֵ�ͼ
    private List<AstarPoint> parentList = new List<AstarPoint>();//���ڵ��б�·���ɸ��ڵ����ݵõ���
    private AstarPoint startpoint;//��Ѱ·���
    private AstarPoint endpoint;//��Ѱ·�յ�
    private Ray pointray;//����̽����ʵǽ�����߼��
    private float timer;
    //����ͷ�������
    public enum GuideMode
    {
        Vertical,
        Horizontal
    }
    [Tooltip("С��ͼ��ʾģʽ������Ⱦ������ӽ�")]
    public GuideMode mode = GuideMode.Vertical;
    [Tooltip("ѡ��һ������ȾNavigationUI��NavigationArea������")]
    public Transform Camera;
    private Vector3 relativepoint;
    private Vector3 addposition;
    private Quaternion addrotation;
    void Awake()
    {
        Waypoints = GameObject.FindGameObjectsWithTag("Waypoint");//��ȡ���е�waypoint�������������
    }
    void Start()
    {
        float timer = 0;
        //�޳�û�д�WayPoint�ű��Ĵ���㣬��ʹ��list������
        foreach (GameObject p in Waypoints)
        {
            if (p.GetComponent<AITrafficWaypoint>() != null)
            {
                Waypointslist.Add(p);
            }
        }
        //��ȡwaypoint������Ϣ������̳е�Astarpoint�࣬��������ͼ����
        foreach (GameObject Waypoint in Waypointslist)
        {
            AstarPoint pointa = new AstarPoint(Waypoint.transform);
            MapPointlist.Add(pointa);
        }
        MapPoint = MapPointlist.ToArray();
        FindPath();
        DrawLine();
        //���������
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
        //������ˢ����
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
        List<AstarPoint> openList = new List<AstarPoint>();//���б����Һ�ϸ񣬴�ѡ��ĵ㣩
        List<AstarPoint> closeList = new List<AstarPoint>();//���б������߹��������ĵ㶼���ɱ��ٴβ��ң�
        openList.Add(startpoint);
        while (openList.Count > 0)//ֻҪ�����б�����Ԫ�ؾͼ���
        {
            AstarPoint point = GetMinFOfList(openList);//ѡ��open������Fֵ��С�ĵ�
            openList.Remove(point);
            closeList.Add(point);//��open��point����close���߹���������
            List<AstarPoint> SurroundPoints = GetSurroundPoint(point);//����point�����ĵ�
            foreach (AstarPoint p in closeList)//����Χ���а��Ѿ��ڹر��б�ĵ�ɾ��
            {
                if (SurroundPoints.Contains(p))
                {
                    SurroundPoints.Remove(p);
                }
            }//���ٲ��ҹ��б�
            for (int i = 0; i < openList.Count; i++)
            {
                if (!SurroundPoints.Contains(openList[i]))
                {
                    openList.Remove((openList[i]));
                }
            }//��Ϊ����������һ����Χ�����ֵ���openlist�ﵫû����SurroungPoint���û�б��ټ��㣬������Ҫ������
            for (int i = 0; i < SurroundPoints.Count; i++)//������Χ�ĵ�
            {
                if (openList.Contains(SurroundPoints[i]))//��Χ���Ѿ��ڿ����б���
                {
                    //���¼���G,�����ԭ����G��С,�͸��������ĸ���
                    float newG = (SurroundPoints[i].Transform.position - point.Transform.position).magnitude + point.G;
                    if (newG < SurroundPoints[i].G)
                    {
                        SurroundPoints[i].SetParent(point, newG);
                    }
                }
                else
                {
                    //���ø��׺�F�����뿪���б�
                    SurroundPoints[i].parent = point;
                    GetF(SurroundPoints[i]);
                    openList.Add(SurroundPoints[i]);
                }
            }
            if ((point.Transform.position - endpoint.Transform.position).magnitude < 100f)//ֻҪ�����յ�ͽ���
            {
                break;
            }
        }
        parentList.Add(closeList[closeList.Count - 1]);//�����Ĳ��ҵ㣨αĿ��㣩���븸�ڵ��б�
        //���ݸ��ڵ㣬ֱ�����
        while (parentList[parentList.Count - 1].parent != null)
        {
            parentList.Add(parentList[parentList.Count - 1].parent);
        }
    }
    public void SetStart(Transform startpos)//�������
    {
        startpoint = new AstarPoint(startpos);
    }
    public void SetEnd(Transform endpos)//�����յ�
    {
        endpoint = new AstarPoint(endpos);
    }
    public List<AstarPoint> GetSurroundPoint(AstarPoint Point)//Ѱ����Χ��
    {
        List<AstarPoint> SurroundPoint = new List<AstarPoint>();
        for (int i = 0; i < MapPoint.Length; i++)
        {
            MapPoint[i].Dis = (MapPoint[i].Transform.position - Point.Transform.position).magnitude;
            //��������1���ڲ��ҷ�Χ��
            if (MapPoint[i].Dis < range)
            {
                Vector3 relativePoint = Point.Transform.InverseTransformPoint(MapPoint[i].Transform.position);
                //��������2��������ǰ��10m��
                if (relativePoint.z > 10f)
                {
                    SurroundPoint.Add(MapPoint[i]);
                }
            }
        }
        return SurroundPoint;
    }
    public void GetF(AstarPoint Point)//���ô���
    {
        float G = 0;
        Vector3 vDis = Point.Transform.position - Point.parent.Transform.position;
        Vector3 forward = Point.parent.Transform.forward;
        if (Point.parent != null)
        {
            Ray pointray = new Ray(Point.parent.Transform.position, vDis);
            RaycastHit hit;
            //Ѱ��ǽ�����Ծ�㣬������
            if (Point.Transform.position.y - Point.parent.Transform.position.y > 3f || Physics.Raycast(pointray, out hit, vDis.magnitude, 1 << LayerMask.NameToLayer("wall")))
            {
                G = 9999;
            }
            //���õ������ۣ��������+�����ŷ������*·��ƫ��ϵ�������ٲ���Ҫ�Ļ�������һ���̶��������������·�����ȣ�
            else
            {
                G = (Point.Transform.position - Point.parent.Transform.position).magnitude + Point.parent.G * (1 + Vector3.Angle(vDis, forward) / 45f);
            }
        }
        float H = (endpoint.Transform.position - Point.Transform.position).magnitude;//Ԥ�����۵��ڵ��յ��ŷ������
        float F = H + G;
        Point.H = H;
        Point.G = G;
        Point.F = F;
    }
    public AstarPoint GetMinFOfList(List<AstarPoint> list)//�õ�һ��������Fֵ��С�ĵ�
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
