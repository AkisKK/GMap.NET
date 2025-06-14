﻿using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Etc;

public abstract class WikiMapiaMapProviderBase : GMapProvider
{
    public WikiMapiaMapProviderBase()
    {
        MaxZoom = 22;
        ReferrerUrl = "http://wikimapia.org/";
        Copyright = string.Format("© WikiMapia.org - Map data ©{0} WikiMapia", DateTime.Today.Year);
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

    public override GMapProvider[] Overlays
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        throw new NotImplementedException();
    }

    #endregion

    public static int GetServerNum(GPoint pos)
    {
        return (int)(pos.X % 4 + pos.Y % 4 * 4);
    }
}

/// <summary>
///     WikiMapiaMap provider, http://wikimapia.org/
/// </summary>
public class WikiMapiaMapProvider : WikiMapiaMapProviderBase
{
    public static readonly WikiMapiaMapProvider Instance;

    WikiMapiaMapProvider()
    {
    }

    static WikiMapiaMapProvider()
    {
        Instance = new WikiMapiaMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("7974022B-1AA6-41F1-8D01-F49940E4B48C");

    public override string Name { get; } = "WikiMapiaMap";

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
        return string.Format(m_UrlFormat, GetServerNum(pos), pos.X, pos.Y, zoom);
    }

    static readonly string m_UrlFormat = "http://i{0}.wikimapia.org/?x={1}&y={2}&zoom={3}";
}
