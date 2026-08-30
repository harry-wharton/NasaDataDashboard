using System.Security.Cryptography.Xml;
using System.Text.Json.Serialization;

namespace NasaDataDashboard.Data.Objects
{
	// Response model for SBDB Query API
	public class SbdbQueryResponse
	{
		[JsonPropertyName("signature")]
		public Signature Signature { get; set; }

		[JsonPropertyName("count")]
		public int Count { get; set; }

		[JsonPropertyName("fields")]
		public List<string> Fields { get; set; }

		[JsonPropertyName("data")]
		public List<List<object>> Data { get; set; }
	}
	public class Signature
	{
		[JsonPropertyName("source")]
		public string Source { get; set; }

		[JsonPropertyName("version")]
		public string Version { get; set; }
	}
}
