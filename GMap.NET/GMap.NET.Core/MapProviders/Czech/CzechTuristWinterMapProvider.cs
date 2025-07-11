﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Czech;

/// <summary>
///     CzechTuristMap provider, http://www.mapy.cz/
/// </summary>
public class CzechTuristWinterMapProvider : CzechMapProviderBase
{
    public static readonly CzechTuristWinterMapProvider Instance;

    CzechTuristWinterMapProvider()
    {
    }

    static CzechTuristWinterMapProvider()
    {
        Instance = new CzechTuristWinterMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("F7B7FC9E-BDC2-4A9D-A1D3-A6BEC8FE0EB2");

    public override string Name { get; } = "CzechTuristWinterMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://m3.mapserver.mapy.cz/wturist_winter-m/14-8802-5528

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/wturist_winter-m/{1}-{2}-{3}";
}
