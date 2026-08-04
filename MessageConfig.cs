namespace AutoMessages;

public sealed class MessageConfig
{
    public string Id { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public string Message { get; set; } = string.Empty;

    public float Time { get; set; }
}
