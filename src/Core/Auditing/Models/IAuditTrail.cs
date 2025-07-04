
// <auto-generated>
//     This code was generated by d3i.interpreter
//
//     Changes to this file may cause incorrect behavior and will be lost if the code is regenerated.
// </auto-generated>

using Core.Auditing;
using PolyPersist.Net.Attributes;

namespace Core.Auditing
{

	/// audit trail bázis osztély, ebben lehet egy entity (root entity) változásait követni
	public partial interface IAuditTrail
	{
		public TrailOperations trailOperation { get; set; }
		public string entityType { get; set; }
		public string entityId { get; set; }
		public string userId { get; set; }
		public string userName { get; set; }
		public string payload { get; set; }
		public string previousTrailId { get; set; }
		public string deltaPayload { get; set; }
		[ClusteringColumn(1)]
		public DateTime timestamp { get; set; }
	}
}
