﻿using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Etc;

/// <summary>
///     TurkeyMap provider, http://maps.pergo.com.tr/
/// </summary>
public class TurkeyMapProvider : GMapProvider
{
    public static readonly TurkeyMapProvider Instance;

    TurkeyMapProvider()
    {
        Copyright = string.Format("©{0} Pergo - Map data ©{0} Fideltus Advanced Technology", DateTime.Today.Year);
        Area = new RectLatLng(42.5830078125, 25.48828125, 19.05029296875, 6.83349609375);
        InvertedAxisY = true;
    }

    static TurkeyMapProvider()
    {
        Instance = new TurkeyMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("EDE895BD-756D-4BE4-8D03-D54DD8856F1D");

    public override string Name { get; } = "TurkeyMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this];

            return m_Overlays;
        }
    }

    public override PureProjection Projection => MercatorProjection.Instance;

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    static string MakeTileImageUrl(GPoint pos, int zoom)
    {
        // http://{domain}/{layerName}/{zoomLevel}/{first3LetterOfTileX}/{second3LetterOfTileX}/{third3LetterOfTileX}/{first3LetterOfTileY}/{second3LetterOfTileY}/{third3LetterOfTileXY}.png

        // http://map3.pergo.com.tr/tile/00/000/000/001/000/000/000.png   
        // That means: Zoom Level: 0 TileX: 1 TileY: 0

        // http://domain/tile/14/000/019/371/000/011/825.png
        // That means: Zoom Level: 14 TileX: 19371 TileY:11825

        // updated version
        // http://map1.pergo.com.tr/publish/tile/tile9913/06/000/000/038/000/000/039.png

        string x = pos.X.ToString(m_Zeros).Insert(3, m_Slash).Insert(7, m_Slash); // - 000/000/001
        string y = pos.Y.ToString(m_Zeros).Insert(3, m_Slash).Insert(7, m_Slash); // - 000/000/000

        return string.Format(m_UrlFormat, GetServerNum(pos, 3), zoom, x, y);
    }

    static readonly string m_Zeros = "000000000";
    static readonly string m_Slash = "/";
    static readonly string m_UrlFormat = "http://map{0}.pergo.com.tr/publish/tile/tile9913/{1:00}/{2}/{3}.png";
}
