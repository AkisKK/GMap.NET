using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Czech;

/// <summary>
///     CzechTuristMap provider, http://www.mapy.cz/
/// </summary>
public class CzechGeographicMapProvider : CzechMapProviderBase
{
    public static readonly CzechGeographicMapProvider Instance;

    CzechGeographicMapProvider()
    {
    }

    static CzechGeographicMapProvider()
    {
        Instance = new CzechGeographicMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("50EC9FCC-E4D7-4F53-8700-2D1DB73A1D48");

    public override string Name { get; } = "CzechGeographicMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://m3.mapserver.mapy.czzemepis-m/14-8802-5528

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/zemepis-m/{1}-{2}-{3}";
}
