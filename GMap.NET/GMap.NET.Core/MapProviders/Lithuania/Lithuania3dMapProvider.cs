﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Lithuania;

/// <summary>
///     Lithuania3dMap (2.5d) provider
/// </summary>
public class Lithuania3dMapProvider : LithuaniaMapProviderBase
{
    public static readonly Lithuania3dMapProvider Instance;

    Lithuania3dMapProvider()
    {
    }

    static Lithuania3dMapProvider()
    {
        Instance = new Lithuania3dMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("CCC5B65F-C8BC-47CE-B39D-5E262E6BF083");

    public override string Name { get; } = "Lithuania 2.5d Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://dc1.maps.lt/cache/mapslt_25d_vkkp/map/_alllayers/L01/R00007194/C0000a481.png
        int z = zoom;
        if (zoom >= 10)
        {
            z -= 10;
        }

        return string.Format(m_UrlFormat, z, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://dc1.maps.lt/cache/mapslt_25d_vkkp/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.png";
}
