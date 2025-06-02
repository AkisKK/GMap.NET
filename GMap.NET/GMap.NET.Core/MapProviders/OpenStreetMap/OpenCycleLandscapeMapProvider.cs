using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenCycleMap Landscape provider - http://www.opencyclemap.org
/// </summary>
public class OpenCycleLandscapeMapProvider : OpenStreetMapProviderBase
{
    public static readonly OpenCycleLandscapeMapProvider Instance;

    OpenCycleLandscapeMapProvider()
    {
        ReferrerUrl = "http://www.opencyclemap.org/";
    }

    static OpenCycleLandscapeMapProvider()
    {
        Instance = new OpenCycleLandscapeMapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("BDBAA939-6597-4D87-8F4F-261C49E35F56");

    public override string Name { get; } = "OpenCycleLandscapeMap";

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


    static readonly string m_UrlFormat = "http://{0}.tile3.opencyclemap.org/landscape/{1}/{2}/{3}.png";
}
