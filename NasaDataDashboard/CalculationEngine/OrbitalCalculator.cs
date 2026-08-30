using NasaDataDashboard.Data.Objects;
using static NasaDataDashboard.Data.Objects.Vector3d;

namespace NasaDataDashboard.CalculationEngine
{
    public class OrbitalCalculator
    {
        // Conversion constants
        public const decimal AU_TO_METERS = 1.495978707e11m;
        private const decimal DAYS_PER_YEAR = 365.25m;
        private const decimal MS_PER_DAY = 86400000m;
        private static readonly decimal TWO_PI = (decimal)(Math.PI * 2.0);
        private static readonly decimal PI = (decimal)Math.PI;

        /*
            HELPERS -----------------------------------------------------------------
        */

        // Degrees/Radians helpers
        public static decimal DegreesToRadians(decimal deg)
        {
            return deg * (decimal)(Math.PI / 180.0);
        }

        public static decimal RadiansToDegrees(decimal rad)
        {
            return rad * (decimal)(180.0 / Math.PI);
        }

        private static decimal NormalizeAngleRad(decimal angle)
        {
            angle = (angle + PI) % TWO_PI;
            if (angle < 0) angle += TWO_PI;
            return angle - PI;
        }

        // Convert Julian Date to Unix milliseconds (ms since 1970-01-01)
        public static decimal JulianDateToUnixMilliseconds(decimal jd)
        {
            const decimal UnixEpochJd = 2440587.5m;
            return (jd - UnixEpochJd) * MS_PER_DAY;
        }

        // Convert Unix milliseconds to Julian Date
        public static decimal UnixMillisecondsToJulianDate(decimal ms)
        {
            const decimal UnixEpochJd = 2440587.5m;
            return UnixEpochJd + ms / MS_PER_DAY;
        }

        // Mean motion (rad per ms) via Kepler's 3rd law
        public static decimal MeanMotionRadPerMs(decimal semiMajorAxisAu)
        {
            double a = (double)semiMajorAxisAu;
            double periodDays = Math.Pow(a, 1.5) * (double)DAYS_PER_YEAR;
            double result = (double)TWO_PI / (periodDays * (double)MS_PER_DAY);
            return (decimal)result;
        }

        /*
            END OF HELPERS -----------------------------------------------------------------
        */

        // Solve Kepler's equation: M = E - e sin E
        public static decimal SolveEccentricAnomaly(decimal meanAnomalyRad, decimal eccentricity)
        {
            decimal Mdec = NormalizeAngleRad(meanAnomalyRad);

            double M = (double)Mdec;
            double e = (double)eccentricity;

            double E = M + e * Math.Sin(M) * 0.85;

            for (int i = 0; i < 50; i++)
            {
                double f = E - e * Math.Sin(E) - M;
                double fp = 1.0 - e * Math.Cos(E);
                double delta = f / fp;
                E -= delta;

                if (Math.Abs(delta) < 1e-12)
                    break;
            }

            return (decimal)E;
        }

        // Convert eccentric anomaly E to true anomaly ν
        private static decimal EccentricToTrueAnomaly(decimal Edec, decimal edec)
        {
            double E = (double)Edec;
            double e = (double)edec;

            double cosE = Math.Cos(E);
            double sinE = Math.Sin(E);
            double denom = 1.0 - e * cosE;

            double cosV = (cosE - e) / denom;
            double sinV = (Math.Sqrt(Math.Max(0.0, 1.0 - e * e)) * sinE) / denom;

            return (decimal)Math.Atan2(sinV, cosV);
        }

        // Convert orbital elements to heliocentric Cartesian coordinates (AU)
        public static (Vector3d position, decimal radiusAu) OrbitalElementsToCartesian(
            decimal aAu, decimal e, decimal inclinationDeg, decimal ascendingNodeDeg,
            decimal argPeriDeg, decimal meanAnomalyRad)
        {
            decimal inc = DegreesToRadians(inclinationDeg);
            decimal ascNode = DegreesToRadians(ascendingNodeDeg);
            decimal argPeri = DegreesToRadians(argPeriDeg);
               
            // Find E from mean anomaly M
            decimal E = SolveEccentricAnomaly(meanAnomalyRad, e);
            double E_d = (double)E;

            // Find distance from the sun: r using eulers   
            decimal r = aAu * (1 - e * (decimal)Math.Cos(E_d));

            // Convert E to true anomaly v
            decimal trueAnomaly = EccentricToTrueAnomaly(E, e);
            double v = (double)trueAnomaly;

            decimal xOrb = r * (decimal)Math.Cos(v);
            decimal yOrb = r * (decimal)Math.Sin(v);

            /*
              From api we have W = argument of perihelion
              I = inclination, O = longitude of ascending node
             */
            double cosW = Math.Cos((double)argPeri);
            double sinW = Math.Sin((double)argPeri);
            double cosI = Math.Cos((double)inc);
            double sinI = Math.Sin((double)inc);
            double cosO = Math.Cos((double)ascNode);
            double sinO = Math.Sin((double)ascNode);

            // We can now use these to find 3D orbital coords
            decimal x =
                (decimal)(cosO * cosW - sinO * sinW * cosI) * xOrb +
                (decimal)(-cosO * sinW - sinO * cosW * cosI) * yOrb;

            decimal y =
                (decimal)(sinO * cosW + cosO * sinW * cosI) * xOrb +
                (decimal)(-sinO * sinW + cosO * cosW * cosI) * yOrb;

            decimal z =
                (decimal)(sinW * sinI) * xOrb +
                (decimal)(cosW * sinI) * yOrb;

            return (new Vector3d { X = x, Y = y, Z = z }, r);
        }   

        // Position at time in Unix ms
        public static Vector3d PositionAtUnixMilliseconds(AsteroidWithOrbitalData elements, decimal unixMs)
        {
            decimal M0 = DegreesToRadians((decimal)elements.MeanAnomalyDeg);
            decimal epochMs = JulianDateToUnixMilliseconds((decimal)elements.EpochJd);
            decimal n = MeanMotionRadPerMs((decimal)elements.SemiMajorAxisAU);

            decimal Mnow = M0 + n * (unixMs - epochMs);

            var (pos, _) = OrbitalElementsToCartesian(
                (decimal)elements.SemiMajorAxisAU,
                (decimal)elements.Eccentricity,
                (decimal)elements.InclinationDeg,
                (decimal)elements.LongitudeAscNodeDeg,
                (decimal)elements.ArgumentPerihelionDeg,
                Mnow);

            return pos;
        }

        // Position at DateTime UTC
        public static Vector3d PositionAtUtc(AsteroidWithOrbitalData elements, DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
                utcTime = utcTime.ToUniversalTime();

            decimal unixMs = (decimal)(utcTime - DateTime.UnixEpoch).TotalMilliseconds;
            return PositionAtUnixMilliseconds(elements, unixMs);
        }
    }       
}
