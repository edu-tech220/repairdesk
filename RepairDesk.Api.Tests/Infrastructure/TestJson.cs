using System.Text.Json;
using System.Text.Json.Serialization;

namespace RepairDesk.Api.Tests.Infrastructure;

/// <summary>Matches the API's own JSON options (enums as strings) so tests can deserialize responses correctly.</summary>
public static class TestJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
