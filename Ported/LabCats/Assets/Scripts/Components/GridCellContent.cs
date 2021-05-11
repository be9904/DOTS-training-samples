using Unity.Entities;
using System;


public struct GridCellContent : IBufferElementData
{
    public GridCellType Type;
    public WallBoundaries Walls;
}

public enum GridCellType : byte
{
    Goal,
    Hole,
    ArrowLeft,
    ArrowRight,
    ArrowDown,
    ArrowUp
}

[Flags]
public enum WallBoundaries : byte
{
    WallLeft,
    WallRight,
    WallDown,
    WallUp
}