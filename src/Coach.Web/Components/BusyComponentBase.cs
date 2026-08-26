using Microsoft.AspNetCore.Components;

namespace Coach.Web.Components;

/// <summary>
/// A page that runs one user-triggered action at a time and surfaces its failure as a dismissible
/// error message. Extracted from the identical busy/error try-catch-finally shape repeated across
/// the chat and goals pages.
/// </summary>
public abstract class BusyComponentBase : ComponentBase
{
    protected bool Busy { get; private set; }

    /// <summary>Settable directly (not only via <see cref="RunBusyAsync"/>) so a page can report a
    /// failure from outside a busy-guarded action, e.g. its initial data load.</summary>
    protected string? Error { get; set; }

    /// <summary>Runs <paramref name="action"/> guarded by <see cref="Busy"/>; a thrown exception is
    /// turned into <see cref="Error"/> via <paramref name="describeError"/> rather than propagating.</summary>
    protected async Task RunBusyAsync(Func<Task> action, Func<Exception, string> describeError)
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
