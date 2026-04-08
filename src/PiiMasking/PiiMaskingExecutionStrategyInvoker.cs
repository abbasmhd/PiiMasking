namespace PiiMasking;

internal static class PiiMaskingExecutionStrategyInvoker
{
    /// <summary>
    /// When <see cref="PiiMaskingAttribute.Mode"/> is set, finds a strategy with a matching <see cref="IPiiMaskingExecutionStrategy.Name"/>.
    /// </summary>
    /// <returns>The masked string, or <see langword="null"/> when <paramref name="marker"/> has no mode (whitespace-only counts as no mode).</returns>
    /// <exception cref="InvalidOperationException">Mode is set but no registered strategy matches.</exception>
    public static string? TryApplyNamedStrategy(
        IReadOnlyList<IPiiMaskingExecutionStrategy> strategies,
        string value,
        PiiMaskingAttribute marker,
        PiiMaskingSettings settings)
    {
        if (string.IsNullOrWhiteSpace(marker.Mode))
        {
            return null;
        }

        for (var i = 0; i < strategies.Count; i++)
        {
            var strategy = strategies[i];
            if (string.Equals(strategy.Name, marker.Mode, StringComparison.Ordinal))
            {
                return strategy.Mask(value, marker, settings) ?? value;
            }
        }

        var availableStrategies = strategies.Count == 0
            ? "(none registered)"
            : string.Join(", ", strategies.Select(s => $"'{s.Name}'"));

        throw new InvalidOperationException(
            $"No {nameof(IPiiMaskingExecutionStrategy)} is registered with {nameof(IPiiMaskingExecutionStrategy.Name)} '{marker.Mode}' for {nameof(PiiMaskingAttribute.Mode)}. " +
            $"Available strategies: {availableStrategies}");
    }
}
