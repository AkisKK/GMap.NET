﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Lithuania;

/// <summary>
///     LithuaniaOrtoFotoNewMap, from 2010 data, provider
/// </summary>
public class LithuaniaOrtoFotoOldMapProvider : LithuaniaMapProviderBase
{
    public static readonly LithuaniaOrtoFotoOldMapProvider Instance;

    LithuaniaOrtoFotoOldMapProvider()
    {
    }

    static LithuaniaOrtoFotoOldMapProvider()
    {
        Instance = new LithuaniaOrtoFotoOldMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("C37A148E-0A7D-4123-BE4E-D0D3603BE46B");

    public override string Name { get; } = "LithuaniaOrtoFotoMapOld";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://dc1.maps.lt/cache/mapslt_ortofoto_2010/map/_alllayers/L09/R000016b1/C000020e2.jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://dc1.maps.lt/cache/mapslt_ortofoto_2010/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.jpg";
}
