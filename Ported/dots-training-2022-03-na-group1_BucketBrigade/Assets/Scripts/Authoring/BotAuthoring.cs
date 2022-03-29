﻿using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityMonoBehaviour = UnityEngine.MonoBehaviour;
using UnityMeshRenderer = UnityEngine.MeshRenderer;

public class BotAuthoring : UnityMonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager
        , GameObjectConversionSystem conversionSystem)
    {
        var allRenderers = transform.GetComponentsInChildren<UnityMeshRenderer>();
        var needBaseColor = new NativeArray<Entity>(allRenderers.Length, Allocator.Temp);

        for(int i = 0; i < allRenderers.Length; ++i)
        {
            var meshRenderer = allRenderers[i];
            needBaseColor[i] = conversionSystem.GetPrimaryEntity(meshRenderer.gameObject);
        }

        // We could have used AddComponent in the loop above, but as a general rule in
        // DOTS, doing a batch of things at once is more efficient.
        dstManager.AddComponent<URPMaterialPropertyBaseColor>(needBaseColor);
        dstManager.AddComponent<PropagateColor>(entity);
        dstManager.AddComponent<Position>(entity);
        dstManager.AddComponent<Color>(entity);
        dstManager.AddComponent<Destination>(entity);
        dstManager.AddComponent<WorkerTag>(entity);
        dstManager.AddComponent<HoldingWhichBucket>(entity);
        dstManager.AddComponent<DestinationWorker>(entity);
        dstManager.AddComponent<MyFireCaptain>(entity);
        dstManager.AddComponent<MyWaterCaptain>(entity);
        dstManager.AddComponent<MyWorkerState>(entity);
    }
}