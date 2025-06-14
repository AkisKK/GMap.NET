﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_Topo_US_2D_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_Topo_US_2D_MapProvider : ArcGISMapPlateCarreeProviderBase
{
    public static readonly ArcGIS_Topo_US_2D_MapProvider Instance;

    ArcGIS_Topo_US_2D_MapProvider()
    {
    }

    static ArcGIS_Topo_US_2D_MapProvider()
    {
        Instance = new ArcGIS_Topo_US_2D_MapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("7652CC72-5C92-40F5-B572-B8FEAA728F6D");

    public override string Name { get; } = "ArcGIS_Topo_US_2D_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://server.arcgisonline.com/ArcGIS/rest/services/NGS_Topo_US_2D/MapServer/tile/4/3/15

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/NGS_Topo_US_2D/MapServer/tile/{0}/{1}/{2}";
}
