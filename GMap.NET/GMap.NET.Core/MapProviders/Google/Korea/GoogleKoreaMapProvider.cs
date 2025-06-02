using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Google.Korea;

/// <summary>
///     GoogleKoreaMap provider
/// </summary>
public class GoogleKoreaMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleKoreaMapProvider Instance;

    GoogleKoreaMapProvider()
    {
        Area = new RectLatLng(38.6597777307125, 125.738525390625, 4.02099609375, 4.42072406219614);
    }

    static GoogleKoreaMapProvider()
    {
        Instance = new GoogleKoreaMapProvider();
    }

    public string Version = "kr1.12";

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("0079D360-CB1B-4986-93D5-AD299C8E20E6");

    public override string Name { get; } = "GoogleKoreaMap";

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
