﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using GMap.NET.MapProviders;

namespace GMap.NET;

/// <summary>
///     represents route of map
/// </summary>
[Serializable]
public class MapRoute : ISerializable, IDeserializationCallback
{
    /// <summary>
    ///     points of route
    /// </summary>
    public readonly List<PointLatLng> Points = [];

    /// <summary>
    ///     route info
    /// </summary>
    public string Name;

    /// <summary>
    ///     custom object
    /// </summary>
    public object Tag;

    /// <summary>
    ///     time of route
    /// </summary>
    public string Duration;

    public List<string> Instructions = [];

    /// <summary>
    ///     Status of Route
    /// </summary>
    public RouteStatusCode Status { get; set; }

    public string ErrorMessage { get; set; }

    public int ErrorCode { get; set; }

    public string WarningMessage { get; set; }

    /// <summary>
    ///     route start point
    /// </summary>
    public PointLatLng? From
    {
        get
        {
            if (Points.Count > 0)
            {
                return Points[0];
            }

            return null;
        }
    }

    /// <summary>
    ///     route end point
    /// </summary>
    public PointLatLng? To
    {
        get
        {
            if (Points.Count > 1)
            {
                return Points[^1];
            }

            return null;
        }
    }

    public MapRoute(string name)
    {
        Name = name;
    }

    public MapRoute(IEnumerable<PointLatLng> points, string name)
    {
        Points.AddRange(points);
        Name = name;
    }

    public MapRoute(MapRoute route)
    {
        if (route != null)
        {
            var myObjectFields = route.GetType()
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var fi in myObjectFields)
            {
                fi.SetValue(this, fi.GetValue(route));
            }
        }
    }

    /// <summary>
    ///     route distance (in km)
    /// </summary>
    public double Distance
    {
        get
        {
            double distance = 0.0;

            if (From.HasValue && To.HasValue)
            {
                for (int i = 1; i < Points.Count; i++)
                {
                    distance += GMapProviders.EmptyProvider.Projection.GetDistance(Points[i - 1], Points[i]);
                }
            }

            return Math.Round(distance, 4);
        }
    }

    /// <summary>
    ///     Gets the minimum distance (in mts) from the route to a point. Gets null if total points of route are less than 2.
    /// </summary>
    /// <param name="point">Point to calculate distance.</param>
    /// <returns>Distance in meters.</returns>
    public double? DistanceTo(PointLatLng point)
    {
        // Minimun of two elements required to compare.
        if (Points.Count >= 2)
        {
            // First element as the min.
            double min = DistanceToLinealRoute(Points[0], Points[1], point);

            // From 2.
            for (int i = 2; i < Points.Count; i++)
            {
                double distance = DistanceToLinealRoute(Points[i - 1], Points[i], point);

                if (distance < min)
                {
                    min = distance;
                }
            }

            return min;
        }

        return null;
    }

    /// <summary>
    ///     Gets the distance (in mts) between the nearest point of a lineal route (of two points), and a point.
    /// </summary>
    /// <param name="start">Start point of lineal route.</param>
    /// <param name="to">End point of lineal route.</param>
    /// <param name="point">Point to calculate distance.</param>
    /// <returns>Distance in meters.</returns>
    public static double DistanceToLinealRoute(PointLatLng start, PointLatLng to, PointLatLng point)
    {
        // Lineal function formula => y = mxb (y is lat, x is lng).
        // Member m.
        double m = (start.Lat - to.Lat) / (start.Lng - to.Lng);

        // Obtain of b => b = y-mx
        double b = -(m * start.Lng - start.Lat);

        // Possible points of Lat and Lng based on formula replacement (formulaLat and formulaLng).
        // Lat = m*Lngb
        double formulaLat = m * point.Lng + b;

        // Lat = m*Lngb => (Lat-b)/m=Lng 
        double formulaLng = (point.Lat - b) / m;

        // Possibles distances: One from the given point.Lat, and other from the point.Lng.
        double distance1 =
            GMapProviders.EmptyProvider.Projection.GetDistance(new PointLatLng(point.Lat, formulaLng), point);
        double distance2 =
            GMapProviders.EmptyProvider.Projection.GetDistance(new PointLatLng(formulaLat, point.Lng), point);

        // Min of the distances.
        double distance = distance1 <= distance2 ? distance1 : distance2;

        // To mts.
        return distance * 1000;
    }

    /// <summary>
    ///     clears points and sets tag and name to null
    /// </summary>
    public void Clear()
    {
        Points.Clear();
        Tag = null;
        Name = null;
    }

    #region ISerializable Members

    // Temp store for de-serialization.
    private readonly PointLatLng[] m_DeserializedPoints;

    /// <summary>
    ///     Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with the data needed to serialize the
    ///     target object.
    /// </summary>
    /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> to populate with data.</param>
    /// <param name="context">
    ///     The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext" />) for this
    ///     serialization.
    /// </param>
    /// <exception cref="T:System.Security.SecurityException">
    ///     The caller does not have the required permission.
    /// </exception>
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Name", Name);
        info.AddValue("Tag", Tag);
        info.AddValue("Points", Points.ToArray());
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MapRoute" /> class.
    /// </summary>
    /// <param name="info">The info.</param>
    /// <param name="context">The context.</param>
    protected MapRoute(SerializationInfo info, StreamingContext context)
    {
        Name = info.GetString("Name");
        Tag = Extensions.GetValue<object>(info, "Tag", null);
        m_DeserializedPoints = Extensions.GetValue<PointLatLng[]>(info, "Points");
        Points = [];
    }

    #endregion

    #region IDeserializationCallback Members

    /// <summary>
    ///     Runs when the entire object graph has been de-serialized.
    /// </summary>
    /// <param name="sender">
    ///     The object that initiated the callback. The functionality for this parameter is not currently
    ///     implemented.
    /// </param>
    public virtual void OnDeserialization(object sender)
    {
        // Accounts for the de-serialization being breadth first rather than depth first.
        Points.AddRange(m_DeserializedPoints);
        Points.TrimExcess();
    }

    #endregion
}
