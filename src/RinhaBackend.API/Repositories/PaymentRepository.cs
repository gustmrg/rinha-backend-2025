using Dapper;
using Npgsql;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.DTOs.Responses;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private ILogger<PaymentRepository> _logger;

    public PaymentRepository(IDbConnectionFactory connectionFactory, ILogger<PaymentRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    
    // TODO: Add transaction support for payment creation and updates
    // TODO: Add logging for better error handling and debugging
    public async Task CreatePaymentAsync(Payment payment)
    {
        const string sql = @"
            INSERT INTO payments (payment_id, amount, status, requested_at, correlation_id)
            VALUES (@Id, @Amount, @Status, @RequestedAt, @CorrelationId)";
        
        using var connection = _connectionFactory.CreateConnection();

        try
        {
            await connection.ExecuteAsync(sql, payment);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<IEnumerable<Payment>> GetPaymentsAsync(DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT payment_id as Id, amount, status, requested_at as RequestedAt, 
                   correlation_id as CorrelationId, processor_name as ProcessorName
            FROM payments
            WHERE (@From IS NULL OR requested_at >= @From)
              AND (@To IS NULL OR requested_at <= @To)";
        
        using var connection = _connectionFactory.CreateConnection();

        try
        {
            return await connection.QueryAsync<Payment>(sql, new { From = from, To = to });
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

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId)
    {
        const string sql = @"
            SELECT payment_id as Id, amount, status, requested_at as RequestedAt, 
                   correlation_id as CorrelationId, processor_name as ProcessorName
            FROM payments 
            WHERE payment_id = @Id
            LIMIT 1";
        
        using var connection = _connectionFactory.CreateConnection();

        try
        {
            return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { Id = paymentId });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<Payment?> GetPaymentByCorrelationIdAsync(Guid correlationId)
    {
        const string sql = @"
            SELECT * FROM payments
            WHERE correlation_id = @CorrelationId";
        
        using var connection = _connectionFactory.CreateConnection();
        
        try
        {
            return await connection.QueryFirstOrDefaultAsync<Payment>(sql, new { CorrelationId = correlationId });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task UpdatePaymentAsync(Payment payment)
    {
        const string sql = @"
            UPDATE payments
            SET status = @Status,
                processor_name = @ProcessorName
            WHERE payment_id = @Id";
        
        using var connection = _connectionFactory.CreateConnection();

        try
        {
            await connection.ExecuteAsync(sql, payment);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<PaymentsSummaryResponse> GetPaymentsSummaryAsync(DateTime from, DateTime to)
    {
        const string sql = @"
            SELECT 
                COALESCE(processor_name, 0) as ProcessorName,
                COUNT(*) as TotalRequests,
                COALESCE(SUM(amount), 0) as TotalAmount
            FROM payments
            WHERE (@From IS NULL OR requested_at >= @From)
              AND (@To IS NULL OR requested_at <= @To)
            GROUP BY processor_name";
        
        using var connection = _connectionFactory.CreateConnection();

        try
        {
            var results = await connection.QueryAsync(sql, new { From = from, To = to });
            
            var defaultSummary = new PaymentSummary(0, 0);
            var fallbackSummary = new PaymentSummary(0, 0);
            
            foreach (var result in results)
            {
                var processorName = (int?)result.ProcessorName;
                var totalRequests = (int)result.TotalRequests;
                var totalAmount = (decimal)result.TotalAmount;
                
                if (processorName == (int)PaymentProcessor.Default)
                {
                    defaultSummary = new PaymentSummary(totalRequests, totalAmount);
                }
                else if (processorName == (int)PaymentProcessor.Fallback)
                {
                    fallbackSummary = new PaymentSummary(totalRequests, totalAmount);
                }
            }
            
            return new PaymentsSummaryResponse
            {
                Default = defaultSummary,
                Fallback = fallbackSummary
            };
        }
        catch (NpgsqlException ex) when (ex.Message.Contains("timeout"))
        {
            _logger.LogError(ex, "Database timeout occurred during GetPaymentsSummaryAsync");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during GetPaymentsSummaryAsync");
            throw;
        }
    }

    public Task<bool> PaymentExistsAsync(Guid correlationId)
    {
        const string sql = @"
            SELECT EXISTS (
                SELECT 1 FROM payments 
                WHERE correlation_id = @CorrelationId
            )";
        
        using var connection = _connectionFactory.CreateConnection();
        
        try
        {
            return connection.ExecuteScalarAsync<bool>(sql, new { CorrelationId = correlationId });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}