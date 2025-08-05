using System.Data;
using Dapper;
using Npgsql;
using RinhaBackend.API.Domain.Aggregates;
using RinhaBackend.API.Domain.Entities;
using RinhaBackend.API.Domain.Enums;
using RinhaBackend.API.DTOs;
using RinhaBackend.API.Factories.Interfaces;
using RinhaBackend.API.Repositories.Interfaces;

namespace RinhaBackend.API.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<PaymentRepository> _logger;

    public PaymentRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<PaymentRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    [DapperAot]
    public async Task SavePaymentAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        const string sql = @"
        INSERT INTO payments (payment_id, amount, status, requested_at, correlation_id, processor)
        VALUES (@Id, @Amount, @Status, @RequestedAt, @CorrelationId, @PaymentProcessor)";
        
        using var connection = _connectionFactory.CreateConnection();

        try
        {
            var parameters = new
            {
                Id = payment.Id,
                Amount = payment.Amount,
                Status = (int)payment.Status,
                RequestedAt = payment.RequestedAt,
                CorrelationId = payment.CorrelationId,
                PaymentProcessor = (int)payment.PaymentProcessor
            };
            
            await connection.ExecuteAsync(sql, parameters);
        }
        catch (NpgsqlException ex) when (ex.Message.Contains("timeout"))
        {
            _logger.LogError(ex, "Database timeout occurred during SavePaymentAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during SavePaymentAsync");
            throw;
        }
    }

    [DapperAot]
    public async Task<IEnumerable<PaymentSummary>> GetPaymentSummaryAsync(DateTime from, DateTime to)
    {
        const string query = @"
            SELECT 
                processor AS Processor,
                COUNT(*) AS TotalRequests,
                SUM(amount) AS TotalAmount
            FROM payments
            WHERE requested_at >= @From 
              AND requested_at <= @To
            GROUP BY processor";

        var parameters = new { From = from, To = to };
        
        using var connection = _connectionFactory.CreateConnection();

        try
        {
            var results = await connection.QueryAsync<PaymentSummaryDto>(query, parameters);
        
            return results.Select(dto => new PaymentSummary
            {
                Processor = (PaymentProcessor)dto.Processor,
                TotalRequests = dto.TotalRequests,
                TotalAmount = dto.TotalAmount
            });
        }
        catch (NpgsqlException ex) when (ex.Message.Contains("timeout"))
        {
            _logger.LogError(ex, "Database timeout occurred during GetPaymentsAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetPaymentsAsync");
            throw;
        }
    }
}