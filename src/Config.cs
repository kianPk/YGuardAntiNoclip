using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace YGuardAntiNoclip;

public class YGuardAntiNoclipConfig : BasePluginConfig
{
    [JsonPropertyName("ChatPrefix")]
    public string ChatPrefix { get; set; } = "AntiNoclip";

    /// <summary>
    /// When true, players holding AdminFlag (or @css/root) may use cheats/noclip.
    /// Default false — nobody on public, including panel owners.
    /// </summary>
    [JsonPropertyName("AllowAdminNoclip")]
    public bool AllowAdminNoclip { get; set; }

    [JsonPropertyName("AdminFlag")]
    public string AdminFlag { get; set; } = "@css/root";

    /// <summary>Practice servers keep noclip for utility training.</summary>
    [JsonPropertyName("SkipPractice")]
    public bool SkipPractice { get; set; } = true;

    /// <summary>
    /// Leaves noclip that another plugin grants (ServersBhop's !noclip practice)
    /// and its css_noclip alone. sv_cheats stays locked and every other cheat
    /// command stays blocked.
    /// </summary>
    [JsonPropertyName("AllowPluginNoclip")]
    public bool AllowPluginNoclip { get; set; }
}
