﻿using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Here;

public abstract class HereMapProviderBase : GMapProvider
{
    public HereMapProviderBase()
    {
        MaxZoom = null;
        ReferrerUrl = "http://wego.here.com/";
        Copyright = string.Format("©{0} Here - Map data ©{0} NAVTEQ, Imagery ©{0} DigitalGlobe",
            DateTime.Today.Year);
    }

    public string AppId = string.Empty;
    public string AppCode = string.Empty;

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

    protected static readonly string UrlServerLetters = "1234";
}

/// <summary>
///     OviMap provider
/// </summary>
public class HereMapProvider : HereMapProviderBase
{
    public static readonly HereMapProvider Instance;

    HereMapProvider()
    {
    }

    static HereMapProvider()
    {
        Instance = new HereMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("30DC2083-AC4D-4471-A232-D8A67AC9373A");

    public override string Name { get; } = "HereMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom)
    {
        return string.Format(m_UrlFormat, UrlServerLetters[GetServerNum(pos, 4)], zoom, pos.X, pos.Y, AppId, AppCode);
    }

    static readonly string m_UrlFormat =
        "http://{0}.traffic.maps.cit.api.here.com/maptile/2.1/traffictile/newest/normal.day/{1}/{2}/{3}/256/png8?app_id={4}&app_code={5}";
}
