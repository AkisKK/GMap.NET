using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenCycleMap provider - http://www.opencyclemap.org
/// </summary>
public class OpenCycleMapProvider : OpenStreetMapProviderBase
{
    public static readonly OpenCycleMapProvider Instance;

    OpenCycleMapProvider()
    {
        ReferrerUrl = "http://www.opencyclemap.org/";
    }

    static OpenCycleMapProvider()
    {
        Instance = new OpenCycleMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("D7E1826E-EE1E-4441-9F15-7C2DE0FE0B0A");

    public override string Name { get; } = "OpenCycleMap";

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

    static readonly string m_UrlFormat = "http://{0}.tile.opencyclemap.org/cycle/{1}/{2}/{3}.png";
}
