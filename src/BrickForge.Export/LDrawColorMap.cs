namespace BrickForge.Export;

/// <summary>
/// Maps BrickForge colour names to LDraw numeric colour codes.
/// </summary>
public static class LDrawColorMap
{
    /// <summary>
    /// LDraw colour code used when the name is not in the known mapping.
    /// 16 = main colour placeholder in LDraw.
    /// </summary>
    public const int FallbackCode = 16;

    private static readonly Dictionary<string, int> KnownColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"]              = 0,
        ["white"]              = 15,
        ["red"]                = 4,
        ["blue"]               = 1,
        ["yellow"]             = 14,
        ["light_bluish_gray"]  = 71,
        ["dark_bluish_gray"]   = 72,
        ["transparent_clear"]  = 47,
    };

    /// <summary>
    /// Returns the LDraw numeric code for the given colour name.
    /// Returns <see cref="FallbackCode"/> for unknown names.
    /// </summary>
    public static int GetCode(string colorName) =>
        KnownColors.TryGetValue(colorName, out var code) ? code : FallbackCode;
}
