using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace YGuardAntiNoclip;

/// <summary>
/// Blocks engine + plugin noclip on Public / Custom / Ranked.
/// Practice is left alone so utility training keeps .noclip.
/// </summary>
public class YGuardAntiNoclipPlugin : BasePlugin, IPluginConfig<YGuardAntiNoclipConfig>
{
    public override string ModuleName => "YGuard Anti Noclip";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "YGuard";
    public override string ModuleDescription =>
        "Blocks bind/noclip and plugin noclip for everyone on non-Practice servers";

    public YGuardAntiNoclipConfig Config { get; set; } = new();

    private bool _active;
    private readonly HashSet<ulong> _warned = [];

    public void OnConfigParsed(YGuardAntiNoclipConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        var serverType = (Environment.GetEnvironmentVariable("SERVER_TYPE") ?? "").Trim();
        _active = !(Config.SkipPractice
                    && serverType.Equals("Practice", StringComparison.OrdinalIgnoreCase));

        if (!_active)
        {
            Logger.LogInformation(
                "YGuardAntiNoclip idle (SERVER_TYPE={Type})",
                serverType);
            return;
        }

        AddCommandListener("noclip", OnNoclipCommand, HookMode.Pre);
        AddCommandListener("css_noclip", OnNoclipCommand, HookMode.Pre);
        AddCommandListener("sm_noclip", OnNoclipCommand, HookMode.Pre);

        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _warned.Clear();
            LockCheatsOff();
        });

        AddTimer(15f, LockCheatsOff, TimerFlags.REPEAT);
        LockCheatsOff();

        Logger.LogInformation(
            "YGuardAntiNoclip active (SERVER_TYPE={Type}, allowAdmin={Allow})",
            serverType,
            Config.AllowAdminNoclip);
    }

    private void LockCheatsOff()
    {
        if (!_active) return;
        Server.ExecuteCommand("sv_cheats 0");
    }

    private HookResult OnNoclipCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!_active) return HookResult.Continue;
        if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
        if (MayNoclip(player)) return HookResult.Continue;

        WarnOnce(player);
        return HookResult.Handled;
    }

    private void OnTick()
    {
        if (!_active) return;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player is not { IsValid: true, IsBot: false, IsHLTV: false }) continue;
            if (MayNoclip(player)) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;
            if (pawn.MoveType != MoveType_t.MOVETYPE_NOCLIP) continue;

            SetWalk(pawn);
            WarnOnce(player);
        }
    }

    private bool MayNoclip(CCSPlayerController player)
    {
        if (!Config.AllowAdminNoclip) return false;
        return AdminManager.PlayerHasPermissions(player, Config.AdminFlag)
               || AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private void WarnOnce(CCSPlayerController player)
    {
        if (!_warned.Add(player.SteamID)) return;
        player.PrintToChat(
            $" {ChatColors.Red}[{Config.ChatPrefix}]{ChatColors.Default} Noclip is disabled on this server.");
    }

    private static void SetWalk(CCSPlayerPawn pawn)
    {
        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (byte)MoveType_t.MOVETYPE_WALK);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }
}
