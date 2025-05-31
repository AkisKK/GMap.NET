using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.CzechOld;

/// <summary>
///     CzechTuristMap provider, http://www.mapy.cz/
/// </summary>
public class CzechTuristMapProviderOld : CzechMapProviderBaseOld
{
    public static readonly CzechTuristMapProviderOld Instance;

    CzechTuristMapProviderOld()
    {
    }

    static CzechTuristMapProviderOld()
    {
        Instance = new CzechTuristMapProviderOld();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("B923C81D-880C-42EB-88AB-AF8FE42B564D");

    public override string Name { get; } = "CzechTuristOldMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://m1.mapserver.mapy.cz/turist/3_8000000_8000000

        long xx = pos.X << 28 - zoom;
        long yy = (long)Math.Pow(2.0, zoom) - 1 - pos.Y << 28 - zoom;

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, xx, yy);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/turist/{1}_{2:x7}_{3:x7}";
}
