using Dapper;
using RinhaBackend.API.Domain.Aggregates;
using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Enums;

namespace RinhaBackend.API.Configurations;

public static class DapperAotConfiguration
{
    public static void Configure()
    {
        SqlMapper.SetTypeMap(typeof(Payment), new CustomPropertyTypeMap(
            typeof(Payment), 
            (type, columnName) => type.GetProperties().FirstOrDefault(prop =>
                string.Equals(prop.Name, columnName, StringComparison.OrdinalIgnoreCase)) ?? 
                throw new InvalidOperationException($"Property '{columnName}' not found on type {type.Name}")
        ));
        
        SqlMapper.SetTypeMap(typeof(PaymentSummary), new CustomPropertyTypeMap(
            typeof(PaymentSummary), 
            (type, columnName) => type.GetProperties().FirstOrDefault(prop =>
                string.Equals(prop.Name, columnName, StringComparison.OrdinalIgnoreCase)) ?? 
                throw new InvalidOperationException($"Property '{columnName}' not found on type {type.Name}")
        ));
    }
}