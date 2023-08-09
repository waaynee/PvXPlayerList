// ReSharper disable RedundantUsingDirective
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper restore RedundantUsingDirective
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;

/*
 * TODO:
 * - seperate/mix option for pvx lists
 */

// ReSharper disable once CheckNamespace
namespace Carbon.Plugins
{
    [Info("PvXPlayerList", "waayne", "1.0")]
    [Description("Shows a list and count of all online and their pvx status by color, non-hidden players")]
    internal class PvXPlayerList : CarbonPlugin
    {
        #region Initialization

        private const string PERM_ALLOW = "pvxplayerlist.allow";
        private const string PERM_HIDE = "pvxplayerlist.hide";

        private bool _adminSeparate;
        private string? _adminColor;
        private string? _pveColor;
        private string? _pvpColor;
        private string? _pveGroupsStr;
        private string[] _pveGroups = { };

        protected override void LoadDefaultConfig()
        {
            Config["Admin List Separate (true/false)"] =
                _adminSeparate = GetConfig("Admin List Separate (true/false)", false);
            Config["Admin Color (Hex Format or Name)"] =
                _adminColor = GetConfig("Admin Color (Hex Format or Name)", "e68c17");
            Config["PvE Color (Hex Format or Name)"] =
                _pveColor = GetConfig("PvE Color (Hex Format or Name)", "green");
            Config["PvP Color (Hex Format or Name)"] =
                _pvpColor = GetConfig("PvP Color (Hex Format or Name)", "red");
            Config["PvE group (if more than one seperate with comma: pve,nodmg)"] =
                _pveGroupsStr = GetConfig("PvE group (if more than one seperate with comma: pve,nodmg)", "pve");

            _pveGroups = _pveGroupsStr.Split(',');

            // Cleanup
            Config.Remove("SeparateAdmin");
            Config.Remove("AdminColor");

            SaveConfig();
        }

        private void Init()
        {
            LoadDefaultConfig();
            LoadDefaultMessages();
            permission.RegisterPermission(PERM_ALLOW, this);
            permission.RegisterPermission(PERM_HIDE, this);
        }

        #endregion

        #region Localization

        private new void LoadDefaultMessages()
        {
            // English
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} admin online",
                ["AdminList"] = "Admin online ({0}): {1}",
                ["NobodyOnline"] = "No players are currently online",
                ["NotAllowed"] = "You are not allowed to use the '{0}' command",
                ["OnlyYou"] = "You are the only one online!",
                ["PlayerCount"] = "{0} player(s) online",
                ["PlayerList"] = "Players online ({0}): {1}"
            }, this);

            // French
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} administrateurs en ligne",
                ["AdminList"] = "Administrateurs en ligne ({0}) : {1}",
                ["NobodyOnline"] = "Aucuns joueurs ne sont actuellement en ligne",
                ["NotAllowed"] = "Vous n’êtes pas autorisé à utiliser la commande « {0} »",
                ["OnlyYou"] = "Vous êtes la seule personne en ligne !",
                ["PlayerCount"] = "{0} joueur(s) en ligne",
                ["PlayerList"] = "Joueurs en ligne ({0}) : {1}"
            }, this, "fr");

            // German
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} Administratoren online",
                ["AdminList"] = "Administratoren online ({0}): {1}",
                ["NobodyOnline"] = "Keine Spieler sind gerade online",
                ["NotAllowed"] = "Sie sind nicht berechtigt, verwenden Sie den Befehl '{0}'",
                ["OnlyYou"] = "Du bist der einzige Online!",
                ["PlayerCount"] = "{0} Spieler online",
                ["PlayerList"] = "Spieler online ({0}): {1}"
            }, this, "de");

            // Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} администраторы онлайн",
                ["AdminList"] = "Администраторы онлайн ({0}): {1}",
                ["NobodyOnline"] = "Ни один из игроков онлайн",
                ["NotAllowed"] = "Нельзя использовать команду «{0}»",
                ["OnlyYou"] = "Вы являетесь единственным онлайн!",
                ["PlayerCount"] = "{0} игрока (ов) онлайн",
                ["PlayerList"] = "Игроков онлайн ({0}): {1}"
            }, this, "ru");

            // Spanish
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["AdminCount"] = "{0} administradores en línea",
                ["AdminList"] = "Los administradores en línea ({0}): {1}",
                ["NobodyOnline"] = "No hay jugadores están actualmente en línea",
                ["NotAllowed"] = "No se permite utilizar el comando '{0}'",
                ["OnlyYou"] = "Usted es el único en línea!",
                ["PlayerCount"] = "{0} jugadores en línea",
                ["PlayerList"] = "Jugadores en línea ({0}): {1}"
            }, this, "es");
        }

        #endregion

        #region Commands

        [Command("online")]
        private void OnlineCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(PERM_ALLOW))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            int adminCount = covalence.Players.Connected.Count(p => p.IsAdmin && !p.HasPermission(PERM_HIDE));
            int playerCount = covalence.Players.Connected.Count(p => !p.IsAdmin && !p.HasPermission(PERM_HIDE));

            player.Reply($"{Lang("AdminCount", player.Id, adminCount)}, {Lang("PlayerCount", player.Id, playerCount)}");
        }

        [Command("players", "who")]
        private void PlayersCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(PERM_ALLOW))
            {
                player.Reply(Lang("NotAllowed", player.Id, command));
                return;
            }

            int adminCount = covalence.Players.Connected.Count(p => p.IsAdmin && !p.HasPermission(PERM_HIDE));
            int playerCount = covalence.Players.Connected.Count(p => !p.IsAdmin && !p.HasPermission(PERM_HIDE));
            int totalCount = adminCount + playerCount;

            if (totalCount <= 0)
            {
                player.Reply(Lang("NobodyOnline", player.Id));
                return;
            }
            if (totalCount == 1 && player.Id != "server_console")
            {
                player.Reply(Lang("OnlyYou", player.Id));
                return;
            }

            string adminList = string.Join(", ",
                covalence.Players.Connected.Where(p => p.IsAdmin && !p.HasPermission(PERM_HIDE))
                    .Select(p => covalence.FormatText($"[#{_adminColor}]{p.Name.Sanitize()}[/#]")).ToArray());

            bool isPlayerPve = false;
            List<IPlayer> pvePlayerList = new();
            List<IPlayer> pvpPlayerList = new();
            foreach (IPlayer p in covalence.Players.Connected)
            {
                if (p.IsAdmin || p.HasPermission(PERM_HIDE))
                    continue;

                foreach (string pveGroup in _pveGroups)
                    if (p.BelongsToGroup(pveGroup))
                        isPlayerPve = true;

                if (isPlayerPve)
                {
                    pvePlayerList.Add(p);
                    isPlayerPve = false;
                }
                else
                {
                    pvpPlayerList.Add(p);
                }
            }
                
            pvePlayerList.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
            pvpPlayerList.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));

            string pveList = string.Join(", ",
                pvePlayerList.Select(p => covalence.FormatText($"[#{_pveColor}]{p.Name.Sanitize()}[/#]"))
                    .ToArray());

            string pvpList = string.Join(", ",
                pvpPlayerList.Select(p => covalence.FormatText($"[#{_pvpColor}]{p.Name.Sanitize()}[/#]"))
                    .ToArray());

            string playerList;
            if (!string.IsNullOrEmpty(pveList) && !string.IsNullOrEmpty(pvpList))
                playerList = string.Concat(pveList, ", ", pvpList);
            else if (!string.IsNullOrEmpty(pveList) && string.IsNullOrEmpty(pvpList))
                playerList = pveList;
            else
                playerList = pvpList;

            if (_adminSeparate && !string.IsNullOrEmpty(adminList))
                player.Reply(Lang("AdminList", player.Id, adminCount, adminList.TrimEnd(' ').TrimEnd(',')));
            else
            {
                playerCount = adminCount + playerCount;
                playerList = string.Concat(adminList, ", ", playerList);
            }

            if (!string.IsNullOrEmpty(playerList))
                player.Reply(Lang("PlayerList", player.Id, playerCount, playerList.TrimEnd(' ').TrimEnd(',')));
        }

        #endregion

        #region Helpers

        private T GetConfig<T>(string name, T value) =>
            Config[name] == null ? value : (T) Convert.ChangeType(Config[name], typeof(T));

        private string Lang(string key, string? id = null, params object[] args) =>
            string.Format(lang.GetMessage(key, this, id), args);

        #endregion
    }
}
