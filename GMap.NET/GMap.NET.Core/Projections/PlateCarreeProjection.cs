using System;

namespace GMap.NET.Projections;

/// <summary>
///     Plate Carrée (literally, “plane square”) projection
///     PROJCS["WGS 84 / World Equidistant
///     Cylindrical",GEOGCS["GCS_WGS_1984",DATUM["D_WGS_1984",SPHEROID["WGS_1984",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["Degree",0.017453292519943295]],UNIT["Meter",1]]
/// </summary>
public class PlateCarreeProjection : PureProjection
{
    public static readonly PlateCarreeProjection Instance = new();

    static readonly double m_MinLatitude = -85.05112878;
    static readonly double m_MaxLatitude = 85.05112878;
    static readonly double m_MinLongitude = -180;
    static readonly double m_MaxLongitude = 180;

    public override RectLatLng Bounds => RectLatLng.FromLTRB(m_MinLongitude, m_MaxLatitude, m_MaxLongitude, m_MinLatitude);

    public override GSize TileSize { get; } = new GSize(512, 512);

    public override double Axis => 6378137;

    public override double Flattening => 1.0 / 298.257223563;

    public override GPoint FromLatLngToPixel(double lat, double lng, int zoom)
    {
        var ret = GPoint.Empty;

        lat = Clip(lat, m_MinLatitude, m_MaxLatitude);
        lng = Clip(lng, m_MinLongitude, m_MaxLongitude);

        var s = GetTileMatrixSizePixel(zoom);
        double mapSizeX = s.Width;
        // double mapSizeY = s.Height;

        double scale = 360.0 / mapSizeX;

        ret.Y = (long)((90.0 - lat) / scale);
        ret.X = (long)((lng + 180.0) / scale);

        return ret;
    }

    public override PointLatLng FromPixelToLatLng(long x, long y, int zoom)
    {
        var ret = PointLatLng.Empty;

        var s = GetTileMatrixSizePixel(zoom);
        double mapSizeX = s.Width;
        // double mapSizeY = s.Height;

        double scale = 360.0 / mapSizeX;

        ret.Lat = 90 - y * scale;
        ret.Lng = x * scale - 180;

        return ret;
    }

    public override GSize GetTileMatrixMaxXY(int zoom)
    {
        long y = (long)Math.Pow(2, zoom);
        return new GSize(2 * y - 1, y - 1);
    }

    public override GSize GetTileMatrixMinXY(int zoom)
    {
        return new GSize(0, 0);
    }
}
