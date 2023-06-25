using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class AstarPoint 
{
    public Transform Transform;
    public float Dis;
    public float G;//起点到的顶点确切成本
    public float H;//顶点到目标点的估计成本
    public float F;//起点到目标点的总成本
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
