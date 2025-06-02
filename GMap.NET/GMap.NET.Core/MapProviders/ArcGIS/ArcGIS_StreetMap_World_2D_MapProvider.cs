using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.ArcGIS;

public abstract class ArcGISMapPlateCarreeProviderBase : GMapProvider
{
    public ArcGISMapPlateCarreeProviderBase()
    {
        Copyright = string.Format("©{0} ESRI - Map data ©{0} ArcGIS", DateTime.Today.Year);
    }

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => PlateCarreeProjection.Instance;

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

public abstract class ArcGISMapMercatorProviderBase : GMapProvider
{
    public ArcGISMapMercatorProviderBase()
    {
        MaxZoom = null;
        Copyright = string.Format("©{0} ESRI - Map data ©{0} ArcGIS", DateTime.Today.Year);
    }

    #region GMapProvider Members

    public override Guid Id
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override string Name
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override PureProjection Projection
    {
        get
        {
            return MercatorProjection.Instance;
        }
    }

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
///     ArcGIS_StreetMap_World_2D_Map provider, http://server.arcgisonline.com
/// </summary>
public class ArcGIS_StreetMap_World_2D_MapProvider : ArcGISMapPlateCarreeProviderBase
{
    public static readonly ArcGIS_StreetMap_World_2D_MapProvider Instance;

    ArcGIS_StreetMap_World_2D_MapProvider()
    {
    }

    static ArcGIS_StreetMap_World_2D_MapProvider()
    {
        Instance = new ArcGIS_StreetMap_World_2D_MapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("00BF56D4-4B48-4939-9B11-575BBBE4A718");

    public override string Name { get; } = "ArcGIS_StreetMap_World_2D_Map";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_StreetMap_World_2D/MapServer/tile/0/0/0.jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://server.arcgisonline.com/ArcGIS/rest/services/ESRI_StreetMap_World_2D/MapServer/tile/{0}/{1}/{2}";
}
