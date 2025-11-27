namespace NasaDataDashboard.Data.Objects
{
    public class Vector3D
    {
        public struct Vector3d
        {
            public decimal X;
            public decimal Y;
            public decimal Z;

            public Vector3d(decimal x, decimal y, decimal z) { X = x; Y = y; Z = z; }

            public override string ToString() => $"({X}, {Y}, {Z})";
        }

    }
}
