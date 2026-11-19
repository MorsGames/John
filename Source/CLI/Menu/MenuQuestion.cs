namespace John;

internal sealed record MenuQuestion(string Title, IReadOnlyList<(string Value, string? Description)> Choices, string? Initial = null);
