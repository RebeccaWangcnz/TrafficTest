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
        scencetext.color = new Color32(0, 0, 0, 0);//一部分是浮现文本的（你可以不用，有报错但不会影响运行）
    }
    void OnTriggerEnter(Collider other)
    {
        if (other.transform.gameObject.tag == "DriverCar")//给驾驶人的车打标签“DrivingCar”
        {
            //当驾驶车驶入触发区域后，在特定点生成环境车辆
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
            //当驾驶车驶入触发区后，浮现文本提示场景
            scencetext.enabled = true;
            texttrigger = true;
            //当驾驶车驶入触发区后，出现语音导航提示
            alarm.enabled = true;
        }
    }
    void FixedUpdate()//浮现文本场景
    {
        if(texttrigger)//触发器触发
        {
            int a = Mathf.RoundToInt(scencetext.color.a * 255.0f);//颜色的a值（不透明度）显化
            texttimer += Time.deltaTime;
            if(texttimer< textshowtime&& a<255)
            {
                scencetext.color += new Color32(0, 0, 0, 2);//每帧+8单位a值
            }
            if(texttimer > textshowtime)
            {
                scencetext.color -= new Color32(0, 0, 0, 2);//每帧+8单位a值
            }
            else if (texttimer > textshowtime&&a<=0)
            {
                scencetext.enabled = false;
            }
        }
    }
}
