using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using GMap.NET.Entity;
using GMap.NET.Internals;
using Newtonsoft.Json;

namespace GMap.NET.MapProviders.OpenStreetMap;

/// <summary>
///     OpenStreetMapGraphHopper provider - http://graphhopper.com
/// </summary>
public class OpenStreetMapGraphHopperProvider : OpenStreetMapProviderBase
{
    public static readonly OpenStreetMapGraphHopperProvider Instance;

    OpenStreetMapGraphHopperProvider()
    {
        ReferrerUrl = "http://openseamap.org/";
    }

    static OpenStreetMapGraphHopperProvider()
    {
        Instance = new OpenStreetMapGraphHopperProvider();
    }

    #region GMapProvider Members
    public override Guid Id { get; protected set; } = new Guid("FAACDE73-4B90-5AE6-BB4A-ADE4F3545559");

    public override string Name { get; } = "OpenStreetMapGraphHopper";

    public string ApiKey = string.Empty;

    GMapProvider[] m_Overlays;

    public override GMapProvider[] Overlays
    {
        get
        {
            m_Overlays ??= [OpenStreetMapProvider.Instance, this];

            return m_Overlays;
        }
    }
    #endregion

    #region GMapRoutingProvider Members
    public override MapRoute GetRoute(PointLatLng start,
                                      PointLatLng end,
                                      bool avoidHighways,
                                      bool walkingMode,
                                      int zoom)
    {
        return GetRoute(MakeRoutingUrl(start, end, walkingMode ? m_TravelTypeFoot : m_TravelTypeMotorCar));
    }

    public override MapRoute GetRoute(string start, string end, bool avoidHighways, bool walkingMode, int zoom)
    {
        throw new NotImplementedException("use GetRoute(PointLatLng start, PointLatLng end...");
    }

    #region -- internals --
    static string MakeRoutingUrl(PointLatLng start, PointLatLng end, string travelType)
    {
        return string.Format(CultureInfo.InvariantCulture,
            m_RoutingUrlFormat,
            start.Lat,
            start.Lng,
            end.Lat,
            end.Lng,
            travelType);
    }

    MapRoute GetRoute(string url)
    {
        MapRoute ret = null;
        OpenStreetMapGraphHopperRouteEntity result = null;

        try
        {
            string route = GMaps.Instance.UseRouteCache
                ? Cache.Instance.GetContent(url, CacheType.RouteCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(route))
            {
                route = GetContentUsingHttp(!string.IsNullOrEmpty(ApiKey) ? url + "&key=" + ApiKey : url);

                if (!string.IsNullOrEmpty(route))
                {
                    result = JsonConvert.DeserializeObject<OpenStreetMapGraphHopperRouteEntity>(route);

                    if (GMaps.Instance.UseRouteCache && result != null &&
                        result.RoutePaths != null && result.RoutePaths.Count > 0)
                    {
                        Cache.Instance.SaveContent(url, CacheType.RouteCache, route);
                    }
                }
            }
            else
            {
                result = JsonConvert.DeserializeObject<OpenStreetMapGraphHopperRouteEntity>(route);
            }

            if (!string.IsNullOrEmpty(route))
            {
                ret = new MapRoute("Route");

                if (result != null)
                {
                    if (result.RoutePaths != null && result.RoutePaths.Count > 0)
                    {
                        ret.Status = RouteStatusCode.OK;

                        ret.Duration = result.RoutePaths[0].Time.ToString();

                        var points = new List<PointLatLng>();
                        PureProjection.PolylineDecode(points, result.RoutePaths[0].Points);
                        ret.Points.AddRange(points);

                        foreach (var item in result.RoutePaths[0].Instructions)
                        {
                            ret.Instructions.Add(item.Text);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ret = null;
            Debug.WriteLine("GetRoute: " + ex);
        }

        return ret;
    }

    static readonly string m_TravelTypeFoot = "foot";
    static readonly string m_TravelTypeMotorCar = "car";
    static readonly string m_RoutingUrlFormat = "https://graphhopper.com/api/1/route?point={0},{1}&point={2},{3}&vehicle={4}&type=json";
    #endregion
    #endregion

    #region GeocodingProvider Members
    public new GeoCoderStatusCode GetPoints(string keywords, out List<PointLatLng> pointList)
    {
        return GetLatLngFromGeocoderUrl(MakeGeocoderUrl(keywords), out pointList);
    }

    public new PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status)
    {
        status = GetPoints(keywords, out var pointList);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    public new GeoCoderStatusCode GetPoints(Placemark placemark, out List<PointLatLng> pointList)
    {
        throw new NotImplementedException("use GetPoint");
        //return GetLatLngFromGeocoderUrl(MakeDetailedGeocoderUrl(placemark), out pointList);
    }

    public new PointLatLng? GetPoint(Placemark placemark, out GeoCoderStatusCode status)
    {
        throw new NotImplementedException("use GetPoint");
        //List<PointLatLng> pointList;
        //status = GetPoints(placemark, out pointList);
        //return pointList != null && pointList.Count > 0 ? pointList[0] : (PointLatLng?)null;
    }

    public new GeoCoderStatusCode GetPlacemarks(PointLatLng location, out List<Placemark> placemarkList)
    {
        placemarkList = GetPlacemarkFromReverseGeocoderUrl(MakeReverseGeocoderUrl(location), out var status);
        return status;
    }

    public new Placemark? GetPlacemark(PointLatLng location, out GeoCoderStatusCode status)
    {
        status = GetPlacemarks(location, out var placemarkList);
        return placemarkList != null && placemarkList.Count > 0 ? placemarkList[0] : null;
    }

    #region -- internals --
    static string MakeGeocoderUrl(string keywords)
    {
        return string.Format(m_GeocoderUrlFormat, keywords.Replace(' ', '+'));
    }

    static string MakeReverseGeocoderUrl(PointLatLng pt)
    {
        return string.Format(CultureInfo.InvariantCulture, m_ReverseGeocoderUrlFormat, pt.Lat, pt.Lng);
    }

    GeoCoderStatusCode GetLatLngFromGeocoderUrl(string url, out List<PointLatLng> pointList)
    {
        var status = GeoCoderStatusCode.UNKNOWN_ERROR;
        pointList = null;
        OpenStreetMapGraphHopperGeocodeEntity result = null;

        try
        {
            string geo = GMaps.Instance.UseGeocoderCache
                ? Cache.Instance.GetContent(url, CacheType.GeocoderCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(geo))
            {
                geo = GetContentUsingHttp(!string.IsNullOrEmpty(ApiKey) ? url + "&key=" + ApiKey : url);

                if (!string.IsNullOrEmpty(geo))
                {
                    result = JsonConvert.DeserializeObject<OpenStreetMapGraphHopperGeocodeEntity>(geo);

                    if (GMaps.Instance.UseRouteCache && result != null &&
                        result.Hits != null && result.Hits.Count > 0)
                    {
                        Cache.Instance.SaveContent(url, CacheType.GeocoderCache, geo);
                    }
                }
            }
            else
            {
                result = JsonConvert.DeserializeObject<OpenStreetMapGraphHopperGeocodeEntity>(geo);
            }

            if (!string.IsNullOrEmpty(geo))
            {
                pointList = [];

                foreach (var item in result.Hits)
                {
                    pointList.Add(new PointLatLng(item.Point.Latitude, item.Point.Longitude));
                }

                status = GeoCoderStatusCode.OK;
            }
        }
        catch (Exception ex)
        {
            status = GeoCoderStatusCode.EXCEPTION_IN_CODE;
            Debug.WriteLine("GetLatLngFromGeocoderUrl: " + ex);
        }

        return status;
    }

    List<Placemark> GetPlacemarkFromReverseGeocoderUrl(string url, out GeoCoderStatusCode status)
    {
        status = GeoCoderStatusCode.UNKNOWN_ERROR;
        List<Placemark> ret = null;
        OpenStreetMapGraphHopperGeocodeEntity routeResult = null;

        try
        {
            string geo = GMaps.Instance.UsePlacemarkCache
                ? Cache.Instance.GetContent(url, CacheType.PlacemarkCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(geo))
            {
                geo = GetContentUsingHttp(!string.IsNullOrEmpty(ApiKey) ? url + "&key=" + ApiKey : url);

                if (!string.IsNullOrEmpty(geo))
                {
                    routeResult = JsonConvert.DeserializeObject<OpenStreetMapGraphHopperGeocodeEntity>(geo);

                    if (GMaps.Instance.UsePlacemarkCache && routeResult != null &&
                        routeResult.Hits != null && routeResult.Hits.Count > 0)
                    {
                        Cache.Instance.SaveContent(url, CacheType.PlacemarkCache, geo);
                    }
                }
            }
            else
            {
                routeResult = JsonConvert.DeserializeObject<OpenStreetMapGraphHopperGeocodeEntity>(geo);
            }

            if (!string.IsNullOrEmpty(geo))
            {
                ret = [];

                foreach (var item in routeResult.Hits)
                {
                    var p = new Placemark(item.Name);

                    p = new Placemark
                    {
                        PlacemarkId = item.OsmId,
                        Address = item.Name,
                        CountryName = item.Country,
                        CountryNameCode = item.CountryCode,
                        PostalCodeNumber = item.Postcode,
                        AdministrativeAreaName = item.State,
                        SubAdministrativeAreaName = item.City,
                        LocalityName = null,
                        ThoroughfareName = null
                    };

                    ret.Add(p);
                }

                status = GeoCoderStatusCode.OK;
            }
        }
        catch (Exception ex)
        {
            ret = null;
            status = GeoCoderStatusCode.EXCEPTION_IN_CODE;
            Debug.WriteLine("GetPlacemarkFromReverseGeocoderUrl: " + ex);
        }

        return ret;
    }

    static readonly string m_ReverseGeocoderUrlFormat = "https://graphhopper.com/api/1/geocode?point={0},{1}&locale=en&reverse=true";

    static readonly string m_GeocoderUrlFormat = "https://graphhopper.com/api/1/geocode?q={0}&locale=en";
    #endregion
    #endregion
}
