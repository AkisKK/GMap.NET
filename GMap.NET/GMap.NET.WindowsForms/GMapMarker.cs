using System;
using System.Drawing;
using System.Runtime.Serialization;
using System.Windows.Forms;
using GMap.NET.WindowsForms.ToolTips;

namespace GMap.NET.WindowsForms;

/// <summary>
///     GMap.NET marker
/// </summary>
[Serializable]
public abstract class GMapMarker : ISerializable, IDisposable
{
    GMapOverlay m_Overlay;

    public GMapOverlay Overlay
    {
        get => m_Overlay;
        internal set => m_Overlay = value;
    }

    private PointLatLng m_Position;

    public PointLatLng Position
    {
        get => m_Position;
        set
        {
            if (m_Position != value)
            {
                m_Position = value;

                if (IsVisible)
                {
                    if (Overlay != null && Overlay.Control != null)
                    {
                        Overlay.Control.UpdateMarkerLocalPosition(this);
                    }
                }
            }
        }
    }

    public object Tag;

    Point m_Offset;

    public Point Offset
    {
        get => m_Offset;
        set
        {
            if (m_Offset != value)
            {
                m_Offset = value;

                if (IsVisible)
                {
                    if (Overlay != null && Overlay.Control != null)
                    {
                        Overlay.Control.UpdateMarkerLocalPosition(this);
                    }
                }
            }
        }
    }

    Rectangle m_Area;

    /// <summary>
    ///     marker position in local coordinates, internal only, do not set it manualy
    /// </summary>
    public Point LocalPosition
    {
        get => m_Area.Location;
        set
        {
            if (m_Area.Location != value)
            {
                m_Area.Location = value;
                {
                    if (Overlay != null && Overlay.Control != null)
                    {
                        if (!Overlay.Control.HoldInvalidation)
                        {
                            Overlay.Control.Invalidate();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    ///     ToolTip position in local coordinates
    /// </summary>
    public Point ToolTipPosition
    {
        get
        {
            var ret = m_Area.Location;
            ret.Offset(-Offset.X, -Offset.Y);
            return ret;
        }
    }

    public Size Size
    {
        get => m_Area.Size;
        set => m_Area.Size = value;
    }

    public Rectangle LocalArea => m_Area;

    public GMapToolTip ToolTip;

    public MarkerTooltipMode ToolTipMode = MarkerTooltipMode.OnMouseOver;

    string m_ToolTipText;

    public string ToolTipText
    {
        get => m_ToolTipText;
        set
        {
            if (ToolTip == null && !string.IsNullOrEmpty(value))
            {
                ToolTip = new GMapRoundedToolTip(this);
            }

            m_ToolTipText = value;
        }
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
            if (value != m_Visible)
            {
                m_Visible = value;

                if (Overlay != null && Overlay.Control != null)
                {
                    if (m_Visible)
                    {
                        Overlay.Control.UpdateMarkerLocalPosition(this);
                    }
                    else
                    {
                        if (Overlay.Control.IsMouseOverMarker)
                        {
                            Overlay.Control.IsMouseOverMarker = false;
                            Overlay.Control.RestoreCursorOnLeave();
                        }
                    }

                    {
                        if (!Overlay.Control.HoldInvalidation)
                        {
                            Overlay.Control.Invalidate();
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    ///     if true, marker will be rendered even if it's outside current view
    /// </summary>
    public bool DisableRegionCheck;

    /// <summary>
    ///     can maker receive input
    /// </summary>
    public bool IsHitTestVisible = true;

    private bool m_IsMouseOver;

    /// <summary>
    ///     is mouse over marker
    /// </summary>
    public bool IsMouseOver
    {
        get => m_IsMouseOver;
        internal set => m_IsMouseOver = value;
    }

    public GMapMarker(PointLatLng pos)
    {
        Position = pos;
    }

    public virtual void OnRender(Graphics g)
    {
        //
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
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Position", Position);
        info.AddValue("Tag", Tag);
        info.AddValue("Offset", Offset);
        info.AddValue("Area", m_Area);
        info.AddValue("ToolTip", ToolTip);
        info.AddValue("ToolTipMode", ToolTipMode);
        info.AddValue("ToolTipText", ToolTipText);
        info.AddValue("Visible", IsVisible);
        info.AddValue("DisableregionCheck", DisableRegionCheck);
        info.AddValue("IsHitTestVisible", IsHitTestVisible);
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GMapMarker" /> class.
    /// </summary>
    /// <param name="info">The info.</param>
    /// <param name="context">The context.</param>
    protected GMapMarker(SerializationInfo info, StreamingContext context)
    {
        Position = Extensions.GetStruct(info, "Position", PointLatLng.Empty);
        Tag = Extensions.GetValue<object>(info, "Tag", null);
        Offset = Extensions.GetStruct(info, "Offset", Point.Empty);
        m_Area = Extensions.GetStruct(info, "Area", Rectangle.Empty);

        ToolTip = Extensions.GetValue<GMapToolTip>(info, "ToolTip", null);
        if (ToolTip != null) ToolTip.Marker = this;

        ToolTipMode = Extensions.GetStruct(info, "ToolTipMode", MarkerTooltipMode.OnMouseOver);
        ToolTipText = info.GetString("ToolTipText");
        IsVisible = info.GetBoolean("Visible");
        DisableRegionCheck = info.GetBoolean("DisableregionCheck");
        IsHitTestVisible = info.GetBoolean("IsHitTestVisible");
    }
    #endregion

    #region IDisposable Members
    bool m_Disposed;

    public virtual void Dispose()
    {
        if (!m_Disposed)
        {
            m_Disposed = true;

            Tag = null;

            if (ToolTip != null)
            {
                m_ToolTipText = null;
                ToolTip.Dispose();
                ToolTip = null;
            }
        }
        GC.SuppressFinalize(this);
    }
    #endregion
}

public delegate void MarkerClick(GMapMarker item, MouseEventArgs e);

public delegate void MarkerDoubleClick(GMapMarker item, MouseEventArgs e);

public delegate void MarkerEnter(GMapMarker item);

public delegate void MarkerLeave(GMapMarker item);

/// <summary>
///     modeof tooltip
/// </summary>
public enum MarkerTooltipMode
{
    OnMouseOver,
    Never,
    Always,
}
