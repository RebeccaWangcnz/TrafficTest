using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class AstarPoint 
{
    public Transform Transform;
    public float Dis;
    public float G;//��㵽�Ķ���ȷ�гɱ�
    public float H;//���㵽Ŀ���Ĺ��Ƴɱ�
    public float F;//��㵽Ŀ�����ܳɱ�
    public AstarPoint parent = null;
    public AstarPoint(Transform transform)
    {
        Transform = transform;
    }
    public void SetParent(AstarPoint parent, float g)
    {
        this.parent = parent;
        G = g;
        F = G + H;
    }
}
