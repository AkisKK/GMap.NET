﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.NearMap;

/// <summary>
///     NearSatelliteMap provider - http://www.nearmap.com/
/// </summary>
public class NearSatelliteMapProvider : NearMapProviderBase
{
    public static readonly NearSatelliteMapProvider Instance;

    NearSatelliteMapProvider()
    {
    }

    static NearSatelliteMapProvider()
    {
        Instance = new NearSatelliteMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("56D00148-05B7-408D-8F7A-8D7250FF8121");

    public override string Name { get; } = "NearSatelliteMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://web2.nearmap.com/maps/hl=en&x=14&y=8&z=5&nml=Vert&s=kdj00
        // http://web2.nearmap.com/maps/hl=en&x=6&y=4&z=4&nml=Vert
        // http://web2.nearmap.com/maps/hl=en&x=3&y=1&z=3&nml=Vert&s=2edd
        // http://web0.nearmap.com/maps/hl=en&x=69&y=39&z=7&nml=Vert&s=z80wiTM

        return string.Format(m_UrlFormat, GetServerNum(pos, 4), pos.X, pos.Y, zoom, GetSafeString(pos));
    }

    static readonly string m_UrlFormat = "http://web{0}.nearmap.com/maps/hl=en&x={1}&y={2}&z={3}&nml=Vert{4}";
}
