using System.Collections.Generic;
using Newtonsoft.Json;

namespace GMap.NET.Entity;

public class OpenStreetMapGraphHopperRouteEntity
{
    [JsonProperty("hints")]
    public Hints RouteHints { get; set; }
    [JsonProperty("info")]
    public Info RouteInfo { get; set; }
    [JsonProperty("paths")]
    public List<Path> RoutePaths { get; set; }

    public class Details
    {
    }

    public class Hints
    {
        [JsonProperty("visited_nodes.sum")]
        public int VisitedNodesSum { get; set; }

        [JsonProperty("visited_nodes.average")]
        public double VisitedNodesAverage { get; set; }
    }

    public class Info
    {
        [JsonProperty("copyrights")]
        public List<string> Copyrights { get; set; }
        [JsonProperty("took")]
        public int Took { get; set; }
    }

    public class Instruction
    {
        [JsonProperty("distance")]
        public double Distance { get; set; }
        [JsonProperty("heading")]
        public double Heading { get; set; }
        [JsonProperty("sign")]
        public int Sign { get; set; }
        [JsonProperty("interval")]
        public List<int> Interval { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("time")]
        public int Time { get; set; }
        [JsonProperty("street_name")]
        public string StreetName { get; set; }
        [JsonProperty("last_heading")]
        public double? LastHeading { get; set; }
    }

    public class Path
    {
        [JsonProperty("distance")]
        public double Distance { get; set; }
        [JsonProperty("weight")]
        public double Weight { get; set; }
        [JsonProperty("time")]
        public int Time { get; set; }
        [JsonProperty("transfers")]
        public int Transfers { get; set; }
        [JsonProperty("points_encoded")]
        public bool PointsEncoded { get; set; }
        [JsonProperty("bbox")]
        public List<double> BoundingBox { get; set; }
        [JsonProperty("points")]
        public string Points { get; set; }
        [JsonProperty("instructions")]
        public List<Instruction> Instructions { get; set; }
        [JsonProperty("legs")]
        public List<object> Legs { get; set; }
        [JsonProperty("details")]
        public Details Details { get; set; }
        [JsonProperty("ascend")]
        public double Ascend { get; set; }
        [JsonProperty("descend")]
        public double Descend { get; set; }
        [JsonProperty("snapped_waypoints")]
        public string SnappedWaypoints { get; set; }
    }
}

public class OpenStreetMapGraphHopperGeocodeEntity
{
    [JsonProperty("hits")]
    public List<Hit> Hits { get; set; }
    [JsonProperty("locale")]
    public string Locale { get; set; }

    public class Hit
    {
        [JsonProperty("point")]
        public Point Point { get; set; }
        [JsonProperty("extent")]
        public List<double> Extent { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("countrycode")]
        public string CountryCode { get; set; }
        [JsonProperty("city")]
        public string City { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
        [JsonProperty("osm_id")]
        public long OsmId { get; set; }
        [JsonProperty("osm_type")]
        public string OsmType { get; set; }
        [JsonProperty("osm_key")]
        public string OsmKey { get; set; }
        [JsonProperty("osm_value")]
        public string OsmValue { get; set; }
        [JsonProperty("postcode")]
        public string Postcode { get; set; }
    }

    public class Point
    {
        [JsonProperty("lat")]
        public double Latitude { get; set; }
        [JsonProperty("lng")]
        public double Longitude { get; set; }
    }
}
