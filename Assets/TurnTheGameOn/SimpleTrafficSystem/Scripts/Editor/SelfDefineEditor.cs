using UnityEngine;
using UnityEditor;
using TurnTheGameOn.SimpleTrafficSystem;

[CustomEditor(typeof(AITrafficWaypoint))]//声明要处理的组件类型
public class SelfDefineEditor:Editor
{
    AITrafficWaypoint point;
    AITrafficWaypoint[] nextPoints;
    //private void OnEnable()
    //{
    //    //包含该组件的物体被选中的时候调用
    //    point = (AITrafficWaypoint)target;
    //    nextPoints = point.onReachWaypointSettings.newRoutePoints;
    //}

     void OnSceneGUI()//只有当物体选中的时候每帧会调用
    {
        point = (AITrafficWaypoint)target;
        nextPoints = point.onReachWaypointSettings.newRoutePoints;
        if (nextPoints.Length!=0)
        {
            foreach (var nextPoint in nextPoints)
            {
                Handles.color = Color.blue;
                Handles.DrawLine(point.transform.position, nextPoint.transform.position, 3f);
            }
        }

    }
}
