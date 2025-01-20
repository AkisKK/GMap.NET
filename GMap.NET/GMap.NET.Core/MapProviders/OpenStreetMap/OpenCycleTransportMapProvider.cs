using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenCycleMap Transport provider - http://www.opencyclemap.org
/// </summary>
public class OpenCycleTransportMapProvider : OpenStreetMapProviderBase
{
    public static readonly OpenCycleTransportMapProvider Instance;

    OpenCycleTransportMapProvider()
    {
        RefererUrl = "http://www.opencyclemap.org/";
    }

    static OpenCycleTransportMapProvider()
    {
        Instance = new OpenCycleTransportMapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("AF66DF88-AD25-43A9-8F82-56FCA49A748A");

    public override string Name { get; } = "OpenCycleTransportMap";

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

    string MakeTileImageUrl(GPoint pos, int zoom)
    {
        char letter = ServerLetters[GetServerNum(pos, 3)];
        return string.Format(m_UrlFormat, letter, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "http://{0}.tile2.opencyclemap.org/transport/{1}/{2}/{3}.png";
}
