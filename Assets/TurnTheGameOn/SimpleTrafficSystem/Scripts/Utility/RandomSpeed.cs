namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using System.Collections;
    public class RandomSpeed : MonoBehaviour
    {
        public float average = 80f;
        public float sigma = 5f;
        void OnEnable()
        {
            GetComponent<AITrafficCar>().topSpeed = ChooseFromNormal();
        }
        float Normal(float X,float Average,float Sigma)
        {
            return 1.0f / (Mathf.Sqrt(2f * Mathf.PI) * Sigma) * Mathf.Exp(-1f * (X - Average) * (X - Average) / (2f * Sigma * Sigma));
        }//正态密度函数
        float ChooseFromNormal()
        {
            float checkNum;
            float x;
            float n;
            float range = sigma * 3f;//剔除了3σ外的波动
            do
            {
                x = UnityEngine.Random.Range(average - range, average + range);//在范围内取随机数
                n = Normal(x, average, sigma);//所获得随机数的正态密度函数
                checkNum = UnityEngine.Random.Range(0, Normal(average, average, sigma));//获得该正态分布的最大单位密度,在该区间内抽样
            } while (checkNum > n);//当抽样结果满足概率检验时，返回随机数
            return x;
        }
    }
}