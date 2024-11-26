using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace GMap.NET.Internals.SocksProxySocket;

// http://www.mentalis.org/soft/class.qpx?id=9

/// <summary>
///     http://ditrans.blogspot.com/2009/03/making-witty-work-with-socks-proxy.html
/// </summary>
internal class SocksHttpWebRequest : WebRequest
{
    #region Member Variables
    private WebHeaderCollection m_RequestHeaders;
    private string m_Method;
    private SocksHttpWebResponse m_Response;
    private string m_RequestMessage;
    private byte[] m_RequestContentBuffer;

    // darn MS for making everything internal (yeah, I'm talking about you, System.net.KnownHttpVerb)
    static readonly StringCollection m_ValidHttpVerbs =
        [
            "GET",
            "HEAD",
            "POST",
            "PUT",
            "DELETE",
            "TRACE",
            "OPTIONS"
        ];
    #endregion

    #region Constructor
    private SocksHttpWebRequest(Uri requestUri)
    {
        RequestUri = requestUri;
    }
    #endregion

    #region WebRequest Members
    public override WebResponse GetResponse()
    {
        if (Proxy == null)
        {
            throw new InvalidOperationException("Proxy property cannot be null.");
        }

        if (string.IsNullOrEmpty(Method))
        {
            throw new InvalidOperationException("Method has not been set.");
        }

        if (RequestSubmitted)
        {
            return m_Response;
        }

        m_Response = InternalGetResponse();
        RequestSubmitted = true;
        return m_Response;
    }

    public override Uri RequestUri { get; }

    public override IWebProxy Proxy { get; set; }

    public override WebHeaderCollection Headers
    {
        get
        {
            m_RequestHeaders ??= [];

            return m_RequestHeaders;
        }
        set
        {
            if (RequestSubmitted)
            {
                throw new InvalidOperationException(
                    "This operation cannot be performed after the request has been submitted.");
            }

            m_RequestHeaders = value;
        }
    }

    public bool RequestSubmitted { get; private set; }

    public override string Method
    {
        get => m_Method ?? "GET";
        set
        {
            if (m_ValidHttpVerbs.Contains(value))
            {
                m_Method = value;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    string.Format("'{0}' is not a known HTTP verb.", value));
            }
        }
    }

    public override long ContentLength { get; set; }

    public override string ContentType { get; set; }

    public override Stream GetRequestStream()
    {
        if (RequestSubmitted)
        {
            throw new InvalidOperationException(
                "This operation cannot be performed after the request has been submitted.");
        }

        if (m_RequestContentBuffer == null)
        {
            m_RequestContentBuffer = new byte[ContentLength];
        }
        else if (ContentLength == default)
        {
            m_RequestContentBuffer = new byte[int.MaxValue];
        }
        else if (m_RequestContentBuffer.Length != ContentLength)
        {
            Array.Resize(ref m_RequestContentBuffer, (int)ContentLength);
        }

        return new MemoryStream(m_RequestContentBuffer);
    }
    #endregion

    #region Methods
    public static new WebRequest Create(string requestUri)
    {
        return new SocksHttpWebRequest(new Uri(requestUri));
    }

    public static new WebRequest Create(Uri requestUri)
    {
        return new SocksHttpWebRequest(requestUri);
    }

    private string BuildHttpRequestMessage()
    {
        if (RequestSubmitted)
        {
            throw new InvalidOperationException(
                "This operation cannot be performed after the request has been submitted.");
        }

        var message = new StringBuilder();

        message.AppendFormat("{0} {1} HTTP/1.0\r\nHost: {2}\r\n", Method, RequestUri.PathAndQuery, RequestUri.Host);

        // add the headers
        foreach (object key in Headers.Keys)
        {
            message.AppendFormat("{0}: {1}\r\n", key, Headers[key.ToString()]);
        }

        if (!string.IsNullOrEmpty(ContentType))
        {
            message.AppendFormat("Content-Type: {0}\r\n", ContentType);
        }

        if (ContentLength > 0)
        {
            message.AppendFormat("Content-Length: {0}\r\n", ContentLength);
        }

        // add a blank line to indicate the end of the headers
        message.Append("\r\n");

        // add content
        if (m_RequestContentBuffer != null && m_RequestContentBuffer.Length > 0)
        {
            using var stream = new MemoryStream(m_RequestContentBuffer, false);
            using var reader = new StreamReader(stream);
            message.Append(reader.ReadToEnd());
        }

        return message.ToString();
    }

    private SocksHttpWebResponse InternalGetResponse()
    {
        MemoryStream data = null;
        string header = string.Empty;

        using (var socksConnection =
            new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            var proxyUri = Proxy.GetProxy(RequestUri);
            var ipAddress = GetProxyIpAddress(proxyUri);
            socksConnection.ProxyEndPoint = new IPEndPoint(ipAddress, proxyUri.Port);
            socksConnection.ProxyType = ProxyTypes.Socks5;

            // open connection
            socksConnection.Connect(RequestUri.Host, 80);

            // send an HTTP request
            socksConnection.Send(Encoding.UTF8.GetBytes(RequestMessage));

            // read the HTTP reply
            byte[] buffer = new byte[1024 * 4];
            int bytesReceived;
            bool headerDone = false;

            while ((bytesReceived = socksConnection.Receive(buffer)) > 0)
            {
                if (!headerDone)
                {
                    string headPart = Encoding.UTF8.GetString(buffer, 0, bytesReceived > 1024 ? 1024 : bytesReceived);
                    int indexOfFirstBlankLine = headPart.IndexOf("\r\n\r\n");
                    if (indexOfFirstBlankLine > 0)
                    {
                        headPart = headPart[..indexOfFirstBlankLine];
                        header += headPart;
                        headerDone = true;

                        int headerPartLength = Encoding.UTF8.GetByteCount(headPart) + 4;

                        // 0123456789
                        //   ----
                        if (headerPartLength < bytesReceived)
                        {
                            data = new MemoryStream();
                            data.Write(buffer, headerPartLength, bytesReceived - headerPartLength);
                        }
                    }
                    else
                    {
                        header += headPart;
                    }
                }
                else
                {
                    data ??= new MemoryStream();

                    data.Write(buffer, 0, bytesReceived);
                }
            }

            if (data != null)
            {
                data.Position = 0;
            }
        }

        return new SocksHttpWebResponse(data, header);
    }

    private static IPAddress GetProxyIpAddress(Uri proxyUri)
    {
        if (!IPAddress.TryParse(proxyUri.Host, out var ipAddress))
        {
            try
            {
                return Dns.GetHostEntry(proxyUri.Host).AddressList[0];
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    string.Format("Unable to resolve proxy hostname '{0}' to a valid IP address.", proxyUri.Host),
                    e);
            }
        }

        return ipAddress;
    }
    #endregion

    #region Properties
    public string RequestMessage
    {
        get
        {
            if (string.IsNullOrEmpty(m_RequestMessage))
            {
                m_RequestMessage = BuildHttpRequestMessage();
            }

            return m_RequestMessage;
        }
    }
    #endregion
}

internal class SocksHttpWebResponse : WebResponse
{
    #region Member Variables
    WebHeaderCollection m_HttpResponseHeaders;
    readonly MemoryStream m_Data;

    public override long ContentLength
    {
        get;
        set;
    }

    public override string ContentType
    {
        get;
        set;
    }
    #endregion

    #region Constructors
    public SocksHttpWebResponse(MemoryStream data, string headers)
    {
        m_Data = data;

        string[] headerValues = headers.Split(["\r\n"], StringSplitOptions.RemoveEmptyEntries);

        // ignore the first line in the header since it is the HTTP response code
        for (int i = 1; i < headerValues.Length; i++)
        {
            string[] headerEntry = headerValues[i].Split([':']);
            Headers.Add(headerEntry[0], headerEntry[1]);

            switch (headerEntry[0])
            {
                case "Content-Type":
                    {
                        ContentType = headerEntry[1];
                    }
                    break;

                case "Content-Length":
                    {
                        if (long.TryParse(headerEntry[1], out long r))
                        {
                            ContentLength = r;
                        }
                    }
                    break;
            }
        }
    }
    #endregion

    #region WebResponse Members
    public override Stream GetResponseStream()
    {
        return m_Data ?? Stream.Null;
    }

    public override void Close()
    {
        m_Data?.Close();

        /* the base implementation throws an exception */
    }

    public override WebHeaderCollection Headers
    {
        get
        {
            m_HttpResponseHeaders ??= [];

            return m_HttpResponseHeaders;
        }
    }
    #endregion
}
