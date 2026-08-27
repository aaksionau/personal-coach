using Coach.Application.Models;
using Coach.Application.Services;
using Microsoft.AspNetCore.Components;

namespace Coach.Web.Components;

/// <summary>
/// Base for a page scoped to one coach via a <c>{CoachSlug}</c> route parameter. Resolves the slug
/// against the registry once per distinct coach -- rendering the not-found page when it is unknown
/// -- and then calls <see cref="OnCoachChangedAsync"/> so the page can (re)load its data. Owns a
/// <see cref="CancellationToken"/> that is cancelled both on disposal and whenever the selected
/// coach changes, so work started for one coach can't land on another.
/// </summary>
public abstract class CoachScopedPage : ComponentBase, IDisposable
{
    private CancellationTokenSource _cts = new();

    [Parameter]
    public string CoachSlug { get; set; } = string.Empty;

    [Inject]
    private CoachPersonaRegistry Registry { get; set; } = default!;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The resolved persona for the current route, or <c>null</c> before the first resolve.</summary>
    protected CoachPersona? Persona { get; private set; }

    /// <summary>Short label for the current coach ("Career"), or <c>""</c> before the first resolve.</summary>
    protected string CoachName => Persona?.ShortName ?? string.Empty;

    /// <summary>Cancelled on disposal and on every coach switch -- pass it to all async page work.</summary>
    protected CancellationToken CancellationToken => _cts.Token;

    protected sealed override async Task OnParametersSetAsync()
    {
        if (Persona?.Slug == CoachSlug)
        {
            return;
        }

        if (!Registry.TryGet(CoachSlug, out var persona))
        {
            Navigation.NotFound();
            return;
        }

        CancelWork();
        _cts = new CancellationTokenSource();

        Persona = persona;
        await OnCoachChangedAsync();
    }

    /// <summary>Runs once per distinct resolved coach. Reset and reload page state here.</summary>
    protected abstract Task OnCoachChangedAsync();

    public void Dispose()
    {
        CancelWork();
        GC.SuppressFinalize(this);
    }

    private void CancelWork()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
