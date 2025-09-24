// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using System.Globalization;
using System;
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{
    [Info("AdminCall", "S1m0n", "1.1.7")]
    [Description("A plugin for player-admin communication.")]

    public class AdminCall : RustPlugin
    {
        StoredData storedData;
        string DataFile = "ticket_data";
        Dictionary<string, string> Commands;
        Dictionary<string, string> AdminCommands;

        void Loaded()
        {
            openDataFile();

            Commands = new Dictionary<string, string>
            {
                { "<color=#ffd479>/ticket <сообщение></color>", "Отправить вопрос администратору на проверку." },
                { "<color=#ffd479>/calladmin <причина></color>", "Свистнуть админу для телепортации к вам." },
                { "<color=#ffd479>/admins</color>", "Показать список онлайн-админов." },
            };

            AdminCommands = new Dictionary<string, string>
            {
                { "<color=orange>/tickettp <id></color>", "Телепортироваться на локацию, откуда был отправлен вопрос." },
                { "<color=orange>/ticketlist <страница></color>", "Списки отправленных вопросов." },
                { "<color=orange>/ticketview <id></color>", "Просмотр сведений о конкретном вопросе." },
                { "<color=orange>/ticketdel <id></color>", "Удаляет вопрос после того, как вы решили его." },
                { "<color=orange>/ticketclear</color>", "Удаляет ВСЕ сохраненные вопросы." }
            };
        }

        #region PlayerCommands
        [ChatCommand("ticket")]
        void onCommandTicket(BasePlayer sender, string command, string[] args)
        {
            if (args.Length == 0)
                { showHelp(sender); return; }

            string msg = "";
            foreach (string arg in args)
                msg += $"{arg} ";

            newTicket(sender, $"{msg}");

            if (adminOnline())
                sendMessage(sender, $"Ваш вопрос отправлен и вскоре будет рассмотрен администратором.."); else
                sendMessage(sender, $"Ваш вопрос отправлен и будет рассмотрен администратором, когда он появится в сети..");

        }

        [ChatCommand("calladmin")]
        void onCommandCallAdmin(BasePlayer sender, string command, string[] args)
        {
            string msg = "";
            foreach (string arg in args)
                msg += $"{arg} ";

            sendToAdmins($"{col("#66ff66", sender.displayName)} попросил о помощи.\nПричина: {col("#66ff66", msg)}");

            if (adminOnline())
                sendMessage(sender, "Администратор был уведомлен о вашем запросе."); else
                sendMessage(sender, "В настоящее время нет администраторов в онлайне. Пожалуйста, оставьте вопрос <color=#ffd479>/ticket <сообщение></color>");
        }

        [ChatCommand("admins")]
        void onCommandAdmins(BasePlayer sender, string command, string[] args)
        {
            if (!adminOnline())
            { sendMessage(sender, "В настоящее время нет администраторов в онлайне."); return; }

            string msg = $"{col("#66ff66", "Админы в сети:")}\n";
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.IsAdmin)
                    msg += $"> {player.displayName}\n";
            sendMessage(sender, msg, false);
        }
        #endregion

        #region AdminCommands
        [ChatCommand("tickettp")] // Ticket teleport
        void onCommandTtp(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin)
                { sendMessage(sender, col("#cc3f3f", "Insufficient permissions.")); return; }

            int ticketNum = Int32.Parse(args[0]);
            Ticket ticket = getTicket(ticketNum);
            Teleport(sender, new Vector3(ticket.x, ticket.y, ticket.z));
            sendMessage(sender, $"Вы были телепортированы на место, откуда был задан вопрос.");
        }

        [ChatCommand("ticketlist")] // Ticket list
        void onCommandTl(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin)
            { sendMessage(sender, col("#cc3f3f", "Insufficient permissions.")); return; }

            int page = 0;
            if (args.Length > 0)
                page = Int32.Parse(args[0]);

            showTicketList(sender, page);
        }

        [ChatCommand("ticketview")] // Ticket view
        void onCommandTv(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin)
            { sendMessage(sender, col("#cc3f3f", "Insufficient permissions.")); return; }

            int ticketNum = Int32.Parse(args[0]);
            Ticket ticket = getTicket(ticketNum);
            sendMessage(sender, getTicketData(ticket), false);
        }

        [ChatCommand("ticketdel")] // Ticket delete
        void onCommandDel(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin)
            { sendMessage(sender, col("#cc3f3f", "Insufficient permissions.")); return; }

            int ticketNum = Int32.Parse(args[0]);
            Ticket ticket = getTicket(ticketNum);
            storedData.AllTickets.Remove(ticket);
            writeFile();
            sendMessage(sender, $"Вопрос <color=#ffd479>[{ticket.ticketID}] {ticket.senderName}</color> был удален.", true);
        }

        [ChatCommand("ticketclear")] // Ticket clear list
        void onCommandTcl(BasePlayer sender, string command, string[] args)
        {
            if (!sender.IsAdmin)
            { sendMessage(sender, col("#cc3f3f", "Insufficient permissions.")); return; }

            storedData.AllTickets.Clear();
            sendMessage(sender, "Вы очистили список вопросов.");
            writeFile();
        }
        #endregion

        #region Tickets
        Ticket getTicket(int id)
        {
            foreach (Ticket t in storedData.AllTickets)
                if (t.ticketID == id)
                    return t;
            return null;
        }
        void newTicket(BasePlayer player, string message)
        {
            Ticket ticket = new Ticket(player.displayName, player.userID, message, player.transform.position, DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());
            ticket.ticketID = generateUniqueID();
            storedData.AllTickets.Add(ticket);
            writeFile();
            sendToAdmins(getTicketData(ticket));
        }
        void showTicketList(BasePlayer player, int page)
        {
            string msg = $"Вопросы <color=#ffd479>{page}</color>:\n";
            int perPage = 7;
            for (int i = (perPage*page); i < storedData.AllTickets.Count; i ++)
            {
                Ticket t = storedData.AllTickets[i];
                if (i < perPage*(page+1))
                    msg += $"\n<color=#ffd479>[{t.ticketID.ToString()}]</color> -> [{col("#66ff66", t.myDateS)}] {t.senderName} : {col("#66ff66", t.message)}";
                else
                    break;
            }
            sendMessage(player, msg, false);
        } // Displays the page
        string getTicketData(Ticket t)
        {
            return $"\n<color=#ffd479>{t.senderName}</color> отправил вопрос:\n\nИмя: {col("#66ff66", t.senderName)}\nСообщение: {col("#518eef", t.message)}\nМестоположение: {col("#66ff66", t.x.ToString())}, {col("#66ff66", t.y.ToString())}, {col("#66ff66", t.z.ToString())}\nДата отправки: {col("#66ff66", t.myDate)}\nВремя отправки: {col("#66ff66", t.myTime)}\nID вопроса: {col("#66ff66", t.ticketID.ToString())}";
        } // Sends the ticket info to player
        #endregion

        #region PlayerInformation
        bool adminOnline()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.IsAdmin)
                    return true;
            return false;
        } // Checks whether there is at least 1 admin online
        void sendToAll(string msg, bool prefix = true)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                sendMessage(player, msg, prefix);
        } // Sends a message to all active users
        void sendToAdmins(string msg)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.IsAdmin)
                    sendMessage(player, msg);
        } // Sends a message to all admins
        void sendMessage(BasePlayer player, string message, bool prefix=true)
        {
            if (prefix == false)
                PrintToChat(player, $"{message}"); else
                PrintToChat(player, $"{getPrefix()} {message}");
        } // Sends a formatted message
        void showHelp(BasePlayer player)
        {
            string msg = "<size=24>AdminCall</size>\n" + $"{col("#66ff66", "<size=16>Доступные команды:</size>")}\n";
            foreach (string s in Commands.Keys)
                msg += $"{col("#4286f4", s)} - {Commands[s]}\n";

            if (player.IsAdmin)
                foreach (string s in AdminCommands.Keys)
                    msg += $"{col("#4286f4", s)} - {AdminCommands[s]}\n";

            sendMessage(player, msg, false);
        } // Shows commands list
        #endregion

        #region Data
        void writeFile() { Interface.Oxide.DataFileSystem.WriteObject(DataFile, storedData); } // Do this when changes are made
        void openDataFile() { storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(DataFile); } // Only do this once
        #endregion

        #region StringFormatting
        string col(string colour, string text)
        {
            return $"<color={colour}>{text}</color>";
        } // Adds colour to the string
        string getPrefix()
        {
            return $"<color=#5beaea>[Admin-Call]</color>";
        } // Returns formatted prefix
        #endregion

        #region Misc
        void Teleport(BasePlayer player, Vector3 position)
        {
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading", null, null, null, null, null);
            StartSleeping(player);
            player.MovePosition(position);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", position);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null) return;
            try { player.ClearEntityQueue(null); } catch { }
            player.SendFullSnapshot();
        } // teleport to position as sleeper
        void StartSleeping(BasePlayer player)
        {
            if (player.IsSleeping())
                return;
            player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
            if (!BasePlayer.sleepingPlayerList.Contains(player))
                BasePlayer.sleepingPlayerList.Add(player);
            player.CancelInvoke("InventoryUpdate");
        }
        int generateUniqueID()
        {
            int num = 1; int ran;
        Start:
            ran = UnityEngine.Random.Range(100, 999);
            num = ran;

            foreach (Ticket t in storedData.AllTickets)
                if (t.ticketID == num)
                    goto Start;

            return num;
        } // Generates a unique id for the ticket
        #endregion

        class Ticket
        {
            public string senderName;
            public ulong senderID;
            public string message;
            public string myDate;
            public string myDateS;
            public string myTime;
            public float x, y, z;
            public int ticketID;

            public Ticket(string name, ulong id, string msg, Vector3 loc, string date, string time)
            {
                senderName = name;
                senderID = id;
                message = msg;
                myDate = date;
                myTime = time;
                x = loc.x;
                y = loc.y;
                z = loc.z;
                myDateS = DateTime.Today.ToShortDateString();
            }
        }

        class StoredData
        {
            public List<Ticket> AllTickets = new List<Ticket>();
        }

    }
}
