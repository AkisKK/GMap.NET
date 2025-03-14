﻿using System.Globalization;

namespace GMap.NET;

/// <summary>
///     the size
/// </summary>
public struct GSize
{
    public static readonly GSize Empty = new();

    public GSize(GPoint pt)
    {
        Width = pt.X;
        Height = pt.Y;
    }

    public GSize(long width, long height)
    {
        Width = width;
        Height = height;
    }

    public static GSize operator +(GSize sz1, GSize sz2)
    {
        return Add(sz1, sz2);
    }

    public static GSize operator -(GSize sz1, GSize sz2)
    {
        return Subtract(sz1, sz2);
    }

    public static bool operator ==(GSize sz1, GSize sz2)
    {
        return sz1.Width == sz2.Width && sz1.Height == sz2.Height;
    }

    public static bool operator !=(GSize sz1, GSize sz2)
    {
        return !(sz1 == sz2);
    }

    public static explicit operator GPoint(GSize size)
    {
        return new GPoint(size.Width, size.Height);
    }

    public readonly bool IsEmpty => Width == 0 && Height == 0;

    public long Width { get; set; }

    public long Height { get; set; }

    public static GSize Add(GSize sz1, GSize sz2)
    {
        return new GSize(sz1.Width + sz2.Width, sz1.Height + sz2.Height);
    }

    public static GSize Subtract(GSize sz1, GSize sz2)
    {
        return new GSize(sz1.Width - sz2.Width, sz1.Height - sz2.Height);
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is not GSize)
        {
            return false;
        }

        var comp = (GSize)obj;
        // Note value types can't have derived classes, so we don't need to
        //
        return comp.Width == Width &&
               comp.Height == Height;
    }

    public override readonly int GetHashCode()
    {
        if (IsEmpty)
        {
            return 0;
        }

        return Width.GetHashCode() ^ Height.GetHashCode();
    }

    public override readonly string ToString()
    {
        return "{Width=" + Width.ToString(CultureInfo.CurrentCulture) + ", Height=" +
               Height.ToString(CultureInfo.CurrentCulture) + "}";
    }
}
