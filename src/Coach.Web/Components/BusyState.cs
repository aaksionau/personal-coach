namespace Coach.Web.Components;

/// <summary>
/// Tracks one user-triggered action at a time and its failure as a dismissible error message.
/// Held as a field (composition) rather than a component base class, since a Blazor component's
/// single inheritance slot may be needed for something else (e.g. <c>OwningComponentBase&lt;T&gt;</c>).
/// </summary>
public sealed class BusyState
{
    public bool Busy { get; private set; }

    /// <summary>Settable directly (not only via <see cref="RunAsync"/>) so a page can report a
    /// failure from outside a busy-guarded action, e.g. its initial data load.</summary>
    public string? Error { get; set; }

    /// <summary>Runs <paramref name="action"/> guarded by <see cref="Busy"/>; a thrown exception is
    /// turned into <see cref="Error"/> via <paramref name="describeError"/> rather than propagating.</summary>
    public async Task RunAsync(Func<Task> action, Func<Exception, string> describeError)
    {
        if (Busy)
        {
            return;
        }

        Busy = true;
        Error = null;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Error = describeError(ex);
        }
        finally
        {
            Busy = false;
        }
    }
}
