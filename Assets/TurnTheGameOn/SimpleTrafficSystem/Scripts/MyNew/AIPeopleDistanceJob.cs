namespace TurnTheGameOn.SimpleTrafficSystem
{
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Burst;
    using UnityEngine.Jobs;
    [BurstCompile]
    public struct AIPeopleDistanceJob : IJobParallelForTransform
    {
        public float3 playerPosition;//centerposition
        public float spawnZone;
        public NativeArray<bool> isDisabledNA;
        public NativeArray<bool> outOfBoundsNA;
        public NativeArray<float> distanceToPlayerNA;
        public void Execute(int index, TransformAccess carTransformAccessArray)
        {
            if (isDisabledNA[index] == false)
            {
                distanceToPlayerNA[index] = math.distance(carTransformAccessArray.position, playerPosition);
                outOfBoundsNA[index] = distanceToPlayerNA[index] > spawnZone;//是否在生成范围外
            }
        }
    }
}
