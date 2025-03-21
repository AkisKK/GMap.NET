﻿using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using GMap.NET.CacheProviders;

namespace GMap.NET.Internals;

internal class CacheLocator
{
    private static string m_Location;

    public static string Location
    {
        get
        {
            if (string.IsNullOrEmpty(m_Location))
            {
                Reset();
            }

            return m_Location;
        }
        set
        {
            if (string.IsNullOrEmpty(value)) // setting to null resets to default
            {
                Reset();
            }
            else
            {
                m_Location = value;
            }

            if (Delay)
            {
                Cache.Instance.CacheLocation = m_Location;
            }
        }
    }

    static void Reset()
    {
        string appDataLocation = GetApplicationDataFolderPath();

        // http://greatmaps.codeplex.com/discussions/403151
        // by default Network Service don't have disk write access
        if (string.IsNullOrEmpty(appDataLocation))
        {
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            GMaps.Instance.UseDirectionsCache = false;
            GMaps.Instance.UseGeocoderCache = false;
            GMaps.Instance.UsePlacemarkCache = false;
            GMaps.Instance.UseRouteCache = false;
            GMaps.Instance.UseUrlCache = false;
        }
        else
        {
            Location = appDataLocation;
        }
    }

    public static string GetApplicationDataFolderPath()
    {
        bool isSystem = false;
        try
        {
            using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            if (identity != null)
            {
                isSystem = identity.IsSystem;
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine("SQLitePureImageCache, WindowsIdentity.GetCurrent: " + ex);
        }

        string path;

        // https://greatmaps.codeplex.com/workitem/16112
        if (isSystem)
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        }
        else
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        if (!string.IsNullOrEmpty(path))
        {
            path += Path.DirectorySeparatorChar + "GMap.NET" + Path.DirectorySeparatorChar;
        }

        return path;
    }

    public static bool Delay;
}

/// <summary>
///     cache system for tiles, geocoding, etc...
/// </summary>
internal class Cache
{
    /// <summary>
    ///     abstract image cache
    /// </summary>
    public IPureImageCache ImageCache;

    /// <summary>
    ///     second level abstract image cache
    /// </summary>
    public IPureImageCache ImageCacheSecond;

    string m_Cache;

    /// <summary>
    ///     local cache location
    /// </summary>
    public string CacheLocation
    {
        get
        {
            return m_Cache;
        }
        set
        {
            m_Cache = value;
#if SQLite
            if (ImageCache is SQLitePureImageCache)
            {
                (ImageCache as SQLitePureImageCache).CacheLocation = value;
            }
#else
        if(ImageCache is MsSQLCePureImageCache)
        {
           (ImageCache as MsSQLCePureImageCache).CacheLocation = value;
        }
#endif
            CacheLocator.Delay = true;
        }
    }

    public static Cache Instance { get; } = new Cache();

    private Cache()
    {
#if SQLite
        ImageCache = new SQLitePureImageCache();
#else
     // you can use $ms stuff if you like too ;}
     ImageCache = new MsSQLCePureImageCache();
#endif

        {
            string newCache = CacheLocator.Location;

            string oldCache = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                              Path.DirectorySeparatorChar + "GMap.NET" + Path.DirectorySeparatorChar;

            // move database to non-roaming user directory
            if (Directory.Exists(oldCache))
            {
                try
                {
                    if (Directory.Exists(newCache))
                    {
                        Directory.Delete(oldCache, true);
                    }
                    else
                    {
                        Directory.Move(oldCache, newCache);
                    }

                    CacheLocation = newCache;
                }
                catch (Exception ex)
                {
                    CacheLocation = oldCache;
                    Trace.WriteLine("SQLitePureImageCache, moving data: " + ex.ToString());
                }
            }
            else
            {
                CacheLocation = newCache;
            }
        }
    }

    #region -- etc cache --
    static readonly SHA1 m_HashProvider = SHA1.Create();

    static void ConvertToHash(ref string s)
    {
        s = BitConverter.ToString(m_HashProvider.ComputeHash(Encoding.Unicode.GetBytes(s)));
    }

    public void SaveContent(string url, CacheType type, string content)
    {
        try
        {
            ConvertToHash(ref url);

            string dir = Path.Combine(m_Cache, type.ToString()) + Path.DirectorySeparatorChar;

            // Pre-create directory
            Directory.CreateDirectory(dir);

            string file = dir + url + ".txt";

            using var writer = new StreamWriter(file, false, Encoding.UTF8);
            writer.Write(content);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("SaveContent: " + ex);
        }
    }

    public string GetContent(string url, CacheType type, TimeSpan stayInCache)
    {
        string ret = null;

        try
        {
            ConvertToHash(ref url);

            string dir = Path.Combine(m_Cache, type.ToString()) + Path.DirectorySeparatorChar;
            string file = dir + url + ".txt";

            if (File.Exists(file))
            {
                var writeTime = File.GetLastWriteTime(file);
                if (DateTime.Now - writeTime < stayInCache)
                {
                    using var r = new StreamReader(file, Encoding.UTF8);
                    ret = r.ReadToEnd();
                }
                else
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception ex)
        {
            ret = null;
            Debug.WriteLine("GetContent: " + ex);
        }

        return ret;
    }

    public string GetContent(string url, CacheType type)
    {
        return GetContent(url, type, TimeSpan.FromDays(100));
    }
    #endregion
}

internal enum CacheType
{
    GeocoderCache,
    PlacemarkCache,
    RouteCache,
    UrlCache,
    DirectionsCache,
}
