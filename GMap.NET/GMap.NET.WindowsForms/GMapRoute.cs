using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace GMap.NET.WindowsForms;

/// <summary>
///     GMap.NET route
/// </summary>
[Serializable]
public class GMapRoute : MapRoute, ISerializable, IDeserializationCallback, IDisposable
{
    GMapOverlay m_Overlay;

    public GMapOverlay Overlay
    {
        get => m_Overlay;
        internal set => m_Overlay = value;
    }

    private bool m_Visible = true;

    /// <summary>
    ///     is marker visible
    /// </summary>
    public bool IsVisible
    {
        get => m_Visible;
        set
        {
            if (value == m_Visible)
            {
                // Early exit.
                return;
            }

            m_Visible = value;

            if (Overlay != null && Overlay.Control != null)
            {
                if (m_Visible)
                {
                    Overlay.Control.UpdateRouteLocalPosition(this);
                }
                else
                {
                    if (Overlay.Control.IsMouseOverRoute)
                    {
                        Overlay.Control.IsMouseOverRoute = false;
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

    /// <summary>
    ///     Indicates whether the specified point is contained within this System.Drawing.Drawing2D.GraphicsPath
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    internal bool IsInside(int x, int y)
    {
        if (m_GraphicsPath != null)
        {
            return m_GraphicsPath.IsOutlineVisible(x, y, Stroke);
        }

        return false;
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
            m_GraphicsPath.Reset();
        }

        for (int i = 0; i < LocalPoints.Count; i++)
        {
            var p2 = LocalPoints[i];

            if (i == 0)
            {
                m_GraphicsPath.AddLine(p2.X, p2.Y, p2.X, p2.Y);
            }
            else
            {
                var p = m_GraphicsPath.GetLastPoint();
                m_GraphicsPath.AddLine(p.X, p.Y, p2.X, p2.Y);
            }
        }
    }

    public virtual void OnRender(Graphics g)
    {
        if (IsVisible)
        {
            if (m_GraphicsPath != null)
            {
                g.DrawPath(Stroke, m_GraphicsPath);
            }
        }
    }

    public static readonly Pen DefaultStroke = new(Color.FromArgb(144, Color.MidnightBlue));

    /// <summary>
    ///     specifies how the outline is painted
    /// </summary>
    [NonSerialized] public Pen Stroke = DefaultStroke;

    public readonly List<GPoint> LocalPoints = [];

    static GMapRoute()
    {
        DefaultStroke.LineJoin = LineJoin.Round;
        DefaultStroke.Width = 5;
    }

    public GMapRoute(string name)
        : base(name)
    {
    }

    public GMapRoute(IEnumerable<PointLatLng> points, string name)
        : base(points, name)
    {
    }

    public GMapRoute(MapRoute oRoute)
        : base(oRoute)
    {
    }

    #region ISerializable Members
    // Temp store for de-serialization.
    private readonly GPoint[] m_DeserializedLocalPoints;

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

        info.AddValue("Visible", IsVisible);
        info.AddValue("LocalPoints", LocalPoints.ToArray());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GMapRoute" /> class.
    /// </summary>
    /// <param name="info">The info.</param>
    /// <param name="context">The context.</param>
    protected GMapRoute(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        //this.Stroke = Extensions.GetValue<Pen>(info, "Stroke", new Pen(Color.FromArgb(144, Color.MidnightBlue)));
        IsVisible = Extensions.GetStruct(info, "Visible", true);
        m_DeserializedLocalPoints = Extensions.GetValue<GPoint[]>(info, "LocalPoints");
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
            // Return early.
            return;
        }

        m_Disposed = true;

        LocalPoints.Clear();

        if (m_GraphicsPath != null)
        {
            m_GraphicsPath.Dispose();
            m_GraphicsPath = null;
        }

        Clear();
        GC.SuppressFinalize(this);
    }
    #endregion
}

public delegate void RouteClick(GMapRoute item, MouseEventArgs e);

public delegate void RouteDoubleClick(GMapRoute item, MouseEventArgs e);

public delegate void RouteEnter(GMapRoute item);

public delegate void RouteLeave(GMapRoute item);
