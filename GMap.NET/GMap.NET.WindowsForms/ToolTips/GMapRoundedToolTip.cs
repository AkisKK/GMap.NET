﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;

namespace GMap.NET.WindowsForms.ToolTips;

/// <summary>
///     GMap.NET marker
/// </summary>
[Serializable]
public class GMapRoundedToolTip : GMapToolTip, ISerializable
{
    public float Radius = 10f;

    public GMapRoundedToolTip(GMapMarker marker)
        : base(marker)
    {
        TextPadding = new Size((int)Radius, (int)Radius);
    }

    public new void DrawRoundRectangle(Graphics g, Pen pen, float h, float v, float width, float height, float radius)
    {
        using var gp = new GraphicsPath();
        gp.AddLine(h + radius, v, h + width - radius * 2, v);
        gp.AddArc(h + width - radius * 2, v, radius * 2, radius * 2, 270, 90);
        gp.AddLine(h + width, v + radius, h + width, v + height - radius * 2);
        gp.AddArc(h + width - radius * 2, v + height - radius * 2, radius * 2, radius * 2, 0, 90); // Corner
        gp.AddLine(h + width - radius * 2, v + height, h + radius, v + height);
        gp.AddArc(h, v + height - radius * 2, radius * 2, radius * 2, 90, 90);
        gp.AddLine(h, v + height - radius * 2, h, v + radius);
        gp.AddArc(h, v, radius * 2, radius * 2, 180, 90);

        gp.CloseFigure();

        g.FillPath(Fill, gp);
        g.DrawPath(pen, gp);
    }

    public override void OnRender(Graphics g)
    {
        var st = g.MeasureString(Marker.ToolTipText, Font).ToSize();

        var rect = new Rectangle(Marker.ToolTipPosition.X,
            Marker.ToolTipPosition.Y - st.Height,
            st.Width + TextPadding.Width * 2,
            st.Height + TextPadding.Height);
        rect.Offset(Offset.X, Offset.Y);

        int lineOffset = 0;
        if (!g.VisibleClipBounds.Contains(rect))
        {
            var clippingOffset = new Point();
            if (rect.Right > g.VisibleClipBounds.Right)
            {
                clippingOffset.X = -((rect.Left - Marker.LocalPosition.X) / 2 + rect.Width);
                lineOffset = -(rect.Width - (int)Radius);
            }

            if (rect.Top < g.VisibleClipBounds.Top)
            {
                clippingOffset.Y = ((rect.Bottom - Marker.LocalPosition.Y) + (rect.Height * 2));
            }

            rect.Offset(clippingOffset);
        }

        g.DrawLine(Stroke,
            Marker.ToolTipPosition.X,
            Marker.ToolTipPosition.Y,
            (rect.X - lineOffset) + Radius / 2,
            rect.Y + rect.Height - Radius / 2);

        DrawRoundRectangle(g, Stroke, rect.X, rect.Y, rect.Width, rect.Height, Radius);

        if (Format.Alignment == StringAlignment.Near)
        {
            rect.Offset(TextPadding.Width, 0);
        }

        g.DrawString(Marker.ToolTipText, Font, Foreground, rect, Format);
    }


    #region ISerializable Members

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Radius", Radius);

        base.GetObjectData(info, context);
    }

    protected GMapRoundedToolTip(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        Radius = Extensions.GetStruct(info, "Radius", 10f);
    }

    #endregion
}
