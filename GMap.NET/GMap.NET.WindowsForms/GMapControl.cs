using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using GMap.NET.WindowsForms.ObjectModel;

namespace GMap.NET.WindowsForms;

/// <summary>
///     GMap.NET control for Windows Forms
/// </summary>
public partial class GMapControl : UserControl, IInterface
{
    /// <summary>
    ///   occurs when clicked on map.
    /// </summary>
    public event MapClick OnMapClick;

    /// <summary>
    ///     occurs when double clicked on map.
    /// </summary>
    public event MapDoubleClick OnMapDoubleClick;

    /// <summary>
    ///     occurs when clicked on marker
    /// </summary>
    public event MarkerClick OnMarkerClick;

    /// <summary>
    ///     occurs when double clicked on marker
    /// </summary>
    public event MarkerDoubleClick OnMarkerDoubleClick;

    /// <summary>
    ///     occurs when clicked on polygon
    /// </summary>
    public event PolygonClick OnPolygonClick;

    /// <summary>
    ///     occurs when double clicked on polygon
    /// </summary>
    public event PolygonDoubleClick OnPolygonDoubleClick;

    /// <summary>
    ///     occurs when clicked on route
    /// </summary>
    public event RouteClick OnRouteClick;

    /// <summary>
    ///     occurs when double clicked on route
    /// </summary>
    public event RouteDoubleClick OnRouteDoubleClick;

    /// <summary>
    ///     occurs on mouse enters route area
    /// </summary>
    public event RouteEnter OnRouteEnter;

    /// <summary>
    ///     occurs on mouse leaves route area
    /// </summary>
    public event RouteLeave OnRouteLeave;

    /// <summary>
    ///     occurs when mouse selection is changed
    /// </summary>
    public event SelectionChange OnSelectionChange;

    /// <summary>
    ///     occurs on mouse enters marker area
    /// </summary>
    public event MarkerEnter OnMarkerEnter;

    /// <summary>
    ///     occurs on mouse leaves marker area
    /// </summary>
    public event MarkerLeave OnMarkerLeave;

    /// <summary>
    ///     occurs on mouse enters Polygon area
    /// </summary>
    public event PolygonEnter OnPolygonEnter;

    /// <summary>
    ///     occurs on mouse leaves Polygon area
    /// </summary>
    public event PolygonLeave OnPolygonLeave;

    /// <summary>
    ///     occurs when an exception is thrown inside the map control
    /// </summary>
    public event ExceptionThrown OnExceptionThrown;

    /// <summary>
    ///     list of overlays, should be thread safe
    /// </summary>
    public readonly ObservableCollectionThreadSafe<GMapOverlay> Overlays = [];

    /// <summary>
    ///     max zoom
    /// </summary>
    [Category("GMap.NET")]
    [Description("maximum zoom level of map")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int MaxZoom
    {
        get => m_Core.m_MaxZoom;
        set => m_Core.m_MaxZoom = value;
    }

    /// <summary>
    ///     min zoom
    /// </summary>
    [Category("GMap.NET")]
    [Description("minimum zoom level of map")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int MinZoom
    {
        get => m_Core.m_MinZoom;
        set => m_Core.m_MinZoom = value;
    }

    /// <summary>
    ///     map zooming type for mouse wheel
    /// </summary>
    [Category("GMap.NET")]
    [Description("map zooming type for mouse wheel")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public MouseWheelZoomType MouseWheelZoomType
    {
        get => m_Core.MouseWheelZoomType;
        set => m_Core.MouseWheelZoomType = value;
    }

    /// <summary>
    ///     enable map zoom on mouse wheel
    /// </summary>
    [Category("GMap.NET")]
    [Description("enable map zoom on mouse wheel")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool MouseWheelZoomEnabled
    {
        get => m_Core.MouseWheelZoomEnabled;
        set => m_Core.MouseWheelZoomEnabled = value;
    }

    /// <summary>
    ///     text on empty tiles
    /// </summary>
    public string EmptyTileText = "We are sorry, but we don't\nhave imagery at this zoom\nlevel for this region.";

    /// <summary>
    ///     pen for empty tile borders
    /// </summary>
    public Pen EmptyTileBorders = new(Brushes.White, 1);

    public bool ShowCenter = true;

    /// <summary>
    ///     pen for scale info
    /// </summary>
    public Pen ScalePen = new(Color.Black, 3);

    public Pen ScalePenBorder = new(Color.WhiteSmoke, 6);
    public Pen CenterPen = new(Brushes.Red, 1);

    /// <summary>
    ///     area selection pen
    /// </summary>
    public Pen SelectionPen = new(Brushes.Blue, 2);

    Brush m_SelectedAreaFill = new SolidBrush(Color.FromArgb(33, Color.RoyalBlue));
    Color m_SelectedAreaFillColor = Color.FromArgb(33, Color.RoyalBlue);

    /// <summary>
    ///     background of selected area
    /// </summary>
    [Category("GMap.NET")]
    [Description("background color of the selected area")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color SelectedAreaFillColor
    {
        get => m_SelectedAreaFillColor;
        set
        {
            if (m_SelectedAreaFillColor != value)
            {
                m_SelectedAreaFillColor = value;

                if (m_SelectedAreaFill != null)
                {
                    m_SelectedAreaFill.Dispose();
                    m_SelectedAreaFill = null;
                }

                m_SelectedAreaFill = new SolidBrush(m_SelectedAreaFillColor);
            }
        }
    }

    HelperLineOptions m_HelperLineOption = HelperLineOptions.DontShow;

    /// <summary>
    ///     draw lines at the mouse pointer position
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public HelperLineOptions HelperLineOption
    {
        get => m_HelperLineOption;
        set
        {
            m_HelperLineOption = value;
            m_RenderHelperLine = m_HelperLineOption == HelperLineOptions.ShowAlways;
            if (m_Core.m_IsStarted)
            {
                Invalidate();
            }
        }
    }

    public Pen HelperLinePen = new(Color.Blue, 1);
    bool m_RenderHelperLine;

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (HelperLineOption == HelperLineOptions.ShowOnModifierKey)
        {
            m_RenderHelperLine = e.Modifiers == Keys.Shift || e.Modifiers == Keys.Alt;
            if (m_RenderHelperLine)
            {
                Invalidate();
            }
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (HelperLineOption == HelperLineOptions.ShowOnModifierKey)
        {
            m_RenderHelperLine = e.Modifiers == Keys.Shift || e.Modifiers == Keys.Alt;
            if (!m_RenderHelperLine)
            {
                Invalidate();
            }
        }
    }

    Brush m_EmptyTileBrush = new SolidBrush(Color.Navy);
    Color m_EmptyTileColor = Color.Navy;

    /// <summary>
    ///     color of empty tile background
    /// </summary>
    [Category("GMap.NET")]
    [Description("background color of the empty tile")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public Color EmptyTileColor
    {
        get => m_EmptyTileColor;
        set
        {
            if (m_EmptyTileColor != value)
            {
                m_EmptyTileColor = value;

                if (m_EmptyTileBrush != null)
                {
                    m_EmptyTileBrush.Dispose();
                    m_EmptyTileBrush = null;
                }

                m_EmptyTileBrush = new SolidBrush(m_EmptyTileColor);
            }
        }
    }

    /// <summary>
    ///     show map scale info
    /// </summary>
    public bool MapScaleInfoEnabled = false;

    /// <summary>
    ///     Position of the map scale info
    /// </summary>
    public MapScaleInfoPosition MapScaleInfoPosition;

    /// <summary>
    ///     enables filling empty tiles using lower level images
    /// </summary>
    public bool FillEmptyTiles = true;

    /// <summary>
    ///     if true, selects area just by holding mouse and moving
    /// </summary>
    public bool DisableAltForSelection = false;

    /// <summary>
    ///     retry count to get tile
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int RetryLoadTile
    {
        get => m_Core.RetryLoadTile;
        set => m_Core.RetryLoadTile = value;
    }

    /// <summary>
    ///     how many levels of tiles are staying decompressed in memory
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public int LevelsKeepInMemory
    {
        get => m_Core.LevelsKeepInMemory;
        set => m_Core.LevelsKeepInMemory = value;
    }

    /// <summary>
    ///     map drag button
    /// </summary>
    [Category("GMap.NET")] public MouseButtons DragButton = MouseButtons.Right;

    private bool m_ShowTileGridLines;

    /// <summary>
    ///     shows tile grid lines
    /// </summary>
    [Category("GMap.NET")]
    [Description("shows tile grid lines")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ShowTileGridLines
    {
        get => m_ShowTileGridLines;
        set
        {
            m_ShowTileGridLines = value;
            Invalidate();
        }
    }

    /// <summary>
    ///     current selected area in map
    /// </summary>
    private RectLatLng m_SelectedArea;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RectLatLng SelectedArea
    {
        get => m_SelectedArea;
        set
        {
            m_SelectedArea = value;

            if (m_Core.m_IsStarted)
            {
                Invalidate();
            }
        }
    }

    /// <summary>
    ///     map boundaries
    /// </summary>
    public RectLatLng? BoundsOfMap = null;

    /// <summary>
    ///     enables integrated DoubleBuffer for running on windows mobile
    /// </summary>
    public bool ForceDoubleBuffer;

    readonly bool m_MobileMode = false;

    /// <summary>
    ///     stops immediate marker/route/polygon invalidations;
    ///     call Refresh to perform single refresh and reset invalidation state
    /// </summary>
    public bool HoldInvalidation;

    /// <summary>
    ///     call this to stop HoldInvalidation and perform single forced instant refresh
    /// </summary>
    public override void Refresh()
    {
        HoldInvalidation = false;

        lock (m_Core.m_InvalidationLock)
        {
            m_Core.m_LastInvalidation = DateTime.Now;
        }

        base.Refresh();
    }

#if !DESIGN
    /// <summary>
    ///     enqueue built-in thread safe invalidation
    /// </summary>
    public new void Invalidate()
    {
        m_Core.Refresh?.Set();
    }
#endif

    private bool m_GrayScale;

    [Category("GMap.NET")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool GrayScaleMode
    {
        get => m_GrayScale;
        set
        {
            m_GrayScale = value;
            ColorMatrix = value ? ColorMatrixs.GrayScale : null;
        }
    }

    private bool m_Negative;

    [Category("GMap.NET")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool NegativeMode
    {
        get => m_Negative;
        set
        {
            m_Negative = value;
            ColorMatrix = value ? ColorMatrixs.Negative : null;
        }
    }

    ColorMatrix m_ColorMatrix;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public ColorMatrix ColorMatrix
    {
        get => m_ColorMatrix;
        set
        {
            m_ColorMatrix = value;
            if (GMapProvider.m_TileImageProxy != null && GMapProvider.m_TileImageProxy is GMapImageProxy)
            {
                (GMapProvider.m_TileImageProxy as GMapImageProxy).m_ColorMatrix = value;
                if (m_Core.m_IsStarted)
                {
                    ReloadMap();
                }
            }
        }
    }

    // internal stuff
    internal readonly Core m_Core = new();

    internal readonly Font m_CopyrightFont = new(FontFamily.GenericSansSerif, 7, FontStyle.Regular);
    internal readonly Font m_MissingDataFont = new(FontFamily.GenericSansSerif, 11, FontStyle.Bold);
    readonly Font m_ScaleFont = new(FontFamily.GenericSansSerif, 7, FontStyle.Regular);
    internal readonly StringFormat m_CenterFormat = new();
    internal readonly StringFormat m_BottomFormat = new();
    readonly ImageAttributes m_TileFlipXYAttributes = new();

    double m_ZoomReal;
    Bitmap m_BackBuffer;
    Graphics m_GxOff;

#if !DESIGN
    /// <summary>
    ///     construct
    /// </summary>
    public GMapControl()
    {
        if (!IsDesignerHosted)
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            ResizeRedraw = true;

            m_TileFlipXYAttributes.SetWrapMode(WrapMode.TileFlipXY);

            // only one mode will be active, to get mixed mode create new ColorMatrix
            // GrayScaleMode = GrayScaleMode;
            // NegativeMode = NegativeMode;
            bool value = GrayScaleMode;
            GrayScaleMode = value;
            value = NegativeMode;
            NegativeMode = value;
            m_Core.m_SystemType = "WindowsForms";

            RenderMode = RenderMode.GDI_PLUS;

            m_CenterFormat.Alignment = StringAlignment.Center;
            m_CenterFormat.LineAlignment = StringAlignment.Center;

            m_BottomFormat.Alignment = StringAlignment.Center;

            m_BottomFormat.LineAlignment = StringAlignment.Far;

            if (GMaps.Instance.IsRunningOnMono)
            {
                // no imports to move pointer
                MouseWheelZoomType = MouseWheelZoomType.MousePositionWithoutCenter;
            }

            Overlays.CollectionChanged += Overlays_CollectionChanged;
        }
    }

#endif

    static GMapControl()
    {
        if (!IsDesignerHosted)
        {
            GMapImageProxy.Enable();
            GMaps.SQLitePing();
        }
    }

    void Overlays_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GMapOverlay obj in e.NewItems)
            {
                if (obj != null)
                {
                    obj.Control = this;
                }
            }

            if (m_Core.m_IsStarted && !HoldInvalidation)
            {
                Invalidate();
            }
        }
    }

    void InvalidatorEngage(object sender, ProgressChangedEventArgs e)
    {
        base.Invalidate();
    }

    /// <summary>
    ///     update objects when map is dragged/zoomed
    /// </summary>
    internal void ForceUpdateOverlays()
    {
        try
        {
            HoldInvalidation = true;

            foreach (var o in Overlays)
            {
                if (o.IsVisibile)
                {
                    o.ForceUpdate();
                }
            }
        }
        finally
        {
            Refresh();
        }
    }

    /// <summary>
    ///     updates markers local position
    /// </summary>
    /// <param name="marker"></param>
    public void UpdateMarkerLocalPosition(GMapMarker marker)
    {
        var p = FromLatLngToLocal(marker.Position);
        {
            if (!m_MobileMode)
            {
                p.OffsetNegative(m_Core.m_RenderOffset);
            }

            marker.LocalPosition = new Point((int)(p.X + marker.Offset.X), (int)(p.Y + marker.Offset.Y));
        }
    }

    /// <summary>
    ///     updates routes local position
    /// </summary>
    /// <param name="route"></param>
    public void UpdateRouteLocalPosition(GMapRoute route)
    {
        route.LocalPoints.Clear();

        for (int i = 0; i < route.Points.Count; i++)
        {
            var p = FromLatLngToLocal(route.Points[i]);

            if (!m_MobileMode)
            {
                p.OffsetNegative(m_Core.m_RenderOffset);
            }

            route.LocalPoints.Add(p);
        }

        route.UpdateGraphicsPath();
    }

    /// <summary>
    ///     updates polygons local position
    /// </summary>
    /// <param name="polygon"></param>
    public void UpdatePolygonLocalPosition(GMapPolygon polygon)
    {
        List<GPoint> points = [];

        for (int i = 0; i < polygon.Points.Count; i++)
        {
            var p = FromLatLngToLocal(polygon.Points[i]);
            if (!m_MobileMode)
            {
                p.OffsetNegative(m_Core.m_RenderOffset);
            }

            points.Add(p);
        }

        polygon.ReplaceLocalPoints(points);
        polygon.UpdateGraphicsPath();
    }

    /// <summary>
    ///     sets zoom to max to fit rect
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public bool SetZoomToFitRect(RectLatLng rect)
    {
        if (m_LazyEvents)
        {
            m_LazySetZoomToFitRect = rect;
        }
        else
        {
            int maxZoom = m_Core.GetMaxZoomToFitRect(rect);
            if (maxZoom > 0)
            {
                var center = new PointLatLng(rect.Lat - rect.HeightLat / 2, rect.Lng + rect.WidthLng / 2);
                Position = center;

                if (maxZoom > MaxZoom)
                {
                    maxZoom = MaxZoom;
                }

                if ((int)Zoom != maxZoom)
                {
                    Zoom = maxZoom;
                }

                return true;
            }
        }

        return false;
    }

    RectLatLng? m_LazySetZoomToFitRect;
    bool m_LazyEvents = true;

    /// <summary>
    ///     sets to max zoom to fit all markers and centers them in map
    /// </summary>
    /// <param name="overlayId">overlay id or null to check all</param>
    /// <returns></returns>
    public bool ZoomAndCenterMarkers(string overlayId)
    {
        var rect = GetRectOfAllMarkers(overlayId);
        if (rect.HasValue)
        {
            return SetZoomToFitRect(rect.Value);
        }

        return false;
    }

    /// <summary>
    ///     zooms and centers all route
    /// </summary>
    /// <param name="overlayId">overlay id or null to check all</param>
    /// <returns></returns>
    public bool ZoomAndCenterRoutes(string overlayId)
    {
        var rect = GetRectOfAllRoutes(overlayId);
        if (rect.HasValue)
        {
            return SetZoomToFitRect(rect.Value);
        }

        return false;
    }

    /// <summary>
    ///     zooms and centers route
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    public bool ZoomAndCenterRoute(MapRoute route)
    {
        var rect = GetRectOfRoute(route);
        if (rect.HasValue)
        {
            return SetZoomToFitRect(rect.Value);
        }

        return false;
    }

    /// <summary>
    ///     gets rectangle with all objects inside
    /// </summary>
    /// <param name="overlayId">overlay id or null to check all except zoomInsignificant</param>
    /// <returns></returns>
    public RectLatLng? GetRectOfAllMarkers(string overlayId)
    {
        RectLatLng? ret = null;

        double left = double.MaxValue;
        double top = double.MinValue;
        double right = double.MinValue;
        double bottom = double.MaxValue;

        foreach (var o in Overlays)
        {
            if (overlayId == null && o.IsZoomSignificant || o.Id == overlayId)
            {
                if (o.IsVisibile && o.Markers.Count > 0)
                {
                    foreach (var m in o.Markers)
                    {
                        if (m.IsVisible)
                        {
                            // left
                            if (m.Position.Lng < left)
                            {
                                left = m.Position.Lng;
                            }

                            // top
                            if (m.Position.Lat > top)
                            {
                                top = m.Position.Lat;
                            }

                            // right
                            if (m.Position.Lng > right)
                            {
                                right = m.Position.Lng;
                            }

                            // bottom
                            if (m.Position.Lat < bottom)
                            {
                                bottom = m.Position.Lat;
                            }
                        }
                    }
                }
            }
        }

        if (left != double.MaxValue && right != double.MinValue && top != double.MinValue &&
            bottom != double.MaxValue)
        {
            ret = RectLatLng.FromLTRB(left, top, right, bottom);
        }

        return ret;
    }

    /// <summary>
    ///     gets rectangle with all objects inside
    /// </summary>
    /// <param name="overlayId">overlay id or null to check all except zoomInsignificant</param>
    /// <returns></returns>
    public RectLatLng? GetRectOfAllRoutes(string overlayId)
    {
        RectLatLng? ret = null;

        double left = double.MaxValue;
        double top = double.MinValue;
        double right = double.MinValue;
        double bottom = double.MaxValue;

        foreach (var o in Overlays)
        {
            if (overlayId == null && o.IsZoomSignificant || o.Id == overlayId)
            {
                if (o.IsVisibile && o.Routes.Count > 0)
                {
                    foreach (var route in o.Routes)
                    {
                        if (route.IsVisible && route.From.HasValue && route.To.HasValue)
                        {
                            foreach (var p in route.Points)
                            {
                                // left
                                if (p.Lng < left)
                                {
                                    left = p.Lng;
                                }

                                // top
                                if (p.Lat > top)
                                {
                                    top = p.Lat;
                                }

                                // right
                                if (p.Lng > right)
                                {
                                    right = p.Lng;
                                }

                                // bottom
                                if (p.Lat < bottom)
                                {
                                    bottom = p.Lat;
                                }
                            }
                        }
                    }
                }
            }
        }

        if (left != double.MaxValue && right != double.MinValue && top != double.MinValue &&
            bottom != double.MaxValue)
        {
            ret = RectLatLng.FromLTRB(left, top, right, bottom);
        }

        return ret;
    }

    /// <summary>
    ///     gets rect of route
    /// </summary>
    /// <param name="route"></param>
    /// <returns></returns>
    public static RectLatLng? GetRectOfRoute(MapRoute route)
    {
        RectLatLng? ret = null;

        double left = double.MaxValue;
        double top = double.MinValue;
        double right = double.MinValue;
        double bottom = double.MaxValue;

        if (route.From.HasValue && route.To.HasValue)
        {
            foreach (var p in route.Points)
            {
                // left
                if (p.Lng < left)
                {
                    left = p.Lng;
                }

                // top
                if (p.Lat > top)
                {
                    top = p.Lat;
                }

                // right
                if (p.Lng > right)
                {
                    right = p.Lng;
                }

                // bottom
                if (p.Lat < bottom)
                {
                    bottom = p.Lat;
                }
            }

            ret = RectLatLng.FromLTRB(left, top, right, bottom);
        }

        return ret;
    }

    /// <summary>
    ///     gets image of the current view
    /// </summary>
    /// <returns></returns>
    public Image ToImage()
    {
        Image ret = null;

        bool r = ForceDoubleBuffer;
        try
        {
            UpdateBackBuffer();

            if (!r)
            {
                ForceDoubleBuffer = true;
            }

            Refresh();
            Application.DoEvents();

            using var ms = new MemoryStream();
            using (var frame = m_BackBuffer.Clone() as Bitmap)
            {
                frame.Save(ms, ImageFormat.Png);
            }

            ret = Image.FromStream(ms);
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            if (!r)
            {
                ForceDoubleBuffer = false;
                ClearBackBuffer();
            }
        }

        return ret;
    }

    /// <summary>
    ///     offset position in pixels
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void Offset(int x, int y)
    {
        if (IsHandleCreated)
        {
            if (IsRotated)
            {
                var p = new[] { new Point(x, y) };
                m_RotationMatrixInvert.TransformVectors(p);
                x = p[0].X;
                y = p[0].Y;
            }

            m_Core.DragOffset(new GPoint(x, y));

            ForceUpdateOverlays();
        }
    }

    /// <summary>
    ///     Obtains the orientation between two points expressed in degrees
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="endPoint"></param>
    /// <returns></returns>
    public static double GetBearing(PointLatLng startPoint, PointLatLng endPoint)
    {
        //double startLat, double startLong, double endLat, double endLong
        double startLat = Radians(startPoint.Lat);
        double startLong = Radians(startPoint.Lng);
        double endLat = Radians(endPoint.Lat);
        double endLong = Radians(endPoint.Lng);

        double dLong = endLong - startLong;

        double dPhi = Math.Log(Math.Tan(endLat / 2.0 + Math.PI / 4.0) / Math.Tan(startLat / 2.0 + Math.PI / 4.0));
        if (Math.Abs(dLong) > Math.PI)
        {
            if (dLong > 0.0)
            {
                dLong = -(2.0 * Math.PI - dLong);
            }
            else
            {
                dLong = 2.0 * Math.PI + dLong;
            }
        }

        return Math.Round((Degrees(Math.Atan2(dLong, dPhi)) + 360.0) % 360.0, 2);
    }

    /// <summary>
    ///     check if a given point is within the given point based map boundary
    /// </summary>
    /// <param name="points"></param>
    /// <param name="lat"></param>
    /// <param name="lng"></param>
    /// <returns></returns>
    public static bool IsPointInBoundary(List<PointLatLng> points, string lat, string lng)
    {
        var polyOverlay = new GMapOverlay();
        var polygon = new GMapPolygon(points, "routePloygon")
        {
            Fill = new SolidBrush(Color.FromArgb(50, Color.Red)),
            Stroke = new Pen(Color.Red, 1)
        };
        polyOverlay.Polygons.Add(polygon);
        var pnt = new PointLatLng(double.Parse(lat), double.Parse(lng));
        return polygon.IsInside(pnt);
    }

    static double Radians(double n) => n * (Math.PI / 180);

    static double Degrees(double n) => n * (180 / Math.PI);

    #region UserControl Events
    public static readonly bool IsDesignerHosted = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

    protected override void OnLoad(EventArgs e)
    {
        try
        {
            base.OnLoad(e);

            if (!IsDesignerHosted)
            {
                //MethodInvoker m = delegate
                //{
                //   Thread.Sleep(444);

                //OnSizeChanged(null);

                if (m_LazyEvents)
                {
                    m_LazyEvents = false;

                    if (m_LazySetZoomToFitRect.HasValue)
                    {
                        SetZoomToFitRect(m_LazySetZoomToFitRect.Value);
                        m_LazySetZoomToFitRect = null;
                    }
                }

                m_Core.OnMapOpen().ProgressChanged += InvalidatorEngage;
                ForceUpdateOverlays();
                //};
                //this.BeginInvoke(m);
            }
        }
        catch (Exception ex)
        {
            if (OnExceptionThrown != null)
                OnExceptionThrown.Invoke(ex);
            else
                throw;
        }
    }

    protected override void OnCreateControl()
    {
        try
        {
            base.OnCreateControl();

            if (!IsDesignerHosted)
            {
                var f = ParentForm;
                if (f != null)
                {
                    while (f.ParentForm != null)
                    {
                        f = f.ParentForm;
                    }

                    if (f != null)
                    {
                        f.FormClosing += ParentForm_FormClosing;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (OnExceptionThrown != null)
                OnExceptionThrown.Invoke(ex);
            else
                throw;
        }
    }

    void ParentForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.WindowsShutDown || e.CloseReason == CloseReason.TaskManagerClosing)
        {
            Manager.CancelTileCaching();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_Core.OnMapClose();

            Overlays.CollectionChanged -= Overlays_CollectionChanged;

            foreach (var o in Overlays)
            {
                o.Dispose();
            }

            Overlays.Clear();

            m_ScaleFont.Dispose();
            ScalePen.Dispose();
            m_CenterFormat.Dispose();
            CenterPen.Dispose();
            m_BottomFormat.Dispose();
            m_CopyrightFont.Dispose();
            EmptyTileBorders.Dispose();
            m_EmptyTileBrush.Dispose();

            m_SelectedAreaFill.Dispose();
            SelectionPen.Dispose();
            ClearBackBuffer();
        }

        base.Dispose(disposing);
    }

    PointLatLng m_SelectionStart;
    PointLatLng m_SelectionEnd;

    float? m_MapRenderTransform;

    public Color EmptyMapBackground = Color.WhiteSmoke;

#if !DESIGN
    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            if (ForceDoubleBuffer)
            {
                if (m_GxOff != null)
                {
                    DrawGraphics(m_GxOff);
                    e.Graphics.DrawImage(m_BackBuffer, 0, 0);
                }
            }
            else
            {
                DrawGraphics(e.Graphics);
            }

            base.OnPaint(e);
        }
        catch (Exception ex)
        {
            if (OnExceptionThrown != null)
                OnExceptionThrown.Invoke(ex);
            else
                throw;
        }
    }

    void DrawGraphics(Graphics g)
    {
        // render white background
        g.Clear(EmptyMapBackground);

        if (m_MapRenderTransform.HasValue)
        {
            #region -- scale --

            if (!m_MobileMode)
            {
                var center = new GPoint(Width / 2, Height / 2);
                var delta = center;
                delta.OffsetNegative(m_Core.m_RenderOffset);
                var pos = center;
                pos.OffsetNegative(delta);

                g.ScaleTransform(m_MapRenderTransform.Value, m_MapRenderTransform.Value, MatrixOrder.Append);
                g.TranslateTransform(pos.X, pos.Y, MatrixOrder.Append);

                DrawMap(g);
                g.ResetTransform();

                g.TranslateTransform(pos.X, pos.Y, MatrixOrder.Append);
            }
            else
            {
                DrawMap(g);
                g.ResetTransform();
            }

            OnPaintOverlays(g);

            #endregion
        }
        else
        {
            if (IsRotated)
            {
                #region -- rotation --

                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                g.TranslateTransform((float)(m_Core.m_Width / 2.0), (float)(m_Core.m_Height / 2.0));
                g.RotateTransform(-Bearing);
                g.TranslateTransform((float)(-m_Core.m_Width / 2.0), (float)(-m_Core.m_Height / 2.0));

                g.TranslateTransform(m_Core.m_RenderOffset.X, m_Core.m_RenderOffset.Y);

                DrawMap(g);

                g.ResetTransform();
                g.TranslateTransform(m_Core.m_RenderOffset.X, m_Core.m_RenderOffset.Y);

                OnPaintOverlays(g);

                #endregion
            }
            else
            {
                if (!m_MobileMode)
                {
                    g.TranslateTransform(m_Core.m_RenderOffset.X, m_Core.m_RenderOffset.Y);
                }

                DrawMap(g);
                OnPaintOverlays(g);
            }
        }
    }
#endif

    void DrawMap(Graphics g)
    {
        if (m_Core.UpdatingBounds || MapProvider == EmptyProvider.Instance || MapProvider == null)
        {
            Debug.WriteLine("Core.updatingBounds");
            return;
        }

        m_Core.m_TileDrawingListLock.AcquireReaderLock();
        m_Core.Matrix.EnterReadLock();

        //g.TextRenderingHint = TextRenderingHint.AntiAlias;
        //g.SmoothingMode = SmoothingMode.AntiAlias;
        //g.CompositingQuality = CompositingQuality.HighQuality;
        //g.InterpolationMode = InterpolationMode.HighQualityBicubic;  

        try
        {
            foreach (var tilePoint in m_Core.m_TileDrawingList)
            {
                {
                    m_Core.m_TileRect.Location = tilePoint.PosPixel;
                    if (ForceDoubleBuffer)
                    {
                        if (m_MobileMode)
                        {
                            m_Core.m_TileRect.Offset(m_Core.m_RenderOffset);
                        }
                    }

                    m_Core.m_TileRect.OffsetNegative(m_Core.m_CompensationOffset);

                    //if(Core.currentRegion.IntersectsWith(Core.tileRect) || IsRotated)
                    {
                        bool found = false;

                        var t = m_Core.Matrix.GetTileWithNoLock(m_Core.Zoom, tilePoint.PosXY);
                        if (t.NotEmpty)
                        {
                            // render tile
                            {
                                foreach (var img in t.Overlays.Cast<GMapImage>())
                                {
                                    if (img != null && img.Img != null)
                                    {
                                        if (!found)
                                            found = true;

                                        if (!img.m_IsParent)
                                        {
                                            if (!m_MapRenderTransform.HasValue && !IsRotated)
                                            {
                                                g.DrawImage(img.Img,
                                                    m_Core.m_TileRect.X,
                                                    m_Core.m_TileRect.Y,
                                                    m_Core.m_TileRect.Width,
                                                    m_Core.m_TileRect.Height);
                                            }
                                            else
                                            {
                                                g.DrawImage(img.Img,
                                                    new Rectangle((int)m_Core.m_TileRect.X,
                                                        (int)m_Core.m_TileRect.Y,
                                                        (int)m_Core.m_TileRect.Width,
                                                        (int)m_Core.m_TileRect.Height),
                                                    0,
                                                    0,
                                                    m_Core.m_TileRect.Width,
                                                    m_Core.m_TileRect.Height,
                                                    GraphicsUnit.Pixel,
                                                    m_TileFlipXYAttributes);
                                            }
                                        }
                                        else
                                        {
                                            // TODO: move calculations to loader thread
                                            var srcRect = new RectangleF(
                                                img.m_Xoff * (img.Img.Width / img.m_Ix),
                                                img.m_Yoff * (img.Img.Height / img.m_Ix),
                                                img.Img.Width / img.m_Ix,
                                                img.Img.Height / img.m_Ix);
                                            var dst = new Rectangle((int)m_Core.m_TileRect.X,
                                                (int)m_Core.m_TileRect.Y,
                                                (int)m_Core.m_TileRect.Width,
                                                (int)m_Core.m_TileRect.Height);

                                            g.DrawImage(img.Img,
                                                dst,
                                                srcRect.X,
                                                srcRect.Y,
                                                srcRect.Width,
                                                srcRect.Height,
                                                GraphicsUnit.Pixel,
                                                m_TileFlipXYAttributes);
                                        }
                                    }
                                }
                            }
                        }
                        else if (FillEmptyTiles && MapProvider.Projection is MercatorProjection)
                        {
                            #region -- fill empty lines --

                            int zoomOffset = 1;
                            var parentTile = Tile.Empty;
                            long ix = 0;

                            while (!parentTile.NotEmpty && zoomOffset < m_Core.Zoom &&
                                   zoomOffset <= LevelsKeepInMemory)
                            {
                                ix = (long)Math.Pow(2, zoomOffset);
                                parentTile = m_Core.Matrix.GetTileWithNoLock(m_Core.Zoom - zoomOffset++,
                                    new GPoint((int)(tilePoint.PosXY.X / ix), (int)(tilePoint.PosXY.Y / ix)));
                            }

                            if (parentTile.NotEmpty)
                            {
                                long xOff = Math.Abs(tilePoint.PosXY.X - parentTile.Pos.X * ix);
                                long yOff = Math.Abs(tilePoint.PosXY.Y - parentTile.Pos.Y * ix);

                                // render tile 
                                {
                                    foreach (var img in parentTile.Overlays.Cast<GMapImage>())
                                    {
                                        if (img != null && img.Img != null && !img.m_IsParent)
                                        {
                                            if (!found)
                                                found = true;

                                            var srcRect = new RectangleF(
                                                xOff * (img.Img.Width / ix),
                                                yOff * (img.Img.Height / ix),
                                                img.Img.Width / ix,
                                                img.Img.Height / ix);
                                            var dst = new Rectangle((int)m_Core.m_TileRect.X,
                                                (int)m_Core.m_TileRect.Y,
                                                (int)m_Core.m_TileRect.Width,
                                                (int)m_Core.m_TileRect.Height);

                                            g.DrawImage(img.Img,
                                                dst,
                                                srcRect.X,
                                                srcRect.Y,
                                                srcRect.Width,
                                                srcRect.Height,
                                                GraphicsUnit.Pixel,
                                                m_TileFlipXYAttributes);
                                            g.FillRectangle(m_SelectedAreaFill, dst);
                                        }
                                    }
                                }
                            }

                            #endregion
                        }

                        // add text if tile is missing
                        if (!found)
                        {
                            lock (m_Core.m_FailedLoads)
                            {
                                var lt = new LoadTask(tilePoint.PosXY, m_Core.Zoom);
                                if (m_Core.m_FailedLoads.TryGetValue(lt, out var ex))
                                {
                                    g.FillRectangle(m_EmptyTileBrush,
                                        new RectangleF(m_Core.m_TileRect.X,
                                            m_Core.m_TileRect.Y,
                                            m_Core.m_TileRect.Width,
                                            m_Core.m_TileRect.Height));

                                    g.DrawString("Exception: " + ex.Message,
                                        m_MissingDataFont,
                                        Brushes.Red,
                                        new RectangleF(m_Core.m_TileRect.X + 11,
                                            m_Core.m_TileRect.Y + 11,
                                            m_Core.m_TileRect.Width - 11,
                                            m_Core.m_TileRect.Height - 11));

                                    g.DrawString(EmptyTileText,
                                        m_MissingDataFont,
                                        Brushes.Blue,
                                        new RectangleF(m_Core.m_TileRect.X,
                                            m_Core.m_TileRect.Y,
                                            m_Core.m_TileRect.Width,
                                            m_Core.m_TileRect.Height),
                                        m_CenterFormat);

                                    g.DrawRectangle(EmptyTileBorders,
                                        (int)m_Core.m_TileRect.X,
                                        (int)m_Core.m_TileRect.Y,
                                        (int)m_Core.m_TileRect.Width,
                                        (int)m_Core.m_TileRect.Height);
                                }
                            }
                        }

                        if (ShowTileGridLines)
                        {
                            g.DrawRectangle(EmptyTileBorders,
                                (int)m_Core.m_TileRect.X,
                                (int)m_Core.m_TileRect.Y,
                                (int)m_Core.m_TileRect.Width,
                                (int)m_Core.m_TileRect.Height);
                            {
                                g.DrawString(
                                    (tilePoint.PosXY == m_Core.m_CenterTileXYLocation ? "CENTER: " : "TILE: ") +
                                    tilePoint,
                                    m_MissingDataFont,
                                    Brushes.Red,
                                    new RectangleF(m_Core.m_TileRect.X,
                                        m_Core.m_TileRect.Y,
                                        m_Core.m_TileRect.Width,
                                        m_Core.m_TileRect.Height),
                                    m_CenterFormat);
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            m_Core.Matrix.LeaveReadLock();
            m_Core.m_TileDrawingListLock.ReleaseReaderLock();
        }
    }

    /// <summary>
    ///     override, to render something more
    /// </summary>
    /// <param name="g"></param>
    protected virtual void OnPaintOverlays(Graphics g)
    {
        try
        {
            g.SmoothingMode = SmoothingMode.HighQuality;
            foreach (var o in Overlays)
            {
                if (o.IsVisibile)
                {
                    o.OnRender(g);
                }
            }

            // separate tooltip drawing
            foreach (var o in Overlays)
            {
                if (o.IsVisibile)
                {
                    o.OnRenderToolTips(g);
                }
            }

            // center in virtual space...
#if DEBUG
            if (!IsRotated)
            {
                g.DrawLine(ScalePen, -20, 0, 20, 0);
                g.DrawLine(ScalePen, 0, -20, 0, 20);
                g.DrawString("debug build", m_CopyrightFont, Brushes.Blue, 2, m_CopyrightFont.Height);
            }
#endif

            if (!m_MobileMode)
            {
                g.ResetTransform();
            }

            if (!SelectedArea.IsEmpty)
            {
                var p1 = FromLatLngToLocal(SelectedArea.LocationTopLeft);
                var p2 = FromLatLngToLocal(SelectedArea.LocationRightBottom);

                long x1 = p1.X;
                long y1 = p1.Y;
                long x2 = p2.X;
                long y2 = p2.Y;

                g.DrawRectangle(SelectionPen, x1, y1, x2 - x1, y2 - y1);
                g.FillRectangle(m_SelectedAreaFill, x1, y1, x2 - x1, y2 - y1);
            }

            if (m_RenderHelperLine)
            {
                var p = PointToClient(MousePosition);

                g.DrawLine(HelperLinePen, p.X, 0, p.X, Height);
                g.DrawLine(HelperLinePen, 0, p.Y, Width, p.Y);
            }

            if (ShowCenter)
            {
                g.DrawLine(CenterPen, Width / 2 - 5, Height / 2, Width / 2 + 5, Height / 2);
                g.DrawLine(CenterPen, Width / 2, Height / 2 - 5, Width / 2, Height / 2 + 5);
            }

            #region -- copyright --

            if (!string.IsNullOrEmpty(m_Core.Provider.Copyright))
            {
                g.DrawString(m_Core.Provider.Copyright,
                    m_CopyrightFont,
                    Brushes.Navy,
                    3,
                    Height - m_CopyrightFont.Height - 5);
            }

            #endregion

            #region -- draw scale --

            if (MapScaleInfoEnabled)
            {
                int top = MapScaleInfoPosition == MapScaleInfoPosition.Top ? 10 : Bottom - 30;
                int left = 10;
                int bottom = top + 7;

                if (Width > m_Core.m_PxRes5000Km)
                {
                    DrawScale(g, top, left + m_Core.m_PxRes5000Km, bottom, left, "5000 km");
                }

                if (Width > m_Core.m_PxRes1000Km)
                {
                    DrawScale(g, top, left + m_Core.m_PxRes1000Km, bottom, left, "1000 km");
                }

                if (Width > m_Core.m_PxRes100Km && Zoom > 2)
                {
                    DrawScale(g, top, left + m_Core.m_PxRes100Km, bottom, left, "100 km");
                }

                if (Width > m_Core.m_PxRes10Km && Zoom > 5)
                {
                    DrawScale(g, top, left + m_Core.m_PxRes10Km, bottom, left, "10 km");
                }

                if (Width > m_Core.m_PxRes1000M && Zoom >= 10)
                {
                    DrawScale(g, top, left + m_Core.m_PxRes1000M, bottom, left, "1000 m");
                }

                if (Width > m_Core.m_PxRes100M && Zoom > 11)
                {
                    DrawScale(g, top, left + m_Core.m_PxRes100M, bottom, left, "100 m");
                }
            }

            #endregion
        }
        catch (Exception ex)
        {
            if (OnExceptionThrown != null)
            {
                OnExceptionThrown.Invoke(ex);
            }
            else
            {
                throw;
            }
        }
    }

    private void DrawScale(Graphics g, int top, int right, int bottom, int left, string caption)
    {
        g.DrawLine(ScalePenBorder, left, top, left, bottom);
        g.DrawLine(ScalePenBorder, left, bottom, right, bottom);
        g.DrawLine(ScalePenBorder, right, bottom, right, top);

        g.DrawLine(ScalePen, left, top, left, bottom);
        g.DrawLine(ScalePen, left, bottom, right, bottom);
        g.DrawLine(ScalePen, right, bottom, right, top);

        g.DrawString(caption, m_ScaleFont, Brushes.Black, right + 3, top - 5);
    }

    readonly Matrix m_RotationMatrix = new();
    readonly Matrix m_RotationMatrixInvert = new();

    /// <summary>
    ///     updates rotation matrix
    /// </summary>
    void UpdateRotationMatrix()
    {
        var center = new PointF(m_Core.m_Width / 2, m_Core.m_Height / 2);

        m_RotationMatrix.Reset();
        m_RotationMatrix.RotateAt(-Bearing, center);

        m_RotationMatrixInvert.Reset();
        m_RotationMatrixInvert.RotateAt(-Bearing, center);
        m_RotationMatrixInvert.Invert();
    }

    /// <summary>
    ///     returns true if map bearing is not zero
    /// </summary>
    [Browsable(false)]
    public bool IsRotated
    {
        get
        {
            return m_Core.IsRotated;
        }
    }

    /// <summary>
    ///     bearing for rotation of the map
    /// </summary>
    [Category("GMap.NET")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float Bearing
    {
        get
        {
            return m_Core.m_Bearing;
        }
        set
        {
            if (m_Core.m_Bearing != value)
            {
                bool resize = m_Core.m_Bearing == 0;
                m_Core.m_Bearing = value;

                //if(VirtualSizeEnabled)
                //{
                // c.X += (Width - Core.vWidth) / 2;
                // c.Y += (Height - Core.vHeight) / 2;
                //}

                UpdateRotationMatrix();

                if (value != 0 && value % 360 != 0)
                {
                    m_Core.IsRotated = true;

                    if (m_Core.m_TileRectBearing.Size == m_Core.m_TileRect.Size)
                    {
                        m_Core.m_TileRectBearing = m_Core.m_TileRect;
                        m_Core.m_TileRectBearing.Inflate(1, 1);
                    }
                }
                else
                {
                    m_Core.IsRotated = false;
                    m_Core.m_TileRectBearing = m_Core.m_TileRect;
                }

                if (resize)
                {
                    m_Core.OnMapSizeChanged(Width, Height);
                }

                if (!HoldInvalidation && m_Core.m_IsStarted)
                {
                    ForceUpdateOverlays();
                }
            }
        }
    }

    /// <summary>
    ///     shrinks map area, useful just for testing
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool VirtualSizeEnabled
    {
        get
        {
            return m_Core.VirtualSizeEnabled;
        }
        set
        {
            m_Core.VirtualSizeEnabled = value;
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Width == 0 || Height == 0)
        {
            Debug.WriteLine("minimized");
            return;
        }

        if (Width == m_Core.m_Width && Height == m_Core.m_Height)
        {
            Debug.WriteLine("maximized");
            return;
        }

        if (!IsDesignerHosted)
        {
            if (ForceDoubleBuffer)
            {
                UpdateBackBuffer();
            }

            if (VirtualSizeEnabled)
            {
                m_Core.OnMapSizeChanged(m_Core.m_VWidth, m_Core.m_VHeight);
            }
            else
            {
                m_Core.OnMapSizeChanged(Width, Height);
            }
            //Core.currentRegion = new GRect(-50, -50, Core.Width + 50, Core.Height + 50);

            if (Visible && IsHandleCreated && m_Core.m_IsStarted)
            {
                if (IsRotated)
                {
                    UpdateRotationMatrix();
                }

                ForceUpdateOverlays();
            }
        }
    }

    void UpdateBackBuffer()
    {
        ClearBackBuffer();

        m_BackBuffer = new Bitmap(Width, Height);
        m_GxOff = Graphics.FromImage(m_BackBuffer);
    }

    private void ClearBackBuffer()
    {
        if (m_BackBuffer != null)
        {
            m_BackBuffer.Dispose();
            m_BackBuffer = null;
        }

        if (m_GxOff != null)
        {
            m_GxOff.Dispose();
            m_GxOff = null;
        }
    }

    bool m_IsSelected;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsMouseOverMarker)
        {
            if (e.Button == DragButton && CanDragMap)
            {
                m_Core.m_MouseDown = ApplyRotationInversion(e.X, e.Y);
                Invalidate();
            }
            else if (!m_IsSelected)
            {
                m_IsSelected = true;
                SelectedArea = RectLatLng.Empty;
                m_SelectionEnd = PointLatLng.Empty;
                m_SelectionStart = FromLocalToLatLng(e.X, e.Y);
            }
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        if (m_IsSelected)
        {
            m_IsSelected = false;
        }

        if (m_Core.IsDragging)
        {
            if (m_IsDragging)
            {
                m_IsDragging = false;
                Debug.WriteLine("IsDragging = " + m_IsDragging);
                Cursor = m_CursorBefore;
                m_CursorBefore = null;
            }

            m_Core.EndDrag();

            if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
            {
                if (m_Core.LastLocationInBounds.HasValue)
                {
                    Position = m_Core.LastLocationInBounds.Value;
                }
            }
        }
        else
        {
            if (e.Button == DragButton)
            {
                m_Core.m_MouseDown = GPoint.Empty;
            }

            if (!m_SelectionEnd.IsEmpty && !m_SelectionStart.IsEmpty)
            {
                bool zoomtofit = false;

                if (!SelectedArea.IsEmpty && ModifierKeys == Keys.Shift)
                {
                    zoomtofit = SetZoomToFitRect(SelectedArea);
                }

                OnSelectionChange?.Invoke(SelectedArea, zoomtofit);
            }
            else
            {
                Invalidate();
            }
        }
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);

        if (!m_Core.IsDragging)
        {
            bool overlayObjet = false;

            for (int i = Overlays.Count - 1; i >= 0; i--)
            {
                var o = Overlays[i];

                if (o != null && o.IsVisibile && o.IsHitTestVisible)
                {
                    foreach (var m in o.Markers)
                    {
                        if (m.IsVisible && m.IsHitTestVisible)
                        {
                            #region -- check --
                            var rp = new GPoint(e.X, e.Y);
                            if (!m_MobileMode)
                            {
                                rp.OffsetNegative(m_Core.m_RenderOffset);
                            }

                            if (m.LocalArea.Contains((int)rp.X, (int)rp.Y))
                            {
                                OnMarkerClick?.Invoke(m, e);
                                overlayObjet = true;
                                break;
                            }
                            #endregion
                        }
                    }
                    if (true == overlayObjet)
                    {
                        // Already found an object, exit the outer loop.
                        break;
                    }

                    foreach (var m in o.Routes)
                    {
                        if (m.IsVisible && m.IsHitTestVisible)
                        {
                            #region -- check --
                            var rp = new GPoint(e.X, e.Y);
                            if (!m_MobileMode)
                            {
                                rp.OffsetNegative(m_Core.m_RenderOffset);
                            }

                            if (m.IsInside((int)rp.X, (int)rp.Y))
                            {
                                OnRouteClick?.Invoke(m, e);
                                overlayObjet = true;
                                break;
                            }
                            #endregion
                        }
                    }
                    if (true == overlayObjet)
                    {
                        // Already found an object, exit the outer loop.
                        break;
                    }

                    foreach (var m in o.Polygons)
                    {
                        if (m.IsVisible && m.IsHitTestVisible)
                        {
                            #region -- check --
                            if (m.IsInside(FromLocalToLatLng(e.X, e.Y)))
                            {
                                OnPolygonClick?.Invoke(m, e);
                                overlayObjet = true;
                                break;
                            }
                            #endregion
                        }
                    }
                    if (true == overlayObjet)
                    {
                        // Already found an object, exit the outer loop.
                        break;
                    }
                }
            }

            if (!overlayObjet && m_Core.m_MouseDown != GPoint.Empty)
            {
                OnMapClick?.Invoke(FromLocalToLatLng(e.X, e.Y), e);
            }
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (!m_Core.IsDragging)
        {
            bool overlayObjet = false;

            for (int i = Overlays.Count - 1; i >= 0; i--)
            {
                var o = Overlays[i];

                if (o != null && o.IsVisibile && o.IsHitTestVisible)
                {
                    foreach (var m in o.Markers)
                    {
                        if (m.IsVisible && m.IsHitTestVisible)
                        {
                            #region -- check --
                            var rp = new GPoint(e.X, e.Y);
                            if (!m_MobileMode)
                            {
                                rp.OffsetNegative(m_Core.m_RenderOffset);
                            }

                            if (m.LocalArea.Contains((int)rp.X, (int)rp.Y))
                            {
                                OnMarkerDoubleClick?.Invoke(m, e);
                                overlayObjet = true;
                                break;
                            }
                            #endregion
                        }
                    }

                    foreach (var m in o.Routes)
                    {
                        if (m.IsVisible && m.IsHitTestVisible)
                        {
                            #region -- check --
                            var rp = new GPoint(e.X, e.Y);
                            if (!m_MobileMode)
                            {
                                rp.OffsetNegative(m_Core.m_RenderOffset);
                            }

                            if (m.IsInside((int)rp.X, (int)rp.Y))
                            {
                                OnRouteDoubleClick?.Invoke(m, e);
                                overlayObjet = true;
                                break;
                            }
                            #endregion
                        }
                    }

                    foreach (var m in o.Polygons)
                    {
                        if (m.IsVisible && m.IsHitTestVisible)
                        {
                            #region -- check --
                            if (m.IsInside(FromLocalToLatLng(e.X, e.Y)))
                            {
                                OnPolygonDoubleClick?.Invoke(m, e);
                                overlayObjet = true;
                                break;
                            }
                            #endregion
                        }
                    }
                }
            }

            if (!overlayObjet && m_Core.m_MouseDown != GPoint.Empty)
            {
                OnMapDoubleClick?.Invoke(FromLocalToLatLng(e.X, e.Y), e);
            }
        }
    }

    /// <summary>
    ///     apply transformation if in rotation mode
    /// </summary>
    GPoint ApplyRotationInversion(int x, int y)
    {
        var ret = new GPoint(x, y);

        if (IsRotated)
        {
            var tt = new[] { new Point(x, y) };
            m_RotationMatrixInvert.TransformPoints(tt);
            var f = tt[0];

            ret.X = f.X;
            ret.Y = f.Y;
        }

        return ret;
    }

    ///// <summary>
    /////     apply transformation if in rotation mode
    ///// </summary>
    //GPoint ApplyRotation(int x, int y)
    //{
    //    var ret = new GPoint(x, y);

    //    if (IsRotated)
    //    {
    //        var tt = new[] { new Point(x, y) };
    //        m_RotationMatrix.TransformPoints(tt);
    //        var f = tt[0];

    //        ret.X = f.X;
    //        ret.Y = f.Y;
    //    }

    //    return ret;
    //}

    Cursor m_CursorBefore = Cursors.Default;

    /// <summary>
    ///     Gets the width and height of a rectangle centered on the point the mouse
    ///     button was pressed, within which a drag operation will not begin.
    /// </summary>
    public Size DragSize = SystemInformation.DragSize;

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!m_Core.IsDragging && !m_Core.m_MouseDown.IsEmpty)
        {
            var p = ApplyRotationInversion(e.X, e.Y);
            if (Math.Abs(p.X - m_Core.m_MouseDown.X) * 2 >= DragSize.Width ||
                Math.Abs(p.Y - m_Core.m_MouseDown.Y) * 2 >= DragSize.Height)
            {
                m_Core.BeginDrag(m_Core.m_MouseDown);
            }
        }

        if (m_Core.IsDragging)
        {
            if (!m_IsDragging)
            {
                m_IsDragging = true;
                Debug.WriteLine("IsDragging = " + m_IsDragging);

                m_CursorBefore = Cursor;
                Cursor = Cursors.SizeAll;
            }

            if (BoundsOfMap.HasValue && !BoundsOfMap.Value.Contains(Position))
            {
                // ...
            }
            else
            {
                m_Core.m_MouseCurrent = ApplyRotationInversion(e.X, e.Y);
                m_Core.Drag(m_Core.m_MouseCurrent);
                if (m_MobileMode || IsRotated)
                {
                    ForceUpdateOverlays();
                }

                base.Invalidate();
            }
        }
        else
        {
            if (m_IsSelected && !m_SelectionStart.IsEmpty &&
                (ModifierKeys == Keys.Alt || ModifierKeys == Keys.Shift || DisableAltForSelection))
            {
                m_SelectionEnd = FromLocalToLatLng(e.X, e.Y);
                {
                    var p1 = m_SelectionStart;
                    var p2 = m_SelectionEnd;

                    double x1 = Math.Min(p1.Lng, p2.Lng);
                    double y1 = Math.Max(p1.Lat, p2.Lat);
                    double x2 = Math.Max(p1.Lng, p2.Lng);
                    double y2 = Math.Min(p1.Lat, p2.Lat);

                    SelectedArea = new RectLatLng(y1, x1, x2 - x1, y1 - y2);
                }
            }
            else
            if (m_Core.m_MouseDown.IsEmpty)
            {
                for (int i = Overlays.Count - 1; i >= 0; i--)
                {
                    // This variable tracks whether the mouse is over an object, so as to facilitate loop exits.
                    bool isMouseOverObject = false;
                    var o = Overlays[i];
                    if (o != null && o.IsVisibile && o.IsHitTestVisible)
                    {
                        foreach (var m in o.Markers)
                        {
                            if (m.IsVisible && m.IsHitTestVisible)
                            {
                                #region -- check --

                                var rp = new GPoint(e.X, e.Y);
                                if (!m_MobileMode)
                                {
                                    rp.OffsetNegative(m_Core.m_RenderOffset);
                                }

                                if (m.LocalArea.Contains((int)rp.X, (int)rp.Y))
                                {
                                    if (!m.IsMouseOver)
                                    {
                                        SetCursorHandOnEnter();
                                        m.IsMouseOver = true;
                                        IsMouseOverMarker = true;

                                        OnMarkerEnter?.Invoke(m);

                                        Invalidate();
                                    }
                                    // Found an object, exit the loop.
                                    isMouseOverObject = true;
                                    break;
                                }
                                else if (m.IsMouseOver)
                                {
                                    m.IsMouseOver = false;
                                    IsMouseOverMarker = false;
                                    RestoreCursorOnLeave();
                                    OnMarkerLeave?.Invoke(m);

                                    Invalidate();
                                }
                                #endregion
                            }
                        }
                        if (true == isMouseOverObject)
                        {
                            // Already found an object, exit the outer loop.
                            break;
                        }

                        foreach (var m in o.Routes)
                        {
                            if (m.IsVisible && m.IsHitTestVisible)
                            {
                                #region -- check --
                                var rp = new GPoint(e.X, e.Y);
                                if (!m_MobileMode)
                                {
                                    rp.OffsetNegative(m_Core.m_RenderOffset);
                                }

                                if (m.IsInside((int)rp.X, (int)rp.Y))
                                {
                                    if (!m.IsMouseOver)
                                    {
                                        SetCursorHandOnEnter();
                                        m.IsMouseOver = true;
                                        IsMouseOverRoute = true;

                                        OnRouteEnter?.Invoke(m);

                                        Invalidate();
                                    }
                                    // Found an object, exit the loop.
                                    isMouseOverObject = true;
                                    break;
                                }
                                else
                                {
                                    if (m.IsMouseOver)
                                    {
                                        m.IsMouseOver = false;
                                        IsMouseOverRoute = false;
                                        RestoreCursorOnLeave();
                                        OnRouteLeave?.Invoke(m);

                                        Invalidate();
                                    }
                                }
                                #endregion
                            }
                        }
                        if (true == isMouseOverObject)
                        {
                            // Already found an object, exit the outer loop.
                            break;
                        }

                        foreach (var m in o.Polygons)
                        {
                            if (m.IsVisible && m.IsHitTestVisible)
                            {
                                #region -- check --
                                var rp = new GPoint(e.X, e.Y);

                                if (!m_MobileMode)
                                {
                                    rp.OffsetNegative(m_Core.m_RenderOffset);
                                }

                                if (m.IsInsideLocal((int)rp.X, (int)rp.Y))
                                {
                                    if (!m.IsMouseOver)
                                    {
                                        SetCursorHandOnEnter();
                                        m.IsMouseOver = true;
                                        IsMouseOverPolygon = true;

                                        OnPolygonEnter?.Invoke(m);

                                        Invalidate();
                                    }
                                    // Found an object, exit the loop.
                                    isMouseOverObject = true;
                                    break;
                                }
                                else
                                {
                                    if (m.IsMouseOver)
                                    {
                                        m.IsMouseOver = false;
                                        IsMouseOverPolygon = false;
                                        RestoreCursorOnLeave();
                                        OnPolygonLeave?.Invoke(m);

                                        Invalidate();
                                    }
                                }
                                #endregion
                            }
                        }
                        if (true == isMouseOverObject)
                        {
                            // Already found an object, exit the outer loop.
                            break;
                        }
                    }
                }
            }

            if (m_RenderHelperLine)
            {
                base.Invalidate();
            }
        }
    }

    /// <summary>
    /// Checks whether the specified local coordinates on the map control are over any markers, routes, or polygons.
    /// </summary>
    /// <param name="x">The x value of the coordinates.</param>
    /// <param name="y">The y value of the coordinates.</param>
    /// <returns>A tuple made of three boolean values indicating whether the coordinates are over any object.</returns>
    public (bool isMouseOverMarker, bool isMouseOverRoute, bool isMouseOverPolygon) IsLocalPointOverObjects(int x, int y)
    {
        bool isMouseOverMarker = false;
        bool isMouseOverRoute = false;
        bool isMouseOverPolygon = false;

        for (int i = Overlays.Count - 1; i >= 0; i--)
        {
            var o = Overlays[i];
            if (o != null && o.IsVisibile && o.IsHitTestVisible)
            {
                foreach (var m in o.Markers)
                {
                    if (m.IsVisible && m.IsHitTestVisible)
                    {
                        #region Check
                        var rp = new GPoint(x, y);
                        if (!m_MobileMode)
                        {
                            rp.OffsetNegative(m_Core.m_RenderOffset);
                        }

                        if (m.LocalArea.Contains((int)rp.X, (int)rp.Y))
                        {
                            // Found an object, exit the loop.
                            isMouseOverMarker = true;
                            break;
                        }
                        #endregion
                    }
                }

                foreach (var m in o.Routes)
                {
                    if (m.IsVisible && m.IsHitTestVisible)
                    {
                        #region Check
                        var rp = new GPoint(x, y);
                        if (!m_MobileMode)
                        {
                            rp.OffsetNegative(m_Core.m_RenderOffset);
                        }

                        if (m.IsInside((int)rp.X, (int)rp.Y))
                        {
                            // Found an object, exit the loop.
                            isMouseOverRoute = true;
                            break;
                        }
                        #endregion
                    }
                }

                foreach (var m in o.Polygons)
                {
                    if (m.IsVisible && m.IsHitTestVisible)
                    {
                        #region Check
                        var rp = new GPoint(x, y);
                        if (!m_MobileMode)
                        {
                            rp.OffsetNegative(m_Core.m_RenderOffset);
                        }

                        if (m.IsInsideLocal((int)rp.X, (int)rp.Y))
                        {
                            // Found an object, exit the loop.
                            isMouseOverPolygon = true;
                            break;
                        }
                        #endregion
                    }
                }
            }
        }

        return (isMouseOverMarker, isMouseOverRoute, isMouseOverPolygon);
    }

    internal void RestoreCursorOnLeave()
    {
        if (m_OverObjectCount <= 0 && m_CursorBefore != null)
        {
            m_OverObjectCount = 0;
            Cursor = m_CursorBefore;
            m_CursorBefore = null;
        }
    }

    internal void SetCursorHandOnEnter()
    {
        if (m_OverObjectCount <= 0 && Cursor != Cursors.Hand)
        {
            m_OverObjectCount = 0;
            m_CursorBefore = Cursor;
            Cursor = Cursors.Hand;
        }
    }

    /// <summary>
    ///     prevents focusing map if mouse enters it's area
    /// </summary>
    public bool DisableFocusOnMouseEnter = false;

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);

        if (!DisableFocusOnMouseEnter)
        {
            Focus();
        }

        m_MouseIn = true;
    }

    bool m_MouseIn;

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        m_MouseIn = false;
    }

    /// <summary>
    ///     reverses MouseWheel zooming direction
    /// </summary>
    public bool InvertedMouseWheelZooming = false;

    /// <summary>
    ///     lets you zoom by MouseWheel even when pointer is in area of marker
    /// </summary>
    public bool IgnoreMarkerOnMouseWheel = false;

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (MouseWheelZoomEnabled
            && m_MouseIn
            && (!IsMouseOverMarker || IgnoreMarkerOnMouseWheel)
            && !m_Core.IsDragging)
        {
            if (m_Core.m_MouseLastZoom.X != e.X && m_Core.m_MouseLastZoom.Y != e.Y)
            {
                if (MouseWheelZoomType == MouseWheelZoomType.MousePositionAndCenter)
                {
                    m_Core.m_Position = FromLocalToLatLng(e.X, e.Y);
                }
                else if (MouseWheelZoomType == MouseWheelZoomType.ViewCenter)
                {
                    m_Core.m_Position = FromLocalToLatLng(Width / 2, Height / 2);
                }
                else if (MouseWheelZoomType == MouseWheelZoomType.MousePositionWithoutCenter)
                {
                    m_Core.m_Position = FromLocalToLatLng(e.X, e.Y);
                }

                m_Core.m_MouseLastZoom.X = e.X;
                m_Core.m_MouseLastZoom.Y = e.Y;
            }

            // set mouse position to map center
            if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
            {
                if (!GMaps.Instance.IsRunningOnMono)
                {
                    var p = PointToScreen(new Point(Width / 2, Height / 2));
                    Stuff.SetCursorPos(p.X, p.Y);
                }
            }

            m_Core.MouseWheelZooming = true;

            if (e.Delta > 0)
            {
                if (!InvertedMouseWheelZooming)
                {
                    Zoom = (int)Zoom + 1;
                }
                else
                {
                    Zoom = (int)(Zoom + 0.99) - 1;
                }
            }
            else if (e.Delta < 0)
            {
                if (!InvertedMouseWheelZooming)
                {
                    Zoom = (int)(Zoom + 0.99) - 1;
                }
                else
                {
                    Zoom = (int)Zoom + 1;
                }
            }

            m_Core.MouseWheelZooming = false;
        }
    }
    #endregion

    #region IGControl Members
    /// <summary>
    ///     Call it to empty tile cache & reload tiles
    /// </summary>
    public void ReloadMap() => m_Core.ReloadMap();

    /// <summary>
    ///     set current position using keywords
    /// </summary>
    /// <param name="keys"></param>
    /// <returns>true if successfull</returns>
    public GeoCoderStatusCode SetPositionByKeywords(string keys)
    {
        var status = GeoCoderStatusCode.UNKNOWN_ERROR;
        var gp = MapProvider as IGeocodingProvider;

        gp ??= GMapProviders.OpenStreetMap as IGeocodingProvider;

        if (gp != null)
        {
            var pt = gp.GetPoint(keys.Replace("#", "%23"), out status);

            if (status == GeoCoderStatusCode.OK && pt.HasValue)
            {
                Position = pt.Value;
            }
        }

        return status;
    }

    /// <summary>
    ///     get current position using keywords
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="point"></param>
    /// <returns></returns>
    public GeoCoderStatusCode GetPositionByKeywords(string keys, out PointLatLng point)
    {
        point = new PointLatLng();

        var status = GeoCoderStatusCode.UNKNOWN_ERROR;
        var gp = MapProvider as IGeocodingProvider;

        gp ??= GMapProviders.OpenStreetMap as IGeocodingProvider;

        if (gp != null)
        {
            var pt = gp.GetPoint(keys.Replace("#", "%23"), out status);

            if (status == GeoCoderStatusCode.OK && pt.HasValue)
            {
                point = pt.Value;
            }
        }

        return status;
    }

    /// <summary>
    ///     gets world coordinate from local control coordinate
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public PointLatLng FromLocalToLatLng(int x, int y)
    {
        if (m_MapRenderTransform.HasValue)
        {
            //var xx = (int)(Core.renderOffset.X + ((x - Core.renderOffset.X) / MapRenderTransform.Value));
            //var yy = (int)(Core.renderOffset.Y + ((y - Core.renderOffset.Y) / MapRenderTransform.Value));

            //PointF center = new PointF(Core.Width / 2, Core.Height / 2);

            //Matrix m = new Matrix();
            //m.Translate(-Core.renderOffset.X, -Core.renderOffset.Y);
            //m.Scale(MapRenderTransform.Value, MapRenderTransform.Value);

            //System.Drawing.Point[] tt = new System.Drawing.Point[] { new System.Drawing.Point(x, y) };
            //m.TransformPoints(tt);
            //var z = tt[0];

            //

            x = (int)(m_Core.m_RenderOffset.X + (x - m_Core.m_RenderOffset.X) / m_MapRenderTransform.Value);
            y = (int)(m_Core.m_RenderOffset.Y + (y - m_Core.m_RenderOffset.Y) / m_MapRenderTransform.Value);
        }

        if (IsRotated)
        {
            var tt = new[] { new Point(x, y) };
            m_RotationMatrixInvert.TransformPoints(tt);
            var f = tt[0];

            if (VirtualSizeEnabled)
            {
                f.X += (Width - m_Core.m_VWidth) / 2;
                f.Y += (Height - m_Core.m_VHeight) / 2;
            }

            x = f.X;
            y = f.Y;
        }

        return m_Core.FromLocalToLatLng(x, y);
    }

    /// <summary>
    ///     gets local coordinate from world coordinate
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public GPoint FromLatLngToLocal(PointLatLng point)
    {
        var ret = m_Core.FromLatLngToLocal(point);

        if (m_MapRenderTransform.HasValue)
        {
            ret.X = (int)(m_Core.m_RenderOffset.X + (m_Core.m_RenderOffset.X - ret.X) * -m_MapRenderTransform.Value);
            ret.Y = (int)(m_Core.m_RenderOffset.Y + (m_Core.m_RenderOffset.Y - ret.Y) * -m_MapRenderTransform.Value);
        }

        if (IsRotated)
        {
            var tt = new[] { new Point((int)ret.X, (int)ret.Y) };
            m_RotationMatrix.TransformPoints(tt);
            var f = tt[0];

            if (VirtualSizeEnabled)
            {
                f.X += (Width - m_Core.m_VWidth) / 2;
                f.Y += (Height - m_Core.m_VHeight) / 2;
            }

            ret.X = f.X;
            ret.Y = f.Y;
        }

        return ret;
    }

    /// <summary>
    ///     shows map db export dialog
    /// </summary>
    /// <returns></returns>
    public bool ShowExportDialog()
    {
        using var dlg = new SaveFileDialog();
        dlg.CheckPathExists = true;
        dlg.CheckFileExists = false;
        dlg.AddExtension = true;
        dlg.DefaultExt = "gmdb";
        dlg.ValidateNames = true;
        dlg.Title = "GMap.NET: Export map to db, if file exsist only new data will be added";
        dlg.FileName = "DataExp";
        dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
        dlg.FilterIndex = 1;
        dlg.RestoreDirectory = true;

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            bool ok = GMaps.ExportToGMDB(dlg.FileName);
            if (ok)
            {
                MessageBox.Show("Complete!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Failed!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return ok;
        }

        return false;
    }

    /// <summary>
    ///     shows map dbimport dialog
    /// </summary>
    /// <returns></returns>
    public bool ShowImportDialog()
    {
        using var dlg = new OpenFileDialog();
        dlg.CheckPathExists = true;
        dlg.CheckFileExists = false;
        dlg.AddExtension = true;
        dlg.DefaultExt = "gmdb";
        dlg.ValidateNames = true;
        dlg.Title = "GMap.NET: Import to db, only new data will be added";
        dlg.FileName = "DataImport";
        dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        dlg.Filter = "GMap.NET DB files (*.gmdb)|*.gmdb";
        dlg.FilterIndex = 1;
        dlg.RestoreDirectory = true;

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            bool ok = GMaps.ImportFromGMDB(dlg.FileName);
            if (ok)
            {
                MessageBox.Show("Complete!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ReloadMap();
            }
            else
            {
                MessageBox.Show("Failed!", "GMap.NET", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return ok;
        }

        return false;
    }

    [Category("GMap.NET")]
    [Description("map scale type")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ScaleModes ScaleMode { get; set; } = ScaleModes.Integer;

    [Category("GMap.NET")]
    [DefaultValue(0)]
    public double Zoom
    {
        get => m_ZoomReal;
        set
        {
            if (m_ZoomReal != value)
            {
                Debug.WriteLine("ZoomPropertyChanged: " + m_ZoomReal + " -> " + value);

                if (value > MaxZoom)
                {
                    m_ZoomReal = MaxZoom;
                }
                else if (value < MinZoom)
                {
                    m_ZoomReal = MinZoom;
                }
                else
                {
                    m_ZoomReal = value;
                }

                double remainder = value % 1;
                if (ScaleMode == ScaleModes.Fractional && remainder != 0)
                {
                    float scaleValue = (float)Math.Pow(2d, remainder);
                    {
                        m_MapRenderTransform = scaleValue;
                    }

                    ZoomStep = Convert.ToInt32(value - remainder);
                }
                else
                {
                    m_MapRenderTransform = null;
                    ZoomStep = (int)Math.Floor(value);
                    //zoomReal = ZoomStep;
                }

                if (m_Core.m_IsStarted && !IsDragging)
                {
                    ForceUpdateOverlays();
                }
            }
        }
    }

    /// <summary>
    ///     map zoom level
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    internal int ZoomStep
    {
        get => m_Core.Zoom;
        set
        {
            if (value > MaxZoom)
            {
                m_Core.Zoom = MaxZoom;
            }
            else if (value < MinZoom)
            {
                m_Core.Zoom = MinZoom;
            }
            else
            {
                m_Core.Zoom = value;
            }
        }
    }

    /// <summary>
    ///     current map center position
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public PointLatLng Position
    {
        get => m_Core.Position;
        set
        {
            m_Core.Position = value;

            if (m_Core.m_IsStarted)
            {
                ForceUpdateOverlays();
            }
        }
    }

    /// <summary>
    ///     current position in pixel coordinates
    /// </summary>
    [Browsable(false)]
    public GPoint PositionPixel => m_Core.PositionPixel;

    /// <summary>
    ///     location of cache
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string CacheLocation
    {
        get
        {
#if !DESIGN
            return CacheLocator.Location;
#else
        return string.Empty;
#endif
        }
        set
        {
#if !DESIGN
            CacheLocator.Location = value;
#endif
        }
    }

    bool m_IsDragging;

    /// <summary>
    ///     is user dragging map
    /// </summary>
    [Browsable(false)]
    public bool IsDragging => m_IsDragging;

    bool m_IsMouseOverMarker;
    internal int m_OverObjectCount;

    /// <summary>
    ///     is mouse over marker
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IsMouseOverMarker
    {
        get => m_IsMouseOverMarker;
        internal set
        {
            m_IsMouseOverMarker = value;
            m_OverObjectCount += value ? 1 : -1;
        }
    }

    bool m_IsMouseOverRoute;

    /// <summary>
    ///     is mouse over route
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IsMouseOverRoute
    {
        get => m_IsMouseOverRoute;
        internal set
        {
            m_IsMouseOverRoute = value;
            m_OverObjectCount += value ? 1 : -1;
        }
    }

    bool m_IsMouseOverPolygon;

    /// <summary>
    ///     is mouse over polygon
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool IsMouseOverPolygon
    {
        get => m_IsMouseOverPolygon;
        internal set
        {
            m_IsMouseOverPolygon = value;
            m_OverObjectCount += value ? 1 : -1;
        }
    }

    /// <summary>
    ///     gets current map view top/left coordinate, width in Lng, height in Lat
    /// </summary>
    [Browsable(false)]
    public RectLatLng ViewArea
    {
        get
        {
            if (!IsRotated)
            {
                return m_Core.ViewArea;
            }
            else if (m_Core.Provider.Projection != null)
            {
                var p = FromLocalToLatLng(0, 0);
                var p2 = FromLocalToLatLng(Width, Height);

                return RectLatLng.FromLTRB(p.Lng, p.Lat, p2.Lng, p2.Lat);
            }

            return RectLatLng.Empty;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public GMapProvider MapProvider
    {
        get => m_Core.Provider;
        set
        {
            if (m_Core.Provider == null || !m_Core.Provider.Equals(value))
            {
                Debug.WriteLine("MapType: " + m_Core.Provider.Name + " -> " + value.Name);

                var viewarea = SelectedArea;

                if (viewarea != RectLatLng.Empty)
                {
                    Position = new PointLatLng(viewarea.Lat - viewarea.HeightLat / 2,
                        viewarea.Lng + viewarea.WidthLng / 2);
                }
                else
                {
                    viewarea = ViewArea;
                }

                m_Core.Provider = value;

                if (m_Core.m_IsStarted)
                {
                    if (m_Core.m_ZoomToArea)
                    {
                        // restore zoomrect as close as possible
                        if (viewarea != RectLatLng.Empty && viewarea != ViewArea)
                        {
                            int bestZoom = m_Core.GetMaxZoomToFitRect(viewarea);
                            if (bestZoom > 0 && Zoom != bestZoom)
                            {
                                Zoom = bestZoom;
                            }
                        }
                    }
                    else
                    {
                        ForceUpdateOverlays();
                    }
                }
            }
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public IRoutingProvider RoutingProvider
    {
        get
        {
            var dp = MapProvider as IRoutingProvider;

            // use OpenStreetMap if provider does not implement routing
            dp ??= GMapProviders.OpenStreetMap as IRoutingProvider;

            return dp;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public IDirectionsProvider DirectionsProvider
    {
        get
        {
            var dp = MapProvider as IDirectionsProvider;

            // use OpenStreetMap if provider does not implement routing
            dp ??= GMapProviders.OpenStreetMap as IDirectionsProvider;

            return dp;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public IGeocodingProvider GeocodingProvider
    {
        get
        {
            var dp = MapProvider as IGeocodingProvider;
            // use OpenStreetMap if provider does not implement routing
            dp ??= GMapProviders.OpenStreetMap as IGeocodingProvider;

            return dp;
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public IRoadsProvider RoadsProvider
    {
        get
        {
            var dp = MapProvider as IRoadsProvider;
            // use GoogleMap if provider does not implement routing
            dp ??= GMapProviders.GoogleMap as IRoadsProvider;

            return dp;
        }
    }

    /// <summary>
    ///     is routes enabled
    /// </summary>
    [Category("GMap.NET")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool RoutesEnabled
    {
        get => m_Core.RoutesEnabled;
        set => m_Core.RoutesEnabled = value;
    }

    /// <summary>
    ///     is polygons enabled
    /// </summary>
    [Category("GMap.NET")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool PolygonsEnabled
    {
        get => m_Core.PolygonsEnabled;
        set => m_Core.PolygonsEnabled = value;
    }

    /// <summary>
    ///     is markers enabled
    /// </summary>
    [Category("GMap.NET")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool MarkersEnabled
    {
        get => m_Core.MarkersEnabled;
        set => m_Core.MarkersEnabled = value;
    }

    /// <summary>
    ///     can user drag map
    /// </summary>
    [Category("GMap.NET")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool CanDragMap
    {
        get => m_Core.CanDragMap;
        set => m_Core.CanDragMap = value;
    }

    /// <summary>
    ///     map render mode
    /// </summary>
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderMode RenderMode
    {
        get => m_Core.RenderMode;
        internal set => m_Core.RenderMode = value;
    }

    /// <summary>
    ///     gets map manager
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public static GMaps Manager => GMaps.Instance;
    #endregion

    #region IGControl event Members

    /// <summary>
    ///     occurs when current position is changed
    /// </summary>
    public event PositionChanged OnPositionChanged
    {
        add => m_Core.OnCurrentPositionChanged += value;
        remove => m_Core.OnCurrentPositionChanged -= value;
    }

    /// <summary>
    ///     occurs when tile set load is complete
    /// </summary>
    public event TileLoadComplete OnTileLoadComplete
    {
        add => m_Core.OnTileLoadComplete += value;
        remove => m_Core.OnTileLoadComplete -= value;
    }

    /// <summary>
    ///     occurs when tile set is starting to load
    /// </summary>
    public event TileLoadStart OnTileLoadStart
    {
        add => m_Core.OnTileLoadStart += value;
        remove => m_Core.OnTileLoadStart -= value;
    }

    /// <summary>
    ///     occurs on map drag
    /// </summary>
    public event MapDrag OnMapDrag
    {
        add => m_Core.OnMapDrag += value;
        remove => m_Core.OnMapDrag -= value;
    }

    /// <summary>
    ///     occurs on map zoom changed
    /// </summary>
    public event MapZoomChanged OnMapZoomChanged
    {
        add => m_Core.OnMapZoomChanged += value;
        remove => m_Core.OnMapZoomChanged -= value;
    }

    /// <summary>
    ///     occurs on map type changed
    /// </summary>
    public event MapTypeChanged OnMapTypeChanged
    {
        add => m_Core.OnMapTypeChanged += value;
        remove => m_Core.OnMapTypeChanged -= value;
    }

    /// <summary>
    ///     occurs on empty tile displayed
    /// </summary>
    public event EmptyTileError OnEmptyTileError
    {
        add => m_Core.OnEmptyTileError += value;
        remove => m_Core.OnEmptyTileError -= value;
    }
    #endregion

    #region Serialization
    //static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

    /// <summary>
    ///     Serializes the overlays.
    /// </summary>
    /// <param name="stream">The stream.</param>
    //public void SerializeOverlays(Stream stream)
    //{
    //    if (stream == null)
    //    {
    //        throw new ArgumentNullException("stream");
    //    }

    //    // Create an array from the overlays
    //    var overlayArray = new GMapOverlay[Overlays.Count];
    //    Overlays.CopyTo(overlayArray, 0);

    //    // Serialize the overlays
    //    BinaryFormatter.Serialize(stream, overlayArray);
    //}

    /// <summary>
    ///     De-serializes the overlays.
    /// </summary>
    /// <param name="stream">The stream.</param>
    //public void DeserializeOverlays(Stream stream)
    //{
    //    if (stream == null)
    //    {
    //        throw new ArgumentNullException("stream");
    //    }

    //    // De-serialize the overlays
    //    var overlayArray = BinaryFormatter.Deserialize(stream) as GMapOverlay[];

    //    // Populate the collection of overlays.
    //    foreach (var overlay in overlayArray)
    //    {
    //        overlay.Control = this;
    //        Overlays.Add(overlay);
    //    }

    //    ForceUpdateOverlays();
    //}
    #endregion
}

public enum ScaleModes
{
    /// <summary>
    ///     no scaling
    /// </summary>
    Integer,

    /// <summary>
    ///     scales to fractional level, CURRENT VERSION DOESN'T HANDLE OBJECT POSITIONS CORRECLTY,
    ///     http://greatmaps.codeplex.com/workitem/16046
    /// </summary>
    Fractional,
}

public enum HelperLineOptions
{
    DontShow = 0,
    ShowAlways = 1,
    ShowOnModifierKey = 2
}

public enum MapScaleInfoPosition
{
    Top,
    Bottom
}

public delegate void SelectionChange(RectLatLng selection, bool zoomToFit);

public delegate void MapClick(PointLatLng pointClick, MouseEventArgs e);

public delegate void MapDoubleClick(PointLatLng pointClick, MouseEventArgs e);
