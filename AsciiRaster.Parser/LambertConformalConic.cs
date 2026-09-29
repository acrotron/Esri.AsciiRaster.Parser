namespace AsciiRaster.Parser;

/// <summary>
/// Tangential Lambert Conformal Conic projection on the WGS84 ellipsoid, matching the AEDT/Esri
/// "Custom_Lambert_Conformal_Conic" projection: both standard parallels and the latitude of origin are the
/// airport latitude, the central meridian is the airport longitude, no false easting/northing.
/// </summary>
/// <remarks>
/// Copied from Contour.Core's <c>LambertConformalConic</c> (MIT, Acrotron) so this package does not need
/// ProjNET4GeoAPI (LGPL-2.1). Grid positions agree with the former ProjNet implementation to within 1e-6 m.
/// Formulas: Snyder (1987), Map Projections - A Working Manual, chapter 15.
/// </remarks>
internal sealed class LambertConformalConic
{
    private const double A = 6378137.0; // WGS84 semi-major axis (meters)
    private const double F = 1.0 / 298.257223563; // WGS84 flattening
    private static readonly double E = Math.Sqrt(2 * F - F * F); // eccentricity
    private static readonly double E2 = E * E;

    private readonly double _lonOriginRad;
    private readonly double _n;
    private readonly double _bigF;
    private readonly double _rho0;

    /// <summary>
    /// Creates the projection centered on the given origin.
    /// </summary>
    /// <param name="latOriginDeg">Latitude of origin in degrees (also both standard parallels).</param>
    /// <param name="lonOriginDeg">Central meridian in degrees.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="latOriginDeg"/> is 0 (the cone degenerates) or not strictly between -90 and 90.
    /// </exception>
    public LambertConformalConic(double latOriginDeg, double lonOriginDeg)
    {
        if (latOriginDeg == 0.0 || !(Math.Abs(latOriginDeg) < 90.0))
        {
            throw new ArgumentOutOfRangeException(nameof(latOriginDeg), latOriginDeg,
                "The latitude of origin must be non-zero and strictly between -90 and 90 degrees for a tangential Lambert Conformal Conic projection.");
        }

        _lonOriginRad = DegreesToRadians(lonOriginDeg);
        double lat0Rad = DegreesToRadians(latOriginDeg);

        double m0 = ComputeM(lat0Rad);
        double t0 = ComputeT(lat0Rad);

        _n = Math.Sin(lat0Rad);
        _bigF = m0 / (_n * Math.Pow(t0, _n));
        _rho0 = A * _bigF * Math.Pow(t0, _n);
    }

    /// <summary>
    /// Projects a WGS84 (longitude, latitude) coordinate in degrees to (easting, northing) in meters.
    /// </summary>
    public (double Easting, double Northing) Forward(double lonDeg, double latDeg)
    {
        double t = ComputeT(DegreesToRadians(latDeg));
        double rho = A * _bigF * Math.Pow(t, _n);

        // Normalize the longitude difference to [-pi, pi] so points far from the central meridian don't project
        // the long way around.
        double deltaLon = DegreesToRadians(lonDeg) - _lonOriginRad;
        deltaLon = Math.Atan2(Math.Sin(deltaLon), Math.Cos(deltaLon));
        double theta = _n * deltaLon;

        return (rho * Math.Sin(theta), _rho0 - rho * Math.Cos(theta));
    }

    /// <summary>
    /// Inverse projects (easting, northing) in meters to WGS84 (longitude, latitude) in degrees.
    /// </summary>
    public (double Longitude, double Latitude) Inverse(double easting, double northing)
    {
        // Snyder eq. 14-10 and 14-11: for a southern-hemisphere origin (n < 0) the signs of x, y and rho0 are
        // reversed before taking the angle.
        double sign = Math.Sign(_n);
        double rhoPrime = sign * Math.Sqrt(easting * easting + (_rho0 - northing) * (_rho0 - northing));
        double thetaPrime = Math.Atan2(sign * easting, sign * (_rho0 - northing));

        double t = Math.Pow(rhoPrime / (A * _bigF), 1.0 / _n);
        double lonDeg = RadiansToDegrees(thetaPrime / _n + _lonOriginRad);

        // Normalize longitude to [-180, 180]
        lonDeg = ((lonDeg + 180.0) % 360.0) - 180.0;
        if (lonDeg < -180.0) lonDeg += 360.0;

        return (lonDeg, RadiansToDegrees(InverseT(t)));
    }

    // m(phi) = cos(phi) / sqrt(1 - e^2 sin^2(phi))
    private static double ComputeM(double latRad)
    {
        double sinLat = Math.Sin(latRad);
        return Math.Cos(latRad) / Math.Sqrt(1.0 - E2 * sinLat * sinLat);
    }

    // t(phi) = tan(pi/4 - phi/2) / ((1 - e sin(phi)) / (1 + e sin(phi)))^(e/2)
    private static double ComputeT(double latRad)
    {
        double eSinLat = E * Math.Sin(latRad);
        return Math.Tan(Math.PI / 4.0 - latRad / 2.0) / Math.Pow((1.0 - eSinLat) / (1.0 + eSinLat), E / 2.0);
    }

    // Iteratively inverts t to recover the latitude.
    private static double InverseT(double t)
    {
        double lat = Math.PI / 2.0 - 2.0 * Math.Atan(t);

        for (int i = 0; i < 20; i++)
        {
            double eSinLat = E * Math.Sin(lat);
            double latNew = Math.PI / 2.0 - 2.0 * Math.Atan(t * Math.Pow((1.0 - eSinLat) / (1.0 + eSinLat), E / 2.0));

            if (Math.Abs(latNew - lat) < 1e-14)
                return latNew;

            lat = latNew;
        }

        return lat;
    }

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
    private static double RadiansToDegrees(double rad) => rad * 180.0 / Math.PI;
}
