namespace GMap.NET.MapProviders.Etc;

using System;
using GMap.NET;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

public class SwissTopoProvider : GMapProvider
{
    private readonly string m_Name = "SwissTopo";
    private readonly Random m_RandomGen;

    public override Guid Id { get; protected set; } = new("0F1F1EC5-B297-4B5B-8EB4-27AA403D1860");

    public static readonly SwissTopoProvider Instance;

    SwissTopoProvider()
    {
        // Terms of use: https://api3.geo.admin.ch/api/terms_of_use.html

        MaxZoom = null;
        m_RandomGen = new Random();
    }

    private GMapProvider[] m_Overlays;

    string MakeTileImageUrl(GPoint pos, int zoom)
    {
        int serverMaxDigits = 10; // from wmts[0-9].geo.admin.ch 
        int serverDigit = m_RandomGen.Next() % serverMaxDigits;
        string layerName = "ch.swisstopo.pixelkarte-farbe";
        string tileMatrixSet = "2056";
        string time = "current";

        // <Scheme>://<ServerName>/<ProtocolVersion>/<LayerName>/<StyleName>/<Time>/<TileMatrixSet>/<TileSetId=Zoom>/<TileRow>/<TileCol>.<FormatExtension>
        string formattedUrl = $"https://wmts{serverDigit}.geo.admin.ch/1.0.0/{layerName}/default/{time}/{tileMatrixSet}/{zoom}/{pos.X}/{pos.Y}.jpeg";

        return formattedUrl;
    }

    static SwissTopoProvider()
    {
        Instance = new SwissTopoProvider();
    }

    #region GMapProvider Members

    public override string Name => m_Name;
    public override PureProjection Projection => SwissTopoProjection.Instance;

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
}
