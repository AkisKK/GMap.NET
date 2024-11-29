using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Czech;

/// <summary>
///     CzechHistoryMap provider, http://www.mapy.cz/
/// </summary>
public class CzechHistoryMapProvider : CzechMapProviderBase
{
    public static readonly CzechHistoryMapProvider Instance;

    CzechHistoryMapProvider()
    {
        MaxZoom = 15;
    }

    static CzechHistoryMapProvider()
    {
        Instance = new CzechHistoryMapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("CD44C19D-5EED-4623-B367-FB39FDC55B8F");

    public override string Name { get; } = "CzechHistoryMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this, CzechHybridMapProvider.Instance];

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
        // http://m3.mapserver.mapy.cz/army2-m/14-8802-5528

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/army2-m/{1}-{2}-{3}";
}
