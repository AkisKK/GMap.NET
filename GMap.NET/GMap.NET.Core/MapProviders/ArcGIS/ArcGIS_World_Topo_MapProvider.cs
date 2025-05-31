using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_World_Topo_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_World_Topo_MapProvider : ArcGISMapMercatorProviderBase
{
    public static readonly ArcGIS_World_Topo_MapProvider Instance;

    ArcGIS_World_Topo_MapProvider()
    {
    }

    static ArcGIS_World_Topo_MapProvider()
    {
        Instance = new ArcGIS_World_Topo_MapProvider();
    }

    #region GMapProvider Members

    public override Guid Id
    {
        get; protected set;
    } = new Guid("E0354A49-7447-4C9A-814F-A68565ED834B");

    public override string Name
    {
        get;
    } = "ArcGIS_World_Topo_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/0/0/0jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{0}/{1}/{2}";
}
