using System.Collections.Generic;
using System.Linq;
using Jotunn.Entities;

namespace Munin
{
    /// <summary>
    /// Console command that routes to the Munin command system.
    /// Usage: munin [command] [args...]
    /// </summary>
    public class MuninConsoleCommand : ConsoleCommand
    {
        public override string Name => "munin";

        public override string Help => "Munin command system. Type 'munin help' for available commands.";

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
            // Get current input to provide context-aware completions
            var input = Console.instance?.m_input?.text ?? "";
            var parts = input.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            // "munin" alone or "munin " - show all commands and mods
            if (parts.Length <= 1)
            {
                return GetFirstLevelOptions();
            }

            // "munin <cmd>" - check what the first arg is
            var firstArg = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";

            // "munin spawn " - show prefabs
            if (firstArg == "spawn" && parts.Length >= 2)
            {
                return GetPrefabList();
            }

            // "munin teleport " - show player names
            if (firstArg == "teleport" && parts.Length >= 2)
            {
                return GetPlayerNames();
            }

            // "munin <mod> " - show mod's commands
            if (Command.GetRegisteredMods().Any(m => m.Equals(firstArg, System.StringComparison.OrdinalIgnoreCase)))
            {
                return Command.GetModCommands(firstArg).OrderBy(x => x).ToList();
            }

            // Default: show first level options
            return GetFirstLevelOptions();
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

            return options.OrderBy(x => x).ToList();
        }

        private List<string> GetPrefabList()
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
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"Error getting prefab list: {ex.Message}");
            }

            return prefabs.OrderBy(x => x).ToList();
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

            return players.OrderBy(x => x).ToList();
        }
    }
}
