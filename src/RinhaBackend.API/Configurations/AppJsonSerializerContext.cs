using System.Text.Json.Serialization;
using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Enums;
using RinhaBackend.API.Domain.Results;
using RinhaBackend.API.Domain.Aggregates;
using RinhaBackend.API.DTOs;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.DTOs.Responses;
using StackExchange.Redis;

namespace RinhaBackend.API.Configurations;

[JsonSerializable(typeof(Payment))]
[JsonSerializable(typeof(PaymentStatus))]
[JsonSerializable(typeof(PaymentProcessor))]
[JsonSerializable(typeof(PaymentProcessingResult))]
[JsonSerializable(typeof(PaymentSummary))]
[JsonSerializable(typeof(ProcessorSummary))]
[JsonSerializable(typeof(CreatePaymentRequest))]
[JsonSerializable(typeof(PaymentRequest))]
[JsonSerializable(typeof(PaymentSummaryResponse))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(RedisValue))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}