using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Munin
{
    /// <summary>
    /// Parsed command arguments with helper methods.
    /// </summary>
    public class CommandArgs
    {
        private readonly string[] _positional;
        private readonly Dictionary<string, string> _named;
        private readonly HashSet<string> _flags;

        /// <summary>The raw input string.</summary>
        public string RawInput { get; }

        /// <summary>Number of positional arguments.</summary>
        public int Count => _positional.Length;

        /// <summary>The player who executed the command.</summary>
        public Player Player { get; }

        public CommandArgs(string[] positional, Dictionary<string, string> named, HashSet<string> flags, string rawInput, Player player)
        {
            _positional = positional ?? Array.Empty<string>();
            _named = named ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _flags = flags ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            RawInput = rawInput ?? "";
            Player = player;
        }

        /// <summary>
        /// Gets a positional argument by index.
        /// </summary>
        public string Get(int index)
        {
            if (index < 0 || index >= _positional.Length)
                return null;
            return _positional[index];
        }

        /// <summary>
        /// Gets a positional argument as a specific type.
        /// </summary>
        public T Get<T>(int index, T defaultValue = default)
        {
            var value = Get(index);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                return ConvertTo<T>(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Checks if a flag is present (e.g., --force or -f).
        /// </summary>
        public bool HasFlag(params string[] names)
        {
            return names.Any(n => _flags.Contains(n));
        }

        /// <summary>
        /// Gets a named argument value (e.g., --filter=value).
        /// </summary>
        public string GetNamed(string name)
        {
            return _named.TryGetValue(name, out var value) ? value : null;
        }

        /// <summary>
        /// Gets a named argument as a specific type.
        /// </summary>
        public T GetNamed<T>(string name, T defaultValue = default)
        {
            var value = GetNamed(name);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            try
            {
                return ConvertTo<T>(value);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Checks if at least N required arguments are present.
        /// </summary>
        public bool HasRequired(int count)
        {
            return _positional.Length >= count;
        }

        /// <summary>
        /// Gets all positional arguments from a starting index joined together.
        /// Useful for messages or text that spans multiple args.
        /// </summary>
        public string GetRest(int startIndex)
        {
            if (startIndex < 0 || startIndex >= _positional.Length)
                return "";

            return string.Join(" ", _positional.Skip(startIndex));
        }

        /// <summary>
        /// Tries to find a player by name or partial match.
        /// </summary>
        public Player GetPlayer(int index)
        {
            var name = Get(index);
            if (string.IsNullOrEmpty(name))
                return null;

            return FindPlayer(name);
        }

        /// <summary>
        /// Gets all positional arguments as an array.
        /// </summary>
        public string[] GetAll()
        {
            return _positional.ToArray();
        }

        /// <summary>
        /// Parses raw argument string into CommandArgs.
        /// </summary>
        public static CommandArgs Parse(string input, Player player)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new CommandArgs(Array.Empty<string>(), null, null, input, player);

            var positional = new List<string>();
            var named = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var tokens = Tokenize(input);

            foreach (var token in tokens)
            {
                if (token.StartsWith("--"))
                {
                    var arg = token.Substring(2);
                    var eqIndex = arg.IndexOf('=');
                    if (eqIndex > 0)
                    {
                        var key = arg.Substring(0, eqIndex);
                        var value = arg.Substring(eqIndex + 1);
                        named[key] = value;
                    }
                    else
                    {
                        flags.Add(arg);
                    }
                }
                else if (token.StartsWith("-") && token.Length > 1 && !char.IsDigit(token[1]))
                {
                    // Short flags like -f -v
                    foreach (var c in token.Substring(1))
                    {
                        flags.Add(c.ToString());
                    }
                }
                else
                {
                    positional.Add(token);
                }
            }

            return new CommandArgs(positional.ToArray(), named, flags, input, player);
        }

        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            var current = "";
            bool inQuotes = false;
            char quoteChar = '"';

            foreach (var c in input)
            {
                if ((c == '"' || c == '\'') && !inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar && inQuotes)
                {
                    inQuotes = false;
                }
                else if (c == ' ' && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current);
                        current = "";
                    }
                }
                else
                {
                    current += c;
                }
            }

            if (current.Length > 0)
                tokens.Add(current);

            return tokens;
        }

        private static T ConvertTo<T>(string value)
        {
            var type = typeof(T);

            if (type == typeof(string))
                return (T)(object)value;

            if (type == typeof(int))
                return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);

            if (type == typeof(float))
                return (T)(object)float.Parse(value, CultureInfo.InvariantCulture);

            if (type == typeof(double))
                return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);

            if (type == typeof(bool))
            {
                value = value.ToLowerInvariant();
                return (T)(object)(value == "true" || value == "1" || value == "yes" || value == "on");
            }

            if (type == typeof(long))
                return (T)(object)long.Parse(value, CultureInfo.InvariantCulture);

            return (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        private static Player FindPlayer(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            name = name.ToLowerInvariant();

            // Exact match first
            foreach (var player in Player.GetAllPlayers())
            {
                if (player.GetPlayerName().ToLowerInvariant() == name)
                    return player;
            }

            // Partial match
            foreach (var player in Player.GetAllPlayers())
            {
                if (player.GetPlayerName().ToLowerInvariant().Contains(name))
                    return player;
            }

            return null;
        }
    }
}
