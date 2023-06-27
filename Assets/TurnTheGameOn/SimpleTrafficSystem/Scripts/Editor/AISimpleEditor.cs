using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using TurnTheGameOn.SimpleTrafficSystem;
/// <summary>
/// Rebe0627��һ���򵥵�û���õĹ���
/// </summary>
public class AISimpleEditor : EditorWindow
{
    private MonoScript targetScript;
    private string targetVariableName;
    private string newValue;

    [MenuItem("AI��ݲ���/һ����ֵ")]
    private static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(AISimpleEditor));
    }
    private void OnGUI()
    {
        GUILayout.Label("Assign Component Variables", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("��Ҫ��ѡ������Ҫ���ĵ����巶Χ��һ�������ѡ����������е�gameobject����", MessageType.None);

        targetScript = EditorGUILayout.ObjectField("Target Script", targetScript, typeof(MonoScript), false) as MonoScript;
        targetVariableName = EditorGUILayout.TextField("Target Variable Name", targetVariableName);
        newValue = EditorGUILayout.TextField("New Value", newValue);

        if (GUILayout.Button("Assign Variables"))
        {
            if (targetScript != null)
            {
                //AssignComponentVariables<AITrafficWaypoint>(targetScript, targetVariableName, newValue);
                // �������ͷ�����MethodInfo����
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
