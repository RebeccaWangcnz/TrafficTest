using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransPortationDoor : MonoBehaviour
{
    public GameObject Pos;
    public GameObject TargetPos;
    private void OnTriggerEnter(Collider other)
    {
        // 将角色的世界位置和朝向赋予门记录目标位置的空物体
        Pos.transform.position = other.transform.position;
        Pos.transform.rotation = other.transform.rotation;

        // 使目标门用于记录角色位置的空物体相对于目标门的相对位置与源门的相同
        TargetPos.transform.localPosition = Pos.transform.localPosition;
        TargetPos.transform.localRotation = Pos.transform.localRotation;

        // 将角色传送过去
        other.transform.position = TargetPos.transform.position;
        other.transform.rotation = TargetPos.transform.rotation;
    }
}
