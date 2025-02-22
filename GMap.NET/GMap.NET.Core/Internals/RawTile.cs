﻿using System.Collections.Generic;

namespace GMap.NET.Internals;

/// <summary>
///     struct for raw tile
/// </summary>
internal struct RawTile
{
    public int Type;
    public GPoint Pos;
    public int Zoom;

    public RawTile(int type, GPoint pos, int zoom)
    {
        Type = type;
        Pos = pos;
        Zoom = zoom;
    }

    public override readonly string ToString()
    {
        return Type + " at zoom " + Zoom + ", pos: " + Pos;
    }
}

internal class RawTileComparer : IEqualityComparer<RawTile>
{
    public bool Equals(RawTile x, RawTile y)
    {
        return x.Type == y.Type && x.Zoom == y.Zoom && x.Pos == y.Pos;
    }

    public int GetHashCode(RawTile obj)
    {
        return obj.Type ^ obj.Zoom ^ obj.Pos.GetHashCode();
    }
}
