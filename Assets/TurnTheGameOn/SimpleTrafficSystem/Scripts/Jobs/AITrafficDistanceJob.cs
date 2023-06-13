namespace TurnTheGameOn.SimpleTrafficSystem
{
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Burst;
    using UnityEngine.Jobs;

    [BurstCompile]
    public struct AITrafficDistanceJob : IJobParallelForTransform
    {
        public float cullDistance;
        public float actizeZone;
        public float spawnZone;
        public float3 playerPosition;//centerposition
        public NativeArray<bool> canProcessNA;
        public NativeArray<bool> withinLimitNA;
        public NativeArray<bool> isVisibleNA;
        public NativeArray<bool> lightIsActiveNA;
        public NativeArray<bool> outOfBoundsNA;
        public NativeArray<bool> isDisabledNA;
        public NativeArray<float> distanceToPlayerNA;

        public void Execute(int index, TransformAccess carTransformAccessArray)
        {
            if (canProcessNA[index])
            {
                if (isDisabledNA[index] == false)
                {
                    distanceToPlayerNA[index] = math.distance(carTransformAccessArray.position, playerPosition);
                    withinLimitNA[index] = distanceToPlayerNA[index] < cullDistance;//是否在开灯范围内
                    outOfBoundsNA[index] = distanceToPlayerNA[index] > spawnZone;//是否在生成范围外

                    if (isVisibleNA[index] || withinLimitNA[index])
                    {
                        lightIsActiveNA[index] = true;
                    }
                    else
                    {
                        lightIsActiveNA[index] = false;
                    }
                }
            }
        }
    }
}