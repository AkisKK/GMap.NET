﻿using System;

namespace GMap.NET.Projections;

class MercatorProjectionYandex : PureProjection
{
    public static readonly MercatorProjectionYandex Instance = new();

    static readonly double m_MinLatitude = -85.05112878;
    static readonly double m_MaxLatitude = 85.05112878;
    static readonly double m_MinLongitude = -177;
    static readonly double m_MaxLongitude = 177;

    static readonly double m_RAD_DEG = 180 / Math.PI;
    static readonly double m_DEG_RAD = Math.PI / 180;
    static readonly double m_MathPiDiv4 = Math.PI / 4;

    public override RectLatLng Bounds => RectLatLng.FromLTRB(m_MinLongitude, m_MaxLatitude, m_MaxLongitude, m_MinLatitude);

    public override GSize TileSize { get; } = new GSize(256, 256);

    public override double Axis => 6356752.3142;

    public override double Flattening => 1.0 / 298.257223563;

    public override GPoint FromLatLngToPixel(double lat, double lng, int zoom)
    {
        lat = Clip(lat, m_MinLatitude, m_MaxLatitude);
        lng = Clip(lng, m_MinLongitude, m_MaxLongitude);

        double rLon = lng * m_DEG_RAD; // Math.PI / 180; 
        double rLat = lat * m_DEG_RAD; // Math.PI / 180; 

        double a = 6378137;
        double k = 0.0818191908426;

        double z = Math.Tan(m_MathPiDiv4 + rLat / 2) /
                   Math.Pow(Math.Tan(m_MathPiDiv4 + Math.Asin(k * Math.Sin(rLat)) / 2), k);
        double z1 = Math.Pow(2, 23 - zoom);

        double dx = (20037508.342789 + a * rLon) * 53.5865938 / z1;
        double dy = (20037508.342789 - a * Math.Log(z)) * 53.5865938 / z1;

        return new GPoint((long)dx, (long)dy);
    }

    public override PointLatLng FromPixelToLatLng(long x, long y, int zoom)
    {
        _ = GetTileMatrixSizePixel(zoom);

        // double mapSizeX = s.Width;
        // double mapSizeY = s.Height;

        double a = 6378137;
        double c1 = 0.00335655146887969;
        double c2 = 0.00000657187271079536;
        double c3 = 0.00000001764564338702;
        double c4 = 0.00000000005328478445;
        double z1 = 23 - zoom;
        double mercX = x * Math.Pow(2, z1) / 53.5865938 - 20037508.342789;
        double mercY = 20037508.342789 - y * Math.Pow(2, z1) / 53.5865938;

        double g = Math.PI / 2 - 2 * Math.Atan(1 / Math.Exp(mercY / a));
        double z = g + c1 * Math.Sin(2 * g) + c2 * Math.Sin(4 * g) + c3 * Math.Sin(6 * g) + c4 * Math.Sin(8 * g);

        var ret = PointLatLng.Empty;
        ret.Lat = z * m_RAD_DEG;
        ret.Lng = mercX / a * m_RAD_DEG;

        return ret;
    }

    public override GSize GetTileMatrixMinXY(int zoom)
    {
        return new GSize(0, 0);
    }

    public override GSize GetTileMatrixMaxXY(int zoom)
    {
        long xy = 1 << zoom;
        return new GSize(xy - 1, xy - 1);
    }
}
