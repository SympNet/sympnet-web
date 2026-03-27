namespace SympNet.Infrastructure.Exceptions;

public class AppException : Exception
{
    public AppException(string message) : base(message) { }

    public AppException(string message, Exception innerException)
        : base(message, innerException) { }
}

// Classes optionnelles pour plus de précision
public class BadRequestException : AppException
{
    public BadRequestException(string message) : base(message) { }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message) { }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message) { }
}