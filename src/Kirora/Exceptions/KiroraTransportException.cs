namespace Kirora.Exceptions;

/// <summary>KiroraTransportException Kirora API contract.</summary>
public class KiroraTransportException : Exception
{
    internal KiroraTransportException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>KiroraTimeoutException Kirora API contract.</summary>
public sealed class KiroraTimeoutException : KiroraTransportException
{
    internal KiroraTimeoutException(Exception innerException) : base("The Kirora request timed out.", innerException) { }
}

/// <summary>KiroraInvalidResponseException Kirora API contract.</summary>
public sealed class KiroraInvalidResponseException : Exception
{
    /// <summary>Provides the documented configuration or response value.</summary>
    public Kirora.Http.KiroraResponseMetadata Metadata { get; }
    internal KiroraInvalidResponseException(string message, Kirora.Http.KiroraResponseMetadata metadata, Exception? innerException = null)
        : base(message, innerException) => Metadata = metadata;
}
