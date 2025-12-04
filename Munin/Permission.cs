namespace Munin
{
    /// <summary>
    /// Permission levels for command execution.
    /// </summary>
    public enum PermissionLevel
    {
        /// <summary>All players can use this command.</summary>
        Anyone = 0,

        /// <summary>Only server admins can use this command.</summary>
        Admin = 1,

        /// <summary>Only the server host/owner can use this command.</summary>
        Host = 2
    }

    /// <summary>
    /// Permission checking utilities.
    /// </summary>
    public static class Permission
    {
        /// <summary>
        /// Checks if a player has the specified permission level.
        /// </summary>
        public static bool HasPermission(Player player, PermissionLevel level)
        {
            if (player == null) return false;

            return level switch
            {
                PermissionLevel.Anyone => true,
                PermissionLevel.Admin => IsAdmin(player),
                PermissionLevel.Host => IsHost(player),
                _ => false
            };
        }

        /// <summary>
        /// Checks if a player is a server admin.
        /// </summary>
        public static bool IsAdmin(Player player)
        {
            if (player == null) return false;

            // Single player is always admin
            if (ZNet.instance == null) return true;

            // Server host is always admin
            if (ZNet.instance.IsServer()) return true;

            // For clients, check if cheats are enabled (requires admin rights)
            if (player == Player.m_localPlayer && Console.instance != null)
            {
                return Console.instance.IsCheatsEnabled();
            }

            return false;
        }

        /// <summary>
        /// Checks if a player is the server host.
        /// </summary>
        public static bool IsHost(Player player)
        {
            if (player == null) return false;

            // Single player is always host
            if (ZNet.instance == null) return true;

            // Check if this is the server
            return ZNet.instance.IsServer() && player == Player.m_localPlayer;
        }

        /// <summary>
        /// Gets a human-readable string for a permission level.
        /// </summary>
        public static string GetPermissionName(PermissionLevel level)
        {
            return level switch
            {
                PermissionLevel.Anyone => "Anyone",
                PermissionLevel.Admin => "Admin",
                PermissionLevel.Host => "Host",
                _ => "Unknown"
            };
        }
    }
}
