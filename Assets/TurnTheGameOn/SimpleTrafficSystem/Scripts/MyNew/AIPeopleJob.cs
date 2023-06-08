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
        public NativeArray<float> speedNA;//当前速度
        public NativeArray<float> accelNA;//加速度
        public NativeArray<float> accelerationInputNA;//加速度输入
        public NativeArray<float3> routePointPositionNA;
        public NativeArray<int> currentRoutePointIndexNA;
        public NativeArray<float3> finalRoutePointPositionNA;
        public NativeArray<int> waypointDataListCountNA;

        //碰撞
        public NativeArray<bool> frontHitNA;//是否前方存在碰撞
        public NativeArray<float> frontSensorLengthNA;
        public NativeArray<float> frontHitDistanceNA;

        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
            #region move
            //if (isWalkingNA[index])
            //{
            //    targetSpeedNA[index] = topSpeedNA[index];//目标速度设为最大速度
            //    if (frontHitNA[index]) targetSpeedNA[index] = Mathf.InverseLerp(0, frontSensorLengthNA[index], frontHitDistanceNA[index]) * targetSpeedNA[index];//前方存在障碍

            //    accelNA[index] = targetSpeedNA[index] - speedNA[index];//加速度
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
