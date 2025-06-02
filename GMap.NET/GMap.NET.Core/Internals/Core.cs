using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

#if NETFRAMEWORK
using System.Collections.Concurrent;
#endif

namespace GMap.NET.Internals;

/// <summary>
///     internal map control core
/// </summary>
internal sealed class Core : IDisposable
{
    internal PointLatLng m_Position;
    private GPoint m_PositionPixel;

    internal GPoint m_RenderOffset;
    internal GPoint m_CenterTileXYLocation;
    private GPoint m_CenterTileXYLocationLast;
    private GPoint m_DragPoint;
    internal GPoint m_CompensationOffset;

    internal GPoint m_MouseDown;
    internal GPoint m_MouseCurrent;
    internal GPoint m_MouseLastZoom;
    internal GPoint m_TouchCurrent;

    public MouseWheelZoomType MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
    public bool MouseWheelZoomEnabled = true;

    public PointLatLng? LastLocationInBounds;
    public bool VirtualSizeEnabled = false;

    private GSize m_SizeOfMapArea;
    private GSize m_MinOfTiles;
    private GSize m_MaxOfTiles;

    internal GRect m_TileRect;

    internal GRect m_TileRectBearing;

    //private GRect _currentRegion;
    internal float m_Bearing = 0;
    public bool IsRotated = false;

    internal bool m_FillEmptyTiles = true;

    public TileMatrix Matrix = new();

    internal List<DrawTile> m_TileDrawingList = [];
    internal FastReaderWriterLock m_TileDrawingListLock = new();

#if !NETFRAMEWORK
    public readonly Stack<LoadTask> TileLoadQueue = new Stack<LoadTask>();
#endif

    static readonly int m_GThreadPoolSize = 4;

    DateTime m_LastTileLoadStart = DateTime.Now;
    DateTime m_LastTileLoadEnd = DateTime.Now;
    internal volatile bool m_IsStarted;
    int m_Zoom;

    internal double m_ScaleX = 1;
    internal double m_ScaleY = 1;

    internal int m_MaxZoom = 2;
    internal int m_MinZoom = 2;
    internal int m_Width;
    internal int m_Height;

    internal int m_PxRes100M; // 100 meters
    internal int m_PxRes1000M; // 1km  
    internal int m_PxRes10Km; // 10km
    internal int m_PxRes100Km; // 100km
    internal int m_PxRes1000Km; // 1000km
    internal int m_PxRes5000Km; // 5000km

    /// <summary>
    ///     is user dragging map
    /// </summary>
    public bool IsDragging;

    /// <summary>
    /// Gets a value indicating whether the map control core has started.
    /// </summary>
    public bool IsStarted => m_IsStarted;

    public Core()
    {
        Provider = EmptyProvider.Instance;
    }

    /// <summary>
    ///     map zoom
    /// </summary>
    public int Zoom
    {
        get => m_Zoom;
        set
        {
            if (m_Zoom != value && !IsDragging)
            {
                m_Zoom = value;

                m_MinOfTiles = Provider.Projection.GetTileMatrixMinXY(value);
                m_MaxOfTiles = Provider.Projection.GetTileMatrixMaxXY(value);

                m_PositionPixel = Provider.Projection.FromLatLngToPixel(Position, value);

                if (m_IsStarted)
                {
                    CancelAsyncTasks();

                    Matrix.ClearLevelsBelove(m_Zoom - LevelsKeepInMemory);
                    Matrix.ClearLevelsAbove(m_Zoom + LevelsKeepInMemory);

                    lock (m_FailedLoads)
                    {
                        m_FailedLoads.Clear();
                        m_RaiseEmptyTileError = true;
                    }

                    GoToCurrentPositionOnZoom();
                    UpdateBounds();

                    OnMapZoomChanged?.Invoke();
                }
            }
        }
    }

    /// <summary>
    ///     current marker position in pixel coordinates
    /// </summary>
    public GPoint PositionPixel => m_PositionPixel;

    /// <summary>
    ///     current marker position
    /// </summary>
    public PointLatLng Position
    {
        get => m_Position;
        set
        {
            m_Position = value;
            m_PositionPixel = Provider.Projection.FromLatLngToPixel(value, Zoom);

            if (m_IsStarted)
            {
                if (!IsDragging)
                {
                    GoToCurrentPosition();
                }

                OnCurrentPositionChanged?.Invoke(m_Position);
            }
        }
    }

    private GMapProvider m_Provider;

    public GMapProvider Provider
    {
        get => m_Provider;
        set
        {
            if (m_Provider == null || !m_Provider.Equals(value))
            {
                bool diffProjection = m_Provider == null || m_Provider.Projection != value.Projection;

                m_Provider = value;

                if (!m_Provider.IsInitialized)
                {
                    m_Provider.IsInitialized = true;
                    m_Provider.OnInitialized();
                }

                if (m_Provider.Projection != null && diffProjection)
                {
                    m_TileRect = new GRect(GPoint.Empty, Provider.Projection.TileSize);
                    m_TileRectBearing = m_TileRect;
                    if (IsRotated)
                    {
                        m_TileRectBearing.Inflate(1, 1);
                    }

                    m_MinOfTiles = Provider.Projection.GetTileMatrixMinXY(Zoom);
                    m_MaxOfTiles = Provider.Projection.GetTileMatrixMaxXY(Zoom);
                    m_PositionPixel = Provider.Projection.FromLatLngToPixel(Position, Zoom);
                }

                if (m_IsStarted)
                {
                    CancelAsyncTasks();
                    if (diffProjection)
                    {
                        OnMapSizeChanged(m_Width, m_Height);
                    }

                    ReloadMap();

                    if (m_MinZoom < m_Provider.MinZoom)
                    {
                        m_MinZoom = m_Provider.MinZoom;
                    }

                    //if(provider.MaxZoom.HasValue && maxZoom > provider.MaxZoom)
                    //{
                    //   maxZoom = provider.MaxZoom.Value;
                    //}

                    m_ZoomToArea = true;

                    if (m_Provider.Area.HasValue && !m_Provider.Area.Value.Contains(Position))
                    {
                        SetZoomToFitRect(m_Provider.Area.Value);
                        m_ZoomToArea = false;
                    }

                    OnMapTypeChanged?.Invoke(value);
                }
            }
        }
    }

    internal bool m_ZoomToArea = true;

    /// <summary>
    ///     sets zoom to max to fit rect
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public bool SetZoomToFitRect(RectLatLng rect)
    {
        int mmaxZoom = GetMaxZoomToFitRect(rect);
        if (mmaxZoom > 0)
        {
            var center = new PointLatLng(rect.Lat - rect.HeightLat / 2, rect.Lng + rect.WidthLng / 2);
            Position = center;

            if (mmaxZoom > m_MaxZoom)
            {
                mmaxZoom = m_MaxZoom;
            }

            if (Zoom != mmaxZoom)
            {
                Zoom = mmaxZoom;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     is polygons enabled
    /// </summary>
    public bool PolygonsEnabled = true;

    /// <summary>
    ///     is routes enabled
    /// </summary>
    public bool RoutesEnabled = true;

    /// <summary>
    ///     is markers enabled
    /// </summary>
    public bool MarkersEnabled = true;

    /// <summary>
    ///     can user drag map
    /// </summary>
    public bool CanDragMap = true;

    /// <summary>
    ///     retry count to get tile
    /// </summary>
    public int RetryLoadTile = 0;

    /// <summary>
    ///     how many levels of tiles are staying decompressed in memory
    /// </summary>
    public int LevelsKeepInMemory = 5;

    /// <summary>
    ///     map render mode
    /// </summary>
    public RenderMode RenderMode = RenderMode.GDI_PLUS;

    /// <summary>
    ///     occurs when current position is changed
    /// </summary>
    public event PositionChanged OnCurrentPositionChanged;

    /// <summary>
    ///     occurs when tile set load is complete
    /// </summary>
    public event TileLoadComplete OnTileLoadComplete;

    /// <summary>
    ///     occurs when tile set is starting to load
    /// </summary>
    public event TileLoadStart OnTileLoadStart;

    /// <summary>
    ///     occurs on empty tile displayed
    /// </summary>
    public event EmptyTileError OnEmptyTileError;

    /// <summary>
    ///     occurs on map drag
    /// </summary>
    public event MapDrag OnMapDrag;

    /// <summary>
    ///     occurs on map zoom changed
    /// </summary>
    public event MapZoomChanged OnMapZoomChanged;

    /// <summary>
    ///     occurs on map type changed
    /// </summary>
    public event MapTypeChanged OnMapTypeChanged;

    // should be only one pool for multiply controls, any ideas how to fix?
    //static readonly List<Thread> GThreadPool = new List<Thread>();
    // windows forms or WPF
    internal string m_SystemType;

    internal static int m_Instances;

    BackgroundWorker m_Invalidator;

    public BackgroundWorker OnMapOpen()
    {
        if (!m_IsStarted)
        {
            int x = Interlocked.Increment(ref m_Instances);
            Debug.WriteLine("OnMapOpen: " + x);

            m_IsStarted = true;

            if (x == 1)
            {
                GMaps.Instance.m_NoMapInstances = false;
            }

            GoToCurrentPosition();

            m_Invalidator = new BackgroundWorker
            {
                WorkerSupportsCancellation = true,
                WorkerReportsProgress = true
            };
            m_Invalidator.DoWork += InvalidatorWatch;
            m_Invalidator.RunWorkerAsync();

            //if(x == 1)
            //{
            // first control shown
            //}
        }

        return m_Invalidator;
    }

    public void OnMapClose()
    {
        Dispose();
    }

    internal readonly object m_InvalidationLock = new();
    internal DateTime m_LastInvalidation = DateTime.Now;

    void InvalidatorWatch(object sender, DoWorkEventArgs e)
    {
        var w = sender as BackgroundWorker;

        var span = TimeSpan.FromMilliseconds(111);
        int spanMs = (int)span.TotalMilliseconds;
        bool skiped = false;
        TimeSpan delta;
        DateTime now;

        while (Refresh != null && (!skiped && Refresh.WaitOne() || Refresh.WaitOne(spanMs, false) || true))
        {
            if (w.CancellationPending)
            {
                break;
            }

            now = DateTime.Now;
            lock (m_InvalidationLock)
            {
                delta = now - m_LastInvalidation;
            }

            if (delta > span)
            {
                lock (m_InvalidationLock)
                {
                    m_LastInvalidation = now;
                }

                skiped = false;

                w.ReportProgress(1);
                Debug.WriteLine("Invalidate delta: " + (int)delta.TotalMilliseconds + "ms");
            }
            else
            {
                skiped = true;
            }
        }
    }

    public void UpdateCenterTileXYLocation()
    {
        var center = FromLocalToLatLng(m_Width / 2, m_Height / 2);
        var centerPixel = Provider.Projection.FromLatLngToPixel(center, Zoom);
        m_CenterTileXYLocation = Provider.Projection.FromPixelToTileXY(centerPixel);
    }

    internal int m_VWidth = 800;
    internal int m_VHeight = 400;

    public void OnMapSizeChanged(int width, int height)
    {
        m_Width = width;
        m_Height = height;

        if (IsRotated)
        {
            int diag = (int)Math.Round(
                Math.Sqrt(m_Width * m_Width + m_Height * m_Height) / Provider.Projection.TileSize.Width,
                MidpointRounding.AwayFromZero);
            m_SizeOfMapArea.Width = 1 + diag / 2;
            m_SizeOfMapArea.Height = 1 + diag / 2;
        }
        else
        {
            m_SizeOfMapArea.Width = 1 + m_Width / Provider.Projection.TileSize.Width / 2;
            m_SizeOfMapArea.Height = 1 + m_Height / Provider.Projection.TileSize.Height / 2;
        }

        Debug.WriteLine("OnMapSizeChanged, w: " + width + ", h: " + height + ", size: " + m_SizeOfMapArea);

        if (m_IsStarted)
        {
            UpdateBounds();
            GoToCurrentPosition();
        }
    }

    /// <summary>
    ///     gets current map view top/left coordinate, width in Lng, height in Lat
    /// </summary>
    /// <returns></returns>
    public RectLatLng ViewArea
    {
        get
        {
            if (Provider.Projection != null)
            {
                var p = FromLocalToLatLng(0, 0);
                var p2 = FromLocalToLatLng(m_Width, m_Height);

                return RectLatLng.FromLTRB(p.Lng, p.Lat, p2.Lng, p2.Lat);
            }

            return RectLatLng.Empty;
        }
    }

    /// <summary>
    ///     gets lat/lng from local control coordinates
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public PointLatLng FromLocalToLatLng(long x, long y)
    {
        var p = new GPoint(x, y);
        p.OffsetNegative(m_RenderOffset);
        p.Offset(m_CompensationOffset);

        return Provider.Projection.FromPixelToLatLng(p, Zoom);
    }

    /// <summary>
    ///     return local coordinates from lat/lng
    /// </summary>
    /// <param name="latlng"></param>
    /// <returns></returns>
    public GPoint FromLatLngToLocal(PointLatLng latlng)
    {
        var pLocal = Provider.Projection.FromLatLngToPixel(latlng, Zoom);
        pLocal.Offset(m_RenderOffset);
        pLocal.OffsetNegative(m_CompensationOffset);
        return pLocal;
    }

    /// <summary>
    ///     gets max zoom level to fit rectangle
    /// </summary>
    /// <param name="rect"></param>
    /// <returns></returns>
    public int GetMaxZoomToFitRect(RectLatLng rect)
    {
        int zoom = m_MinZoom;

        if (rect.HeightLat == 0 || rect.WidthLng == 0)
        {
            zoom = m_MaxZoom / 2;
        }
        else
        {
            for (int i = zoom; i <= m_MaxZoom; i++)
            {
                var p1 = Provider.Projection.FromLatLngToPixel(rect.LocationTopLeft, i);
                var p2 = Provider.Projection.FromLatLngToPixel(rect.LocationRightBottom, i);

                if (p2.X - p1.X <= m_Width + 10 && p2.Y - p1.Y <= m_Height + 10)
                {
                    zoom = i;
                }
                else
                {
                    break;
                }
            }
        }

        return zoom;
    }

    /// <summary>
    ///     initiates map dragging
    /// </summary>
    /// <param name="pt"></param>
    public void BeginDrag(GPoint pt)
    {
        m_DragPoint.X = pt.X - m_RenderOffset.X;
        m_DragPoint.Y = pt.Y - m_RenderOffset.Y;
        IsDragging = true;
    }

    /// <summary>
    ///     ends map dragging
    /// </summary>
    public void EndDrag()
    {
        IsDragging = false;
        m_MouseDown = GPoint.Empty;

        Refresh.Set();
    }

    /// <summary>
    ///     reloads map
    /// </summary>
    public void ReloadMap()
    {
        if (m_IsStarted)
        {
            Debug.WriteLine("------------------");

            m_OkZoom = 0;
            m_SkipOverZoom = 0;

            CancelAsyncTasks();

            Matrix.ClearAllLevels();

            lock (m_FailedLoads)
            {
                m_FailedLoads.Clear();
                m_RaiseEmptyTileError = true;
            }

            Refresh.Set();

            UpdateBounds();
        }
        else
        {
            throw new Exception("Please, do not call ReloadMap before form is loaded, it's useless");
        }
    }

#if !NETFRAMEWORK
    public Task ReloadMapAsync()
    {
        ReloadMap();
        return Task.Factory.StartNew(() =>
        {
            bool wait;
            do
            {
                Thread.Sleep(100);
                Monitor.Enter(TileLoadQueue);
                try
                {
                    wait = TileLoadQueue.Any();
                }
                finally
                {
                    Monitor.Exit(TileLoadQueue);
                }
            } while (wait);
        });
    }
#endif

    /// <summary>
    ///     moves current position into map center
    /// </summary>
    public void GoToCurrentPosition()
    {
        m_CompensationOffset = m_PositionPixel; // TODO: fix

        // reset stuff
        m_RenderOffset = GPoint.Empty;
        m_DragPoint = GPoint.Empty;

        //var dd = new GPoint(-(CurrentPositionGPixel.X - Width / 2), -(CurrentPositionGPixel.Y - Height / 2));
        //dd.Offset(compensationOffset);

        var d = new GPoint(m_Width / 2, m_Height / 2);

        Drag(d);
    }

    public bool MouseWheelZooming = false;

    /// <summary>
    ///     moves current position into map center
    /// </summary>
    internal void GoToCurrentPositionOnZoom()
    {
        m_CompensationOffset = m_PositionPixel; // TODO: fix

        // reset stuff
        m_RenderOffset = GPoint.Empty;
        m_DragPoint = GPoint.Empty;

        // goto location and centering
        if (MouseWheelZooming)
        {
            if (MouseWheelZoomType != MouseWheelZoomType.MousePositionWithoutCenter)
            {
                var pt = new GPoint(-(m_PositionPixel.X - m_Width / 2), -(m_PositionPixel.Y - m_Height / 2));
                pt.Offset(m_CompensationOffset);
                m_RenderOffset.X = pt.X - m_DragPoint.X;
                m_RenderOffset.Y = pt.Y - m_DragPoint.Y;
            }
            else // without centering
            {
                m_RenderOffset.X = -m_PositionPixel.X - m_DragPoint.X;
                m_RenderOffset.Y = -m_PositionPixel.Y - m_DragPoint.Y;
                m_RenderOffset.Offset(m_MouseLastZoom);
                m_RenderOffset.Offset(m_CompensationOffset);
            }
        }
        else // use current map center
        {
            m_MouseLastZoom = GPoint.Empty;

            var pt = new GPoint(-(m_PositionPixel.X - m_Width / 2), -(m_PositionPixel.Y - m_Height / 2));
            pt.Offset(m_CompensationOffset);
            m_RenderOffset.X = pt.X - m_DragPoint.X;
            m_RenderOffset.Y = pt.Y - m_DragPoint.Y;
        }

        UpdateCenterTileXYLocation();
    }

    /// <summary>
    ///     Drag map by offset in pixels.
    /// </summary>
    /// <param name="offset">The offset in pixels.</param>
    public void DragOffset(GPoint offset)
    {
        m_RenderOffset.Offset(offset);

        UpdateCenterTileXYLocation();

        if (m_CenterTileXYLocation != m_CenterTileXYLocationLast)
        {
            m_CenterTileXYLocationLast = m_CenterTileXYLocation;
            UpdateBounds();
        }

        {
            LastLocationInBounds = Position;

            IsDragging = true;
            Position = FromLocalToLatLng(m_Width / 2, m_Height / 2);
            IsDragging = false;
        }

        OnMapDrag?.Invoke();
    }

    /// <summary>
    ///     drag map
    /// </summary>
    /// <param name="pt"></param>
    public void Drag(GPoint pt)
    {
        m_RenderOffset.X = pt.X - m_DragPoint.X;
        m_RenderOffset.Y = pt.Y - m_DragPoint.Y;

        UpdateCenterTileXYLocation();

        if (m_CenterTileXYLocation != m_CenterTileXYLocationLast)
        {
            m_CenterTileXYLocationLast = m_CenterTileXYLocation;
            UpdateBounds();
        }

        if (IsDragging)
        {
            LastLocationInBounds = Position;
            Position = FromLocalToLatLng(m_Width / 2, m_Height / 2);

            OnMapDrag?.Invoke();
        }
    }

    /// <summary>
    ///     cancels tile loaders and bounds checker
    /// </summary>
    public void CancelAsyncTasks()
    {
        if (m_IsStarted)
        {
#if NETFRAMEWORK
            //TODO: clear loading
#else
            Monitor.Enter(TileLoadQueue);
            try
            {
                TileLoadQueue.Clear();
            }
            finally
            {
                Monitor.Exit(TileLoadQueue);
            }
#endif
        }
    }

    bool m_RaiseEmptyTileError;

    internal Dictionary<LoadTask, Exception> m_FailedLoads =
        new(new LoadTaskComparer());

    internal static readonly int m_WaitForTileLoadThreadTimeout = 5 * 1000 * 60; // 5 min.

    volatile int m_OkZoom;
    volatile int m_SkipOverZoom;

#if NETFRAMEWORK
    static readonly BlockingCollection<LoadTask> m_TileLoadQueue4 =
        new(new ConcurrentStack<LoadTask>());

    static List<Task> m_TileLoadQueue4Tasks;
    static int m_LoadWaitCount;
    void AddLoadTask(LoadTask t)
    {
        if (m_TileLoadQueue4Tasks == null)
        {
            lock (m_TileLoadQueue4)
            {
                if (m_TileLoadQueue4Tasks == null)
                {
                    m_TileLoadQueue4Tasks = [];

                    while (m_TileLoadQueue4Tasks.Count < m_GThreadPoolSize)
                    {
                        Debug.WriteLine("creating ProcessLoadTask: " + m_TileLoadQueue4Tasks.Count);

                        m_TileLoadQueue4Tasks.Add(Task.Factory.StartNew(delegate ()
                            {
                                string ctid = "ProcessLoadTask[" + Environment.CurrentManagedThreadId + "]";
                                Thread.CurrentThread.Name = ctid;

                                Debug.WriteLine(ctid + ": started");
                                do
                                {
                                    if (m_TileLoadQueue4.Count == 0)
                                    {
                                        Debug.WriteLine(ctid + ": ready");

                                        if (Interlocked.Increment(ref m_LoadWaitCount) >= m_GThreadPoolSize)
                                        {
                                            Interlocked.Exchange(ref m_LoadWaitCount, 0);
                                            OnLoadComplete(ctid);
                                        }
                                    }

                                    ProcessLoadTask(m_TileLoadQueue4.Take(), ctid);
                                } while (!m_TileLoadQueue4.IsAddingCompleted);

                                Debug.WriteLine(ctid + ": exit");
                            },
                            TaskCreationOptions.LongRunning));
                    }
                }
            }
        }

        m_TileLoadQueue4.Add(t);
    }
#else
    byte _loadWaitCount = 0;

    void TileLoadThread()
    {
        LoadTask? task = null;
        bool stop = false;

        var ct = Thread.CurrentThread;
        string ctid = "Thread[" + ct.ManagedThreadId + "]";
        while (!stop && IsStarted)
        {
            task = null;

            Monitor.Enter(TileLoadQueue);
            try
            {
                while (TileLoadQueue.Count == 0)
                {
                    Debug.WriteLine(ctid + " - Wait " + _loadWaitCount + " - " + DateTime.Now.TimeOfDay);

                    if (++_loadWaitCount >= GThreadPoolSize)
                    {
                        _loadWaitCount = 0;
                        OnLoadComplete(ctid);
                    }

                    if (!IsStarted || false == Monitor.Wait(TileLoadQueue, WaitForTileLoadThreadTimeout, false) || !IsStarted)
                    {
                        stop = true;
                        break;
                    }
                }

                if (IsStarted && !stop || TileLoadQueue.Count > 0)
                {
                    task = TileLoadQueue.Pop();
                }
            }
            finally
            {
                Monitor.Exit(TileLoadQueue);
            }

            if (task.HasValue && IsStarted)
            {
                ProcessLoadTask(task.Value, ctid);
            }
        }

        Monitor.Enter(TileLoadQueue);
        try
        {
            Debug.WriteLine("Quit - " + ct.Name);
            lock (_gThreadPool)
            {
                _gThreadPool.Remove(ct);
            }
        }
        finally
        {
            Monitor.Exit(TileLoadQueue);
        }
    }
#endif

    static void ProcessLoadTask(LoadTask task, string ctid)
    {
        try
        {
            #region -- execute --

            var matrix = task.m_Core.Matrix;
            if (matrix == null)
            {
                return;
            }

            var m = task.m_Core.Matrix.GetTileWithReadLock(task.Zoom, task.Pos);
            if (!m.NotEmpty)
            {
                Debug.WriteLine(ctid + " - try load: " + task);

                var t = new Tile(task.Zoom, task.Pos);

                foreach (var tl in task.m_Core.m_Provider.Overlays)
                {
                    int retry = 0;
                    do
                    {
                        PureImage img = null;
                        Exception ex = null;

                        if (task.Zoom >= task.m_Core.m_Provider.MinZoom &&
                            (!task.m_Core.m_Provider.MaxZoom.HasValue || task.Zoom <= task.m_Core.m_Provider.MaxZoom))
                        {
                            if (task.m_Core.m_SkipOverZoom == 0 || task.Zoom <= task.m_Core.m_SkipOverZoom)
                            {
                                // tile number inversion(BottomLeft -> TopLeft)
                                if (tl.InvertedAxisY)
                                {
                                    img = GMaps.Instance.GetImageFrom(tl,
                                        new GPoint(task.Pos.X, task.m_Core.m_MaxOfTiles.Height - task.Pos.Y),
                                        task.Zoom,
                                        out ex);
                                }
                                else // ok
                                {
                                    img = GMaps.Instance.GetImageFrom(tl, task.Pos, task.Zoom, out ex);
                                }
                            }
                        }

                        if (img != null && ex == null)
                        {
                            if (task.m_Core.m_OkZoom < task.Zoom)
                            {
                                task.m_Core.m_OkZoom = task.Zoom;
                                task.m_Core.m_SkipOverZoom = 0;
                                Debug.WriteLine("skipOverZoom disabled, okZoom: " + task.m_Core.m_OkZoom);
                            }
                        }
                        else if (ex != null)
                        {
                            if (task.m_Core.m_SkipOverZoom != task.m_Core.m_OkZoom && task.Zoom > task.m_Core.m_OkZoom)
                            {
                                if (ex.Message.Contains("(404) Not Found"))
                                {
                                    task.m_Core.m_SkipOverZoom = task.m_Core.m_OkZoom;
                                    Debug.WriteLine("skipOverZoom enabled: " + task.m_Core.m_SkipOverZoom);
                                }
                            }
                        }

                        // check for parent tiles if not found
                        if (img == null && task.m_Core.m_OkZoom > 0 && task.m_Core.m_FillEmptyTiles &&
                            task.m_Core.Provider.Projection is MercatorProjection)
                        {
                            int zoomOffset = task.Zoom > task.m_Core.m_OkZoom ? task.Zoom - task.m_Core.m_OkZoom : 1;
                            long ix = 0;
                            var parentTile = GPoint.Empty;

                            while (img == null && zoomOffset < task.Zoom)
                            {
                                ix = (long)Math.Pow(2, zoomOffset);
                                parentTile = new GPoint(task.Pos.X / ix, task.Pos.Y / ix);
                                img = GMaps.Instance.GetImageFrom(tl, parentTile, task.Zoom - zoomOffset++, out ex);
                            }

                            if (img != null)
                            {
                                // offsets in quadrant
                                long xOff = Math.Abs(task.Pos.X - parentTile.X * ix);
                                long yOff = Math.Abs(task.Pos.Y - parentTile.Y * ix);

                                img.m_IsParent = true;
                                img.m_Ix = ix;
                                img.m_Xoff = xOff;
                                img.m_Yoff = yOff;

                                // WPF
                                //var geometry = new RectangleGeometry(new Rect(Core.tileRect.X + 0.6, Core.tileRect.Y + 0.6, Core.tileRect.Width + 0.6, Core.tileRect.Height + 0.6));
                                //var parentImgRect = new Rect(Core.tileRect.X - Core.tileRect.Width * Xoff + 0.6, Core.tileRect.Y - Core.tileRect.Height * Yoff + 0.6, Core.tileRect.Width * Ix + 0.6, Core.tileRect.Height * Ix + 0.6);

                                // GDI+
                                //System.Drawing.Rectangle dst = new System.Drawing.Rectangle((int)Core.tileRect.X, (int)Core.tileRect.Y, (int)Core.tileRect.Width, (int)Core.tileRect.Height);
                                //System.Drawing.RectangleF srcRect = new System.Drawing.RectangleF((float)(Xoff * (img.Img.Width / Ix)), (float)(Yoff * (img.Img.Height / Ix)), (img.Img.Width / Ix), (img.Img.Height / Ix));
                            }
                        }

                        if (img != null)
                        {
                            Debug.WriteLine(ctid + " - tile loaded: " + img.Data.Length / 1024 + "KB, " + task);
                            {
                                t.AddOverlay(img);
                            }
                            break;
                        }
                        else
                        {
                            if (ex != null && task.m_Core.m_FailedLoads != null)
                            {
                                lock (task.m_Core.m_FailedLoads)
                                {
                                    if (task.m_Core.m_FailedLoads.TryAdd(task, ex))
                                    {
                                        if (task.m_Core.OnEmptyTileError != null)
                                        {
                                            if (!task.m_Core.m_RaiseEmptyTileError)
                                            {
                                                task.m_Core.m_RaiseEmptyTileError = true;
                                                task.m_Core.OnEmptyTileError(task.Zoom, task.Pos);
                                            }
                                        }
                                    }
                                }
                            }

                            if (task.m_Core.RetryLoadTile > 0)
                            {
                                Debug.WriteLine(ctid + " - ProcessLoadTask: " + task + " -> empty tile, retry " +
                                                retry);
                                {
                                    Thread.Sleep(1111);
                                }
                            }
                        }
                    } while (++retry < task.m_Core.RetryLoadTile);
                }

                if (t.HasAnyOverlays && task.m_Core.m_IsStarted)
                {
                    task.m_Core.Matrix.SetTile(t);
                }
                else
                {
                    t.Dispose();
                }
            }

            #endregion
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ctid + " - ProcessLoadTask: " + ex.ToString());
        }
        finally
        {
            task.m_Core.Refresh?.Set();
        }
    }

    void OnLoadComplete(string ctid)
    {
        m_LastTileLoadEnd = DateTime.Now;
        long lastTileLoadTimeMs = (long)(m_LastTileLoadEnd - m_LastTileLoadStart).TotalMilliseconds;

        #region -- clear stuff--

        if (m_IsStarted)
        {
            GMaps.Instance.MemoryCache.RemoveOverload();

            m_TileDrawingListLock.AcquireReaderLock();
            try
            {
                Matrix.ClearLevelAndPointsNotIn(Zoom, m_TileDrawingList);
            }
            finally
            {
                m_TileDrawingListLock.ReleaseReaderLock();
            }
        }

        #endregion

        UpdateGroundResolution();
#if UseGC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
#endif
        Debug.WriteLine(ctid + " - OnTileLoadComplete: " + lastTileLoadTimeMs + "ms, MemoryCacheSize: " +
                        GMaps.Instance.MemoryCache.Size + "MB");

        OnTileLoadComplete?.Invoke(lastTileLoadTimeMs);
    }

    public AutoResetEvent Refresh = new(false);

    public bool UpdatingBounds;

    /// <summary>
    ///     updates map bounds
    /// </summary>
    void UpdateBounds()
    {
        if (!m_IsStarted || Provider.Equals(EmptyProvider.Instance))
        {
            return;
        }

        UpdatingBounds = true;

        m_TileDrawingListLock.AcquireWriterLock();
        try
        {
            #region -- find tiles around --

            m_TileDrawingList.Clear();

            for (long i = (int)Math.Floor(-m_SizeOfMapArea.Width * m_ScaleX),
                countI = (int)Math.Ceiling(m_SizeOfMapArea.Width * m_ScaleX);
                i <= countI;
                i++)
            {
                for (long j = (int)Math.Floor(-m_SizeOfMapArea.Height * m_ScaleY),
                    countJ = (int)Math.Ceiling(m_SizeOfMapArea.Height * m_ScaleY);
                    j <= countJ;
                    j++)
                {
                    var p = m_CenterTileXYLocation;
                    p.X += i;
                    p.Y += j;

#if ContinuesMap
           // ----------------------------
           if(p.X < minOfTiles.Width)
           {
              p.X += (maxOfTiles.Width + 1);
           }

           if(p.X > maxOfTiles.Width)
           {
              p.X -= (maxOfTiles.Width + 1);
           }
           // ----------------------------
#endif

                    if (p.X >= m_MinOfTiles.Width && p.Y >= m_MinOfTiles.Height && p.X <= m_MaxOfTiles.Width &&
                        p.Y <= m_MaxOfTiles.Height)
                    {
                        var dt = new DrawTile()
                        {
                            PosXY = p,
                            PosPixel = new GPoint(p.X * m_TileRect.Width, p.Y * m_TileRect.Height),
                            DistanceSqr = (m_CenterTileXYLocation.X - p.X) * (m_CenterTileXYLocation.X - p.X) +
                                          (m_CenterTileXYLocation.Y - p.Y) * (m_CenterTileXYLocation.Y - p.Y)
                        };

                        if (!m_TileDrawingList.Contains(dt))
                        {
                            m_TileDrawingList.Add(dt);
                        }
                    }
                }
            }

            if (GMaps.Instance.ShuffleTilesOnLoad)
            {
                Stuff.Shuffle(m_TileDrawingList);
            }
            else
            {
                m_TileDrawingList.Sort();
            }

            #endregion
        }
        finally
        {
            m_TileDrawingListLock.ReleaseWriterLock();
        }

#if NETFRAMEWORK
        Interlocked.Exchange(ref m_LoadWaitCount, 0);
#else
        Monitor.Enter(TileLoadQueue);
        try
        {
#endif
        m_TileDrawingListLock.AcquireReaderLock();
        try
        {
            foreach (var p in m_TileDrawingList)
            {
                var task = new LoadTask(p.PosXY, Zoom, this);
#if NETFRAMEWORK
                AddLoadTask(task);
#else
                    {
                        if (!TileLoadQueue.Contains(task))
                        {
                            TileLoadQueue.Push(task);
                        }
                    }
#endif
            }
        }
        finally
        {
            m_TileDrawingListLock.ReleaseReaderLock();
        }

#if !NETFRAMEWORK
        #region -- starts loader threads if needed --

            lock (_gThreadPool)
            {
                while (_gThreadPool.Count < GThreadPoolSize)
                {
                    var t = new Thread(TileLoadThread);
                    {
                        t.Name = "TileLoader: " + _gThreadPool.Count;
                        t.IsBackground = true;
                        t.Priority = ThreadPriority.BelowNormal;
                    }

                    _gThreadPool.Add(t);

                    Debug.WriteLine("add " + t.Name + " to GThreadPool");

                    t.Start();
                }
            }
        #endregion
#endif
        {
            m_LastTileLoadStart = DateTime.Now;
            Debug.WriteLine("OnTileLoadStart - at zoom " + Zoom + ", time: " + m_LastTileLoadStart.TimeOfDay);
        }
#if !NETFRAMEWORK
            _loadWaitCount = 0;
            Monitor.PulseAll(TileLoadQueue);
        }
        finally
        {
            Monitor.Exit(TileLoadQueue);
        }
#endif
        UpdatingBounds = false;

        OnTileLoadStart?.Invoke();
    }

    /// <summary>
    ///     updates ground resolution info
    /// </summary>
    void UpdateGroundResolution()
    {
        double rez = Provider.Projection.GetGroundResolution(Zoom, Position.Lat);
        m_PxRes100M = (int)(100.0 / rez); // 100 meters
        m_PxRes1000M = (int)(1000.0 / rez); // 1km  
        m_PxRes10Km = (int)(10000.0 / rez); // 10km
        m_PxRes100Km = (int)(100000.0 / rez); // 100km
        m_PxRes1000Km = (int)(1000000.0 / rez); // 1000km
        m_PxRes5000Km = (int)(5000000.0 / rez); // 5000km
    }

    #region IDisposable Members

    ~Core()
    {
        Dispose(false);
    }

    void Dispose(bool disposing)
    {
        if (m_IsStarted)
        {
            if (m_Invalidator != null)
            {
                m_Invalidator.CancelAsync();
                m_Invalidator.DoWork -= InvalidatorWatch;
                m_Invalidator.Dispose();
                m_Invalidator = null;
            }

            if (Refresh != null)
            {
                Refresh.Set();
                Refresh.Close();
                Refresh = null;
            }

            int x = Interlocked.Decrement(ref m_Instances);
            Debug.WriteLine("OnMapClose: " + x);

            CancelAsyncTasks();
            m_IsStarted = false;

            if (Matrix != null)
            {
                Matrix.Dispose();
                Matrix = null;
            }

            if (m_FailedLoads != null)
            {
                lock (m_FailedLoads)
                {
                    m_FailedLoads.Clear();
                    m_RaiseEmptyTileError = false;
                }

                m_FailedLoads = null;
            }

            m_TileDrawingListLock.AcquireWriterLock();
            try
            {
                m_TileDrawingList.Clear();
            }
            finally
            {
                m_TileDrawingListLock.ReleaseWriterLock();
            }

#if NETFRAMEWORK
            //TODO: maybe
#else
            // cancel waiting loaders
            Monitor.Enter(TileLoadQueue);
            try
            {
                Monitor.PulseAll(TileLoadQueue);
            }
            finally
            {
                Monitor.Exit(TileLoadQueue);
            }
#endif

            if (m_TileDrawingListLock != null)
            {
                m_TileDrawingListLock.Dispose();
                m_TileDrawingListLock = null;
                m_TileDrawingList = null;
            }

            if (x == 0)
            {
#if DEBUG
                GMaps.Instance.CancelTileCaching();
#endif
                GMaps.Instance.m_NoMapInstances = true;
                GMaps.Instance.m_WaitForCache.Set();
                if (disposing)
                {
                    GMaps.Instance.MemoryCache.Clear();
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
