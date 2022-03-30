using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial class RailSpawnerSystem : SystemBase
{
    private EntityQuery spawnerQuery;

    protected override void OnCreate()
    {
        // Run ONLY if RailSpawnerComponent exists
        RequireForUpdate(spawnerQuery);
    }

    protected override void OnUpdate()
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.TempJob);
        
        Entity trackPrefab = new Entity();
        Entity platformPrefab = new Entity();
        Entity carriagePrefab = new Entity();
        Entity trainPrefab = new Entity();
        float railSpacing = 0;
        
        Entities
            .WithStoreEntityQueryInField(ref spawnerQuery)
            .ForEach((Entity entity, in RailSpawnerComponent railSpawner) =>
        {
            
            trackPrefab = railSpawner.TrackPrefab;
            platformPrefab = railSpawner.PlatformPrefab;
            trainPrefab = railSpawner.TrainPrefab;
            carriagePrefab = railSpawner.CarriagePrefab;
            railSpacing = railSpawner.RailSpacing;
            
            ecb.DestroyEntity(entity);
        }).Run();
        
        

        Entities.ForEach((Entity entity, in LineComponent lineComponent,
            in DynamicBuffer<BezierPointBufferElement> bezierPoints,
            in LineTotalDistanceComponent lineTotalDistanceComponent) =>
        {
            float lineLength = lineTotalDistanceComponent.Value;
            
            for (float i = 0; i * railSpacing < lineLength; i++)
            {
                float t = i / lineLength;

                var instance = ecb.Instantiate(trackPrefab);
                float3 position = BezierHelpers.GetPosition(bezierPoints, lineLength, t);
                var translation = new Translation { Value = position};
                ecb.SetComponent(instance, translation);

                var lookRot = quaternion.LookRotation(BezierHelpers.GetNormalAtPosition(bezierPoints, lineLength, t), new float3(0, 1, 0));
                var rotation = new Rotation { Value = lookRot };
                ecb.SetComponent(instance, rotation);
            }
            
            int trainCount = 10;
            for (int i = 0; i < trainCount; i++)
            {
                var train = ecb.Instantiate(trainPrefab);
                var trainComponent = new TrainComponent { Line = entity };
                ecb.SetComponent(train, trainComponent);

                var trackPosition = new TrackPositionComponent {Value = (float)i / trainCount};
                ecb.SetComponent(train, trackPosition);
                
                for (int j = 0; j < lineComponent.CarriageCount; j++)
                {
                    var carriage = ecb.Instantiate(carriagePrefab);
                    var carriageComponent = new CarriageComponent { Index = j, Train = train };
                    ecb.SetComponent(carriage, carriageComponent);
                }
            }
            
        }).Run();
        
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}