namespace RinhaBackend.API.Models;

public record PaymentsSummaryResponse(PaymentSummaryData Default, PaymentSummaryData Fallback);