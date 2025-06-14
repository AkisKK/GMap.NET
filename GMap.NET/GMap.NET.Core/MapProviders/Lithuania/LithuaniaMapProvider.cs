﻿using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Lithuania;

public abstract class LithuaniaMapProviderBase : GMapProvider
{
    public LithuaniaMapProviderBase()
    {
        ReferrerUrl = "http://www.maps.lt/map/";
        Copyright = string.Format("©{0} Hnit-Baltic - Map data ©{0} ESRI", DateTime.Today.Year);
        MaxZoom = 12;
        Area = new RectLatLng(56.431489960361, 20.8962105239809, 5.8924169643369, 2.58940626652217);
    }

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => LKS94Projection.Instance;

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
///     LithuaniaMap provider, http://www.maps.lt/map/
/// </summary>
public class LithuaniaMapProvider : LithuaniaMapProviderBase
{
    public static readonly LithuaniaMapProvider Instance;

    LithuaniaMapProvider()
    {
    }

    static LithuaniaMapProvider()
    {
        Instance = new LithuaniaMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("5859079F-1B5E-484B-B05C-41CE664D8A93");

    public override string Name { get; } = "LithuaniaMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://dc5.maps.lt/cache/mapslt/map/_alllayers/L08/R00000912/C00000d25.png

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat = "http://dc5.maps.lt/cache/mapslt/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.png";
}
