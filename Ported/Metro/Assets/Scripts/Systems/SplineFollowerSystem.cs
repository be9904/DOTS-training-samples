using Onboarding.BezierPath;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class SplineFollowerSystem : SystemBase
{
    protected override void OnUpdate() {
        float deltaTime = Time.DeltaTime;

        Entities
            .WithAll<SplineFollower>()
            .ForEach((ref Translation translation,
                      ref Rotation rotation,
                      ref TrackProgress trackProgress,
                      ref SplineFollower splineFollower,
                      in Ticker ticker,
                      in Speed speed) =>
            {
                if (ticker.TimeRemaining <= 0)
                {
                     var spline = GetComponent<Spline>(splineFollower.track);
                     ref var splineData = ref spline.splinePath.Value;
                     trackProgress.Value += speed.Value * deltaTime;
                     trackProgress.Value = trackProgress.Value % splineData.pathLength;
                     
                     SplineInterpolationHelper.InterpolatePositionAndDirection(ref splineData, ref trackProgress.SplineLookupCache, trackProgress.Value, out var position, out var direction);
                     translation.Value = position;
                     rotation.Value = quaternion.LookRotationSafe(direction, new float3(0, 1, 0));   
                }
                
            }).ScheduleParallel();
    }
}