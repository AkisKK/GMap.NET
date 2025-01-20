using System.Collections.Generic;

namespace GMap.NET;

/// <summary>
///     roads interface
/// </summary>
public interface IRoadsProvider
{
    MapRoute GetRoadsRoute(List<PointLatLng> points, bool interpolate);

    MapRoute GetRoadsRoute(string points, bool interpolate);
}
