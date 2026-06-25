using GMap.NET;
using GMap.NET.MapProviders;

namespace UnitTest.GMap.NET.Core;

[TestClass]
public class UnitTestOpenStreetMapProvider
{
    [TestMethod]
    public void TestGetPoint()
    {
        var mapProvider = GMapProviders.OpenStreetMap;

        var point = mapProvider.GetPoint("Barranquilla", out var status);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(point);
    }

    [TestMethod]
    public void TestGetPoints()
    {
        var mapProvider = GMapProviders.OpenStreetMap;

        GeoCoderStatusCode status;

        status = mapProvider.GetPoints("Barranquilla", out var pointList);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(pointList);
    }

    [TestMethod]
    public void TestGetPoints2()
    {
        var mapProvider = GMapProviders.OpenStreetMap;

        var location = new PointLatLng { Lat = 10.98335, Lng = -74.802319 };
        var point = mapProvider.GetPlacemark(location, out var status);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(point);

        status = mapProvider.GetPoints(point!.Value, out var pointList);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(pointList);
    }

    [TestMethod]
    public void TestGetPlacemark()
    {
        var mapProvider = GMapProviders.OpenStreetMap;

        var location = new PointLatLng { Lat = 10.98335, Lng = -74.802319 };

        var point = mapProvider.GetPlacemark(location, out var status);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(point);
    }

    [TestMethod]
    public void TestGetPlacemarks()
    {
        var mapProvider = GMapProviders.OpenStreetMap;

        var location = new PointLatLng { Lat = 10.98335, Lng = -74.802319 };

        var status = mapProvider.GetPlacemarks(location, out var placemarkList);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(placemarkList);
    }

    [TestMethod]
    public void TestGetRoute()
    {
        var mapProvider = GMapProviders.OpenStreetMap;

        var point1 = new PointLatLng(8.681495, 49.41461);
        var point2 = new PointLatLng(8.687872, 49.420318);

        var mapRoute = mapProvider.GetRoute(point1, point2, false, false, 15);

        Assert.AreEqual(RouteStatusCode.OK, mapRoute?.Status);
        Assert.IsNotNull(mapRoute);
    }
}
