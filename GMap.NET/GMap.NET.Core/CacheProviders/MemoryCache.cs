using System;
using System.Diagnostics;
using GMap.NET.Internals;

namespace GMap.NET.CacheProviders;

public class MemoryCache : IDisposable
{
    private readonly KiberTileCache m_TilesInMemory = [];

    private FastReaderWriterLock m_KiberCacheLock = new();

    /// <summary>
    ///     the amount of tiles in MB to keep in memory, default: 22MB, if each ~100Kb it's ~222 tiles
    /// </summary>
    public int Capacity
    {
        get
        {
            m_KiberCacheLock.AcquireReaderLock();
            try
            {
                return m_TilesInMemory.MemoryCacheCapacity;
            }
            finally
            {
                m_KiberCacheLock.ReleaseReaderLock();
            }
        }
        set
        {
            m_KiberCacheLock.AcquireWriterLock();
            try
            {
                m_TilesInMemory.MemoryCacheCapacity = value;
            }
            finally
            {
                m_KiberCacheLock.ReleaseWriterLock();
            }
        }
    }

    /// <summary>
    ///     current memory cache size in MB
    /// </summary>
    public double Size
    {
        get
        {
            m_KiberCacheLock.AcquireReaderLock();
            try
            {
                return m_TilesInMemory.MemoryCacheSize;
            }
            finally
            {
                m_KiberCacheLock.ReleaseReaderLock();
            }
        }
    }

    public void Clear()
    {
        m_KiberCacheLock.AcquireWriterLock();
        try
        {
            m_TilesInMemory.Clear();
        }
        finally
        {
            m_KiberCacheLock.ReleaseWriterLock();
        }
    }

    // ...

    internal byte[] GetTileFromMemoryCache(RawTile tile)
    {
        m_KiberCacheLock.AcquireReaderLock();
        try
        {
            if (m_TilesInMemory.TryGetValue(tile, out byte[] ret))
            {
                return ret;
            }
        }
        finally
        {
            m_KiberCacheLock.ReleaseReaderLock();
        }

        return null;
    }

    internal void AddTileToMemoryCache(RawTile tile, byte[] data)
    {
        if (data != null)
        {
            m_KiberCacheLock.AcquireWriterLock();
            try
            {
                if (!m_TilesInMemory.ContainsKey(tile))
                {
                    m_TilesInMemory.Add(tile, data);
                }
            }
            finally
            {
                m_KiberCacheLock.ReleaseWriterLock();
            }
        }
#if DEBUG
        else
        {
            Debug.WriteLine("adding empty data to MemoryCache ;} ");
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
#endif
    }

    internal void RemoveOverload()
    {
        m_KiberCacheLock.AcquireWriterLock();
        try
        {
            m_TilesInMemory.RemoveMemoryOverload();
        }
        finally
        {
            m_KiberCacheLock.ReleaseWriterLock();
        }
    }

    #region IDisposable Members

    ~MemoryCache()
    {
        Dispose(false);
    }

    void Dispose(bool disposing)
    {
        if (m_KiberCacheLock != null)
        {
            if (disposing)
            {
                Clear();
            }

            m_KiberCacheLock.Dispose();
            m_KiberCacheLock = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
