﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Yandex;

/// <summary>
///     YandexHybridMap provider
/// </summary>
public class YandexHybridMapProvider : YandexMapProviderBase
{
    public static readonly YandexHybridMapProvider Instance;

    YandexHybridMapProvider()
    {
    }

    static YandexHybridMapProvider()
    {
        Instance = new YandexHybridMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("78A3830F-5EE3-432C-A32E-91B7AF6BBCB9");

    public override string Name { get; } = "YandexHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [YandexSatelliteMapProvider.Instance, this];

            return m_Overlays;
        }
    }

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

    static readonly string m_UrlServer = "vec";
    static readonly string m_UrlFormat = "http://{0}0{1}.{7}/tiles?l=skl&v={2}&x={3}&y={4}&z={5}&lang={6}";
}
