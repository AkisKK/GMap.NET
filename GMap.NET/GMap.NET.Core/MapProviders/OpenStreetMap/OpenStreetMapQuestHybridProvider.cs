using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenStreetMapQuestHybrid provider - http://wiki.openstreetmap.org/wiki/MapQuest
/// </summary>
public class OpenStreetMapQuestHybridProvider : OpenStreetMapProviderBase
{
    public static readonly OpenStreetMapQuestHybridProvider Instance;

    OpenStreetMapQuestHybridProvider()
    {
        Copyright = string.Format("© MapQuest - Map data ©{0} MapQuest, OpenStreetMap", DateTime.Today.Year);
    }

    static OpenStreetMapQuestHybridProvider()
    {
        Instance = new OpenStreetMapQuestHybridProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("95E05027-F846-4429-AB7A-9445ABEEFA2A");

    public override string Name { get; } = "OpenStreetMapQuestHybrid";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [OpenStreetMapQuestSatelliteProvider.Instance, this];

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

    static readonly string m_UrlFormat = "http://otile{0}.mqcdn.com/tiles/1.0.0/hyb/{1}/{2}/{3}.png";
}
