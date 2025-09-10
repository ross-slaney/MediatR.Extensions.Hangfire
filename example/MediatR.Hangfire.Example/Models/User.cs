namespace MediatR.Hangfire.Example.Models;

/// <summary>
/// Example user model for demonstration purposes
/// </summary>
public class User
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Result wrapper for operations
/// </summary>
public class OperationResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static OperationResult<T> Success(T data, string? message = null)
    {
        return new OperationResult<T>
        {
            IsSuccess = true,
            Data = data,
            Message = message
        };
    }

    public static OperationResult<T> Failure(string message, params string[] errors)
    {
        return new OperationResult<T>
        {
            IsSuccess = false,
            Message = message,
            Errors = errors.ToList()
        };
    }
}

/// <summary>
/// Report model for demonstration
/// </summary>
public class Report
{
    public required string Type { get; set; }
    public required string Period { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
    public int RecordCount { get; set; }
}
