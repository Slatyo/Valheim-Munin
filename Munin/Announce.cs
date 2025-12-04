using UnityEngine;

namespace Munin
{
    /// <summary>
    /// Big center-screen announcements.
    /// </summary>
    public static class Announce
    {
        /// <summary>
        /// Shows a center-screen announcement to all players.
        /// </summary>
        public static void Show(string title, string subtitle = null, float duration = 3f)
        {
            foreach (var player in Player.GetAllPlayers())
            {
                Show(player, title, subtitle, duration);
            }
        }

        /// <summary>
        /// Shows a center-screen announcement to a specific player.
        /// </summary>
        public static void Show(Player player, string title, string subtitle = null, float duration = 3f)
        {
            if (player == null || player != Player.m_localPlayer) return;
            if (string.IsNullOrEmpty(title)) return;

            if (MessageHud.instance != null)
            {
                // Format with optional subtitle
                var message = title;
                if (!string.IsNullOrEmpty(subtitle))
                {
                    message = $"{title}\n<size=75%><color=#{ChatColor.Gray}>{subtitle}</color></size>";
                }

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, message);
            }
        }

        /// <summary>
        /// Shows a boss-style announcement (large text).
        /// </summary>
        public static void ShowBoss(string title)
        {
            if (MessageHud.instance != null)
            {
                // Use the boss message type for dramatic effect
                MessageHud.instance.ShowBiomeFoundMsg(title, true);
            }
        }

        /// <summary>
        /// Shows a biome-discovery style announcement.
        /// </summary>
        public static void ShowBiome(string biomeName)
        {
            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowBiomeFoundMsg(biomeName, false);
            }
        }

        /// <summary>
        /// Shows a corner notification (top-left).
        /// </summary>
        public static void ShowNotification(Player player, string message, Sprite icon = null)
        {
            if (player == null || player != Player.m_localPlayer) return;
            if (string.IsNullOrEmpty(message)) return;

            if (MessageHud.instance != null)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, message);
            }
        }
    }
}
