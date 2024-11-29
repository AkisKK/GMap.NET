using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenStreetMapQuestSatellite provider - http://wiki.openstreetmap.org/wiki/MapQuest
/// </summary>
public class OpenStreetMapQuestSatelliteProvider : OpenStreetMapProviderBase
{
    public static readonly OpenStreetMapQuestSatelliteProvider Instance;

    OpenStreetMapQuestSatelliteProvider()
    {
        Copyright = string.Format("© MapQuest - Map data ©{0} MapQuest, OpenStreetMap", DateTime.Today.Year);
    }

    static OpenStreetMapQuestSatelliteProvider()
    {
        Instance = new OpenStreetMapQuestSatelliteProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("E590D3B1-37F4-442B-9395-ADB035627F67");

    public override string Name { get; } = "OpenStreetMapQuestSatellite";

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

    static readonly string m_UrlFormat = "http://otile{0}.mqcdn.com/tiles/1.0.0/sat/{1}/{2}/{3}.jpg";
}
