// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("MgAlerts", "megargan", "1.0.0")]
    class MgAlerts : RustPlugin
    {
        private const bool Eng = true;
        [PluginReference] Plugin MgPanel;
        #region Configuration

        private static Configuration _config = new Configuration();
        public class Configuration
        {
            [JsonProperty(Eng?"Interval between auto-messages in seconds":"Интервал между авто-сообщениями в секундах")]
            public float Secs { get; set; } = 600f;

            [JsonProperty(Eng?"List of auto-notifications":"Список авто-оповещений")]
            public List<Message> AutoAlerts = new List<Message>();

            [JsonProperty(Eng?"List of alert presets":"Список пресетов для оповещений")]
            public Dictionary<string, Message> PresetsMessages = new Dictionary<string, Message>();
            internal class Message
            {
                [JsonProperty(Eng?"Title":"Заголовок")] public string Title;
                
                [JsonProperty(Eng?"Text":"Текст")] public string Text;

                [JsonProperty(Eng?"Command to be executed when the OK button is clicked":"Исполняемая команда при нажатии на кнопку OK")]
                public string Cmd;

                [JsonProperty(Eng?"URL that can be copied (leave blank if not required)":"URL которую можно скопировать(оставьте поле пустым если не требуется)")]
                public string Url;
            }
            
            public static Configuration GetNewConfiguration()
            {
                return new Configuration
                {
                    AutoAlerts = new List<Message>
                    {
                        new Message()
                        {
                            Title = "GRAFFITI",
                            Text = Eng?"If you text /graffiti, you can choose your spray pattern!\n Click <color=green>OK</color> and check it out!":"Если прописать /graffiti можно выбрать свой рисунок для баллончика!\n Жми <color=green>OK</color> и проверь!",
                            Cmd = "chat.say /graffiti",
                            Url = ""
                        },
                        new Message()
                        {
                            Title = "SKIN",
                            Text = Eng?"Skins are available to everyone: /skin and a sea of skins is already in front of your eyes!\n Press <color=green>OK</color> and check it out!":"Скины доступны всем: /skin  море скинов уже перед глазами!\n Жми <color=green>OK</color> и проверь!",
                            Cmd = "chat.say /skin",
                            Url = ""
                        }
                    },
                    PresetsMessages = new Dictionary<string, Message>
                    {
                        {
                            "sale", new Message
                            {
                                Title = Eng?"DISCOUNTS IN STORE!":"СКИДКИ В МАГАЗИНЕ!",
                                Text = Eng?"Come to the store, discounts will end soon!":"Заходи в магазин, скидки скоро закончится!",
                                Cmd = "chat.say /store",
                                Url = "codefling.com"
                            }
                        }
                    }
                };
            }
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) LoadDefaultConfig();
            }
            catch
            {
                PrintError(Eng?"!!!!CONFIGURATION ERROR!!!! create a new":"!!!!ОШИБКА КОНФИГУРАЦИИ!!!! создаем новую");
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }
        protected override void LoadDefaultConfig() => _config = Configuration.GetNewConfiguration();
        protected override void SaveConfig() => Config.WriteObject(_config);

        #endregion

        
        void OnServerInitialized()
        {
            if (!MgPanel)
            {
                PrintError(Eng?"MgPanel is not installed! This is not a full plugin, it's an addon for MgPanel. You can download the main plugin here - https://codefling.com/plugins/mgpanel-easy-customizable":"MgPanel не установлена! Это не полноценный плагин, это дополнение для MgPanel. Скачать основной плагин можно тут - https://foxplugins.ru/resources/mgpanel.70/");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            if(MgPanel.Version.Minor < 2)
            {
                PrintError(Eng?"The version of MgPanel you have installed does not support this add-on! Update plugin - https:foxplugins.ruresourcesmgpanel.70":"Установленная у вас версия MgPanel не поддерживает это дополнение! Обновите плагин - https://foxplugins.ru/resources/mgpanel.70/");
                Interface.Oxide.UnloadPlugin(Name);
                return;
            }
            
            permission.RegisterPermission("MgAlerts.cansendmessage", this);
            timer.Every(_config.Secs, () => AutoMessage());
        }
        

        private int AMessageNumber = 0;

        void AutoMessage()
        {
            if (_config.AutoAlerts.Count < AMessageNumber + 1) AMessageNumber = 0;
            MgPanel.Call("SendNoteMessageEveryone",
                _config.AutoAlerts[AMessageNumber].Title,
                _config.AutoAlerts[AMessageNumber].Text,
                _config.AutoAlerts[AMessageNumber].Cmd,
                _config.AutoAlerts[AMessageNumber].Url);
            AMessageNumber++;

        }
        void Reply(BasePlayer player, string message)
        {
            if (player == null)
            {
                Puts(message);
            }
            else
            {
                SendReply(player, message);
            }
        }

        #region CMD
        
        [ConsoleCommand("MgAlert")]
        void MessageSendConsole(ConsoleSystem.Arg args)
        {
            MessageSend(args.Player(), "mgalert", args.Args);
        }
        [ChatCommand("MgAlert")]
        void MessageSend(BasePlayer player, string command, string[] args)
        {
            if (player != null)
            {
               if (!permission.UserHasPermission(player.UserIDString, "MgAlerts.cansendmessage")) return; 
            }
            if (args.Length < 2)
            {
                /*Reply(player,"Неверный синтаксис!\n" +
                              "MgAlert SendPresetEveryone [presetname] - отправить всем сообщение из присетов в конфиге\n" +
                              "MgAlert SendPreset [nickname/id] [presetname]  отправить одному игроку сообщение из присетов" +
                              "MgAlert SendEveryone [text] - отправить всем обычное сообщение с текстом\n" +
                              "Mgalert Send [nickname/id] [text] - отправить сообщнеи одному игроку"); */
                Reply(player,"Invalid syntax!\n" +
                             "MgAlert SendPresetEveryone [presetname] - send a message to everyone from the presets in the config\n" +
                             "MgAlert SendPreset [nickname/id] [presetname] - send a message from the presets to one player" +
                             "MgAlert SendEveryone [text] - send a normal text message to everyone\n" +
                             "Mgalert Send [nickname/id] [text] - send a message to one player");
                return;
            }

            switch (args[0])
            {
                case "sendpreseteveryone":
                case "SendPresetEveryone":
                {
                    Configuration.Message t;
                    if (_config.PresetsMessages.TryGetValue(args[1], out t))
                    {
                        MgPanel.Call("SendNoteMessageEveryone", t.Title, t.Text, t.Cmd, t.Url);
                    }
                    else
                    {
                        Reply(player, (Eng?"Sign with this name was not found, here is the list of presets from the config:\n":"Присета с таким именем не найдено, вот скисок присетов из конфига:\n") +
                                      string.Join("\n", _config.PresetsMessages.Keys));
                    }
                    break;
                }
                case "sendpreset":
                case "SendPreset":
                {
                    BasePlayer target = BasePlayer.Find(args[1]);
                    if (target == null)
                    {
                        Reply(player, Eng?"Player not found.":"Игрок не найден.");
                        return;
                    }
                    Configuration.Message t;
                    if (_config.PresetsMessages.TryGetValue(args[1], out t))
                    {
                        MgPanel.Call("SendNoteMessage", target, t.Title, t.Text, t.Cmd, t.Url);
                    }
                    else
                    {
                        Reply(player, (Eng?"Sign with this name was not found, here is the list of presets from the config:\n":"Присета с таким именем не найдено, вот скисок присетов из конфига:\n") +
                                       string.Join("\n", _config.PresetsMessages.Keys));
                    }
                    break;
                }
                case "sendeveryone":
                case "SendEveryone":
                {
                    MgPanel.Call("SendNoteMessageEveryone", Eng?"NOTIFICATION":"ОПОВЕЩЕНИЕ",
                        string.Join(" ", args).Replace("SendEveryone ", ""));
                    break;
                }
                case "send":
                case "Send":
                {
                    BasePlayer target = BasePlayer.Find(args[1]);
                    if (target == null)
                    {
                        Reply(player, Eng?"Player not found.":"Игрок не найден.");
                        return;
                    }
                    MgPanel.Call("SendNoteMessage", target ,Eng?"NOTIFICATION":"ОПОВЕЩЕНИЕ",
                        string.Join(" ", args).Replace($"Send {args[1]}", ""));
                    break;
                }
                default:
                {
                    Reply(player,"Invalid syntax!\n" +
                                 "MgAlert SendPresetEveryone [presetname] - send a message to everyone from the presets in the config\n" +
                                 "MgAlert SendPreset [nickname/id] [presetname] - send a message from the presets to one player" +
                                 "MgAlert SendEveryone [text] - send a normal text message to everyone\n" +
                                 "Mgalert Send [nickname/id] [text] - send a message to one player");
                    /*Reply(player,"Неверный синтаксис!\n" +
                                      "MgAlert SendPresetEveryone [presetname] - отправить всем сообщение из присетов в конфиге\n" +
                                      "MgAlert SendPreset [nickname/id] [presetname]  отправить одному игроку сообщение из присетов" +
                                      "MgAlert SendEveryone [text] - отправить всем обычное сообщение с текстом\n" +
                                      "Mgalert Send [nickname/id] [text] - отправить сообщнеи одному игроку");*/
                    break;
                }
                    
            }
        }
        
        #endregion
        

    }
}