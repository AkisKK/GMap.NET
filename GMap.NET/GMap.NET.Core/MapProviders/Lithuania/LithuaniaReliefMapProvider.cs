﻿using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Lithuania;

/// <summary>
///     LithuaniaReliefMap provider, http://www.maps.lt/map/
/// </summary>
public class LithuaniaReliefMapProvider : LithuaniaMapProviderBase
{
    public static readonly LithuaniaReliefMapProvider Instance;

    LithuaniaReliefMapProvider()
    {
    }

    static LithuaniaReliefMapProvider()
    {
        Instance = new LithuaniaReliefMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("85F89205-1062-4F10-B536-90CD8B2F1B7D");

    public override string Name { get; } = "LithuaniaReliefMap";

    public override PureProjection Projection => LKS94rProjection.Instance;

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://dc5.maps.lt/cache/mapslt_relief_vector/map/_alllayers/L09/R00001892/C000020df.jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://dc5.maps.lt/cache/mapslt_relief_vector/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.png";
}
