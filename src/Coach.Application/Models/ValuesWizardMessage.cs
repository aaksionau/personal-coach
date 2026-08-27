namespace Coach.Application.Models;

/// <summary>
/// One message in the (non-persisted) values wizard conversation. Kept as a plain app-layer record
/// so the Blazor page can hold the running transcript without depending on Microsoft.Extensions.AI
/// chat types.
/// </summary>
public sealed record ValuesWizardMessage(bool IsUser, string Content);
