using GMap.NET;
using GMap.NET.MapProviders;

namespace UnitTest.GMap.NET.Core;

[TestClass]
public class UnitTestGoogleMapProvider
{
    readonly string ApiKey = "AIzaSyDn8qjiDcnGHOriIrmCnbHs8RK4h_WoGpg";

    [TestMethod]
    [TestCategory("Integration")]
    public void TestGetPoint()
    {
        var mapProvider = GMapProviders.GoogleMap;
        mapProvider.ApiKey = ApiKey;

        var Point = mapProvider.GetPoint("Barranquilla", out var status);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(Point);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void TestGetPoints()
    {
        var mapProvider = GMapProviders.GoogleMap;
        mapProvider.ApiKey = ApiKey;

        var status = mapProvider.GetPoints("Barranquilla", out var pointList);

        Assert.AreEqual(GeoCoderStatusCode.OK, status);
        Assert.IsNotNull(pointList);
    }

    [TestMethod]
    [TestCategory("Integration")]
    public void TestGetRoute()
    {
        var mapProvider = GMapProviders.GoogleMap;
        mapProvider.ApiKey = ApiKey;

        var point1 = new PointLatLng(10.981233, -74.798384);
        var point2 = new PointLatLng(10.981897, -74.792719);

        var mapRoute = mapProvider.GetRoute(point1, point2, false, false, 15);

        Assert.AreEqual(RouteStatusCode.OK, mapRoute?.Status);
        Assert.IsNotNull(mapRoute);
    }
}
