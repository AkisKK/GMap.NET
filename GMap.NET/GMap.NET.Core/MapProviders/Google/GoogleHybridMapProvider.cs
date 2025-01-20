using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Google;

/// <summary>
///     GoogleHybridMap provider
/// </summary>
public class GoogleHybridMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleHybridMapProvider Instance;

    GoogleHybridMapProvider()
    {
    }

    static GoogleHybridMapProvider()
    {
        Instance = new GoogleHybridMapProvider();
    }

    public string Version = "h@333000000";

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("B076C255-6D12-4466-AAE0-4A73D20A7E6A");

    public override string Name { get; } = "GoogleHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [GoogleSatelliteMapProvider.Instance, this];

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
    static readonly string m_UrlFormat = "http://{0}{1}.{10}/maps/{2}/lyrs={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}";
}
