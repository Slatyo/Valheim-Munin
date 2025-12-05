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

            // Invalidate prefab cache when new prefabs are registered
            PrefabManager.OnPrefabsRegistered += OnPrefabsRegistered;

            // Initialize Harmony patches (for chat display, etc.)
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded - Odin's raven is ready");
        }

        private void OnPrefabsRegistered()
        {
            // Invalidate prefab cache so it rebuilds with new prefabs
            MuninConsoleCommand.InvalidatePrefabCache();
            Log.LogDebug("Prefab cache invalidated due to new prefab registration");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }

    /// <summary>
    /// Harmony patches for fixing Valheim's console autocomplete caching.
    ///
    /// PROBLEM: Valheim caches m_tabOptions after the first call to the options fetcher.
    /// This means our CommandOptionList() only gets called ONCE, even though we need
    /// to return different options based on what the user has typed (context-aware).
    ///
    /// SOLUTION: Clear m_tabOptions for the "munin" command before each autocomplete,
    /// forcing Valheim to call our CommandOptionList() every time.
    /// </summary>
    [HarmonyPatch]
    public static class ConsoleAutocompletePatch
    {
        // Cached reflection accessors for private fields
        private static System.Reflection.FieldInfo _commandsField;
        private static System.Reflection.FieldInfo _tabOptionsField;
        private static bool _initialized;

        private static void InitializeReflection()
        {
            if (_initialized) return;

            // Get Terminal.commands (private static field)
            _commandsField = AccessTools.Field(typeof(Terminal), "commands");
            if (_commandsField == null)
            {
                Plugin.Log.LogError("Could not find Terminal.commands field");
            }

            // Get Terminal.ConsoleCommand.m_tabOptions (public but we need the type)
            var consoleCommandType = typeof(Terminal).GetNestedType("ConsoleCommand", System.Reflection.BindingFlags.Public);
            if (consoleCommandType != null)
            {
                _tabOptionsField = AccessTools.Field(consoleCommandType, "m_tabOptions");
                if (_tabOptionsField == null)
                {
                    Plugin.Log.LogError("Could not find ConsoleCommand.m_tabOptions field");
                }
            }
            else
            {
                Plugin.Log.LogError("Could not find Terminal.ConsoleCommand type");
            }

            _initialized = true;
        }

        /// <summary>
        /// Patch Terminal.updateCommandList to clear munin's cached tab options.
        /// This method is called when the user presses Tab for autocomplete.
        /// </summary>
        [HarmonyPatch(typeof(Terminal), nameof(Terminal.updateCommandList))]
        [HarmonyPrefix]
        public static void ClearMuninTabOptionsCache()
        {
            try
            {
                InitializeReflection();

                if (_commandsField == null || _tabOptionsField == null)
                    return;

                // Get the commands dictionary
                var commands = _commandsField.GetValue(null) as System.Collections.IDictionary;
                if (commands == null)
                    return;

                // Find the "munin" command and clear its cached options
                if (commands.Contains("munin"))
                {
                    var muninCommand = commands["munin"];
                    if (muninCommand != null)
                    {
                        // Clear the cached tab options so CommandOptionList() gets called again
                        _tabOptionsField.SetValue(muninCommand, null);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"Error clearing tab options cache: {ex.Message}");
            }
        }
    }
}
