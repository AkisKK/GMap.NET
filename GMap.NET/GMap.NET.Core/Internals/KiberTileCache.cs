using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GMap.NET.Internals;

/// <summary>
///     kiber speed memory cache for tiles with history support ;}
/// </summary>
internal class KiberTileCache : Dictionary<RawTile, byte[]>
{
    public KiberTileCache() : base(new RawTileComparer())
    {
    }

    readonly Queue<RawTile> m_Queue = new();

    /// <summary>
    ///     the amount of tiles in MB to keep in memory, default: 22MB, if each ~100Kb it's ~222 tiles
    /// </summary>
    public int MemoryCacheCapacity = 22;

    long m_MemoryCacheSize;

    /// <summary>
    ///     current memory cache size in MB
    /// </summary>
    public double MemoryCacheSize
    {
        get
        {
            return m_MemoryCacheSize / 1048576.0;
        }
    }

    public new void Add(RawTile key, byte[] value)
    {
        m_Queue.Enqueue(key);
        base.Add(key, value);

        m_MemoryCacheSize += value.Length;
    }

    public new void Clear()
    {
        m_Queue.Clear();
        base.Clear();
        m_MemoryCacheSize = 0;
    }

    internal void RemoveMemoryOverload()
    {
        while (MemoryCacheSize > MemoryCacheCapacity)
        {
            if (Keys.Count > 0 && m_Queue.Count > 0)
            {
                var first = m_Queue.Dequeue();
                try
                {
                    byte[] m = base[first];
                    {
                        base.Remove(first);
                        m_MemoryCacheSize -= m.Length;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("RemoveMemoryOverload: " + ex);
                }
            }
            else
            {
                break;
            }
        }
    }
}
