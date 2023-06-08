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
        public NativeArray<bool> isWalkingNA;
        public NativeArray<float> targetSpeedNA;
        public NativeArray<float> topSpeedNA;
        public NativeArray<float> speedNA;//��ǰ�ٶ�
        public NativeArray<float> accelNA;//���ٶ�
        public NativeArray<float> accelerationInputNA;//���ٶ�����
        public NativeArray<float3> routePointPositionNA;
        public NativeArray<int> currentRoutePointIndexNA;
        public NativeArray<float3> finalRoutePointPositionNA;
        public NativeArray<int> waypointDataListCountNA;

        //��ײ
        public NativeArray<bool> frontHitNA;//�Ƿ�ǰ��������ײ
        public NativeArray<float> frontSensorLengthNA;
        public NativeArray<float> frontHitDistanceNA;

        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
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
