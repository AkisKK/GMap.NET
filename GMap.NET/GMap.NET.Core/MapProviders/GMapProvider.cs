﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using GMap.NET.Internals;
using GMap.NET.Internals.SocksProxySocket;
using GMap.NET.MapProviders.ArcGIS;
using GMap.NET.MapProviders.Bing;
using GMap.NET.MapProviders.Custom;
using GMap.NET.MapProviders.Czech;
using GMap.NET.MapProviders.CzechOld;
using GMap.NET.MapProviders.Etc;
using GMap.NET.MapProviders.Google;
using GMap.NET.MapProviders.Google.China;
using GMap.NET.MapProviders.Google.Korea;
using GMap.NET.MapProviders.Here;
using GMap.NET.MapProviders.Lithuania;
using GMap.NET.MapProviders.NearMap;
using GMap.NET.MapProviders.OpenStreetMap;
using GMap.NET.MapProviders.UMP;
using GMap.NET.MapProviders.Yahoo;
using GMap.NET.MapProviders.Yandex;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders;

/// <summary>
///     providers that are already build in
/// </summary>
public class GMapProviders
{
    static GMapProviders()
    {
        #region Populate the list of supported built-in providers
        MapProviderList = [];

        var type = typeof(GMapProviders);

        foreach (var p in type.GetFields())
        {
            // static classes cannot be instanced, so use null...
            if (p.GetValue(null) is GMapProvider v)
            {
                MapProviderList.Add(v);
            }
        }
        #endregion

        #region Populate the hash tables for fast access
        m_ProviderGuidDictionary = [];
        foreach (var p in MapProviderList)
        {
            m_ProviderGuidDictionary.Add(p.Id, p);
        }

        m_ProviderDatabaseIdHashDictionary = [];
        foreach (var p in MapProviderList)
        {
            m_ProviderDatabaseIdHashDictionary.Add(p.DatabaseId, p);
        }
        #endregion
    }

    GMapProviders()
    {
    }

    public static readonly EmptyProvider EmptyProvider = EmptyProvider.Instance;

    public static readonly OpenCycleMapProvider OpenCycleMap = OpenCycleMapProvider.Instance;
    public static readonly OpenCycleLandscapeMapProvider OpenCycleLandscapeMap = OpenCycleLandscapeMapProvider.Instance;
    public static readonly OpenCycleTransportMapProvider OpenCycleTransportMap = OpenCycleTransportMapProvider.Instance;

    public static readonly OpenStreetMapProvider OpenStreetMap = OpenStreetMapProvider.Instance;
    public static readonly OpenStreetMapGraphHopperProvider OpenStreetMapGraphHopper = OpenStreetMapGraphHopperProvider.Instance;
    public static readonly OpenStreet4UMapProvider OpenStreet4UMap = OpenStreet4UMapProvider.Instance;
    public static readonly OpenStreetMapQuestProvider OpenStreetMapQuest = OpenStreetMapQuestProvider.Instance;
    public static readonly OpenStreetMapQuestSatelliteProvider OpenStreetMapQuestSatellite = OpenStreetMapQuestSatelliteProvider.Instance;
    public static readonly OpenStreetMapQuestHybridProvider OpenStreetMapQuestHybrid = OpenStreetMapQuestHybridProvider.Instance;
    public static readonly OpenSeaMapHybridProvider OpenSeaMapHybrid = OpenSeaMapHybridProvider.Instance;

#if OpenStreetOsm
    public static readonly OpenStreetOsmProvider OpenStreetOsm = OpenStreetOsmProvider.Instance;
#endif

#if OpenStreetMapSurfer
    public static readonly OpenStreetMapSurferProvider OpenStreetMapSurfer = OpenStreetMapSurferProvider.Instance;
    public static readonly OpenStreetMapSurferTerrainProvider OpenStreetMapSurferTerrain = OpenStreetMapSurferTerrainProvider.Instance;
#endif

    public static readonly WikiMapiaMapProvider WikiMapiaMap = WikiMapiaMapProvider.Instance;

    public static readonly BingMapProvider BingMap = BingMapProvider.Instance;
    public static readonly BingSatelliteMapProvider BingSatelliteMap = BingSatelliteMapProvider.Instance;
    public static readonly BingHybridMapProvider BingHybridMap = BingHybridMapProvider.Instance;
    public static readonly BingOSMapProvider BingOSMap = BingOSMapProvider.Instance;

    public static readonly YahooMapProvider YahooMap = YahooMapProvider.Instance;
    public static readonly YahooSatelliteMapProvider YahooSatelliteMap = YahooSatelliteMapProvider.Instance;
    public static readonly YahooHybridMapProvider YahooHybridMap = YahooHybridMapProvider.Instance;

    public static readonly GoogleMapProvider GoogleMap = GoogleMapProvider.Instance;
    public static readonly GoogleSatelliteMapProvider GoogleSatelliteMap = GoogleSatelliteMapProvider.Instance;
    public static readonly GoogleHybridMapProvider GoogleHybridMap = GoogleHybridMapProvider.Instance;
    public static readonly GoogleTerrainMapProvider GoogleTerrainMap = GoogleTerrainMapProvider.Instance;

    public static readonly GoogleChinaMapProvider GoogleChinaMap = GoogleChinaMapProvider.Instance;
    public static readonly GoogleChinaSatelliteMapProvider GoogleChinaSatelliteMap = GoogleChinaSatelliteMapProvider.Instance;
    public static readonly GoogleChinaHybridMapProvider GoogleChinaHybridMap = GoogleChinaHybridMapProvider.Instance;
    public static readonly GoogleChinaTerrainMapProvider GoogleChinaTerrainMap = GoogleChinaTerrainMapProvider.Instance;

    public static readonly GoogleKoreaMapProvider GoogleKoreaMap = GoogleKoreaMapProvider.Instance;
    public static readonly GoogleKoreaSatelliteMapProvider GoogleKoreaSatelliteMap = GoogleKoreaSatelliteMapProvider.Instance;
    public static readonly GoogleKoreaHybridMapProvider GoogleKoreaHybridMap = GoogleKoreaHybridMapProvider.Instance;

    public static readonly NearMapProvider NearMap = NearMapProvider.Instance;
    public static readonly NearSatelliteMapProvider NearSatelliteMap = NearSatelliteMapProvider.Instance;
    public static readonly NearHybridMapProvider NearHybridMap = NearHybridMapProvider.Instance;

    public static readonly HereMapProvider HereMap = HereMapProvider.Instance;
    public static readonly HereSatelliteMapProvider HereSatelliteMap = HereSatelliteMapProvider.Instance;
    public static readonly HereHybridMapProvider HereHybridMap = HereHybridMapProvider.Instance;
    public static readonly HereTerrainMapProvider HereTerrainMap = HereTerrainMapProvider.Instance;

    public static readonly YandexMapProvider YandexMap = YandexMapProvider.Instance;
    public static readonly YandexSatelliteMapProvider YandexSatelliteMap = YandexSatelliteMapProvider.Instance;
    public static readonly YandexHybridMapProvider YandexHybridMap = YandexHybridMapProvider.Instance;

    public static readonly LithuaniaMapProvider LithuaniaMap = LithuaniaMapProvider.Instance;
    public static readonly LithuaniaReliefMapProvider LithuaniaReliefMap = LithuaniaReliefMapProvider.Instance;
    public static readonly Lithuania3dMapProvider Lithuania3dMap = Lithuania3dMapProvider.Instance;
    public static readonly LithuaniaOrtoFotoMapProvider LithuaniaOrtoFotoMap = LithuaniaOrtoFotoMapProvider.Instance;
    public static readonly LithuaniaOrtoFotoOldMapProvider LithuaniaOrtoFotoOldMap = LithuaniaOrtoFotoOldMapProvider.Instance;
    public static readonly LithuaniaHybridMapProvider LithuaniaHybridMap = LithuaniaHybridMapProvider.Instance;
    public static readonly LithuaniaHybridOldMapProvider LithuaniaHybridOldMap = LithuaniaHybridOldMapProvider.Instance;
    public static readonly LithuaniaTOP50 LithuaniaTOP50Map = LithuaniaTOP50.Instance;

    public static readonly LatviaMapProvider LatviaMap = LatviaMapProvider.Instance;

    public static readonly MapBenderWMSProvider MapBenderWMSdemoMap = MapBenderWMSProvider.Instance;

    public static readonly TurkeyMapProvider TurkeyMap = TurkeyMapProvider.Instance;

    public static readonly CloudMadeMapProvider CloudMadeMap = CloudMadeMapProvider.Instance;

    public static readonly SpainMapProvider SpainMap = SpainMapProvider.Instance;

    public static readonly CzechMapProviderOld CzechOldMap = CzechMapProviderOld.Instance;
    public static readonly CzechSatelliteMapProviderOld CzechSatelliteOldMap = CzechSatelliteMapProviderOld.Instance;
    public static readonly CzechHybridMapProviderOld CzechHybridOldMap = CzechHybridMapProviderOld.Instance;
    public static readonly CzechTuristMapProviderOld CzechTuristOldMap = CzechTuristMapProviderOld.Instance;
    public static readonly CzechHistoryMapProviderOld CzechHistoryOldMap = CzechHistoryMapProviderOld.Instance;

    public static readonly CzechMapProvider CzechMap = CzechMapProvider.Instance;
    public static readonly CzechSatelliteMapProvider CzechSatelliteMap = CzechSatelliteMapProvider.Instance;
    public static readonly CzechHybridMapProvider CzechHybridMap = CzechHybridMapProvider.Instance;
    public static readonly CzechTuristMapProvider CzechTuristMap = CzechTuristMapProvider.Instance;

    public static readonly CzechTuristWinterMapProvider CzechTuristWinterMap = CzechTuristWinterMapProvider.Instance;
    public static readonly CzechHistoryMapProvider CzechHistoryMap = CzechHistoryMapProvider.Instance;
    public static readonly CzechGeographicMapProvider CzechGeographicMap = CzechGeographicMapProvider.Instance;

    public static readonly ArcGIS_Imagery_World_2D_MapProvider ArcGIS_Imagery_World_2D_Map = ArcGIS_Imagery_World_2D_MapProvider.Instance;
    public static readonly ArcGIS_ShadedRelief_World_2D_MapProvider ArcGIS_ShadedRelief_World_2D_Map = ArcGIS_ShadedRelief_World_2D_MapProvider.Instance;
    public static readonly ArcGIS_StreetMap_World_2D_MapProvider ArcGIS_StreetMap_World_2D_Map = ArcGIS_StreetMap_World_2D_MapProvider.Instance;
    public static readonly ArcGIS_Topo_US_2D_MapProvider ArcGIS_Topo_US_2D_Map = ArcGIS_Topo_US_2D_MapProvider.Instance;
    public static readonly ArcGIS_World_Physical_MapProvider ArcGIS_World_Physical_Map = ArcGIS_World_Physical_MapProvider.Instance;
    public static readonly ArcGIS_World_Shaded_Relief_MapProvider ArcGIS_World_Shaded_Relief_Map = ArcGIS_World_Shaded_Relief_MapProvider.Instance;
    public static readonly ArcGIS_World_Street_MapProvider ArcGIS_World_Street_Map = ArcGIS_World_Street_MapProvider.Instance;
    public static readonly ArcGIS_World_Terrain_Base_MapProvider ArcGIS_World_Terrain_Base_Map = ArcGIS_World_Terrain_Base_MapProvider.Instance;
    public static readonly ArcGIS_World_Topo_MapProvider ArcGIS_World_Topo_Map = ArcGIS_World_Topo_MapProvider.Instance;
    public static readonly ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_Map = ArcGIS_DarbAE_Q2_2011_NAVTQ_Eng_V5_MapProvider.Instance;

    public static readonly SwissTopoProvider SwissMap = SwissTopoProvider.Instance;

    public static readonly SwedenMapProvider SwedenMap = SwedenMapProvider.Instance;
    public static readonly SwedenMapProviderAlt SwedenMapAlternative = SwedenMapProviderAlt.Instance;

    public static readonly UMPMapProvider UMPMap = UMPMapProvider.Instance;

    public static readonly CustomMapProvider CustomMap = CustomMapProvider.Instance;

    /// <summary>
    /// Get all instances of the supported map providers.
    /// </summary>
    protected static List<GMapProvider> MapProviderList { get; }

    /// <summary>
    /// A static lock object used to synchronize access to the <see cref="MapProviderList"/> of supported map providers.
    /// </summary>
    protected static readonly Lock m_MapProviderListLock = new();

    //public static OpenStreetMapGraphHopperProvider OpenStreetMapGraphHopperProvider => openStreetMapGraphHopperProvider;

    /// <summary>
    /// A dictionary that maps each provider's unique identifier (<see cref="Guid"/>) to the corresponding
    /// <see cref="GMapProvider"/> instance.
    /// </summary>
    /// <remarks>This dictionary is used to associate a globally unique identifier (GUID) with specific
    /// <see cref="GMapProvider"/> objects, enabling efficient lookups and management of map providers.</remarks>
    static readonly Dictionary<Guid, GMapProvider> m_ProviderGuidDictionary;

    /// <summary>
    /// A static lock object used to synchronize access to the provider GUID dictionary.
    /// </summary>
    static readonly Lock m_ProviderGuidDictionaryLock = new();

    /// <summary>
    /// Attempts to retrieve a <see cref="GMapProvider"/> instance associated with the specified unique identifier.
    /// </summary>
    /// <remarks>This method is thread-safe and ensures synchronized access to the underlying provider dictionary.
    /// </remarks>
    /// <param name="id">The unique identifier of the provider to retrieve.</param>
    /// <returns>The <see cref="GMapProvider"/> instance associated with the specified <paramref name="id"/>, or
    /// <see langword="null"/> if no provider is found.</returns>
    public static GMapProvider TryGetProvider(Guid id)
    {
        lock (m_ProviderGuidDictionaryLock)
        {
            if (m_ProviderGuidDictionary.TryGetValue(id, out var ret))
            {
                return ret;
            }
        }

        return null;
    }

    /// <summary>
    /// A static, read-only dictionary that maps database IDs to their corresponding <see cref="GMapProvider"/>
    /// instances.
    /// </summary>
    /// <remarks>This dictionary is used to efficiently retrieve a <see cref="GMapProvider"/> based on its associated
    /// database ID. The keys represent unique integer IDs, and the values are <see cref="GMapProvider"/>objects. The
    /// database ID is derived from the provider's GUID (<see cref="GMapProvider.Id"/>) using a hash function.
    /// </remarks>
    static readonly Dictionary<int, GMapProvider> m_ProviderDatabaseIdHashDictionary;

    /// <summary>
    /// A static lock object used to synchronize access to the provider database ID hash dictionary.
    /// </summary>
    static readonly Lock m_ProviderDatabaseIdHashDictionaryLock = new();

    /// <summary>
    /// Attempts to retrieve a <see cref="GMapProvider"/> instance associated with the specified database Id.
    /// </summary>
    /// <remarks>This method is thread-safe and ensures synchronized access to the underlying provider dictionary.
    /// </remarks>
    /// <param name="databaseId">The unique identifier of the provider in the database.</param>
    /// <returns>The <see cref="GMapProvider"/> instance associated with the specified database Id, or
    /// <see langword="null"/> if no provider is found for the given Id.</returns>
    public static GMapProvider TryGetProvider(int databaseId)
    {
        lock (m_ProviderDatabaseIdHashDictionaryLock)
        {
            // Check if the database ID exists in the dictionary.
            if (m_ProviderDatabaseIdHashDictionary.TryGetValue(databaseId, out var ret))
            {
                return ret;
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to retrieve a map provider by its name.
    /// </summary>
    /// <remarks>This method searches the list of available map providers for a provider with a name that matches the
    /// specified <paramref name="providerName"/>. If multiple providers share the same name, the first
    /// match is returned. The search is thread-safe.</remarks>
    /// <param name="providerName">The name of the map provider to retrieve. This value is case-sensitive and cannot be
    /// <see langword="null"/>.</param>
    /// <returns>The <see cref="GMapProvider"/> instance matching the specified name, or <see langword="null"/> if no
    /// provider with the given name exists.</returns>
    public static GMapProvider TryGetProvider(string providerName)
    {
        lock (m_MapProviderListLock)
        {
            if (MapProviderList.Exists(x => x.Name == providerName))
            {
                return MapProviderList.Find(x => x.Name == providerName);
            }
            else
            {
                return null;
            }
        }
    }
}

/// <summary>
///     base class for each map provider
/// </summary>
public abstract class GMapProvider
{
    /// <summary>
    /// A unique provider Id. This is used to identify the provider in the system for caching and other purposes.
    /// </summary>
    public abstract Guid Id { get; protected set; }

    /// <summary>
    /// The provider name.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// The provider projection.
    /// </summary>
    public abstract PureProjection Projection { get; }

    /// <summary>
    /// The provider overlays.
    /// </summary>
    public abstract GMapProvider[] Overlays { get; }

    /// <summary>
    /// Gets a tile image using the implemented provider.
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="zoom"></param>
    /// <returns></returns>
    public abstract PureImage GetTileImage(GPoint pos, int zoom);

    /// <summary>
    /// A static, read-only list containing all available map providers.
    /// </summary>
    /// <remarks>This list is initialized with the available and initialized map providers. It is used to prevent
    /// multiple instances of the a provider with the same <see cref="Id"/> to be created.</remarks>
    protected static readonly List<GMapProvider> m_MapProviders = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="GMapProvider"/> class.
    /// </summary>
    /// <remarks>This constructor assigns a unique database identifier to the provider and ensures that no other
    /// provider with the same identifier already exists. If a duplicate identifier is detected, an exception is thrown.
    /// The provider is then registered in the global list of map providers.</remarks>
    /// <exception cref="Exception">Thrown if a provider with the same <see cref="Id"/> or <see cref="DatabaseId"/> already
    /// exists.</exception>
    protected GMapProvider()
    {
        DatabaseId = Math.Abs(BitConverter.ToInt32(SHA1.HashData(Id.ToByteArray()), 0));

        if (m_MapProviders.Exists(p => p.Id == Id || p.DatabaseId == DatabaseId))
        {
            throw new Exception("such provider id already exists, try regenerate your provider guid...");
        }

        m_MapProviders.Add(this);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GMapProvider"/> class with the specified unique identifier.
    /// </summary>
    /// <remarks>The <paramref name="guid"/> must be unique among all existing map providers. If a provider with the
    /// same <paramref name="guid"/> or derived database ID already exists, an exception will be thrown.</remarks>
    /// <param name="guid">A globally unique identifier (GUID) that represents the provider's unique identity.</param>
    /// <exception cref="Exception">Thrown if a provider with the same <paramref name="guid"/> or database ID already
    /// exists.</exception>
    protected GMapProvider(Guid guid)
    {
        Id = guid;
        DatabaseId = Math.Abs(BitConverter.ToInt32(SHA1.HashData(Id.ToByteArray()), 0));

        if (m_MapProviders.Exists(p => p.Id == Id || p.DatabaseId == DatabaseId))
        {
            throw new Exception("such provider id already exists, try regenerate your provider guid...");
        }

        m_MapProviders.Add(this);
    }

    static GMapProvider()
    {
        //WebProxy = EmptyWebProxy.Instance;
    }

    /// <summary>
    ///     was provider initialized
    /// </summary>
    public bool IsInitialized
    {
        get;
        internal set;
    }

    /// <summary>
    ///     called before first use
    /// </summary>
    public virtual void OnInitialized()
    {
        // nice place to detect current provider version
    }

    /// <summary>
    /// The Id used for database access. It is a hash of the provider's GUID (<see cref="Id"/>)."/>
    /// </summary>
    public readonly int DatabaseId;

    /// <summary>
    ///     area of map
    /// </summary>
    public RectLatLng? Area;

    /// <summary>
    ///     minimum level of zoom
    /// </summary>
    public virtual int MinZoom { get; protected set; }

    /// <summary>
    ///     maximum level of zoom
    /// </summary>
    public virtual int? MaxZoom { get; protected set; } = 17;

    /// <summary>
    ///     Connect trough a SOCKS 4/5 proxy server
    /// </summary>
    public static bool IsSocksProxy { get; set; }

    /// <summary>
    ///     The web request factory
    /// </summary>
    public static Func<GMapProvider, string, WebRequest> WebRequestFactory { get; internal set; } = null;

    /// <summary>
    ///     Gets or sets the value of the User-agent HTTP header.
    ///     It's pseudo-randomized to avoid blockages...
    /// </summary>
    public static string UserAgent => string.Format(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:{0}.0) Gecko/20100101 Firefox/{0}.0",
        Stuff.Random.Next((DateTime.Today.Year - 2012) * 10 - 10, (DateTime.Today.Year - 2012) * 10));

    /// <summary>
    ///     Time to live of cache, in hours. Default: 240 (10 days).
    /// </summary>
    public static int TTLCache { get; } = 240;

    /// <summary>
    ///     Gets or sets the value of the Referrer HTTP header.
    /// </summary>
    public string ReferrerUrl = string.Empty;

    public string Copyright = string.Empty;

    /// <summary>
    ///     true if tile origin at BottomLeft, WMS-C
    /// </summary>
    public bool InvertedAxisY = false;

    public static string LanguageStr { get; private set; } = "en";

    static LanguageType m_Language = LanguageType.English;

    /// <summary>
    ///     map language
    /// </summary>
    public static LanguageType Language
    {
        get => m_Language;
        set
        {
            m_Language = value;
            LanguageStr = Stuff.EnumToString(Language);
        }
    }

    /// <summary>
    /// To bypass the cache, set to true.
    /// </summary>
    public bool BypassCache = false;

    /// <summary>
    /// Internal proxy for image management.
    /// </summary>
    internal static PureImageProxy m_TileImageProxy = DefaultImageProxy.Instance;

    static readonly string m_RequestAccept = "*/*";
    static readonly string m_ResponseContentType = "image";

    protected virtual bool CheckTileImageHttpResponse(HttpContentHeaders headers)
    {
        if (headers.ContentType != null)
        {
            string contentType = headers.ContentType.MediaType;
            return contentType.Contains(m_ResponseContentType, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    string m_Authorization = string.Empty;
    string m_AuthorizationType = string.Empty;

    /// <summary>
    ///     http://blog.kowalczyk.info/article/at3/Forcing-basic-http-authentication-for-HttpWebReq.html
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="userPassword"></param>
    public void ForceBasicHttpAuthentication(string userName, string userPassword)
    {
        m_Authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(userName + ":" + userPassword));
        m_AuthorizationType = "Basic";
    }

    protected virtual void InitializeWebRequest(HttpRequestMessage request) { }

    protected PureImage GetTileImageUsingHttp(string url)
    {
        PureImage ret = null;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        #region Populate request headers
        if (!string.IsNullOrEmpty(m_Authorization))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(m_AuthorizationType, m_Authorization);
        }

        if (!string.IsNullOrEmpty(UserAgent))
        {
            request.Headers.Add("User-Agent", UserAgent);
        }

        if (!string.IsNullOrEmpty(m_RequestAccept))
        {
            request.Headers.Add("Accept", m_RequestAccept);
        }

        if (!string.IsNullOrEmpty(ReferrerUrl))
        {
            request.Headers.Add("Referer", ReferrerUrl);
        }

        InitializeWebRequest(request);
        #endregion

        if (!IsSocksProxy)
        {
            // Use HttpClient.
            var httpClient = HttpClientFactory.CreateClient();
            using var response = httpClient.Send(request);

            if (CheckTileImageHttpResponse(response.Content.Headers))
            {
                // Get stream.
                using var responseStream = response.Content.ReadAsStream();

                // Get image.
                ret = ExtractPureImageFromStream(url, responseStream);
            }
        }
        else
        {
            // Use Socks proxy.
            var socksRequest = new SocksHttpWebRequest(request);
            var socksResponse = socksRequest.GetResponse();

            if (CheckTileImageHttpResponse(socksResponse.Headers))
            {
                // Get stream.
                using var responseStream = socksResponse.GetResponseStream();

                // Get image.
                ret = ExtractPureImageFromStream(url, responseStream);
            }
        }

        if (ret is null)
        {
            // Could not retrieve image.
            Debug.WriteLine("CheckTileImageHttpResponse[false]: " + url);
        }

        return ret;
    }

    private static PureImage ExtractPureImageFromStream(string url, Stream responseStream)
    {
        PureImage ret = null;
        var data = Stuff.CopyStream(responseStream, false);

        Debug.WriteLine("Response[" + data.Length + " bytes]: " + url);

        if (data.Length > 0)
        {
            ret = m_TileImageProxy.FromStream(data);

            if (ret != null)
            {
                ret.Data = data;
                ret.Data.Position = 0;
            }
            else
            {
                data.Dispose();
            }
        }

        return ret;
    }

    protected string GetContentUsingHttp(string url)
    {
        string ret = string.Empty;

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        #region Populate request headers
        if (!string.IsNullOrEmpty(m_Authorization))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(m_AuthorizationType, m_Authorization);
        }

        if (!string.IsNullOrEmpty(UserAgent))
        {
            request.Headers.Add("User-Agent", UserAgent);
        }

        if (!string.IsNullOrEmpty(m_RequestAccept))
        {
            request.Headers.Add("Accept", m_RequestAccept);
        }

        if (!string.IsNullOrEmpty(ReferrerUrl))
        {
            request.Headers.Add("Referer", ReferrerUrl);
        }

        InitializeWebRequest(request);
        #endregion

        try
        {
            if (!IsSocksProxy)
            {
                // Use HttpClient.
                var httpClient = HttpClientFactory.CreateClient();
                var response = httpClient.Send(request);
                response.EnsureSuccessStatusCode();

                using var responseStream = response.Content.ReadAsStream();
                using var read = new StreamReader(responseStream, Encoding.UTF8);
                ret = read.ReadToEnd();
            }
            else
            {
                // Use Socks proxy.
                var socksRequest = new SocksHttpWebRequest(request);
                var socksResponse = socksRequest.GetResponse();

                using var responseStream = socksResponse.GetResponseStream();
                using var read = new StreamReader(responseStream, Encoding.UTF8);
                ret = read.ReadToEnd();
            }
        }
        catch (WebException ex)
        {
            // response = (HttpWebResponse)ex.Response;
            return ex.Response.ToString();
        }
        catch (Exception)
        {
            // response = null;
        }

        return ret;
    }

    /// <summary>
    ///     use at your own risk, storing tiles in files is slow and hard on the file system
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    protected virtual PureImage GetTileImageFromFile(string fileName)
    {
        return GetTileImageFromArray(File.ReadAllBytes(fileName));
    }

    protected virtual PureImage GetTileImageFromArray(byte[] data)
    {
        return m_TileImageProxy.FromArray(data);
    }

    protected static int GetServerNum(GPoint pos, int max)
    {
        return (int)(pos.X + 2 * pos.Y) % max;
    }

    public override int GetHashCode()
    {
        return DatabaseId;
    }

    public override bool Equals(object obj)
    {
        if (obj is GMapProvider)
        {
            return Id.Equals((obj as GMapProvider).Id);
        }

        return false;
    }

    public override string ToString()
    {
        return Name;
    }
}

/// <summary>
///     represents empty provider
/// </summary>
public class EmptyProvider : GMapProvider
{
    public static readonly EmptyProvider Instance;

    EmptyProvider()
    {
        MaxZoom = null;
    }

    static EmptyProvider()
    {
        Instance = new EmptyProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; protected set; } = Guid.Empty;

    public override string Name { get; } = "None";

    readonly MercatorProjection m_Projection = MercatorProjection.Instance;

    public override PureProjection Projection => m_Projection;

    public override GMapProvider[] Overlays => null;

    public override PureImage GetTileImage(GPoint pos, int zoom) => null;
    #endregion
}

public sealed class EmptyWebProxy : IWebProxy
{
    public static readonly EmptyWebProxy Instance = new();

    public ICredentials Credentials { get; set; }

    public Uri GetProxy(Uri uri)
    {
        return uri;
    }

    public bool IsBypassed(Uri uri)
    {
        return true;
    }
}
