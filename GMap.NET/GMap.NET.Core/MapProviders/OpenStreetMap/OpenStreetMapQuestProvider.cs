﻿using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenStreetMapQuest provider - http://wiki.openstreetmap.org/wiki/MapQuest
/// </summary>
public class OpenStreetMapQuestProvider : OpenStreetMapProviderBase
{
    public static readonly OpenStreetMapQuestProvider Instance;

    OpenStreetMapQuestProvider()
    {
        Copyright = string.Format("© MapQuest - Map data ©{0} MapQuest, OpenStreetMap", DateTime.Today.Year);
    }

    static OpenStreetMapQuestProvider()
    {
        Instance = new OpenStreetMapQuestProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("D0A12840-973A-448B-B9C2-89B8A07DFF0F");

    public override string Name { get; } = "OpenStreetMapQuest";

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
        return string.Format(m_UrlFormat, GetServerNum(pos, 3) + 1, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://otile{0}.mqcdn.com/tiles/1.0.0/osm/{1}/{2}/{3}.png";
}
