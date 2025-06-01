using System;
using System.Collections.Generic;
using System.IO;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Etc;

#if SQLite && !MONO

/// <summary>Map provider for MBTiles files (https://github.com/mapbox/mbtiles-spec/blob/master/1.3/spec.md)</summary>
/// <remarks>Sample files are available at https://ftp.gwdg.de/pub/misc/openstreetmap/openseamap/charts/mbtiles/.</remarks>
public class MBTilesMapProvider : GMapProvider, IDisposable
{
    private class MBTiles : IDisposable
    {
        private System.Data.SQLite.SQLiteConnection m_Db = null;
        public Dictionary<string, string> metadata = [];

        public MBTiles(string file)
        {
            m_Db = new System.Data.SQLite.SQLiteConnection(string.Format("Data Source=\"{0}\";Pooling=True", file));
            m_Db.Open();
            using var cmd = new System.Data.SQLite.SQLiteCommand("SELECT * FROM metadata;", m_Db);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                metadata[rd.GetString(0)] = rd.GetString(1);
            }
        }

        /// <summary>
        /// Retrieves a tile image from the database for the specified position and zoom level.
        /// </summary>
        /// <remarks>The method queries a SQLite database to retrieve the tile image data for the specified position and
        /// zoom level. The tile data is converted into a <see cref="PureImage"/> object using the configured tile image
        /// proxy. If no tile is found in the database, the method returns <see langword="null"/>.</remarks>
        /// <param name="position">The position of the tile, represented as a <see cref="GPoint"/> object, where
        /// <c>X</c> is the column and <c>Y</c> is the row.</param>
        /// <param name="zoom">The zoom level of the tile. Must be a non-negative integer.</param>
        /// <returns>A <see cref="PureImage"/> object representing the tile image, or <see langword="null"/> if no tile
        /// is found for the specified position and zoom level.</returns>
        public PureImage GetImage(GPoint position, int zoom)
        {
            PureImage ret = null;
            using (var cmd = new System.Data.SQLite.SQLiteCommand(string.Format("SELECT tile_data FROM tiles WHERE zoom_level={2} AND tile_column={0} AND tile_row={1} LIMIT 1", position.X, (long)Math.Pow(2, zoom) - 1 - position.Y, zoom), m_Db))
            {
                using var rd = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
                if (rd.Read())
                {
                    long length = rd.GetBytes(0, 0, null, 0, 0);
                    byte[] tile = new byte[length];
                    rd.GetBytes(0, 0, tile, 0, tile.Length);
                    {
                        if (m_TileImageProxy != null)
                        {
                            ret = m_TileImageProxy.FromArray(tile);
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Retrieves the minimum zoom level available in the tiles database by querying the "tiles" table.
        /// </summary>
        /// <remarks>This method queries the database for the minimum zoom level stored in the "tiles" table. If the
        /// query fails or no data is available, the method returns <c>-1</c>.</remarks>
        /// <returns>The minimum zoom level as an integer if the query succeeds; otherwise, <c>-1</c>.</returns>
        public int GetZoomMin()
        {
            try
            {
                using var cmd = new System.Data.SQLite.SQLiteCommand("SELECT MIN(zoom_level) AS zoom FROM tiles;", m_Db);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    return rd.GetInt32(0);
                }
            }
            catch { }
            return -1;
        }

        /// <summary>
        /// Retrieves the maximum zoom level available in the tiles database by querying the "tiles" table.
        /// </summary>
        /// <remarks>This method queries the database for the highest zoom level in the "tiles" table. If the query
        /// fails or no data is available, the method returns <c>-1</c>.</remarks>
        /// <returns>The maximum zoom level as an integer if the query succeeds; otherwise, <c>-1</c>.</returns>
        public int GetZoomMax()
        {
            try
            {
                using var cmd = new System.Data.SQLite.SQLiteCommand("SELECT MAX(zoom_level) AS zoom FROM tiles;", m_Db);
                using var rd = cmd.ExecuteReader();
                if (rd.Read())
                {
                    return rd.GetInt32(0);
                }
            }
            catch { }
            return -1;
        }

        #region IDisposable implementation
        private bool m_DisposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!m_DisposedValue)
            {
                if (disposing)
                {
                    if (m_Db != null)
                    {
                        m_Db.Close();
                        m_Db = null;
                    }
                }

                m_DisposedValue = true;
            }
        }

        ~MBTiles()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    private MBTiles m_Source = null;

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="MBTilesMapProvider"/> class with the specified name, identifier,
    /// and MBTiles file path.
    /// </summary>
    /// <remarks>The constructor initializes the map provider and opens the specified MBTiles file for use. Ensure that
    /// the file path provided is valid and accessible.</remarks>
    /// <param name="name">The name of the map provider.</param>
    /// <param name="id">The unique identifier for the map provider.</param>
    /// <param name="mbTilesFilePath">The file path to the MBTiles database. Must not be <see langword="null"/> or
    /// empty.</param>
    public MBTilesMapProvider(string name, Guid id, string mbTilesFilePath) : base(id)
    {
        Name = name;
        DataLoadedSuccessfully = Open(mbTilesFilePath);
    }
    #endregion

    #region Properties
    /// <summary>
    /// The human-readable name of the tileset.
    /// </summary>
    public string DataName { get; private set; }

    /// <summary>
    /// The file format of the tile data: pbf, jpg, png, webp, or an IETF media type for other formats.
    /// </summary>
    /// <remarks>pbf as a format refers to gzip-compressed vector tile data in Mapbox Vector Tile format.</remarks>
    public string Format { get; private set; }

    /// <summary>
    /// The maximum extent of the rendered map area. Bounds must define an area covered by all zoom levels. The bounds
    /// are represented as WGS 84 latitude and longitude values, in the OpenLayers Bounds format (left, bottom, right,
    /// top). For example, the bounds of the full Earth, minus the poles, would be: -180.0,-85,180,85.
    /// </summary>
    public PointLatLng[] Bounds { get; private set; }

    /// <summary>
    /// The longitude, latitude, and zoom level of the default view of the map. Example: -122.1906,37.7599,11.
    /// </summary>
    public PointLatLng CenterLocation { get; private set; }

    /// <summary>
    /// The longitude, latitude, and zoom level of the default view of the map. Example: -122.1906,37.7599,11.
    /// </summary>
    public int CenterZoom { get; private set; }

    /// <summary>
    /// The lowest zoom level for which the tileset provides data.
    /// </summary>
    public override int MinZoom { get; protected set; }

    /// <summary>
    /// The highest zoom level for which the tileset provides data.
    /// </summary>
    public override int? MaxZoom { get; protected set; }

    /// <summary>
    /// An attribution string, which explains the sources of data and/or style for the map.
    /// </summary>
    public string Attribution { get; private set; }

    /// <summary>
    /// A description of the tileset's content.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Overlay or baselayer
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// The version of the tileset. This refers to a revision of the tileset itself, not of the MBTiles specification.
    /// </summary>
    public string Version { get; private set; }

    /// <summary>
    /// The metadata table MAY contain additional rows for tilesets that implement UTFGrid-based interaction or for
    /// other purposes.
    /// </summary>
    public Dictionary<string, string> Metadata => m_Source != null ? m_Source.metadata : [];

    /// <summary>
    /// Gets a value indicating whether the data was loaded successfully.
    /// </summary>
    public bool DataLoadedSuccessfully { get; }
    #endregion

    #region GMapProvider Members
    /// <inheritdoc />
    public override Guid Id { get; protected set; }

    /// <inheritdoc />
    public override string Name { get; } = "MBTilesMapProvider";

    /// <summary>
    /// Backing field for the <see cref="Overlays"/> property.
    /// </summary>
    GMapProvider[] m_Overlays;

    /// <inheritdoc />
    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this];
            return m_Overlays;
        }
    }

    /// <inheritdoc />
    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        if (m_Source is null)
        {
            return null;
        }

        if (zoom < MinZoom || zoom > MaxZoom)
        {
            return null;
        }

        return m_Source.GetImage(pos, zoom);
    }

    /// <inheritdoc />
    public override PureProjection Projection => MercatorProjection.Instance;
    #endregion

    /// <summary>
    /// Opens an MBTiles file and initializes the metadata and configuration properties for the map provider.
    /// </summary>
    /// <remarks>This method attempts to load the specified MBTiles file and extract its metadata to configure the map
    /// provider. The metadata fields "name" and "format" are required for successful initialization. If the format is
    /// "pbf", the metadata must also include a "json" row. If any required metadata is missing or invalid, the method
    /// will fail.</remarks>
    /// <param name="mbTilesFilePath">The file path to the MBTiles file to be opened. The file must exist and be
    /// accessible.</param>
    /// <returns><see langword="true"/> if the MBTiles file was successfully opened and its metadata was loaded;
    /// otherwise, <see langword="false"/> if the file does not exist, is invalid, or required metadata is missing.
    /// </returns>
    public bool Open(string mbTilesFilePath)
    {
        if (!File.Exists(mbTilesFilePath))
        {
            return false;
        }

        try
        {
            m_Source = new MBTiles(mbTilesFilePath);
            DataName = string.Empty;
            Format = string.Empty;
            Bounds = null;
            CenterLocation = PointLatLng.Empty;
            CenterZoom = -1;
            MinZoom = -1;
            MaxZoom = -1;
            Attribution = string.Empty;
            Description = string.Empty;
            Type = string.Empty;
            Version = "0";
            foreach (var kvp in m_Source.metadata)
            {
                switch (kvp.Key.ToLower())
                {
                    case "name": DataName = kvp.Value; break;
                    case "format": Format = kvp.Value; break;
                    case "bounds":
                        string[] tmp1 = kvp.Value.Split(',');
                        if (tmp1.Length == 4)
                        {
                            Bounds =
                            [
                                new(double.Parse(tmp1[3], System.Globalization.CultureInfo.InvariantCulture),double.Parse(tmp1[0], System.Globalization.CultureInfo.InvariantCulture)),
                                new(double.Parse(tmp1[1], System.Globalization.CultureInfo.InvariantCulture), double.Parse(tmp1[2], System.Globalization.CultureInfo.InvariantCulture))
                            ];
                        }
                        break;
                    case "center":
                        string[] tmp2 = kvp.Value.Split(',');
                        if (tmp2.Length == 3)
                        {
                            CenterLocation = new PointLatLng(double.Parse(tmp2[1], System.Globalization.CultureInfo.InvariantCulture), double.Parse(tmp2[0], System.Globalization.CultureInfo.InvariantCulture));
                            CenterZoom = int.Parse(tmp2[2]);
                        }
                        break;
                    case "minzoom": MinZoom = int.Parse(kvp.Value); break;
                    case "maxzoom": MaxZoom = int.Parse(kvp.Value); break;
                    case "attribution": Attribution = kvp.Value; break;
                    case "description": Description = kvp.Value; break;
                    case "type": Type = kvp.Value; break;
                    case "version": Version = kvp.Value; break;
                    default: break;
                }
            }
            if (string.IsNullOrEmpty(DataName) || string.IsNullOrEmpty(Format))
            {
                m_Source = null;
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[MBTilesMapProvider] Metafields 'name' and 'format' are required!");
#endif
                return false;
            }
            if (Format.Equals("pbf", StringComparison.OrdinalIgnoreCase) && !Metadata.ContainsKey("json"))
            {
                m_Source = null;
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[MBTilesMapProvider] If the format is pbf, the metadata table MUST contain 'json' row!");
#endif
                return false;
            }
            if (MinZoom < 0)
            {
                MinZoom = m_Source.GetZoomMin();
            }

            if (MaxZoom < 0)
            {
                MaxZoom = m_Source.GetZoomMax();
            }

            if (Bounds != null && CenterLocation == PointLatLng.Empty)
            {
                CenterLocation = new PointLatLng(Bounds[0].Lat - 0.5 * (Bounds[1].Lat - Bounds[0].Lat), Bounds[0].Lng + 0.5 * (Bounds[1].Lng - Bounds[0].Lng));
            }

            if (CenterZoom < 0)
            {
                CenterZoom = MinZoom + (MaxZoom.Value - MinZoom) / 2;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    #region IDisposable implementation
    private bool m_DisposedValue;

    protected virtual void Dispose(bool disposing)
    {
        if (!m_DisposedValue)
        {
            if (disposing)
            {
                m_Source?.Dispose();
                m_MapProviders.Remove(this);
            }

            m_DisposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}

#endif
