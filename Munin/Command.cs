using System;
using System.Collections.Generic;
using System.Linq;

namespace Munin
{
    /// <summary>
    /// Central command registration and execution system.
    ///
    /// Command format: munin [mod] command [args...]
    /// - Built-in: munin teleport 100 50 100
    /// - Mod command: munin veneer reload
    /// </summary>
    public static class Command
    {
        // Built-in commands (no mod prefix)
        private static readonly Dictionary<string, CommandConfig> _builtInCommands =
            new Dictionary<string, CommandConfig>(StringComparer.OrdinalIgnoreCase);

        // Mod commands: mod name -> command name -> config
        private static readonly Dictionary<string, Dictionary<string, CommandConfig>> _modCommands =
            new Dictionary<string, Dictionary<string, CommandConfig>>(StringComparer.OrdinalIgnoreCase);

        // Track registered mod names for help
        private static readonly HashSet<string> _registeredMods =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        internal static void Initialize()
        {
            Plugin.Log.LogInfo("Command system initialized");
        }

        #region Registration

        /// <summary>
        /// Registers a built-in command (no mod prefix needed).
        /// Usage: munin commandName [args]
        /// </summary>
        public static void Register(CommandConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.Name))
                throw new ArgumentException("Command name is required", nameof(config));
            if (config.Handler == null)
                throw new ArgumentException("Command handler is required", nameof(config));

            var name = config.Name.ToLowerInvariant();

            if (_builtInCommands.ContainsKey(name))
            {
                Plugin.Log.LogWarning($"Overwriting existing built-in command: {name}");
            }

            _builtInCommands[name] = config;
            Plugin.Log.LogDebug($"Registered built-in command: {name}");
        }

        /// <summary>
        /// Registers a mod command.
        /// Usage: munin modName commandName [args]
        /// </summary>
        public static void Register(string modName, CommandConfig config)
        {
            if (string.IsNullOrWhiteSpace(modName))
                throw new ArgumentException("Mod name is required", nameof(modName));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (string.IsNullOrWhiteSpace(config.Name))
                throw new ArgumentException("Command name is required", nameof(config));
            if (config.Handler == null)
                throw new ArgumentException("Command handler is required", nameof(config));

            modName = modName.ToLowerInvariant();
            var commandName = config.Name.ToLowerInvariant();

            config.ModName = modName;

            if (!_modCommands.TryGetValue(modName, out var commands))
            {
                commands = new Dictionary<string, CommandConfig>(StringComparer.OrdinalIgnoreCase);
                _modCommands[modName] = commands;
            }

            if (commands.ContainsKey(commandName))
            {
                Plugin.Log.LogWarning($"Overwriting existing command: {modName} {commandName}");
            }

            commands[commandName] = config;
            _registeredMods.Add(modName);

            Plugin.Log.LogDebug($"Registered mod command: {modName} {commandName}");
        }

        /// <summary>
        /// Registers multiple commands for a mod at once.
        /// </summary>
        public static void RegisterMany(string modName, params CommandConfig[] configs)
        {
            foreach (var config in configs)
            {
                Register(modName, config);
            }
        }

        /// <summary>
        /// Unregisters a built-in command.
        /// </summary>
        public static void Unregister(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName)) return;
            _builtInCommands.Remove(commandName.ToLowerInvariant());
        }

        /// <summary>
        /// Unregisters a mod command.
        /// </summary>
        public static void Unregister(string modName, string commandName)
        {
            if (string.IsNullOrWhiteSpace(modName) || string.IsNullOrWhiteSpace(commandName)) return;

            modName = modName.ToLowerInvariant();
            commandName = commandName.ToLowerInvariant();

            if (_modCommands.TryGetValue(modName, out var commands))
            {
                commands.Remove(commandName);

                if (commands.Count == 0)
                {
                    _modCommands.Remove(modName);
                    _registeredMods.Remove(modName);
                }
            }
        }

        /// <summary>
        /// Unregisters all commands for a mod.
        /// </summary>
        public static void UnregisterMod(string modName)
        {
            if (string.IsNullOrWhiteSpace(modName)) return;

            modName = modName.ToLowerInvariant();
            _modCommands.Remove(modName);
            _registeredMods.Remove(modName);
        }

        #endregion

        #region Execution

        /// <summary>
        /// Executes a command from raw input.
        /// Input should NOT include the munin prefix.
        /// </summary>
        public static CommandResult Execute(string input, Player player)
        {
            if (string.IsNullOrWhiteSpace(input))
                return CommandResult.Error("No command specified. Type 'munin help' for available commands.");

            var parts = input.Trim().Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            var firstWord = parts[0].ToLowerInvariant();
            var rest = parts.Length > 1 ? parts[1] : "";

            // Check if first word is "help"
            if (firstWord == "help")
            {
                return ExecuteHelp(rest, player);
            }

            // Check if first word is a built-in command
            if (_builtInCommands.TryGetValue(firstWord, out var builtInConfig))
            {
                return ExecuteCommand(builtInConfig, rest, player);
            }

            // Check if first word is a mod name
            if (_modCommands.TryGetValue(firstWord, out var modCommands))
            {
                // Get command name from rest
                var modParts = rest.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (modParts.Length == 0)
                {
                    return ShowModHelp(firstWord, modCommands, player);
                }

                var commandName = modParts[0].ToLowerInvariant();
                var commandArgs = modParts.Length > 1 ? modParts[1] : "";

                if (commandName == "help")
                {
                    return ShowModHelp(firstWord, modCommands, player);
                }

                if (modCommands.TryGetValue(commandName, out var modConfig))
                {
                    return ExecuteCommand(modConfig, commandArgs, player);
                }

                return CommandResult.NotFound($"Unknown command: {firstWord} {commandName}. Type 'munin {firstWord} help' for available commands.");
            }

            return CommandResult.NotFound($"Unknown command or mod: {firstWord}. Type 'munin help' for available commands.");
        }

        private static CommandResult ExecuteCommand(CommandConfig config, string argsString, Player player)
        {
            // Check permissions
            if (!Permission.HasPermission(player, config.Permission))
            {
                return CommandResult.NoPermission();
            }

            try
            {
                var args = CommandArgs.Parse(argsString, player);
                return config.Handler(args);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Command execution error: {ex}");
                return CommandResult.Error($"Error executing command: {ex.Message}");
            }
        }

        #endregion

        #region Help

        private static CommandResult ExecuteHelp(string args, Player player)
        {
            args = args.Trim();

            // munin help - show general help
            if (string.IsNullOrEmpty(args))
            {
                return ShowGeneralHelp(player);
            }

            // munin help <command> - show specific built-in command help
            if (_builtInCommands.TryGetValue(args.ToLowerInvariant(), out var builtInConfig))
            {
                return ShowCommandHelp(builtInConfig);
            }

            // munin help <mod> - show mod help
            if (_modCommands.TryGetValue(args.ToLowerInvariant(), out var modCommands))
            {
                return ShowModHelp(args.ToLowerInvariant(), modCommands, player);
            }

            // munin help <mod> <command> - show specific mod command help
            var parts = args.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && _modCommands.TryGetValue(parts[0].ToLowerInvariant(), out var mc))
            {
                if (mc.TryGetValue(parts[1].ToLowerInvariant(), out var cmdConfig))
                {
                    return ShowCommandHelp(cmdConfig);
                }
            }

            return CommandResult.NotFound($"Unknown command or mod: {args}");
        }

        private static CommandResult ShowGeneralHelp(Player player)
        {
            var lines = new List<string>
            {
                $"<color=#{ChatColor.Gold}>Munin Command System</color>",
                ""
            };

            // Built-in commands
            var visibleBuiltIn = _builtInCommands.Values
                .Where(c => !c.Hidden && Permission.HasPermission(player, c.Permission))
                .OrderBy(c => c.Name)
                .ToList();

            if (visibleBuiltIn.Count > 0)
            {
                lines.Add($"<color=#{ChatColor.Gold}>Built-in Commands:</color>");
                foreach (var cmd in visibleBuiltIn)
                {
                    var permTag = cmd.Permission != PermissionLevel.Anyone
                        ? $" <color=#{ChatColor.Warning}>[{Permission.GetPermissionName(cmd.Permission)}]</color>"
                        : "";
                    lines.Add($"  munin {cmd.Name}{permTag} - {cmd.Description ?? "No description"}");
                }
                lines.Add("");
            }

            // Registered mods
            if (_registeredMods.Count > 0)
            {
                lines.Add($"<color=#{ChatColor.Gold}>Registered Mods:</color>");
                foreach (var mod in _registeredMods.OrderBy(m => m))
                {
                    var cmdCount = _modCommands[mod].Count;
                    lines.Add($"  munin {mod} ... ({cmdCount} command{(cmdCount != 1 ? "s" : "")})");
                }
                lines.Add("");
            }

            lines.Add($"Type 'munin help <command>' or 'munin <mod> help' for details");

            return CommandResult.Info(string.Join("\n", lines));
        }

        private static CommandResult ShowModHelp(string modName, Dictionary<string, CommandConfig> commands, Player player)
        {
            var lines = new List<string>
            {
                $"<color=#{ChatColor.Gold}>{modName} Commands:</color>",
                ""
            };

            var visible = commands.Values
                .Where(c => !c.Hidden && Permission.HasPermission(player, c.Permission))
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var cmd in visible)
            {
                var permTag = cmd.Permission != PermissionLevel.Anyone
                    ? $" <color=#{ChatColor.Warning}>[{Permission.GetPermissionName(cmd.Permission)}]</color>"
                    : "";
                lines.Add($"  munin {modName} {cmd.Name}{permTag} - {cmd.Description ?? "No description"}");
            }

            if (visible.Count == 0)
            {
                lines.Add("  No commands available");
            }

            return CommandResult.Info(string.Join("\n", lines));
        }

        private static CommandResult ShowCommandHelp(CommandConfig config)
        {
            var lines = new List<string>();

            var prefix = string.IsNullOrEmpty(config.ModName)
                ? $"munin {config.Name}"
                : $"munin {config.ModName} {config.Name}";

            lines.Add($"<color=#{ChatColor.Gold}>{prefix}</color>");

            if (!string.IsNullOrEmpty(config.Description))
            {
                lines.Add(config.Description);
            }

            if (!string.IsNullOrEmpty(config.Usage))
            {
                lines.Add("");
                lines.Add($"<color=#{ChatColor.Info}>Usage:</color> {prefix} {config.Usage}");
            }

            if (config.Permission != PermissionLevel.Anyone)
            {
                lines.Add($"<color=#{ChatColor.Warning}>Permission:</color> {Permission.GetPermissionName(config.Permission)}");
            }

            if (config.Examples != null && config.Examples.Length > 0)
            {
                lines.Add("");
                lines.Add($"<color=#{ChatColor.Info}>Examples:</color>");
                foreach (var example in config.Examples)
                {
                    lines.Add($"  {prefix} {example}");
                }
            }

            return CommandResult.Info(string.Join("\n", lines));
        }

        #endregion

        #region Queries

        /// <summary>
        /// Checks if a built-in command exists.
        /// </summary>
        public static bool Exists(string commandName)
        {
            return !string.IsNullOrWhiteSpace(commandName) &&
                   _builtInCommands.ContainsKey(commandName.ToLowerInvariant());
        }

        /// <summary>
        /// Checks if a mod command exists.
        /// </summary>
        public static bool Exists(string modName, string commandName)
        {
            if (string.IsNullOrWhiteSpace(modName) || string.IsNullOrWhiteSpace(commandName))
                return false;

            return _modCommands.TryGetValue(modName.ToLowerInvariant(), out var commands) &&
                   commands.ContainsKey(commandName.ToLowerInvariant());
        }

        /// <summary>
        /// Gets all registered mod names.
        /// </summary>
        public static IEnumerable<string> GetRegisteredMods()
        {
            return _registeredMods.ToList();
        }

        /// <summary>
        /// Gets all command names for a mod.
        /// </summary>
        public static IEnumerable<string> GetModCommands(string modName)
        {
            if (string.IsNullOrWhiteSpace(modName))
                return Enumerable.Empty<string>();

            if (_modCommands.TryGetValue(modName.ToLowerInvariant(), out var commands))
                return commands.Keys.ToList();

            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Gets all built-in command names.
        /// </summary>
        public static IEnumerable<string> GetBuiltInCommands()
        {
            return _builtInCommands.Keys.ToList();
        }

        #endregion
    }
}
