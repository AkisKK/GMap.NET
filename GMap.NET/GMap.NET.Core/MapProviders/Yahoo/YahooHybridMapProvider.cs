﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Yahoo;

/// <summary>
///     YahooHybridMap provider
/// </summary>
public class YahooHybridMapProvider : YahooMapProviderBase
{
    public static readonly YahooHybridMapProvider Instance;

    YahooHybridMapProvider()
    {
    }

    static YahooHybridMapProvider()
    {
        Instance = new YahooHybridMapProvider();
    }

    public string Version = "4.3";

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("A084E0DB-F9A6-45C1-BC2F-791E1F4E958E");

    public override string Name { get; } = "YahooHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [YahooSatelliteMapProvider.Instance, this];

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
        // http://maps1.yimg.com/hx/tl?b=1&v=4.3&t=h&.intl=en&x=14&y=5&z=7&r=1

        return string.Format(m_UrlFormat,
                             GetServerNum(pos, 2) + 1,
                             Version,
                             language,
                             pos.X,
                             (1 << zoom >> 1) - 1 - pos.Y,
                             zoom + 1);
    }

    static readonly string m_UrlFormat = "http://maps{0}.yimg.com/hx/tl?v={1}&t=h&.intl={2}&x={3}&y={4}&z={5}&r=1";
}
