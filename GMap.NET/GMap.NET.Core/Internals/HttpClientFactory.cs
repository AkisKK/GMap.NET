using System.Net;
using System.Net.Http;
using System.Threading;
using GMap.NET.MapProviders;

namespace GMap.NET.Internals;

/// <summary>
/// Factory class used to create to <see cref="HttpClient"/> based on a single <see cref="HttpClientHandler"/>.
/// </summary>
/// <remarks>
/// The class also support authentication and proxy, which requires <see cref="Credentials"/> and <see cref="WebProxy"/>
/// to be set before calling <see cref="CreateClient()"/> for the first time.
/// </remarks>
public static class HttpClientFactory
{
    static readonly Lock m_Lock = new();

    /// <summary>
    ///     NetworkCredential for tile HTTP access
    /// </summary>
    public static ICredentials Credentials { get; set; } = null;

    /// <summary>
    ///     proxy for net access
    /// </summary>
    public static IWebProxy WebProxy { get; set; } = EmptyWebProxy.Instance;

    /// <summary>
    /// The static <see cref="HttpClientHandler"/> which will be shared by all created <see cref="HttpClient"/>.
    /// </summary>
    private static HttpClientHandler m_HttpClientHandler;

    /// <summary>
    /// Function to create an <see cref="HttpClient"/> which uses a static shared <see cref="HttpClientHandler"/>.
    /// </summary>
    /// <returns>An <see cref="HttpClient"/>.</returns>
    public static HttpClient CreateClient()
    {
        lock (m_Lock)
        {
            if (m_HttpClientHandler is null)
            {
                m_HttpClientHandler = new();
                if (Credentials is not null)
                {
                    m_HttpClientHandler.PreAuthenticate = true;
                    m_HttpClientHandler.Credentials = Credentials;
                }
                if (WebProxy is not null)
                {
                    m_HttpClientHandler.Proxy = WebProxy;
                }
            }
        }
        // client.DefaultRequestHeaders.Add("User-Agent", "HttpClientFactorySample");
        // Add any default headers or settings here.

        return new HttpClient(m_HttpClientHandler, disposeHandler: false);
    }
}
