using Dapper;
using Npgsql;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Entities;
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
            INSERT INTO payments (payment_id, amount, status, created_at, correlation_id)
            VALUES (@Id, @Amount, @Status, @CreatedAt, @CorrelationId)";
        
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
            SELECT payment_id as Id, amount, status, created_at as CreatedAt, 
                   processed_at as ProcessedAt, correlation_id as CorrelationId, 
                   processor_name as ProcessorName
            FROM payments
            WHERE (@From IS NULL OR processed_at >= @From)
              AND (@To IS NULL OR processed_at <= @To)
            ORDER BY created_at DESC";
        
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
            SELECT payment_id as Id, amount, status, created_at as CreatedAt, 
                   processed_at as ProcessedAt, correlation_id as CorrelationId, 
                   processor_name as ProcessorName
            FROM payments 
            WHERE payment_id = @Id";
        
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
                processed_at = @ProcessedAt,
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
}