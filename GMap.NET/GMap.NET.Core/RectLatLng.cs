using System;
using System.Globalization;

namespace GMap.NET;

/// <summary>
///     the rect of coordinates
/// </summary>
public struct RectLatLng
{
    public static readonly RectLatLng Empty;

    public RectLatLng(double lat, double lng, double widthLng, double heightLat)
    {
        Lng = lng;
        Lat = lat;
        WidthLng = widthLng;
        HeightLat = heightLat;
        m_NotEmpty = true;
    }

    public RectLatLng(PointLatLng location, SizeLatLng size)
    {
        Lng = location.Lng;
        Lat = location.Lat;
        WidthLng = size.WidthLng;
        HeightLat = size.HeightLat;
        m_NotEmpty = true;
    }

    public static RectLatLng FromLTRB(double leftLng, double topLat, double rightLng, double bottomLat)
    {
        return new RectLatLng(topLat, leftLng, rightLng - leftLng, topLat - bottomLat);
    }

    public PointLatLng LocationTopLeft
    {
        readonly get => new(Lat, Lng);
        set
        {
            Lng = value.Lng;
            Lat = value.Lat;
        }
    }

    public readonly PointLatLng LocationRightBottom
    {
        get
        {
            var ret = new PointLatLng(Lat, Lng);
            ret.Offset(HeightLat, WidthLng);
            return ret;
        }
    }

    public readonly PointLatLng LocationMiddle
    {
        get
        {
            var ret = new PointLatLng(Lat, Lng);
            ret.Offset(HeightLat / 2, WidthLng / 2);
            return ret;
        }
    }

    public SizeLatLng Size
    {
        readonly get => new(HeightLat, WidthLng);
        set
        {
            WidthLng = value.WidthLng;
            HeightLat = value.HeightLat;
        }
    }

    public double Lng { get; set; }

    public double Lat { get; set; }

    public double WidthLng { get; set; }

    public double HeightLat { get; set; }

    public readonly double Left => Lng;

    public readonly double Top => Lat;

    public readonly double Right => Lng + WidthLng;

    public readonly double Bottom => Lat - HeightLat;

    private readonly bool m_NotEmpty;

    /// <summary>
    ///     returns true if coordinates wasn't assigned
    /// </summary>
    public readonly bool IsEmpty => !m_NotEmpty;

    public override readonly bool Equals(object obj)
    {
        if (obj is not RectLatLng)
        {
            return false;
        }

        var ef = (RectLatLng)obj;
        return ef.Lng == Lng && ef.Lat == Lat && ef.WidthLng == WidthLng &&
               ef.HeightLat == HeightLat;
    }

    public static bool operator ==(RectLatLng left, RectLatLng right)
    {
        return left.Lng == right.Lng && left.Lat == right.Lat && left.WidthLng == right.WidthLng &&
               left.HeightLat == right.HeightLat;
    }

    public static bool operator !=(RectLatLng left, RectLatLng right)
    {
        return !(left == right);
    }

    public readonly bool Contains(double lat, double lng)
    {
        return Lng <= lng && lng < Lng + WidthLng && Lat >= lat &&
               lat > Lat - HeightLat;
    }

    public readonly bool Contains(PointLatLng pt) => Contains(pt.Lat, pt.Lng);

    public readonly bool Contains(RectLatLng rect)
    {
        return Lng <= rect.Lng && rect.Lng + rect.WidthLng <= Lng + WidthLng &&
               Lat >= rect.Lat && rect.Lat - rect.HeightLat >= Lat - HeightLat;
    }

    public override readonly int GetHashCode()
    {
        if (IsEmpty)
        {
            return 0;
        }

        return Lng.GetHashCode() ^ Lat.GetHashCode() ^ WidthLng.GetHashCode() ^
               HeightLat.GetHashCode();
    }

    // from here down need to test each function to be sure they work good
    // |
    // .

    #region -- unsure --

    public void Inflate(double lat, double lng)
    {
        Lng -= lng;
        Lat += lat;
        WidthLng += 2d * lng;
        HeightLat += 2d * lat;
    }

    public void Inflate(SizeLatLng size)
    {
        Inflate(size.HeightLat, size.WidthLng);
    }

    public static RectLatLng Inflate(RectLatLng rect, double lat, double lng)
    {
        var ef = rect;
        ef.Inflate(lat, lng);
        return ef;
    }

    public void Intersect(RectLatLng rect)
    {
        var ef = Intersect(rect, this);
        Lng = ef.Lng;
        Lat = ef.Lat;
        WidthLng = ef.WidthLng;
        HeightLat = ef.HeightLat;
    }

    // ok ???
    public static RectLatLng Intersect(RectLatLng a, RectLatLng b)
    {
        double lng = Math.Max(a.Lng, b.Lng);
        double num2 = Math.Min(a.Lng + a.WidthLng, b.Lng + b.WidthLng);

        double lat = Math.Max(a.Lat, b.Lat);
        double num4 = Math.Min(a.Lat + a.HeightLat, b.Lat + b.HeightLat);

        if (num2 >= lng && num4 >= lat)
        {
            return new RectLatLng(lat, lng, num2 - lng, num4 - lat);
        }

        return Empty;
    }

    // ok ???
    // http://greatmaps.codeplex.com/workitem/15981
    public readonly bool IntersectsWith(RectLatLng a)
    {
        return Left < a.Right && Top > a.Bottom && Right > a.Left && Bottom < a.Top;
    }

    // ok ???
    // http://greatmaps.codeplex.com/workitem/15981
    public static RectLatLng Union(RectLatLng a, RectLatLng b)
    {
        return FromLTRB(
            Math.Min(a.Left, b.Left),
            Math.Max(a.Top, b.Top),
            Math.Max(a.Right, b.Right),
            Math.Min(a.Bottom, b.Bottom));
    }

    #endregion

    // .
    // |
    // unsure ends here

    public void Offset(PointLatLng pos)
    {
        Offset(pos.Lat, pos.Lng);
    }

    public void Offset(double lat, double lng)
    {
        Lng += lng;
        Lat -= lat;
    }

    public override readonly string ToString()
    {
        return "{Lat=" + Lat.ToString(CultureInfo.CurrentCulture) + ",Lng=" +
               Lng.ToString(CultureInfo.CurrentCulture) + ",WidthLng=" +
               WidthLng.ToString(CultureInfo.CurrentCulture) + ",HeightLat=" +
               HeightLat.ToString(CultureInfo.CurrentCulture) + "}";
    }

    static RectLatLng()
    {
        Empty = new RectLatLng();
    }
}
