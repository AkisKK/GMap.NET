using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Etc;

/// <summary>
///     SpainMap provider, http://sigpac.mapa.es/fega/visor/
/// </summary>
public class SpainMapProvider : GMapProvider
{
    public static readonly SpainMapProvider Instance;

    SpainMapProvider()
    {
        Copyright = string.Format("©{0} SIGPAC", DateTime.Today.Year);
        MinZoom = 5;
        Area = new RectLatLng(43.8741381814747, -9.700927734375, 14.34814453125, 7.8605775962932);
    }

    static SpainMapProvider()
    {
        Instance = new SpainMapProvider();
    }

    readonly string[] m_Levels =
    [
        "0", "1", "2", "3", "4", "MTNSIGPAC", "MTN2000", "MTN2000", "MTN2000", "MTN2000", "MTN2000", "MTN200",
        "MTN200", "MTN200", "MTN25", "MTN25", "ORTOFOTOS", "ORTOFOTOS", "ORTOFOTOS", "ORTOFOTOS"
    ];

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("7B70ABB0-1265-4D34-9442-F0788F4F689F");

    public override string Name { get; } = "SpainMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this];

            return m_Overlays;
        }
    }

    public override PureProjection Projection => MercatorProjection.Instance;

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom)
    {
        return string.Format(m_UrlFormat, m_Levels[zoom], zoom, pos.X, (2 << zoom - 1) - pos.Y - 1);
    }

    static readonly string m_UrlFormat = "http://sigpac.mapa.es/kmlserver/raster/{0}@3785/{1}.{2}.{3}.img";
}
