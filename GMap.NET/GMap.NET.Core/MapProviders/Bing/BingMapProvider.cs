﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using GMap.NET.Internals;
using GMap.NET.Projections;

namespace GMap.NET.MapProviders.Bing;

public abstract partial class BingMapProviderBase : GMapProvider, IRoutingProvider, IGeocodingProvider
{
    public BingMapProviderBase()
    {
        MaxZoom = null;
        ReferrerUrl = "http://www.bing.com/maps/";
        Copyright = string.Format("©{0} Microsoft Corporation, ©{0} NAVTEQ, ©{0} Image courtesy of NASA",
            DateTime.Today.Year);
    }

    public string Version = "4810";

    /// <summary>
    ///     Bing Maps Customer Identification.
    ///     |
    ///     FOR LEGAL AND COMMERCIAL USAGE SET YOUR OWN REGISTERED KEY
    ///     |
    ///     http://msdn.microsoft.com/en-us/library/ff428642.aspx
    /// </summary>
    public string ClientKey = string.Empty;

    internal string m_SessionId = string.Empty;

    /// <summary>
    ///     set true to append SessionId on requesting tiles
    /// </summary>
    public bool ForceSessionIdOnTileAccess = false;

    /// <summary>
    ///     set true to avoid using dynamic tile url format
    /// </summary>
    public bool DisableDynamicTileUrlFormat = false;

    /// <summary>
    ///     Converts tile XY coordinates into a QuadKey at a specified level of detail.
    /// </summary>
    /// <param name="tileX">Tile X coordinate.</param>
    /// <param name="tileY">Tile Y coordinate.</param>
    /// <param name="levelOfDetail">
    ///     Level of detail, from 1 (lowest detail)
    ///     to 23 (highest detail).
    /// </param>
    /// <returns>A string containing the QuadKey.</returns>
    internal static string TileXYToQuadKey(long tileX, long tileY, int levelOfDetail)
    {
        var quadKey = new StringBuilder();
        for (int i = levelOfDetail; i > 0; i--)
        {
            char digit = '0';
            int mask = 1 << i - 1;
            if ((tileX & mask) != 0)
            {
                digit++;
            }

            if ((tileY & mask) != 0)
            {
                digit++;
                digit++;
            }

            quadKey.Append(digit);
        }

        return quadKey.ToString();
    }

    /// <summary>
    ///     Converts a QuadKey into tile XY coordinates.
    /// </summary>
    /// <param name="quadKey">QuadKey of the tile.</param>
    /// <param name="tileX">Output parameter receiving the tile X coordinate.</param>
    /// <param name="tileY">Output parameter receiving the tile Y coordinate.</param>
    /// <param name="levelOfDetail">Output parameter receiving the level of detail.</param>
    internal static void QuadKeyToTileXY(string quadKey, out int tileX, out int tileY, out int levelOfDetail)
    {
        tileX = tileY = 0;
        levelOfDetail = quadKey.Length;
        for (int i = levelOfDetail; i > 0; i--)
        {
            int mask = 1 << i - 1;
            switch (quadKey[levelOfDetail - i])
            {
                case '0':
                    break;

                case '1':
                    tileX |= mask;
                    break;

                case '2':
                    tileY |= mask;
                    break;

                case '3':
                    tileX |= mask;
                    tileY |= mask;
                    break;

                default:
                    throw new ArgumentException("Invalid QuadKey digit sequence.");
            }
        }
    }

    #region GMapProvider Members

    public override Guid Id
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override string Name
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public override PureProjection Projection
    {
        get
        {
            return MercatorProjection.Instance;
        }
    }

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [this];

            return m_Overlays;
        }
    }

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        throw new NotImplementedException();
    }

    #endregion

    public bool TryCorrectVersion = true;

    /// <summary>
    ///     set false to use your own key.
    ///     FOR LEGAL AND COMMERCIAL USAGE SET YOUR OWN REGISTERED KEY
    ///     http://msdn.microsoft.com/en-us/library/ff428642.aspx
    /// </summary>
    public bool TryGetDefaultKey = true;

    static bool m_Init;

    public override void OnInitialized()
    {
        if (!m_Init)
        {
            try
            {
                string key = ClientKey;

                // to avoid registration stuff, default key
                if (TryGetDefaultKey && string.IsNullOrEmpty(ClientKey))
                {
                    //old: Vx8dmDflxzT02jJUG8bEjMU07Xr9QWRpPTeRuAZTC1uZFQdDCvK/jUbHKdyHEWj4LvccTPoKofDHtzHsWu/0xuo5u2Y9rj88
                    key = Stuff.GString(
                        "Jq7FrGTyaYqcrvv9ugBKv4OVSKnmzpigqZtdvtcDdgZexmOZ2RugOexFSmVzTAhOWiHrdhFoNCoySnNF3MyyIOo5u2Y9rj88");
                }

                #region -- try get session key --

                if (!string.IsNullOrEmpty(key))
                {
                    string keyResponse = GMaps.Instance.UseUrlCache
                        ? Cache.Instance.GetContent("BingLoggingServiceV1" + key,
                            CacheType.UrlCache,
                            TimeSpan.FromHours(TTLCache))
                        : string.Empty;

                    if (string.IsNullOrEmpty(keyResponse))
                    {
                        // Bing Maps WPF Control
                        // http://dev.virtualearth.net/webservices/v1/LoggingService/LoggingService.svc/Log?entry=0&auth={0}&fmt=1&type=3&group=MapControl&name=WPF&version=1.0.0.0&session=00000000-0000-0000-0000-000000000000&mkt=en-US

                        keyResponse = GetContentUsingHttp(string.Format(
                            "http://dev.virtualearth.net/webservices/v1/LoggingService/LoggingService.svc/Log?entry=0&fmt=1&type=3&group=MapControl&name=AJAX&mkt=en-us&auth={0}&jsonp=microsoftMapsNetworkCallback",
                            key));

                        if (!string.IsNullOrEmpty(keyResponse) && keyResponse.Contains("ValidCredentials"))
                        {
                            if (GMaps.Instance.UseUrlCache)
                            {
                                Cache.Instance.SaveContent("BingLoggingServiceV1" + key,
                                    CacheType.UrlCache,
                                    keyResponse);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(keyResponse) && keyResponse.Contains("sessionId") &&
                        keyResponse.Contains("ValidCredentials"))
                    {
                        // microsoftMapsNetworkCallback({"sessionId" : "xxx", "authenticationResultCode" : "ValidCredentials"})

                        m_SessionId = keyResponse.Split(',')[0].Split(':')[1].Replace("\"", string.Empty)
                            .Replace(" ", string.Empty);
                        Debug.WriteLine("GMapProviders.BingMap.SessionId: " + m_SessionId);
                    }
                    else
                    {
                        Debug.WriteLine("BingLoggingServiceV1: " + keyResponse);
                    }
                }

                #endregion

                // supporting old road

                if (TryCorrectVersion && DisableDynamicTileUrlFormat)
                {
                    #region -- get the version --

                    string url = @"http://www.bing.com/maps";
                    string html = GMaps.Instance.UseUrlCache
                        ? Cache.Instance.GetContent(url, CacheType.UrlCache, TimeSpan.FromDays(TTLCache))
                        : string.Empty;

                    if (string.IsNullOrEmpty(html))
                    {
                        html = GetContentUsingHttp(url);

                        if (!string.IsNullOrEmpty(html))
                        {
                            if (GMaps.Instance.UseUrlCache)
                            {
                                Cache.Instance.SaveContent(url, CacheType.UrlCache, html);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(html))
                    {
                        #region -- match versions --

                        var reg = TileGenerationRegex();
                        var mat = reg.Match(html);
                        if (mat.Success)
                        {
                            var gc = mat.Groups;
                            int count = gc.Count;
                            if (count == 2)
                            {
                                string ver = gc[1].Value;
                                string old = GMapProviders.BingMap.Version;
                                if (ver != old)
                                {
                                    GMapProviders.BingMap.Version = ver;
                                    GMapProviders.BingSatelliteMap.Version = ver;
                                    GMapProviders.BingHybridMap.Version = ver;
                                    GMapProviders.BingOSMap.Version = ver;
#if DEBUG
                                    Debug.WriteLine("GMapProviders.BingMap.Version: " + ver + ", old: " + old +
                                                    ", consider updating source");
                                    if (Debugger.IsAttached)
                                    {
                                        Thread.Sleep(5555);
                                    }
#endif
                                }
                                else
                                {
                                    Debug.WriteLine("GMapProviders.BingMap.Version: " + ver + ", OK");
                                }
                            }
                        }

                        #endregion
                    }

                    #endregion
                }

                m_Init = true; // try it only once
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TryCorrectBingVersions failed: " + ex);
            }
        }
    }

    protected override bool CheckTileImageHttpResponse(HttpContentHeaders headers)
    {
        bool pass = base.CheckTileImageHttpResponse(headers);
        if (pass)
        {
            string tileInfo = headers.TryGetValues("X-VE-Tile-Info", out var values)
                ? values.FirstOrDefault()
                : null;
            if (tileInfo != null)
            {
                return !tileInfo.Equals("no-tile");
            }
        }

        return pass;
    }

    internal string GetTileUrl(string imageryType)
    {
        //Retrieve map tile URL from the Imagery Metadata service: http://msdn.microsoft.com/en-us/library/ff701716.aspx
        //This ensures that the current tile URL is always used. 
        //This will prevent the app from breaking when the map tiles change.

        string ret = string.Empty;
        if (!string.IsNullOrEmpty(m_SessionId))
        {
            try
            {
                string url = "http://dev.virtualearth.net/REST/V1/Imagery/Metadata/" + imageryType +
                             "?output=xml&key=" + m_SessionId;

                string r = GMaps.Instance.UseUrlCache
                    ? Cache.Instance.GetContent("GetTileUrl" + imageryType,
                        CacheType.UrlCache,
                        TimeSpan.FromHours(TTLCache))
                    : string.Empty;
                bool cache = false;

                if (string.IsNullOrEmpty(r))
                {
                    r = GetContentUsingHttp(url);
                    cache = true;
                }

                if (!string.IsNullOrEmpty(r))
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(r);

                    var xn = doc["Response"];
                    string statusCode = xn["StatusCode"].InnerText;

                    if (string.Compare(statusCode, "200", true) == 0)
                    {
                        xn = xn["ResourceSets"]["ResourceSet"]["Resources"];
                        var xnl = xn.ChildNodes;

                        foreach (XmlNode xno in xnl)
                        {
                            var imageUrl = xno["ImageUrl"];

                            if (imageUrl != null && !string.IsNullOrEmpty(imageUrl.InnerText))
                            {
                                if (cache && GMaps.Instance.UseUrlCache)
                                {
                                    Cache.Instance.SaveContent("GetTileUrl" + imageryType, CacheType.UrlCache, r);
                                }

                                string baseTileUrl = imageUrl.InnerText;

                                if (baseTileUrl.Contains("{key}") || baseTileUrl.Contains("{token}"))
                                {
                                    baseTileUrl = baseTileUrl.Replace("{key}", m_SessionId).Replace("{token}", m_SessionId);
                                }
                                else if (ForceSessionIdOnTileAccess)
                                {
                                    // haven't seen anyone doing that, yet? ;/                            
                                    baseTileUrl += "&key=" + m_SessionId;
                                }

                                Debug.WriteLine("GetTileUrl, UrlFormat[" + imageryType + "]: " + baseTileUrl);

                                ret = baseTileUrl;
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GetTileUrl: Error getting Bing Maps tile URL - " + ex);
            }
        }

        return ret;
    }

    #region RoutingProvider

    public MapRoute GetRoute(List<PointLatLng> list, bool avoidHighways, bool walkingMode, int zoom)
    {
        MapRoute ret = null;
        var points = GetRoutePoints(MakeRouteUrl(list, LanguageStr, avoidHighways, walkingMode),
            zoom,
            out string tooltip,
            out _,
            out _);
        if (points != null)
        {
            ret = new MapRoute(points, tooltip);
        }

        return ret;
    }

    public MapRoute GetRoute(PointLatLng start, PointLatLng end, bool avoidHighways, bool walkingMode, int zoom)
    {
        MapRoute ret = null;
        var points = GetRoutePoints(MakeRouteUrl(start, end, LanguageStr, avoidHighways, walkingMode),
            zoom,
            out string tooltip,
            out _,
            out _);
        if (points != null)
        {
            ret = new MapRoute(points, tooltip);
        }

        return ret;
    }

    public MapRoute GetRoute(string start, string end, bool avoidHighways, bool walkingMode, int zoom)
    {
        MapRoute ret = null;
        var points = GetRoutePoints(MakeRouteUrl(start, end, LanguageStr, avoidHighways, walkingMode), zoom, out string tooltip, out _, out _);
        if (points != null)
        {
            ret = new MapRoute(points, tooltip);
        }

        return ret;
    }

    private string MakeRouteUrl(List<PointLatLng> list, string _, bool avoidHighways, bool walkingMode)
    {
        string addition = avoidHighways ? "&avoid=highways" : string.Empty;
        string mode = walkingMode ? "Walking" : "Driving";

        var wayPoints = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            var point = list[i];
            wayPoints.Append($"&wp.{i}={point.Lat},{point.Lng}");

        }
        return string.Format(CultureInfo.InvariantCulture,
            m_RouteUrlFormatListPointLatLng,
            mode,
            wayPoints.ToString(),
            addition,
            ClientKey);
    }
    string MakeRouteUrl(string start, string end, string _, bool avoidHighways, bool walkingMode)
    {
        string addition = avoidHighways ? "&avoid=highways" : string.Empty;
        string mode = walkingMode ? "Walking" : "Driving";

        return string.Format(CultureInfo.InvariantCulture,
            m_RouteUrlFormatPointQueries,
            mode,
            start,
            end,
            addition,
            ClientKey);
    }

    string MakeRouteUrl(PointLatLng start, PointLatLng end, string _, bool avoidHighways, bool walkingMode)
    {
        string addition = avoidHighways ? "&avoid=highways" : string.Empty;
        string mode = walkingMode ? "Walking" : "Driving";

        return string.Format(CultureInfo.InvariantCulture,
            m_RouteUrlFormatPointLatLng,
            mode,
            start.Lat,
            start.Lng,
            end.Lat,
            end.Lng,
            addition,
            ClientKey);
    }

    List<PointLatLng> GetRoutePoints(string url, int _, out string tooltipHtml, out int numLevel,
        out int zoomFactor)
    {
        List<PointLatLng> points = null;
        tooltipHtml = string.Empty;
        numLevel = -1;
        zoomFactor = -1;
        try
        {
            string route = GMaps.Instance.UseRouteCache
                ? Cache.Instance.GetContent(url, CacheType.RouteCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(route))
            {
                route = GetContentUsingHttp(url);

                if (!string.IsNullOrEmpty(route))
                {
                    if (GMaps.Instance.UseRouteCache)
                    {
                        Cache.Instance.SaveContent(url, CacheType.RouteCache, route);
                    }
                }
            }

            // parse values
            if (!string.IsNullOrEmpty(route))
            {
                #region -- title --

                int tooltipEnd;
                {
                    int x = route.IndexOf("<RoutePath><Line>") + 17;
                    if (x >= 17)
                    {
                        tooltipEnd = route.IndexOf("</Line></RoutePath>", x + 1);
                        if (tooltipEnd > 0)
                        {
                            int l = tooltipEnd - x;
                            if (l > 0)
                            {
                                //tooltipHtml = route.Substring(x, l).Replace(@"\x26#160;", " ");
                                tooltipHtml = route.Substring(x, l);
                            }
                        }
                    }
                }

                #endregion

                #region -- points --

                var doc = new XmlDocument();
                doc.LoadXml(route);
                var xn = doc["Response"];
                string statusCode = xn["StatusCode"].InnerText;
                switch (statusCode)
                {
                    case "200":
                        {
                            xn = xn["ResourceSets"]["ResourceSet"]["Resources"]["Route"]["RoutePath"]["Line"];
                            var xnl = xn.ChildNodes;
                            if (xnl.Count > 0)
                            {
                                points = [];
                                foreach (XmlNode xno in xnl)
                                {
                                    var latitude = xno["Latitude"];
                                    var longitude = xno["Longitude"];
                                    points.Add(new PointLatLng(
                                        double.Parse(latitude.InnerText, CultureInfo.InvariantCulture),
                                        double.Parse(longitude.InnerText, CultureInfo.InvariantCulture)));
                                }
                            }

                            break;
                        }
                    // No status implementation on routes yet although when introduced these are the codes.
                    // Exception will be caught.
                    case "400":
                        throw new Exception("Bad Request, The request contained an error.");
                    case "401":
                        throw new Exception(
                            "Unauthorized, Access was denied. You may have entered your credentials incorrectly, or you might not have access to the requested resource or operation.");
                    case "403":
                        throw new Exception(
                            "Forbidden, The request is for something forbidden. Authorization will not help.");
                    case "404":
                        throw new Exception("Not Found, The requested resource was not found.");
                    case "500":
                        throw new Exception(
                            "Internal Server Error, Your request could not be completed because there was a problem with the service.");
                    case "501":
                        throw new Exception(
                            "Service Unavailable, There's a problem with the service right now. Please try again later.");
                    default:
                        break; // unknown, for possible future error codes
                }

                #endregion
            }
        }
        catch (Exception ex)
        {
            points = null;
            Debug.WriteLine("GetRoutePoints: " + ex);
        }

        return points;
    }

    // example : http://dev.virtualearth.net/REST/V1/Routes/Driving?o=xml&wp.0=44.979035,-93.26493&wp.1=44.943828508257866,-93.09332862496376&optmz=distance&rpo=Points&key=[PROVIDEYOUROWNKEY!!]
    static readonly string m_RouteUrlFormatPointLatLng =
        "http://dev.virtualearth.net/REST/V1/Routes/{0}?o=xml&wp.0={1},{2}&wp.1={3},{4}{5}&optmz=distance&rpo=Points&key={6}";

    static readonly string m_RouteUrlFormatPointQueries =
        "http://dev.virtualearth.net/REST/V1/Routes/{0}?o=xml&wp.0={1}&wp.1={2}{3}&optmz=distance&rpo=Points&key={4}";

    static readonly string m_RouteUrlFormatListPointLatLng =
        "http://dev.virtualearth.net/REST/V1/Routes/{0}?o=xml{1}{2}&optmz=distance&rpo=Points&key={3}";

    #endregion RoutingProvider

    #region GeocodingProvider

    public GeoCoderStatusCode GetPoints(string keywords, out List<PointLatLng> pointList)
    {
        //Escape keywords to better handle special characters.
        return GetLatLngFromGeocoderUrl(MakeGeocoderUrl("q=" + Uri.EscapeDataString(keywords)), out pointList);
    }

    public PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status)
    {
        status = GetPoints(keywords, out var pointList);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    public GeoCoderStatusCode GetPoints(Placemark placemark, out List<PointLatLng> pointList)
    {
        return GetLatLngFromGeocoderUrl(MakeGeocoderDetailedUrl(placemark), out pointList);
    }

    public PointLatLng? GetPoint(Placemark placemark, out GeoCoderStatusCode status)
    {
        status = GetLatLngFromGeocoderUrl(MakeGeocoderDetailedUrl(placemark), out var pointList);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    string MakeGeocoderDetailedUrl(Placemark placemark)
    {
        string parameters = string.Empty;

        if (!AddFieldIfNotEmpty(ref parameters, "countryRegion", placemark.CountryNameCode))
        {
            AddFieldIfNotEmpty(ref parameters, "countryRegion", placemark.CountryName);
        }

        AddFieldIfNotEmpty(ref parameters, "adminDistrict", placemark.DistrictName);
        AddFieldIfNotEmpty(ref parameters, "locality", placemark.LocalityName);
        AddFieldIfNotEmpty(ref parameters, "postalCode", placemark.PostalCodeNumber);

        if (!string.IsNullOrEmpty(placemark.HouseNo))
        {
            AddFieldIfNotEmpty(ref parameters, "addressLine", placemark.ThoroughfareName + " " + placemark.HouseNo);
        }
        else
        {
            AddFieldIfNotEmpty(ref parameters, "addressLine", placemark.ThoroughfareName);
        }

        return MakeGeocoderUrl(parameters);
    }

    static bool AddFieldIfNotEmpty(ref string input, string fieldName, string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            if (string.IsNullOrEmpty(input))
            {
                input = string.Empty;
            }
            else
            {
                input += "&";
            }

            input = input + fieldName + "=" + value;

            return true;
        }

        return false;
    }

    public GeoCoderStatusCode GetPlacemarks(PointLatLng location, out List<Placemark> placemarkList)
    {
        // http://msdn.microsoft.com/en-us/library/ff701713.aspx
        throw new NotImplementedException();
    }

    public Placemark? GetPlacemark(PointLatLng location, out GeoCoderStatusCode status)
    {
        // http://msdn.microsoft.com/en-us/library/ff701713.aspx
        throw new NotImplementedException();
    }

    string MakeGeocoderUrl(string keywords)
    {
        return string.Format(CultureInfo.InvariantCulture, m_GeocoderUrlFormat, keywords, ClientKey);
    }

    GeoCoderStatusCode GetLatLngFromGeocoderUrl(string url, out List<PointLatLng> pointList)
    {
        GeoCoderStatusCode status;
        pointList = null;

        try
        {
            string geo = GMaps.Instance.UseGeocoderCache
                ? Cache.Instance.GetContent(url, CacheType.GeocoderCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            bool cache = false;

            if (string.IsNullOrEmpty(geo))
            {
                geo = GetContentUsingHttp(url);

                if (!string.IsNullOrEmpty(geo))
                {
                    cache = true;
                }
            }

            status = GeoCoderStatusCode.UNKNOWN_ERROR;
            if (!string.IsNullOrEmpty(geo))
            {
                if (geo.StartsWith("<?xml") && geo.Contains("<Response"))
                {
                    var doc = new XmlDocument();
                    doc.LoadXml(geo);
                    var xn = doc["Response"];
                    string statusCode = xn["StatusCode"].InnerText;
                    switch (statusCode)
                    {
                        case "200":
                            {
                                pointList = [];
                                xn = xn["ResourceSets"]["ResourceSet"]["Resources"];
                                var xnl = xn.ChildNodes;
                                foreach (XmlNode xno in xnl)
                                {
                                    var latitude = xno["Point"]["Latitude"];
                                    var longitude = xno["Point"]["Longitude"];
                                    pointList.Add(new PointLatLng(
                                        double.Parse(latitude.InnerText, CultureInfo.InvariantCulture),
                                        double.Parse(longitude.InnerText, CultureInfo.InvariantCulture)));
                                }

                                if (pointList.Count > 0)
                                {
                                    status = GeoCoderStatusCode.OK;

                                    if (cache && GMaps.Instance.UseGeocoderCache)
                                    {
                                        Cache.Instance.SaveContent(url, CacheType.GeocoderCache, geo);
                                    }

                                    break;
                                }

                                status = GeoCoderStatusCode.ZERO_RESULTS;
                                break;
                            }

                        case "400":
                            status = GeoCoderStatusCode.INVALID_REQUEST;
                            break; // bad request, The request contained an error.
                        case "401":
                            status = GeoCoderStatusCode.REQUEST_DENIED;
                            break; // Unauthorized, Access was denied. You may have entered your credentials incorrectly, or you might not have access to the requested resource or operation.
                        case "403":
                            status = GeoCoderStatusCode.INVALID_REQUEST;
                            break; // Forbidden, The request is for something forbidden. Authorization will not help.
                        case "404":
                            status = GeoCoderStatusCode.ZERO_RESULTS;
                            break; // Not Found, The requested resource was not found. 
                        case "500":
                            status = GeoCoderStatusCode.ERROR;
                            break; // Internal Server Error, Your request could not be completed because there was a problem with the service.
                        case "501":
                            status = GeoCoderStatusCode.UNKNOWN_ERROR;
                            break; // Service Unavailable, There's a problem with the service right now. Please try again later.
                        default:
                            status = GeoCoderStatusCode.UNKNOWN_ERROR;
                            break; // unknown, for possible future error codes
                    }
                }
            }
        }
        catch (Exception ex)
        {
            status = GeoCoderStatusCode.EXCEPTION_IN_CODE;
            Debug.WriteLine("GetLatLngFromGeocoderUrl: " + ex);
        }

        return status;
    }

    // http://dev.virtualearth.net/REST/v1/Locations/1%20Microsoft%20Way%20Redmond%20WA%2098052?o=xml&key=BingMapsKey
    static readonly string m_GeocoderUrlFormat = "http://dev.virtualearth.net/REST/v1/Locations?{0}&o=xml&key={1}";

    [GeneratedRegex("tilegeneration:(\\d*)", RegexOptions.IgnoreCase)]
    private static partial Regex TileGenerationRegex();

    #endregion GeocodingProvider
}

/// <summary>
///     BingMapProvider provider
/// </summary>
public class BingMapProvider : BingMapProviderBase
{
    public static readonly BingMapProvider Instance;

    BingMapProvider()
    {
    }

    static BingMapProvider()
    {
        Instance = new BingMapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; protected set; } = new Guid("D0CEB371-F10A-4E12-A2C1-DF617D6674A8");

    public override string Name { get; } = "BingMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom, LanguageStr);

        return GetTileImageUsingHttp(url);
    }

    public override void OnInitialized()
    {
        base.OnInitialized();

        if (!DisableDynamicTileUrlFormat)
        {
            //UrlFormat[Road]: http://ecn.{subdomain}.tiles.virtualearth.net/tiles/r{quadkey}.jpeg?g=3179&mkt={culture}&shading=hill

            m_UrlDynamicFormat = GetTileUrl("Road");
            if (!string.IsNullOrEmpty(m_UrlDynamicFormat))
            {
                m_UrlDynamicFormat = m_UrlDynamicFormat.Replace("{subdomain}", "t{0}").Replace("{quadkey}", "{1}")
                    .Replace("{culture}", "{2}");
            }
        }
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom, string language)
    {
        string key = TileXYToQuadKey(pos.X, pos.Y, zoom);

        if (!DisableDynamicTileUrlFormat && !string.IsNullOrEmpty(m_UrlDynamicFormat))
        {
            return string.Format(m_UrlDynamicFormat, GetServerNum(pos, 4), key, language);
        }

        return string.Format(m_UrlFormat,
            GetServerNum(pos, 4),
            key,
            Version,
            language,
            ForceSessionIdOnTileAccess ? "&key=" + m_SessionId : string.Empty);
    }

    string m_UrlDynamicFormat = string.Empty;

    // http://ecn.t0.tiles.virtualearth.net/tiles/r120030?g=875&mkt=en-us&lbl=l1&stl=h&shading=hill&n=z

    static readonly string m_UrlFormat =
        "http://ecn.t{0}.tiles.virtualearth.net/tiles/r{1}?g={2}&mkt={3}&lbl=l1&stl=h&shading=hill&n=z{4}";
}
