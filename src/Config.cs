using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace YGuardAntiNoclip;

public class YGuardAntiNoclipConfig : BasePluginConfig
{
    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = "AntiNoclip";

    /// <summary>
    /// When true, players holding AdminFlag (or @css/root) may keep noclip.
    /// Default false — nobody flies on public servers, including panel owners.
    /// </summary>
    [JsonPropertyName("AllowAdminNoclip")]
    public bool AllowAdminNoclip { get; set; }

    [JsonPropertyName("AdminFlag")]
    public string AdminFlag { get; set; } = "@css/root";

    /// <summary>Practice servers keep noclip for utility training.</summary>
    [JsonPropertyName("SkipPractice")]
    public bool SkipPractice { get; set; } = true;
}
