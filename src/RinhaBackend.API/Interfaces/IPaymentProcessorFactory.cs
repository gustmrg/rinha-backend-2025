namespace RinhaBackend.API.Interfaces;

public interface IPaymentProcessorFactory
{
    /// <summary>
    /// Creates an instance of a payment processor based on the provided processor name.
    /// </summary>
    /// <param name="processorName">The name of the payment processor.</param>
    /// <returns>An instance of <see cref="IPaymentProcessor"/>.</returns>
    IPaymentProcessor Create(string processorName);
}