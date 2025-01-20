using System;
using GMap.NET.Internals;

namespace GMap.NET.MapProviders.Lithuania;

/// <summary>
///     LithuaniaHybridMap provider
/// </summary>
public class LithuaniaHybridMapProvider : LithuaniaMapProviderBase
{
    public static readonly LithuaniaHybridMapProvider Instance;

    LithuaniaHybridMapProvider()
    {
    }

    static LithuaniaHybridMapProvider()
    {
        Instance = new LithuaniaHybridMapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("279AB0E0-4704-4AA6-86AD-87D13B1F8975");

    public override string Name { get; } = "LithuaniaHybridMap";

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [LithuaniaOrtoFotoMapProvider.Instance, this];

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
        // http://dc5.maps.lt/cache/mapslt_ortofoto_overlay/map/_alllayers/L09/R000016b1/C000020e1.jpg

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    internal static readonly string m_UrlFormat =
        "http://dc5.maps.lt/cache/mapslt_ortofoto_overlay/map/_alllayers/L{0:00}/R{1:x8}/C{2:x8}.png";
}
