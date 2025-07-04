﻿//--------------------------------------------------------------------------------------------
// 20200313 (jokubokla): The Pseudo Mercator (EPSG:3857) instead of SWEREF99 (EPSG:3006)
//
// This project contains the Lantmäteriet SWEREF99 Map for quite some time. Recently I found 
// out (by using QGIS and the GetCapabilities function of WMTS) that the Lantmäteriet map 
// is available also in a Pseudo Mercator Projection (EPSG:3857). 
// 
// This is very convenient if one uses this project to generate offline Maps, e.g. in 
// the .mbtiles SQLite database format. Android Apps like LOCUS or ORUX only understand
// Mercator to my knowledge. With this Provider, Android maps from Lantmäteriet can be 
// created for those Apps as well.
//--------------------------------------------------------------------------------------------

namespace GMap.NET.MapProviders.Etc;

using System;
using GMap.NET;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

public abstract class SwedenMapProviderAltBase : GMapProvider
{
    public SwedenMapProviderAltBase()
    {
        ReferrerUrl = "https://kso.etjanster.lantmateriet.se/?lang=en";
        Copyright = string.Format("©{0} Lantmäteriet", DateTime.Today.Year);
        MaxZoom = 15;
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

    protected static readonly string UrlServerLetters = "bcde";
}

/// <summary>
/// SwedenMapAlt provider
/// </summary>
public class SwedenMapProviderAlt : SwedenMapProviderAltBase
{
    public static readonly SwedenMapProviderAlt Instance;

    SwedenMapProviderAlt()
    {
    }

    static SwedenMapProviderAlt()
    {
        Instance = new SwedenMapProviderAlt();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new("d5e8e0de-3a93-4983-941e-9b66d79f50d6");

    readonly string m_Name = "SwedenMapAlternative";
    public override string Name => m_Name;

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion


    // 20200313 (jokubokla): Here is the magic: Use another Projection for Lantmateriet


    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // https://kso.etjanster.lantmateriet.se/karta/topowebb/v1/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=topowebb&STYLE=default&TILEMATRIXSET=3857&TILEMATRIX=2&TILEROW=6&TILECOL=7&FORMAT=image%2Fpng

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat = "https://kso.etjanster.lantmateriet.se/karta/topowebb/v1.1/wmts?SERVICE=WMTS&REQUEST=GetTile&VERSION=1.0.0&LAYER=topowebb&STYLE=default&TILEMATRIXSET=3857&TILEMATRIX={0}&TILEROW={1}&TILECOL={2}&FORMAT=image%2Fpng";


}
