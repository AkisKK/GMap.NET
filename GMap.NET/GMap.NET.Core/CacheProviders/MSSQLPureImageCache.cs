using System;
using System.Diagnostics;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using Microsoft.Data.SqlClient;

namespace GMap.NET.CacheProviders;

/// <summary>
/// Image cache for MS SQL server.
/// </summary>
/// <remarks>
/// Optimized by mmurfinsimmons@gmail.com
/// </remarks>
public class MsSQLPureImageCache : IPureImageCache, IDisposable
{
    string m_ConnectionString = string.Empty;

    public string ConnectionString
    {
        get
        {
            return m_ConnectionString;
        }
        set
        {
            if (m_ConnectionString != value)
            {
                m_ConnectionString = value;

                if (Initialized)
                {
                    Dispose();
                    Initialize();
                }
            }
        }
    }

    SqlCommand m_CmdInsert;
    SqlCommand m_CmdFetch;
    SqlConnection m_CnGet;
    SqlConnection m_CnSet;

    bool m_Initialized;

    /// <summary>
    ///     is cache initialized
    /// </summary>
    public bool Initialized
    {
        get
        {
            lock (this)
            {
                return m_Initialized;
            }
        }
        private set
        {
            lock (this)
            {
                m_Initialized = value;
            }
        }
    }

    /// <summary>
    ///     inits connection to server
    /// </summary>
    /// <returns></returns>
    public bool Initialize()
    {
        lock (this)
        {
            if (!Initialized)
            {
                #region prepare mssql & cache table

                try
                {
                    // different connections so the multi-thread inserts and selects don't collide on open readers.
                    m_CnGet = new SqlConnection(m_ConnectionString);
                    m_CnGet.Open();
                    m_CnSet = new SqlConnection(m_ConnectionString);
                    m_CnSet.Open();

                    bool tableExists;
                    using (var cmd = new SqlCommand("select object_id('GMapNETcache')", m_CnGet))
                    {
                        object objid = cmd.ExecuteScalar();
                        tableExists = objid != null && objid != DBNull.Value;
                    }

                    if (!tableExists)
                    {
                        using var cmd = new SqlCommand(
                            "CREATE TABLE [GMapNETcache] ( \n"
                            + "   [Type] [int]   NOT NULL, \n"
                            + "   [Zoom] [int]   NOT NULL, \n"
                            + "   [X]    [int]   NOT NULL, \n"
                            + "   [Y]    [int]   NOT NULL, \n"
                            + "   [Tile] [image] NOT NULL, \n"
                            + "   CONSTRAINT [PK_GMapNETcache] PRIMARY KEY CLUSTERED (Type, Zoom, X, Y) \n"
                            + ")",
                            m_CnGet);
                        cmd.ExecuteNonQuery();
                    }

                    m_CmdFetch =
                        new SqlCommand(
                            "SELECT [Tile] FROM [GMapNETcache] WITH (NOLOCK) WHERE [X]=@x AND [Y]=@y AND [Zoom]=@zoom AND [Type]=@type",
                            m_CnGet);
                    m_CmdFetch.Parameters.Add("@x", System.Data.SqlDbType.Int);
                    m_CmdFetch.Parameters.Add("@y", System.Data.SqlDbType.Int);
                    m_CmdFetch.Parameters.Add("@zoom", System.Data.SqlDbType.Int);
                    m_CmdFetch.Parameters.Add("@type", System.Data.SqlDbType.Int);
                    m_CmdFetch.Prepare();

                    m_CmdInsert =
                        new SqlCommand(
                            "INSERT INTO [GMapNETcache] ( [X], [Y], [Zoom], [Type], [Tile] ) VALUES ( @x, @y, @zoom, @type, @tile )",
                            m_CnSet);
                    m_CmdInsert.Parameters.Add("@x", System.Data.SqlDbType.Int);
                    m_CmdInsert.Parameters.Add("@y", System.Data.SqlDbType.Int);
                    m_CmdInsert.Parameters.Add("@zoom", System.Data.SqlDbType.Int);
                    m_CmdInsert.Parameters.Add("@type", System.Data.SqlDbType.Int);
                    m_CmdInsert.Parameters.Add("@tile", System.Data.SqlDbType.Image); //, calcmaximgsize);
                    //can't prepare insert because of the IMAGE field having a variable size.  Could set it to some 'maximum' size?

                    Initialized = true;
                }
                catch (Exception ex)
                {
                    m_Initialized = false;
                    Debug.WriteLine(ex.Message);
                }

                #endregion
            }

            return Initialized;
        }
    }

    #region IDisposable Members

    public void Dispose()
    {
        lock (m_CmdInsert)
        {
            if (m_CmdInsert != null)
            {
                m_CmdInsert.Dispose();
                m_CmdInsert = null;
            }

            if (m_CnSet != null)
            {
                m_CnSet.Dispose();
                m_CnSet = null;
            }
        }

        lock (m_CmdFetch)
        {
            if (m_CmdFetch != null)
            {
                m_CmdFetch.Dispose();
                m_CmdFetch = null;
            }

            if (m_CnGet != null)
            {
                m_CnGet.Dispose();
                m_CnGet = null;
            }
        }

        Initialized = false;
        GC.SuppressFinalize(this);
    }

    #endregion

    #region PureImageCache Members

    public bool PutImageToCache(byte[] tile, int type, GPoint pos, int zoom)
    {
        bool ret = true;
        {
            if (Initialize())
            {
                try
                {
                    lock (m_CmdInsert)
                    {
                        m_CmdInsert.Parameters["@x"].Value = pos.X;
                        m_CmdInsert.Parameters["@y"].Value = pos.Y;
                        m_CmdInsert.Parameters["@zoom"].Value = zoom;
                        m_CmdInsert.Parameters["@type"].Value = type;
                        m_CmdInsert.Parameters["@tile"].Value = tile;
                        m_CmdInsert.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    ret = false;
                    Dispose();
                }
            }
        }
        return ret;
    }

    public PureImage GetImageFromCache(int type, GPoint pos, int zoom)
    {
        PureImage ret = null;
        {
            if (Initialize())
            {
                try
                {
                    object odata;
                    lock (m_CmdFetch)
                    {
                        m_CmdFetch.Parameters["@x"].Value = pos.X;
                        m_CmdFetch.Parameters["@y"].Value = pos.Y;
                        m_CmdFetch.Parameters["@zoom"].Value = zoom;
                        m_CmdFetch.Parameters["@type"].Value = type;
                        odata = m_CmdFetch.ExecuteScalar();
                    }

                    if (odata != null && odata != DBNull.Value)
                    {
                        byte[] tile = (byte[])odata;
                        if (tile != null && tile.Length > 0)
                        {
                            if (GMapProvider.m_TileImageProxy != null)
                            {
                                ret = GMapProvider.m_TileImageProxy.FromArray(tile);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString());
                    ret = null;
                    Dispose();
                }
            }
        }
        return ret;
    }

    /// <summary>
    ///     NotImplemented
    /// </summary>
    /// <param name="date"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    int IPureImageCache.DeleteOlderThan(DateTime date, int? type)
    {
        throw new NotImplementedException();
    }

    #endregion
}
