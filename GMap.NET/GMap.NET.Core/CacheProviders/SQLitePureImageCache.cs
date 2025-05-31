using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading;
using GMap.NET.Internals;
using GMap.NET.MapProviders;

namespace GMap.NET.CacheProviders;

#if SQLite

#if !MONO
#else
using SQLiteConnection = Mono.Data.Sqlite.SqliteConnection;
using SQLiteTransaction = Mono.Data.Sqlite.SqliteTransaction;
using SQLiteCommand = Mono.Data.Sqlite.SqliteCommand;
using SQLiteDataReader = Mono.Data.Sqlite.SqliteDataReader;
using SQLiteParameter = Mono.Data.Sqlite.SqliteParameter;
#endif

/// <summary>
///     ultra fast cache system for tiles
/// </summary>
public class SQLitePureImageCache : IPureImageCache
{
#if !MONO
    static SQLitePureImageCache()
    {
        AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
    }

    static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
    {
        return null;
    }

    static int m_Ping;

    /// <summary>
    ///     triggers dynamic sqlite loading
    /// </summary>
    public static void Ping()
    {
        if (++m_Ping == 1)
        {
            Trace.WriteLine("SQLiteVersion: " + SQLiteConnection.SQLiteVersion + " | " +
                            SQLiteConnection.SQLiteSourceId + " | " + SQLiteConnection.DefineConstants);
        }
    }
#endif

    string m_Cache;
    string m_Directory;
    string m_Db;
    bool m_Created;

    public string GTileCache { get; private set; }

    /// <summary>
    /// Local cache location.
    /// </summary>
    public string CacheLocation
    {
        get => m_Cache;
        set
        {
            m_Cache = value;

            GTileCache = Path.Combine(m_Cache, "TileDBv5") + Path.DirectorySeparatorChar;

            m_Directory = GTileCache + GMapProvider.LanguageStr + Path.DirectorySeparatorChar;

            // Pre-create directory.
            if (!Directory.Exists(m_Directory))
            {
                Directory.CreateDirectory(m_Directory);
            }

#if !MONO
            SQLiteConnection.ClearAllPools();
#endif
            // make empty db
            {
                m_Db = m_Directory + "Data.gmdb";

                if (!File.Exists(m_Db))
                {
                    m_Created = CreateEmptyDB(m_Db);
                }
                else
                {
                    m_Created = AlterDBAddTimeColumn(m_Db);
                }

                CheckPreAllocation();

                //var connBuilder = new SQLiteConnectionStringBuilder();
                //connBuilder.DataSource = "c:\filePath.db";
                //connBuilder.Version = 3;
                //connBuilder.PageSize = 4096;
                //connBuilder.JournalMode = SQLiteJournalModeEnum.Wal;
                //connBuilder.Pooling = true;
                //var x = connBuilder.ToString();
#if !MONO
                m_ConnectionString =
                    string.Format("Data Source=\"{0}\";Page Size=32768;Pooling=True", m_Db); //;Journal Mode=Wal
#else
           ConnectionString =
string.Format("Version=3,URI=file://{0},FailIfMissing=True,Page Size=32768,Pooling=True", db);
#endif
            }

            // clear old attachments
            m_AttachedCaches.Clear();
            RebuildFinalSelect();

            // attach all databases from main cache location
            string[] dbs = Directory.GetFiles(m_Directory, "*.gmdb", SearchOption.AllDirectories);
            foreach (string d in dbs)
            {
                if (d != m_Db)
                {
                    Attach(d);
                }
            }
        }
    }

    /// <summary>
    ///     pre-allocate 32MB free space 'ahead' if needed,
    ///     decreases fragmentation
    /// </summary>
    void CheckPreAllocation()
    {
        {
            byte[] pageSizeBytes = new byte[2];
            byte[] freePagesBytes = new byte[4];

            lock (this)
            {
                using var dbf = File.Open(m_Db, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                dbf.Seek(16, SeekOrigin.Begin);

#if !MONO
                dbf.Lock(16, 2);
                dbf.ReadExactly(pageSizeBytes, 0, 2);
                dbf.Unlock(16, 2);

                dbf.Seek(36, SeekOrigin.Begin);

                dbf.Lock(36, 4);
                dbf.ReadExactly(freePagesBytes, 0, 4);
                dbf.Unlock(36, 4);
#else
                    dbf.Read(pageSizeBytes, 0, 2);
                    dbf.Seek(36, SeekOrigin.Begin);
                    dbf.Read(freePagesBytes, 0, 4);
#endif

                dbf.Close();
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(pageSizeBytes);
                Array.Reverse(freePagesBytes);
            }

            ushort pageSize = BitConverter.ToUInt16(pageSizeBytes, 0);
            uint freePages = BitConverter.ToUInt32(freePagesBytes, 0);

            double freeMB = pageSize * freePages / (1024.0 * 1024.0);

            int addSizeMB = 32;
            int waitUntilMB = 4;

            Debug.WriteLine("FreePageSpace in cache: " + freeMB + "MB | " + freePages + " pages");

            if (freeMB <= waitUntilMB)
            {
                PreAllocateDB(m_Db, addSizeMB);
            }
        }
    }

    #region -- import / export --

    public static bool CreateEmptyDB(string file)
    {
        bool ret = true;

        try
        {
            string dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            using var cn = new SQLiteConnection();
#if !MONO
            cn.ConnectionString =
                string.Format("Data Source=\"{0}\";FailIfMissing=False;Page Size=32768", file);
#else
           cn.ConnectionString =
string.Format("Version=3,URI=file://{0},FailIfMissing=False,Page Size=32768", file);
#endif
            cn.Open();
            {
                using (var tr = cn.BeginTransaction())
                {
                    try
                    {
                        using var cmd = cn.CreateCommand();
                        cmd.Transaction = tr;
                        cmd.CommandText = Properties.Resources.CreateTileDb;
                        cmd.ExecuteNonQuery();

                        tr.Commit();
                    }
                    catch (Exception exx)
                    {
#if MONO
                    Console.WriteLine("CreateEmptyDB: " + exx.ToString());
#endif
                        Debug.WriteLine("CreateEmptyDB: " + exx.ToString());

                        tr.Rollback();
                        ret = false;
                    }
                }

                cn.Close();
            }
        }
        catch (Exception ex)
        {
#if MONO
        Console.WriteLine("CreateEmptyDB: " + ex.ToString());
#endif
            Debug.WriteLine("CreateEmptyDB: " + ex.ToString());
            ret = false;
        }

        return ret;
    }

    public static bool PreAllocateDB(string file, int addSizeInMBytes)
    {
        bool ret = true;

        try
        {
            Debug.WriteLine("PreAllocateDB: " + file + ", +" + addSizeInMBytes + "MB");

            using var cn = new SQLiteConnection();
#if !MONO
            cn.ConnectionString =
                string.Format("Data Source=\"{0}\";FailIfMissing=False;Page Size=32768", file);
#else
           cn.ConnectionString =
string.Format("Version=3,URI=file://{0},FailIfMissing=False,Page Size=32768", file);
#endif
            cn.Open();
            {
                using var tr = cn.BeginTransaction();
                try
                {
                    using var cmd = cn.CreateCommand();
                    cmd.Transaction = tr;
                    cmd.CommandText =
                        string.Format(
                            "create table large (a); insert into large values (zeroblob({0})); drop table large;",
                            addSizeInMBytes * 1024 * 1024);
                    cmd.ExecuteNonQuery();

                    tr.Commit();
                }
                catch (Exception exx)
                {
#if MONO
                    Console.WriteLine("PreAllocateDB: " + exx.ToString());
#endif
                    Debug.WriteLine("PreAllocateDB: " + exx.ToString());

                    tr.Rollback();
                    ret = false;
                }

                cn.Close();
            }
        }
        catch (Exception ex)
        {
#if MONO
        Console.WriteLine("PreAllocateDB: " + ex.ToString());
#endif
            Debug.WriteLine("PreAllocateDB: " + ex.ToString());
            ret = false;
        }

        return ret;
    }

    private static bool AlterDBAddTimeColumn(string file)
    {
        bool ret = true;

        try
        {
            if (File.Exists(file))
            {
                using var cn = new SQLiteConnection();
#if !MONO
                cn.ConnectionString =
                    string.Format("Data Source=\"{0}\";FailIfMissing=False;Page Size=32768;Pooling=True", file);
#else
              cn.ConnectionString =
string.Format("Version=3,URI=file://{0},FailIfMissing=False,Page Size=32768,Pooling=True", file);
#endif
                cn.Open();
                {
                    using (var tr = cn.BeginTransaction())
                    {
                        bool? noCacheTimeColumn;

                        try
                        {
                            using var cmd = new SQLiteCommand("SELECT CacheTime FROM Tiles", cn);
                            cmd.Transaction = tr;

                            using var rd = cmd.ExecuteReader();
                            rd.Close();

                            noCacheTimeColumn = false;
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("no such column: CacheTime"))
                            {
                                noCacheTimeColumn = true;
                            }
                            else
                            {
                                throw;
                            }
                        }

                        try
                        {
                            if (noCacheTimeColumn.HasValue && noCacheTimeColumn.Value)
                            {
                                using var cmd = cn.CreateCommand();
                                cmd.Transaction = tr;

                                cmd.CommandText = "ALTER TABLE Tiles ADD CacheTime DATETIME";

                                cmd.ExecuteNonQuery();

                                tr.Commit();
                            }
                        }
                        catch (Exception exx)
                        {
#if MONO
                       Console.WriteLine("AlterDBAddTimeColumn: " + exx.ToString());
#endif
                            Debug.WriteLine("AlterDBAddTimeColumn: " + exx.ToString());

                            tr.Rollback();
                            ret = false;
                        }
                    }

                    cn.Close();
                }
            }
            else
            {
                ret = false;
            }
        }
        catch (Exception ex)
        {
#if MONO
        Console.WriteLine("AlterDBAddTimeColumn: " + ex.ToString());
#endif
            Debug.WriteLine("AlterDBAddTimeColumn: " + ex.ToString());
            ret = false;
        }

        return ret;
    }

    public static bool VacuumDb(string file)
    {
        bool ret = true;

        try
        {
            using var cn = new SQLiteConnection();
#if !MONO
            cn.ConnectionString = string.Format("Data Source=\"{0}\";FailIfMissing=True;Page Size=32768", file);
#else
           cn.ConnectionString = string.Format("Version=3,URI=file://{0},FailIfMissing=True,Page Size=32768", file);
#endif
            cn.Open();
            using var cmd = cn.CreateCommand();
            cmd.CommandText = "vacuum;";
            cmd.ExecuteNonQuery();

            cn.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("VacuumDb: " + ex.ToString());
            ret = false;
        }

        return ret;
    }

    public static bool ExportMapDataToDB(string sourceFile, string destFile)
    {
        bool ret = true;

        try
        {
            if (!File.Exists(destFile))
            {
                ret = CreateEmptyDB(destFile);
            }

            if (ret)
            {
                using var cn1 = new SQLiteConnection();
#if !MONO
                cn1.ConnectionString = string.Format("Data Source=\"{0}\";Page Size=32768", sourceFile);
#else
              cn1.ConnectionString =
string.Format("Version=3,URI=file://{0},FailIfMissing=True,Page Size=32768", sourceFile);
#endif

                cn1.Open();
                if (cn1.State == System.Data.ConnectionState.Open)
                {
                    using var cn2 = new SQLiteConnection();
#if !MONO
                    cn2.ConnectionString = string.Format("Data Source=\"{0}\";Page Size=32768", destFile);
#else
                    cn2.ConnectionString =
string.Format("Version=3,URI=file://{0},FailIfMissing=True,Page Size=32768", destFile);
#endif
                    cn2.Open();
                    if (cn2.State == System.Data.ConnectionState.Open)
                    {
                        using var cmd = new SQLiteCommand(
                            string.Format("ATTACH DATABASE \"{0}\" AS Source", sourceFile),
                            cn2);
                        cmd.ExecuteNonQuery();

                        using var tr = cn2.BeginTransaction();
                        try
                        {
                            var add = new List<long>();
                            using var cmd1 = new SQLiteCommand("SELECT id, X, Y, Zoom, Type FROM Tiles;", cn1);
                            using var rd = cmd1.ExecuteReader();
                            while (rd.Read())
                            {
                                long id = rd.GetInt64(0);
                                using var cmd2 =
                                    new SQLiteCommand(
                                        string.Format(
                                            "SELECT id FROM Tiles WHERE X={0} AND Y={1} AND Zoom={2} AND Type={3};",
                                            rd.GetInt32(1),
                                            rd.GetInt32(2),
                                            rd.GetInt32(3),
                                            rd.GetInt32(4)),
                                        cn2);
                                using var rd2 = cmd2.ExecuteReader();
                                if (!rd2.Read())
                                {
                                    add.Add(id);
                                }
                            }

                            foreach (long id in add)
                            {
                                using var cmd3 = new SQLiteCommand(string.Format(
                                        "INSERT INTO Tiles(X, Y, Zoom, Type, CacheTime) SELECT X, Y, Zoom, Type, CacheTime FROM Source.Tiles WHERE id={0}; INSERT INTO TilesData(id, Tile) Values((SELECT last_insert_rowid()), (SELECT Tile FROM Source.TilesData WHERE id={0}));",
                                        id),
                                    cn2);
                                cmd3.Transaction = tr;
                                cmd3.ExecuteNonQuery();
                            }

                            add.Clear();

                            tr.Commit();
                        }
                        catch (Exception exx)
                        {
                            Debug.WriteLine("ExportMapDataToDB: " + exx.ToString());
                            tr.Rollback();
                            ret = false;
                        }

                        using var cmd4 = new SQLiteCommand("DETACH DATABASE Source;", cn2);
                        cmd4.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("ExportMapDataToDB: " + ex.ToString());
            ret = false;
        }

        return ret;
    }

    #endregion

    static readonly string m_SingleSqlSelect =
        "SELECT Tile FROM main.TilesData WHERE id = (SELECT id FROM main.Tiles WHERE X={0} AND Y={1} AND Zoom={2} AND Type={3})";

    static readonly string m_SingleSqlInsert =
        "INSERT INTO main.Tiles(X, Y, Zoom, Type, CacheTime) VALUES(@p1, @p2, @p3, @p4, @p5)";

    static readonly string m_SingleSqlInsertLast =
        "INSERT INTO main.TilesData(id, Tile) VALUES((SELECT last_insert_rowid()), @p1)";

    string m_ConnectionString;

    readonly List<string> m_AttachedCaches = [];
    string m_FinalSqlSelect = m_SingleSqlSelect;
    string m_AttachSqlQuery = string.Empty;
    string m_DetachSqlQuery = string.Empty;

    /// <summary>
    /// Rebuilds the final SQL SELECT query and associated attach/detach SQL commands for accessing data from multiple
    /// attached databases.
    /// </summary>
    /// <remarks>This method constructs the final SQL SELECT query by combining the primary query with additional UNION
    /// SELECT statements for each attached database. It also generates the SQL commands required to attach and detach
    /// the databases. The method resets the existing queries and rebuilds them based on the current state of the
    /// attached caches.</remarks>
    void RebuildFinalSelect()
    {
        m_FinalSqlSelect = null;
        m_FinalSqlSelect = m_SingleSqlSelect;

        m_AttachSqlQuery = null;
        m_AttachSqlQuery = string.Empty;

        m_DetachSqlQuery = null;
        m_DetachSqlQuery = string.Empty;

        int i = 1;

        foreach (string c in m_AttachedCaches)
        {
            m_FinalSqlSelect +=
                string.Format(
                    "\nUNION SELECT Tile FROM db{0}.TilesData WHERE id = (SELECT id FROM db{0}.Tiles WHERE X={{0}} AND Y={{1}} AND Zoom={{2}} AND Type={{3}})",
                    i);
            m_AttachSqlQuery += string.Format("\nATTACH '{0}' as db{1};", c, i);
            m_DetachSqlQuery += string.Format("\nDETACH DATABASE db{0};", i);

            i++;
        }
    }

    public void Attach(string db)
    {
        if (!m_AttachedCaches.Contains(db))
        {
            m_AttachedCaches.Add(db);
            RebuildFinalSelect();
        }
    }

    public void Detach(string db)
    {
        if (m_AttachedCaches.Remove(db))
        {
            RebuildFinalSelect();
        }
    }

    #region PureImageCache Members

    int m_PreAllocationPing;

    bool IPureImageCache.PutImageToCache(byte[] tile, int type, GPoint pos, int zoom)
    {
        bool ret = true;

        if (m_Created)
        {
            try
            {
                using var cn = new SQLiteConnection();
                cn.ConnectionString = m_ConnectionString;
                cn.Open();
                {
                    using var tr = cn.BeginTransaction();
                    try
                    {
                        using var cmd1 = cn.CreateCommand();
                        cmd1.Transaction = tr;
                        cmd1.CommandText = m_SingleSqlInsert;

                        cmd1.Parameters.Add(new SQLiteParameter("@p1", pos.X));
                        cmd1.Parameters.Add(new SQLiteParameter("@p2", pos.Y));
                        cmd1.Parameters.Add(new SQLiteParameter("@p3", zoom));
                        cmd1.Parameters.Add(new SQLiteParameter("@p4", type));
                        cmd1.Parameters.Add(new SQLiteParameter("@p5", DateTime.Now));

                        cmd1.ExecuteNonQuery();

                        using var cmd2 = cn.CreateCommand();
                        cmd2.Transaction = tr;

                        cmd2.CommandText = m_SingleSqlInsertLast;
                        cmd2.Parameters.Add(new SQLiteParameter("@p1", tile));

                        cmd2.ExecuteNonQuery();

                        tr.Commit();
                    }
                    catch (Exception ex)
                    {
#if MONO
                        Console.WriteLine("PutImageToCache: " + ex.ToString());
#endif
                        Debug.WriteLine("PutImageToCache: " + ex.ToString());

                        tr.Rollback();
                        ret = false;
                    }
                }
                cn.Close();

                if (Interlocked.Increment(ref m_PreAllocationPing) % 22 == 0)
                {
                    CheckPreAllocation();
                }
            }
            catch (Exception ex)
            {
#if MONO
            Console.WriteLine("PutImageToCache: " + ex.ToString());
#endif
                Debug.WriteLine("PutImageToCache: " + ex.ToString());
                ret = false;
            }
        }

        return ret;
    }

    PureImage IPureImageCache.GetImageFromCache(int type, GPoint pos, int zoom)
    {
        PureImage ret = null;
        try
        {
            using var cn = new SQLiteConnection();
            cn.ConnectionString = m_ConnectionString;
            cn.Open();
            {
                if (!string.IsNullOrEmpty(m_AttachSqlQuery))
                {
                    using var com = cn.CreateCommand();
                    com.CommandText = m_AttachSqlQuery;
                    int x = com.ExecuteNonQuery();
                    //Debug.WriteLine("Attach: " + x);                         
                }

                using var cmd1 = cn.CreateCommand();
                cmd1.CommandText = string.Format(m_FinalSqlSelect, pos.X, pos.Y, zoom, type);

                using var rd = cmd1.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
                if (rd.Read())
                {
                    long length = rd.GetBytes(0, 0, null, 0, 0);
                    byte[] tile = new byte[length];
                    rd.GetBytes(0, 0, tile, 0, tile.Length);
                    {
                        if (GMapProvider.m_TileImageProxy != null)
                        {
                            ret = GMapProvider.m_TileImageProxy.FromArray(tile);
                        }
                    }
                }

                rd.Close();

                if (!string.IsNullOrEmpty(m_DetachSqlQuery))
                {
                    using var cmd2 = cn.CreateCommand();
                    cmd2.CommandText = m_DetachSqlQuery;
                    int x = cmd2.ExecuteNonQuery();
                    //Debug.WriteLine("Detach: " + x);
                }
            }
            cn.Close();
        }
        catch (Exception ex)
        {
#if MONO
        Console.WriteLine("GetImageFromCache: " + ex.ToString());
#endif
            Debug.WriteLine("GetImageFromCache: " + ex.ToString());
            ret = null;
        }

        return ret;
    }

    int IPureImageCache.DeleteOlderThan(DateTime date, int? type)
    {
        int affectedRows = 0;

        try
        {
            using var cn = new SQLiteConnection();
            cn.ConnectionString = m_ConnectionString;
            cn.Open();
            {
                using var com = cn.CreateCommand();
                com.CommandText =
                    string.Format(
                        "DELETE FROM Tiles WHERE CacheTime is not NULL and CacheTime < datetime('{0}')",
                        date.ToString("s"));
                if (type.HasValue)
                {
                    com.CommandText += " and Type = " + type;
                }

                affectedRows = com.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
#if MONO
        Console.WriteLine("DeleteOlderThan: " + ex);
#endif
            Debug.WriteLine("DeleteOlderThan: " + ex);
        }

        return affectedRows;
    }

    #endregion
}
#endif
