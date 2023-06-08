namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Burst;
    using UnityEngine.Jobs;

    [BurstCompile]
    public struct AIPeopleJob : IJobParallelForTransform
    {
        public NativeArray<bool> stopForTrafficLightNA;//�Ƿ���Ҫ�����źŵ�ͣ��
        public NativeArray<float> routeProgressNA;//��·����
        public NativeArray<int> currentRoutePointIndexNA;//��ǰ����·�ߵ��index
        public NativeArray<int> waypointDataListCountNA;//��ǰ·�ߵ����е���
        public NativeArray<bool> isWalkingNA;
        public NativeArray<bool> isFrontHitNA;//ǰ���Ƿ����ϰ�
        public NativeArray<bool> isLastPointNA;//�Ƿ��ߵ��˾�ͷ



        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
            #region StopThreshold
            //����ȫ��ͣ���߼�
            if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1)
            {//�������·�Ľ�ͨ����Ҫͣ��&&�����н�&&Ŀǰ���ڵ�·����>=·�������е�·�ߵ������-1��Ӧ�þ��ǵ�����·��ĩ�ˣ�
                isWalkingNA[index] = false;
            }//�������ͣ���˶�
            else if(isFrontHitNA[index])
            {
                isWalkingNA[index] = false;
            }//ǰ�����ϰ�ֹͣ�˶�
            #endregion

            #region move
            //�����˴���ֹͣ״̬ʱ
            if(!isWalkingNA[index])
            {
                if(!isFrontHitNA[index]&& !stopForTrafficLightNA[index]&& !isLastPointNA[index])
                {
                    isWalkingNA[index] = true;
                }
            }
            #endregion
        }
    }
}
