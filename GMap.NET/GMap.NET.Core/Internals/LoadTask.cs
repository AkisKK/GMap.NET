using System;
using System.Collections.Generic;

namespace GMap.NET.Internals;

/// <summary>
///     tile load task
/// </summary>
internal struct LoadTask : IEquatable<LoadTask>
{
    public GPoint Pos;
    public int Zoom;

    internal Core m_Core;

    public LoadTask(GPoint pos, int zoom, Core core = null)
    {
        Pos = pos;
        Zoom = zoom;
        m_Core = core;
    }

    public override string ToString()
    {
        return Zoom + " - " + Pos.ToString();
    }

    #region IEquatable<LoadTask> Members

    public readonly bool Equals(LoadTask other)
    {
        return Zoom == other.Zoom && Pos == other.Pos;
    }

    public override readonly bool Equals(object obj)
    {
        return obj is LoadTask task && Equals(task);
    }

    public override readonly int GetHashCode()
    {
        // Combine the hash codes of Pos and Zoom for better distribution
        int hashPos = Pos.GetHashCode();
        int hashZoom = Zoom.GetHashCode();

        // Combine using XOR and a prime number multiplier
        return hashPos ^ (hashZoom * 397);
    }
    #endregion
}

internal class LoadTaskComparer : IEqualityComparer<LoadTask>
{
    public bool Equals(LoadTask x, LoadTask y)
    {
        return x.Zoom == y.Zoom && x.Pos == y.Pos;
    }

    public int GetHashCode(LoadTask obj)
    {
        return obj.Zoom ^ obj.Pos.GetHashCode();
    }
}
