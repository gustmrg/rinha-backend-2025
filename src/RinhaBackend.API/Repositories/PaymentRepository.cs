using Dapper;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PaymentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
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

    public async Task<Payment?> GetPaymentByIdAsync(Guid paymentId)
    {
        throw new NotImplementedException();
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
                processed_at = @ProcessedAt
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