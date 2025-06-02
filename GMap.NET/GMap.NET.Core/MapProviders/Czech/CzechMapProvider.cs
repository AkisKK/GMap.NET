using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Czech;

public abstract class CzechMapProviderBase : GMapProvider
{
    public CzechMapProviderBase()
    {
        ReferrerUrl = "http://www.mapy.cz/";
        Area = new RectLatLng(51.2024819920053, 11.8401353319027, 7.22833716731277, 2.78312271922872);
    }

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => MercatorProjection.Instance;

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this];

            return m_Overlays;
        }
    }

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        throw new NotImplementedException();
    }

    #endregion
}

/// <summary>
///     CzechMap provider, http://www.mapy.cz/
/// </summary>
public class CzechMapProvider : CzechMapProviderBase
{
    public static readonly CzechMapProvider Instance;

    CzechMapProvider()
    {
    }

    static CzechMapProvider()
    {
        Instance = new CzechMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("13AB92EF-8C3B-4FAC-B2CD-2594C05F8BFC");

    public override string Name { get; } = "CzechMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // ['base-m','ophoto-m','turist-m','army2-m']
        // http://m3.mapserver.mapy.cz/base-m/14-8802-5528

        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://m{0}.mapserver.mapy.cz/base-m/{1}-{2}-{3}";
    //static readonly string UrlFormat = "https://m{0}.mapserver.mapy.cz/turist-m/{1}-{2}-{3}";
}
