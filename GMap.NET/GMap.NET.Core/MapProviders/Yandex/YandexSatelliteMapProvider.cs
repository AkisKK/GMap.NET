﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Yandex;

/// <summary>
///     YandexSatelliteMap provider
/// </summary>
public class YandexSatelliteMapProvider : YandexMapProviderBase
{
    public static readonly YandexSatelliteMapProvider Instance;

    YandexSatelliteMapProvider()
    {
    }

    static YandexSatelliteMapProvider()
    {
        Instance = new YandexSatelliteMapProvider();
    }

    public new string Version = "3.135.0";

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("2D4CE763-0F91-40B2-A511-13EF428237AD");

    public override string Name { get; } = "YandexSatelliteMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom, LanguageStr);

        return GetTileImageUsingHttp(url);
    }
    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom, string language)
    {
        return string.Format(m_UrlFormat,
                             m_UrlServer,
                             GetServerNum(pos, 4) + 1,
                             Version,
                             pos.X,
                             pos.Y,
                             zoom,
                             language,
                             Server);
    }

    static readonly string m_UrlServer = "sat";
    static readonly string m_UrlFormat = "http://{0}0{1}.{7}/tiles?l=sat&v={2}&x={3}&y={4}&z={5}&lang={6}";
}
