using Microsoft.AspNetCore.Mvc;
using RinhaBackend.API.DTOs.Requests;
using RinhaBackend.API.Entities;
using RinhaBackend.API.Enums;
using RinhaBackend.API.Interfaces;

namespace RinhaBackend.API.Controllers;

[ApiController]
public class PaymentsController : ControllerBase
{
    [HttpGet("payments-summary")]
    public IActionResult GetPaymentsSummaryAsync()
    {
        return Ok();
    }

    [HttpPost]
    [Route("payments")]
    public async Task<IActionResult> CreatePaymentAsync(
        [FromServices] IPaymentRepository paymentRepository,
        [FromServices] IProcessorHealthMonitor processorHealthMonitor,
        [FromServices] IPaymentProcessorFactory paymentProcessorFactory,
        [FromBody] CreatePaymentRequest request)
    {
        // TODO: Validate request model
        
        var existingPayment = await paymentRepository.GetPaymentByCorrelationIdAsync(request.CorrelationId);
    
        if (existingPayment is not null)
        {
            return Conflict("Payment with the same correlation ID already exists.");
        }
        
        var payment = new Payment
        {
            Id = Guid.CreateVersion7(),
            Amount = request.Amount,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CorrelationId = request.CorrelationId
        };
    
        await paymentRepository.CreatePaymentAsync(payment);
        
        // TODO: Get all processor health infos
        // This should be cached for performance
        // and to avoid hitting the processors too frequently
        var processorHealthInfos = await processorHealthMonitor.GetAllHealthInfosAsync();
        
        if (processorHealthInfos is null || !processorHealthInfos.Any())
        {
            return BadRequest("No payment processors available.");
        }
        
        // TODO: Get the processor selection strategy from DI
        // Select the best processor based on health info
        // and configuration weights
        // This should be done asynchronously
        // and should not block the request
        // If no healthy processor is found, return BadRequest (or put in a queue for later processing)
        var paymentProcessor = paymentProcessorFactory.Create(processorHealthInfos.First().ProcessorName); 
        
        // TODO: Get payment processor from DI
        // Use the selected processor to process the payment
        // If the processor is not healthy, return BadRequest
        // If the processor fails to process the payment, update the payment status to Failed
        
        try
        {
            await paymentProcessor.ProcessPaymentAsync(payment);
        
            payment.Status = PaymentStatus.Completed;
            payment.ProcessedAt = DateTime.UtcNow;
            await paymentRepository.UpdatePaymentAsync(payment);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
    
            payment.Status = PaymentStatus.Failed;
            await paymentRepository.UpdatePaymentAsync(payment);
        
            return Problem("Payment processing failed.");
        }

        return Ok();
    }

}