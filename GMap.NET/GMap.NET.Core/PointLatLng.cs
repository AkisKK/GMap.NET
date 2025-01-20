using System;
using System.Globalization;

namespace GMap.NET;

/// <summary>
///     the point of coordinates
/// </summary>
[Serializable]
public struct PointLatLng
{
    public static readonly PointLatLng Empty = new();
    private double m_Latitude;
    private double m_Longitude;

    bool m_NotEmpty;

    public PointLatLng(double lat, double lng)
    {
        m_Latitude = lat;
        m_Longitude = lng;
        m_NotEmpty = true;
    }

    /// <summary>
    ///     returns true if coordinates wasn't assigned
    /// </summary>
    public readonly bool IsEmpty => !m_NotEmpty;

    public double Lat
    {
        readonly get => m_Latitude;
        set
        {
            m_Latitude = value;
            m_NotEmpty = true;
        }
    }

    public double Lng
    {
        readonly get => m_Longitude;
        set
        {
            m_Longitude = value;
            m_NotEmpty = true;
        }
    }

    public static PointLatLng operator +(PointLatLng pt, SizeLatLng sz)
    {
        return Add(pt, sz);
    }

    public static PointLatLng operator -(PointLatLng pt, SizeLatLng sz)
    {
        return Subtract(pt, sz);
    }

    public static SizeLatLng operator -(PointLatLng pt1, PointLatLng pt2)
    {
        return new SizeLatLng(pt1.Lat - pt2.Lat, pt2.Lng - pt1.Lng);
    }

    public static bool operator ==(PointLatLng left, PointLatLng right)
    {
        return left.Lng == right.Lng && left.Lat == right.Lat;
    }

    public static bool operator !=(PointLatLng left, PointLatLng right)
    {
        return !(left == right);
    }

    public static PointLatLng Add(PointLatLng pt, SizeLatLng sz)
    {
        return new PointLatLng(pt.Lat - sz.HeightLat, pt.Lng + sz.WidthLng);
    }

    public static PointLatLng Subtract(PointLatLng pt, SizeLatLng sz)
    {
        return new PointLatLng(pt.Lat + sz.HeightLat, pt.Lng - sz.WidthLng);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is not PointLatLng)
        {
            return false;
        }

        var tf = (PointLatLng)obj;
        return tf.Lng == Lng && tf.Lat == Lat && tf.GetType().Equals(GetType());
    }

    public void Offset(PointLatLng pos)
    {
        Offset(pos.Lat, pos.Lng);
    }

    public void Offset(double lat, double lng)
    {
        Lng += lng;
        Lat -= lat;
    }

    public override readonly int GetHashCode()
    {
        return Lng.GetHashCode() ^ Lat.GetHashCode();
    }

    public override readonly string ToString()
    {
        return string.Format(CultureInfo.CurrentCulture, "{{Lat={0}, Lng={1}}}", Lat, Lng);
    }
}
