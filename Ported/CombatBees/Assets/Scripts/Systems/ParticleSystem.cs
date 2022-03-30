using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Mathf = UnityEngine.Mathf;
using UnityMaterialPropertyBlock = UnityEngine.MaterialPropertyBlock;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial class ParticleSystemFixed : SystemBase
{
    // Move to a common area
    static float3 fieldSize = new float3(100f, 20f, 30f);
    static float gravity = -20f;

    EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        var up = new float3(0, 1, 0);

        var ecb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();

        // When components are stuck to a surface, velocity is removed which prevents them being considered for any movement jobs.

        // Update velocities
        var velocityJob = Entities
            .ForEach((Entity entity, ref Velocity velocity, in ParticleComponent particle) =>
            {
                velocity.Value += up * (gravity * deltaTime); // using the old gravity const, which could be ported over
            }).ScheduleParallel(Dependency);

        // Update positions from velocities
        var moveJob = Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref Translation translation, ref Scale scale, in Velocity velocity, in ParticleComponent particle) =>
            {
                translation.Value += velocity.Value * deltaTime;

                if (Mathf.Abs(translation.Value.x) > fieldSize.x * .5f)
                {
                    translation.Value.x = fieldSize.x * .5f * Mathf.Sign(translation.Value.x);
                    float splat = Mathf.Abs(velocity.Value.x * .3f) + 1f;
                    scale.Value *= splat;

                    ecb.RemoveComponent(entityInQueryIndex, entity, typeof(Velocity));
                }
                if (Mathf.Abs(translation.Value.y) > fieldSize.y * .5f)
                {
                    translation.Value.y = fieldSize.y * .5f * Mathf.Sign(translation.Value.y);
                    float splat = Mathf.Abs(velocity.Value.y * .3f) + 1f;
                    scale.Value *= splat;

                    ecb.RemoveComponent(entityInQueryIndex, entity, typeof(Velocity));
                }
                if (Mathf.Abs(translation.Value.z) > fieldSize.z * .5f)
                {
                    translation.Value.z = fieldSize.z * .5f * Mathf.Sign(translation.Value.z);
                    float splat = Mathf.Abs(velocity.Value.z * .3f) + 1f;
                    scale.Value *= splat;

                    ecb.RemoveComponent(entityInQueryIndex, entity, typeof(Velocity));
                }
            }).ScheduleParallel(velocityJob);


        // Should be its own system as it is, but it being here should make adding pooling easier
        Dependency = Entities
           .ForEach((Entity entity, int entityInQueryIndex, ref Lifetime lifetime, in ParticleComponent particle) =>
           {
               lifetime.Value -= deltaTime / lifetime.Duration;
               if (lifetime.Value < 0f)
               {
                   ecb.DestroyEntity(entityInQueryIndex, entity);
               }
           }).ScheduleParallel(moveJob);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(ParticleSystemFixed))]
public partial class ParticleSystem : SystemBase
{
    public static void SpawnParticle(Entity entityPrefab, EntityManager entityManager, Random rand, EntityCommandBuffer ecb, float3 position, ParticleComponent.ParticleType type, float3 velocity, float velocityJitter = 6f, int count = 1)
    {
        // Processing each particle via the ECS makes a lot of sense, but creation costs may be an issue. Can pooling be built in?
        for (int i = 0; i < count; i++)
        {
            var entity = entityManager.Instantiate(entityPrefab);

            if (type == ParticleComponent.ParticleType.Blood)
            {
                float3 scale = rand.NextFloat(.1f, .2f);

                ecb.SetComponent(entity, new Lifetime { Value = 1f, Duration = rand.NextFloat(3f, 5f) });
                ecb.SetComponent(entity, new ParticleComponent { Type = type });
                ecb.SetComponent(entity, new Translation { Value = position });
                ecb.SetComponent(entity, new Velocity { Value = velocity + rand.NextFloat3Direction() * velocityJitter });
                ecb.SetComponent(entity, new Scale { Value = scale.x });
                ecb.SetComponent(entity, new Size { Value = scale.x });
                //ecb.SetComponent(entity, new ColorComponent { Value = new float3(1, 0, 0) }); // Was Random.ColorHSV(-.05f,.05f,.75f,1f,.3f,.8f), hardcoding a colour for now
            }
            else
            {
                float3 scale = rand.NextFloat(1f, 2f);

                ecb.SetComponent(entity, new Lifetime { Value = 1f, Duration = rand.NextFloat(.25f, .5f) });
                ecb.SetComponent(entity, new ParticleComponent { Type = type });
                ecb.SetComponent(entity, new Translation { Value = position });
                ecb.SetComponent(entity, new Velocity { Value = rand.NextFloat3Direction() * 5f });
                ecb.SetComponent(entity, new Scale { Value = scale.x });
                ecb.SetComponent(entity, new Size { Value = scale.x });
            }
        }
    }

    protected override void OnUpdate()
    {
        // Update for rendering
        Entities
           .ForEach((Entity entity, ref Rotation rotation, ref Scale renderScale, in Translation translation, in Size size, in Velocity velocity, in Lifetime lifetime, in ParticleComponent particle) =>
           {
               renderScale.Value = size.Value * lifetime.Value;
               if (particle.Type == ParticleComponent.ParticleType.Blood)
               {
                   rotation.Value = quaternion.LookRotation(velocity.Value, new float3(0, 1, 0));
                   renderScale.Value *= 1f + math.length(velocity.Value) * /*speedStretch*/ 0.25f;
               }

           }).ScheduleParallel();
    }
}
