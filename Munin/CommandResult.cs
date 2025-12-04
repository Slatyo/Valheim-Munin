using System.Collections.Generic;
using System.Linq;

namespace Munin
{
    /// <summary>
    /// Result of a command execution.
    /// </summary>
    public class CommandResult
    {
        /// <summary>Whether the command executed successfully.</summary>
        public bool IsSuccess { get; private set; }

        /// <summary>Message to display to the player.</summary>
        public string Message { get; private set; }

        /// <summary>Whether to suppress the message output.</summary>
        public bool IsSilent { get; private set; }

        /// <summary>Result type for formatting.</summary>
        public ResultType Type { get; private set; }

        /// <summary>Table data for table results.</summary>
        public string[][] TableData { get; private set; }

        private CommandResult() { }

        /// <summary>Creates a success result with an optional message.</summary>
        public static CommandResult Success(string message = null)
        {
            return new CommandResult
            {
                IsSuccess = true,
                Message = message,
                IsSilent = message == null,
                Type = ResultType.Success
            };
        }

        /// <summary>Creates an error result.</summary>
        public static CommandResult Error(string message)
        {
            return new CommandResult
            {
                IsSuccess = false,
                Message = message,
                Type = ResultType.Error
            };
        }

        /// <summary>Creates a "not found" error result.</summary>
        public static CommandResult NotFound(string message)
        {
            return new CommandResult
            {
                IsSuccess = false,
                Message = message,
                Type = ResultType.NotFound
            };
        }

        /// <summary>Creates a "no permission" error result.</summary>
        public static CommandResult NoPermission()
        {
            return new CommandResult
            {
                IsSuccess = false,
                Message = "You don't have permission to use this command",
                Type = ResultType.NoPermission
            };
        }

        /// <summary>Creates an info result.</summary>
        public static CommandResult Info(string message)
        {
            return new CommandResult
            {
                IsSuccess = true,
                Message = message,
                Type = ResultType.Info
            };
        }

        /// <summary>Creates a table result.</summary>
        public static CommandResult Table(string[][] data)
        {
            return new CommandResult
            {
                IsSuccess = true,
                TableData = data,
                Type = ResultType.Table
            };
        }

        /// <summary>Creates a table result from rows.</summary>
        public static CommandResult Table(IEnumerable<string[]> rows)
        {
            return Table(rows.ToArray());
        }

        /// <summary>
        /// Formats the result for display.
        /// </summary>
        public string Format()
        {
            if (IsSilent) return null;

            if (Type == ResultType.Table && TableData != null)
            {
                return FormatTable();
            }

            string color = Type switch
            {
                ResultType.Success => ChatColor.Success,
                ResultType.Error => ChatColor.Error,
                ResultType.NotFound => ChatColor.Warning,
                ResultType.NoPermission => ChatColor.Error,
                ResultType.Info => ChatColor.Info,
                _ => ChatColor.White
            };

            return $"<color=#{color}>{Message}</color>";
        }

        private string FormatTable()
        {
            if (TableData == null || TableData.Length == 0) return "";

            var lines = new List<string>();

            // Calculate column widths
            int[] widths = new int[TableData[0].Length];
            foreach (var row in TableData)
            {
                for (int i = 0; i < row.Length && i < widths.Length; i++)
                {
                    if (row[i] != null && row[i].Length > widths[i])
                        widths[i] = row[i].Length;
                }
            }

            // Format rows
            bool isHeader = true;
            foreach (var row in TableData)
            {
                var cells = new List<string>();
                for (int i = 0; i < row.Length; i++)
                {
                    string cell = row[i] ?? "";
                    cells.Add(cell.PadRight(widths[i]));
                }

                string line = string.Join("  ", cells);

                if (isHeader)
                {
                    line = $"<color=#{ChatColor.Gold}>{line}</color>";
                    isHeader = false;
                }

                lines.Add(line);
            }

            return string.Join("\n", lines);
        }

        public enum ResultType
        {
            Success,
            Error,
            NotFound,
            NoPermission,
            Info,
            Table
        }
    }

    /// <summary>
    /// Predefined colors for command output.
    /// </summary>
    public static class ChatColor
    {
        public const string White = "FFFFFF";
        public const string Gold = "FFD966";
        public const string Red = "FF6B6B";
        public const string Green = "6BCB77";
        public const string Blue = "4D96FF";
        public const string Gray = "808080";

        // Semantic colors
        public const string Success = Green;
        public const string Warning = Gold;
        public const string Error = Red;
        public const string Info = Blue;
    }
}
