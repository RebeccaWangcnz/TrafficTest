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
        public NativeArray<bool> isFootHitNA;//脚是否撞到了台阶
        public NativeArray<Quaternion> targetRotationNA;
        public NativeArray<bool> needChangeLanesNA;
        public NativeArray<bool> stopForHornNA;
        public NativeArray<int> runDirectionNA;
        public NativeArray<bool> crossRoadNA;
        public bool useLaneChanging;
        [ReadOnly]public float deltaTime;


        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
            #region rotation
            driveTargetTransformAccessArray.rotation= Quaternion.Lerp(driveTargetTransformAccessArray.rotation, targetRotationNA[index], 5f * deltaTime);//5f是转速
            #endregion
            #region StopThreshold
            //以下全是停车逻辑
            //Debug.Log(routeProgressNA[index] > 0);
           // Debug.Log(currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1);
            if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1)
            {//如果这条路的交通灯需要停车&&车有行进&&目前所在的路径点>=路线中所有的路线点的数量-1（应该就是到达了路线末端）
                isWalkingNA[index] = false;
            }//红灯立刻停下运动
            else if(isFrontHitNA[index])
            {
                if(!useLaneChanging)
                {
                    isWalkingNA[index] = false;
                }
                else
                {
                    needChangeLanesNA[index] = true;
                }
            }//前方有障碍停止运动,或者需要变道
            else if (!isLastPointNA[index] && !stopForHornNA[index])
            {
                isWalkingNA[index] = true;
                needChangeLanesNA[index] = false;
            }//既不是最后一个点也没有听到汽车鸣笛就继续行走
            #endregion

            #region move
            //当行人处于停止状态时
            if(!isWalkingNA[index])
            {
                if (!isFrontHitNA[index]&& !stopForTrafficLightNA[index]&& !isLastPointNA[index]&&!stopForHornNA[index])
                {
                    isWalkingNA[index] = true;
                    needChangeLanesNA[index] = false;
                }
            }
            if(isFootHitNA[index])
            {
                driveTargetTransformAccessArray.position+= new Vector3(0, 0.3f *deltaTime, 0);//行人微微上移
                if (crossRoadNA[index])
                    crossRoadNA[index] = false;
            }
            #endregion
        }
    }
}
