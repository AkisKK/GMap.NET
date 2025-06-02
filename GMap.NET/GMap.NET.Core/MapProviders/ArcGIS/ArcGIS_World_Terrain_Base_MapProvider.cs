using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_World_Terrain_Base_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_World_Terrain_Base_MapProvider : ArcGISMapMercatorProviderBase
{
    public static readonly ArcGIS_World_Terrain_Base_MapProvider Instance;

    ArcGIS_World_Terrain_Base_MapProvider()
    {
    }

    static ArcGIS_World_Terrain_Base_MapProvider()
    {
        Instance = new ArcGIS_World_Terrain_Base_MapProvider();
    }

    #region GMapProvider Members

    public override Guid Id
    {
        get; protected set;
    } = new Guid("927F175B-5200-4D95-A99B-1C87C93099DA");

    public override string Name
    {
        get;
    } = "ArcGIS_World_Terrain_Base_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Terrain_Base/MapServer/tile/0/0/0jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/World_Terrain_Base/MapServer/tile/{0}/{1}/{2}";
}
