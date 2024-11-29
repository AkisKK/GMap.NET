using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.CzechOld;

/// <summary>
///     CzechHybridMap provider, http://www.mapy.cz/
/// </summary>
public class CzechHybridMapProviderOld : CzechMapProviderBaseOld
{
    public static readonly CzechHybridMapProviderOld Instance;

    CzechHybridMapProviderOld()
    {
    }

    static CzechHybridMapProviderOld()
    {
        Instance = new CzechHybridMapProviderOld();
    }

    #region GMapProvider Members

    public override Guid Id    {        get;    } = new Guid("F785D98E-DD1D-46FD-8BC1-1AAB69604980");

    public override string Name    {        get;    } = "CzechHybridOldMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [CzechSatelliteMapProviderOld.Instance, this];

            return m_Overlays;
        }
    }

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://m2.mapserver.mapy.cz/hybrid/9_7d00000_7b80000

        long xx = pos.X << 28 - zoom;
        long yy = (long)Math.Pow(2.0, zoom) - 1 - pos.Y << 28 - zoom;

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, xx, yy);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/hybrid/{1}_{2:x7}_{3:x7}";
}
