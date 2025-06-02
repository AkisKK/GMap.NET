using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Czech;

/// <summary>
///     CzechTuristMap provider, http://www.mapy.cz/
/// </summary>
public class CzechTuristMapProvider : CzechMapProviderBase
{
    public static readonly CzechTuristMapProvider Instance;

    CzechTuristMapProvider()
    {
    }

    static CzechTuristMapProvider()
    {
        Instance = new CzechTuristMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("102A54BE-3894-439B-9C1F-CA6FF2EA1FE9");

    public override string Name { get; } = "CzechTuristMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://m3.mapserver.mapy.cz/wtourist-m/14-8802-5528

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "https://mapserver.mapy.cz/turist-m/{1}-{2}-{3}";
}
