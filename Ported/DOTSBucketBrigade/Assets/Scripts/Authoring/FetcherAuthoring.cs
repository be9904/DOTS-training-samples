﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

public class FetcherAuthoring : MonoBehaviour
    , IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager
        , GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<BucketFetcher>(entity);
        dstManager.AddComponentData(entity, new Speed()
        {
            Value = new float3(0.2f, 0.0f, 0.2f)
        });

        dstManager.AddComponentData(entity, new TargetPosition()
        {
            Value = new float3(0.0f, 0.0f, 0.0f)
        });

        dstManager.AddComponent<BucketID>(entity);
    }
}