using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using GMap.NET.Entity;
using GMap.NET.Internals;
using GMap.NET.Projections;
using Newtonsoft.Json;

namespace GMap.NET.MapProviders.OpenStreetMap;

public abstract class OpenStreetMapProviderBase : GMapProvider, RoutingProvider, GeocodingProvider
{
    public OpenStreetMapProviderBase()
    {
        MaxZoom = null;
        //Tile usage policy of openstreetmap (https://operations.osmfoundation.org/policies/tiles/) define as optional and providing referer 
        //only if one valid available. by providing http://www.openstreetmap.org/ a 418 error is given by the server.
        //RefererUrl = "http://www.openstreetmap.org/";
        Copyright = string.Format("© OpenStreetMap - Map data ©{0} OpenStreetMap", DateTime.Today.Year);
    }

    public readonly string ServerLetters = "abc";
    public int MinExpectedRank = 0;

    #region GMapProvider Members

    public override Guid Id => throw new NotImplementedException();

    public override string Name => throw new NotImplementedException();

    public override PureProjection Projection => MercatorProjection.Instance;

    public override GMapProvider[] Overlays => throw new NotImplementedException();

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        throw new NotImplementedException();
    }

    #endregion

    #region GMapRoutingProvider Members

    public virtual MapRoute GetRoute(PointLatLng start, PointLatLng end, bool avoidHighways, bool walkingMode, int zoom)
    {
        return GetRoute(MakeRoutingUrl(start, end, walkingMode ? m_TravelTypeFoot : m_TravelTypeMotorCar));
    }

    /// <summary>
    ///     NotImplemented
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="avoidHighways"></param>
    /// <param name="walkingMode"></param>
    /// <param name="zoom"></param>
    /// <returns></returns>
    public virtual MapRoute GetRoute(string start, string end, bool avoidHighways, bool walkingMode, int zoom)
    {
        return GetRoute(MakeRoutingUrl(start, end, walkingMode ? m_TravelTypeFoot : m_TravelTypeMotorCar));
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

    static string MakeRoutingUrl(string start, string end, string travelType)
    {
        return string.Format(CultureInfo.InvariantCulture,
                             m_RoutingUrlFormat,
                             start,
                             end,
                             travelType);
    }

    MapRoute GetRoute(string url)
    {
        MapRoute ret = null;
        OpenStreetMapRouteEntity result = null;

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
                    result = JsonConvert.DeserializeObject<OpenStreetMapRouteEntity>(route);

                    if (GMaps.Instance.UseRouteCache && result != null &&
                        result.Routes != null && result.Routes.Count > 0)
                    {
                        Cache.Instance.SaveContent(url, CacheType.RouteCache, route);
                    }
                }
            }
            else
            {
                result = JsonConvert.DeserializeObject<OpenStreetMapRouteEntity>(route);
            }

            if (!string.IsNullOrEmpty(route))
            {
                ret = new MapRoute("Route");

                if (result != null)
                {
                    if (result.Routes != null && result.Routes.Count > 0)
                    {
                        ret.Status = RouteStatusCode.OK;

                        ret.Duration = result.Routes[0].Duration.ToString();

                        var points = new List<PointLatLng>();
                        PureProjection.PolylineDecode(points, result.Routes[0].Geometry);
                        ret.Points.AddRange(points);
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

    static readonly string m_RoutingUrlFormat = "http://router.project-osrm.org/route/v1/driving/{1},{0};{3},{2}";
    static readonly string m_TravelTypeFoot = "foot";
    static readonly string m_TravelTypeMotorCar = "motorcar";

    //static readonly string WalkingStr = "Walking";
    //static readonly string DrivingStr = "Driving";
    #endregion
    #endregion

    #region GeocodingProvider Members
    public GeoCoderStatusCode GetPoints(string keywords, out List<PointLatLng> pointList)
    {
        // http://nominatim.openstreetmap.org/search?q=lithuania,vilnius&format=xml
        return GetLatLngFromGeocoderUrl(MakeGeocoderUrl(keywords), out pointList);
    }

    public PointLatLng? GetPoint(string keywords, out GeoCoderStatusCode status)
    {
        status = GetPoints(keywords, out var pointList);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    public GeoCoderStatusCode GetPoints(Placemark placemark, out List<PointLatLng> pointList)
    {
        // http://nominatim.openstreetmap.org/search?street=&city=vilnius&county=&state=&country=lithuania&postalcode=&format=xml
        return GetLatLngFromGeocoderUrl(MakeDetailedGeocoderUrl(placemark), out pointList);
    }

    public PointLatLng? GetPoint(Placemark placemark, out GeoCoderStatusCode status)
    {
        status = GetPoints(placemark, out var pointList);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    public GeoCoderStatusCode GetPlacemarks(PointLatLng location, out List<Placemark> placemarkList)
    {
        placemarkList = GetPlacemarkFromReverseGeocoderUrl(MakeReverseGeocoderUrl(location), out var status);
        return status;
    }

    public Placemark? GetPlacemark(PointLatLng location, out GeoCoderStatusCode status)
    {
        List<Placemark> pointList;
        pointList = GetPlacemarkFromReverseGeocoderUrl(MakeReverseGeocoderUrl(location), out status);
        return pointList != null && pointList.Count > 0 ? pointList[0] : null;
    }

    #region -- internals --
    static string MakeGeocoderUrl(string keywords)
    {
        return string.Format(m_GeocoderUrlFormat, keywords.Replace(' ', '+'));
    }

    static string MakeDetailedGeocoderUrl(Placemark placemark)
    {
        string street = string.Join(" ", new[] { placemark.HouseNo, placemark.ThoroughfareName }).Trim();

        return string.Format(m_GeocoderDetailedUrlFormat,
                             street.Replace(' ', '+'),
                             placemark.LocalityName.Replace(' ', '+'),
                             placemark.SubAdministrativeAreaName.Replace(' ', '+'),
                             placemark.AdministrativeAreaName.Replace(' ', '+'),
                             placemark.CountryName.Replace(' ', '+'),
                             placemark.PostalCodeNumber.Replace(' ', '+'));
    }

    static string MakeReverseGeocoderUrl(PointLatLng pt)
    {
        return string.Format(CultureInfo.InvariantCulture, m_ReverseGeocoderUrlFormat, pt.Lat, pt.Lng);
    }

    GeoCoderStatusCode GetLatLngFromGeocoderUrl(string url, out List<PointLatLng> pointList)
    {
        var status = GeoCoderStatusCode.UNKNOWN_ERROR;
        pointList = null;
        List<OpenStreetMapGeocodeEntity> result = null;

        try
        {
            string geo = GMaps.Instance.UseGeocoderCache
                ? Cache.Instance.GetContent(url, CacheType.GeocoderCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(geo))
            {
                geo = GetContentUsingHttp(url);

                if (!string.IsNullOrEmpty(geo))
                {
                    result = JsonConvert.DeserializeObject<List<OpenStreetMapGeocodeEntity>>(geo);

                    if (GMaps.Instance.UseGeocoderCache && result != null && result.Count > 0)
                    {
                        Cache.Instance.SaveContent(url, CacheType.GeocoderCache, geo);
                    }
                }
            }
            else
            {
                result = JsonConvert.DeserializeObject<List<OpenStreetMapGeocodeEntity>>(geo);
            }

            if (!string.IsNullOrEmpty(geo))
            {
                pointList = [];

                foreach (var item in result)
                {
                    pointList.Add(new PointLatLng { Lat = item.Latitude, Lng = item.Longitude });
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
        OpenStreetMapGeocodeEntity result = null;

        try
        {
            string geo = GMaps.Instance.UsePlacemarkCache
                ? Cache.Instance.GetContent(url, CacheType.PlacemarkCache, TimeSpan.FromHours(TTLCache))
                : string.Empty;

            if (string.IsNullOrEmpty(geo))
            {
                geo = GetContentUsingHttp(url);

                if (!string.IsNullOrEmpty(geo))
                {
                    result = JsonConvert.DeserializeObject<OpenStreetMapGeocodeEntity>(geo);

                    if (GMaps.Instance.UsePlacemarkCache && result != null)
                    {
                        Cache.Instance.SaveContent(url, CacheType.PlacemarkCache, geo);
                    }
                }
            }
            else
            {
                result = JsonConvert.DeserializeObject<OpenStreetMapGeocodeEntity>(geo);
            }

            if (!string.IsNullOrEmpty(geo))
            {
                ret = [];

                var p = new Placemark(result.DisplayName);

                p = new Placemark
                {
                    PlacemarkId = result.PlaceId,
                    Address = result.EntityAddress.ToString(),
                    CountryName = result.EntityAddress.Country,
                    CountryNameCode = result.EntityAddress.CountryCode,
                    PostalCodeNumber = result.EntityAddress.Postcode,
                    AdministrativeAreaName = result.EntityAddress.State,
                    SubAdministrativeAreaName = result.EntityAddress.City,
                    LocalityName = result.EntityAddress.Suburb,
                    ThoroughfareName = result.EntityAddress.Road
                };

                ret.Add(p);

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

    static readonly string m_ReverseGeocoderUrlFormat =
        "https://nominatim.openstreetmap.org/reverse?format=json&lat={0}&lon={1}&zoom=18&addressdetails=1";

    static readonly string m_GeocoderUrlFormat = "https://nominatim.openstreetmap.org/search?q={0}&format=json";

    static readonly string m_GeocoderDetailedUrlFormat =
        "https://nominatim.openstreetmap.org/search?street={0}&city={1}&county={2}&state={3}&country={4}&postalcode={5}&format=json";
    #endregion
    #endregion
}

/// <summary>
///     OpenStreetMap provider - http://www.openstreetmap.org/
/// </summary>
public class OpenStreetMapProvider : OpenStreetMapProviderBase
{
    public static readonly OpenStreetMapProvider Instance;

    OpenStreetMapProvider()
    {
    }

    static OpenStreetMapProvider()
    {
        Instance = new OpenStreetMapProvider();
    }

    #region GMapProvider Members

    public override Guid Id { get; } = new Guid("0521335C-92EC-47A8-98A5-6FD333DDA9C0");

    public override string Name { get; } = "OpenStreetMap";

    public string YoursClientName { get; set; }

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
        string url = MakeTileImageUrl(pos, zoom);

        return GetTileImageUsingHttp(url);
    }

    protected override void InitializeWebRequest(HttpRequestMessage request)
    {
        base.InitializeWebRequest(request);

        if (!string.IsNullOrEmpty(YoursClientName))
        {
            request.Headers.Add("X-Yours-client", YoursClientName);
        }
    }

    #endregion

    string MakeTileImageUrl(GPoint pos, int zoom)
    {
        char letter = ServerLetters[GetServerNum(pos, 3)];
        return string.Format(m_UrlFormat, letter, zoom, pos.X, pos.Y);
    }

    static readonly string m_UrlFormat = "https://{0}.tile.openstreetmap.org/{1}/{2}/{3}.png";
}
