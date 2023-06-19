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
        public NativeArray<bool> isFootHitNA;//���Ƿ�ײ����̨��
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
            driveTargetTransformAccessArray.rotation= Quaternion.Lerp(driveTargetTransformAccessArray.rotation, targetRotationNA[index], 5f * deltaTime);//5f��ת��
            #endregion
            #region StopThreshold
            //����ȫ��ͣ���߼�
            //Debug.Log(routeProgressNA[index] > 0);
           // Debug.Log(currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1);
            if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1)
            {//�������·�Ľ�ͨ����Ҫͣ��&&�����н�&&Ŀǰ���ڵ�·����>=·�������е�·�ߵ������-1��Ӧ�þ��ǵ�����·��ĩ�ˣ�
                isWalkingNA[index] = false;
            }//�������ͣ���˶�
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
            }//ǰ�����ϰ�ֹͣ�˶�,������Ҫ���
            else if (!isLastPointNA[index] && !stopForHornNA[index])
            {
                isWalkingNA[index] = true;
                needChangeLanesNA[index] = false;
            }//�Ȳ������һ����Ҳû�������������Ѿͼ�������
            #endregion

            #region move
            //�����˴���ֹͣ״̬ʱ
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
                driveTargetTransformAccessArray.position+= new Vector3(0, 0.3f *deltaTime, 0);//����΢΢����
                if (crossRoadNA[index])
                    crossRoadNA[index] = false;
            }
            #endregion
        }
    }
}
