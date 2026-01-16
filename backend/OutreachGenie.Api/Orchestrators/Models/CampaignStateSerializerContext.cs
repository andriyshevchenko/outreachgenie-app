using System.Text.Json;
using System.Text.Json.Serialization;

namespace OutreachGenie.Api.Orchestrators.Models;

/// <summary>
/// JSON serialization context for state models.
/// </summary>
[JsonSerializable(typeof(CampaignState))]
[JsonSerializable(typeof(TaskState))]
internal sealed partial class CampaignStateSerializerContext : JsonSerializerContext
{
}
