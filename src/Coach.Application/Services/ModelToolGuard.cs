namespace Coach.Application.Services;

/// <summary>
/// Shared guard for model tool methods. Tool-call arguments are untrusted model output -- the model
/// may reference an id that doesn't exist or doesn't belong to the current coach -- so a validation
/// failure is reported back as text for the model to see rather than thrown.
/// </summary>
internal static class ModelToolGuard
{
    public static async Task<string> GuardedAsync(Func<Task<string>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return $"Could not complete the action: {ex.Message}";
        }
    }
}
