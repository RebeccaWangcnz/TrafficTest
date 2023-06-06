using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransPortationDoor : MonoBehaviour
{
    public GameObject Pos;
    public GameObject TargetPos;
    private void OnTriggerEnter(Collider other)
    {
        // ����ɫ������λ�úͳ������ż�¼Ŀ��λ�õĿ�����
        Pos.transform.position = other.transform.position;
        Pos.transform.rotation = other.transform.rotation;

        // ʹĿ�������ڼ�¼��ɫλ�õĿ����������Ŀ���ŵ����λ����Դ�ŵ���ͬ
        TargetPos.transform.localPosition = Pos.transform.localPosition;
        TargetPos.transform.localRotation = Pos.transform.localRotation;

        // ����ɫ���͹�ȥ
        other.transform.position = TargetPos.transform.position;
        other.transform.rotation = TargetPos.transform.rotation;
    }
}
