namespace TurnTheGameOn.SimpleTrafficSystem
{
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Burst;
    using UnityEngine.Jobs;
    using UnityEngine;

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
                distanceToPlayerNA[index] = Vector2.Distance(new Vector2(carTransformAccessArray.position.x, carTransformAccessArray.position.z), new Vector2(playerPosition.x,playerPosition.z));
                outOfBoundsNA[index] = distanceToPlayerNA[index] > spawnZone;//是否在生成范围外
            }
        }
    }
}
