using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using GMap.NET.Internals;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;

namespace GMap.NET;

/// <summary>
///     form helping to prefetch tiles on local db
/// </summary>
public partial class TilePrefetcher : Form
{
    readonly BackgroundWorker m_Worker = new();
    List<GPoint> m_List;
    int m_Zoom;
    GMapProvider m_Provider;
    int m_Sleep;
    int m_All;
    public bool ShowCompleteMessage = false;
    RectLatLng m_Area;
    GSize m_MaxOfTiles;
    public GMapOverlay Overlay;
    int m_Retry;
    public bool Shuffle = true;

    public TilePrefetcher()
    {
        InitializeComponent();

        GMaps.Instance.OnTileCacheComplete += OnTileCacheComplete;
        GMaps.Instance.OnTileCacheStart += OnTileCacheStart;
        GMaps.Instance.OnTileCacheProgress += OnTileCacheProgress;

        m_Worker.WorkerReportsProgress = true;
        m_Worker.WorkerSupportsCancellation = true;
        m_Worker.ProgressChanged += Worker_ProgressChanged;
        m_Worker.DoWork += Worker_DoWork;
        m_Worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
    }

    readonly AutoResetEvent m_Done = new(true);

    void OnTileCacheComplete()
    {
        if (!IsDisposed)
        {
            m_Done.Set();

            MethodInvoker m = delegate
            {
                label2.Text = "all tiles saved";
            };
            Invoke(m);
        }
    }

    void OnTileCacheStart()
    {
        if (!IsDisposed)
        {
            m_Done.Reset();

            MethodInvoker m = delegate
            {
                label2.Text = "saving tiles...";
            };
            Invoke(m);
        }
    }

    void OnTileCacheProgress(int left)
    {
        if (!IsDisposed)
        {
            MethodInvoker m = delegate
            {
                label2.Text = left + " tile to save...";
            };
            Invoke(m);
        }
    }

    public void Start(RectLatLng area, int zoom, GMapProvider provider, int sleep, int retry)
    {
        if (!m_Worker.IsBusy)
        {
            label1.Text = "...";
            progressBarDownload.Value = 0;

            m_Area = area;
            m_Zoom = zoom;
            m_Provider = provider;
            m_Sleep = sleep;
            m_Retry = retry;

            GMaps.Instance.UseMemoryCache = false;
            GMaps.Instance.CacheOnIdleRead = false;
            GMaps.Instance.BoostCacheEngine = true;

            Overlay?.Markers.Clear();

            m_Worker.RunWorkerAsync();

            ShowDialog();
        }
    }

    public void Stop()
    {
        GMaps.Instance.OnTileCacheComplete -= OnTileCacheComplete;
        GMaps.Instance.OnTileCacheStart -= OnTileCacheStart;
        GMaps.Instance.OnTileCacheProgress -= OnTileCacheProgress;

        m_Done.Set();

        if (m_Worker.IsBusy)
        {
            m_Worker.CancelAsync();
        }

        GMaps.Instance.CancelTileCaching();

        m_Done.Close();
    }

    void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        if (ShowCompleteMessage)
        {
            if (!e.Cancelled)
            {
                MessageBox.Show(this, "Prefetch Complete! => " + ((int)e.Result).ToString() + " of " + m_All);
            }
            else
            {
                MessageBox.Show(this, "Prefetch Canceled! => " + ((int)e.Result).ToString() + " of " + m_All);
            }
        }

        m_List.Clear();

        GMaps.Instance.UseMemoryCache = true;
        GMaps.Instance.CacheOnIdleRead = true;
        GMaps.Instance.BoostCacheEngine = false;

        m_Worker.Dispose();

        Close();
    }

    bool CacheTiles(int zoom, GPoint p)
    {
        foreach (var pr in m_Provider.Overlays)
        {
            PureImage img;

            // tile number inversion(BottomLeft -> TopLeft)
            if (pr.InvertedAxisY)
            {
                img = GMaps.Instance.GetImageFrom(pr, new GPoint(p.X, m_MaxOfTiles.Height - p.Y), zoom, out _);
            }
            else // ok
            {
                img = GMaps.Instance.GetImageFrom(pr, p, zoom, out _);
            }

            if (img != null)
            {
                img.Dispose();
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    public readonly Queue<GPoint> CachedTiles = new();

    void Worker_DoWork(object sender, DoWorkEventArgs e)
    {
        if (m_List != null)
        {
            m_List.Clear();
            m_List = null;
        }

        m_List = m_Provider.Projection.GetAreaTileList(m_Area, m_Zoom, 0);
        m_MaxOfTiles = m_Provider.Projection.GetTileMatrixMaxXY(m_Zoom);
        m_All = m_List.Count;

        int countOk = 0;
        int retryCount = 0;

        if (Shuffle)
        {
            Stuff.Shuffle(m_List);
        }

        lock (this)
        {
            CachedTiles.Clear();
        }

        for (int i = 0; i < m_All; i++)
        {
            if (m_Worker.CancellationPending)
            {
                break;
            }

            var p = m_List[i];
            {
                if (CacheTiles(m_Zoom, p))
                {
                    if (Overlay != null)
                    {
                        lock (this)
                        {
                            CachedTiles.Enqueue(p);
                        }
                    }

                    countOk++;
                    retryCount = 0;
                }
                else
                {
                    if (++retryCount <= m_Retry) // retry only one
                    {
                        i--;
                        Thread.Sleep(1111);
                        continue;
                    }
                    else
                    {
                        retryCount = 0;
                    }
                }
            }

            m_Worker.ReportProgress((i + 1) * 100 / m_All, i + 1);

            if (m_Sleep > 0)
            {
                Thread.Sleep(m_Sleep);
            }
        }

        e.Result = countOk;

        if (!IsDisposed)
        {
            m_Done.WaitOne();
        }
    }

    void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        label1.Text = "Fetching tile at zoom (" + m_Zoom + "): " + ((int)e.UserState).ToString() + " of " + m_All +
                           ", complete: " + e.ProgressPercentage.ToString() + "%";
        progressBarDownload.Value = e.ProgressPercentage;

        if (Overlay is null)
        {
            // Exit early.
            return;
        }

        GPoint? l = null;

        lock (this)
        {
            if (CachedTiles.Count > 0)
            {
                l = CachedTiles.Dequeue();
            }
        }

        if (l.HasValue)
        {
            var px = Overlay.Control.MapProvider.Projection.FromTileXYToPixel(l.Value);
            var p = Overlay.Control.MapProvider.Projection.FromPixelToLatLng(px, m_Zoom);

            double r1 = Overlay.Control.MapProvider.Projection.GetGroundResolution(m_Zoom, p.Lat);
            double r2 = Overlay.Control.MapProvider.Projection.GetGroundResolution((int)Overlay.Control.Zoom,
                p.Lat);
            double sizeDiff = r2 / r1;

            var m = new GMapMarkerTile(p,
                (int)(Overlay.Control.MapProvider.Projection.TileSize.Width / sizeDiff));
            Overlay.Markers.Add(m);
        }
    }

    private void Prefetch_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
        }
    }

    private void Prefetch_FormClosed(object sender, FormClosedEventArgs e)
    {
        Stop();
    }
}

class GMapMarkerTile : GMapMarker
{
    static readonly Brush m_Fill = new SolidBrush(Color.FromArgb(155, Color.Blue));

    public GMapMarkerTile(PointLatLng p, int size) : base(p)
    {
        Size = new Size(size, size);
    }

    public override void OnRender(Graphics g)
    {
        g.FillRectangle(m_Fill, new Rectangle(LocalPosition.X, LocalPosition.Y, Size.Width, Size.Height));
    }
}
