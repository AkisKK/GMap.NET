using System;
using System.IO;

namespace GMap.NET.Internals;

/// <summary>
///     image abstraction proxy
/// </summary>
public abstract class PureImageProxy
{
    public abstract PureImage FromStream(Stream stream);

    public abstract bool Save(Stream stream, PureImage image);

    public PureImage FromArray(byte[] data)
    {
        var m = new MemoryStream(data, 0, data.Length, false, true);
        var pi = FromStream(m);
        if (pi != null)
        {
            m.Position = 0;
            pi.Data = m;
        }

        return pi;
    }
}

/// <summary>
///     image abstraction
/// </summary>
public abstract class PureImage : IDisposable
{
    public MemoryStream Data;

    internal bool m_IsParent;
    internal long m_Ix;
    internal long m_Xoff;
    internal long m_Yoff;

    #region IDisposable Members

    public abstract void Dispose();

    #endregion
}

public class DefaultImageProxy : PureImageProxy
{
    public static readonly DefaultImageProxy Instance = new();

    private DefaultImageProxy()
    {
    }

    public override PureImage FromStream(Stream stream) => new DefaultImage();

    public override bool Save(Stream stream, PureImage image)
    {
        if (image.Data != null)
        {
            image.Data.WriteTo(stream);
            image.Data.Position = 0;
            return true;
        }

        return false;
    }

    private class DefaultImage : PureImage
    {
        public override void Dispose()
        {
        }
    }
}
