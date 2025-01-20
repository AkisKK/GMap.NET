using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Google.Korea;

/// <summary>
///     GoogleKoreaHybridMap provider
/// </summary>
public class GoogleKoreaHybridMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleKoreaHybridMapProvider Instance;

    GoogleKoreaHybridMapProvider()
    {
    }

    static GoogleKoreaHybridMapProvider()
    {
        Instance = new GoogleKoreaHybridMapProvider();
    }

    public string Version = "kr1t.12";

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("41A91842-04BC-442B-9AC8-042156238A5B");

    public override string Name { get; } = "GoogleKoreaHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [GoogleKoreaSatelliteMapProvider.Instance, this];

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
            ServerKorea);
    }

    static readonly string m_UrlFormatServer = "mt";
    static readonly string m_UrlFormatRequest = "mt";
    static readonly string m_UrlFormat = "https://{0}{1}.{10}/{2}/v={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}";
}
