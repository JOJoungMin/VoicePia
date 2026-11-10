namespace Voicepia.Api.Common;

public class Result<T>
{
    public bool Success { get; set; }
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public T? Data { get; set; }

    public static Result<T> Ok(T data) => new() { Success = true, Data = data };
    public static Result<T> Fail(string code, string message) => new() { Code = code, Message = message };
}
