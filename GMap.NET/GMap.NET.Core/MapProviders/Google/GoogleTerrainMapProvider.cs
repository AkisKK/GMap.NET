﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Google;

/// <summary>
///     GoogleTerrainMap provider
/// </summary>
public class GoogleTerrainMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleTerrainMapProvider Instance;

    GoogleTerrainMapProvider()
    {
    }

    static GoogleTerrainMapProvider()
    {
        Instance = new GoogleTerrainMapProvider();
    }

    public string Version = "t@132,r@333000000";

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("A42EDF2E-63C5-4967-9DBF-4EFB3AF7BC11");

    public override string Name { get; } = "GoogleTerrainMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom, LanguageStr);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom, string language)
    {
        // sec1: after &x=...
        // sec2: after &zoom=...
        GetSecureWords(pos, out string sec1, out string sec2);

        return string.Format(m_UrlFormat,
            m_UrlFormatServer,
            GetServerNum(pos, 4),
            m_UrlFormatRequest,
            Version,
            language,
            pos.X,
            sec1,
            pos.Y,
            zoom,
            sec2,
            Server);
    }

    static readonly string m_UrlFormatServer = "mt";
    static readonly string m_UrlFormatRequest = "vt";
    static readonly string m_UrlFormat = "https://{0}{1}.{10}/maps/{2}/lyrs={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}";
}
