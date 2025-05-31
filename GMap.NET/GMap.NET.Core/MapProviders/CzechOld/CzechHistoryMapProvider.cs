using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.CzechOld;

/// <summary>
///     CzechHistoryMap provider, http://www.mapy.cz/
/// </summary>
public class CzechHistoryMapProviderOld : CzechMapProviderBaseOld
{
    public static readonly CzechHistoryMapProviderOld Instance;

    CzechHistoryMapProviderOld()
    {
    }

    static CzechHistoryMapProviderOld()
    {
        Instance = new CzechHistoryMapProviderOld();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("C666AAF4-9D27-418F-97CB-7F0D8CC44544");

    public override string Name { get; } = "CzechHistoryOldMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this, CzechHybridMapProviderOld.Instance];

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
        // http://m4.mapserver.mapy.cz/army2/9_7d00000_8080000

        long xx = pos.X << 28 - zoom;
        long yy = (long)Math.Pow(2.0, zoom) - 1 - pos.Y << 28 - zoom;

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, xx, yy);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/army2/{1}_{2:x7}_{3:x7}";
}
