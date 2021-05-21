﻿using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct GameConfig : IComponentData
{
    public float MouseSpeed;
    public float CatSpeed;
    public int NumOfCats;
    public int NumOfMice;
    public int NumOfAIPlayers;
    public int RoundDuration;
    public int2 BoardDimensions;
    public float4 TileColor1;
    public float4 TileColor2;
    public float ControlSensitivity;
    public bool FixedCamera;

    public Entity CellPrefab;
    public Entity WallPrefab;
    public float WallProbability;
    public Entity CatPrefab;
    public Entity ArrowPrefab;
    public Entity MousePrefab;
    public Entity CursorPrefab;
    public int MaximumArrows;

    public bool RandomSeed;
    public uint Seed;

    public float MouseSpawnDelay;
    public float CatSpawnDelay;
    public Entity HomebasePrefab;

    public int MaxAnimalsSpawnedPerFrame;
    public bool MiceSpawnInRandomLocations;
    public float CameraOffset;
}