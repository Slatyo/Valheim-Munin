using System;

namespace Munin
{
    /// <summary>
    /// Configuration for registering a command.
    /// </summary>
    public class CommandConfig
    {
        /// <summary>Command name (e.g., "spawn", "teleport").</summary>
        public string Name { get; set; }

        /// <summary>Short description of what the command does.</summary>
        public string Description { get; set; }

        /// <summary>Usage syntax (e.g., "spawn &lt;prefab&gt; [amount]").</summary>
        public string Usage { get; set; }

        /// <summary>Permission level required to execute this command.</summary>
        public PermissionLevel Permission { get; set; } = PermissionLevel.Anyone;

        /// <summary>Command handler function.</summary>
        public Func<CommandArgs, CommandResult> Handler { get; set; }

        /// <summary>Optional mod namespace (e.g., "veneer", "vault"). Null for built-in commands.</summary>
        public string ModName { get; set; }

        /// <summary>Example usages shown in help.</summary>
        public string[] Examples { get; set; }

        /// <summary>Whether this command is hidden from help listings.</summary>
        public bool Hidden { get; set; }
    }
}
