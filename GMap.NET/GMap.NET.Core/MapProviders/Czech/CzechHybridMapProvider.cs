using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Czech;

/// <summary>
///     CzechHybridMap provider, http://www.mapy.cz/
/// </summary>
public class CzechHybridMapProvider : CzechMapProviderBase
{
    public static readonly CzechHybridMapProvider Instance;

    CzechHybridMapProvider()
    {
    }

    static CzechHybridMapProvider()
    {
        Instance = new CzechHybridMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("7540CE5B-F634-41E9-B23E-A6E0A97526FD");

    public override string Name { get; } = "CzechHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [CzechSatelliteMapProvider.Instance, this];

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
        // http://m3.mapserver.mapy.cz/hybrid-m/14-8802-5528

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/hybrid-m/{1}-{2}-{3}";
}
