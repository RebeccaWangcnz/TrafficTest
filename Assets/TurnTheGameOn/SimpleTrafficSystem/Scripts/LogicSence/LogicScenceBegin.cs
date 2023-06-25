using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;
using UnityEngine.UI;

public class LogicScenceBegin : MonoBehaviour
{
    public AITrafficController Instance;
    public AITrafficWaypoint[] spawnpoints;
    public Text scencetext;
    public AudioSource alarm;
    private AITrafficCar[] spawncars = new AITrafficCar[10];
    private Vector3 spawnPosition;
    private float textshowtime = 10f;
    private float texttimer = 0f;
    private bool texttrigger = false;
    void Start()
    {
        scencetext.enabled = false;
        alarm.enabled = false;
        scencetext.color = new Color32(0, 0, 0, 0);//һ�����Ǹ����ı��ģ�����Բ��ã��б�������Ӱ�����У�
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.transform.gameObject.tag == "DriverCar")//����ʻ�˵ĳ����ǩ��DrivingCar��
        {
            //����ʻ��ʻ�봥����������ض������ɻ�������
            for (int i = 0; i < spawnpoints.Length; i++)
            {
                spawncars[i] = Instance.GetCarFromPool(spawnpoints[i].onReachWaypointSettings.parentRoute);
                if (spawncars[i] != null)
                {
                    spawnpoints[i].onReachWaypointSettings.parentRoute.currentDensity += 1;
                    spawnPosition = spawnpoints[i].transform.position;
                    spawncars[i].transform.SetPositionAndRotation(
                        spawnPosition,
                        spawnpoints[i].transform.rotation
                        );
                    spawncars[i].transform.LookAt(spawnpoints[i].onReachWaypointSettings.parentRoute.waypointDataList[spawnpoints[i].onReachWaypointSettings.waypointIndexnumber]._transform);
                }
            }
            //����ʻ��ʻ�봥�����󣬸����ı���ʾ����
            scencetext.enabled = true;
            texttrigger = true;
            //����ʻ��ʻ�봥�����󣬳�������������ʾ
            alarm.enabled = true;
        }
    }
    void FixedUpdate()//�����ı�����
    {
        if(texttrigger)//����������
        {
            int a = Mathf.RoundToInt(scencetext.color.a * 255.0f);//��ɫ��aֵ����͸���ȣ��Ի�
            texttimer += Time.deltaTime;
            if(texttimer< textshowtime&& a<255)
            {
                scencetext.color += new Color32(0, 0, 0, 2);//ÿ֡+8��λaֵ
            }
            if(texttimer > textshowtime)
            {
                scencetext.color -= new Color32(0, 0, 0, 2);//ÿ֡+8��λaֵ
            }
            else if (texttimer > textshowtime&&a<=0)
            {
                scencetext.enabled = false;
            }
        }
    }
}
