using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Managers;
using Jotunn.Utils;

namespace Munin
{
    /// <summary>
    /// Munin - Command & Chat Framework for Valheim mods.
    /// Named after Odin's raven who flies across the world and reports back.
    /// </summary>
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.slatyo.munin";
        public const string PluginName = "Munin";
        public const string PluginVersion = "1.0.0";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            // Initialize command system
            Command.Initialize();

            // Register built-in commands
            BuiltInCommands.Register();

            // Register the munin console command with Jotunn
            CommandManager.Instance.AddConsoleCommand(new MuninConsoleCommand());

            // Initialize Harmony patches (for chat display, etc.)
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded - Odin's raven is ready");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
