using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.NearMap;

/// <summary>
///     NearHybridMap provider - http://www.nearmap.com/
/// </summary>
public class NearHybridMapProvider : NearMapProviderBase
{
    public static readonly NearHybridMapProvider Instance;

    NearHybridMapProvider()
    {
    }

    static NearHybridMapProvider()
    {
        Instance = new NearHybridMapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("4BF8819A-635D-4A94-8DC7-94C0E0F04BFD");

    public override string Name { get; } = "NearHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [NearSatelliteMapProvider.Instance, this];

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
        // http://web1.nearmap.com/maps/hl=en&x=37&y=19&z=6&nml=MapT&nmg=1&s=2KbhmZZ             
        // http://web1.nearmap.com/maps/hl=en&x=36&y=19&z=6&nml=MapT&nmg=1&s=2YKWhQi

        return string.Format(m_UrlFormat, GetServerNum(pos, 3), pos.X, pos.Y, zoom);
    }

    static readonly string m_UrlFormat = "http://web{0}.nearmap.com/maps/hl=en&x={1}&y={2}&z={3}&nml=MapT&nmg=1";
}
