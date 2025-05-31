using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_Imagery_World_2D_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_Imagery_World_2D_MapProvider : ArcGISMapPlateCarreeProviderBase
{
    public static readonly ArcGIS_Imagery_World_2D_MapProvider Instance;

    ArcGIS_Imagery_World_2D_MapProvider()
    {
    }

    static ArcGIS_Imagery_World_2D_MapProvider()
    {
        Instance = new ArcGIS_Imagery_World_2D_MapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; protected set; } = new Guid("FF7ADDAD-F155-41DB-BC42-CC6FD97C8B9D");

    public override string Name { get; } = "ArcGIS_Imagery_World_2D_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_Imagery_World_2D/MapServer/tile/1/0/1.jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_Imagery_World_2D/MapServer/tile/{0}/{1}/{2}";
}
