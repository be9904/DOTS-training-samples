using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(PickLinePositionsForTeamSystem))]
public partial class SetLakeAsTargetSystem : SystemBase
{
    private EntityCommandBufferSystem CommandBufferSystem;

    private EntityQuery LakeQuery;

    protected override void OnCreate()
    {
        LakeQuery = GetEntityQuery(ComponentType.ReadOnly<Lake>(), ComponentType.ReadOnly<Translation>());
        CommandBufferSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var ecb = CommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // TODO: if there are no flames don't do anything
        Entities
            .ForEach((Entity e, int entityInQueryIndex, in Translation translation, in BucketFetcher bucketFetcher,
                in HoldingBucket holdingBucket) =>
            {
                var distance = math.distance(bucketFetcher.LakePosition, translation.Value);

                if (distance < 0.1f )
                {
                    if (!HasComponent<TargetDestination>(e))
                        return;
                    
                    ecb.RemoveComponent<TargetDestination>(entityInQueryIndex, e);

                    if (HasComponent<HoldsBucketBeingFilled>(e))
                    {
                        ecb.AppendToBuffer(entityInQueryIndex, bucketFetcher.Lake,
                            new BucketFillAction
                            {
                                Bucket = holdingBucket.HeldBucket, FireFighter = e, BucketVolume = 0f,
                                Position = translation.Value
                            });
                        return;
                    }
                    
                    ecb.RemoveComponent<HoldsEmptyBucket>(entityInQueryIndex, e);
                    ecb.AddComponent<HoldsBucketBeingFilled>(entityInQueryIndex, e);
                    ecb.RemoveComponent<EmptyBucket>(entityInQueryIndex, holdingBucket.HeldBucket);
                    // BUcket volume == hack
                    ecb.AppendToBuffer(entityInQueryIndex, bucketFetcher.Lake,
                        new BucketFillAction
                        {
                            Bucket = holdingBucket.HeldBucket, FireFighter = e, BucketVolume = 0f,
                            Position = translation.Value
                        });
                }
                else
                {
                    ecb.AddComponent(entityInQueryIndex, e, new TargetDestination { Value = bucketFetcher.LakePosition.xz });
                }

                /*
                if (lakeTranslations.Length == 0)
                    return;

                // HACK: We assume that a flame exists here...
                var closestIndex = -1;
                var closest = new float2(10000000, 100000); // This is bad HACK
                var bestDistance = float.MaxValue;
                // HACK: We are mixing types, this is awful.

                for (int i = 0; i < lakeTranslations.Length; i++)
                {
                    var dist = math.distance(lakeTranslations[i].Value, translation.Value);

                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        closest = lakeTranslations[i].Value.xz;
                        closestIndex = i;
                    }
                }

                // TODO: If distance is close enough to fill bucket, set a "filling bucket" tag instead
                if (bestDistance < 0.1f)
                {
                    ecb.RemoveComponent<HoldsEmptyBucket>(entityInQueryIndex, e);
                    ecb.AddComponent<HoldsBucketBeingFilled>(entityInQueryIndex, e);
                    ecb.RemoveComponent<EmptyBucket>(entityInQueryIndex, holdingBucket.HeldBucket);
                // BUcket volume == hack
                    ecb.AppendToBuffer(entityInQueryIndex, lakeEntities[closestIndex], new BucketFillAction { Bucket = holdingBucket.HeldBucket, FireFighter = e, BucketVolume = 0f , Position = translation.Value });
                }
                else
                {
                    ecb.AddComponent(entityInQueryIndex, e, new TargetDestination { Value = closest });
                }*/
            }).ScheduleParallel();


        CommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}