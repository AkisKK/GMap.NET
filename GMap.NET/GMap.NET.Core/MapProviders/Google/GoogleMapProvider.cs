﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using GMap.NET.Internals;
using GMap.NET.Projections;
using Newtonsoft.Json;

namespace GMap.NET.MapProviders.Google;

public abstract class GoogleMapProviderBase : GMapProvider, IRoutingProvider, IGeocodingProvider, IDirectionsProvider,
    IRoadsProvider
{
    public GoogleMapProviderBase()
    {
        MaxZoom = null;
        ReferrerUrl = string.Format("https://maps.{0}/", Server);
        Copyright = string.Format("©{0} Google - Map data ©{0} Tele Atlas, Imagery ©{0} TerraMetrics",
            DateTime.Today.Year);
    }

    public readonly string ServerAPIs /* ;}~~ */ = Stuff.GString( /*{^_^}*/"9gERyvblybF8iMuCt/LD6w==" /*d{'_'}b*/);
    public readonly string Server /* ;}~~~~ */ = Stuff.GString( /*{^_^}*/"gosr2U13BoS+bXaIxt6XWg==" /*d{'_'}b*/);
    public readonly string ServerChina /* ;}~ */ = Stuff.GString( /*{^_^}*/"gosr2U13BoTEJoJJuO25gQ==" /*d{'_'}b*/);
    public readonly string ServerKorea /* ;}~~ */ = Stuff.GString( /*{^_^}*/"8ZVBOEsBinzi+zmP7y7pPA==" /*d{'_'}b*/);
    public readonly string ServerKoreaKr /* ;}~ */ = Stuff.GString( /*{^_^}*/"gosr2U13BoQyz1gkC4QLfg==" /*d{'_'}b*/);

    public string SecureWord = "Galileo";

    /// <summary>
    ///     Your application's API key, obtained from the Google Developers Console.
    ///     This key identifies your application for purposes of quota management.
    ///     Must provide either API key or Maps for Work credentials.
    /// </summary>
    public string ApiKey = string.Empty;

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => MercatorProjection.Instance;

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
    static bool m_Init;

    public override void OnInitialized()
    {
        if (!m_Init && TryCorrectVersion)
        {
            string url =
                string.Format(
                    "https://maps.{0}/maps/api/js?client=google-maps-lite&amp;libraries=search&amp;language=en&amp;region=",
                    ServerAPIs);
            try
            {
                string html = GMaps.Instance.UseUrlCache
                    ? Cache.Instance.GetContent(url, CacheType.UrlCache, TimeSpan.FromHours(TTLCache))
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

                    var reg = new Regex(string.Format(@"https?://mts?\d.{0}/maps/vt\?lyrs=m@(\d*)", Server),
                        RegexOptions.IgnoreCase);
                    var mat = reg.Match(html);

                    if (mat.Success)
                    {
                        var gc = mat.Groups;
                        int count = gc.Count;

                        if (count > 0)
                        {
                            string ver = string.Format("m@{0}", gc[1].Value);
                            string old = GMapProviders.GoogleMap.Version;

                            GMapProviders.GoogleMap.Version = ver;
                            GMapProviders.GoogleChinaMap.Version = ver;

                            string verh = string.Format("h@{0}", gc[1].Value);
                            string oldh = GMapProviders.GoogleHybridMap.Version;

                            GMapProviders.GoogleHybridMap.Version = verh;
                            GMapProviders.GoogleChinaHybridMap.Version = verh;
#if DEBUG
                            Debug.WriteLine("GMapProviders.GoogleMap.Version: " + ver + ", " +
                                            (ver == old ? "OK" : "old: " + old + ", consider updating source"));
                            Debug.WriteLine("GMapProviders.GoogleHybridMap.Version: " + verh + ", " +
                                            (verh == oldh ? "OK" : "old: " + oldh + ", consider updating source"));

                            if (Debugger.IsAttached && ver != old)
                            {
                                Thread.Sleep(1111);
                            }
#endif
                        }
                    }

                    reg = new Regex(string.Format(@"https?://khms?\d.{0}/kh\?v=(\d*)", Server),
                        RegexOptions.IgnoreCase);
                    mat = reg.Match(html);

                    if (mat.Success)
                    {
                        var gc = mat.Groups;
                        int count = gc.Count;

                        if (count > 0)
                        {
                            string ver = gc[1].Value;
                            string old = GMapProviders.GoogleSatelliteMap.Version;

                            GMapProviders.GoogleSatelliteMap.Version = ver;
                            GMapProviders.GoogleKoreaSatelliteMap.Version = ver;
                            GMapProviders.GoogleChinaSatelliteMap.Version = "s@" + ver;
#if DEBUG
                            Debug.WriteLine("GMapProviders.GoogleSatelliteMap.Version: " + ver + ", " +
                                            (ver == old ? "OK" : "old: " + old + ", consider updating source"));
                            if (Debugger.IsAttached && ver != old)
                            {
                                Thread.Sleep(1111);
                            }
#endif
                        }
                    }

                    reg = new Regex(string.Format(@"https?://mts?\d.{0}/maps/vt\?lyrs=t@(\d*),r@(\d*)", Server),
                        RegexOptions.IgnoreCase);
                    mat = reg.Match(html);

                    if (mat.Success)
                    {
                        var gc = mat.Groups;
                        int count = gc.Count;

                        if (count > 1)
                        {
                            string ver = string.Format("t@{0},r@{1}", gc[1].Value, gc[2].Value);
                            string old = GMapProviders.GoogleTerrainMap.Version;

                            GMapProviders.GoogleTerrainMap.Version = ver;
                            GMapProviders.GoogleChinaTerrainMap.Version = ver;
#if DEBUG
                            Debug.WriteLine("GMapProviders.GoogleTerrainMap.Version: " + ver + ", " +
                                            (ver == old ? "OK" : "old: " + old + ", consider updating source"));

                            if (Debugger.IsAttached && ver != old)
                            {
                                Thread.Sleep(1111);
                            }
#endif
                        }
                    }

                    #endregion
                }

                m_Init = true; // try it only once
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TryCorrectGoogleVersions failed: " + ex.ToString());
            }
        }
    }

    internal void GetSecureWords(GPoint pos, out string sec1, out string sec2)
    {
        sec1 = string.Empty; // after &x=...
        int secureLength = (int)(pos.X * 3 + pos.Y) % 8;
        sec2 = SecureWord[..secureLength];

        if (pos.Y >= 10000 && pos.Y < 100000)
        {
            sec1 = m_Sec1;
        }
    }

    static readonly string m_Sec1 = "&s=";

    #region RoutingProvider Members

    public virtual MapRoute GetRoute(PointLatLng start, PointLatLng end, bool avoidHighways, bool walkingMode,
        int zoom)
    {
        return GetRoute(MakeRouteUrl(start, end, LanguageStr, avoidHighways, walkingMode));
    }

    public virtual MapRoute GetRoute(string start, string end, bool avoidHighways, bool walkingMode, int zoom)
    {
        return GetRoute(MakeRouteUrl(start, end, LanguageStr, avoidHighways, walkingMode));
    }

    #region -- internals --

    string MakeRouteUrl(PointLatLng start, PointLatLng end, string language, bool avoidHighways, bool walkingMode)
    {
        string opt = walkingMode ? m_WalkingStr : avoidHighways ? m_RouteWithoutHighwaysStr : m_RouteStr;
        return string.Format(CultureInfo.InvariantCulture,
            m_RouteUrlFormatPointLatLng,
            language,
            opt,
            start.Lat,
            start.Lng,
            end.Lat,
            end.Lng,
            ServerAPIs);
    }

    string MakeRouteUrl(string start, string end, string language, bool avoidHighways, bool walkingMode)
    {
        string opt = walkingMode ? m_WalkingStr : avoidHighways ? m_RouteWithoutHighwaysStr : m_RouteStr;
        return string.Format(m_RouteUrlFormatStr,
            language,
            opt,
            start.Replace(' ', '+'),
            end.Replace(' ', '+'),
            Server);
    }

    MapRoute GetRoute(string url)
    {
        MapRoute ret = null;
        StrucRute routeResult = null;

        try
        {
            string route = GMaps.Instance.UseRouteCache
                ? Cache.Instance.GetContent(url, CacheType.RouteCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(route))
            {
                route = GetContentUsingHttp(!string.IsNullOrEmpty(ClientId) ? GetSignedUri(url) :
                    !string.IsNullOrEmpty(ApiKey) ? url + "&key=" + ApiKey : url);

                if (!string.IsNullOrEmpty(route))
                {
                    routeResult = JsonConvert.DeserializeObject<StrucRute>(route);

                    if (GMaps.Instance.UseRouteCache && routeResult != null &&
                        routeResult.Status == RouteStatusCode.OK)
                    {
                        Cache.Instance.SaveContent(url, CacheType.RouteCache, route);
                    }
                }
            }
            else
            {
                routeResult = JsonConvert.DeserializeObject<StrucRute>(route);
            }

            if (routeResult != null)
            {
                if (routeResult.Error == null && routeResult.Routes != null && routeResult.Routes.Count > 0)
                {
                    ret = new MapRoute(routeResult.Routes[0].Summary);
                }
                else
                {
                    ret = new MapRoute("Route");
                }

                if (routeResult.Error == null)
                {
                    ret.Status = routeResult.Status;

                    if (routeResult.Routes != null && routeResult.Routes.Count > 0)
                    {
                        if (routeResult.Routes.Count > 0)
                        {
                            if (routeResult.Routes[0].OverviewPolyline != null &&
                                routeResult.Routes[0].OverviewPolyline.Points != null)
                            {
                                var points = new List<PointLatLng>();
                                PureProjection.PolylineDecode(points,
                                    routeResult.Routes[0].OverviewPolyline.Points);

                                ret.Points.Clear();
                                ret.Points.AddRange(points);

                                ret.Duration = routeResult.Routes[0].Legs[0].Duration.Text;
                                //ret.DistanceRoute = Math.Round((RouteResult.routes[0].legs[0].distance.value / 1000.0), 1);
                            }
                        }
                    }
                }
                else
                {
                    ret.ErrorCode = routeResult.Error.Code;
                    ret.ErrorMessage = routeResult.Error.Message;

                    if (Enum.TryParse(routeResult.Error.Status, false, out RouteStatusCode code))
                    {
                        ret.Status = code;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ret = null;
            Debug.WriteLine("GetRoutePoints: " + ex);
        }

        return ret;
    }

    static readonly string m_RouteUrlFormatPointLatLng =
        "https://maps.{6}/maps/api/directions/json?origin={2},{3}&destination={4},{5}&mode=driving";

    static readonly string m_RouteUrlFormatStr =
        "http://maps.{4}/maps?f=q&output=dragdir&doflg=p&hl={0}{1}&q=&saddr=@{2}&daddr=@{3}";

    static readonly string m_WalkingStr = "&mra=ls&dirflg=w";
    static readonly string m_RouteWithoutHighwaysStr = "&mra=ls&dirflg=dh";
    static readonly string m_RouteStr = "&mra=ls&dirflg=d";

    #endregion

    #endregion

    #region GeocodingProvider Members

    public GeoCoderStatusCode GetPoints(string keywords, out List<PointLatLng> pointList)
    {
        return GetLatLngFromGeocoderUrl(MakeGeocoderUrl(keywords, LanguageStr), out pointList);
    }

    public PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status)
    {
        status = GetPoints(keywords, out var pointList);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    /// <summary>
    ///     NotImplemented
    /// </summary>
    /// <param name="placemark"></param>
    /// <param name="pointList"></param>
    /// <returns></returns>
    public GeoCoderStatusCode GetPoints(Placemark placemark, out List<PointLatLng> pointList)
    {
        throw new NotImplementedException("use GetPoints(string keywords...");
    }

    /// <summary>
    ///     NotImplemented
    /// </summary>
    /// <param name="placemark"></param>
    /// <param name="status"></param>
    /// <returns></returns>
    public PointLatLng? GetPoint(Placemark placemark, out GeoCoderStatusCode status)
    {
        throw new NotImplementedException("use GetPoint(string keywords...");
    }

    public GeoCoderStatusCode GetPlacemarks(PointLatLng location, out List<Placemark> placemarkList)
    {
        return GetPlacemarkFromReverseGeocoderUrl(MakeReverseGeocoderUrl(location, LanguageStr), out placemarkList);
    }

    public Placemark? GetPlacemark(PointLatLng location, out GeoCoderStatusCode status)
    {
        status = GetPlacemarks(location, out var pointList);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    #region -- internals --

    // The Google Geocoding API: http://tinyurl.com/cdlj889

    string MakeGeocoderUrl(string keywords, string language)
    {
        return string.Format(CultureInfo.InvariantCulture,
            m_GeocoderUrlFormat,
            ServerAPIs,
            Uri.EscapeDataString(keywords).Replace(' ', '+'),
            language);
    }

    string MakeReverseGeocoderUrl(PointLatLng pt, string language)
    {
        return string.Format(CultureInfo.InvariantCulture,
            m_ReverseGeocoderUrlFormat,
            ServerAPIs,
            pt.Lat,
            pt.Lng,
            language);
    }

    GeoCoderStatusCode GetLatLngFromGeocoderUrl(string url, out List<PointLatLng> pointList)
    {
        var status = GeoCoderStatusCode.UNKNOWN_ERROR;

        pointList = null;

        try
        {
            string geo = GMaps.Instance.UseGeocoderCache
                ? Cache.Instance.GetContent(url, CacheType.GeocoderCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            bool cache = false;

            if (string.IsNullOrEmpty(geo))
            {
                string urls = url;

                // Must provide either API key or Maps for Work credentials.
                if (!string.IsNullOrEmpty(ClientId))
                {
                    urls = GetSignedUri(url);
                }
                else if (!string.IsNullOrEmpty(ApiKey))
                {
                    urls += "&key=" + ApiKey;
                }

                geo = GetContentUsingHttp(urls);

                if (!string.IsNullOrEmpty(geo))
                {
                    cache = true;
                }
            }

            if (!string.IsNullOrEmpty(geo))
            {
                var geoResult = JsonConvert.DeserializeObject<StrucGeocode>(geo);

                if (geoResult != null)
                {
                    status = geoResult.Status;

                    if (geoResult.Status == GeoCoderStatusCode.OK)
                    {
                        if (cache && GMaps.Instance.UseGeocoderCache)
                        {
                            Cache.Instance.SaveContent(url, CacheType.GeocoderCache, geo);
                        }

                        pointList = [];

                        if (geoResult.Results != null && geoResult.Results.Count > 0)
                        {
                            for (int i = 0; i < geoResult.Results.Count; i++)
                            {
                                pointList.Add(new PointLatLng(geoResult.Results[i].Geometry.Location.Latitude,
                                    geoResult.Results[i].Geometry.Location.Longitude));
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("GetLatLngFromGeocoderUrl: " + geoResult.Status);
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

    GeoCoderStatusCode GetPlacemarkFromReverseGeocoderUrl(string url, out List<Placemark> placemarkList)
    {
        var status = GeoCoderStatusCode.UNKNOWN_ERROR;
        placemarkList = null;

        try
        {
            string reverse = GMaps.Instance.UsePlacemarkCache
                ? Cache.Instance.GetContent(url, CacheType.PlacemarkCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            bool cache = false;

            if (string.IsNullOrEmpty(reverse))
            {
                string urls = url;

                // Must provide either API key or Maps for Work credentials.
                if (!string.IsNullOrEmpty(ClientId))
                {
                    urls = GetSignedUri(url);
                }
                else if (!string.IsNullOrEmpty(ApiKey))
                {
                    urls += "&key=" + ApiKey;
                }

                reverse = GetContentUsingHttp(urls);

                if (!string.IsNullOrEmpty(reverse))
                {
                    cache = true;
                }
            }

            if (!string.IsNullOrEmpty(reverse))
            {
                var geoResult = JsonConvert.DeserializeObject<StrucGeocode>(reverse);

                if (geoResult != null)
                {
                    status = geoResult.Status;

                    if (geoResult.Status == GeoCoderStatusCode.OK)
                    {
                        if (cache && GMaps.Instance.UseGeocoderCache)
                        {
                            Cache.Instance.SaveContent(url, CacheType.GeocoderCache, reverse);
                        }

                        placemarkList = [];

                        if (geoResult.Results != null && geoResult.Results.Count > 0)
                        {
                            Debug.WriteLine("---------------------");

                            for (int i = 0; i < geoResult.Results.Count; i++)
                            {
                                var ret = new Placemark(geoResult.Results[i].FormattedAddress);

                                Debug.WriteLine("formatted_address: [" + geoResult.Results[i].FormattedAddress +
                                                "]");

                                if (geoResult.Results[i].Types != null)
                                {
                                    Debug.WriteLine("type: " + geoResult.Results[i].Types);
                                }

                                if (geoResult.Results[i].AddressComponents != null &&
                                    geoResult.Results[i].AddressComponents.Count > 0)
                                {
                                    for (int j = 0; j < geoResult.Results[i].AddressComponents.Count; j++)
                                    {
                                        if (geoResult.Results[i].AddressComponents[j].Types != null &&
                                            geoResult.Results[i].AddressComponents[j].Types.Count > 0)
                                        {
                                            Debug.Write(
                                                "Type: [" + geoResult.Results[i].AddressComponents[j].Types[0] +
                                                "], ");
                                            Debug.WriteLine(
                                                "long_name: [" +
                                                geoResult.Results[i].AddressComponents[j].LongName + "]");

                                            switch (geoResult.Results[i].AddressComponents[j].Types[0])
                                            {
                                                case "street_number":
                                                    {
                                                        ret.StreetNumber = geoResult.Results[i].AddressComponents[j]
                                                            .LongName;
                                                    }
                                                    break;

                                                case "street_address":
                                                    {
                                                        ret.StreetAddress = geoResult.Results[i].AddressComponents[j]
                                                            .LongName;
                                                    }
                                                    break;

                                                case "route":
                                                    {
                                                        ret.ThoroughfareName = geoResult.Results[i]
                                                            .AddressComponents[j].LongName;
                                                    }
                                                    break;

                                                case "postal_code":
                                                    {
                                                        ret.PostalCodeNumber = geoResult.Results[i]
                                                            .AddressComponents[j].LongName;
                                                    }
                                                    break;

                                                case "country":
                                                    {
                                                        ret.CountryName = geoResult.Results[i].AddressComponents[j]
                                                            .LongName;
                                                    }
                                                    break;

                                                case "locality":
                                                    {
                                                        ret.LocalityName = geoResult.Results[i].AddressComponents[j]
                                                            .LongName;
                                                    }
                                                    break;

                                                case "administrative_area_level_2":
                                                    {
                                                        ret.DistrictName = geoResult.Results[i].AddressComponents[j]
                                                            .LongName;
                                                    }
                                                    break;

                                                case "administrative_area_level_1":
                                                    {
                                                        ret.AdministrativeAreaName = geoResult.Results[i]
                                                            .AddressComponents[j].LongName;
                                                    }
                                                    break;

                                                case "administrative_area_level_3":
                                                    {
                                                        ret.SubAdministrativeAreaName = geoResult.Results[i]
                                                            .AddressComponents[j].LongName;
                                                    }
                                                    break;

                                                case "neighborhood":
                                                    {
                                                        ret.Neighborhood = geoResult.Results[i].AddressComponents[j]
                                                            .LongName;
                                                    }
                                                    break;
                                                default:
                                                    break;
                                            }
                                        }
                                    }

                                    placemarkList.Add(ret);
                                }
                            }
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("GetPlacemarkFromReverseGeocoderUrl: " + geoResult.Status);
                }
            }
        }
        catch (Exception ex)
        {
            status = GeoCoderStatusCode.EXCEPTION_IN_CODE;
            placemarkList = null;
            Debug.WriteLine("GetPlacemarkReverseGeocoderUrl: " + ex.ToString());
        }

        return status;
    }

    static readonly string m_ReverseGeocoderUrlFormat =
        "https://maps.{0}/maps/api/geocode/json?latlng={1},{2}&language={3}&sensor=false";

    static readonly string m_GeocoderUrlFormat =
        "https://maps.{0}/maps/api/geocode/json?address={1}&language={2}&sensor=false";

    #endregion

    #endregion

    #region DirectionsProvider Members

    public DirectionsStatusCode GetDirections(out GDirections direction, PointLatLng start, PointLatLng end,
        bool avoidHighways, bool avoidTolls, bool walkingMode, bool sensor, bool metric)
    {
        return GetDirectionsUrl(MakeDirectionsUrl(start,
                end,
                LanguageStr,
                avoidHighways,
                avoidTolls,
                walkingMode,
                sensor,
                metric),
            out direction);
    }

    public DirectionsStatusCode GetDirections(out GDirections direction, string start, string end,
        bool avoidHighways, bool avoidTolls, bool walkingMode, bool sensor, bool metric)
    {
        return GetDirectionsUrl(MakeDirectionsUrl(start,
                end,
                LanguageStr,
                avoidHighways,
                avoidTolls,
                walkingMode,
                sensor,
                metric),
            out direction);
    }

    /// <summary>
    ///     NotImplemented
    /// </summary>
    /// <param name="status"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="avoidHighways"></param>
    /// <param name="avoidTolls"></param>
    /// <param name="walkingMode"></param>
    /// <param name="sensor"></param>
    /// <param name="metric"></param>
    /// <returns></returns>
    public IEnumerable<GDirections> GetDirections(out DirectionsStatusCode status, string start, string end,
        bool avoidHighways, bool avoidTolls, bool walkingMode, bool sensor, bool metric)
    {
        // TODO: add alternative directions

        throw new NotImplementedException();
    }

    /// <summary>
    ///     NotImplemented
    /// </summary>
    /// <param name="status"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="avoidHighways"></param>
    /// <param name="avoidTolls"></param>
    /// <param name="walkingMode"></param>
    /// <param name="sensor"></param>
    /// <param name="metric"></param>
    /// <returns></returns>
    public IEnumerable<GDirections> GetDirections(out DirectionsStatusCode status, PointLatLng start,
        PointLatLng end, bool avoidHighways, bool avoidTolls, bool walkingMode, bool sensor, bool metric)
    {
        // TODO: add alternative directions

        throw new NotImplementedException();
    }

    public DirectionsStatusCode GetDirections(out GDirections direction, PointLatLng start,
        IEnumerable<PointLatLng> wayPoints, PointLatLng end, bool avoidHighways, bool avoidTolls, bool walkingMode,
        bool sensor, bool metric)
    {
        return GetDirectionsUrl(MakeDirectionsUrl(start,
                wayPoints,
                end,
                LanguageStr,
                avoidHighways,
                avoidTolls,
                walkingMode,
                sensor,
                metric),
            out direction);
    }

    public DirectionsStatusCode GetDirections(out GDirections direction, string start,
        IEnumerable<string> wayPoints, string end, bool avoidHighways, bool avoidTolls, bool walkingMode,
        bool sensor, bool metric)
    {
        return GetDirectionsUrl(MakeDirectionsUrl(start,
                wayPoints,
                end,
                LanguageStr,
                avoidHighways,
                avoidTolls,
                walkingMode,
                sensor,
                metric),
            out direction);
    }

    #region -- internals --

    // The Google Directions API: http://tinyurl.com/6vv4cac

    string MakeDirectionsUrl(PointLatLng start, PointLatLng end, string language, bool avoidHighways,
        bool avoidTolls, bool walkingMode, bool sensor, bool metric)
    {
        string av = (avoidHighways ? "&avoid=highways" : string.Empty) +
                    (avoidTolls ? "&avoid=tolls" : string.Empty); // 6
        string mt = "&units=" + (metric ? "metric" : "imperial"); // 7
        string wk = "&mode=" + (walkingMode ? "walking" : "driving"); // 8

        return string.Format(CultureInfo.InvariantCulture,
            m_DirectionUrlFormatPoint,
            start.Lat,
            start.Lng,
            end.Lat,
            end.Lng,
            sensor.ToString().ToLower(),
            language,
            av,
            mt,
            wk,
            ServerAPIs);
    }

    string MakeDirectionsUrl(string start, string end, string language, bool avoidHighways, bool walkingMode,
        bool avoidTolls, bool sensor, bool metric)
    {
        string av = (avoidHighways ? "&avoid=highways" : string.Empty) +
                    (avoidTolls ? "&avoid=tolls" : string.Empty); // 4
        string mt = "&units=" + (metric ? "metric" : "imperial"); // 5
        string wk = "&mode=" + (walkingMode ? "walking" : "driving"); // 6

        return string.Format(m_DirectionUrlFormatStr,
            start.Replace(' ', '+'),
            end.Replace(' ', '+'),
            sensor.ToString().ToLower(),
            language,
            av,
            mt,
            wk,
            ServerAPIs);
    }

    string MakeDirectionsUrl(PointLatLng start, IEnumerable<PointLatLng> wayPoints, PointLatLng end,
        string language, bool avoidHighways, bool avoidTolls, bool walkingMode, bool sensor, bool metric)
    {
        string av = (avoidHighways ? "&avoid=highways" : string.Empty) +
                    (avoidTolls ? "&avoid=tolls" : string.Empty); // 6
        string mt = "&units=" + (metric ? "metric" : "imperial"); // 7
        string wk = "&mode=" + (walkingMode ? "walking" : "driving"); // 8

        string wpLatLng = string.Empty;
        int i = 0;

        foreach (var wp in wayPoints)
        {
            wpLatLng += string.Format(CultureInfo.InvariantCulture,
                i++ == 0 ? "{0},{1}" : "|{0},{1}",
                wp.Lat,
                wp.Lng);
        }

        return string.Format(CultureInfo.InvariantCulture,
            m_DirectionUrlFormatWaypoint,
            start.Lat,
            start.Lng,
            wpLatLng,
            sensor.ToString().ToLower(),
            language,
            av,
            mt,
            wk,
            ServerAPIs,
            end.Lat,
            end.Lng);
    }

    string MakeDirectionsUrl(string start, IEnumerable<string> wayPoints, string end, string language,
        bool avoidHighways, bool avoidTolls, bool walkingMode, bool sensor, bool metric)
    {
        string av = (avoidHighways ? "&avoid=highways" : string.Empty) +
                    (avoidTolls ? "&avoid=tolls" : string.Empty); // 6
        string mt = "&units=" + (metric ? "metric" : "imperial"); // 7
        string wk = "&mode=" + (walkingMode ? "walking" : "driving"); // 8

        string wpLatLng = string.Empty;
        int i = 0;

        foreach (string wp in wayPoints)
        {
            wpLatLng += string.Format(CultureInfo.InvariantCulture,
                i++ == 0 ? "{0}" : "|{0}",
                wp.Replace(' ', '+'));
        }

        return string.Format(CultureInfo.InvariantCulture,
            m_DirectionUrlFormatWaypointStr,
            start.Replace(' ', '+'),
            wpLatLng,
            sensor.ToString().ToLower(),
            language,
            av,
            mt,
            wk,
            ServerAPIs,
            end.Replace(' ', '+'));
    }

    DirectionsStatusCode GetDirectionsUrl(string url, out GDirections direction)
    {
        var ret = DirectionsStatusCode.UNKNOWN_ERROR;
        direction = null;

        try
        {
            string kml = GMaps.Instance.UseDirectionsCache
                ? Cache.Instance.GetContent(url, CacheType.DirectionsCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;
            bool cache = false;

            if (string.IsNullOrEmpty(kml))
            {
                kml = GetContentUsingHttp(!string.IsNullOrEmpty(ClientId) ? GetSignedUri(url) :
                    !string.IsNullOrEmpty(ApiKey) ? url + "&key=" + ApiKey : url);

                if (!string.IsNullOrEmpty(kml))
                {
                    cache = true;
                }
            }

            if (!string.IsNullOrEmpty(kml))
            {
                var directionResult = JsonConvert.DeserializeObject<StrucDirection>(kml);

                if (directionResult != null)
                {
                    if (GMaps.Instance.UseDirectionsCache && cache)
                    {
                        Cache.Instance.SaveContent(url, CacheType.DirectionsCache, kml);
                    }

                    ret = directionResult.Status;

                    if (ret == DirectionsStatusCode.OK)
                    {
                        direction = new GDirections();

                        if (directionResult.Routes != null && directionResult.Routes.Count > 0)
                        {
                            direction.Summary = directionResult.Routes[0].Summary;
                            Debug.WriteLine("summary: " + direction.Summary);

                            if (directionResult.Routes[0].Copyrights != null)
                            {
                                direction.Copyrights = directionResult.Routes[0].Copyrights;
                                Debug.WriteLine("copyrights: " + direction.Copyrights);
                            }

                            if (directionResult.Routes[0].OverviewPolyline != null &&
                                directionResult.Routes[0].OverviewPolyline.Points != null)
                            {
                                direction.Route = [];
                                PureProjection.PolylineDecode(direction.Route,
                                    directionResult.Routes[0].OverviewPolyline.Points);
                            }

                            if (directionResult.Routes[0].Legs != null && directionResult.Routes[0].Legs.Count > 0)
                            {
                                direction.Duration = string.Join(", ", directionResult.Routes[0].Legs.Select(x => x.Duration.Text));
                                Debug.WriteLine("duration: " + direction.Duration);

                                direction.DurationValue = (uint)directionResult.Routes[0].Legs.Sum(x => x.Duration.Value);
                                Debug.WriteLine("value: " + direction.DurationValue);

                                direction.Distance = string.Join(", ", directionResult.Routes[0].Legs.Where(x => x.Distance != null).Select(x => x.Distance.Text));
                                Debug.WriteLine("distance: " + direction.Distance);

                                direction.DistanceValue =
                                    (uint)directionResult.Routes[0].Legs.Sum(x => x.Distance?.Value ?? 0);
                                Debug.WriteLine("value: " + direction.DistanceValue);

                                if (directionResult.Routes[0].Legs[0].StartLocation != null)
                                {
                                    direction.StartLocation.Lat =
                                        directionResult.Routes[0].Legs[0].StartLocation.Latitude;
                                    direction.StartLocation.Lng =
                                        directionResult.Routes[0].Legs[0].StartLocation.Longitude;
                                }

                                if (directionResult.Routes[0].Legs[^1].EndLocation != null)
                                {
                                    direction.EndLocation.Lat = directionResult.Routes[0].Legs[^1].EndLocation.Latitude;
                                    direction.EndLocation.Lng = directionResult.Routes[0].Legs[^1].EndLocation.Longitude;
                                }

                                if (directionResult.Routes[0].Legs[0].StartAddress != null)
                                {
                                    direction.StartAddress = directionResult.Routes[0].Legs[0].StartAddress;
                                    Debug.WriteLine("start_address: " + direction.StartAddress);
                                }

                                if (directionResult.Routes[0].Legs[^1].EndAddress != null)
                                {
                                    direction.EndAddress = directionResult.Routes[0].Legs[^1].EndAddress;
                                    Debug.WriteLine("end_address: " + direction.EndAddress);
                                }

                                direction.Steps = [];

                                foreach (var leg in directionResult.Routes[0].Legs)
                                {
                                    for (int i = 0; i < leg.Steps.Count; i++)
                                    {
                                        var step = new GDirectionStep();
                                        Debug.WriteLine("----------------------");

                                        step.TravelMode = leg.Steps[i].TravelMode;
                                        Debug.WriteLine("travel_mode: " + step.TravelMode);

                                        step.Duration = leg.Steps[i].Duration.Text;
                                        Debug.WriteLine("duration: " + step.Duration);

                                        step.Distance = leg.Steps[i].Distance.Text;
                                        Debug.WriteLine("distance: " + step.Distance);

                                        step.HtmlInstructions = leg.Steps[i].HtmlInstructions;
                                        Debug.WriteLine("html_instructions: " + step.HtmlInstructions);

                                        if (leg.Steps[i].StartLocation != null)
                                        {
                                            step.StartLocation.Lat = leg.Steps[i].StartLocation.Latitude;
                                            step.StartLocation.Lng = leg.Steps[i].StartLocation.Longitude;
                                        }

                                        if (leg.Steps[i].EndLocation != null)
                                        {
                                            step.EndLocation.Lat = leg.Steps[i].EndLocation.Latitude;
                                            step.EndLocation.Lng = leg.Steps[i].EndLocation.Longitude;
                                        }

                                        if (leg.Steps[i].Polyline != null && leg.Steps[i].Polyline.Points != null)
                                        {
                                            step.Points = [];
                                            PureProjection.PolylineDecode(step.Points,
                                                leg.Steps[i].Polyline.Points);
                                        }

                                        direction.Steps.Add(step);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            direction = null;
            ret = DirectionsStatusCode.EXCEPTION_IN_CODE;
            Debug.WriteLine("GetDirectionsUrl: " + ex);
        }
        return ret;
    }

    static readonly string m_DirectionUrlFormatStr =
        "https://maps.{7}/maps/api/directions/json?origin={0}&destination={1}&sensor={2}&language={3}{4}{5}{6}";

    static readonly string m_DirectionUrlFormatPoint =
        "https://maps.{9}/maps/api/directions/json?origin={0},{1}&destination={2},{3}&sensor={4}&language={5}{6}{7}{8}";

    static readonly string m_DirectionUrlFormatWaypoint =
        "https://maps.{8}/maps/api/directions/json?origin={0},{1}&waypoints={2}&destination={9},{10}&sensor={3}&language={4}{5}{6}{7}";

    static readonly string m_DirectionUrlFormatWaypointStr =
        "https://maps.{7}/maps/api/directions/json?origin={0}&waypoints={1}&destination={8}&sensor={2}&language={3}{4}{5}{6}";

    #endregion

    #endregion

    #region RoadsProvider Members

    public virtual MapRoute GetRoadsRoute(List<PointLatLng> points, bool interpolate)
    {
        return GetRoadsRoute(MakeRoadsUrl(points, interpolate.ToString()));
    }

    public virtual MapRoute GetRoadsRoute(string points, bool interpolate)
    {
        return GetRoadsRoute(MakeRoadsUrl(points, interpolate.ToString()));
    }

    #region -- internals --

    string MakeRoadsUrl(List<PointLatLng> points, string interpolate)
    {
        string pointString = "";

        foreach (var item in points)
        {
            pointString += string.Format("{2}{0},{1}", item.Lat, item.Lng, pointString == "" ? "" : "|");
        }

        return string.Format(m_RoadsUrlFormatStr, interpolate, pointString, ServerAPIs);
    }

    string MakeRoadsUrl(string points, string interpolate)
    {
        return string.Format(m_RoadsUrlFormatStr, interpolate, points, Server);
    }

    MapRoute GetRoadsRoute(string url)
    {
        MapRoute ret = null;
        StrucRoads roadsResult = null;

        try
        {
            string route = GMaps.Instance.UseRouteCache
                ? Cache.Instance.GetContent(url, CacheType.RouteCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(route))
            {
                route = GetContentUsingHttp(!string.IsNullOrEmpty(ClientId) ? GetSignedUri(url) :
                    !string.IsNullOrEmpty(ApiKey) ? url + "&key=" + ApiKey : url);

                if (!string.IsNullOrEmpty(route))
                {
                    roadsResult = JsonConvert.DeserializeObject<StrucRoads>(route);

                    if (GMaps.Instance.UseRouteCache && roadsResult != null && roadsResult.Error == null &&
                        roadsResult.SnappedPoints != null && roadsResult.SnappedPoints.Count > 0)
                    {
                        Cache.Instance.SaveContent(url, CacheType.RouteCache, route);
                    }
                }
            }
            else
            {
                roadsResult = JsonConvert.DeserializeObject<StrucRoads>(route);
            }

            // parse values
            if (roadsResult != null)
            {
                ret = new MapRoute("Route")
                {
                    WarningMessage = roadsResult.WarningMessage
                };

                if (roadsResult.Error == null)
                {
                    if (roadsResult.SnappedPoints != null && roadsResult.SnappedPoints.Count > 0)
                    {
                        ret.Points.Clear();

                        foreach (var item in roadsResult.SnappedPoints)
                        {
                            ret.Points.Add(new PointLatLng(item.PointLocation.Latitude, item.PointLocation.Longitude));
                        }

                        ret.Status = RouteStatusCode.OK;
                    }
                }
                else
                {
                    ret.ErrorCode = roadsResult.Error.Code;
                    ret.ErrorMessage = roadsResult.Error.Message;

                    if (Enum.TryParse(roadsResult.Error.Status, false, out RouteStatusCode code))
                    {
                        ret.Status = code;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ret = null;
            Debug.WriteLine("GetRoutePoints: " + ex);
        }

        return ret;
    }

    static readonly string m_RoadsUrlFormatStr = "https://roads.{2}/v1/snapToRoads?interpolate={0}&path={1}";

    #endregion

    #endregion

    #region -- Maps API for Work --

    /// <summary>
    ///     https://developers.google.com/maps/documentation/business/webservices/auth#how_do_i_get_my_signing_key
    ///     To access the special features of the Google Maps API for Work you must provide a client ID
    ///     when accessing any of the API libraries or services.
    ///     When registering for Google Google Maps API for Work you will receive this client ID from Enterprise Support.
    ///     All client IDs begin with a gme- prefix. Your client ID is passed as the value of the client parameter.
    ///     Generally, you should store your private key someplace safe and read them into your code
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="privateKey"></param>
    public void SetEnterpriseCredentials(string clientId, string privateKey)
    {
        privateKey = privateKey.Replace("-", "+").Replace("_", "/");
        m_PrivateKeyBytes = Convert.FromBase64String(privateKey);
        ClientId = clientId;
    }

    private byte[] m_PrivateKeyBytes;

    /// <summary>
    ///     Your client ID. To access the special features of the Google Maps API for Work
    ///     you must provide a client ID when accessing any of the API libraries or services.
    ///     When registering for Google Google Maps API for Work you will receive this client ID
    ///     from Enterprise Support. All client IDs begin with a gme- prefix.
    /// </summary>
    public string ClientId { get; private set; } = string.Empty;

    string GetSignedUri(Uri uri)
    {
        var builder = new UriBuilder(uri);
        builder.Query = string.Concat(builder.Query.AsSpan(1), "&client=", ClientId);
        uri = builder.Uri;
        string signature = GetSignature(uri);

        return uri.Scheme + "://" + uri.Host + uri.LocalPath + uri.Query + "&signature=" + signature;
    }

    string GetSignedUri(string url) => GetSignedUri(new Uri(url));

    string GetSignature(Uri uri)
    {
        byte[] encodedPathQuery = Encoding.ASCII.GetBytes(uri.LocalPath + uri.Query);
        var hashAlgorithm = new HMACSHA1(m_PrivateKeyBytes);
        byte[] hashed = hashAlgorithm.ComputeHash(encodedPathQuery);
        return Convert.ToBase64String(hashed).Replace("+", "-").Replace("/", "_");
    }

    #endregion
}

/// <summary>
///     GoogleMap provider
/// </summary>
public class GoogleMapProvider : GoogleMapProviderBase
{
    public static readonly GoogleMapProvider Instance;

    GoogleMapProvider()
    {
    }

    static GoogleMapProvider()
    {
        Instance = new GoogleMapProvider();
    }

    public string Version = "m@333000000";

    #region GMapProvider Members

    public override Guid Id
    {
        get; protected set;
    } = new Guid("D7287DA0-A7FF-405F-8166-B6BAF26D066C");

    public override string Name
    {
        get;
    } = "GoogleMap";

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = MakeTileImageUrl(pos, zoom, LanguageStr);

        return GetTileImageUsingHttp(url);
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom, string language)
    {
        // sec1: after &x=...
        // sec2: after &zoom=...
        GetSecureWords(pos, out string sec1, out string sec2);

        return string.Format(m_UrlFormat,
            m_UrlFormatServer,
            GetServerNum(pos, 4),
            m_UrlFormatRequest,
            Version,
            language,
            pos.X,
            sec1,
            pos.Y,
            zoom,
            sec2,
            Server);
    }

    static readonly string m_UrlFormatServer = "mt";
    static readonly string m_UrlFormatRequest = "vt";
    static readonly string m_UrlFormat = "https://{0}{1}.{10}/maps/{2}/lyrs={3}&hl={4}&x={5}{6}&y={7}&z={8}&s={9}";
}
