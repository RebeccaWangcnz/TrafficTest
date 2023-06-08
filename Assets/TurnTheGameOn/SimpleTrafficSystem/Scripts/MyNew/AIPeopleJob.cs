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
        public NativeArray<bool> stopForTrafficLightNA;//是否需要根据信号灯停车
        public NativeArray<float> routeProgressNA;//道路进程
        public NativeArray<int> currentRoutePointIndexNA;//当前所在路线点的index
        public NativeArray<int> waypointDataListCountNA;//当前路线的所有点数
        public NativeArray<bool> isWalkingNA;
        public NativeArray<bool> isFrontHitNA;//前方是否有障碍
        public NativeArray<bool> isLastPointNA;//是否走到了尽头



        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
            #region StopThreshold
            //以下全是停车逻辑
            if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1)
            {//如果这条路的交通灯需要停车&&车有行进&&目前所在的路径点>=路线中所有的路线点的数量-1（应该就是到达了路线末端）
                isWalkingNA[index] = false;
            }//红灯立刻停下运动
            else if(isFrontHitNA[index])
            {
                isWalkingNA[index] = false;
            }//前方有障碍停止运动
            #endregion

            #region move
            //当行人处于停止状态时
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
