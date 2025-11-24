namespace NasaDataDashboard.Data.Objects
{
	public class AsteroidWithOrbitalData
	{
		public string SpkId { get; set; }
		public string FullName { get; set; }
		public string Designation { get; set; }
		public string Name { get; set; }
		public bool IsNeo { get; set; }
		public bool IsPha { get; set; }
		public string OrbitClass { get; set; }
		public decimal Moid { get; set; }
		public decimal DiameterKm { get; set; }
		public decimal AbsoluteMagnitude { get; set; }

		// Orbital elements
		public decimal Eccentricity { get; set; }
		public decimal SemiMajorAxisAU { get; set; }
		public decimal PerihelionDistanceAU { get; set; }
		public decimal InclinationDeg { get; set; }
		public decimal LongitudeAscNodeDeg { get; set; }
		public decimal ArgumentPerihelionDeg { get; set; }
		public decimal MeanAnomalyDeg { get; set; }
		public decimal EpochJd { get; set; }
		public DateTime OrbitalEpoch { get; set; }
		public decimal OrbitalPeriodDays { get; set; }

		// Calculated values
		public decimal ApproximateDistanceKm { get; set; }
		public decimal ApproximateVelocityKmH { get; set; }
	}
}
