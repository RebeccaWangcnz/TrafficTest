namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using System.Collections.Generic;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(AITrafficWaypointRoute))]
    public class Editor_AITrafficWaypointRoute : Editor
    {
        private static int tab;
        AITrafficWaypointRoute circuit;
        private string filePath;
        public bool clickcheck1 = false;
        public bool clickcheck2 = false;
        public Vector3 ControlPoint1;
        public Vector3 ControlPoint2;
        public enum EditMode
        {
            InsertBetweenPoints,
            Extend,
            Circle
        }
        public EditMode mode;
        void OnEnable()
        {
            circuit = (AITrafficWaypointRoute)target;//目标对应脚本
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();//每帧更新一次所序列化的游戏对象信息
            EditorGUILayout.BeginVertical(EditorStyles.inspectorFullWidthMargins);//生成一个垂直（排布）组，全宽排布
            EditorStyles.label.wordWrap = true;//label自动换行

            EditorGUILayout.HelpBox("Shift + Left Click in scene view on a Collider to add new points to the route", MessageType.None);
            EditorGUILayout.HelpBox("Shift + Ctrl + Left Click in scene view on a Collider to insert new points to the route", MessageType.None);
            EditorGUILayout.HelpBox("Shift + Right Click in scene view on a Collider to add a Yield Trigger to the route", MessageType.None);

            AITrafficWaypointRoute _AITrafficWaypointRoute = (AITrafficWaypointRoute)target;
            tab = GUILayout.Toolbar(tab, new string[] { "Settings", "Batch", "Edit"});
            EditorGUILayout.BeginVertical("Box");
            switch (tab)
            {
                case 0:
                    SerializedProperty isPeopleRoute = serializedObject.FindProperty("isPeopleRoute");//序列化变量，以读写数值据
                    EditorGUI.BeginChangeCheck();//值变化
                    EditorGUILayout.PropertyField(isPeopleRoute, true);//序列化变量“声明”
                    if (EditorGUI.EndChangeCheck())//完成值变化
                        serializedObject.ApplyModifiedProperties();//保存

                    SerializedProperty isCrossRoad = serializedObject.FindProperty("isCrossRoad");//Rebe0627
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(isCrossRoad, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty useSpawnPoints = serializedObject.FindProperty("useSpawnPoints");//序列化变量，以读写数值据
                    EditorGUI.BeginChangeCheck();//值变化
                    EditorGUILayout.PropertyField(useSpawnPoints, true);//序列化变量“声明”
                    if (EditorGUI.EndChangeCheck())//完成值变化
                        serializedObject.ApplyModifiedProperties();//保存
                    
                    SerializedProperty spawnFromAITrafficController = serializedObject.FindProperty("spawnFromAITrafficController");
                    EditorStyles.label.wordWrap = false;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(spawnFromAITrafficController, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    if (_AITrafficWaypointRoute.spawnFromAITrafficController)
                    {
                        SerializedProperty spawnAmount = serializedObject.FindProperty("spawnAmount");
                        EditorGUI.BeginChangeCheck();
                        EditorGUILayout.PropertyField(spawnAmount, true);
                        if (EditorGUI.EndChangeCheck())
                            serializedObject.ApplyModifiedProperties();
                    }

                    SerializedProperty Avespeed = serializedObject.FindProperty("Avespeed");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(Avespeed, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty spawnTrafficVehicles = serializedObject.FindProperty("spawnTrafficVehicles");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(spawnTrafficVehicles, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.EndVertical();


                    EditorGUILayout.BeginVertical("Box");

                    SerializedProperty maxDensity = serializedObject.FindProperty("maxDensity");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(maxDensity, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty vehicleTypes = serializedObject.FindProperty("vehicleTypes");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(vehicleTypes, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.EndVertical();


                    EditorGUILayout.BeginVertical("Box");

                    SerializedProperty waypointDataList = serializedObject.FindProperty("waypointDataList");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(waypointDataList, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    if (GUILayout.Button(new GUIContent("Reverse Waypoints", "Reverses all waypoints in the route's waypointDataList.")))
                    {
                        circuit.ReversePoints();
                        EditorUtility.SetDirty(this);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }

                    if (GUILayout.Button(new GUIContent("Align Waypoints", "Aligns the rotation of all waypoints to face toward the next point.")))
                    {
                        circuit.AlignPoints();
                        EditorUtility.SetDirty(this);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }

                    if (GUILayout.Button(new GUIContent("Setup Random Spawn Points", "First removes all spawn points, then randomly adds new spawn points.")))
                    {

                        if (circuit.waypointDataList.Count > 4)
                        {
                            Undo.RegisterFullObjectHierarchyUndo(circuit.gameObject, "Remove All Spawn Points");
                            AITrafficSpawnPoint[] spawnPoints = circuit.GetComponentsInChildren<AITrafficSpawnPoint>();
                            for (int i = 0; i < spawnPoints.Length; i++)
                            {
                                string message = "removing old spawn point " + i.ToString() + "/" + spawnPoints.Length.ToString();
                                EditorUtility.DisplayProgressBar("Setup Random Spawn Points", message, i / (float)spawnPoints.Length);
                                Undo.DestroyObjectImmediate(spawnPoints[i].gameObject);
                            }
                            int randomIndex = UnityEngine.Random.Range(0, 3);
                            for (int i = randomIndex; i < circuit.waypointDataList.Count && i < circuit.waypointDataList.Count - 2; i += UnityEngine.Random.Range(2, 4))
                            {
                                string message = "updating route point " + i.ToString() + "/" + circuit.waypointDataList.Count.ToString();
                                EditorUtility.DisplayProgressBar("Setup Random Spawn Points", message, i / (float)circuit.waypointDataList.Count);
                                GameObject loadedSpawnPoint = Instantiate(STSRefs.AssetReferences._AITrafficSpawnPoint, circuit.waypointDataList[i]._transform) as GameObject;
                                Undo.RegisterCreatedObjectUndo(loadedSpawnPoint, "AITrafficSpawnPoint");
                                AITrafficSpawnPoint trafficSpawnPoint = loadedSpawnPoint.GetComponent<AITrafficSpawnPoint>();
                                trafficSpawnPoint.waypoint = trafficSpawnPoint.transform.parent.GetComponent<AITrafficWaypoint>();
                            }
                            Undo.FlushUndoRecordObjects();
                            EditorUtility.SetDirty(this);
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                            EditorUtility.ClearProgressBar();
                        }
                    }

                    if (GUILayout.Button(new GUIContent("Remove All Spawn Points", "Removes all spawn points from the route.")))
                    {
                        Undo.RegisterFullObjectHierarchyUndo(circuit.gameObject, "Remove All Spawn Points");
                        AITrafficSpawnPoint[] spawnPoints = circuit.GetComponentsInChildren<AITrafficSpawnPoint>();
                        for (int i = 0; i < spawnPoints.Length; i++)
                        {
                            string message = "removing spawn point " + i.ToString() + "/" + spawnPoints.Length.ToString();
                            EditorUtility.DisplayProgressBar("Remove All Spawn Points", message, i / (float)spawnPoints.Length);
                            Undo.DestroyObjectImmediate(spawnPoints[i].gameObject);
                        }
                        Undo.FlushUndoRecordObjects();
                        EditorUtility.SetDirty(this);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        EditorUtility.ClearProgressBar();
                    }
                    EditorGUILayout.Space(10);
                    EditorGUILayout.BeginVertical("Box");
                    if (GUILayout.Button(new GUIContent("CleanWaypointDataList", "删除所有Waypoint(不可撤回)")))
                    {
                        circuit.CleanWaypointDataList();
                        EditorUtility.SetDirty(this);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        EditorUtility.ClearProgressBar();
                    }
                    if (GUILayout.Button(new GUIContent("AddWaypointDataList", "把层级面板中Waypoint物体导入脚本中")))
                    {
                        circuit.AddWaypointDataList();
                        EditorUtility.SetDirty(this);//将指定的对象标记为已修改，确保场景保存时，对该对象所做的更改被保存
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());//将当前活动场景标记为已修改，确保场景保存时，对场景的更改被保存。
                        EditorUtility.ClearProgressBar();//清除进度条
                    }
                    EditorGUILayout.EndVertical();
                    break;
                case 1:
                    EditorGUILayout.BeginVertical("Box");

                    SerializedProperty PointArray = serializedObject.FindProperty("PointArray");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(PointArray, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty Positionbias = serializedObject.FindProperty("Positionbias");
                    if(Positionbias.vector3Value == new Vector3(0,0,0))
                        Positionbias.vector3Value = circuit.transform.position;
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(Positionbias, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.HelpBox("默认的坐标偏差值为挂载物体与Unity世界坐标的偏差", MessageType.None);

                    if (GUILayout.Button(new GUIContent("AddWaypoint", "从数组里生成Waypoint")))
                    {
                        SpawnPointFromArray(_AITrafficWaypointRoute);
                        EditorUtility.SetDirty(this);
                        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                    }

                    if (GUILayout.Button(new GUIContent("CleanPoints", "清空Waypoint数组")))
                    {
                        circuit.CleanPointData();
                        EditorUtility.SetDirty(this);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("拖曳文件到下方文本框，出现路径即为完成获取");
                    EditorGUILayout.BeginHorizontal("Box");
                    GUIContent title = new GUIContent("Drag Object here from Project view to get the object");
                    Rect PathRect = EditorGUILayout.GetControlRect(GUILayout.Height(100));
                    //EditorGUI.BeginChangeCheck();
                    filePath = EditorGUI.TextField(PathRect, filePath);
                    // 判断当前鼠标正拖拽某对象或者在拖拽的过程中松开了鼠标按键
                    // 同时还需要判断拖拽时鼠标所在位置处于文本输入框内
                    if ((Event.current.type == EventType.DragUpdated
                        || Event.current.type == EventType.DragExited)
                        && PathRect.Contains(Event.current.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                        // 判断是否拖拽了文件
                        if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                        {
                            filePath = DragAndDrop.paths[0];
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal("Box", GUILayout.Width(200));
                    EditorGUILayout.LabelField("第一行是否可读：", GUILayout.Width(100));
                    SerializedProperty ReadFirstLine = serializedObject.FindProperty("ReadFirstLine");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(ReadFirstLine, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.HelpBox("读取所有ASCII文本格式（txt、csv、json、doc），但必须由半角逗号做分隔符", MessageType.None);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button(new GUIContent("ReadData", "读取点坐标信息")))
                    {
                        circuit.ReadDataFromText(filePath);
                        EditorUtility.SetDirty(this);
                    }
                    if (GUILayout.Button(new GUIContent("SaveData", "保存点坐标信息为CSV")))
                    {
                        circuit.SaveDataFromText();
                        EditorUtility.SetDirty(this);
                    }
                    break;
                case 2:
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.BeginHorizontal();
                    mode = (EditMode)EditorGUILayout.EnumPopup("请选择操作：",mode);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(10);
                    switch (mode)
                    {
                        case EditMode.InsertBetweenPoints:
                            EditorGUILayout.BeginVertical();
                            if (GUILayout.Button(new GUIContent("InsertBetweenPoints", "在两点中间插入新的点")))
                            {
                                InsertBetweenPoints(_AITrafficWaypointRoute);
                                Undo.RegisterFullObjectHierarchyUndo(circuit.gameObject, "InsertBetweenPoints");
                                foreach(Transform child in circuit.transform)
                                Undo.RegisterCreatedObjectUndo(child.gameObject, "InsertBetweenPoints");
                                Undo.FlushUndoRecordObjects();
                                EditorUtility.SetDirty(this);
                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                                EditorUtility.ClearProgressBar();
                            }
                            EditorGUILayout.EndVertical();
                            break;
                        case EditMode.Extend:
                            EditorGUILayout.BeginVertical();

                            SerializedProperty spacing = serializedObject.FindProperty("spacing");
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(spacing, true);
                            if (EditorGUI.EndChangeCheck())
                                serializedObject.ApplyModifiedProperties();

                            SerializedProperty count = serializedObject.FindProperty("count");
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(count, true);
                            if (EditorGUI.EndChangeCheck())
                                serializedObject.ApplyModifiedProperties();

                            if (GUILayout.Button(new GUIContent("ExtendRoute", "直线延长Route")))
                            {
                                ExtendRoute(_AITrafficWaypointRoute,spacing.floatValue,count.floatValue);
                                Undo.RegisterFullObjectHierarchyUndo(circuit.gameObject, "ExtendRoute");
                                foreach (Transform child in circuit.transform)
                                    Undo.RegisterCreatedObjectUndo(child.gameObject, "ExtendRoute");
                                Undo.FlushUndoRecordObjects();
                                EditorUtility.SetDirty(this);
                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                                EditorUtility.ClearProgressBar();
                            }
                            EditorGUILayout.EndVertical();
                            break;
                        case EditMode.Circle:
                            EditorGUILayout.HelpBox("使用三点定圆得到圆曲线，再插值生成waypoint，第一个坐标点默认为当前线路的终点，不可更改", MessageType.None);
                            EditorGUILayout.HelpBox("选点时，先点击ChoosePoint按钮，再按住Ctrl+鼠标左键在场景中点选", MessageType.None);
                            EditorGUI.BeginChangeCheck();
                            ControlPoint1 = EditorGUILayout.Vector3Field("圆曲线控制点1",ControlPoint1);
                            if (EditorGUI.EndChangeCheck())
                                serializedObject.ApplyModifiedProperties();
                            if (GUILayout.Button(new GUIContent("ChoosePoint1", "Ctrl+鼠标左键点选")))
                            {
                                clickcheck1 = true;
                                clickcheck2 = false;
                            }

                            EditorGUI.BeginChangeCheck();
                            ControlPoint2 = EditorGUILayout.Vector3Field("圆曲线控制点2",ControlPoint2) ;
                            if (EditorGUI.EndChangeCheck())
                                serializedObject.ApplyModifiedProperties();
                            if (GUILayout.Button(new GUIContent("ChoosePoint2", "Ctrl+鼠标左键点选")))
                            {
                                clickcheck2 = true;
                                clickcheck1 = false;
                            }

                            SerializedProperty Count = serializedObject.FindProperty("count");
                            EditorGUI.BeginChangeCheck();
                            EditorGUILayout.PropertyField(Count, true);
                            if (EditorGUI.EndChangeCheck())
                                serializedObject.ApplyModifiedProperties();

                            if (GUILayout.Button(new GUIContent("AddCircle", "延申一段圆曲线")))
                            {
                                clickcheck1 = false;
                                clickcheck2 = false;
                                AddCircle(_AITrafficWaypointRoute,ControlPoint1, ControlPoint2,Count.floatValue);
                                EditorUtility.SetDirty(this);
                                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                                EditorUtility.ClearProgressBar();
                            }
                            break;
                    }

                    EditorGUILayout.EndVertical();
                    break;

            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }

        void OnSceneGUI()
        {
            AITrafficWaypointRoute _AITrafficWaypointRoute = (AITrafficWaypointRoute)target;

            for (int i = 0; i < _AITrafficWaypointRoute.waypointDataList.Count; i++)
            {
                if (_AITrafficWaypointRoute.waypointDataList[i]._waypoint)
                {
                    GUIStyle style = new GUIStyle();
                    string target = "";
                    style.normal.textColor = Color.green;
                    Handles.Label(_AITrafficWaypointRoute.waypointDataList[i]._waypoint.transform.position + new Vector3(0, 0.25f, 0),
                    "    Waypoint:   " + _AITrafficWaypointRoute.waypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber.ToString() + "\n" +
                    "    SpeedLimit: " + _AITrafficWaypointRoute.waypointDataList[i]._waypoint.onReachWaypointSettings.speedLimit + "\n" +
                    target,
                    style
                    );
                }
            }

            Event e = Event.current;
            if(clickcheck1)
            {
                if (e.control)
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        int controlId = GUIUtility.GetControlID(FocusType.Passive);
                        Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                        RaycastHit hitInfo;
                        if (Physics.Raycast(worldRay, out hitInfo))
                        {
                            ControlPoint1 = hitInfo.point;
                        }
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }
                }
            }
            if (clickcheck2)
            {
                if (e.control)
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        int controlId = GUIUtility.GetControlID(FocusType.Passive);
                        Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                        RaycastHit hitInfo;
                        if (Physics.Raycast(worldRay, out hitInfo))
                        {
                            ControlPoint2 = hitInfo.point;
                        }
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }
                }
            }
            if (e.shift)
            {
                if (e.control)
                {
                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        int controlId = GUIUtility.GetControlID(FocusType.Passive);
                        Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                        RaycastHit hitInfo;
                        if (Physics.Raycast(worldRay, out hitInfo))
                        {
                            ClickToInsertSpawnNextWaypoint(_AITrafficWaypointRoute, hitInfo.point);
                        }
                        GUIUtility.hotControl = controlId;
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseDown && e.button == 1)
                {
                    int controlId = GUIUtility.GetControlID(FocusType.Passive);
                    Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    RaycastHit hitInfo;
                    if (Physics.Raycast(worldRay, out hitInfo))
                    {
                        ClickToSpawnYieldTrigger(_AITrafficWaypointRoute, hitInfo.point);
                    }
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                else if (e.type == EventType.MouseDown && e.button == 0)
                {
                    int controlId = GUIUtility.GetControlID(FocusType.Passive);
                    Ray worldRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                    RaycastHit hitInfo;
                    if (Physics.Raycast(worldRay, out hitInfo))
                    {
                        ClickToSpawnNextWaypoint(_AITrafficWaypointRoute, hitInfo.point);
                    }
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
            }
        }

        public Transform ClickToSpawnYieldTrigger(AITrafficWaypointRoute __AITrafficWaypointRoute, Vector3 _position)
        {
            AITrafficWaypointRouteInfo routeInfo = __AITrafficWaypointRoute.GetComponent<AITrafficWaypointRouteInfo>();
            if (routeInfo.yieldTrigger == null)
            {
                // this needs to spawn a yield trigger
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("ClickToSpawnYieldTrigger");
                var undoGroupIndex = Undo.GetCurrentGroup();
                Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ClickToSpawnYieldTrigger");
                Undo.RegisterCompleteObjectUndo(routeInfo, "ClickToSpawnYieldTrigger");
                GameObject yieldTrigger = Instantiate(STSRefs.AssetReferences._YieldTrigger, _position, Quaternion.identity, __AITrafficWaypointRoute.transform) as GameObject;
                routeInfo.yieldTrigger = yieldTrigger.GetComponent<BoxCollider>();
                Undo.RegisterCreatedObjectUndo(yieldTrigger, "ClickToSpawnYieldTrigger");
                Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ClickToSpawnYieldTrigger");
                Undo.CollapseUndoOperations(undoGroupIndex);
                return yieldTrigger.transform;
            }
            else
            {
                return null;
            }
        }

        //按点数组坐标生成waypoint
        public Transform[] SpawnPointFromArray(AITrafficWaypointRoute __AITrafficWaypointRoute)
        {
            //Undo回调函数，用于ctrl+z撤回操作
            Undo.IncrementCurrentGroup();//生成回调组
            Undo.SetCurrentGroupName("SpawnPointFromArray");//回调组命名
            var undoGroupIndex = Undo.GetCurrentGroup();//回调组声明并赋予ID
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "SpawnPointFromArray");//撤回生成的物体（物体名，组名）
            List<Transform> newPoints = new List<Transform>();
            for (int i = 0; i < __AITrafficWaypointRoute.PointArray.Length; i++)
            {
                GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, __AITrafficWaypointRoute.PointArray[i] + __AITrafficWaypointRoute.Positionbias, Quaternion.identity, __AITrafficWaypointRoute.transform) as GameObject;
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (__AITrafficWaypointRoute.waypointDataList.Count + 1);
                newPoint._transform = newWaypoint.transform;
                newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = __AITrafficWaypointRoute.waypointDataList.Count + 1;
                newPoint._waypoint.onReachWaypointSettings.parentRoute = __AITrafficWaypointRoute;;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                newPoints.Add(newPoint._transform);
                __AITrafficWaypointRoute.waypointDataList.Add(newPoint);
                Undo.RegisterCreatedObjectUndo(newWaypoint, "SpawnPointFromArray");
            }           
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "SpawnPointFromArray");
            Undo.CollapseUndoOperations(undoGroupIndex);
            return newPoints.ToArray();
        }

        public void InsertBetweenPoints(AITrafficWaypointRoute __AITrafficWaypointRoute)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("InsertBetweenPoints");
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "InsertBetweenPoints");
            int insertIndex = 0;
            List<GameObject> newWaypoints = new List<GameObject>();
            List<CarAIWaypointInfo> newPoints = new List<CarAIWaypointInfo>();
            for (int i = 0; i < __AITrafficWaypointRoute.waypointDataList.Count - 1; i++)
            {
                Vector3 _position = (__AITrafficWaypointRoute.waypointDataList[i]._transform.position + __AITrafficWaypointRoute.waypointDataList[i + 1]._transform.position) * 0.5f;
                newWaypoints.Add(Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, __AITrafficWaypointRoute.transform) as GameObject);
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
                newPoint._waypoint.onReachWaypointSettings.parentRoute = circuit;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                newPoints.Add(newPoint);
                insertIndex = 2 * i + 1;
                newPoints[i]._transform.SetSiblingIndex(insertIndex);
                newPoint._name = "AITrafficWaypoint " + (insertIndex + 1);
                newWaypoints[i].name = "AITrafficWaypoint " + (insertIndex + 1);
                __AITrafficWaypointRoute.waypointDataList.Insert(insertIndex, newPoints[i]);
                Undo.RegisterCreatedObjectUndo(newWaypoints[i], "InsertBetweenPoints");
            }
            for (int i = 0; i < __AITrafficWaypointRoute.waypointDataList.Count; i++)
            {
                int newIndexName = i + 1;
                __AITrafficWaypointRoute.waypointDataList[i]._transform.gameObject.name = "AITrafficWaypoint " + newIndexName;
                __AITrafficWaypointRoute.waypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber = i + 1;
            }
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "InsertBetweenPoints");
        }

        public Transform[] AddCircle(AITrafficWaypointRoute __AITrafficWaypointRoute,Vector3 Control1, Vector3 Control2, float Count)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("SpawnPointFromArray");
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "SpawnPointFromArray");
            List<Transform> newPointtrans = new List<Transform>();
            List<GameObject> newWaypoints = new List<GameObject>();
            List<CarAIWaypointInfo> newPoints = new List<CarAIWaypointInfo>();
            Vector3 Control0 = __AITrafficWaypointRoute.waypointDataList[__AITrafficWaypointRoute.waypointDataList.Count - 1]._transform.position;
            Vector3 xian1 = Control2 - Control0;
            Vector3 xian2 = Control1 - Control0;
            Vector3 Normal = Vector3.Cross(xian1, xian2);
            float A1 = Normal.x;
            float B1 = Normal.y;
            float C1 = Normal.z;
            float D1 = -(Normal.x * Control0.x + Normal.y * Control0.y + Normal.z * Control0.z);//三点共面，平面参数为法向量
            float A2 = 2f * xian1.x;
            float B2 = 2f * xian1.y;
            float C2 = 2f * xian1.z;
            float D2 = Control0.x * Control0.x + Control0.y * Control0.y + Control0.z * Control0.z - Control1.x * Control1.x - Control1.y * Control1.y - Control1.z * Control1.z;//任意两点在同一大圆面上，大圆面法向量为弦向量
            float A3 = 2f * xian2.x;
            float B3 = 2f * xian2.y;
            float C3 = 2f * xian2.z;
            float D3 = Control0.x * Control0.x + Control0.y * Control0.y + Control0.z * Control0.z - Control2.x * Control2.x - Control2.y * Control2.y - Control2.z * Control2.z;//同上
            Matrix4x4 matrix = new Matrix4x4();//从（x，y，z）到（D1，D2，D3）变换矩阵，3*3的，但Unity只有4*4
            matrix.SetRow(0, new Vector4(A1, B1, C1, 0));
            matrix.SetRow(1, new Vector4(A2, B2, C2, 0));
            matrix.SetRow(2, new Vector4(A3, B3, C3, 0));
            matrix.SetRow(3, new Vector4(0, 0, 0, 1));
            Vector4 vector = new Vector4(D1, D2, D3, 0);
            Matrix4x4 inver = matrix.inverse;
            Vector4 O = inver.MultiplyVector(vector);
            Vector3 center = new Vector3(O.x, O.y, O.z);
            float radius = (Control2 - center).magnitude;
            Vector3 R0 = Control0 - center;
            Vector3 R1 = Control2 - center;
            float arc = Vector3.Angle(R0, R1);
            float acrper = (float)(arc / Count);
            for (int i = 0; i < Count - 1; i++)
            {
                Vector3 point = Quaternion.AngleAxis(acrper * (i + 1), __AITrafficWaypointRoute.transform.up) * (Control0*(radius/ R0.magnitude) - center* (radius / R0.magnitude));
                Vector3 _position = point + center;
                newWaypoints.Add(Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, __AITrafficWaypointRoute.transform) as GameObject);
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._transform = newWaypoints[i].transform;
                newPoint._waypoint = newWaypoints[i].GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.parentRoute = __AITrafficWaypointRoute;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                int insertIndex = __AITrafficWaypointRoute.waypointDataList.Count;
                newPoint._transform.SetSiblingIndex(insertIndex);
                newPoint._name = "AITrafficWaypoint " + (insertIndex + 1);
                newWaypoints[i].name = "AITrafficWaypoint " + (insertIndex + 1);
                __AITrafficWaypointRoute.waypointDataList.Add(newPoint);
                newPointtrans.Add(newPoint._transform);
                Undo.RegisterCreatedObjectUndo(newWaypoints[i], "AddCircle");
            }
            return newPointtrans.ToArray();
        }

        public Transform[] ExtendRoute(AITrafficWaypointRoute __AITrafficWaypointRoute, float Spacing, float Count)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("ExtendRoute");
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ExtendRoute");
            int insertIndex = 0;
            List<Transform> newPoints = new List<Transform>();
            List<GameObject> newWaypoints = new List<GameObject>();
            for (int i = 0; i < Count; i++)
            {
                Vector3 _position=__AITrafficWaypointRoute.waypointDataList[__AITrafficWaypointRoute.waypointDataList.Count - 1]._transform.position + __AITrafficWaypointRoute.waypointDataList[__AITrafficWaypointRoute.waypointDataList.Count - 1]._transform.forward * Spacing;
                newWaypoints.Add(Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, __AITrafficWaypointRoute.waypointDataList[__AITrafficWaypointRoute.waypointDataList.Count - 1]._transform.rotation, __AITrafficWaypointRoute.transform) as GameObject);
                CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
                newPoint._transform = newWaypoints[i].transform;
                newPoint._waypoint = newWaypoints[i].GetComponent<AITrafficWaypoint>();
                newPoint._waypoint.onReachWaypointSettings.parentRoute = __AITrafficWaypointRoute;
                newPoint._waypoint.onReachWaypointSettings.speedLimit = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.averagespeed = __AITrafficWaypointRoute.Avespeed;
                newPoint._waypoint.onReachWaypointSettings.sigma = 0;
                insertIndex = __AITrafficWaypointRoute.waypointDataList.Count;
                newPoint._transform.SetSiblingIndex(insertIndex);
                newPoint._name = "AITrafficWaypoint " + (insertIndex + 1);
                newWaypoints[i].name = "AITrafficWaypoint " + (insertIndex + 1);
                __AITrafficWaypointRoute.waypointDataList.Add(newPoint);
                Undo.RegisterCreatedObjectUndo(newWaypoints[i], "ExtendRoute");
            }
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ExtendRoute");
            Undo.CollapseUndoOperations(undoGroupIndex);
            return newPoints.ToArray();
        }

        [MenuItem("CONTEXT/AITrafficWaypointRoute/ClickToSpawnNextWaypoint" )]
        public static void ClickToSpawnNextWaypoint()
        {
            Debug.Log("!!");
        }

        public Transform ClickToSpawnNextWaypoint(AITrafficWaypointRoute __AITrafficWaypointRoute, Vector3 _position)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("ClickToSpawnNextWaypoint");
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ClickToSpawnNextWaypoint");
            GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, __AITrafficWaypointRoute.transform) as GameObject;
            CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
            newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (__AITrafficWaypointRoute.waypointDataList.Count + 1);
            newPoint._transform = newWaypoint.transform;
            newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
            newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = __AITrafficWaypointRoute.waypointDataList.Count + 1;
            newPoint._waypoint.onReachWaypointSettings.parentRoute = __AITrafficWaypointRoute;
            newPoint._waypoint.onReachWaypointSettings.speedLimit = __AITrafficWaypointRoute.Avespeed;
            newPoint._waypoint.onReachWaypointSettings.averagespeed = __AITrafficWaypointRoute.Avespeed;
            __AITrafficWaypointRoute.waypointDataList.Add(newPoint);
            Undo.RegisterCreatedObjectUndo(newWaypoint, "ClickToSpawnNextWaypoint");
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ClickToSpawnNextWaypoint");
            Undo.CollapseUndoOperations(undoGroupIndex);
            return newPoint._transform;
        }

        public void ClickToInsertSpawnNextWaypoint(AITrafficWaypointRoute __AITrafficWaypointRoute, Vector3 _position)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("ClickToInsertSpawnNextWaypoint");
            var undoGroupIndex = Undo.GetCurrentGroup();
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ClickToInsertSpawnNextWaypoint");
            List<Object> waypointObjectList = new List<Object>();
            for (int i = 0; i < __AITrafficWaypointRoute.waypointDataList.Count; i++)
            {
                waypointObjectList.Add(__AITrafficWaypointRoute.waypointDataList[i]._waypoint);
            }
            Object[] waypointObjectArray = waypointObjectList.ToArray();
            Undo.RegisterCompleteObjectUndo(waypointObjectArray, "ClickToSpawnNextWaypoint");
            bool isBetweenPoints = false;
            int insertIndex = 0;
            if (__AITrafficWaypointRoute.waypointDataList.Count >= 2)
            {
                for (int i = 0; i < __AITrafficWaypointRoute.waypointDataList.Count - 1; i++)
                {
                    Vector3 point_A = __AITrafficWaypointRoute.waypointDataList[i]._transform.position;
                    Vector3 point_B = __AITrafficWaypointRoute.waypointDataList[i + 1]._transform.position;
                    isBetweenPoints = IsCBetweenAB(point_A, point_B, _position);
                    insertIndex = i + 1;
                    if (isBetweenPoints) break;
                }
            }

            GameObject newWaypoint = Instantiate(STSRefs.AssetReferences._AITrafficWaypoint, _position, Quaternion.identity, __AITrafficWaypointRoute.transform) as GameObject;
            CarAIWaypointInfo newPoint = new CarAIWaypointInfo();
            newPoint._transform = newWaypoint.transform;
            newPoint._waypoint = newWaypoint.GetComponent<AITrafficWaypoint>();
            newPoint._waypoint.onReachWaypointSettings.parentRoute = __AITrafficWaypointRoute;
            newPoint._waypoint.onReachWaypointSettings.speedLimit = __AITrafficWaypointRoute.Avespeed;
            newPoint._waypoint.onReachWaypointSettings.averagespeed = __AITrafficWaypointRoute.Avespeed;
            if (isBetweenPoints)
            {
                newPoint._transform.SetSiblingIndex(insertIndex);
                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (insertIndex + 1);
                __AITrafficWaypointRoute.waypointDataList.Insert(insertIndex, newPoint);
                for (int i = 0; i < __AITrafficWaypointRoute.waypointDataList.Count; i++)
                {
                    int newIndexName = i + 1;
                    __AITrafficWaypointRoute.waypointDataList[i]._transform.gameObject.name = "AITrafficWaypoint " + newIndexName;
                    __AITrafficWaypointRoute.waypointDataList[i]._waypoint.onReachWaypointSettings.waypointIndexnumber = i + 1;
                }
            }
            else
            {
                newPoint._name = newWaypoint.name = "AITrafficWaypoint " + (__AITrafficWaypointRoute.waypointDataList.Count + 1);
                newPoint._waypoint.onReachWaypointSettings.waypointIndexnumber = __AITrafficWaypointRoute.waypointDataList.Count + 1;
                __AITrafficWaypointRoute.waypointDataList.Add(newPoint);
            }
            Undo.RegisterCreatedObjectUndo(newWaypoint, "ClickToInsertSpawnNextWaypoint");
            Undo.RegisterCompleteObjectUndo(__AITrafficWaypointRoute, "ClickToInsertSpawnNextWaypoint");
            Undo.CollapseUndoOperations(undoGroupIndex);
        }

        bool IsCBetweenAB(Vector3 A, Vector3 B, Vector3 C)
        {
            return (
                Vector3.Dot((B - A).normalized, (C - B).normalized) < 0f && Vector3.Dot((A - B).normalized, (C - A).normalized) < 0f &&
                Vector3.Distance(A, B) >= Vector3.Distance(A, C) &&
                Vector3.Distance(A, B) >= Vector3.Distance(B, C)
                );
        }

    }
}