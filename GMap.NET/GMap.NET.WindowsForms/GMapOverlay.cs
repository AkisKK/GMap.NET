﻿using System;
using System.Drawing;
using System.Runtime.Serialization;
using GMap.NET.WindowsForms.ObjectModel;

namespace GMap.NET.WindowsForms;

/// <summary>
///     GMap.NET overlay
/// </summary>
[Serializable]
public class GMapOverlay : ISerializable, IDeserializationCallback, IDisposable
{
    bool m_IsVisibile = true;

    /// <summary>
    ///     is overlay visible
    /// </summary>
    public bool IsVisibile
    {
        get => m_IsVisibile;
        set
        {
            if (value != m_IsVisibile)
            {
                m_IsVisibile = value;

                if (Control != null)
                {
                    if (m_IsVisibile)
                    {
                        // Save the current value in order to avoid resetting it inadvertently if we call Refresh() below.
                        bool oldHoldInvalidation = Control.HoldInvalidation;
                        Control.HoldInvalidation = true;
                        {
                            ForceUpdate();
                        }

                        if (oldHoldInvalidation == false)
                        {
                            // Only call Refresh() if the HoldInvalidation was previously set to false. We do this because the
                            // call to Refresh() will reset the HoldInvalidation flag to false. If the HoldInvalidation was set
                            // to true we should not call Refresh(). I.e. Setting the Visible property of an overlay should respect
                            // current HoldInvalidation setting, while performing its required job.
                            Control.Refresh();
                        }
                        // Restore the original value to the HoldInvalidation flag.
                        Control.HoldInvalidation = oldHoldInvalidation;
                    }
                    else
                    {
                        if (Control.IsMouseOverMarker)
                        {
                            Control.IsMouseOverMarker = false;
                        }

                        if (Control.IsMouseOverPolygon)
                        {
                            Control.IsMouseOverPolygon = false;
                        }

                        if (Control.IsMouseOverRoute)
                        {
                            Control.IsMouseOverRoute = false;
                        }

                        Control.RestoreCursorOnLeave();

                        if (!Control.HoldInvalidation)
                        {
                            Control.Invalidate();
                        }
                    }
                }
            }
        }
    }

    bool m_IsHitTestVisible = true;

    /// <summary>
    ///     HitTest visibility for entire overlay
    /// </summary>
    public bool IsHitTestVisible
    {
        get => m_IsHitTestVisible;
        set => m_IsHitTestVisible = value;
    }

    bool m_IsZoomSignificant = true;

    /// <summary>
    ///     if false don't consider contained objects when box zooming
    /// </summary>
    public bool IsZoomSignificant
    {
        get => m_IsZoomSignificant;
        set => m_IsZoomSignificant = value;
    }

    /// <summary>
    ///     overlay Id
    /// </summary>
    public string Id;

    /// <summary>
    ///     list of markers, should be thread safe
    /// </summary>
    public readonly ObservableCollectionThreadSafe<GMapMarker> Markers = [];

    /// <summary>
    ///     list of routes, should be thread safe
    /// </summary>
    public readonly ObservableCollectionThreadSafe<GMapRoute> Routes = [];

    /// <summary>
    ///     list of polygons, should be thread safe
    /// </summary>
    public readonly ObservableCollectionThreadSafe<GMapPolygon> Polygons = [];

    GMapControl m_Control;

    public GMapControl Control
    {
        get => m_Control;
        internal set => m_Control = value;
    }

    public GMapOverlay()
    {
        CreateEvents();
    }

    public GMapOverlay(string id)
    {
        Id = id;
        CreateEvents();
    }

    void CreateEvents()
    {
        Markers.CollectionChanged += Markers_CollectionChanged;
        Routes.CollectionChanged += Routes_CollectionChanged;
        Polygons.CollectionChanged += Polygons_CollectionChanged;
    }

    void ClearEvents()
    {
        Markers.CollectionChanged -= Markers_CollectionChanged;
        Routes.CollectionChanged -= Routes_CollectionChanged;
        Polygons.CollectionChanged -= Polygons_CollectionChanged;
    }

    public void Clear()
    {
        Markers.Clear();
        Routes.Clear();
        Polygons.Clear();
    }

    void Polygons_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GMapPolygon obj in e.NewItems)
            {
                if (obj != null)
                {
                    obj.Overlay = this;
                    Control?.UpdatePolygonLocalPosition(obj);
                }
            }
        }

        if (Control != null)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (Control.IsMouseOverPolygon)
                {
                    Control.IsMouseOverPolygon = false;
                    Control.RestoreCursorOnLeave();
                }
            }

            if (!Control.HoldInvalidation)
            {
                Control.Invalidate();
            }
        }
    }

    void Routes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GMapRoute obj in e.NewItems)
            {
                if (obj != null)
                {
                    obj.Overlay = this;
                    Control?.UpdateRouteLocalPosition(obj);
                }
            }
        }

        if (Control != null)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (Control.IsMouseOverRoute)
                {
                    Control.IsMouseOverRoute = false;
                    Control.RestoreCursorOnLeave();
                }
            }

            if (!Control.HoldInvalidation)
            {
                Control.Invalidate();
            }
        }
    }

    void Markers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GMapMarker obj in e.NewItems)
            {
                if (obj != null)
                {
                    obj.Overlay = this;
                    Control?.UpdateMarkerLocalPosition(obj);
                }
            }
        }

        if (Control != null)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (Control.IsMouseOverMarker)
                {
                    Control.IsMouseOverMarker = false;
                    Control.RestoreCursorOnLeave();
                }
            }

            if (!Control.HoldInvalidation)
            {
                Control.Invalidate();
            }
        }
    }

    /// <summary>
    ///     updates local positions of objects
    /// </summary>
    internal void ForceUpdate()
    {
        if (Control != null)
        {
            foreach (var obj in Markers)
            {
                if (obj.IsVisible)
                {
                    Control.UpdateMarkerLocalPosition(obj);
                }
            }

            foreach (var obj in Polygons)
            {
                if (obj.IsVisible)
                {
                    Control.UpdatePolygonLocalPosition(obj);
                }
            }

            foreach (var obj in Routes)
            {
                if (obj.IsVisible)
                {
                    Control.UpdateRouteLocalPosition(obj);
                }
            }
        }
    }

    /// <summary>
    ///     renders objects/routes/polygons
    /// </summary>
    /// <param name="g"></param>
    public virtual void OnRender(Graphics g)
    {
        if (Control != null)
        {
            if (Control.RoutesEnabled)
            {
                foreach (var r in Routes)
                {
                    if (r.IsVisible)
                    {
                        r.OnRender(g);
                    }
                }
            }

            if (Control.PolygonsEnabled)
            {
                foreach (var r in Polygons)
                {
                    if (r.IsVisible)
                    {
                        r.OnRender(g);
                    }
                }
            }

            if (Control.MarkersEnabled)
            {
                // markers
                foreach (var m in Markers)
                {
                    //if(m.IsVisible && (m.DisableRegionCheck || Control.Core.currentRegion.Contains(m.LocalPosition.X, m.LocalPosition.Y)))
                    if (m.IsVisible || m.DisableRegionCheck)
                    {
                        m.OnRender(g);
                    }
                }

                // ToolTips are drawn in separate method to ensure they are top most
            }
        }
    }

    public virtual void OnRenderToolTips(Graphics g)
    {
        if (Control is null)
        {
            // Return early.
            return;
        }

        if (Control.MarkersEnabled)
        {
            // tooltips above
            foreach (var m in Markers)
            {
                //if(m.ToolTip != null && m.IsVisible && Control.Core.currentRegion.Contains(m.LocalPosition.X, m.LocalPosition.Y))
                if (m.ToolTip != null && m.IsVisible)
                {
                    if (!string.IsNullOrEmpty(m.ToolTipText) &&
                        (m.ToolTipMode == MarkerTooltipMode.Always ||
                         m.ToolTipMode == MarkerTooltipMode.OnMouseOver && m.IsMouseOver))
                    {
                        m.ToolTip.OnRender(g);
                        break;
                    }
                }
            }
        }
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
        info.AddValue("Id", Id);
        info.AddValue("IsVisible", IsVisibile);

        var markerArray = new GMapMarker[Markers.Count];
        Markers.CopyTo(markerArray, 0);
        info.AddValue("Markers", markerArray);

        var routeArray = new GMapRoute[Routes.Count];
        Routes.CopyTo(routeArray, 0);
        info.AddValue("Routes", routeArray);

        var polygonArray = new GMapPolygon[Polygons.Count];
        Polygons.CopyTo(polygonArray, 0);
        info.AddValue("Polygons", polygonArray);
    }

    private readonly GMapMarker[] m_DeserializedMarkerArray;
    private readonly GMapRoute[] m_DeserializedRouteArray;
    private readonly GMapPolygon[] m_DeserializedPolygonArray;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GMapOverlay" /> class.
    /// </summary>
    /// <param name="info">The info.</param>
    /// <param name="context">The context.</param>
    protected GMapOverlay(SerializationInfo info, StreamingContext context)
    {
        Id = info.GetString("Id");
        IsVisibile = info.GetBoolean("IsVisible");

        m_DeserializedMarkerArray = Extensions.GetValue(info, "Markers", Array.Empty<GMapMarker>());
        m_DeserializedRouteArray = Extensions.GetValue(info, "Routes", Array.Empty<GMapRoute>());
        m_DeserializedPolygonArray = Extensions.GetValue(info, "Polygons", Array.Empty<GMapPolygon>());

        CreateEvents();
    }
    #endregion

    #region IDeserializationCallback Members
    /// <summary>
    ///     Runs when the entire object graph has been deserialized.
    /// </summary>
    /// <param name="sender">
    ///     The object that initiated the callback. The functionality for this parameter is not currently
    ///     implemented.
    /// </param>
    public void OnDeserialization(object sender)
    {
        // Populate Markers
        foreach (var marker in m_DeserializedMarkerArray)
        {
            marker.Overlay = this;
            Markers.Add(marker);
        }

        // Populate Routes
        foreach (var route in m_DeserializedRouteArray)
        {
            route.Overlay = this;
            Routes.Add(route);
        }

        // Populate Polygons
        foreach (var polygon in m_DeserializedPolygonArray)
        {
            polygon.Overlay = this;
            Polygons.Add(polygon);
        }
    }
    #endregion

    #region IDisposable Members
    bool m_Disposed;

    public void Dispose()
    {
        if (m_Disposed)
        {
            return;
        }

        m_Disposed = true;

        ClearEvents();

        foreach (var m in Markers)
        {
            m.Dispose();
        }

        foreach (var r in Routes)
        {
            r.Dispose();
        }

        foreach (var p in Polygons)
        {
            p.Dispose();
        }

        Clear();
        GC.SuppressFinalize(this);
    }
    #endregion
}
