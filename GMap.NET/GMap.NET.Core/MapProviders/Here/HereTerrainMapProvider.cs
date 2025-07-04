﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Here;

/// <summary>
///     HereTerrainMap provider
/// </summary>
public class HereTerrainMapProvider : HereMapProviderBase
{
    public static readonly HereTerrainMapProvider Instance;

    HereTerrainMapProvider()
    {
    }

    static HereTerrainMapProvider()
    {
        Instance = new HereTerrainMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("7267338C-445E-4E61-B8B8-82D0B7AAACC5");

    public override string Name { get; } = "HereTerrainMap";

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
        "http://{0}.traffic.maps.cit.api.here.com/maptile/2.1/traffictile/newest/terrain.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
}
