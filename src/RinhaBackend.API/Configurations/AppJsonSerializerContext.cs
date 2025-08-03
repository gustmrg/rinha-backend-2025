using System.Text.Json.Serialization;
using RinhaBackend.API.Entities;
using StackExchange.Redis;

namespace RinhaBackend.API.Configurations;

[JsonSerializable(typeof(Payment))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(RedisValue))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}