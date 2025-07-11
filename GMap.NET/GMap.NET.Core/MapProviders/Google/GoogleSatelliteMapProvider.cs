﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Google;

/// <summary>
///     GoogleSatelliteMap provider
/// </summary>
public class GoogleSatelliteMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleSatelliteMapProvider Instance;

    GoogleSatelliteMapProvider()
    {
    }

    static GoogleSatelliteMapProvider()
    {
        Instance = new GoogleSatelliteMapProvider();
    }

    public string Version = "192";

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("9CB89D76-67E9-47CF-8137-B9EE9FC46388");

    public override string Name { get; } = "GoogleSatelliteMap";

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

    static readonly string m_UrlFormatServer = "khm";
    static readonly string m_UrlFormatRequest = "kh";
    static readonly string m_UrlFormat = "https://{0}{1}.{10}/{2}/v={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}";
}
