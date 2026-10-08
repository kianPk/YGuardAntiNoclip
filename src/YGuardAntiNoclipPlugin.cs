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
/// Locks cheats on Public/Custom/Ranked: sv_cheats 0, block engine + CSS cheat
/// commands, strip noclip. Practice is left alone.
/// </summary>
public class YGuardAntiNoclipPlugin : BasePlugin, IPluginConfig<YGuardAntiNoclipConfig>
{
    public override string ModuleName => "YGuard Anti Noclip";
    public override string ModuleVersion => "1.0.4";
    public override string ModuleAuthor => "YGuard";
    public override string ModuleDescription =>
        "Blocks cheats/noclip/give for everyone on non-Practice servers";

    public YGuardAntiNoclipConfig Config { get; set; } = new();

    private bool _active;
    private readonly HashSet<ulong> _warned = [];

    // Engine + common CSS/SimpleAdmin cheat surfaces regular players abuse.
    private static readonly string[] BlockedCommands =
    [
        "noclip",
        "god",
        "buddha",
        "give",
        "impulse",
        "ent_create",
        "ent_fire",
        "ent_teleport",
        "setpos",
        "setang",
        "thirdperson",
        "firstperson",
        "css_noclip",
        "css_god",
        "css_give",
        "css_weapon",
        "css_hp",
        "css_speed",
        "css_gravity",
        "css_money",
        "css_respawn",
        "css_freeze",
        "css_unfreeze",
        "css_strip",
        "css_resize",
        "sm_noclip",
        "sm_god",
        "sm_give",
    ];

    public void OnConfigParsed(YGuardAntiNoclipConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        var serverType = (Environment.GetEnvironmentVariable("SERVER_TYPE") ?? "").Trim();
        _active = !(Config.SkipPractice
                    && serverType.Equals("Practice", StringComparison.OrdinalIgnoreCase));

        AddCommand(
            "css_antinoclip_status",
            "Print AntiNoclip status",
            (player, info) =>
            {
                var cheats = ConVar.Find("sv_cheats")?.GetPrimitiveValue<bool>() ?? false;
                var msg =
                    $"[AntiNoclip] v{ModuleVersion} active={_active} type={serverType} sv_cheats={(cheats ? 1 : 0)}";
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

        foreach (var cmd in BlockedCommands)
        {
            AddCommandListener(cmd, OnBlockedCommand, HookMode.Pre);
        }

        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnMapStart>(_ =>
        {
            _warned.Clear();
            LockCheatsOff();
            Server.PrintToChatAll(
                $" {ChatColors.Green}[AntiNoclip]{ChatColors.Default} cheats locked");
        });

        AddTimer(2f, LockCheatsOff, TimerFlags.REPEAT);
        LockCheatsOff();

        Logger.LogInformation(
            "YGuardAntiNoclip ACTIVE (SERVER_TYPE={Type})",
            string.IsNullOrEmpty(serverType) ? "(unset)" : serverType);
    }

    private void LockCheatsOff()
    {
        if (!_active) return;
        Server.ExecuteCommand("sv_cheats 0");
        try
        {
            var cvar = ConVar.Find("sv_cheats");
            if (cvar != null && cvar.GetPrimitiveValue<bool>())
            {
                cvar.SetValue(false);
            }
        }
        catch
        {
            // ignore CSS ConVar quirks
        }
    }

    private HookResult OnBlockedCommand(CCSPlayerController? player, CommandInfo info)
    {
        if (!_active) return HookResult.Continue;
        if (player == null || !player.IsValid || player.IsBot)
        {
            // Still block anonymous client cheat spam; allow real server console.
            return HookResult.Continue;
        }

        if (MayCheat(player)) return HookResult.Continue;

        WarnOnce(player);
        return HookResult.Handled;
    }

    private void OnTick()
    {
        if (!_active) return;

        // Keep cheats pinned off every frame — other plugins/admins flip it back.
        try
        {
            var cvar = ConVar.Find("sv_cheats");
            if (cvar != null && cvar.GetPrimitiveValue<bool>())
            {
                cvar.SetValue(false);
                Server.ExecuteCommand("sv_cheats 0");
            }
        }
        catch
        {
            // fall through to movetype strip
        }

        foreach (var player in Utilities.GetPlayers())
        {
            if (player is not { IsValid: true, IsBot: false, IsHLTV: false }) continue;
            if (MayCheat(player)) continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            var moveType = pawn.MoveType;
            byte actual = (byte)moveType;
            try
            {
                actual = Schema.GetSchemaValue<byte>(pawn.Handle, "CBaseEntity", "m_nActualMoveType");
            }
            catch
            {
                // ignore
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

    private bool MayCheat(CCSPlayerController player)
    {
        if (!Config.AllowAdminNoclip) return false;
        return AdminManager.PlayerHasPermissions(player, Config.AdminFlag)
               || AdminManager.PlayerHasPermissions(player, "@css/root");
    }

    private void WarnOnce(CCSPlayerController player)
    {
        if (!_warned.Add(player.SteamID)) return;
        player.PrintToChat(
            $" {ChatColors.Red}[{Config.ChatPrefix}]{ChatColors.Default} Cheats are disabled on this server.");
    }

    private static void SetWalk(CCSPlayerPawn pawn)
    {
        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", (byte)MoveType_t.MOVETYPE_WALK);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }
}
