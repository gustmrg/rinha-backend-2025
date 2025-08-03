using System.Text.Json.Serialization;
using RinhaBackend.API.Entities;

namespace RinhaBackend.API.Configurations;

[JsonSerializable(typeof(Payment))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}