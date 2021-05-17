using Unity.Collections;
using Unity.Entities;
using UnityEditor.SceneManagement;
using UnityInput = UnityEngine.Input;
using UnityKeyCode = UnityEngine.KeyCode;
using UnityDebug = UnityEngine.Debug;

public class PlayerInput : SystemBase
{
    private EntityQuery antWallQueryGroup;
    
    protected override void OnUpdate()
    {
        var simSpeedEntity = GetSingletonEntity<SimulationSpeed>();
        
        // Loop through numeric keycodes and assign Simulation Speed upon match
        for( int i = (int) UnityKeyCode.Alpha1 ; i < (int) UnityKeyCode.Alpha9 ; ++i )
        {
            
            if(UnityInput.GetKeyDown((UnityKeyCode)i))
            {
                SimulationSpeed newSimSpeed = new SimulationSpeed
                {
                    Value = i
                };
                SetComponent(simSpeedEntity, newSimSpeed);
            }
        }
        
        // Upon 'Reset' input, find all 'Wall' and 'Ant' entities and
        // destroy them using the EntityCommandBuffer
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        if (UnityInput.GetKeyDown(UnityKeyCode.R))
        {
            // Create group query
            var antQuery = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Position), typeof(Direction) }
            };
            var wallQuery = new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(Wall) }
            };

            antWallQueryGroup = GetEntityQuery(new EntityQueryDesc[] {antQuery, wallQuery});
            
            // Destroy all Walls and Ants
            Entities
                .WithStoreEntityQueryInField(ref antWallQueryGroup)
                .ForEach((Entity entity) =>
                {
                    ecb.DestroyEntity(entity);
                }).Run();
            
            // Add Respawn component to AntSpawner
            Entities
                .WithAny<AntSpawner, WallSpawner>()
                .ForEach((Entity entity) =>
                {
                    ecb.AddComponent<Respawn>(entity);
                }).Run();
        }
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}