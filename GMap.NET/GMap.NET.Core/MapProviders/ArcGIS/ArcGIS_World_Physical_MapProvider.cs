using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_World_Physical_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_World_Physical_MapProvider : ArcGISMapMercatorProviderBase
{
    public static readonly ArcGIS_World_Physical_MapProvider Instance;

    ArcGIS_World_Physical_MapProvider()
    {
    }

    static ArcGIS_World_Physical_MapProvider()
    {
        Instance = new ArcGIS_World_Physical_MapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("0C0E73E3-5EA6-4F08-901C-AE85BCB1BFC8");

    public override string Name { get; } = "ArcGIS_World_Physical_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Physical_Map/MapServer/tile/2/0/2.jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/World_Physical_Map/MapServer/tile/{0}/{1}/{2}";
}
