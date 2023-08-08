namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Burst;
    using UnityEngine.Jobs;
    /// <summary>
    /// һЩ����ʱ������Ϊ�ж�
    /// </summary>
    [BurstCompile]
    public struct AIPeopleJob : IJobParallelForTransform
    {
        public NativeArray<bool> stopForTrafficLightNA;//�Ƿ���Ҫ�����źŵ�ͣ��
        public NativeArray<float> routeProgressNA;//��·����
        public NativeArray<int> currentRoutePointIndexNA;//��ǰ����·�ߵ��index
        public NativeArray<int> waypointDataListCountNA;//��ǰ·�ߵ����е���
        public NativeArray<bool> isWalkingNA;
        public NativeArray<bool> isFrontHitNA;//ǰ���Ƿ����ϰ�
        public NativeArray<bool> isLefttHitNA;//ǰ���Ƿ����ϰ�
        public NativeArray<bool> isRightHitNA;//ǰ���Ƿ����ϰ�
        public NativeArray<bool> isLastPointNA;//�Ƿ��ߵ��˾�ͷ
        public NativeArray<bool> isFootHitNA;//���Ƿ�ײ����̨��
        public NativeArray<Quaternion> targetRotationNA;
        public NativeArray<bool> needChangeLanesNA;
        public NativeArray<bool> stopForHornNA;
        public NativeArray<int> runDirectionNA;
        public NativeArray<bool> crossRoadNA;
        public bool useLaneChanging;
        [ReadOnly]public float deltaTime;//��ȡ����Time.deltaTime


        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
            #region rotation
            //driveTargetTransformAccessArray.rotation= Quaternion.Lerp(driveTargetTransformAccessArray.rotation, targetRotationNA[index], 5f * deltaTime);//5f��ת��
            #endregion
            #region StopThreshold
            //����ȫ��ͣ���߼�
            //Debug.Log(routeProgressNA[index] > 0);
           // Debug.Log(currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1);
            if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1)
            {//�������·�Ľ�ͨ����Ҫͣ��&&�����н�&&Ŀǰ���ڵ�·����>=·�������е�·�ߵ������-1��Ӧ�þ��ǵ�����·��ĩ�ˣ�
                isWalkingNA[index] = false;
            }//�������ͣ���˶�
            else if(isFrontHitNA[index])//ǰ�������ϰ�
            {
                if(!useLaneChanging)//�ǻ���ͣ��
                {
                    isWalkingNA[index] = false;
                }
                else//���Ի����򻻵�
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
                {//ǰ��û���ϰ�&&���Ǻ��&&�������һ����&&û���ܵ�����
                    isWalkingNA[index] = true;//����ǰ��
                    needChangeLanesNA[index] = false;
                }
            }
            if(isFootHitNA[index])//��ǿ�ƹ���·�����������̨�ף�����ɹ���·����������ر�ǿ�ƹ���·
            {
                if (crossRoadNA[index])
                    crossRoadNA[index] = false;
            }
            #endregion
        }
    }
}
