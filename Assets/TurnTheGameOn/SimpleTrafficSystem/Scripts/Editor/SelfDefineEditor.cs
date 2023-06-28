using UnityEngine;
using UnityEditor;
using TurnTheGameOn.SimpleTrafficSystem;

[CustomEditor(typeof(AITrafficWaypoint))]//����Ҫ������������
public class SelfDefineEditor:Editor
{
    AITrafficWaypoint point;
    AITrafficWaypoint[] nextPoints;
    //private void OnEnable()
    //{
    //    //��������������屻ѡ�е�ʱ�����
    //    point = (AITrafficWaypoint)target;
    //    nextPoints = point.onReachWaypointSettings.newRoutePoints;
    //}

     void OnSceneGUI()//ֻ�е�����ѡ�е�ʱ��ÿ֡�����
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
