using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;

namespace GMap.NET.WindowsForms;

/// <summary>
///     GMap.NET polygon
/// </summary>
[Serializable]
public class GMapPolygon : MapRoute, ISerializable, IDeserializationCallback, IDisposable
{
    /// <summary>
    /// Lock object for the <see cref="m_GraphicsPath"/> field.
    /// </summary>
    protected readonly Lock m_GraphicsPathLock = new();

    /// <summary>
    /// Lock object for the <see cref="LocalPoints"/> field.
    /// </summary>
    protected readonly Lock m_LocalPointsLock = new();

    private bool m_Visible = true;
    /// <summary>
    ///     is polygon visible
    /// </summary>
    public bool IsVisible
    {
        get => m_Visible;
        set
        {
            if (value != m_Visible)
            {
                m_Visible = value;

                if (Overlay != null && Overlay.Control != null)
                {
                    if (m_Visible)
                    {
                        Overlay.Control.UpdatePolygonLocalPosition(this);
                    }
                    else
                    {
                        if (Overlay.Control.IsMouseOverPolygon)
                        {
                            Overlay.Control.IsMouseOverPolygon = false;
                            Overlay.Control.RestoreCursorOnLeave();
                        }
                    }

                    if (!Overlay.Control.HoldInvalidation)
                    {
                        Overlay.Control.Invalidate();
                    }
                }
            }
        }
    }

    /// <summary>
    ///     can receive input
    /// </summary>
    public bool IsHitTestVisible = false;

    private bool m_IsMouseOver;

    /// <summary>
    ///     is mouse over
    /// </summary>
    public bool IsMouseOver
    {
        get => m_IsMouseOver;
        internal set => m_IsMouseOver = value;
    }

    public GMapOverlay Overlay { get; set; }

    /// <summary>
    ///     Indicates whether the specified point is contained within this System.Drawing.Drawing2D.GraphicsPath
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    internal bool IsInsideLocal(int x, int y)
    {
        if (m_GraphicsPath != null)
        {
            return m_GraphicsPath.IsVisible(x, y);
        }

        return false;
    }

    /// <summary>
    /// Replaces the local points with the provided list of points.
    /// </summary>
    /// <param name="points">The list of points to replace the current local points.</param>
    internal void ReplaceLocalPoints(List<GPoint> points)
    {
        lock (m_LocalPointsLock)
        {
            LocalPoints.Clear();
            LocalPoints.AddRange(points);
        }
    }

    GraphicsPath m_GraphicsPath;
    internal void UpdateGraphicsPath()
    {
        if (m_GraphicsPath == null)
        {
            m_GraphicsPath = new GraphicsPath();
        }
        else
        {
            lock (m_GraphicsPathLock)
            {
                m_GraphicsPath.Reset();
            }
        }

        Point[] points;
        lock (m_LocalPointsLock)
        {
            points = new Point[LocalPoints.Count];
            for (int i = 0; i < LocalPoints.Count; i++)
            {
                var p2 = new Point((int)LocalPoints[i].X, (int)LocalPoints[i].Y);
                points[points.Length - 1 - i] = p2;
            }
        }

        if (points.Length > 2)
        {
            lock (m_GraphicsPathLock)
            {
                m_GraphicsPath.AddPolygon(points);
            }
        }
        else if (points.Length == 2)
        {
            lock (m_GraphicsPathLock)
            {
                m_GraphicsPath.AddLines(points);
            }
        }
    }

    public virtual void OnRender(Graphics g)
    {
        if (!IsVisible)
        {
            return;
        }

        if (m_GraphicsPath != null)
        {
            lock (m_GraphicsPathLock)
            {
                g.FillPath(Fill, m_GraphicsPath);
                g.DrawPath(Stroke, m_GraphicsPath);
            }
        }
    }

    //public double Area
    //{
    //   get
    //   {
    //      return 0;
    //   }
    //}

    public static readonly Pen DefaultStroke = new(Color.FromArgb(155, Color.MidnightBlue));

    /// <summary>
    ///     specifies how the outline is painted
    /// </summary>
    [NonSerialized] public Pen Stroke = DefaultStroke;

    public static readonly Brush DefaultFill = new SolidBrush(Color.FromArgb(155, Color.AliceBlue));

    /// <summary>
    ///     background color
    /// </summary>
    [NonSerialized] public Brush Fill = DefaultFill;

    public readonly List<GPoint> LocalPoints = [];

    static GMapPolygon()
    {
        DefaultStroke.LineJoin = LineJoin.Round;
        DefaultStroke.Width = 5;
    }

    public GMapPolygon(List<PointLatLng> points, string name)
        : base(points, name)
    {
        LocalPoints.Capacity = Points.Count;
    }

    /// <summary>
    ///     checks if point is inside the polygon,
    ///     info.: http://greatmaps.codeplex.com/discussions/279437#post700449
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    public bool IsInside(PointLatLng p)
    {
        int count = Points.Count;

        if (count < 3)
        {
            return false;
        }

        bool result = false;

        for (int i = 0, j = count - 1; i < count; i++)
        {
            var p1 = Points[i];
            var p2 = Points[j];

            if (p1.Lat < p.Lat && p2.Lat >= p.Lat || p2.Lat < p.Lat && p1.Lat >= p.Lat)
            {
                if (p1.Lng + (p.Lat - p1.Lat) / (p2.Lat - p1.Lat) * (p2.Lng - p1.Lng) < p.Lng)
                {
                    result = !result;
                }
            }

            j = i;
        }

        return result;
    }

    #region ISerializable Members
    /// <summary>
    ///     Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with the data needed to serialize the
    ///     target object.
    /// </summary>
    /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> to populate with data.</param>
    /// <param name="context">
    ///     The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext" />) for this
    ///     serialization.
    /// </param>
    /// <exception cref="T:System.Security.SecurityException">
    ///     The caller does not have the required permission.
    /// </exception>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);

        info.AddValue("LocalPoints", LocalPoints.ToArray());
        info.AddValue("Visible", IsVisible);
    }

    // Temp store for de-serialization.
    private readonly GPoint[] m_DeserializedLocalPoints;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MapRoute" /> class.
    /// </summary>
    /// <param name="info">The info.</param>
    /// <param name="context">The context.</param>
    protected GMapPolygon(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        m_DeserializedLocalPoints = Extensions.GetValue<GPoint[]>(info, "LocalPoints");
        IsVisible = Extensions.GetStruct(info, "Visible", true);
    }
    #endregion

    #region IDeserializationCallback Members
    /// <summary>
    ///     Runs when the entire object graph has been de-serialized.
    /// </summary>
    /// <param name="sender">
    ///     The object that initiated the callback. The functionality for this parameter is not currently
    ///     implemented.
    /// </param>
    public override void OnDeserialization(object sender)
    {
        base.OnDeserialization(sender);

        // Accounts for the de-serialization being breadth first rather than depth first.
        LocalPoints.AddRange(m_DeserializedLocalPoints);
        LocalPoints.Capacity = Points.Count;
    }
    #endregion

    #region IDisposable Members
    bool m_Disposed;


    public virtual void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;
        lock (m_LocalPointsLock)
        {
            LocalPoints.Clear();
        }

        if (m_GraphicsPath != null)
        {
            lock (m_GraphicsPathLock)
            {
                m_GraphicsPath.Dispose();
                m_GraphicsPath = null;
            }
        }

        Clear();
        GC.SuppressFinalize(this);
    }
    #endregion
}

public delegate void PolygonClick(GMapPolygon item, MouseEventArgs e);

public delegate void PolygonDoubleClick(GMapPolygon item, MouseEventArgs e);

public delegate void PolygonEnter(GMapPolygon item);

public delegate void PolygonLeave(GMapPolygon item);
