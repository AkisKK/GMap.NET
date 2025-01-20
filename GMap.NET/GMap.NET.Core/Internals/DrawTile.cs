using System;

namespace GMap.NET.Internals;

/// <summary>
///     struct for drawing tile
/// </summary>
internal struct DrawTile : IEquatable<DrawTile>, IComparable<DrawTile>
{
    public GPoint PosXY;
    public GPoint PosPixel;
    public double DistanceSqr;

    public override readonly string ToString()
    {
        return PosXY + ", px: " + PosPixel;
    }

    #region IEquatable<DrawTile> Members
    public readonly bool Equals(DrawTile other)
    {
        return PosXY == other.PosXY;
    }

    public override readonly bool Equals(object obj)
    {
        return obj is DrawTile tile && Equals(tile);
    }

    public override int GetHashCode()
    {
        // Combine the hash codes of PosXY and DistanceSqr
        int hashPosXY = PosXY.GetHashCode();
        int hashDistanceSqr = DistanceSqr.GetHashCode();

        // Combine the hash codes using XOR and a prime multiplier for better distribution
        return hashPosXY ^ (hashDistanceSqr * 397);
    }
    #endregion

    #region IComparable<DrawTile> Members
    public readonly int CompareTo(DrawTile other)
    {
        return other.DistanceSqr.CompareTo(DistanceSqr);
    }
    #endregion
}
