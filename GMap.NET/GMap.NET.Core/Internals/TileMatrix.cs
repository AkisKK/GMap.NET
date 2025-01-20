using System;
using System.Collections.Generic;

namespace GMap.NET.Internals;

/// <summary>
///     matrix for tiles
/// </summary>
internal class TileMatrix : IDisposable
{
    List<Dictionary<GPoint, Tile>> m_Levels = new(33);
    FastReaderWriterLock m_Lock = new();

    public TileMatrix()
    {
        for (int i = 0; i < m_Levels.Capacity; i++)
        {
            m_Levels.Add(new Dictionary<GPoint, Tile>(55, new GPointComparer()));
        }
    }

    public void ClearAllLevels()
    {
        m_Lock.AcquireWriterLock();
        try
        {
            foreach (var matrix in m_Levels)
            {
                foreach (var t in matrix)
                {
                    t.Value.Dispose();
                }

                matrix.Clear();
            }
        }
        finally
        {
            m_Lock.ReleaseWriterLock();
        }
    }

    public void ClearLevel(int zoom)
    {
        m_Lock.AcquireWriterLock();
        try
        {
            if (zoom < m_Levels.Count)
            {
                var l = m_Levels[zoom];

                foreach (var t in l)
                {
                    t.Value.Dispose();
                }

                l.Clear();
            }
        }
        finally
        {
            m_Lock.ReleaseWriterLock();
        }
    }

    List<KeyValuePair<GPoint, Tile>> m_Tmp = new(44);

    public void ClearLevelAndPointsNotIn(int zoom, List<DrawTile> list)
    {
        m_Lock.AcquireWriterLock();
        try
        {
            if (zoom < m_Levels.Count)
            {
                var l = m_Levels[zoom];

                m_Tmp.Clear();

                foreach (var t in l)
                {
                    if (!list.Exists(p => p.PosXY == t.Key))
                    {
                        m_Tmp.Add(t);
                    }
                }

                foreach (var r in m_Tmp)
                {
                    l.Remove(r.Key);
                    r.Value.Dispose();
                }

                m_Tmp.Clear();
            }
        }
        finally
        {
            m_Lock.ReleaseWriterLock();
        }
    }

    public void ClearLevelsBelove(int zoom)
    {
        m_Lock.AcquireWriterLock();
        try
        {
            if (zoom - 1 < m_Levels.Count)
            {
                for (int i = zoom - 1; i >= 0; i--)
                {
                    var l = m_Levels[i];

                    foreach (var t in l)
                    {
                        t.Value.Dispose();
                    }

                    l.Clear();
                }
            }
        }
        finally
        {
            m_Lock.ReleaseWriterLock();
        }
    }

    public void ClearLevelsAbove(int zoom)
    {
        m_Lock.AcquireWriterLock();
        try
        {
            if (zoom + 1 < m_Levels.Count)
            {
                for (int i = zoom + 1; i < m_Levels.Count; i++)
                {
                    var l = m_Levels[i];

                    foreach (var t in l)
                    {
                        t.Value.Dispose();
                    }

                    l.Clear();
                }
            }
        }
        finally
        {
            m_Lock.ReleaseWriterLock();
        }
    }

    public void EnterReadLock()
    {
        m_Lock.AcquireReaderLock();
    }

    public void LeaveReadLock()
    {
        m_Lock.ReleaseReaderLock();
    }

    public Tile GetTileWithNoLock(int zoom, GPoint p)
    {
        Tile ret;

        //if(zoom < Levels.Count)
        {
            m_Levels[zoom].TryGetValue(p, out ret);
        }

        return ret;
    }

    public Tile GetTileWithReadLock(int zoom, GPoint p)
    {
        var ret = Tile.Empty;

        m_Lock.AcquireReaderLock();
        try
        {
            ret = GetTileWithNoLock(zoom, p);
        }
        finally
        {
            m_Lock.ReleaseReaderLock();
        }

        return ret;
    }

    public void SetTile(Tile t)
    {
        m_Lock.AcquireWriterLock();
        try
        {
            if (t.Zoom < m_Levels.Count)
            {
                m_Levels[t.Zoom][t.Pos] = t;
            }
        }
        finally
        {
            m_Lock.ReleaseWriterLock();
        }
    }

    #region IDisposable Members

    ~TileMatrix()
    {
        Dispose(false);
    }

    void Dispose(bool disposing)
    {
        if (m_Lock != null)
        {
            if (disposing)
            {
                ClearAllLevels();
            }

            m_Levels.Clear();
            m_Levels = null;

            m_Tmp.Clear();
            m_Tmp = null;

            m_Lock.Dispose();
            m_Lock = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
