using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class BeeIdling: SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        //var vector = random.NextFloat3Direction();
        var random = new Unity.Mathematics.Random(1 + (uint)(Time.ElapsedTime*10000));
        Entities
            //.WithNone<GoingForFood,Attacking,BringingFoodBack>()
            .WithoutBurst() // TODO remove in final version, keep for reference to random
            .ForEach((ref Force force, in Velocity velocity, in Bee bee) =>
            {                
                force.Value += random.NextFloat3Direction()*4f;
                //if( worldBound.Value.Contains(translation.Value))
                     
            }).Schedule();
    }
}
