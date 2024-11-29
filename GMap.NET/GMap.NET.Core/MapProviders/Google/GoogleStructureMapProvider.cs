using System.Collections.Generic;
using Newtonsoft.Json;

namespace GMap.NET.MapProviders.Google;

#region Geocode

public class StrucGeocode
{
    [JsonProperty("results")]
    public List<Result> Results { get; set; }
    [JsonProperty("status")]
    public GeoCoderStatusCode Status { get; set; }
}

public class AddressComponent
{
    [JsonProperty("long_name")]
    public string LongName { get; set; }
    [JsonProperty("short_name")]
    public string ShortName { get; set; }
    [JsonProperty("types")]
    public List<string> Types { get; set; }
}

public class Northeast
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Southwest
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Bounds
{
    [JsonProperty("northeast")]
    public Northeast NorthEast { get; set; }
    [JsonProperty("southwest")]
    public Southwest SouthWest { get; set; }
}

public class Location
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Northeast2
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Southwest2
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Viewport
{
    [JsonProperty("northeast")]
    public Northeast2 NorthEast { get; set; }
    [JsonProperty("southwest")]
    public Southwest2 SouthWest { get; set; }
}

public class Geometry
{
    [JsonProperty("bounds")]
    public Bounds Bounds { get; set; }
    [JsonProperty("location")]
    public Location Location { get; set; }
    [JsonProperty("location_type")]
    public string LocationType { get; set; }
    [JsonProperty("viewport")]
    public Viewport Viewport { get; set; }
}

public class Result
{
    [JsonProperty("address_components")]
    public List<AddressComponent> AddressComponents { get; set; }
    [JsonProperty("formatted_address")]
    public string FormattedAddress { get; set; }
    [JsonProperty("geometry")]
    public Geometry Geometry { get; set; }
    [JsonProperty("place_id")]
    public string PlaceId { get; set; }
    [JsonProperty("types")]
    public List<string> Types { get; set; }
}

#endregion

#region Direction

public class StrucDirection
{
    [JsonProperty("geocoded_waypoints")]
    public List<GeocodedWaypoint> GeocodedWaypoints { get; set; }
    [JsonProperty("routes")]
    public List<Route> Routes { get; set; }
    [JsonProperty("status")]
    public DirectionsStatusCode Status { get; set; }
}

public class GeocodedWaypoint
{
    [JsonProperty("geocoder_status")]
    public string GeocoderStatus { get; set; }
    [JsonProperty("place_id")]
    public string PlaceId { get; set; }
    [JsonProperty("types")]
    public List<string> Types { get; set; }
}

public class Distance
{
    [JsonProperty("text")]
    public string Text { get; set; }
    [JsonProperty("value")]
    public int Value { get; set; }
}

public class Duration
{
    [JsonProperty("text")]
    public string Text { get; set; }
    [JsonProperty("value")]
    public int Value { get; set; }
}

public class EndLocation
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class StartLocation
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Distance2
{
    [JsonProperty("text")]
    public string Text { get; set; }
    [JsonProperty("value")]
    public int Value { get; set; }
}

public class Duration2
{
    [JsonProperty("text")]
    public string Text { get; set; }
    [JsonProperty("value")]
    public int Value { get; set; }
}

public class EndLocation2
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Polyline
{
    [JsonProperty("points")]
    public string Points { get; set; }
}

public class StartLocation2
{
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lng")]
    public double Longitude { get; set; }
}

public class Step
{
    [JsonProperty("distance")]
    public Distance2 Distance { get; set; }
    [JsonProperty("duration")]
    public Duration2 Duration { get; set; }
    [JsonProperty("end_location")]
    public EndLocation2 EndLocation { get; set; }
    [JsonProperty("html_instructions")]
    public string HtmlInstructions { get; set; }
    [JsonProperty("polyline")]
    public Polyline Polyline { get; set; }
    [JsonProperty("start_location")]
    public StartLocation2 StartLocation { get; set; }
    [JsonProperty("travel_mode")]
    public string TravelMode { get; set; }
    [JsonProperty("maneuver")]
    public string Maneuver { get; set; }
}

public class Leg
{
    [JsonProperty("distance")]
    public Distance Distance { get; set; }
    [JsonProperty("duration")]
    public Duration Duration { get; set; }
    [JsonProperty("end_address")]
    public string EndAddress { get; set; }
    [JsonProperty("end_location")]
    public EndLocation EndLocation { get; set; }
    [JsonProperty("start_address")]
    public string StartAddress { get; set; }
    [JsonProperty("start_location")]
    public StartLocation StartLocation { get; set; }
    [JsonProperty("steps")]
    public List<Step> Steps { get; set; }
    [JsonProperty("traffic_speed_entry")]
    public List<object> TrafficSpeedEntry { get; set; }
    [JsonProperty("via_waypoint")]
    public List<object> ViaWaypoint { get; set; }
}

public class OverviewPolyline
{
    [JsonProperty("points")]
    public string Points { get; set; }
}

public class Route
{
    [JsonProperty("bounds")]
    public Bounds Bounds { get; set; }
    [JsonProperty("copyrights")]
    public string Copyrights { get; set; }
    [JsonProperty("legs")]
    public List<Leg> Legs { get; set; }
    [JsonProperty("overview_polyline")]
    public OverviewPolyline OverviewPolyline { get; set; }
    [JsonProperty("summary")]
    public string Summary { get; set; }
    [JsonProperty("warnings")]
    public List<object> Warnings { get; set; }
    [JsonProperty("waypoint_order")]
    public List<object> WaypointOrder { get; set; }
}

#endregion

#region Rute

public class StrucRute
{
    [JsonProperty("geocoded_waypoints")]
    public List<GeocodedWaypoint> GeocodedWaypoints { get; set; }
    [JsonProperty("routes")]
    public List<Route> Routes { get; set; }
    [JsonProperty("status")]
    public RouteStatusCode Status { get; set; }
    [JsonProperty("error")]
    public Error Error { get; set; }
}

#endregion

#region Roads

public class StrucRoads
{
    [JsonProperty("error")]
    public Error Error { get; set; }

    [JsonProperty("warningMessage")]
    public string WarningMessage { get; set; }

    [JsonProperty("snappedPoints")]
    public List<SnappedPoint> SnappedPoints { get; set; }

    public class SnappedPoint
    {
        [JsonProperty("location")]
        public Location PointLocation { get; set; }
        [JsonProperty("originalIndex")]
        public int OriginalIndex { get; set; }
        [JsonProperty("placeId")]
        public string PlaceId { get; set; }

        public class Location
        {
            [JsonProperty("latitude")]
            public double Latitude { get; set; }
            [JsonProperty("longitude")]
            public double Longitude { get; set; }
        }
    }
}

#endregion

#region Error

public class Error
{
    [JsonProperty("code")]
    public int Code { get; set; }
    [JsonProperty("message")]
    public string Message { get; set; }
    [JsonProperty("status")]
    public string Status { get; set; }
    [JsonProperty("details")]
    public List<Detail> Details { get; set; }
}

public class Detail
{
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("links")]
    public List<Link> Links { get; set; }
}

public class Link
{
    [JsonProperty("description")]
    public string Description { get; set; }
    [JsonProperty("url")]
    public string Url { get; set; }
}

#endregion
