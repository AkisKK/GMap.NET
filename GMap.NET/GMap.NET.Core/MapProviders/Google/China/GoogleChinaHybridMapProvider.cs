using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Google.China;

/// <summary>
///     GoogleChinaHybridMap provider
/// </summary>
public class GoogleChinaHybridMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleChinaHybridMapProvider Instance;

    GoogleChinaHybridMapProvider()
    {
        ReferrerUrl = string.Format("http://ditu.{0}/", ServerChina);
    }

    static GoogleChinaHybridMapProvider()
    {
        Instance = new GoogleChinaHybridMapProvider();
    }

    public string Version = "h@298";

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("B8A2A78D-1C49-45D0-8F03-9B95C83116B7");

    public override string Name { get; } = "GoogleChinaHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [GoogleChinaSatelliteMapProvider.Instance, this];

            return m_Overlays;
        }
    }

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // sec1: after &x=...
        // sec2: after &zoom=...
        GetSecureWords(pos, out string sec1, out string sec2);

        return string.Format(m_UrlFormat,
            m_UrlFormatServer,
            GetServerNum(pos, 4),
            m_UrlFormatRequest,
            Version,
            m_ChinaLanguage,
            pos.X,
            sec1,
            pos.Y,
            zoom,
            sec2,
            ServerChina);
    }

    static readonly string m_ChinaLanguage = "zh-CN";
    static readonly string m_UrlFormatServer = "mt";
    static readonly string m_UrlFormatRequest = "vt";

    static readonly string m_UrlFormat =
        "http://{0}{1}.{10}/{2}/imgtp=png32&lyrs={3}&hl={4}&gl=cn&x={5}{6}&y={7}&z={8}&s={9}";
}
