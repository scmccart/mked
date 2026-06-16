namespace Mked.Console;

/// <summary>Determines whether plain-text (non-pager) output mode is active.</summary>
public static class RendererSelector
{
    /// <summary>
    /// Returns <see langword="true"/> when <c>--plain</c> is set or stdout is redirected,
    /// indicating that output should be written as plain text with no interactive pager.
    /// </summary>
    public static bool IsPlainMode(ViewSettings settings) =>
        IsPlainMode(settings, System.Console.IsOutputRedirected);

    /// <summary>
    /// Testable overload that accepts the redirect state as a parameter rather than reading
    /// the static <see cref="System.Console.IsOutputRedirected"/> property directly.
    /// </summary>
    public static bool IsPlainMode(ViewSettings settings, bool isOutputRedirected) =>
        settings.Plain || isOutputRedirected;
}
