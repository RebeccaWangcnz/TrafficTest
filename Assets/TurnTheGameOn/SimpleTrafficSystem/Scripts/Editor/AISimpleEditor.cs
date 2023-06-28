using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using TurnTheGameOn.SimpleTrafficSystem;
/// <summary>
/// Rebe0627：一个简单的没有用的功能
/// </summary>
public class AISimpleEditor : EditorWindow
{
    private MonoScript targetScript;
    private string targetVariableName;
    private string newValue;
    private bool showDebug;

    [MenuItem("AI便捷操作/针对脚本查找引用与一键赋值")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AISimpleEditor));
    }
    private void OnGUI()
    {
        GUILayout.Label("Assign Component Variables", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("需要先选择你需要更改的物体范围，可以手动选择物体范围，该范围需要包含你需要更改的物体，" +
        "也可以点击Find all Gameobjects with Target Script，该按钮会自动帮你选出场景中含有该脚本的物体", MessageType.None);

        targetScript = EditorGUILayout.ObjectField("Target Script", targetScript, typeof(MonoScript), false) as MonoScript;
        targetVariableName = EditorGUILayout.TextField("Target Variable Name", targetVariableName);
        newValue = EditorGUILayout.TextField("New Value", newValue);
        GUIContent toggleLabel = new GUIContent("Show Debug", "勾选后在使用Find all Gameobjects with Target Script时，会在控制台打印出来所有包含该脚本的物体名称，方便快速定位");
        showDebug = EditorGUILayout.Toggle(toggleLabel, showDebug);

        if (GUILayout.Button("Find all Gameobjects with Target Script"))
        {
            if (targetScript != null)
            {
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                List<GameObject> objWithScript =new List<GameObject>();
                foreach (var obj in allObjects)
                {
                    if(obj.GetComponent(targetScript.GetClass()))
                    {
                        if(showDebug)
                            Debug.Log(obj.name,obj);
                        objWithScript.Add(obj);
                    }
                }
                Selection.objects = objWithScript.ToArray();
            }
            else
            {
                Debug.LogWarning("Target Script is not assigned.");
            }
        }

        if (GUILayout.Button("Assign Variables"))
        {
            if (targetScript != null)
            {
                //AssignComponentVariables<AITrafficWaypoint>(targetScript, targetVariableName, newValue);
                // 创建泛型方法的MethodInfo对象
                System.Reflection.MethodInfo methodInfo = this.GetType().GetMethod("AssignComponentVariables", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(targetScript.GetClass());
                methodInfo.Invoke(this,null);
            }
            else
            {
                Debug.LogWarning("Target Script is not assigned.");
            }
        }
    }
    private void AssignComponentVariables<T>() where T:Object
    {
        var foundObjs =Selection.gameObjects.Select(go=>go.GetComponent<T>()).ToArray() ;
        foundObjs = foundObjs.Where(go => go != null).ToArray();
        if(foundObjs.Length!=0)
        {
            Debug.Log(foundObjs.Length);
            foreach (var obj in foundObjs)
            {
                SerializedObject serializedObject = new SerializedObject(obj);
                SerializedProperty property = serializedObject.FindProperty(targetVariableName);
                if (property != null)
                {
                    switch (property.propertyType)
                    {
                        case SerializedPropertyType.Integer:
                            property.intValue = int.Parse(newValue);
                            break;
                        case SerializedPropertyType.Float:
                            property.floatValue = float.Parse(newValue);
                            break;
                        case SerializedPropertyType.String:
                            property.stringValue = newValue;
                            break;
                        case SerializedPropertyType.Boolean:
                            property.boolValue = bool.Parse(newValue);
                            break;
                        // Add more cases for other property types as needed

                        default:
                            Debug.LogWarning("Unsupported property type: " + property.propertyType);
                            break;
                    }

                    serializedObject.ApplyModifiedProperties();
                    Debug.Log("Variable " + targetVariableName + " assigned to " + newValue + " for component " +obj.name);
                }
                else
                {
                    Debug.LogWarning( "Property Not Found ");
                }

            }
        }
        else
        {
            Debug.LogWarning("Object " +" not found in component ");
        }
    }
}
