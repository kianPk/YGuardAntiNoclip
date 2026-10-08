using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace YGuardAntiNoclip;

/// <summary>
/// Blocks engine + plugin noclip on every server except Practice.
/// Nobody may fly by default (including owners).
/// </summary>
public class YGuardAntiNoclipPlugin : BasePlugin, IPluginConfig<YGuardAntiNoclipConfig>
{
    public override string ModuleName => "YGuard Anti Noclip";
    public override string ModuleVersion => "1.0.3";
    public override string ModuleAuthor => "YGuard";
    public override string ModuleDescription =>
        "Blocks bind/noclip for everyone on non-Practice servers";

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

        // Always register a status command so RCON can prove the DLL loaded.
        AddCommand(
            "css_antinoclip_status",
            "Print AntiNoclip status",
            (player, info) =>
            {
                var msg =
                    $"[AntiNoclip] v{ModuleVersion} active={_active} type={serverType} allowAdmin={Config.AllowAdminNoclip}";
                Logger.LogInformation("{Msg}", msg);
                info.ReplyToCommand(msg);
                player?.PrintToChat($" {ChatColors.Green}{msg}");
            });

        if (!_active)
        {
            Logger.LogInformation(
                "YGuardAntiNoclip idle (SERVER_TYPE={Type})",
                string.IsNullOrEmpty(serverType) ? "(unset)" : serverType);
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
            // Visible proof the plugin is on this map — remove later if noisy.
            Server.PrintToChatAll(
                $" {ChatColors.Green}[AntiNoclip]{ChatColors.Default} active — noclip blocked");
        });

        AddTimer(5f, LockCheatsOff, TimerFlags.REPEAT);
        LockCheatsOff();

        Logger.LogInformation(
            "YGuardAntiNoclip ACTIVE (SERVER_TYPE={Type}, allowAdmin={Allow})",
            string.IsNullOrEmpty(serverType) ? "(unset)" : serverType,
            Config.AllowAdminNoclip);
    }

    private void LockCheatsOff()
    {
        if (!_active) return;
        Server.ExecuteCommand("sv_cheats 0");
        try
        {
            ConVar.Find("sv_cheats")?.SetValue(false);
        }
        catch
        {
            // older CSS builds may not expose SetValue the same way
        }
    }

    private HookResult OnNoclipCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!_active) return HookResult.Continue;
        // Block even for console/RCON targeting players — nobody gets noclip.
        if (player == null || !player.IsValid || player.IsBot)
        {
            return HookResult.Handled;
        }

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

            var moveType = pawn.MoveType;
            byte actual = 0;
            try
            {
                actual = Schema.GetSchemaValue<byte>(pawn.Handle, "CBaseEntity", "m_nActualMoveType");
            }
            catch
            {
                actual = (byte)moveType;
            }

            if (moveType != MoveType_t.MOVETYPE_NOCLIP
                && actual != (byte)MoveType_t.MOVETYPE_NOCLIP)
            {
                continue;
            }

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
