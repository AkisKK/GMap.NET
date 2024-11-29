using System.Collections.Generic;
using Newtonsoft.Json;

namespace GMap.NET.Entity;

public class OpenStreetMapGeocodeEntity
{
    [JsonProperty("place_id")]
    public long PlaceId { get; set; }
    [JsonProperty("licence")]
    public string Licence { get; set; }
    [JsonProperty("osm_type")]
    public string OsmType { get; set; }
    [JsonProperty("osm_id")]
    public long OsmId { get; set; }
    [JsonProperty("lat")]
    public double Latitude { get; set; }
    [JsonProperty("lon")]
    public double Longitude { get; set; }
    [JsonProperty("display_name")]
    public string DisplayName { get; set; }
    [JsonProperty("address")]
    public Address EntityAddress { get; set; }
    [JsonProperty("class")]
    public string Class { get; set; }
    [JsonProperty("type")]
    public string Type { get; set; }
    [JsonProperty("importance")]
    public double Importance { get; set; }
    [JsonProperty("boundingbox")]
    public List<string> Boundingbox { get; set; }

    public class Address
    {
        [JsonProperty("road")]
        public string Road { get; set; }
        [JsonProperty("suburb")]
        public string Suburb { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
        [JsonProperty("municipality")]
        public string Municipality { get; set; }
        [JsonProperty("county")]
        public string County { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("ISO3166-2-lvl4")]
        public string ISO31662Lvl4 { get; set; }
        [JsonProperty("postcode")]
        public string Postcode { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }
}

public class OpenStreetMapRouteEntity
{
    [JsonProperty("code")]
    public RouteStatusCode Code { get; set; }
    [JsonProperty("routes")]
    public List<Route> Routes { get; set; }
    [JsonProperty("waypoints")]
    public List<Waypoint> Waypoints { get; set; }


    public class Leg
    {
        [JsonProperty("steps")]
        public List<object> Steps { get; set; }
        [JsonProperty("summary")]
        public string Summary { get; set; }
        [JsonProperty("weight")]
        public double Weight { get; set; }
        [JsonProperty("duration")]
        public double Duration { get; set; }
        [JsonProperty("distance")]
        public double Distance { get; set; }
    }


    public class Route
    {
        [JsonProperty("geometry")]
        public string Geometry { get; set; }
        [JsonProperty("legs")]
        public List<Leg> Legs { get; set; }
        [JsonProperty("weight_name")]
        public string WeightName { get; set; }
        [JsonProperty("weight")]
        public double Weight { get; set; }
        [JsonProperty("duration")]
        public double Duration { get; set; }
        [JsonProperty("distance")]
        public double Distance { get; set; }
    }

    public class Waypoint
    {
        [JsonProperty("hint")]
        public string Hint { get; set; }
        [JsonProperty("distance")]
        public double Distance { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("location")]
        public List<double> Location { get; set; }
    }
}
