using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
//using Assets.Scripts.Core;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries;
using Oxide.Game.Hurtworld.Libraries;
using UnityEngine;
using Newtonsoft.Json;
using Oxide.Core.Plugins;


namespace Oxide.Plugins
{
    [Info("ExtTeleport", "Lizzaran", "1.0.4")]
    [Description("ExtTeleport features many standard and unique options.")]
	/* ////////
	// Update for ItemV2 by Vladimir.Kzi version is 1.0.11, plugin author Lizzaran
	// Support for this ItemV2 plugin on www.oxide-russia.ru and www.vk.com/deviantplugins 
	// Поддержка ItemV2 плагина на www.oxide-russia.ru и www.vk.com/deviantplugins 
	//////// */
    public class ExtTeleport : HurtworldPlugin
    {
        #region Variables
        private readonly Dictionary<string, List<Home>> _homes =
        new Dictionary<string, List<Home>>();
        private readonly Dictionary<string, Teleportation> _teleportations = new Dictionary<string, Teleportation>();
        private readonly List<Warp> _warps = new List<Warp>();
        private readonly List<GEvent> _events = new List<GEvent>();
        private Helpers _helpers;
        private int _rayLayer;
		private int layers;

        #endregion Variables

        #region Methods

        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            _rayLayer = LayerMask.GetMask("Terrain", "Constructions");
            _helpers = new Helpers(Config, lang, this, hurt, permission, Log)
            {
                PermissionPrefix = Regex.Replace(Title, "[^0-9a-zA-Z]+", string.Empty).ToLower()
            };

            LoadConfig();
            LoadPermissions();
            LoadData();
            LoadMessages();
        }
		
		private void OnServerInitialized()
        {
            layers = LayerMaskManager.TerrainConstructionsMachines;
        }
		
        private void LoadMessages()
        {
            #region Messages
			
			// English
			lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Prefix", "<color=lime>[Teleport]</color>"},
				{"Misc - No Permission", "You don't have the permission to use this command."},
                {"Misc - Not Enabled", "This command is not enabled."},
                {"Misc - Syntax", "Syntax: {syntax}"},
                {"Misc - Player not found", "The player can't be found."},
                {"Misc - Multiple players found", "Multiple matching players found:\n{players}"},
                {"Teleport - Teleported", "You have been teleported."},
                {"Teleport - Teleporting Soon", "Teleporting in {time} seconds."},
                {"Teleport - Reset", "The cooldowns from '{name}' have been reset."},
                {"Teleport - Reset Receiver", "Your cooldowns have been reset."},
                {"Teleport - Nothing in front", "Nothing found in front of you."},
                {"Player - Request Self", "You can't teleport to yourself."},
                {"Player - Request From Ran Out", "The teleport request from '{name}' ran out of time."},
                {"Player - Request To Ran Out", "'{name}' didn't accept your teleport request in time."},
                {"Player - Request Sent", "Teleport request sent to '{name}'."},
                {"Player - Request Got Single", "'{name}' did send you a teleport request. Accept by using /tpa."},
                {"Player - Request Got Multiple", "'{name}' did send you a teleport request. Accept by using /tpa \"{name}\""},
                {"Player - Accepted Request", "Your teleport request to '{name}' got accepted."},
                {"Player - Accepted Request Self", "You have accepted the teleport request from '{name}'."},
				{"Player - No Pending", "You don't have any pending teleport requests.}"},
                {"Player - No Pending From", "You don't have a pending teleport request from '{name}'."},
                {"Player - Already Pending", "You did already send a teleport request to '{name}'."},
                {"Player - Cooldown", "You need to wait {time} seconds before sending the next teleport request."},
                {"Home - None", "You do not have any homes yet."},
                {"Home - Set", "You have created the home '{name}'."},
                {"Home - Removed", "You have removed the home '{name}'."},
                {"Home - Exists", "The name '{name}' is already in use."},
                {"Home - List", "Homes:\n{names}"},
                {"Home - Max Reached", "You already have reached the max of {count} homes."},
                {"Home - Unknown", "The home '{name}' couldn't be found."},
                {"Home - No Stake", "There isn't any stake to which you are authorized near you."},
                {"Home - No Cell Auth", "You don't have authorization on this cell."},
                {"Home - Cooldown", "You need to wait {time} seconds before teleporting to a home again."},
                {"Home - Blocking", "You can't set a home here because something is blocking you."},
                {"Warp - Set", "You have created the warp '{name}'."},
                {"Warp - Removed", "You have removed the warp '{name}'."},
                {"Warp - Unknown", "The warp '{name}' couldn't be found."},
                {"Warp - List", "Warps:\n{names}"},
                {"Warp - Exists", "The name '{name}' is already in use."},
                {"Warp - None", "There aren't any warps yet."},
                {"Warp - Cooldown", "You need to wait {time} seconds before teleporting to a warp again."},
                {"Event - Broadcast - Set", "The event '{name}' is open now. Join by using /event \"{name}\""},
                {"Event - Broadcast - Removed", "The event '{name}' is closed now."},
                {"Event - Set", "You have created the event '{name}'."},
                {"Event - Removed", "You have removed the event '{name}'."},
                {"Event - Unknown", "The event '{name}' couldn't be found."},
                {"Event - List", "Events:\n{names}"},
                {"Event - Exists", "The name '{name}' is already in use."},
                {"Event - None", "There aren't any events yet."},
                {"Event - Cooldown", "You need to wait {time} seconds before teleporting to an event again."},
				{"Event - Inventory", "<color=red>For enter to the event, inventory must be empty!</color>"},
                {"Reason - Can not", "You can't teleport.\nReason: {reason}"},
                {"Reason - Canceled", "Teleport canceled.\nReason: {reason}"},
                {"Reason - No Stake", "There is no stake near your home."},
                {"Reason - No Cell Auth", "You don't have authorization at your home cell."},
                {"Reason - Blocking", "You can't teleport because your home is blocked."},
                {"Reason - Inside Vehicle", "Player '{name}' is inside a vehicle."},
                {"Reason - Inside Vehicle Self", "You are inside a vehicle."},
                {"Reason - Negative Effect", "Player '{name}' does have a negative effect."},
                {"Reason - Negative Effect Self", "You have a negative effect."},
                {"Reason - Infamous", "Player '{name}' is infamous."},
                {"Reason - Infamous Self", "You are infamous."},
                {"Reason - Death", "Player '{name}' is dead."},
                {"Reason - Death Self", "You are dead."},
                {"Reason - Damage", "Player '{name}' received damage."},
                {"Reason - Damage Self", "You received damage."},
                {"Reason - Health", "Player '{name}' doesn't have enough health."},
                {"Reason - Health Self", "Your health is too low."},
                {"Reason - Already Active", "You already have an active teleportation."},
				{"Reason - Entry Rock", "You tried to get into the Rock."}
            }, this, "en");
			
			// Russian
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Prefix", "<color=lime>[Телепорт]</color>"},
				{"Misc - No Permission", "<color=red>У вас нет разрешения на использование этой команды.</color>"},
                {"Misc - Not Enabled", "<color=red>Эта команда не включена.</color>"},
                {"Misc - Syntax", "Синтаксис: {syntax}"},
                {"Misc - Player not found", "<color=red>Игрок не может быть найден.</color>"},
                {"Misc - Multiple players found", "Несколько подходящих игроков найдены:\n{players}"},
                {"Teleport - Teleported", "Ты телепортирован."},
                {"Teleport - Teleporting Soon", "Телепорт через {time} секунд."},
                {"Teleport - Reset", "Время '<color=green>{name}</color>' было сброшено."},
                {"Teleport - Reset Receiver", "Ваше время было сброшено."},
                {"Teleport - Nothing in front", "Ничего не найдено перед вами."},
                {"Player - Request Self", "Вы не можете телепортироваться к себе."},
                {"Player - Request From Ran Out", "Запрос телепорт '<color=green>{name}</color>' закончилось время."},
                {"Player - Request To Ran Out", "'<color=green>{name}</color>' Не принял ваш запрос телепортацию вовремя."},
                {"Player - Request Sent", "Запрос Телепорт отправлен '<color=green>{name}</color>'."},
                {"Player - Request Got Single", "'<color=green>{name}</color>' Отправил вам запрос на телепортацию. Принять запрос: /tpa."},
                {"Player - Request Got Multiple", "'<color=green>{name}</color>' Отправил вам запрос на телепортацию. Принять запрос: /tpa \"<color=green>{name}</color>\""},
                {"Player - Accepted Request", "Ваш запрос на телепорт '<color=green>{name}</color>' поступил."},
                {"Player - Accepted Request Self", "Вы приняли запрос от телепорта '<color=green>{name}</color>'."},
                {"Player - No Pending", "У вас нет ожидающих запросов телепорта."},
                {"Player - No Pending From", "У вас нет ожидающих запросов телепорта от '<color=green>{name}</color>'."},
                {"Player - Already Pending", "Вы уже отправляли запрос к '<color=green>{name}</color>'."},
                {"Player - Cooldown", "Вы должны ждать {time} секунд прежде,чем послать следующий запрос."},
                {"Home - None", "У Вас еще нет домов."},
                {"Home - Set", "Вы создали дом '<color=green>{name}</color>'."},
                {"Home - Removed", "Вы удалили дом '<color=green>{name}</color>'."},
                {"Home - Exists", "Это имя '<color=green>{name}</color>' уже используется."},
                {"Home - List", "Дома:\n{names}"},
                {"Home - Max Reached", "Вы уже достигли макс. из {count} домов."},
                {"Home - Unknown", "Дом '<color=green>{name}</color>' не найден."},
                {"Home - No Stake", "Нет никаких тотемов рядом с вами в которых вы автоизованы."},
                {"Home - No Cell Auth", "У вас нет разрешения на эту ячейку."},
                {"Home - Cooldown", "Нужно подождать {time} секунд, прежде чем снова телепортироваться к дому."},
                {"Home - Blocking", "Ты не можеш установить дом здесь, потому что что-то блокирует Вас."},
                {"Warp - Set", "Вы создали warp '<color=green>{name}</color>'."},
                {"Warp - Removed", "Вы удалили warp '<color=green>{name}</color>'."},
                {"Warp - Unknown", "Этот warp '<color=green>{name}</color>' не может быть найден."},
                {"Warp - List", "Warps:\n{names}"},
                {"Warp - Exists", "Это имя '<color=green>{name}</color>' уже используется."},
                {"Warp - None", "Еще нет никаких warps."},
                {"Warp - Cooldown", "Нужно подождать {time} секунд, прежде чем снова телепортироваться на warp."},
                {"Event - Broadcast - Set", "Это Событие '<color=green>{name}</color>' открыто в настоящее время. Регистрация с помощью: /event \"<color=green>{name}</color>\""},
                {"Event - Broadcast - Removed", "Это Событие '<color=green>{name}</color>' закрыто в настоящее время."},
                {"Event - Set", "Вы создали событие '<color=green>{name}</color>'."},
                {"Event - Removed", "Вы удалили событие '<color=green>{name}</color>'."},
                {"Event - Unknown", "Это событие '<color=green>{name}</color>' не может быть найдено."},
                {"Event - List", "Событие:\n{names}"},
                {"Event - Exists", "Это имя '<color=green>{name}</color>' уже используется."},
                {"Event - None", "Еще нет никаких событий."},
                {"Event - Cooldown", "Вы должны ждать {time} секунды прежде, чем телепортироваться к событию снова."},
				{"Event - Inventory", "<color=red>Чтобы попасть на ивент, инвентарь должен быть пустым!</color>"},
                {"Reason - Can not", "Вы не можете телепортироваться.\nПричина: {reason}"},
                {"Reason - Canceled", "Телепорт отменен.\nПричина: {reason}"},
                {"Reason - No Stake", "<color=red>Вы должны быть авторизированы в тотеме чтобы поставить точку дома.</color>"},
                {"Reason - No Cell Auth", "<color=red>У вас нет разрешения.</color>"},
                {"Reason - Blocking", "<color=red>Вы не можете телепортироваться, потому что ваш дом заблокирован.</color>"},
                {"Reason - Inside Vehicle", "<color=red>Игрок '<color=green>{name}</color>' в транспортном средстве.</color>"},
                {"Reason - Inside Vehicle Self", "<color=red>Вы в транспортном средстве.</color>"},
                {"Reason - Negative Effect", "<color=red>Игрок '<color=green>{name}</color>' имеет отрицательный эффект.</color>"},
                {"Reason - Negative Effect Self", "<color=red>Вы имеете отрицательный эффект.</color>"},
                {"Reason - Infamous", "<color=red>Игрока '<color=green>{name}</color>' презирают.</color>"},
                {"Reason - Infamous Self", "<color=red>Вас презирают.</color>"},
                {"Reason - Death", "<color=red>Игрок '<color=green>{name}</color>' мертв.</color>"},
                {"Reason - Death Self", "<color=red>Вы мертвы.</color>"},
                {"Reason - Damage", "<color=red>Игрок '<color=green>{name}</color>' получил повреждение.</color>"},
                {"Reason - Damage Self", "<color=red>Вы получили повреждения.</color>"},
                {"Reason - Health", "<color=red>Игрок '<color=green>{name}</color>' не имеет достаточного здоровья.</color>"},
                {"Reason - Health Self", "<color=red>Ваше здоровье слишком низкое.</color>"},
                {"Reason - Already Active", "<color=red>У Вас уже есть активная телепортация.</color>"},
				{"Reason - Entry Rock", "<color=red>Вы пытались попасть в Скалу.</color>"}
            }, this, "ru");
			
			// Polish
            lang.RegisterMessages(new Dictionary<string, string>
            {
				{"Prefix", "<color=lime>[Teleport]</color>"},
				{"Misc - No Permission", "Nie posiadasz uprawnień do użycia tego!"},
				{"Misc - Not Enabled", "Ta komenda jest wyłączona!"},
				{"Misc - Syntax", "Poprawne użycie: {syntax}"},
				{"Misc - Player not found", "Nie znaleziono takiego gracza!"},
				{"Misc - Multiple players found", "Znaleziono podobnych graczy:\n{players}"},
				{"Teleport - Teleported", "Zostałeś teleportowany!"},
				{"Teleport - Teleporting Soon", "Zostaniesz teleportowany za {time} sekund."},
				{"Teleport - Reset", "Oczekiwania na teleportacje dla: {name} zostały odnowione."},
				{"Teleport - Reset Receiver", "Twoje oczekiwania na teleportacje zostały odnowione."},
				{"Teleport - Nothing in front", "Nic nie znaleziono przed Tobą."},
				{"Player - Request Self", "Nie możesz teleportować się sam do siebie! :)"},
				{"Player - Request From Ran Out", "Oczekiwanie na teleportację od: {name} zostało przedawnione."},
				{"Player - Request To Ran Out", "{name} nie zaakceptował Twojej prośby o teleportację."},
				{"Player - Request Sent", "Wysłano prośbę o teleportację do: {name}"},
				{"Player - Request Got Single", "{name} wysłał do Ciebie prośbę teleportacji. Jeśli chcesz zaakceptować, wpisz: /tpa"},
				{"Player - Request Got Multiple", "{name} wysłał do Ciebie prośbę teleportacji. Jeśli chcesz zaakceptować, wpisz /tpa \"{name}\""},
				{"Player - Accepted Request", "{name} zaakceptował Twoją prośbę o teleportację."},
				{"Player - Accepted Request Self", "Zaakceptowałeś prośbę o teleportację od: {name}"},
				{"Player - No Pending", "Nie masz żadnych oczekiwań na teleportację."},
				{"Player - No Pending From", "Nie masz oczekiwania na teleportację od: {name}"},
				{"Player - Already Pending", "Już wysłałeś prośbę o teleportację do: {name}"},
				{"Player - Cooldown", "Musisz poczekać {time} sekund przed następną prośbą o teleportację."},
				{"Home - None", "Nie posiadasz żadnego domu."},
				{"Home - Set", "Stworzyłeś dom: {name}"},
				{"Home - Removed", "Usunąłeś dom: {name}"},
				{"Home - Exists", "Twój dom o nazwie: {name} już istnieje!"},
				{"Home - List", "Domy:\n{names}"},
				{"Home - Max Reached", "Przekroczyłeś maksimum zapisanych {count} domów."},
				{"Home - Unknown", "Dom: {name} nie istnieje."},
				{"Home - No Stake", "W pobliżu Twojego domu nie ma totemu."},
				{"Home - No Cell Auth", "Nie masz autoryzacji w tym domu."},
				{"Home - Cooldown", "Musisz poczekać {time} sekund przed następnym teleportem do domu."},
				{"Home - Blocking", "Nie możesz ustawić tutaj domu ponieważ coś Ciebie blokuje"},
				{"Warp - Set", "Stworzyłeś warp: {name}"},
				{"Warp - Removed", "Usunąłeś warp: {name}"},
				{"Warp - Unknown", "Warp: {name} nie istnieje."},
				{"Warp - List", "Warpy:\n{names}"},
				{"Warp - Exists", "Warp: {name} już istnieje!"},
				{"Warp - None", "Nie ma warpów."},
				{"Warp - Cooldown", "Musisz poczekać {time} sekund przed następnym teleportem do warpa."},
				{"Event - Broadcast - Set", "Event: {name} wystartował!\nDołącz wpisując: /event \"{name}\""},
				{"Event - Broadcast - Removed", "Event: {name} jest zamknięty."},
				{"Event - Set", "Stworzyłeś event: {name}"},
				{"Event - Removed", "Usunąłeś event: {name}"},
				{"Event - Unknown", "Event: {name} nie istnieje."},
				{"Event - List", "Eventy:\n{names}"},
				{"Event - Exists", "Event: {name} już istnieje."},
				{"Event - None", "Nie ma żadnych eventów."},
				{"Event - Cooldown", "Musisz poczekać {time} sekund przed nastepnym teleportowaniem się na event!"},
				{"Event - Inventory", "<color=red>For enter to the event, inventory must be empty!</color>"},
				{"Reason - Can not", "Nie możesz się teleportowaać.\nPowód: {reason}"},
				{"Reason - Canceled", "Przerwano teleportowanie!.\nPowód: {reason}"},
				{"Reason - No Stake", "Nie ma żadnego totemu w pobliżu."},
				{"Reason - No Cell Auth", "Nie masz autoryzacji w domu."},
				{"Reason - Blocking", "Nie możesz się teleportować ponieważ Twój dom jest zablokowany."},
				{"Reason - Inside Vehicle", "{name} jest w pojeździe."},
				{"Reason - Inside Vehicle Self", "Jesteś w pojeździe."},
				{"Reason - Negative Effect", "{name} ma zły efekt na sobie."},
				{"Reason - Negative Effect Self", "Masz zły efekt na sobie."},
				{"Reason - Infamous", "{name} ma niesławność."},
				{"Reason - Infamous Self", "Masz niesławność."},
				{"Reason - Death", "{name} nie żyje."},
				{"Reason - Death Self", "Nie żyjesz."},
				{"Reason - Damage", "{name} otrzymał obrażenia."},
				{"Reason - Damage Self", "Otrzymałeś obrażenia."},
				{"Reason - Health", "{name} ma za mało zdrowia."},
				{"Reason - Health Self", "Masz na mało zdrowia."},
				{"Reason - Already Active", "Masz już aktywny teleport."},
				{"Reason - Entry Rock", "Próbowano uzyskać w skale."}
            }, this, "pl");

            #endregion Messages
        }

        protected override void LoadDefaultConfig()
        {
            Log(ELogType.Warning, "Generating new configuration file.");
        }

        private new void LoadConfig()
        {
            #region Config

            _helpers.SetConfig("Admin", "Cancel", new Dictionary<string, object>
            {
                {"Inside Vehicle", true}
            });

            _helpers.SetConfig("Player", "Enabled", true);
            _helpers.SetConfig("Player", "Cooldown", 300f);
            _helpers.SetConfig("Player", "Pending Timer", 30f);
            _helpers.SetConfig("Player", "Teleport Timer", 15f);
            _helpers.SetConfig("Player", "Minimum Health Percent", 0f);
            _helpers.SetConfig("Player", "Surrender on Teleport", false);

            _helpers.SetConfig("Player", "Cooldowns", new Dictionary<string, object>
            {
                {$"{_helpers.PermissionPrefix}.player.cooldown.mod", 60f}
            });

            _helpers.SetConfig("Player", "Cancel", new Dictionary<string, object>
            {
                {
                    "On Damage", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {"Threshold", 10f},
                        {"Cooldown", 30f}
                    }
                },
                {
                    "Negative Effect", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {
                            "Thresholds", new Dictionary<string, object>
                            {
                                {"ColdBar", 80f},
                                {"HeatBar", 80f},
                                {"Radiation", 80f},
                                {"Toxin", 80f},
                                {"Hungerbar", 99f}
                            }
                        }
                    }
                },
                {"Inside Vehicle", true}
            });

            _helpers.SetConfig("Home", "Enabled", true);
            _helpers.SetConfig("Home", "Cooldown", 300f);
            _helpers.SetConfig("Home", "Teleport Timer", 15f);
            _helpers.SetConfig("Home", "Limit", 5);
            _helpers.SetConfig("Home", "Stake Radius", 10f);
            _helpers.SetConfig("Home", "Check for Authorization", true);
            _helpers.SetConfig("Home", "Check for Blocking", true);
            _helpers.SetConfig("Home", "Surrender on Teleport", false);
            _helpers.SetConfig("Home", "Minimum Health Percent", 0f);

            _helpers.SetConfig("Home", "Limits", new Dictionary<string, object>
            {
                {$"{_helpers.PermissionPrefix}.home.limit.mod", 10}
            });

            _helpers.SetConfig("Home", "Cooldowns", new Dictionary<string, object>
            {
                {$"{_helpers.PermissionPrefix}.home.cooldown.mod", 60f}
            });

            _helpers.SetConfig("Home", "Cancel", new Dictionary<string, object>
            {
                {
                    "On Damage", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {"Threshold", 10f},
                        {"Cooldown", 30f}
                    }
                },
                {
                    "Negative Effect", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {
                            "Thresholds", new Dictionary<string, object>
                            {
                                {"ColdBar", 80f},
                                {"HeatBar", 80f},
                                {"Radiation", 80f},
                                {"Toxin", 80f},
                                {"Hungerbar", 99f}
                            }
                        }
                    }
                },
                {"Inside Vehicle", true}
            });

            _helpers.SetConfig("Warp", "Enabled", true);
            _helpers.SetConfig("Warp", "Cooldown", 1800f);
            _helpers.SetConfig("Warp", "Teleport Timer", 15f);
            _helpers.SetConfig("Warp", "Surrender on Teleport", false);
            _helpers.SetConfig("Warp", "Minimum Health Percent", 0f);

            _helpers.SetConfig("Warp", "Cooldowns", new Dictionary<string, object>
            {
                {$"{_helpers.PermissionPrefix}.warp.cooldown.mod", 60f}
            });

            _helpers.SetConfig("Warp", "Cancel", new Dictionary<string, object>
            {
                {
                    "On Damage", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {"Threshold", 10f},
                        {"Cooldown", 30f}
                    }
                },
                {
                    "Negative Effect", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {
                            "Thresholds", new Dictionary<string, object>
                            {
                                {"ColdBar", 80f},
                                {"HeatBar", 80f},
                                {"Radiation", 80f},
                                {"Toxin", 80f},
                                {"Hungerbar", 99f}
                            }
                        }
                    }
                },
                {"Inside Vehicle", true}
            });

            _helpers.SetConfig("Event", "Enabled", true);
            _helpers.SetConfig("Event", "Broadcast", true);
            _helpers.SetConfig("Event", "Cooldown", 1800f);
            _helpers.SetConfig("Event", "Teleport Timer", 15f);
            _helpers.SetConfig("Event", "Surrender on Teleport", false);
            _helpers.SetConfig("Event", "Minimum Health Percent", 0f);

            _helpers.SetConfig("Event", "Cooldowns", new Dictionary<string, object>
            {
                {$"{_helpers.PermissionPrefix}.event.cooldown.mod", 60f}
            });

            _helpers.SetConfig("Event", "Cancel", new Dictionary<string, object>
            {
                {
                    "On Damage", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {"Threshold", 10f},
                        {"Cooldown", 30f}
                    }
                },
                {
                    "Negative Effect", new Dictionary<string, object>
                    {
                        {"Enabled", false},
                        {
                            "Thresholds", new Dictionary<string, object>
                            {
                                {"ColdBar", 80f},
                                {"HeatBar", 80f},
                                {"Radiation", 80f},
                                {"Toxin", 80f},
                                {"Hungerbar", 99f}
                            }
                        }
                    }
                },
                {"Inside Vehicle", true}
            });

            _helpers.SetConfig("Event", "Player", new Dictionary<string, object>
            {
                {"Heal", false},
                {"Remove Equipment", false}
            });

            _helpers.SetConfig("Global", "Cooldown", new Dictionary<string, object>
            {
                {"On Death", 15f}
            });

            #endregion Config

            SaveConfig();
        }

        private void LoadPermissions()
        {
            _helpers.RegisterPermission("admin");
            _helpers.RegisterPermission("player");
            _helpers.RegisterPermission("home");
            _helpers.RegisterPermission("warp");
            _helpers.RegisterPermission("event");

            var defaultHome = new Dictionary<string, object> {{$"{_helpers.PermissionPrefix}.home.limit.mod", 10}};
            foreach (
                var dicPair in
                    _helpers.GetConfig(defaultHome, "Home",
                        "Limits"))
            {
                _helpers.RegisterPermission(dicPair.Key);
            }

            var teleportTypes = Enum.GetNames(typeof (ETeleportType));
            foreach (var teleportType in teleportTypes)
            {
                var defaultCooldown = new Dictionary<string, object>
                {
                    {$"{_helpers.PermissionPrefix}.{teleportType.ToLower()}.cooldown.mod", 180f}
                };
                foreach (
                    var dicPair in
                        _helpers.GetConfig(defaultCooldown, teleportType,
                            "Cooldowns"))
                {
                    _helpers.RegisterPermission(dicPair.Key);
                }
            }
        }

        private void LoadData()
        {
            var fileSystem = Interface.GetMod().DataFileSystem;
            foreach (
                var dicPair in
                    fileSystem.ReadObject<Dictionary<string, Dictionary<string, string>>>("ExtTeleport/Homes"))
            {
                var home =
                    new List<Home>(
                        dicPair.Value.Select(h => new Home(h.Key, _helpers.StringToVector3(h.Value)))
                            .Where(h => !h.Position.Equals(Vector3.zero))
                            .ToList());
                _homes.Add(dicPair.Key, home);
            }
            foreach (var warp in fileSystem.ReadObject<Dictionary<string, string>>("ExtTeleport/Warps"))
            {
                var position = _helpers.StringToVector3(warp.Value);
                if (!position.Equals(Vector3.zero))
                {
                    _warps.Add(new Warp(warp.Key, position));
                }
            }
            foreach (var gEvent in fileSystem.ReadObject<Dictionary<string, string>>("ExtTeleport/Events"))
            {
                var position = _helpers.StringToVector3(gEvent.Value);
                if (!position.Equals(Vector3.zero))
                {
                    _events.Add(new GEvent(gEvent.Key, position));
                }
            }
            SaveData();
        }

        private void SaveData()
        {
            var fileSystem = Interface.GetMod().DataFileSystem;
            fileSystem.WriteObject("ExtTeleport/Homes",
                _homes.ToDictionary(dicPair => dicPair.Key,
                    dicPair =>
                        dicPair.Value.ToDictionary(home => home.Name, home => _helpers.Vector3ToString(home.Position))));
            fileSystem.WriteObject("ExtTeleport/Warps",
                _warps.ToDictionary(warp => warp.Name, warp => _helpers.Vector3ToString(warp.Position)));
            fileSystem.WriteObject("ExtTeleport/Events",
                _events.ToDictionary(gEvent => gEvent.Name, gEvent => _helpers.Vector3ToString(gEvent.Position)));
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerRespawn(PlayerSession session)
        {
            var deathCooldown = _helpers.GetConfig(0f, "Global", "Cooldown", "On Death");
            if (deathCooldown > 0)
            {
                var playerId = _helpers.GetPlayerId(session);
                var teleportation = GetTeleportation(playerId);

                teleportation.GlobalCooldownTime =
                    new DateTime(Math.Max(teleportation.GlobalCooldownTime.Ticks,
                        DateTime.Now.AddSeconds(deathCooldown).Ticks));
            }
        }

        private bool CanTeleport(ETeleportType teleportType, PlayerSession s1, PlayerSession s2,
            Teleportation teleportation,
            Vector3 position, bool initial)
        {
            var s1Id = _helpers.GetPlayerId(s1);
            var s2Id = teleportType == ETeleportType.Player ? _helpers.GetPlayerId(s2) : s1Id;
            var reasonMessage = lang.GetMessage(initial ? "Reason - Can not" : "Reason - Canceled", this, s1Id);
            var typeString = teleportType.ToString();
			if (Interface.CallHook("canExtTeleport", s1) != null) return false;
			if (s2 != null)
			{
				if (Interface.CallHook("canExtTeleport", s2) != null) return false;
			}
			if(!_helpers.IsSafeLocation(position))
			{
				Puts("[BUG TP] "+s1.Identity.Name.ToString()+" | "+s1.SteamId.ToString()+" tried to get into the Rock");
				SendChatMessage(s1, GetMsg("Prefix"), reasonMessage.Replace("{reason}", lang.GetMessage("Reason - Entry Rock", this, s1Id)));
				//SendChatMessage(s1, GetMsg("Prefix"), "Teleport denied, You tried to Entry Rock");   //Reason - Entry Rock
				if (s2 != null)
				{
					//Puts("[BUG TP] "+s2.Identity.Name.ToString()+" | "+s2.SteamId.ToString()+" tried to get into the Rock");
					SendChatMessage(s2, GetMsg("Prefix"), reasonMessage.Replace("{reason}", lang.GetMessage("Reason - Entry Rock", this, s2Id)));
					//SendChatMessage(s2, GetMsg("Prefix"), "Teleport denied, You tried to Entry Rock");
				}
				return false;
			}
            if (_helpers.GetPlayerHealth(s1) <= 0)
            {
                SendChatMessage(s1, GetMsg("Prefix"), 
                    reasonMessage.Replace("{reason}", lang.GetMessage("Reason - Death Self", this, s1Id)));
                if (teleportType == ETeleportType.Player)
                {
                    SendChatMessage(s2, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}",
                            lang.GetMessage("Reason - Death", this, s2Id).Replace("{name}", s1.Identity.Name)));
                }
                return false;
            }
            if (teleportType == ETeleportType.Player && _helpers.GetPlayerHealth(s2) <= 0)
            {
                SendChatMessage(s1, GetMsg("Prefix"), 
                    reasonMessage.Replace("{reason}",
                        lang.GetMessage("Reason - Death", this, s1Id).Replace("{name}", s2.Identity.Name)));
                SendChatMessage(s2, GetMsg("Prefix"), 
                    reasonMessage.Replace("{reason}",
                        lang.GetMessage("Reason - Death Self", this, s2Id)));
                return false;
            }
            if (_helpers.GetPlayerHealthPercentage(s1) < _helpers.GetConfig(0f, typeString, "Minimum Health Percent"))
            {
                SendChatMessage(s1, GetMsg("Prefix"), 
                    reasonMessage.Replace("{reason}", lang.GetMessage("Reason - Health Self", this, s1Id)));
                if (teleportType == ETeleportType.Player)
                {
                    SendChatMessage(s2, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}",
                            lang.GetMessage("Reason - Health", this, s2Id).Replace("{name}", s1.Identity.Name)));
                }
            }
            if (initial)
            {
                if (teleportation.HasActive())
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}", lang.GetMessage("Reason - Already Active", this, s1Id)));
                    return false;
                }
                var tsCooldown = TimeSpan.FromTicks(
                    Math.Max(teleportation.GetLast(teleportType).AddSeconds(GetConfigCooldown(teleportType, s1)).Ticks,
                        teleportation.GlobalCooldownTime.Ticks)) - TimeSpan.FromTicks(DateTime.Now.Ticks);
                if (tsCooldown.TotalSeconds > 0)
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        lang.GetMessage($"{typeString} - Cooldown", this, s1Id)
                            .Replace("{time}", ((int) Math.Ceiling(tsCooldown.TotalSeconds)).ToString()));
                    return false;
                }
            }
            if (_helpers.GetConfig(false, typeString, "Cancel", "Negative Effect", "Enabled"))
            {
                if (DoesNegativeEffectConfigApply(teleportType, s1))
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}",
                            lang.GetMessage("Reason - Negative Effect Self", this, s1Id)));
                    if (teleportType == ETeleportType.Player)
                    {
                        SendChatMessage(s2, GetMsg("Prefix"), 
                            reasonMessage.Replace("{reason}",
                                lang.GetMessage("Reason - Negative Effect", this, s2Id)
                                    .Replace("{name}", s1.Identity.Name)));
                    }
                    return false;
                }
            }
            if (_helpers.GetConfig(true, typeString, "Cancel", "Inside Vehicle"))
            {
                if (_helpers.IsPlayerInsideVehicle(s1))
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}",
                            lang.GetMessage("Reason - Inside Vehicle Self", this, s1Id)));
                    if (teleportType == ETeleportType.Player)
                    {
                        SendChatMessage(s2, GetMsg("Prefix"), 
                            reasonMessage.Replace("{reason}",
                                lang.GetMessage("Reason - Inside Vehicle", this, s2Id)
                                    .Replace("{name}", s1.Identity.Name)));
                    }
                    return false;
                }
                if (teleportType == ETeleportType.Player && _helpers.IsPlayerInsideVehicle(s2))
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}",
                            lang.GetMessage("Reason - Inside Vehicle", this, s1Id).Replace("{name}", s2.Identity.Name)));
                    SendChatMessage(s2, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}",
                            lang.GetMessage("Reason - Inside Vehicle Self", this, s2Id)));
                    return false;
                }
            }
            if (teleportType == ETeleportType.Home && _helpers.GetConfig(true, "Home", "Check for Authorization"))
            {
                if (
                    !_helpers.HasPlayerStakeAuthorization(s1, position,
                        _helpers.GetConfig(10.0f, "Home", "Stake Radius")))
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}", lang.GetMessage("Reason - No Stake", this, s1Id)));
                    return false;
                }
                if (
                    !_helpers.HasPlayerCellAuthorization(s1, position))
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}", lang.GetMessage("Reason - No Cell Auth", this, s1Id)));
                    return false;
                }
            }
            if (teleportType == ETeleportType.Home && _helpers.GetConfig(true, "Home", "Check for Blocking"))
            {
                if (IsPositionBlocked(position))
                {
                    SendChatMessage(s1, GetMsg("Prefix"), 
                        reasonMessage.Replace("{reason}", lang.GetMessage("Reason - Blocking", this, s1Id)));
                    return false;
                }
            }
            return true;
        }

        private void Teleport(PlayerSession session, Vector3 location)
        {
            session.WorldPlayerEntity.transform.position = location;
        }

        private void Teleport(PlayerSession s1, PlayerSession s2)
        {
            s1.WorldPlayerEntity.transform.position = s2.WorldPlayerEntity.transform.position;
            _helpers.SetPlayerFacingDirection(s1, s2.WorldPlayerEntity.transform.position);
        }

        private void StartTeleportation(ETeleportType teleportType, PlayerSession s1, PlayerSession s2,
            Teleportation teleportation, Vector3 position)
        {
            if (!CanTeleport(teleportType, s1, s2, teleportation, position, true))
            {
                return;
            }
            teleportation.SetActive(teleportType, new ActiveTeleport(DateTime.Now, position));

            var s1Id = _helpers.GetPlayerId(s1);
            var s2Id = teleportType == ETeleportType.Player ? _helpers.GetPlayerId(s2) : s1Id;
            var typeString = teleportType.ToString();
            var teleportTimer = GetConfigTeleportTimer(teleportType, s1);
            var canceled = false;
            var teleported = false;

            if (teleportTimer > 1) 
            {
                timer.Repeat(1, (int) Math.Floor(teleportTimer), delegate
                {
                    if (!canceled && !teleported)
                    {
                        if (!CanTeleport(teleportType, s1, s2, teleportation, position, false))
                        {
                            canceled = true;
                        }

                        if (canceled)
                        {
                            teleportation.RemoveActive(teleportType);
                        }
                    }
                });
            }
            SendChatMessage(s1,
                GetMsg("Prefix"), lang.GetMessage("Teleport - Teleporting Soon", this, s1Id)
                    .Replace("{time}", ((int) Math.Ceiling(teleportTimer)).ToString()));
            timer.Once(teleportTimer, delegate
            {
                if (!canceled && !teleported)
                {
                    teleported = true;
                    if (CanTeleport(teleportType, s1, s2, teleportation, position, false))
                    {
                        if (s2 != null && teleportType == ETeleportType.Player)
                        {
                            Teleport(s1, s2);
                        }
                        else
                        {
                            Teleport(s1, position);
                        }
                        teleportation.SetLast(teleportType, DateTime.Now);
                        SendChatMessage(s1, GetMsg("Prefix"), lang.GetMessage("Teleport - Teleported", this, s1Id));
                    }
                    teleportation.RemoveActive(teleportType);
                }
            });
        }

        private Teleportation GetTeleportation(string playerId)
        {
            if (!_teleportations.ContainsKey(playerId))
            {
                _teleportations[playerId] = new Teleportation();
            }
            return _teleportations[playerId];
        }

        private bool AdminCheckVehicle(PlayerSession session, PlayerSession s1, PlayerSession s2)
        {
            if (_helpers.GetConfig(true, "Admin", "Cancel", "Inside Vehicle"))
            {
                if (_helpers.IsPlayerInsideVehicle(s1))
                {
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Reason - Can not", this, _helpers.GetPlayerId(session)).Replace("{reason}",
                            _helpers.SessionEquals(session, s1)
                                ? lang.GetMessage("Reason - Inside Vehicle Self", this, _helpers.GetPlayerId(session))
                                : lang.GetMessage("Reason - Inside Vehicle", this, _helpers.GetPlayerId(session))
                                    .Replace("{name}", s1.Identity.Name)));
                    return false;
                }
                if (_helpers.IsPlayerInsideVehicle(s2))
                {
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Reason - Can not", this, _helpers.GetPlayerId(session)).Replace("{reason}",
                            _helpers.SessionEquals(session, s2)
                                ? lang.GetMessage("Reason - Inside Vehicle Self", this, _helpers.GetPlayerId(session))
                                : lang.GetMessage("Reason - Inside Vehicle", this, _helpers.GetPlayerId(session))
                                    .Replace("{name}", s2.Identity.Name)));
                    return false;
                }
            }
            return true;
        }

        private bool DoesNegativeEffectConfigApply(ETeleportType teleportType, PlayerSession session)
        {
            var entityStats = session.WorldPlayerEntity.GetComponent<EntityStats>();
            if (entityStats == null)
            {
                return false;
            }
            var negativeEffects =
                _helpers.GetConfig(new Dictionary<string, object> {{string.Empty, 0}},
                    teleportType.ToString(), "Cancel", "Negative Effect", "Thresholds");

			Dictionary<EntityFluidEffectKey, IEntityFluidEffect> effects = entityStats.GetFluidEffects();

            foreach (var dicPair in negativeEffects)
            {
				foreach (KeyValuePair<EntityFluidEffectKey, IEntityFluidEffect> effect in effects)
				{
					//Log(ELogType.Error, $"{effect.Key.name.ToString().ToLower()} - {effect.Value.GetValue().ToString()} -- {dicPair.Key.ToString().ToLower()} - {dicPair.Value.ToString()}");
					if(effect.Key.name.ToString().ToLower() == dicPair.Key.ToString().ToLower())
					{
						if (effect.Value.GetValue() >= Convert.ToInt32(dicPair.Value))
						{
							return true;
						}
					}
				}
			}
            return false;
        }

        private int GetConfigHomeLimit(PlayerSession session)
        {
            var limit = _helpers.GetConfig(5, "Home", "Limit");
            var limits =
                _helpers.GetConfig(new Dictionary<string, object> {{$"{_helpers.PermissionPrefix}.home.limit.mod", 10}},
                    "Home", "Limits");
            return
                (from dicPair in limits
                    where _helpers.HasPermission(session, dicPair.Key)
                    select Convert.ToInt32(dicPair.Value))
                    .Concat(new[] {limit}).Max();
        }

        private float GetConfigTeleportTimer(ETeleportType teleportType, PlayerSession session)
        {
            var typeString = teleportType.ToString();
            var teleportTimer = _helpers.GetConfig(15f, typeString, "Teleport Timer");
            return teleportTimer;
        }

        private bool GetConfigSurrender(ETeleportType teleportType, PlayerSession session)
        {
            var typeString = teleportType.ToString();
            var surrender = _helpers.GetConfig(false, typeString, "Surrender on Teleport");
            return surrender;
        }

        private float GetConfigCooldown(ETeleportType teleportType, PlayerSession session)
        {
            var typeString = teleportType.ToString();
            var cooldown = _helpers.GetConfig(180f, typeString, "Cooldown");
            Dictionary<string, object> cooldowns;
            cooldowns =
                    _helpers.GetConfig(
                        new Dictionary<string, object>
                        {
                            {$"{_helpers.PermissionPrefix}.{typeString.ToLower()}.cooldown.mod", 90f}
                        }, typeString,
                        "Cooldowns");
            return (from dicPair in cooldowns
                where _helpers.HasPermission(session, dicPair.Key)
                select (float) Convert.ToDouble(dicPair.Value))
                .Concat(new[] {cooldown}).Min();
        }

        private bool IsPositionBlocked(Vector3 position)
        {
            RaycastHit hit;
            return
                Physics.Raycast(new Vector3(position.x, position.y + 0.25f, position.z), Vector3.up, out hit, 50,
                    _rayLayer) && hit.distance < 1.25f;
        }

        internal void Log(ELogType type, string message)
        {
            switch (type)
            {
                case ELogType.Info:
                    Puts(message);
                    break;
                case ELogType.Warning:
                    PrintWarning(message);
                    break;
                case ELogType.Error:
                    PrintError(message);
                    break;
            }
        }

        #endregion Methods

        #region Enums

        internal enum ETeleportType
        {
            Player,
            Home,
            Warp,
            Event
        }


        internal enum ELogType
        {
            Info,
            Warning,
            Error
        }

        #endregion Enums

        #region Classes

        internal class Warp
        {
            public Warp(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }

            public string Name { get; }
            public Vector3 Position { get; }
        }

        internal class GEvent
        {
            public GEvent(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }

            public string Name { get; }
            public Vector3 Position { get; }
        }

        internal class Home
        {
            public Home(string name, Vector3 position)
            {
                Name = name;
                Position = position;
            }

            public string Name { get; }
            public Vector3 Position { get; }
        }

        internal class Teleportation
        {
            private readonly Dictionary<ETeleportType, ActiveTeleport> _activeTeleports =
                new Dictionary<ETeleportType, ActiveTeleport>();

            private readonly Dictionary<ETeleportType, DateTime> _lastTeleports =
                new Dictionary<ETeleportType, DateTime>();

            private readonly List<PlayerSession> _pendingTeleports = new List<PlayerSession>();

            public Teleportation()
            {
                GlobalCooldownTime = DateTime.MinValue;
            }

            public DateTime GlobalCooldownTime { get; set; }

            public void Reset()
            {
                _lastTeleports.Clear();
                GlobalCooldownTime = DateTime.MinValue;
            }

            public void SetActive(ETeleportType teleportType, ActiveTeleport pTeleport)
            {
                _activeTeleports[teleportType] = pTeleport;
            }

            public ETeleportType? GetActiveType()
            {
                if (_activeTeleports.Any())
                {
                    return _activeTeleports.First().Key;
                }
                return null;
            }

            public ActiveTeleport GetActive(ETeleportType teleportType)
            {
                return _activeTeleports.ContainsKey(teleportType) ? _activeTeleports[teleportType] : null;
            }

            public Dictionary<ETeleportType, ActiveTeleport> GetActive()
            {
                return _activeTeleports.Any() ? _activeTeleports : null;
            }

            public void RemoveActive(ETeleportType teleportType)
            {
                _activeTeleports.Remove(teleportType);
            }

            public bool HasActive()
            {
                return _activeTeleports.Any();
            }

            public bool HasPending()
            {
                return _pendingTeleports.Any();
            }

            public int GetPendingCount()
            {
                return _pendingTeleports.Count;
            }

            public PlayerSession GetFirstPending()
            {
                return _pendingTeleports.FirstOrDefault();
            }

            public bool HasPendingPlayer(PlayerSession session)
            {
                return _pendingTeleports.Contains(session);
            }

            public void AddPending(PlayerSession session)
            {
                if (!HasPendingPlayer(session))
                {
                    _pendingTeleports.Add(session);
                }
            }

            public void RemovePending(PlayerSession session)
            {
                _pendingTeleports.Remove(session);
            }

            public DateTime GetLast(ETeleportType teleportType)
            {
                return _lastTeleports.ContainsKey(teleportType) ? _lastTeleports[teleportType] : DateTime.MinValue;
            }

            public void SetLast(ETeleportType teleportType, DateTime dateTime)
            {
                _lastTeleports[teleportType] = dateTime;
            }
        }

        internal class ActiveTeleport
        {
            public ActiveTeleport(DateTime startTime, Vector3 position)
            {
                StartTime = startTime;
                Position = position;
            }

            public DateTime StartTime { get; }
            public Vector3 Position { get; }
        }

        internal class Helpers
        {
            private readonly DynamicConfigFile _config;
            private readonly Hurtworld _hurtworld;
            private readonly Lang _language;
            private readonly Action<ELogType, string> _log;
            private readonly Permission _permission;
            private readonly HurtworldPlugin _plugin;

            public Helpers(DynamicConfigFile config, Lang lang, HurtworldPlugin plugin, Hurtworld hurt,
                Permission permission, Action<ELogType, string> log)
            {
                _config = config;
                _language = lang;
                _plugin = plugin;
                _hurtworld = hurt;
                _permission = permission;
                _log = log;
            }

            public string PermissionPrefix { get; set; }

            public bool HasPermission(PlayerSession session, params string[] paramArray)
            {
                var permission = ArrayToString(paramArray, ".");
                return _permission.UserHasPermission(GetPlayerId(session),
                    permission.StartsWith(PermissionPrefix) ? $"{permission}" : $"{PermissionPrefix}.{permission}");
            }

            public void RegisterPermission(params string[] paramArray)
            {
                var permission = ArrayToString(paramArray, ".");
                _permission.RegisterPermission(
                    permission.StartsWith(PermissionPrefix) ? $"{permission}" : $"{PermissionPrefix}.{permission}",
                    _plugin);
            }

            // Credits LaserHydra
            public void SetConfig(params object[] args)
            {
                var stringArgs = ObjectToStringArray(args.Take(args.Length - 1).ToArray());
                if (_config.Get(stringArgs) == null)
                {
                    _config.Set(args);
                }
            }

            // Credits LaserHydra
            public T GetConfig<T>(T defaultVal, params object[] args)
            {
                var stringArgs = ObjectToStringArray(args);
                if (_config.Get(stringArgs) == null)
                {
                    _log(ELogType.Error,
                        $"Couldn't read from config file: {ArrayToString(stringArgs, "/")}");
                    return defaultVal;
                }
                return (T) Convert.ChangeType(_config.Get(stringArgs.ToArray()), typeof (T));
            }

            public void SendChatMessage(PlayerSession session, string prefix, string message)
            {
                if (message != null && !message.All(char.IsWhiteSpace))
                {
					_hurtworld.SendChatMessage(session, prefix, message);
                }
            }

            public void BroadcastChatMessage(string message)
            {
                if (message != null && !message.All(char.IsWhiteSpace))
                {
                    _hurtworld.BroadcastChat(message, null);
                }
            }

            public bool EnumTryParse<T>(string value, out T result) where T : struct, IConvertible
            {
                var retValue = value != null && Enum.IsDefined(typeof (T), value);
                result = retValue
                    ? (T) Enum.Parse(typeof (T), value)
                    : default(T);
                return retValue;
            }

            public string[] ObjectToStringArray(object[] args)
            {
                return args.DefaultIfEmpty().Select(a => a.ToString()).ToArray();
            }

            public bool IsSafeLocation(Vector3 pos)
			{
				var playerCenterOffset = 1.1f; // offset from position to player center in Y-axis
				var crouchHalfHeight = .75f; // half the capsule height of crouching character
				var playerRadius = .36f;
				var capsuleBottom = pos + (playerCenterOffset-crouchHalfHeight+playerRadius)*Vector3.up;
				var capsuleTop = pos + (playerCenterOffset+crouchHalfHeight-playerRadius)*Vector3.up;

				if(Physics.CheckCapsule(capsuleBottom, capsuleTop, playerRadius, LayerMaskManager.TerrainConstructionsMachines, QueryTriggerInteraction.Ignore))
				{
					return false;
				}

				return !PhysicsHelpers.IsInRock(pos+Vector3.up*playerCenterOffset);
			}
			
			public bool IsPlayerInsideVehicle(PlayerSession session)
            {
                return session?.WorldPlayerEntity?.GetComponent<CharacterMotorSimple>()?.InsideVehicle != null;
            }

            public void HealPlayer(PlayerSession session)
            {
                var entityStats = session?.WorldPlayerEntity?.GetComponent<EntityStats>();
                if (entityStats != null)
                {
                    entityStats.GetFluidEffect(EntityFluidEffectKeyDatabase.Instance.InternalTemperature).Reset(true);
					entityStats.GetFluidEffect(EntityFluidEffectKeyDatabase.Instance.ExternalTemperature).Reset(true);
					entityStats.GetFluidEffect(EntityFluidEffectKeyDatabase.Instance.Toxin).SetValue(0f);
					entityStats.GetFluidEffect(EntityFluidEffectKeyDatabase.Instance.Health).SetValue(100f);
					Dictionary<EntityFluidEffectKey, IEntityFluidEffect> effects = entityStats.GetFluidEffects();

					foreach (KeyValuePair<EntityFluidEffectKey, IEntityFluidEffect> effect in effects)
					{
						effect.Value.Reset(true);
					}
                }
            }

            public void SetPlayerFacingDirection(PlayerSession session, Vector3 position)
            {
                session?.WorldPlayerEntity?.GetComponent<FPSInputControllerServer>()?
                    .ResetViewAngleServer(
                        Quaternion.LookRotation((position - session.WorldPlayerEntity.transform.position).normalized));
            }

            public void ClearPlayerInventory(PlayerSession session)
            {
                var pInventory = session.WorldPlayerEntity.GetComponent<PlayerInventory>();
                if (pInventory != null)
                {
					pInventory.ClearItems();
                }
                var itemManager = GlobalItemManager.Instance;
				ItemGeneratorAsset generator = Singleton<GlobalItemManager>.Instance.GetGenerators()[3];
			    itemManager.GiveItem(session.Player, generator, 0);
            }

            public float GetPlayerHealth(PlayerSession session)
            {
                return
                    session?.WorldPlayerEntity?.GetComponent<EntityStats>()?
                        .GetFluidEffect(EntityFluidEffectKeyDatabase.Instance.Health)?
                        .GetValue() ?? -1f;
            }

            public float GetPlayerHealthPercentage(PlayerSession session)
            {
                var healthStats = session?.WorldPlayerEntity?.GetComponent<EntityStats>()?
                    .GetFluidEffect(EntityFluidEffectKeyDatabase.Instance.Health);
                return healthStats?.GetValue()/healthStats?.GetMaxValue()*100 ?? 100;
            }

            public string ArrayToString(string[] array, string separator)
            {
                return string.Join(separator, array);
            }

            public bool IsValidSession(PlayerSession session)
            {
                return session != null && session.IsLoaded && !string.IsNullOrEmpty(session.Identity.Name);
            }

            public string GetPlayerId(PlayerSession session)
            {
                return session.SteamId.ToString();
            }

            public bool StringContains(string source, string toCheck, StringComparison comp)
            {
                return source.IndexOf(toCheck, comp) >= 0;
            }

            public bool SessionEquals(PlayerSession s1, PlayerSession s2)
            {
                return IsValidSession(s1) && IsValidSession(s2) && GetPlayerId(s1).Equals(GetPlayerId(s2));
            }

            public bool HasPlayerCellAuthorization(PlayerSession session, Vector3 position)
            {
                var cell = ConstructionUtilities.GetOwnershipCell(position);
                if (cell >= 0)
                {
                    OwnershipStakeServer stake;
                    ConstructionManager.Instance.OwnershipCells.TryGetValue(cell, out stake);
                    if (stake?.AuthorizedPlayers != null)
                    {
						return IsPlayerStakeAuthorized(session, stake);
                    }
                }
                return false;
            }

            public bool HasPlayerStakeAuthorization(PlayerSession session, float radius)
            {
                return
                    GetStakesInRadius(session.WorldPlayerEntity.transform.position, radius)
                        .Any(s => IsPlayerStakeAuthorized(session, s));
            }

            public bool HasPlayerStakeAuthorization(PlayerSession session, Vector3 position, float radius)
            {
                return
                    GetStakesInRadius(position, radius)
                        .Any(s => IsPlayerStakeAuthorized(session, s));
            }

            public bool CanPlayerMove(PlayerSession session)
            {
                var motor = session.WorldPlayerEntity.GetComponent<CharacterMotorSimple>();
                return motor == null || motor.CanMove;
            }

            public bool IsPlayerStakeAuthorized(PlayerSession session, OwnershipStakeServer stake)
            {
                if (!stake.IsClanTotem)
				{
					return stake.AuthorizedPlayers.Contains(session.Identity);
				}
				else
				{
					return stake.IsDoorAuthorized(session.Identity, false);
				}
            }

            public List<OwnershipStakeServer> GetStakesInRadius(Vector3 position, float radius)
            {
                return
                    Resources.FindObjectsOfTypeAll<OwnershipStakeServer>()?
                        .Where(
                            e =>
                                !e.IsDestroying && e.gameObject != null && e.gameObject.activeSelf &&
                                Vector3.Distance(e.transform.position, position) <= radius)
                        .ToList() ?? new List<OwnershipStakeServer>();
            }

            public void StartEmote(PlayerSession session, string emote)
            {
                EmoteManagerServer emoteManager = session?.WorldPlayerEntity?.GetComponent<EmoteManagerServer>();
                if (emoteManager == null)
                {
					return;
				}
				
				if (emoteManager.CurrentlyEmoting)
				{
					StopEmote(session);
				}
				
				if (!emoteManager.CurrentlyEmoting && !emoteManager.CurrentlyExiting)
				{
					string emoteName = null;
					int emoteIndex = 0;
					foreach (EmoteConfiguration data in emoteManager.Emotes.Data)
					{
						if (!data.NameKey.ToLower().Contains(emote.ToLower()))
						{
							emoteName = data.NameKey;
							break;
						}

						++emoteIndex;
					}

					if (emoteName == null)
					{
						return;
					}
					emoteManager.BeginEmoteServer(emoteIndex);
				}
            }

            public void StopEmote(PlayerSession session)
            {
                session?.WorldPlayerEntity?.GetComponent<EmoteManagerServer>()?.EndEmoteServer();
            }

            public string Vector3ToString(Vector3 v3, string separator = " ")
            {
                return $"{v3.x}{separator}{v3.y}{separator}{v3.z}";
            }

            public Vector3 StringToVector3(string v3)
            {
                var split = v3.Split(' ').Select(Convert.ToSingle).ToArray();
                return split.Length == 3 ? new Vector3(split[0], split[1], split[2]) : Vector3.zero;
            }

            public PlayerSession GetPlayerSession(PlayerSession session, string search)
            {
                var sessions = GameManager.Instance.GetSessions().Values;
                var player =
                    sessions.FirstOrDefault(
                        s => IsValidSession(s) && s.Identity.Name.Equals(search, StringComparison.OrdinalIgnoreCase));
                if (player != null)
                {
                    return player;
                }
                var players =
                    (from pSession in sessions
                        where
                            IsValidSession(pSession) &&
                            StringContains(pSession.Identity.Name, search, StringComparison.OrdinalIgnoreCase)
                        select pSession).ToList();
                switch (players.Count)
                {
                    case 0:
                        SendChatMessage(session, _language.GetMessage("Prefix", _plugin, GetPlayerId(session)), 
                            _language.GetMessage("Misc - Player not found", _plugin, GetPlayerId(session)));
                        break;
                    case 1:
                        player = players.First();
                        break;
                    default:
                        SendChatMessage(session, _language.GetMessage("Prefix", _plugin, GetPlayerId(session)), 
                            _language.GetMessage("Misc - Multiple players found", _plugin, GetPlayerId(session))
                                .Replace("{players}", ArrayToString(players.Select(p => p.Identity.Name).ToArray(), ", ")));
                        break;
                }
                return player;
            }
        }

        #endregion Classes

        #region Commands

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local

        #region General

        [ChatCommand("tp")]
        private void CommandTeleport(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "admin"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            switch (args.Length)
            {
                case 0:
                    RaycastHit rayHit;
					EntityReferenceCache entity = session.WorldPlayerEntity;
					CamData simData = entity.GetComponent<PlayerStatManager>().RefCache.PlayerCamera.SimData;
					CharacterController controller = entity.GetComponent<CharacterController>();
					Vector3 point1 = simData.FirePositionWorldSpace + controller.center + Vector3.up * -controller.height * 0.5f;
					Vector3 point2 = point1 + Vector3.up * controller.height;
					Vector3 direction = simData.FireRotationWorldSpace * Vector3.forward;
					if (!Physics.CapsuleCast(point1, point2, controller.radius, direction, out rayHit, float.MaxValue, layers))
                    {
                        SendChatMessage(session,
                            GetMsg("Prefix"), lang.GetMessage("Teleport - Nothing in front", this, _helpers.GetPlayerId(session)));
                        return;
                    }
					Vector3 safePos = simData.FirePositionWorldSpace + direction * rayHit.distance;
                    Teleport(session, safePos);
                    break;
                case 1:
                    var pSession = _helpers.GetPlayerSession(session, args.First());
                    if (pSession != null)
                    {
                        if (!AdminCheckVehicle(session, session, pSession))
                        {
                            return;
                        }
                        Teleport(session, pSession);
                        SendChatMessage(session,
                            GetMsg("Prefix"), lang.GetMessage("Teleport - Teleported", this, _helpers.GetPlayerId(session)));
                    }
                    break;
                case 2:
                    var sSession = _helpers.GetPlayerSession(session, args[0]);
                    var tSession = _helpers.GetPlayerSession(session, args[1]);
                    if (sSession != null && tSession != null)
                    {
                        if (!AdminCheckVehicle(session, sSession, tSession))
                        {
                            return;
                        }
                        Teleport(sSession, tSession);
                        SendChatMessage(sSession,
                            GetMsg("Prefix"), lang.GetMessage("Teleport - Teleported", this, _helpers.GetPlayerId(session)));
                    }
                    break;
                case 3:
                    Teleport(session,
                        new Vector3(Convert.ToSingle(args[0]), Convert.ToSingle(args[1]), Convert.ToSingle(args[2])));
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Teleport - Teleported", this, _helpers.GetPlayerId(session)));
                    break;
                default:
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                            .Replace("{syntax}", "\n/tp\n/tp <to>\n/tp <from> <to>\n/tp <x> <y> <z>"));
                    break;
            }
        }

        [ChatCommand("tpreset")]
        private void CommandTeleportReset(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "admin"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (args.Length > 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "\n/tpreset\n/tpreset <player>"));
                return;
            }
            var pSession = args.Length == 0 ? session : _helpers.GetPlayerSession(session, args.First());
            if (pSession != null)
            {
                var playerId = _helpers.GetPlayerId(pSession);
                var teleportation = GetTeleportation(playerId);
                teleportation.Reset();
                if (!_helpers.SessionEquals(session, pSession))
                {
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Teleport - Reset", this, _helpers.GetPlayerId(session))
                            .Replace("{name}", pSession.Identity.Name));
                }
                SendChatMessage(pSession,
                    GetMsg("Prefix"), lang.GetMessage("Teleport - Reset Receiver", this, _helpers.GetPlayerId(session)));
            }
        }

        [ChatCommand("tphere")]
        private void CommandTeleportHere(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "admin"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/tphere <player>"));
                return;
            }
            var pSession = _helpers.GetPlayerSession(session, args.First());
            if (pSession != null)
            {
                if (!AdminCheckVehicle(session, session, pSession))
                {
                    return;
                }
                Teleport(pSession, session);
                SendChatMessage(pSession,
                    GetMsg("Prefix"), lang.GetMessage("Teleport - Teleported", this, _helpers.GetPlayerId(session)));
            }
        }

        #endregion General

        #region Player

        [ChatCommand("tpr")]
        private void CommandPlayerRequest(PlayerSession session, string command, string[] args)
        {			
            if (Interface.CallHook("canExtTeleport", session) != null) return;
			if (!_helpers.HasPermission(session, "player"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Player", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/tpr <player>"));
                return;
            }
            var pSession = _helpers.GetPlayerSession(session, args.First());
            if (pSession != null)
            {
                var playerId = _helpers.GetPlayerId(session);
                var teleportation = GetTeleportation(playerId);
                var tPlayerId = _helpers.GetPlayerId(pSession);
                var tTeleportation = GetTeleportation(tPlayerId);
                if (_helpers.SessionEquals(session, pSession))
                {
                    SendChatMessage(pSession, GetMsg("Prefix"), lang.GetMessage("Player - Request Self", this, tPlayerId));
                    return;
                }
                if (tTeleportation.HasPendingPlayer(session))
                {
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Player - Already Pending", this, playerId)
                            .Replace("{name}", pSession.Identity.Name));
                    return;
                }
                if (!CanTeleport(ETeleportType.Player, session, pSession, teleportation,
                    pSession.WorldPlayerEntity.transform.position, true))
                {
                    return;
                }

                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Player - Request Sent", this, playerId).Replace("{name}", pSession.Identity.Name));
                SendChatMessage(pSession, GetMsg("Prefix"), 
                    tTeleportation.GetPendingCount() > 0
                        ? lang.GetMessage("Player - Request Got Multiple", this, tPlayerId)
                            .Replace("{name}", session.Identity.Name)
                        : lang.GetMessage("Player - Request Got Single", this, tPlayerId)
                            .Replace("{name}", session.Identity.Name));

                tTeleportation.AddPending(session);

                timer.Once(_helpers.GetConfig(30f, "Player", "Pending Timer"), delegate
                {
                    if (tTeleportation.HasPendingPlayer(session))
                    {
                        tTeleportation.RemovePending(session);
                        SendChatMessage(session,
                            GetMsg("Prefix"), lang.GetMessage("Player - Request To Ran Out", this, playerId)
                                .Replace("{name}", pSession.Identity.Name));
                        SendChatMessage(pSession,
                            GetMsg("Prefix"), lang.GetMessage("Player - Request From Ran Out", this, tPlayerId)
                                .Replace("{name}", session.Identity.Name));
                    }
                });
            }
        }

        [ChatCommand("tpa")]
        private void CommandPlayerAccept(PlayerSession session, string command, string[] args)
        {			
            if (Interface.CallHook("canExtTeleport", session) != null) return;
			if (!_helpers.HasPermission(session, "player"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Player", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length > 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "\n/tpa\n/tpa <player>"));
                return;
            }
            var playerId = _helpers.GetPlayerId(session);
            var teleportation = GetTeleportation(playerId);

            if (!teleportation.HasPending())
            {
                SendChatMessage(session, GetMsg("Prefix"), lang.GetMessage("Player - No Pending", this, playerId));
                return;
            }
            PlayerSession pSession;
            if (args.Length == 1)
            {
                pSession = _helpers.GetPlayerSession(session, args.First());
                if (pSession == null)
                {
                    return;
                }
            }
            else
            {
                pSession = teleportation.GetFirstPending();
            }
            if (!teleportation.HasPendingPlayer(pSession))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Player - No Pending From", this, playerId)
                        .Replace("{name}", pSession.Identity.Name));
                return;
            }
            var tPlayerId = _helpers.GetPlayerId(pSession);
            var tTeleportation = GetTeleportation(tPlayerId);
            SendChatMessage(session,
                GetMsg("Prefix"), lang.GetMessage("Player - Accepted Request Self", this, playerId)
                    .Replace("{name}", pSession.Identity.Name));
            SendChatMessage(pSession,
                GetMsg("Prefix"), lang.GetMessage("Player - Accepted Request", this, tPlayerId)
                    .Replace("{name}", session.Identity.Name));
            teleportation.RemovePending(pSession);
            StartTeleportation(ETeleportType.Player, pSession, session, tTeleportation,
                session.WorldPlayerEntity.transform.position);
        }

        #endregion Player

        #region Warp
        [ChatCommand("warp")]
        private void CommandWarp(PlayerSession session, string command, string[] args)
        {		
			if (Interface.CallHook("canExtTeleport", session) != null) return;
			if (!_helpers.HasPermission(session, "warp"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Warp", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
			if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/warp <warp>"));
                return;
            }
            var playerId = _helpers.GetPlayerId(session);
            var teleportation = GetTeleportation(playerId);
            var name = args.First();
            var warp = _warps.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (warp == null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Warp - Unknown", this, playerId).Replace("{name}", name));
                return;
            }
            StartTeleportation(ETeleportType.Warp, session, null, teleportation, warp.Position);
        }

        [ChatCommand("warps")]
        private void CommandWarpList(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "warp"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Warp", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            SendChatMessage(session, GetMsg("Prefix"), 
                _warps.Any()
                    ? lang.GetMessage("Warp - List", this, _helpers.GetPlayerId(session))
                        .Replace("{names}", _helpers.ArrayToString(_warps.Select(h => h.Name).ToArray(), ", "))
                    : lang.GetMessage("Warp - None", this, _helpers.GetPlayerId(session)));
        }

        [ChatCommand("setwarp")]
        private void CommandWarpSet(PlayerSession session, string command, string[] args)
        {
			
            if (!_helpers.HasPermission(session, "admin"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Warp", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/setwarp <warp>"));
                return;
            }
            var name = args.First();
            var existWarp = _warps.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existWarp != null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Warp - Exists", this, _helpers.GetPlayerId(session))
                        .Replace("{name}", existWarp.Name));
                return;
            }
            var warp = new Warp(name, session.WorldPlayerEntity.transform.position);
            _warps.Add(warp);
            SendChatMessage(session,
                GetMsg("Prefix"), lang.GetMessage("Warp - Set", this, _helpers.GetPlayerId(session)).Replace("{name}", warp.Name));
            SaveData();
        }

        [ChatCommand("removewarp")]
        private void CommandWarpRemove(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "admin"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Warp", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/setwarp <warp>"));
                return;
            }
            var name = args.First();
            var warp = _warps.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (warp == null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Warp - Unknown", this, _helpers.GetPlayerId(session)).Replace("{name}", name));
                return;
            }
            _warps.Remove(warp);
            SendChatMessage(session,
                GetMsg("Prefix"), lang.GetMessage("Warp - Removed", this, _helpers.GetPlayerId(session)).Replace("{name}", warp.Name));
            SaveData();
        }

        #endregion Warp

        #region Event

        [ChatCommand("event")]
        private void CommandEvent(PlayerSession session, string command, string[] args)
        {			
			if (Interface.CallHook("canExtTeleport", session) != null) return;			
            if (!_helpers.HasPermission(session, "event"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Event", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/event <event>"));
                return;
            }
            var playerId = _helpers.GetPlayerId(session);
            var teleportation = GetTeleportation(playerId);
            var name = args.First();
            var gEvent = _events.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (gEvent == null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Event - Unknown", this, playerId).Replace("{name}", name));
                return;
            }
			if (session.WorldPlayerEntity.GetComponent<PlayerInventory>().GetTotalItemCount() > 0)
			{
				hurt.SendChatMessage(session, GetMsg("Prefix"), lang.GetMessage("Event - Inventory", this, playerId));
				return;
			}
            StartTeleportation(ETeleportType.Event, session, null, teleportation, gEvent.Position);
        }

        [ChatCommand("events")]
        private void CommandEventList(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "event"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Event", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            SendChatMessage(session, GetMsg("Prefix"), 
                _events.Any()
                    ? lang.GetMessage("Event - List", this, _helpers.GetPlayerId(session))
                        .Replace("{names}", _helpers.ArrayToString(_events.Select(h => h.Name).ToArray(), ", "))
                    : lang.GetMessage("Event - None", this, _helpers.GetPlayerId(session)));
        }

        [ChatCommand("setevent")]
        private void CommandEventSet(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "admin"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Event", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/setevent <event>"));
                return;
            }
            var name = args.First();
            var existEvent = _events.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existEvent != null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Event - Exists", this, _helpers.GetPlayerId(session))
                        .Replace("{name}", existEvent.Name));
                return;
            }
            var gEvent = new GEvent(name, session.WorldPlayerEntity.transform.position);
            _events.Add(gEvent);
            SendChatMessage(session,
                GetMsg("Prefix"), lang.GetMessage("Event - Set", this, _helpers.GetPlayerId(session)).Replace("{name}", gEvent.Name));
            if (_helpers.GetConfig(true, "Event", "Broadcast"))
            {
                BroadcastChat(GetMsg("Prefix"), lang.GetMessage("Event - Broadcast - Set", this,
                    _helpers.GetPlayerId(session)).Replace("{name}", gEvent.Name));
            }
            SaveData();
        }

        [ChatCommand("removeevent")]
        private void CommandRemoveEvent(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "admin"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Event", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/setevent <event>"));
                return;
            }
            var name = args.First();
            var gEvent = _events.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (gEvent == null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Event - Unknown", this, _helpers.GetPlayerId(session)).Replace("{name}", name));
                return;
            }
            _events.Remove(gEvent);
            SendChatMessage(session,
                GetMsg("Prefix"), lang.GetMessage("Event - Removed", this, _helpers.GetPlayerId(session)).Replace("{name}", gEvent.Name));
            if (_helpers.GetConfig(true, "Event", "Broadcast"))
            {
                BroadcastChat(GetMsg("Prefix"), lang.GetMessage("Event - Broadcast - Removed", this,
                    _helpers.GetPlayerId(session)).Replace("{name}", gEvent.Name));
            }
            SaveData();
        }

        #endregion

        #region Home

        [ChatCommand("home")]
        private void CommandHome(PlayerSession session, string command, string[] args)
        {			
            if (Interface.CallHook("canExtTeleport", session) != null) return;
			if (!_helpers.HasPermission(session, "home"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Home", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/home <home>"));
                return;
            }
            var playerId = _helpers.GetPlayerId(session);
            var teleportation = GetTeleportation(playerId);
            var name = args.First();
            var homes = _homes.ContainsKey(playerId) ? _homes[playerId] : new List<Home>();
            var home = homes.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (home == null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Home - Unknown", this, playerId).Replace("{name}", name));
                return;
            }
            StartTeleportation(ETeleportType.Home, session, null, teleportation, home.Position);
        }


        [ChatCommand("homes")]
        private void CommandHomeList(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "home"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Home", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            var playerId = _helpers.GetPlayerId(session);
            var homes = _homes.ContainsKey(playerId) ? _homes[playerId] : new List<Home>();
            SendChatMessage(session, GetMsg("Prefix"), 
                homes.Any()
                    ? lang.GetMessage("Home - List", this, playerId)
                        .Replace("{names}", _helpers.ArrayToString(homes.Select(h => h.Name).ToArray(), ", "))
                    : lang.GetMessage("Home - None", this, playerId));
        } 

        [ChatCommand("sethome")]
        private void CommandHomeSet(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "home"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Home", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/sethome <home>"));
                return;
            }
            if (_helpers.GetConfig(true, "Home", "Check for Authorization"))
            {
                if (!_helpers.HasPlayerCellAuthorization(session, session.WorldPlayerEntity.transform.position))
                {
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Home - No Cell Auth", this, _helpers.GetPlayerId(session)));
                    return;
                }
                if (!_helpers.HasPlayerStakeAuthorization(session, _helpers.GetConfig(10.0f, "Home", "Stake Radius")))
                {
                    SendChatMessage(session,
                        GetMsg("Prefix"), lang.GetMessage("Home - No Stake", this, _helpers.GetPlayerId(session)));
                    return;
                }
            }
            if (_helpers.GetConfig(true, "Home", "Check for Blocking") &&
                IsPositionBlocked(session.WorldPlayerEntity.transform.position))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Home - Blocking", this, _helpers.GetPlayerId(session)));
                return;
            }
            var playerId = _helpers.GetPlayerId(session);
            var name = args.First();
            var homes = _homes.ContainsKey(playerId) ? _homes[playerId] : new List<Home>();
            var existHome = homes.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existHome != null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Home - Exists", this, playerId).Replace("{name}", existHome.Name));
                return;
            }
            var maxHomes = GetConfigHomeLimit(session);
            if (homes.Count >= maxHomes)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Home - Max Reached", this, playerId)
                        .Replace("{count}", maxHomes.ToString()));
                return;
            }
            var home = new Home(name, session.WorldPlayerEntity.transform.position);
            if (_homes.ContainsKey(playerId))
            {
                _homes[playerId].Add(home);
            }
            else
            {
                _homes[playerId] = new List<Home> {home};
            }
            SendChatMessage(session,
                GetMsg("Prefix"), lang.GetMessage("Home - Set", this, playerId).Replace("{name}", home.Name));
            SaveData();
        }

        [ChatCommand("removehome")]
        private void CommandHomeRemove(PlayerSession session, string command, string[] args)
        {
            if (!_helpers.HasPermission(session, "home"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - No Permission", this, _helpers.GetPlayerId(session)));
                return;
            }
            if (!_helpers.GetConfig(true, "Home", "Enabled"))
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Not Enabled", this, _helpers.GetPlayerId(session)));
            }
            if (args.Length != 1)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Misc - Syntax", this, _helpers.GetPlayerId(session))
                        .Replace("{syntax}", "/removehome <home>"));
                return;
            }
            var playerId = _helpers.GetPlayerId(session);
            var name = args.First();
            var homes = _homes.ContainsKey(playerId) ? _homes[playerId] : new List<Home>();
            var home = homes.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (home == null)
            {
                SendChatMessage(session,
                    GetMsg("Prefix"), lang.GetMessage("Home - Unknown", this, playerId).Replace("{name}", name));
                return;
            }
            homes.Remove(home);
            SendChatMessage(session,
                GetMsg("Prefix"), lang.GetMessage("Home - Removed", this, playerId).Replace("{name}", home.Name));
            SaveData();
        }

        #endregion Home
        // ReSharper restore UnusedParameter.Local
        // ReSharper restore UnusedMember.Local

        #endregion Commands
		
		string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID == null ? null : userID.ToString());

        void BroadcastChat(string prefix, string msg) => Server.Broadcast(msg, prefix);

        void SendChatMessage(PlayerSession player, string prefix, string msg) => hurt.SendChatMessage(player, prefix, msg);
    }
}