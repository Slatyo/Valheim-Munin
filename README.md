# Munin

> Command framework for Valheim mods. Named after Odin's raven who flies across the world and reports back.

Munin provides a unified console command system for Valheim mods. All commands flow through a single entry point (`munin`), with support for permissions, argument parsing, and autocomplete.

## Features

- **Unified command entry point** - All commands use `munin <command>` or `munin <mod> <command>`
- **Permission system** - Commands can require Anyone, Admin, or Host permissions
- **Rich argument parsing** - Positional args, named args (`--key=value`), and flags (`-f`, `--force`)
- **Autocomplete** - Tab completion for commands, subcommands, and prefab names
- **Built-in commands** - Essential admin/debug commands included
- **Colored output** - Formatted messages with semantic colors

## Requirements

- [BepInEx 5.4.x](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
- [Jotunn 2.26.0+](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)

## Installation

1. Install BepInEx and Jotunn
2. Extract `Munin.dll` to `BepInEx/plugins/`

## Usage

Open the console (F5) and type:

```
munin help              # Show all commands
munin <command>         # Run a built-in command
munin <mod> <command>   # Run a mod's command
munin help <command>    # Show command details
```

## Built-in Commands

| Command | Permission | Description |
|---------|------------|-------------|
| `munin help` | Anyone | Show available commands |
| `munin god` | Admin | Toggle god mode |
| `munin ghost` | Admin | Toggle ghost mode |
| `munin heal` | Admin | Heal to full health |
| `munin position` | Anyone | Show current position |
| `munin teleport <x> <y> <z>` | Admin | Teleport to coordinates |
| `munin teleport <player>` | Admin | Teleport to player |
| `munin spawn <prefab> [amount] [level]` | Admin | Spawn item or creature |
| `munin players` | Anyone | List online players |
| `munin time` | Anyone | Show current game time |
| `munin time <morning\|noon\|evening\|night>` | Admin | Set time of day |
| `munin killnearby [radius]` | Admin | Kill nearby creatures |
| `munin cleardrops [radius]` | Admin | Clear dropped items |

## API Reference

### Registering Commands

```csharp
using Munin;

// Register a mod command: munin mymod mycommand
Command.Register("mymod", new CommandConfig
{
    Name = "mycommand",
    Description = "Does something cool",
    Usage = "<required> [optional]",
    Permission = PermissionLevel.Admin,
    Examples = new[] { "foo", "bar 123" },
    Handler = args =>
    {
        var player = args.Player;
        var value = args.Get<int>(0, 10); // First arg as int, default 10

        return CommandResult.Success($"Did something with {value}");
    }
});

// Register multiple commands at once
Command.RegisterMany("mymod",
    new CommandConfig { Name = "cmd1", Handler = args => CommandResult.Success() },
    new CommandConfig { Name = "cmd2", Handler = args => CommandResult.Success() }
);

// Unregister commands
Command.Unregister("mymod", "mycommand");
Command.UnregisterMod("mymod"); // Unregister all commands for a mod
```

### CommandConfig Properties

```csharp
public class CommandConfig
{
    string Name;                              // Command name (required)
    string Description;                       // Short description for help
    string Usage;                             // Usage syntax (e.g., "<prefab> [amount]")
    PermissionLevel Permission;               // Anyone, Admin, or Host
    Func<CommandArgs, CommandResult> Handler; // Command handler (required)
    string[] Examples;                        // Example usages for help
    bool Hidden;                              // Hide from help listings
}
```

### CommandArgs - Argument Parsing

```csharp
// Example input: spawn SwordIron 5 --silent -f
Handler = args =>
{
    // Positional arguments
    string prefab = args.Get(0);           // "SwordIron"
    int amount = args.Get<int>(1, 1);      // 5 (or default 1)
    float value = args.Get<float>(2, 0f);  // default 0f (not provided)

    // Flags (-f or --force)
    bool force = args.HasFlag("f", "force");  // true

    // Named arguments (--key=value)
    string filter = args.GetNamed("filter");
    int limit = args.GetNamed<int>("limit", 10);

    // Check required args
    if (!args.HasRequired(1))
        return CommandResult.Error("Usage: spawn <prefab>");

    // Get rest of args as string (for messages)
    string message = args.GetRest(1);  // Everything from index 1 onwards

    // Find player by name
    Player target = args.GetPlayer(0);

    // The executing player
    Player executor = args.Player;

    // Raw input string
    string raw = args.RawInput;

    // Argument count
    int count = args.Count;

    return CommandResult.Success();
}
```

### CommandResult - Return Values

```csharp
// Success with message
return CommandResult.Success("Item spawned");

// Success without message (silent)
return CommandResult.Success();

// Error message
return CommandResult.Error("Something went wrong");

// Not found (yellow warning color)
return CommandResult.NotFound("Player not found");

// Permission denied
return CommandResult.NoPermission();

// Info message (blue)
return CommandResult.Info("Current position: 100, 50, 100");

// Table output
return CommandResult.Table(new[]
{
    new[] { "Name", "Health", "Position" },  // Header row
    new[] { "Viking", "100/100", "100, 50" },
    new[] { "Warrior", "80/100", "200, 60" }
});
```

### Permission Levels

```csharp
public enum PermissionLevel
{
    Anyone,  // All players (default)
    Admin,   // Server admins (devcommands enabled)
    Host     // Server host only
}

// Check permissions manually
if (Permission.IsAdmin(player)) { }
if (Permission.IsHost(player)) { }
if (Permission.HasPermission(player, PermissionLevel.Admin)) { }
```

### ChatColor - Predefined Colors

```csharp
public static class ChatColor
{
    string White   = "FFFFFF";
    string Gold    = "FFD966";
    string Red     = "FF6B6B";
    string Green   = "6BCB77";
    string Blue    = "4D96FF";
    string Gray    = "808080";

    // Semantic aliases
    string Success = Green;
    string Warning = Gold;
    string Error   = Red;
    string Info    = Blue;
}

// Usage in formatted strings
$"<color=#{ChatColor.Success}>Operation successful</color>"
```

### Query Methods

```csharp
// Check if commands exist
bool exists = Command.Exists("spawn");
bool modExists = Command.Exists("mymod", "reload");

// Get registered mods
IEnumerable<string> mods = Command.GetRegisteredMods();

// Get commands for a mod
IEnumerable<string> commands = Command.GetModCommands("mymod");

// Get all built-in commands
IEnumerable<string> builtIn = Command.GetBuiltInCommands();
```

## Complete Example

```csharp
using BepInEx;
using Munin;

[BepInPlugin("com.author.mymod", "MyMod", "1.0.0")]
[BepInDependency("com.slaty.munin")]
public class MyMod : BaseUnityPlugin
{
    private void Awake()
    {
        Command.RegisterMany("mymod",
            new CommandConfig
            {
                Name = "greet",
                Description = "Greet a player",
                Usage = "<player> [message]",
                Permission = PermissionLevel.Anyone,
                Examples = new[] { "Viking", "Viking Hello there!" },
                Handler = args =>
                {
                    var target = args.GetPlayer(0);
                    if (target == null)
                        return CommandResult.NotFound("Player not found");

                    var message = args.GetRest(1);
                    if (string.IsNullOrEmpty(message))
                        message = "Hello!";

                    return CommandResult.Success($"Greeted {target.GetPlayerName()}: {message}");
                }
            },
            new CommandConfig
            {
                Name = "reload",
                Description = "Reload configuration",
                Permission = PermissionLevel.Admin,
                Handler = args =>
                {
                    // Reload logic here
                    return CommandResult.Success("Configuration reloaded");
                }
            }
        );

        Logger.LogInfo("MyMod commands registered with Munin");
    }

    private void OnDestroy()
    {
        Command.UnregisterMod("mymod");
    }
}
```

## License

MIT License - See LICENSE file for details.
