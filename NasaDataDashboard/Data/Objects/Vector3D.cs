namespace NasaDataDashboard.Data.Objects
{
    public class Vector3D
    {
        public struct Vector3d
        {
            public double X;
            public double Y;
            public double Z;

            public Vector3d(double x, double y, double z) { X = x; Y = y; Z = z; }

            public override string ToString() => $"({X}, {Y}, {Z})";
        }

    }
}
