public class ValidationErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "Validation failed";
    public Dictionary<string, string[]> Errors { get; set; } = new();
}