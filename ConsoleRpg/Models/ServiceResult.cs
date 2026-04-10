namespace ConsoleRpg.Models;

/// <summary>
/// ServiceResult - A lightweight return type for service methods that need
/// to report both success/failure AND a user-facing message, without
/// throwing exceptions for routine outcomes.
///
/// Use the non-generic ServiceResult for void operations ("did it work?
/// what should I tell the player?") and ServiceResult&lt;T&gt; for operations
/// that also return a value ("what's the new current room?").
///
/// Two pre-built factory methods:
///   ServiceResult.Ok("Moved north")
///   ServiceResult.Fail("The door is locked")
///
/// This pattern is a classic "Result type" - common in functional-leaning
/// C# codebases and widely used in real-world APIs. It's simpler than
/// exceptions for expected failure modes ("door locked" isn't exceptional).
/// </summary>
public class ServiceResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string DetailedOutput { get; init; } = string.Empty;

    public static ServiceResult Ok(string message, string? detailedOutput = null) =>
        new() { Success = true, Message = message, DetailedOutput = detailedOutput ?? message };

    public static ServiceResult Fail(string message, string? detailedOutput = null) =>
        new() { Success = false, Message = message, DetailedOutput = detailedOutput ?? message };
}

/// <summary>
/// Generic ServiceResult for operations that also return a value.
/// </summary>
public class ServiceResult<T> : ServiceResult
{
    public T? Value { get; init; }

    public static ServiceResult<T> Ok(T value, string message, string? detailedOutput = null) =>
        new() { Success = true, Value = value, Message = message, DetailedOutput = detailedOutput ?? message };

    public static new ServiceResult<T> Fail(string message, string? detailedOutput = null) =>
        new() { Success = false, Value = default, Message = message, DetailedOutput = detailedOutput ?? message };
}
