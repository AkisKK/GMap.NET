using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Etc;

public abstract class SwedenMapProviderBase : GMapProvider
{
    public SwedenMapProviderBase()
    {
        ReferrerUrl = "https://kso.etjanster.lantmateriet.se/?lang=en";
        Copyright = string.Format("©{0} Lantmäteriet", DateTime.Today.Year);
        MaxZoom = 11;
        //Area = new RectLatLng(58.0794870805093, 20.3286067123543, 7.90883164336887, 2.506129113082);
    }

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => SWEREF99_TMProjection.Instance;

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
///     SwedenMap provider, https://kso.etjanster.lantmateriet.se/?lang=en#
/// </summary>
public class SwedenMapProvider : SwedenMapProviderBase
{
    public static readonly SwedenMapProvider Instance;

    SwedenMapProvider()
    {
    }

    static SwedenMapProvider()
    {
        Instance = new SwedenMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("40890A96-6E82-4FA7-90A3-73D66B974F63");

    public override string Name { get; } = "SwedenMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // https://kso.etjanster.lantmateriet.se/karta/topowebb/v1/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=topowebb&STYLE=default&TILEMATRIXSET=3006&TILEMATRIX=2&TILEROW=6&TILECOL=7&FORMAT=image%2Fpng

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    private static readonly string m_UrlFormat =
        "https://kso.etjanster.lantmateriet.se/karta/topowebb/v1.1/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=topowebb&STYLE=default&TILEMATRIXSET=3006&TILEMATRIX={0}&TILEROW={1}&TILECOL={2}&FORMAT=image%2Fpng";
}
