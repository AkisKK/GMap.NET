﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Here;

/// <summary>
///     OviSatelliteMap provider
/// </summary>
public class HereSatelliteMapProvider : HereMapProviderBase
{
    public static readonly HereSatelliteMapProvider Instance;

    HereSatelliteMapProvider()
    {
    }

    static HereSatelliteMapProvider()
    {
        Instance = new HereSatelliteMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("6696CE15-7694-4073-BC48-79EE849F2563");

    public override string Name { get; } = "HereSatelliteMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom)
    {
        return string.Format(m_UrlFormat, UrlServerLetters[GetServerNum(pos, 4)], zoom, pos.X, pos.Y, AppId, AppCode);
    }

    static readonly string m_UrlFormat =
        "http://{0}.traffic.maps.cit.api.here.com/maptile/2.1/traffictile/newest/satellite.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
}
