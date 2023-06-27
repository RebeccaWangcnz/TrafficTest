namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    [System.Serializable]
    [RequireComponent(typeof(AITrafficWaypointRouteInfo))]
    [HelpURL("https://simpletrafficsystem.turnthegameon.com/documentation/api/aitrafficwaypointroute")]
    public class AITrafficWaypointRoute : MonoBehaviour
    {
        public bool isPeopleRoute;//Rebe:标识一下是否是人行路
        public bool isRegistered { get; private set; }
        [Tooltip("Array of vehicles types that are allowed to spawn and merge onto this route.")]
        public AITrafficVehicleType[] vehicleTypes = new AITrafficVehicleType[1];
        [Tooltip("List of waypoints in the route that cars will use for path-finding.")]
        public List<CarAIWaypointInfo> waypointDataList;
        public bool stopForTrafficLight { get; private set; }
        [Tooltip("Startup traffic will use spawn points, instead of incrementally spawning every other waypoint.")]
        public bool useSpawnPoints;
        [Tooltip("AITrafficController trafficPrefabs array will populate spawnTrafficVehicles array.")]
        public bool spawnFromAITrafficController = false;
        [Tooltip("Amount of cars to spawn.")]
        public int spawnAmount = 1;
        [Tooltip("Array of traffic car prefabs instantiated to the route on startup.")]
        public GameObject[] spawnTrafficVehicles;
        [Tooltip("Reference to the route's AITrafficWaypointRouteInfo script.")]
        public AITrafficWaypointRouteInfo routeInfo;
        [Tooltip("Amount of cars allowed on the route by startup and pooling spawners.")]
        public int maxDensity = 10;
        public int currentDensity; // unreliable if checked outside of AITrafficController update loop
        public int previousDensity; // more reliable if checked outside of AITrafficController update loop
        public Vector3[] PointArray;
        [Tooltip("数据坐标系与unity场景坐标系的偏差")]
        public Vector3 Positionbias;
        public TextAsset pointdata;
        public bool ReadFirstLine;
        private GetLicense cars;
        private LicenseType licensetype;
        private List<Material> materiallistb = new List<Material>();
        private List<Material> materiallistg = new List<Material>();
        private List<Material> materiallisty = new List<Material>();
        private Material[] materialsb;
        private Material[] materialsg;
        private Material[] materialsy;
        public List<Vector3> _positions = new List<Vector3>();
        //编辑器新功能另加的一些参数
        [Tooltip("车道默认速度")]
        public float Avespeed = 80f;
        [Tooltip("点之间间距")]
        public float spacing = 50f;
        [Tooltip("延长的点数量")]
        public float count = 5f;
        [Tooltip("圆曲线控制点1")]
        public Vector3 ControlPoint1;
        [Tooltip("圆曲线控制点2")]
        public Vector3 ControlPoint2;

        [ContextMenu("SetMaxToChildSpawnPointCount")]
        public void SetMaxToChildSpawnPointCount()
        {
            int spawnPoints = GetComponentsInChildren<AITrafficSpawnPoint>().Length;
            maxDensity = spawnPoints;
        }
        private void Awake()
        {
            routeInfo = GetComponent<AITrafficWaypointRouteInfo>();
            materialsg = Resources.LoadAll<Material>("LicenseMaterial/green");
            materialsy = Resources.LoadAll<Material>("LicenseMaterial/yellow");
            materialsb = Resources.LoadAll<Material>("LicenseMaterial/blue");
        }

        private void Start()
        {
            RegisterRoute();
            if (AITrafficController.Instance.usePooling == false)
            {
                SpawnTrafficVehicles();
            }
        }
        #region Traffic Control
        
        public void StopForTrafficlight(bool _stop)
        {
            stopForTrafficLight = routeInfo.stopForTrafficLight = _stop;
            routeInfo.enabled = _stop ? false : true;
        }
        public void RunForTrafficlight(bool _run)
        {
            stopForTrafficLight = routeInfo.runForTrafficLight = _run;
        }//Rebe:需要跑起来

        public List<AITrafficSpawnPoint> spawnpoints = new List<AITrafficSpawnPoint>();

        public void SpawnTrafficVehicles()
        {
            if (spawnFromAITrafficController)
            {
                spawnTrafficVehicles = new GameObject[spawnAmount];
                List<Material> materiallistb = materialsb.ToList();
                List<Material> materiallistg = materialsg.ToList();
                List<Material> materiallisty = materialsy.ToList();
                //存储材质的数组转为列表，数组用于回收材质以及乱序，列表用于匹配和发放车牌
                for (int i = 0; i < spawnTrafficVehicles.Length; i++)
                {
                    int randomPoolIndex = UnityEngine.Random.Range(0, AITrafficController.Instance.trafficPrefabs.Length);
                    spawnTrafficVehicles[i] = AITrafficController.Instance.trafficPrefabs[randomPoolIndex].gameObject;
                    cars = spawnTrafficVehicles[i].GetComponent<GetLicense>();
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
                }
            }
            if (useSpawnPoints)
            {
                spawnpoints = GetComponentsInChildren<AITrafficSpawnPoint>().ToList();
                for (int i = 0; i < spawnTrafficVehicles.Length; i++)
                {
                    if (spawnpoints.Count > 0)
                    {
                        int randomSpawnPointIndex = UnityEngine.Random.Range(0, spawnpoints.Count - 2);
                        Vector3 spawnPosition = spawnpoints[randomSpawnPointIndex].transform.position;
                        Vector3 spawnOffset = new Vector3(0, -4, 0);
                        spawnPosition += spawnOffset;
                        GameObject spawnedTrafficVehicle = Instantiate(spawnTrafficVehicles[i], spawnPosition, spawnpoints[randomSpawnPointIndex].transform.rotation);
                        if (!isPeopleRoute)
                            spawnedTrafficVehicle.GetComponent<AITrafficCar>().RegisterCar(this);
                        else
                            spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(this);//Rebe:注册行人
                        spawnedTrafficVehicle.transform.LookAt(spawnpoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.parentRoute.waypointDataList[spawnpoints[randomSpawnPointIndex].waypoint.onReachWaypointSettings.waypointIndexnumber]._transform);
                        spawnpoints.RemoveAt(randomSpawnPointIndex);
                    }
                }
            }
            else
            {
                for (int i = 0, j = 0; i < spawnTrafficVehicles.Length && j < waypointDataList.Count - 1; i++, j++)
                {
                    Vector3 spawnPosition = waypointDataList[j]._transform.position;
                    spawnPosition.y += 1;
                    GameObject spawnedTrafficVehicle = Instantiate(spawnTrafficVehicles[i], spawnPosition, waypointDataList[j]._transform.rotation);
                    if (!isPeopleRoute)
                        spawnedTrafficVehicle.GetComponent<AITrafficCar>().RegisterCar(this);
                    else
                        spawnedTrafficVehicle.GetComponent<AIPeople>().RegisterPerson(this);//Rebe:注册行人
                    spawnedTrafficVehicle.transform.LookAt(waypointDataList[j + 1]._transform);
                    j += 1; // increase j again tospawn vehicles with more space between
                }
            }
        }
        #endregion

        #region Unity Editor Helper Methods
        bool IsCBetweenAB(Vector3 A, Vector3 B, Vector3 C)
        {
            return (
                Vector3.Dot((B - A).normalized, (C - B).normalized) < 0f && Vector3.Dot((A - B).normalized, (C - A).normalized) < 0f &&
                Vector3.Distance(A, B) >= Vector3.Distance(A, C) &&
                Vector3.Distance(A, B) >= Vector3.Distance(B, C)
                );
        }

#if UNITY_EDITOR
        //测试：画圆曲线（学学数学）
        public Transform[] AddCircle(Vector3 Control1, Vector3 Control2,float Count)
        {
            List<Transform> newPointtrans = new List<Transform>();
            List<GameObject> newWaypoints = new List<GameObject>();
            List<CarAIWaypointInfo> newPoints = new List<CarAIWaypointInfo>();
            Vector3 Control0 = waypointDataList[waypointDataList.Count - 1]._transform.position;//选定第一个控制点为线路终点
            Vector3 xian1 = Control2 - Control0;//画出第一条弦
            Vector3 xian2 = Control1 - Control0;//第二条弦
            Vector3 Normal = Vector3.Cross(xian1, xian2);//三点共面，面公式Ax+By+Cz+D=0；A、B、C为面的法向量标值
            float A1 = Normal.x;
            float B1 = Normal.y;
            float C1 = Normal.z;
            float D1 = -(Normal.x * Control0.x + Normal.y * Control0.y + Normal.z * Control0.z);//三点共面，平面参数为法向量，带入反算
            float A2 = 2f * xian1.x;
            float B2 = 2f * xian1.y;
            float C2 = 2f * xian1.z;
            float D2 = Control0.x*Control0.x+ Control0.y * Control0.y + Control0.z * Control0.z- Control1.x*Control1.x- Control1.y * Control1.y - Control1.z * Control1.z;
            //任意两点在同一大圆面上，大圆面法向量为该两点形成的弦向量的标值
            float A3 = 2f * xian2.x;
            float B3 = 2f * xian2.y;
            float C3 = 2f * xian2.z;
            float D3 = Control0.x * Control0.x + Control0.y * Control0.y + Control0.z * Control0.z - Control2.x * Control2.x - Control2.y * Control2.y - Control2.z * Control2.z;//同上
            Matrix4x4 matrix = new Matrix4x4();//联立三个等式，得到从（x，y，z）到（D1，D2，D3）变换矩阵，3*3的，但Unity只有4*4，因此复合单位阵
            matrix.SetRow(0, new Vector4(A1, B1, C1, 0));
            matrix.SetRow(1, new Vector4(A2, B2, C2, 0));
            matrix.SetRow(2, new Vector4(A3, B3, C3, 0));
            matrix.SetRow(3, new Vector4(0, 0, 0, 1));
            Vector4 vector = new Vector4(D1, D2, D3, 0);
            Matrix4x4 inver = matrix.inverse;//目标是求xyz，系数矩阵逆变换
            Vector4 O = inver.MultiplyVector(vector);//解线性方程组，得到三面交点即圆心坐标
            Vector3 center = new Vector3(O.x, O.y, O.z);
            float radius = (Control2 - center).magnitude;
            Vector3 R0 = Control0 - center;
            Vector3 R1 = Control2 - center;
            float arc = Vector3.Angle(R0, R1);//求圆心角
            float acrper =(float) (arc / Count);
            for (int i = 0; i < Count - 1; i++)
            {
                Vector3 point = Quaternion.AngleAxis(acrper*(i+1), transform.up) * (Control0 - center);//从初始点绕过圆心y轴转动per角度，由于矩阵运算的精度问题，最终的计算结果会有偏差
                Vector3 _position = point + center;//转动量+圆心
                newWaypoints.Add(Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, gameObject.transform) as GameObject);
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._transform = newWaypoints[i].transform;
                newPoint._waypoint = newWaypoints[i].GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.parentRoute = this;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                int insertIndex = waypointDataList.Count;
                newPoint._transform.SetSiblingIndex(insertIndex);
                newPoint._name = "AITrafficWaypoint " + (insertIndex + 1);
                newWaypoints[i].name = "AITrafficWaypoint " + (insertIndex + 1);
                waypointDataList.Add(newPoint);
                newPointtrans.Add(newPoint._transform);               
            }
            return newPointtrans.ToArray();
        }

        public void CleanWaypointDataList()
        {
            while(transform.childCount!=0)
            {
                foreach (Transform child in transform)
                {
                    DestroyImmediate(child.gameObject);
                }
                if (transform.childCount == 0)
                    break;
            }
            waypointDataList.Clear();
        }
        public Transform[] AddWaypointDataList()
        {
            List<Transform> newPoints = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._transform = transform.GetChild(i).transform;
                newPoint._waypoint = transform.GetChild(i).GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.parentRoute = this;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                newPoint._name = "AITrafficWaypoint " + (i + 1);
                newPoints.Add(newPoint._transform);
                waypointDataList.Add(newPoint);
            }
            return newPoints.ToArray();
        }
        public void InsertBetweenPoints()
        {
            int insertIndex = 0;
            List<GameObject> newWaypoints = new List<GameObject>();
            List<CarAIWaypointInfo> newPoints = new List<CarAIWaypointInfo>();
            for (int i =0;i< waypointDataList.Count-1;i++)
            {
                Vector3 _position = (waypointDataList[i]._transform.position + waypointDataList[i + 1]._transform.position)*0.5f;
                newWaypoints.Add(Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, gameObject.transform) as GameObject);
                /*newPoints[i]._transform = newWaypoints[i].transform;
                newPoints[i]._waypoint = newWaypoints[i].GetComponent<AITrafficWaypoint>();
                newPoints[i]._waypoint.onReachWaypointSettings.parentRoute = this;
                newPoints[i]._waypoint.onReachWaypointSettings.speedLimit = 25f;
                newPoints[i]._waypoint.onReachWaypointSettings.averagespeed = 25f;
                newPoints[i]._waypoint.onReachWaypointSettings.sigma = 0;*///这样的调用是错误的，因为类中的结构体未形成实例，其调用为空值
            }
            for (int i = 0; i < newWaypoints.Count; i++)
            {
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._transform = newWaypoints[i].transform;
                newPoint._waypoint = newWaypoints[i].GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.parentRoute = this;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                newPoints.Add(newPoint);
                insertIndex = 2 * i + 1;
                newPoints[i]._transform.SetSiblingIndex(insertIndex);
                newPoint._name = "AITrafficWaypoint " + (insertIndex + 1);
                newWaypoints[i].name = "AITrafficWaypoint " + (insertIndex + 1);
                waypointDataList.Insert(insertIndex, newPoints[i]);
            }
            for (int i = 0; i < waypointDataList.Count; i++)
            {
                int newIndexName = i + 1;
                waypointDataList[i]._transform.gameObject.name = "AITrafficWaypoint " + newIndexName;
                waypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber = i + 1;
            }
        }
        public Transform[] ExtendRoute(float Spacing, float Count)
        {
            int insertIndex = 0;
            List<Transform> newPoints = new List<Transform>();
            List<GameObject> newWaypoints = new List<GameObject>();
            for(int i = 0; i <Count; i++)
            {
                _positions.Add(waypointDataList[waypointDataList.Count - 1]._transform.position+waypointDataList[waypointDataList.Count - 1]._transform.forward* Spacing);
                newWaypoints.Add(Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _positions[i], waypointDataList[waypointDataList.Count - 1]._transform.rotation, gameObject.transform) as GameObject);
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._transform = newWaypoints[i].transform;
                newPoint._waypoint = newWaypoints[i].GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.parentRoute = this;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                insertIndex = waypointDataList.Count - 1;
                newPoint._transform.SetSiblingIndex(insertIndex);
                newPoint._name = "AITrafficWaypoint " + (insertIndex + 1);
                newWaypoints[i].name = "AITrafficWaypoint " + (insertIndex + 1);
                waypointDataList.Add(newPoint);
            }
            return newPoints.ToArray();
        }

        //按点数组坐标生成waypoint
        public Transform[] SpawnPointFromArray()
        {
            List<Transform> newPoints = new List<Transform>();
            for (int i=0;i<PointArray.Length;i++)
            {
                GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, PointArray[i]+ Positionbias, Quaternion.identity, gameObject.transform) as GameObject;
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (waypointDataList.Count + 1);
                newPoint._transform = newWaypoint.transform;
                newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = waypointDataList.Count + 1;
                newPoint._waypoint.onReachWaypointSettings.parentRoute = this;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                newPoints.Add(newPoint._transform);
                waypointDataList.Add(newPoint);
            }
            return newPoints.ToArray();
        }
        //读取文本文件获取点数组
        public void ReadDataFromText(string filePath)
        {
            List<Vector3> PointList = new List<Vector3>();
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    //记录每次读取的一行记录
                    string strLine = "";
                    //记录每行记录中的各字段内容
                    string[] aryLine = null;
                    //标示列数
                    bool ishead = true;
                    //逐行读取CSV中的数据
                    while ((strLine = sr.ReadLine()) != null)
                    {
                        if(!ReadFirstLine)
                        {
                            if(ishead)
                            {
                                ishead = false;
                            }
                            else
                            {
                                aryLine = strLine.Split(',');
                                PointList.Add(new Vector3(float.Parse(aryLine[0]), float.Parse(aryLine[1]), float.Parse(aryLine[2])));
                            }
                        }
                        else
                        {
                            aryLine = strLine.Split(',');
                            PointList.Add(new Vector3(float.Parse(aryLine[0]), float.Parse(aryLine[1]), float.Parse(aryLine[2])));
                        }
                    }
                    sr.Close();
                    fs.Close();
                    PointArray = PointList.ToArray();
                }
            }
        }
        //保存当前点信息为文本
        public void SaveDataFromText()
        {
            //创建表 设置表名
            DataTable dt = new DataTable("Sheet1");
            //创建列 有三列(相当愚蠢的方法)
            DataColumn x = new DataColumn();
            DataColumn y = new DataColumn();
            DataColumn z = new DataColumn();
            dt.Columns.Add(x);
            dt.Columns.Add(y);
            dt.Columns.Add(z);
            //创建行 每一行有三列数据           
            for (int i = 0; i < waypointDataList.Count; i++)
            {
                DataRow dr = dt.NewRow();
                dt.Rows.Add(dr);
                dt.Rows[i][0] = waypointDataList[i]._transform.position.x;
                dt.Rows[i][1] = waypointDataList[i]._transform.position.y;
                dt.Rows[i][2] = waypointDataList[i]._transform.position.z;
            }
            //判断数据表内是否存在数据
            if (dt.Rows.Count < 1)
                return;
            //读取数据表行数和列数
            int rowCount = dt.Rows.Count;
            int colCount = dt.Columns.Count;
            //创建一个StringBuilder存储数据
            StringBuilder stringBuilder = new StringBuilder();
            string CsvPath = Application.streamingAssetsPath + "\\"+ transform.parent.name+transform.name +".csv";
            //读取数据
            for (int i = 0; i < dt.Columns.Count; i++)
            {
                stringBuilder.Append(dt.Columns[i].ColumnName + ",");
            }
            stringBuilder.Append("\r\n");
            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {
                    //使用","分割每一个数值
                    stringBuilder.Append(dt.Rows[i][j] + ",");
                }
                //使用换行符分割每一行
                stringBuilder.Append("\r\n");
            }
            //写入文件
            using (FileStream fileStream = new FileStream(CsvPath, FileMode.Create, FileAccess.Write))
            {
                using (TextWriter textWriter = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    textWriter.Write(stringBuilder.ToString());
                }
            }
        }
        //清空点数组
        public void CleanPointData()
        {
            PointArray = null;
        }
        public Transform ClickToSpawnNextWaypoint(Vector3 _position)
        {
            GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, gameObject.transform) as GameObject;
            CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
            newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (waypointDataList.Count + 1);
            newPoint._transform = newWaypoint.transform;
            newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
            newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = waypointDataList.Count + 1;
            newPoint._waypoint.onReachWaypointSettings.parentRoute = this;
            newPoint._waypoint.onReachWaypointSettings.speedLimit = Avespeed;
            newPoint._waypoint.onReachWaypointSettings.averagespeed = Avespeed;
            newPoint._waypoint.onReachWaypointSettings.sigma = 0;
            waypointDataList.Add(newPoint);
            return newPoint._transform;
        }

        public void ClickToInsertSpawnNextWaypoint(Vector3 _position)
        {
            bool isBetweenPoints = false;
            int insertIndex = 0;
            if (waypointDataList.Count >= 2)
            {
                for (int i = 0; i < waypointDataList.Count - 1; i++)
                {
                    Vector3 point_A = waypointDataList[i]._transform.position;
                    Vector3 point_B = waypointDataList[i + 1]._transform.position;
                    isBetweenPoints = IsCBetweenAB(point_A, point_B, _position);
                    insertIndex = i + 1;
                    if (isBetweenPoints) break;
                }
            }

            GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, gameObject.transform) as GameObject;
            CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
            newPoint._transform = newWaypoint.transform;
            newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
            newPoint._waypoint.onReachWaypointSettings.parentRoute = this;
            newPoint._waypoint.onReachWaypointSettings.speedLimit = Avespeed;
            newPoint._waypoint.onReachWaypointSettings.averagespeed = Avespeed;
            newPoint._waypoint.onReachWaypointSettings.sigma = 0;
            if (isBetweenPoints)
            {
                newPoint._transform.SetSiblingIndex(insertIndex);
                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (insertIndex + 1);
                waypointDataList.Insert(insertIndex, newPoint);
                for (int i = 0; i < waypointDataList.Count; i++)
                {
                    int newIndexName = i + 1;
                    waypointDataList[i]._transform.gameObject.name = "AITrafficWaypoint " + newIndexName;
                    waypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber = i + 1;
                }
            }
            else
            {
                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (waypointDataList.Count + 1);
                newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = waypointDataList.Count + 1;
                waypointDataList.Add(newPoint);
            }
        }
#endif
        #endregion

        #region Gizmos
        private void OnDrawGizmos() { if (STSPrefs.routeGizmos) DrawGizmos(false); }
        private void OnDrawGizmosSelected() { if (STSPrefs.routeGizmos) DrawGizmos(true); }

        [HideInInspector] Transform arrowPointer;
        private Transform junctionPosition;
        private Matrix4x4 previousMatrix;
        private int lookAtIndex;

        private void DrawGizmos(bool selected)
        {
            if (!arrowPointer)
            {
                arrowPointer = new GameObject("ARROWPOINTER").transform;
                arrowPointer.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            // Draw line to new route points
            Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
            for (int i = 0; i < waypointDataList.Count; i++)
            {
                if (waypointDataList[i]._waypoint != null)
                {
                    Gizmos.color = selected ? STSPrefs.selectedJunctionColor : STSPrefs.junctionColor;
                    if (waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints.Length > 0)
                    {
                        for (int j = 0; j < waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints.Length; j++)
                        {
                            if (waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints[j] != null)
                            {
                                Gizmos.DrawLine(waypointDataList[i]._transform.position, waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints[j].transform.position);
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            // Draw line to next waypoint and lane change points
            if (waypointDataList.Count > 1)
            {
                for (int i = 1; i < waypointDataList.Count; i++)
                {
                    Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
                    Gizmos.DrawLine(waypointDataList[i - 1]._transform.position, waypointDataList[i]._transform.position); /// Line to next waypoint
                    if (waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints != null)
                    {
                        for (int j = 0; j < waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints.Count; j++) // lines to lane chane points
                        {
                            if (waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints[j] != null)
                                Gizmos.DrawLine(waypointDataList[i - 1]._transform.position, waypointDataList[i - 1]._waypoint.onReachWaypointSettings.laneChangePoints[j].transform.position);
                        }
                    }
                }
            }

            // Draw Arrows to connecting waypoints
            if (waypointDataList.Count > 1)
            {
                Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
                for (int i = 0; i < waypointDataList.Count; i++)
                {
                    previousMatrix = Gizmos.matrix;
                    if (waypointDataList[waypointDataList.Count - 2]._waypoint != null && waypointDataList[i]._waypoint != null)
                    {
                        arrowPointer.position = i == 0 ? waypointDataList[waypointDataList.Count - 2]._waypoint.transform.position : waypointDataList[i]._waypoint.transform.position;
                        lookAtIndex = i == 0 ? waypointDataList.Count - 1 : i - 1;
                        if (i == 0)
                        {
                            arrowPointer.LookAt(waypointDataList[waypointDataList.Count - 1]._waypoint.transform);
                            arrowPointer.position = waypointDataList[i]._waypoint.transform.position;
                            arrowPointer.Rotate(0, 180, 0);
                        }
                        else arrowPointer.LookAt(waypointDataList[lookAtIndex]._waypoint.transform);
                        Gizmos.matrix = Matrix4x4.TRS(waypointDataList[lookAtIndex]._waypoint.transform.position, arrowPointer.rotation, STSPrefs.arrowScale); // x, x, scale
                        Gizmos.DrawFrustum(Vector3.zero, 10f, 2f, 0f, 5f); // x, width, length, x, x
                    }
                    else
                    {
                        break;
                    }
                    previousMatrix = Gizmos.matrix;
                }
            }

            // Draw Arrows to junctions
            Gizmos.color = selected ? STSPrefs.selectedYieldTriggerColor : STSPrefs.yieldTriggerColor;
            for (int i = 0; i < waypointDataList.Count; i++)
            {
                if (waypointDataList[i]._waypoint != null && waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints != null)
                {
                    for (int j = 0; j < waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints.Count; ++j)
                    {
                        if (waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints[j] != null)
                        {
                            Gizmos.color = selected ? STSPrefs.selectedPathColor : STSPrefs.pathColor;
                            previousMatrix = Gizmos.matrix;
                            junctionPosition = waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints[j].transform;
                            arrowPointer.position = waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints[j].transform.position; //waypointData [i]._transform.position;
                            arrowPointer.LookAt(waypointDataList[i]._transform);
                            Gizmos.matrix = Matrix4x4.TRS(junctionPosition.position, arrowPointer.rotation, STSPrefs.arrowScale); // x, x, scale
                            Gizmos.DrawFrustum(Vector3.zero, 10f, 2f, 0f, 5f); // x, width, length, x, x
                            Gizmos.matrix = previousMatrix;
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            if (routeInfo == null)
            {
                routeInfo = GetComponent<AITrafficWaypointRouteInfo>();
            }
        }
        #endregion

        #region Utility Methods
        public void RegisterRoute()
        {
            if (isRegistered == false)
            {
                AITrafficController.Instance.RegisterAITrafficWaypointRoute(this);
                isRegistered = true;
            }
        }

        public void RemoveRoute()
        {
            if (isRegistered)
            {
                AITrafficController.Instance.RemoveAITrafficWaypointRoute(this);
                isRegistered = false;
            }
        }

        [ContextMenu("ReversePoints")]
        public void ReversePoints()
        {
            List<CarAIWaypointInfo> reversedWaypointDataList = new List<CarAIWaypointInfo>();
            for (int i = waypointDataList.Count - 1; i >= 0; i--)
            {
                reversedWaypointDataList.Add(waypointDataList[i]);
            }
            for (int i = 0; i < reversedWaypointDataList.Count; i++)
            {
                reversedWaypointDataList[i]._transform.gameObject.name = "AITrafficWaypoint " + (i + 1).ToString();
                reversedWaypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber = i + 1;
                reversedWaypointDataList[i]._transform.SetSiblingIndex(i);
            }
            waypointDataList = reversedWaypointDataList;
        }

        [ContextMenu("AlignPoints")]
        public void AlignPoints()
        {
            for (int i = 0; i < waypointDataList.Count - 1; i++)
            {
                waypointDataList[i]._transform.LookAt(waypointDataList[i + 1]._transform);
            }
            if (waypointDataList.Count > 1)
            {
                waypointDataList[waypointDataList.Count - 1]._transform.rotation = waypointDataList[waypointDataList.Count - 2]._transform.rotation;
            }
        }

        [ContextMenu("RefreshPointIndexes")]
        public void RefreshPointIndexes()
        {
            for (int i = 0; i < waypointDataList.Count; i++)
            {
                CarAIWaypointInfo waypointDataListItem = new CarAIWaypointInfo();
                waypointDataListItem._name = "AITrafficWaypoint " + (i + 1).ToString();
                waypointDataListItem._transform = waypointDataList[i]._transform;
                waypointDataListItem._waypoint = waypointDataList[i]._waypoint;
                waypointDataList[i] = waypointDataListItem;
                waypointDataList[i]._waypoint.gameObject.name = waypointDataList[i]._name;
                waypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber = i + 1;
            }
            if (waypointDataList.Count >= 2)
            {
                waypointDataList[waypointDataList.Count - 1]._transform.LookAt(waypointDataList[waypointDataList.Count - 2]._transform);
            }
        }

        [ContextMenu("ClearAllLaneChangePoints")]
        public void ClearAllLaneChangePoints()
        {
            for (int i = 0; i < waypointDataList.Count; i++)
            {
                waypointDataList[i]._waypoint.onReachWaypointSettings.laneChangePoints.Clear();
            }
        }

        [ContextMenu("ClearAllNewRoutePoints")]
        public void ClearAllNewRoutePoints()
        {
            for (int i = 0; i < waypointDataList.Count; i++)
            {
                waypointDataList[i]._waypoint.onReachWaypointSettings.newRoutePoints = new AITrafficWaypoint[0];
            }
        }

        public void RemoveAllSpawnPoints()
        {
            AITrafficSpawnPoint[] spawnPoints = GetComponentsInChildren<AITrafficSpawnPoint>();
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                Destroy(spawnPoints[i].gameObject);
            }
        }

#if UNITY_EDITOR//条件编译，只有满足条件才编译（不是执行）该语句段
        public void SetupRandomSpawnPoints()
        {
            if (waypointDataList.Count > 4)
            {
                RemoveAllSpawnPoints();
                int randomIndex = UnityEngine.Random.Range(1, 3);
                for (int i = randomIndex; i < waypointDataList.Count && i < waypointDataList.Count - 3; i += UnityEngine.Random.Range(1, 3))
                {
                    GameObject loadedSpawnPoint = Instantiate(STSRefs.AssetReferences._AITrafficSpawnPoint, waypointDataList[i]._transform) as GameObject;
                    AITrafficSpawnPoint trafficSpawnPoint = loadedSpawnPoint.GetComponent<AITrafficSpawnPoint>();
                    trafficSpawnPoint.waypoint = trafficSpawnPoint.transform.parent.GetComponent<AITrafficWaypoint>();
                }
            }
        }
#endif
        #endregion
    }
}