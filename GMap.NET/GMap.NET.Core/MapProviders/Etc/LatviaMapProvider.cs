using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Etc;

public abstract class LatviaMapProviderBase : GMapProvider
{
    public LatviaMapProviderBase()
    {
        RefererUrl = "http://www.ikarte.lv/default.aspx?lang=en";
        Copyright = string.Format("©{0} Hnit-Baltic - Map data ©{0} LR Valsts zemes dieniests, SIA Envirotech",
            DateTime.Today.Year);
        MaxZoom = 11;
        Area = new RectLatLng(58.0794870805093, 20.3286067123543, 7.90883164336887, 2.506129113082);
    }

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => LKS92Projection.Instance;

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
///     LatviaMap provider, http://www.ikarte.lv/
/// </summary>
public class LatviaMapProvider : LatviaMapProviderBase
{
    public static readonly LatviaMapProvider Instance;

    LatviaMapProvider()
    {
    }

    static LatviaMapProvider()
    {
        Instance = new LatviaMapProvider();
    }

    #region GMapProvider Members

    public override Guid Id    {        get;    } = new Guid("2A21CBB1-D37C-458D-905E-05F19536EF1F");

    public override string Name    {        get;    } = "LatviaMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://www.maps.lt/cache/ikartelv/map/_alllayers/L03/R00000037/C00000053.png
        // http://www.maps.lt/arcgiscache/ikartelv/map/_alllayers/L02/R0000001c/C0000002a.png
        // http://services.maps.lt/mapsk_services/rest/services/ikartelv/MapServer/tile/5/271/416.png?cl=ikrlv

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://services.maps.lt/mapsk_services/rest/services/ikartelv/MapServer/tile/{0}/{1}/{2}.png?cl=ikrlv";

    //static readonly string UrlFormat = "http://www.maps.lt/arcgiscache/ikartelv/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.png";
}
