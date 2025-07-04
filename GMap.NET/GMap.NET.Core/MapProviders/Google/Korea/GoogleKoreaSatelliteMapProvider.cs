﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Google.Korea;

/// <summary>
///     GoogleKoreaSatelliteMap provider
/// </summary>
public class GoogleKoreaSatelliteMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleKoreaSatelliteMapProvider Instance;

    GoogleKoreaSatelliteMapProvider()
    {
    }

    static GoogleKoreaSatelliteMapProvider()
    {
        Instance = new GoogleKoreaSatelliteMapProvider();
    }

    public string Version = "170";

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("70370941-D70C-4123-BE4A-AEE6754047F5");

    public override string Name { get; } = "GoogleKoreaSatelliteMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom, LanguageStr);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom, string language)
    {
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
            ServerKoreaKr);
    }

    static readonly string m_UrlFormatServer = "khm";
    static readonly string m_UrlFormatRequest = "kh";
    static readonly string m_UrlFormat = "https://{0}{1}.{10}/{2}/v={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}";
}
