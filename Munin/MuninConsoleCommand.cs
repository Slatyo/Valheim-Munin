using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;

namespace Munin
{
    /// <summary>
    /// Console command that routes to the Munin command system.
    /// Usage: munin [command] [args...]
    ///
    /// AUTOCOMPLETE DESIGN:
    /// Valheim's console autocomplete works by:
    /// 1. Getting the "last word" the user is typing
    /// 2. Calling CommandOptionList() to get all valid options
    /// 3. Filtering options that start with the last word
    /// 4. Replacing the last word with the selected option
    ///
    /// This means we must return FULL argument values (not contextual suggestions).
    /// For multi-argument commands like "munin affix clear", we detect context
    /// and return appropriate options for that argument position.
    /// </summary>
    public class MuninConsoleCommand : ConsoleCommand
    {
        public override string Name => "munin";

        public override string Help => "Munin command system. Type 'munin help' for available commands.";

        // Cached prefab list to avoid rebuilding on every keystroke
        private static List<string> _cachedPrefabs;
        private static bool _prefabCacheInitialized;

        public override void Run(string[] args)
        {
            // Reconstruct the command string from args
            var input = string.Join(" ", args);

            // Execute through Munin's command system
            var player = Player.m_localPlayer;
            var result = Command.Execute(input, player);

            // Show result in console
            if (result != null && !string.IsNullOrEmpty(result.Message))
            {
                var formatted = result.Format();
                if (!string.IsNullOrEmpty(formatted))
                {
                    // Output to console
                    Console.instance.Print(formatted);

                    // Also show in chat if available
                    if (global::Chat.instance != null)
                    {
                        global::Chat.instance.AddString(formatted);
                    }
                }
            }
        }

        public override List<string> CommandOptionList()
        {
            try
            {
                // Get current input to provide context-aware completions
                var input = Console.instance?.m_input?.text ?? "";

                // Determine argument position based on spaces
                var argPosition = GetArgumentPosition(input);

                // Split to get the actual arguments (filter empty but track position separately)
                var parts = input.Split(' ')
                    .Where(p => !string.IsNullOrEmpty(p))
                    .ToArray();

                // Position 1: First argument after "munin" - show commands and mod names
                if (argPosition <= 1)
                {
                    return GetFirstLevelOptions();
                }

                // Position 2+: Need context from first argument
                if (parts.Length < 2)
                {
                    return GetFirstLevelOptions();
                }

                var firstArg = parts[1].ToLowerInvariant();

                // Position 2: Second argument - depends on what first argument is
                if (argPosition == 2)
                {
                    // "munin spawn <prefab>" - show prefabs
                    if (firstArg == "spawn")
                    {
                        return GetPrefabList();
                    }

                    // "munin teleport <player>" - show player names
                    if (firstArg == "teleport")
                    {
                        return GetPlayerNames();
                    }

                    // "munin <mod> <command>" - show mod's commands
                    if (Command.GetRegisteredMods().Any(m => m.Equals(firstArg, System.StringComparison.OrdinalIgnoreCase)))
                    {
                        return GetModCommands(firstArg);
                    }
                }

                // Position 3+: No suggestions for now
                return new List<string>();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"Autocomplete error: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// Determines which argument position the cursor is at.
        /// Position 0 = command name (munin)
        /// Position 1 = first argument
        /// Position 2 = second argument, etc.
        /// </summary>
        private int GetArgumentPosition(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            // Count spaces to determine position
            // Trailing space means we're ready for next argument
            var spaceCount = input.Count(c => c == ' ');

            // If input ends with space, we're at the next position
            if (input.EndsWith(" "))
            {
                return spaceCount;
            }

            // Otherwise we're still typing the current position
            return spaceCount;
        }

        private List<string> GetFirstLevelOptions()
        {
            var options = new List<string>();

            // Add built-in commands
            options.AddRange(Command.GetBuiltInCommands());

            // Add "help"
            if (!options.Contains("help"))
                options.Add("help");

            // Add registered mod names
            options.AddRange(Command.GetRegisteredMods());

            // Return sorted - Valheim handles filtering by what user typed
            return options.OrderBy(x => x).ToList();
        }

        private List<string> GetModCommands(string modName)
        {
            // Return all commands for the mod - Valheim handles filtering
            return Command.GetModCommands(modName).OrderBy(x => x).ToList();
        }

        private List<string> GetPrefabList()
        {
            // Build cache on first access (only once per session)
            if (!_prefabCacheInitialized)
            {
                BuildPrefabCache();
            }

            // Return full cached list - Valheim handles filtering by what user typed
            return _cachedPrefabs ?? new List<string>();
        }

        private void BuildPrefabCache()
        {
            var prefabs = new HashSet<string>();

            try
            {
                // Get items from ObjectDB (spawnable items)
                if (ObjectDB.instance != null)
                {
                    foreach (var item in ObjectDB.instance.m_items)
                    {
                        if (item != null && IsValidPrefabName(item.name))
                        {
                            prefabs.Add(item.name);
                        }
                    }
                }

                // Also get creatures and other spawnable prefabs from Jotunn's cache
                var cachedPrefabs = Jotunn.Managers.PrefabManager.Cache.GetPrefabs(typeof(UnityEngine.GameObject));
                foreach (var kvp in cachedPrefabs)
                {
                    if (IsValidPrefabName(kvp.Key))
                    {
                        prefabs.Add(kvp.Key);
                    }
                }

                // Sort once and cache
                _cachedPrefabs = prefabs.OrderBy(x => x).ToList();
                _prefabCacheInitialized = true;

                Plugin.Log.LogDebug($"Prefab cache built with {_cachedPrefabs.Count} entries");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"Error building prefab cache: {ex.Message}");
                _cachedPrefabs = new List<string>();
            }
        }

        /// <summary>
        /// Invalidates the prefab cache, forcing a rebuild on next access.
        /// Call this when new prefabs are registered.
        /// </summary>
        public static void InvalidatePrefabCache()
        {
            _prefabCacheInitialized = false;
            _cachedPrefabs = null;
        }

        private bool IsValidPrefabName(string name)
        {
            // Skip null/empty
            if (string.IsNullOrEmpty(name))
                return false;

            // Skip clones
            if (name.Contains("(Clone)"))
                return false;

            // Skip localization tokens
            if (name.StartsWith("$"))
                return false;

            // Skip internal prefabs
            if (name.StartsWith("_"))
                return false;

            // Skip VFX/SFX prefabs
            if (name.StartsWith("vfx_") || name.StartsWith("sfx_") || name.StartsWith("fx_"))
                return false;

            // Skip numeric-only names
            if (int.TryParse(name, out _))
                return false;

            // Skip very short names (likely internal)
            if (name.Length < 2)
                return false;

            return true;
        }

        private List<string> GetPlayerNames()
        {
            var players = new List<string>();

            foreach (var player in Player.GetAllPlayers())
            {
                if (player != null)
                {
                    players.Add(player.GetPlayerName());
                }
            }

            // Return all players - Valheim handles filtering by what user typed
            return players.OrderBy(x => x).ToList();
        }
    }
}
