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


        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
            #region StopThreshold
            //����ȫ��ͣ���߼�
            if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1)
            {//�������·�Ľ�ͨ����Ҫͣ��&&�����н�&&Ŀǰ���ڵ�·����>=·�������е�·�ߵ������-1��Ӧ�þ��ǵ�����·��ĩ�ˣ�
                isWalkingNA[index] = false;
            }//����ͣ���˶�
            #endregion

            #region move
            //if (isWalkingNA[index])
            //{
            //    targetSpeedNA[index] = topSpeedNA[index];//Ŀ���ٶ���Ϊ����ٶ�
            //    if (frontHitNA[index]) targetSpeedNA[index] = Mathf.InverseLerp(0, frontSensorLengthNA[index], frontHitDistanceNA[index]) * targetSpeedNA[index];//ǰ�������ϰ�

            //    accelNA[index] = targetSpeedNA[index] - speedNA[index];//���ٶ�
            //    if (speedNA[index] >= topSpeedNA[index])
            //    {
            //        accelerationInputNA[index] = 0;
            //    }
            //    else
            //    {
            //        accelerationInputNA[index] = math.clamp(accelNA[index], 0, 1);
            //    }
            //}
            //else
            //{

            //}
            #endregion
        }
    }
}
