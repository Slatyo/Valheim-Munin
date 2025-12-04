using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Munin
{
    /// <summary>
    /// Built-in commands that come with Munin.
    /// </summary>
    internal static class BuiltInCommands
    {
        public static void Register()
        {
            // Player commands
            Command.Register(new CommandConfig
            {
                Name = "god",
                Description = "Toggle god mode",
                Permission = PermissionLevel.Admin,
                Handler = args =>
                {
                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    Player.m_debugMode = !Player.m_debugMode;
                    player.SetGodMode(Player.m_debugMode);

                    return CommandResult.Success(Player.m_debugMode ? "God mode enabled" : "God mode disabled");
                }
            });

            Command.Register(new CommandConfig
            {
                Name = "ghost",
                Description = "Toggle ghost mode (enemies ignore you)",
                Permission = PermissionLevel.Admin,
                Handler = args =>
                {
                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    player.SetGhostMode(!player.InGhostMode());

                    return CommandResult.Success(player.InGhostMode() ? "Ghost mode enabled" : "Ghost mode disabled");
                }
            });

            Command.Register(new CommandConfig
            {
                Name = "heal",
                Description = "Heal to full health",
                Permission = PermissionLevel.Admin,
                Handler = args =>
                {
                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    player.Heal(player.GetMaxHealth());

                    return CommandResult.Success("Healed to full health");
                }
            });

            Command.Register(new CommandConfig
            {
                Name = "position",
                Description = "Show current position",
                Permission = PermissionLevel.Anyone,
                Handler = args =>
                {
                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    var pos = player.transform.position;
                    return CommandResult.Info($"Position: {pos.x:F1}, {pos.y:F1}, {pos.z:F1}");
                }
            });

            // Teleport commands
            Command.Register(new CommandConfig
            {
                Name = "teleport",
                Description = "Teleport to coordinates or player",
                Usage = "<x> <y> <z> | <player>",
                Permission = PermissionLevel.Admin,
                Examples = new[] { "100 50 100", "Viking123" },
                Handler = args =>
                {
                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    // Try to parse as coordinates
                    if (args.Count >= 3)
                    {
                        var x = args.Get<float>(0, float.NaN);
                        var y = args.Get<float>(1, float.NaN);
                        var z = args.Get<float>(2, float.NaN);

                        if (!float.IsNaN(x) && !float.IsNaN(y) && !float.IsNaN(z))
                        {
                            player.TeleportTo(new Vector3(x, y, z), player.transform.rotation, true);
                            return CommandResult.Success($"Teleported to {x:F1}, {y:F1}, {z:F1}");
                        }
                    }

                    // Try to find player
                    if (args.Count >= 1)
                    {
                        var targetPlayer = args.GetPlayer(0);
                        if (targetPlayer != null && targetPlayer != player)
                        {
                            player.TeleportTo(targetPlayer.transform.position, player.transform.rotation, true);
                            return CommandResult.Success($"Teleported to {targetPlayer.GetPlayerName()}");
                        }
                    }

                    return CommandResult.Error("Usage: munin teleport <x> <y> <z> or munin teleport <player>");
                }
            });

            // Spawn command
            Command.Register(new CommandConfig
            {
                Name = "spawn",
                Description = "Spawn an item or creature",
                Usage = "<prefab> [amount] [level]",
                Permission = PermissionLevel.Admin,
                Examples = new[] { "SwordIron", "Wood 100", "Boar 1 2" },
                Handler = args =>
                {
                    if (!args.HasRequired(1))
                        return CommandResult.Error("Usage: munin spawn <prefab> [amount] [level]");

                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    var prefabName = args.Get(0);
                    var amount = args.Get(1, 1);
                    var level = args.Get(2, 1);

                    var prefab = ZNetScene.instance?.GetPrefab(prefabName);
                    if (prefab == null)
                    {
                        return CommandResult.NotFound($"Prefab not found: {prefabName}");
                    }

                    // Check if it's an item
                    if (prefab.GetComponent<ItemDrop>() != null)
                    {
                        var item = prefab.GetComponent<ItemDrop>();
                        var inventory = player.GetInventory();

                        for (int i = 0; i < amount; i++)
                        {
                            inventory.AddItem(prefabName, 1, item.m_itemData.m_quality, item.m_itemData.m_variant, 0, "");
                        }

                        return CommandResult.Success($"Spawned {amount}x {prefabName}");
                    }

                    // Spawn as creature/object
                    var spawnPos = player.transform.position + player.transform.forward * 2f;
                    for (int i = 0; i < amount; i++)
                    {
                        var obj = Object.Instantiate(prefab, spawnPos + Random.insideUnitSphere, Quaternion.identity);

                        var character = obj.GetComponent<Character>();
                        if (character != null)
                        {
                            character.SetLevel(level);
                        }
                    }

                    return CommandResult.Success($"Spawned {amount}x {prefabName} (level {level})");
                }
            });

            // Players list
            Command.Register(new CommandConfig
            {
                Name = "players",
                Description = "List online players",
                Permission = PermissionLevel.Anyone,
                Handler = args =>
                {
                    var players = Player.GetAllPlayers();
                    var rows = new List<string[]>
                    {
                        new[] { "Name", "Health", "Position" }
                    };

                    foreach (var p in players)
                    {
                        var pos = p.transform.position;
                        rows.Add(new[]
                        {
                            p.GetPlayerName(),
                            $"{p.GetHealth():F0}/{p.GetMaxHealth():F0}",
                            $"{pos.x:F0}, {pos.z:F0}"
                        });
                    }

                    return CommandResult.Table(rows);
                }
            });

            // Time
            Command.Register(new CommandConfig
            {
                Name = "time",
                Description = "Show or set game time",
                Usage = "[day] | [hour]",
                Permission = PermissionLevel.Anyone,
                Examples = new[] { "", "10", "morning" },
                Handler = args =>
                {
                    if (!EnvMan.instance)
                        return CommandResult.Error("EnvMan not available");

                    if (args.Count == 0)
                    {
                        var day = EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds());
                        var fraction = EnvMan.instance.GetDayFraction();
                        var hour = (int)(fraction * 24);
                        var minute = (int)((fraction * 24 - hour) * 60);

                        return CommandResult.Info($"Day {day}, {hour:D2}:{minute:D2}");
                    }

                    // Setting time requires admin
                    if (!Permission.HasPermission(args.Player, PermissionLevel.Admin))
                        return CommandResult.NoPermission();

                    var arg = args.Get(0).ToLowerInvariant();

                    // Named times
                    float? targetFraction = arg switch
                    {
                        "morning" => 0.25f,
                        "noon" => 0.5f,
                        "evening" => 0.75f,
                        "night" => 0f,
                        "midnight" => 0f,
                        _ => null
                    };

                    if (targetFraction.HasValue)
                    {
                        EnvMan.instance.SkipToMorning();
                        return CommandResult.Success($"Time set to {arg}");
                    }

                    // Numeric day
                    if (int.TryParse(arg, out var targetDay))
                    {
                        // Skip to that day
                        var currentDay = EnvMan.instance.GetDay(ZNet.instance.GetTimeSeconds());
                        var daysToSkip = targetDay - currentDay;
                        if (daysToSkip > 0)
                        {
                            // This is a simplified version - in reality you'd need to manipulate time
                            return CommandResult.Info($"Would skip to day {targetDay} (not fully implemented)");
                        }
                    }

                    return CommandResult.Error("Invalid time format. Use: morning, noon, evening, night, or a day number");
                }
            });

            // Kill all nearby
            Command.Register(new CommandConfig
            {
                Name = "killnearby",
                Description = "Kill all nearby creatures",
                Usage = "[radius]",
                Permission = PermissionLevel.Admin,
                Examples = new[] { "", "50" },
                Handler = args =>
                {
                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    var radius = args.Get(0, 30f);
                    var killed = 0;

                    var characters = Character.GetAllCharacters();
                    foreach (var character in characters)
                    {
                        if (character == player) continue;
                        if (character is Player) continue;

                        var distance = Vector3.Distance(player.transform.position, character.transform.position);
                        if (distance <= radius)
                        {
                            var hitData = new HitData
                            {
                                m_damage = { m_damage = 1000000f }
                            };
                            character.Damage(hitData);
                            killed++;
                        }
                    }

                    return CommandResult.Success($"Killed {killed} creatures within {radius}m");
                }
            });

            // Clear drops
            Command.Register(new CommandConfig
            {
                Name = "cleardrops",
                Description = "Clear all dropped items nearby",
                Usage = "[radius]",
                Permission = PermissionLevel.Admin,
                Handler = args =>
                {
                    var player = args.Player;
                    if (player == null) return CommandResult.Error("No player found");

                    var radius = args.Get(0, 50f);
                    var cleared = 0;

                    var items = Object.FindObjectsByType<ItemDrop>(FindObjectsSortMode.None);
                    foreach (var item in items)
                    {
                        var distance = Vector3.Distance(player.transform.position, item.transform.position);
                        if (distance <= radius)
                        {
                            var nview = item.GetComponent<ZNetView>();
                            if (nview != null && nview.IsValid())
                            {
                                nview.Destroy();
                                cleared++;
                            }
                        }
                    }

                    return CommandResult.Success($"Cleared {cleared} dropped items");
                }
            });

            Plugin.Log.LogInfo($"Registered {Command.GetBuiltInCommands().Count()} built-in commands");
        }
    }
}
