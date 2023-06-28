namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using UnityEditor;
    [CustomEditor(typeof(AIPeopleController))]
    public class Editor_AIPeopleController : Editor
    {
        private static int tab;
        public override void OnInspectorGUI()//重写后unity会在监视面板绘制自定义UI元素
        {
            EditorGUIUtility.wideMode = true;//宽模式布局

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((AIPeopleController)target), typeof(AITrafficController), false);
            GUI.enabled = true;

            AIPeopleController _AITrafficController = (AIPeopleController)target;

            EditorGUILayout.BeginVertical("Box");
            tab = GUILayout.Toolbar(tab, new string[] { "People", "Pooling" });
            EditorGUILayout.EndVertical();
            switch (tab)
            {
                case 0:
                    #region peopleSetting
                    EditorGUILayout.BeginVertical("Box");

                    #region People Prefabs
                    EditorGUILayout.BeginVertical("Box");

                    SerializedProperty peoplePrefabs = serializedObject.FindProperty("peoplePrefabs");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(peoplePrefabs, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.EndVertical();
                    #endregion People Prefabs


                    #region Detection Sensors
                    EditorGUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Detection Sensors", EditorStyles.miniLabel);

                    SerializedProperty layerMask = serializedObject.FindProperty("layerMask");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(layerMask, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty footLayerMask = serializedObject.FindProperty("footLayerMask");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(footLayerMask, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.EndVertical();
                    #endregion Detection Sensors


                    #region Lane Change
                    EditorGUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Lane Change", EditorStyles.miniLabel);

                    SerializedProperty useLaneChanging = serializedObject.FindProperty("useLaneChanging");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(useLaneChanging, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty changeLaneCooldown = serializedObject.FindProperty("changeLaneCooldown");
                    EditorGUI.BeginChangeCheck();
                    if(useLaneChanging.boolValue)//如果变道被勾选
                        EditorGUILayout.PropertyField(changeLaneCooldown, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.EndVertical();
                    #endregion


                    #region People Settings
                    EditorGUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Lane Change", EditorStyles.miniLabel);

                    SerializedProperty runningSpeed = serializedObject.FindProperty("runningSpeed");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(runningSpeed, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty walkingSpeedRange = serializedObject.FindProperty("walkingSpeedRange");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(walkingSpeedRange, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty fastestRidingSpeed = serializedObject.FindProperty("fastestRidingSpeed");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(fastestRidingSpeed, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty ridingSpeedRange = serializedObject.FindProperty("ridingSpeedRange");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(ridingSpeedRange, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty waitingTime = serializedObject.FindProperty("ridingSpeedRange");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(waitingTime, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.EndVertical();
                    #endregion People Settings

                    EditorGUILayout.EndVertical();
                    #endregion
                    break;
                case 1:
                    #region pooling
                    EditorGUILayout.BeginVertical("Box");

                    EditorGUILayout.HelpBox("非机动车是否pooling取决于机动车是否勾选了pooling，包括pooling zone，center point与机动车共用一份",MessageType.Info);

                    SerializedProperty density = serializedObject.FindProperty("density");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(density, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty peopleInPool = serializedObject.FindProperty("peopleInPool");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(peopleInPool, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty disabledPosition = serializedObject.FindProperty("disabledPosition");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(disabledPosition, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    SerializedProperty spawnRate = serializedObject.FindProperty("spawnRate");
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(spawnRate, true);
                    if (EditorGUI.EndChangeCheck())
                        serializedObject.ApplyModifiedProperties();

                    EditorGUILayout.EndVertical();
                    #endregion
                    break;
            }
            }

    }
}
