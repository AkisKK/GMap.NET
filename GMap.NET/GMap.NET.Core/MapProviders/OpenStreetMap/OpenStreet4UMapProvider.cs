﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenStreet4UMap provider
///     http://www.4umaps.eu
///     4UMaps are topographic outdoor maps based on OpenStreetmap data.
///     The map contains everything you need for any kind of back country activity like hiking,
///     mountain biking, cycling, climbing etc. 4UMaps has elevation lines, hill shading,
///     peak height and name, streets, ways, tracks and trails, as well as springs, supermarkets,
///     restaurants, hotels, shelters etc.
/// </summary>
public class OpenStreet4UMapProvider : OpenStreetMapProviderBase
{
    public static readonly OpenStreet4UMapProvider Instance;

    OpenStreet4UMapProvider()
    {
        ReferrerUrl = "http://www.4umaps.eu/map.htm";
        Copyright = string.Format("© 4UMaps.eu, © OpenStreetMap - Map data ©{0} OpenStreetMap",
            DateTime.Today.Year);
    }

    static OpenStreet4UMapProvider()
    {
        Instance = new OpenStreet4UMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("3E3D919E-9814-4978-B430-6AAB2C1E41B2");

    public override string Name { get; } = "OpenStreet4UMap";

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
        string url = MakeTileImageUrl(pos, zoom);
        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        return string.Format(m_UrlFormat, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://4umaps.eu/{0}/{1}/{2}.png";
}
