﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_ShadedRelief_World_2D_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_ShadedRelief_World_2D_MapProvider : ArcGISMapPlateCarreeProviderBase
{
    public static readonly ArcGIS_ShadedRelief_World_2D_MapProvider Instance;

    ArcGIS_ShadedRelief_World_2D_MapProvider()
    {
    }

    static ArcGIS_ShadedRelief_World_2D_MapProvider()
    {
        Instance = new ArcGIS_ShadedRelief_World_2D_MapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; protected set; } = new Guid("A8995FA4-D9D8-415B-87D0-51A7E53A90D4");

    public override string Name { get; } = "ArcGIS_ShadedRelief_World_2D_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_ShadedRelief_World_2D/MapServer/tile/1/0/1.jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_ShadedRelief_World_2D/MapServer/tile/{0}/{1}/{2}";
}
