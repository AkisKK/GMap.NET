using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_World_Shaded_Relief_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_World_Shaded_Relief_MapProvider : ArcGISMapMercatorProviderBase
{
    public static readonly ArcGIS_World_Shaded_Relief_MapProvider Instance;

    ArcGIS_World_Shaded_Relief_MapProvider()
    {
    }

    static ArcGIS_World_Shaded_Relief_MapProvider()
    {
        Instance = new ArcGIS_World_Shaded_Relief_MapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("2E821FEF-8EA1-458A-BC82-4F699F4DEE79");

    public override string Name { get; } = "ArcGIS_World_Shaded_Relief_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://services.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer/tile/0/0/0jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/World_Shaded_Relief/MapServer/tile/{0}/{1}/{2}";
}
