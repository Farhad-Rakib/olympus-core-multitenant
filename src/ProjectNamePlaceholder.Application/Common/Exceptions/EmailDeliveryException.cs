namespace ProjectNamePlaceholder.Application.Common.Exceptions;

// 502: the failure is upstream (the mail provider), not in the caller's request.
//
// The recipient is deliberately kept OUT of the message: GlobalExceptionHandler returns
// Message verbatim to the client, and echoing the address back would leak it to whoever
// triggered the send. It stays available on the exception for logging.
public sealed class EmailDeliveryException : AppException
{
    public string Recipient { get; }

    public EmailDeliveryException(string recipient, Exception innerException)
        : base("The email could not be delivered.", innerException, 502)
    {
        Recipient = recipient;
    }
}
