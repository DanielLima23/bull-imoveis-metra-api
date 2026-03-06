namespace Imoveis.Application.Common;

public sealed class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(string message, int statusCode = 400, string code = "business_error") : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}
