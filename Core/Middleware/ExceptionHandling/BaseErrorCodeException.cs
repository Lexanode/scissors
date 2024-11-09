using System;

namespace Core.Middleware.ExceptionHandling;

/// <summary>
/// Делается для маркирования ошибок в 400
/// </summary>
public abstract class BaseErrorCodeException : Exception
{
    public BaseErrorCodeException()
    {
        
    }

    public BaseErrorCodeException(string message) : base(message)
    {
        
    }
}