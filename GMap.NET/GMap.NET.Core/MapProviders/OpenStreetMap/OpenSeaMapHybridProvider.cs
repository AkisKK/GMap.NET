using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenSeaMapHybrid provider - http://openseamap.org
/// </summary>
public class OpenSeaMapHybridProvider : OpenStreetMapProviderBase
{
    public static readonly OpenSeaMapHybridProvider Instance;

    OpenSeaMapHybridProvider()
    {
        ReferrerUrl = "http://openseamap.org/";
    }

    static OpenSeaMapHybridProvider()
    {
        Instance = new OpenSeaMapHybridProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("FAACDE73-4B90-4AE6-BB4A-ADE4F3545592");

    public override string Name { get; } = "OpenSeaMapHybrid";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [OpenStreetMapProvider.Instance, this];

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

    static readonly string m_UrlFormat = "http://tiles.openseamap.org/seamark/{0}/{1}/{2}.png";
}
