namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using Unity.Mathematics;
    using Unity.Collections;
    using Unity.Burst;
    using UnityEngine.Jobs;

    [BurstCompile]
    public struct AITrafficCarJob : IJobParallelForTransform
    {
        public float deltaTime;
        public float maxSteerAngle;
        public float speedMultiplier;
        public float steerSensitivity;
        public float stopThreshold;
        public NativeArray<float> frontSensorLengthNA;
        public NativeArray<int> currentRoutePointIndexNA;
        public NativeArray<int> waypointDataListCountNA;
        public NativeArray<bool> isDrivingNA;
        public NativeArray<bool> isActiveNA;
        public NativeArray<bool> canProcessNA;
        public NativeArray<bool> overrideInputNA;
        public NativeArray<bool> isBrakingNA;
        public NativeArray<bool> frontHitNA;
        public NativeArray<bool> stopForTrafficLightNA;
        public NativeArray<bool> yieldForCrossTrafficNA;
        public NativeArray<float> accelerationPowerNA;
        public NativeArray<float> brakePowerNA;
        public NativeArray<float> accelerationInputNA;
        public NativeArray<float> speedNA;
        public NativeArray<float> topSpeedNA;
        public NativeArray<float> routeProgressNA;
        public NativeArray<float> targetSpeedNA;
        public NativeArray<float> speedLimitNA;
        public NativeArray<float> averagespeedNA;
        public NativeArray<float> sigmaNA;
        public NativeArray<float> accelNA;
        public NativeArray<float> targetAngleNA;
        public NativeArray<float> steerAngleNA;
        public NativeArray<float> motorTorqueNA;
        public NativeArray<float> brakeTorqueNA;
        public NativeArray<float> moveHandBrakeNA;
        public NativeArray<float> overrideAccelerationPowerNA;
        public NativeArray<float> overrideBrakePowerNA;
        public NativeArray<float> frontHitDistanceNA;
        public NativeArray<float> distanceToEndPointNA;
        public NativeArray<float3> finalRoutePointPositionNA;
        public NativeArray<float3> routePointPositionNA;
        public NativeArray<Vector3> carTransformPreviousPositionNA;
        public NativeArray<Vector3> carTransformPositionNA;
        public NativeArray<Vector3> localTargetNA;

        public NativeArray<Vector3> frontSensorTransformPositionNA;

        public void Execute(int index, TransformAccess driveTargetTransformAccessArray)
        {
            if (isActiveNA[index] && canProcessNA[index])
            {
                driveTargetTransformAccessArray.position = routePointPositionNA[index];

                #region StopThreshold
                if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 1)
                {
                    //distanceToEndPointNA[index] = Vector3.Distance(frontSensorTransformPositionNA[index], routePointPositionNA[index]);
                    distanceToEndPointNA[index] = Vector3.Distance(frontSensorTransformPositionNA[index], finalRoutePointPositionNA[index]);
                    //if (overrideInputNA[index])
                    //{
                    overrideInputNA[index] = true;
                    overrideBrakePowerNA[index] = 1f;
                    overrideAccelerationPowerNA[index] = 0f;
                    //}//到终点了刹车
                }
                else if (stopForTrafficLightNA[index] && routeProgressNA[index] > 0 && currentRoutePointIndexNA[index] >= waypointDataListCountNA[index] - 2 && !frontHitNA[index])
                {
                    //distanceToEndPointNA[index] = Vector3.Distance(frontSensorTransformPositionNA[index], routePointPositionNA[index]);
                    distanceToEndPointNA[index] = Vector3.Distance(frontSensorTransformPositionNA[index], finalRoutePointPositionNA[index]);
                    //if (overrideInputNA[index])
                    //{
                    overrideInputNA[index] = true;
                    overrideBrakePowerNA[index] = distanceToEndPointNA[index] < 3 || speedNA[index] > 10 ? 1f : 0f;
                    overrideAccelerationPowerNA[index] = distanceToEndPointNA[index] < 3 || speedNA[index] > 10 ? 0f : 0.3f;
                    //}
                }//快到终点了减速
                else if (frontHitNA[index])
                {
                    overrideInputNA[index] = true;
                    overrideBrakePowerNA[index] = 0f;
                    overrideAccelerationPowerNA[index] = 0f;
                }//前碰
                else if (yieldForCrossTrafficNA[index])
                {
                    if (!overrideInputNA[index])
                    {
                        overrideInputNA[index] = true;
                        overrideBrakePowerNA[index] = 1f;
                        overrideAccelerationPowerNA[index] = 0f;
                    }
                }
                else if(overrideInputNA[index])//盒体投射检测有问题，所以老是会跳到这一步，导致刹车部分失效（有问题是因为盒体碰撞器投射距离改成与速度相关的变量导致，原因未知），改回常数就没事
                {
                    overrideBrakePowerNA[index] = 0f;
                    overrideAccelerationPowerNA[index] = 0f;
                    overrideInputNA[index] = false;
                }
                #endregion

                #region move
                if (isDrivingNA[index])
                {
                    if (targetSpeedNA[index] > speedLimitNA[index]) targetSpeedNA[index] = speedLimitNA[index];
                    if (targetSpeedNA[index] > topSpeedNA[index]) targetSpeedNA[index] = topSpeedNA[index];//目标速度改为由平均速度和方差（写在WayPoint里）正态化随机生成，随机生成的速度如果超过了限速就取限速                
                    if (frontHitNA[index]) targetSpeedNA[index] = Mathf.InverseLerp(0, frontSensorLengthNA[index], frontHitDistanceNA[index]) * targetSpeedNA[index];//前方传感器发现障碍物，减速，目标速度调整为原目标速度*障碍物距离插值
                    accelNA[index] = targetSpeedNA[index] - speedNA[index];//加速度=目标速度-速度
                    localTargetNA[index] = driveTargetTransformAccessArray.localPosition;//获取目标路径点在本地坐标下位置
                    targetAngleNA[index] = math.atan2(localTargetNA[index].x, localTargetNA[index].z) * 52.29578f;//arctan反算目标角度
                    steerAngleNA[index] = math.clamp(targetAngleNA[index] * steerSensitivity, -1, 1) * math.sign(speedNA[index]);//控制方向盘转角（控制的是最大转角的比例），考虑到倒车情况
                    steerAngleNA[index] *= maxSteerAngle;//方向盘转角（*=乘法幅值，类似+=）
                    if (speedNA[index] > targetSpeedNA[index])
                    {
                        motorTorqueNA[index] = 0;
                        accelerationInputNA[index] = 0;
                        overrideInputNA[index] = true;
                        overrideBrakePowerNA[index] = 0f;
                        overrideAccelerationPowerNA[index] = 0f;
                    }//超速
                    else
                    {
                        accelerationInputNA[index] = math.clamp(accelNA[index], 0, 1);//用clamp夹定范围，未接近目标速度时油门踩满，在逼近目标速度时衰减
                        motorTorqueNA[index] = accelerationInputNA[index] * accelerationPowerNA[index];//油门=加速度参数*加速马力
                    }
                    brakeTorqueNA[index] = 0;
                    moveHandBrakeNA[index] = 0;
                }//行驶
                else
                {
                    if (speedNA[index] > 2)
                    {
                        localTargetNA[index] = driveTargetTransformAccessArray.localPosition;
                        targetAngleNA[index] = math.atan2(localTargetNA[index].x, localTargetNA[index].z) * 52.29578f;
                        steerAngleNA[index] = math.clamp(targetAngleNA[index] * steerSensitivity, -1, 1) * math.sign(speedNA[index]);
                        steerAngleNA[index] *= maxSteerAngle;
                        accelerationInputNA[index] = 0;
                        motorTorqueNA[index] = 0;
                        brakeTorqueNA[index] = 1;
                        moveHandBrakeNA[index] = 1;
                    }
                    else
                    {
                        steerAngleNA[index] = 0;
                        accelerationInputNA[index] = 0;
                        motorTorqueNA[index] = 0;
                        brakeTorqueNA[index] = 1;
                        moveHandBrakeNA[index] = 1;
                    }
                }//驻车

                if (overrideInputNA[index])//触发其他函数条件，使用该函数重写输入
                {
                    accelerationInputNA[index] = overrideAccelerationPowerNA[index];
                    motorTorqueNA[index] = overrideAccelerationPowerNA[index] * accelerationPowerNA[index];
                    brakeTorqueNA[index] = overrideBrakePowerNA[index] * brakePowerNA[index];
                    isBrakingNA[index] = true;
                }
                else if (brakeTorqueNA[index] > 0.0f) isBrakingNA[index] = true;
                else if (brakeTorqueNA[index] == 0.0f) isBrakingNA[index] = false;

                speedNA[index] = ((carTransformPositionNA[index] - carTransformPreviousPositionNA[index]).magnitude / deltaTime) * speedMultiplier;
                #endregion
            }
        }
    }
}