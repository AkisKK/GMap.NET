using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using GMap.NET.MapProviders;

namespace GMap.NET.Internals;

internal class TileHttpHost
{
    volatile bool m_Listen;
    TcpListener m_Server;
    int m_Port;

    readonly byte[] m_ResponseHeaderBytes;

    public TileHttpHost()
    {
        string response = "HTTP/1.0 200 OK\r\nContent-Type: image\r\nConnection: close\r\n\r\n";
        m_ResponseHeaderBytes = Encoding.ASCII.GetBytes(response);
    }

    public void Stop()
    {
        if (m_Listen)
        {
            m_Listen = false;
            m_Server?.Stop();
        }
    }

    public void Start(int port)
    {
        if (m_Server == null)
        {
            m_Port = port;
            m_Server = new TcpListener(IPAddress.Any, port);
        }
        else
        {
            if (m_Port != port)
            {
                Stop();
                m_Port = port;
                m_Server = null;
                m_Server = new TcpListener(IPAddress.Any, port);
            }
            else
            {
                if (m_Listen)
                {
                    return;
                }
            }
        }

        m_Server.Start();
        m_Listen = true;

        var t = new Thread(() =>
        {
            Debug.WriteLine("TileHttpHost: " + m_Server.LocalEndpoint);

            while (m_Listen)
            {
                try
                {
                    if (!m_Server.Pending())
                    {
                        Thread.Sleep(111);
                    }
                    else
                    {
                        ThreadPool.QueueUserWorkItem(ProcessRequest, m_Server.AcceptTcpClient());
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("TileHttpHost: " + ex);
                }
            }

            Debug.WriteLine("TileHttpHost: stopped");
        })
        {
            Name = "TileHost",
            IsBackground = true
        };
        t.Start();
    }

    void ProcessRequest(object p)
    {
        try
        {
            using var c = p as TcpClient;
            using var s = c.GetStream();
            using var r = new StreamReader(s, Encoding.UTF8);
            string request = r.ReadLine();

            if (!string.IsNullOrEmpty(request) && request.StartsWith("GET"))
            {
                //Debug.WriteLine("TileHttpHost: " + request);

                // http://localhost:88/88888/5/15/11
                // GET /8888888888/5/15/11 HTTP/1.1

                string[] rq = request.Split(' ');

                if (rq.Length >= 2)
                {
                    string[] ids = rq[1].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                    if (ids.Length == 4)
                    {
                        int dbId = int.Parse(ids[0]);
                        int zoom = int.Parse(ids[1]);
                        int x = int.Parse(ids[2]);
                        int y = int.Parse(ids[3]);

                        var pr = GMapProviders.TryGetProvider(dbId);
                        if (pr != null)
                        {
                            var img = GMaps.Instance.GetImageFrom(pr, new GPoint(x, y), zoom, out var ex);

                            if (img != null)
                            {
                                using (img)
                                {
                                    s.Write(m_ResponseHeaderBytes, 0, m_ResponseHeaderBytes.Length);
                                    img.Data.WriteTo(s);
                                }
                            }
                        }
                    }
                }
            }

            c.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine("TileHttpHost, ProcessRequest: " + ex);
        }

        //Debug.WriteLine("disconnected");
    }
}
