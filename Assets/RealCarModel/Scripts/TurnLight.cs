using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class TurnLight : MonoBehaviour
{
    //�����������ɵ���AICar�ű���
    private AITrafficCar AItrafficcar;
    private float flashtime = 0.4f;//��˸����ʱ��
    private float lighttime = 4f;//��Ƴ���ʱ��
    private float unflashtimetrigger = 0f;//��˸�����а��Ƽ�ʱ��
    private float flashtimetrigger = 0f;//��˸���������Ƽ�ʱ��
    private float lighttimetrigger = 0f;//��Ƽ�ʱ��
    private float checktimetriggerL = 0f;//��Ƽ�ʱ��
    private float checktimetriggerR = 0f;//�ҵƼ�ʱ��
    [HideInInspector] public int isturning = 0;//����ʶ�������-1Ϊ��0Ϊ��ת��1Ϊ�ң�
    private string emissionColorName;//ʹ���ʷ���Ŀ��ƹؼ���
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
        }//�������
        if (isturning==-1)//��ת�ź�
        {
            if(lighttimetrigger<=lighttime)//�źŵƼ�ʱ��С����ֵ
            {
                if(flashtimetrigger<=flashtime&& unflashtimetrigger<= flashtime)//���Ƽ�ʱ��С����ֵ
                {
                    AItrafficcar.leftturnlight.materials[0].SetColor(emissionColorName, lightcolor);
                    AItrafficcar.leftturnlight.materials[0].color = baselightcolor;
                    checktimetriggerL += Time.deltaTime;
                    flashtimetrigger += Time.deltaTime;
                }
                if(flashtimetrigger>flashtime&& unflashtimetrigger <= flashtime)//���Ƽ�ʱ��������ֵ
                {
                    AItrafficcar.leftturnlight.materials[0].SetColor(emissionColorName, darkcolor);
                    AItrafficcar.leftturnlight.materials[0].color = basedarkcolor;
                    unflashtimetrigger += Time.deltaTime;
                }
                else if(unflashtimetrigger> flashtime)//�����ʱ��������ֵ���������Ƽ�ʱ��
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
