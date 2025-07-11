﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Here;

/// <summary>
///     HereHybridMap provider
/// </summary>
public class HereHybridMapProvider : HereMapProviderBase
{
    public static readonly HereHybridMapProvider Instance;

    HereHybridMapProvider()
    {
    }

    static HereHybridMapProvider()
    {
        Instance = new HereHybridMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("B85A8FD7-40F4-40EE-9B45-491AA45D86C1");

    public override string Name { get; } = "HereHybridMap";

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
        "http://{0}.traffic.maps.cit.api.here.com/maptile/2.1/traffictile/newest/hybrid.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
}
