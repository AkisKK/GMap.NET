using System;
using System.Collections.Generic;
using System.Threading;

namespace GMap.NET.Internals;

/// <summary>
///     represent tile
/// </summary>
public struct Tile : IDisposable
{
    public static readonly Tile Empty;

    GPoint m_Pos;
    PureImage[] m_Overlays;
    long m_OverlaysCount;

    public readonly bool NotEmpty;

    public Tile(int zoom, GPoint pos)
    {
        NotEmpty = true;
        Zoom = zoom;
        m_Pos = pos;
        m_Overlays = null;
        m_OverlaysCount = 0;
    }

    public IEnumerable<PureImage> Overlays
    {
        get
        {
            for (long i = 0, size = Interlocked.Read(ref m_OverlaysCount); i < size; i++)
            {
                yield return m_Overlays[i];
            }
        }
    }

    internal void AddOverlay(PureImage i)
    {
        m_Overlays ??= new PureImage[4];

        m_Overlays[Interlocked.Increment(ref m_OverlaysCount) - 1] = i;
    }

    internal bool HasAnyOverlays => Interlocked.Read(ref m_OverlaysCount) > 0;

    public int Zoom { get; private set; }

    public GPoint Pos
    {
        readonly get => m_Pos;
        private set => m_Pos = value;
    }

    #region IDisposable Members

    public void Dispose()
    {
        if (m_Overlays != null)
        {
            for (long i = Interlocked.Read(ref m_OverlaysCount) - 1; i >= 0; i--)
            {
                Interlocked.Decrement(ref m_OverlaysCount);
                m_Overlays[i].Dispose();
                m_Overlays[i] = null;
            }

            m_Overlays = null;
        }
    }

    #endregion

    public static bool operator ==(Tile m1, Tile m2)
    {
        return m1.m_Pos == m2.m_Pos && m1.Zoom == m2.Zoom;
    }

    public static bool operator !=(Tile m1, Tile m2)
    {
        return !(m1 == m2);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is not Tile)
        {
            return false;
        }

        var comp = (Tile)obj;
        return comp.Zoom == Zoom && comp.Pos == Pos;
    }

    public override int GetHashCode()
    {
        return Zoom ^ m_Pos.GetHashCode();
    }
}
