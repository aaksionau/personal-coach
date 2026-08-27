namespace Coach.Application.Models;

/// <summary>
/// The result of one values wizard turn: the assistant's reply text, and whether the model
/// distilled and saved the values profile on this turn (the page uses that to close out the wizard).
/// </summary>
public sealed record ValuesWizardReply(string Reply, bool ProfileSaved);
