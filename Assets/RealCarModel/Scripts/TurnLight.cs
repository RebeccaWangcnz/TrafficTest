using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class TurnLight : MonoBehaviour
{
    //公共变量集成到了AICar脚本里
    private AITrafficCar AItrafficcar;
    private float flashtime = 0.4f;//闪烁持续时间
    private float lighttime = 4f;//打灯持续时间
    private float unflashtimetrigger = 0f;//闪烁过程中暗灯计时器
    private float flashtimetrigger = 0f;//闪烁过程中亮灯计时器
    private float lighttimetrigger = 0f;//打灯计时器
    private float checktimetriggerL = 0f;//左灯计时器
    private float checktimetriggerR = 0f;//右灯计时器
    [HideInInspector] public int isturning = 0;//方向识别变量（-1为左，0为不转向，1为右）
    private string emissionColorName;//使材质发光的控制关键字
    private Color lightcolor;
    private Color darkcolor;
    private Color baselightcolor;
    private Color basedarkcolor;
    void Awake()
    {
        AItrafficcar = GetComponent<AITrafficCar>();
    }
    void Start()
    {
        baselightcolor = new Color(255, 100, 0, 255);
        basedarkcolor = new Color(0, 0, 0, 0);
        lightcolor = new Color(255, 100, 0);
        darkcolor = new Color(0, 0, 0);
        emissionColorName = RenderPipeline.IsDefaultRP || RenderPipeline.IsURP ? "_EmissionColor" : "_EmissiveColor";
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (checktimetriggerL>0)
        {
            checktimetriggerR = 0;
        }
        if (checktimetriggerR>0)
        {
            checktimetriggerL = 0;
        }
        if (checktimetriggerL> flashtime*2)
        {
            AItrafficcar.leftturnlight.materials[0].SetColor(emissionColorName, darkcolor);
            AItrafficcar.leftturnlight.materials[0].color = basedarkcolor;
        }
        if (checktimetriggerR > flashtime * 2)
        {
            AItrafficcar.rightturnlight.materials[0].SetColor(emissionColorName, darkcolor);
            AItrafficcar.leftturnlight.materials[0].color = basedarkcolor;
        }//常亮检查
        if (isturning==-1)//左转信号
        {
            if(lighttimetrigger<=lighttime)//信号灯计时器小于阈值
            {
                if(flashtimetrigger<=flashtime&& unflashtimetrigger<= flashtime)//闪灯计时器小于阈值
                {
                    AItrafficcar.leftturnlight.materials[0].SetColor(emissionColorName, lightcolor);
                    AItrafficcar.leftturnlight.materials[0].color = baselightcolor;
                    checktimetriggerL += Time.deltaTime;
                    flashtimetrigger += Time.deltaTime;
                }
                if(flashtimetrigger>flashtime&& unflashtimetrigger <= flashtime)//闪灯计时器大于阈值
                {
                    AItrafficcar.leftturnlight.materials[0].SetColor(emissionColorName, darkcolor);
                    AItrafficcar.leftturnlight.materials[0].color = basedarkcolor;
                    unflashtimetrigger += Time.deltaTime;
                }
                else if(unflashtimetrigger> flashtime)//闪灭计时器大于阈值，重置闪灯计时器
                {
                    flashtimetrigger = 0;
                    unflashtimetrigger = 0;
                }
                lighttimetrigger += Time.deltaTime;
            }
            else 
            {
                isturning = 0;
                AItrafficcar.leftturnlight.materials[0].SetColor(emissionColorName, darkcolor);
                AItrafficcar.leftturnlight.materials[0].color = basedarkcolor;
                lighttimetrigger = 0;
            }
        }
        if (isturning==1)
        {
            if (lighttimetrigger <= lighttime)
            {
                if (flashtimetrigger <= flashtime && unflashtimetrigger <= flashtime)
                {
                    AItrafficcar.rightturnlight.materials[0].SetColor(emissionColorName, lightcolor);
                    //AItrafficcar.rightturnlight.materials[0].color = baselightcolor;
                    checktimetriggerR += Time.deltaTime;
                    flashtimetrigger += Time.deltaTime;
                }
                if (flashtimetrigger > flashtime && unflashtimetrigger <= flashtime)
                {
                    AItrafficcar.rightturnlight.materials[0].SetColor(emissionColorName, darkcolor);
                    //AItrafficcar.leftturnlight.materials[0].color = basedarkcolor;
                    unflashtimetrigger += Time.deltaTime;
                }
                else if (unflashtimetrigger > flashtime)
                {
                    flashtimetrigger = 0;
                    unflashtimetrigger = 0;
                }
                lighttimetrigger += Time.deltaTime;
            }
            else
            {
                isturning = 0;
                AItrafficcar.rightturnlight.materials[0].SetColor(emissionColorName, darkcolor);
                AItrafficcar.leftturnlight.materials[0].color = basedarkcolor;
                lighttimetrigger = 0;
            }
        }
    }
}
