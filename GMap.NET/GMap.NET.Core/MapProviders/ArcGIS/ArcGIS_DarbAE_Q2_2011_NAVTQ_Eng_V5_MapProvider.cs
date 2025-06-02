using System;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.ArcGIS;

/// <summary>
///     ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_Map provider,
///     http://www.darb.ae/ArcGIS/rest/services/BaseMaps/Q2_2011_NAVTQ_Eng_V5/MapServer
/// </summary>
public class ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider : GMapProvider
{
    public static readonly ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider Instance;

    ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider()
    {
        MaxZoom = 12;
        Area = RectLatLng.FromLTRB(49.8846923723311, 28.0188609585523, 58.2247031977662, 21.154115956732);
        Copyright = string.Format("©{0} ESRI - Map data ©{0} ArcGIS", DateTime.Today.Year);
    }

    static ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider()
    {
        Instance = new ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("E03CFEDF-9277-49B3-9912-D805347F934B");

    public override string Name { get; } = "ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider";

    public override PureProjection Projection => PlateCarreeProjectionDarbAe.Instance;

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
        // http://www.darb.ae/ArcGIS/rest/services/BaseMaps/Q2_2011_NAVTQ_Eng_V5/MapServer/tile/0/121/144

        return string.Format(m_UrlFormat, zoom, pos.Y, pos.X);
    }

    static readonly string m_UrlFormat =
        "http://www.darb.ae/ArcGIS/rest/services/BaseMaps/Q2_2011_NAVTQ_Eng_V5/MapServer/tile/{0}/{1}/{2}";
}
