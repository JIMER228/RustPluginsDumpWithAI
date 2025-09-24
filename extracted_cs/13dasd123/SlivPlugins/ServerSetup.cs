using System;
using ConVar;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Time = UnityEngine.Time;

namespace Oxide.Plugins
{
    [Info("ServerSetup", "tofurahie", "1.0.7")]
    internal class ServerSetup : RustPlugin
    {
        #region Static

        private const string Layer = "UI_ServerSetup";
        private Data _data;

        #region Classes
        private class Data
        {
            public float ridablehorse = 2;
            public float wolf = 2;
            public float horse;
            public float chicken = 3;
            public float boar = 5;
            public float bear = 2;
            public bool bradley = true;
            public bool ship = true;
            public bool xmas;
            public bool halloween;
            public bool serverstability;
            public bool plane = true;
            public bool chinook = true;
            public bool patrol = true;
            public bool santa;
            public float stag = 3;
            public float polabear = 1;
            public float car = 0;
            public float minicopter = 0;
            public float scraptransporter = 0;
            public int startPvp = 0;
            public int endPvp = 24;
            public int startRaid = 0;
            public int endRaid = 24;
            public int supplyDeliveryTime = 300;
            public int codeLockTime = 600;
            public int fuelSetting = 100;
            public int respawnTime = 300;
            public float decay = 1;
            public bool globalChat = true;
            public bool vehiclesExplosive = true;
            public bool suicide = true;
            public bool patrolHelicopterDamage = true;
            public float decayUnkeep = 0.1f;
        }

        #endregion

        #endregion

        #region Data

        private void LoadData() => _data = Interface.Oxide.DataFileSystem.ExistsDatafile($"{Name}/data") ? Interface.Oxide.DataFileSystem.ReadObject<Data>($"{Name}/data") : new Data();

        private void OnServerSave() => SaveData();

        private void SaveData()
        {
            if (_data != null) Interface.Oxide.DataFileSystem.WriteObject($"{Name}/data", _data);
        }

        #endregion

        #region OxideHooks

        private void Init()
        {
            LoadData();
        }

        private void OnServerInitialized()
        {
            timer.In(5f, () =>
            {
                RidableHorse.Population = _data.ridablehorse;
                Wolf.Population = _data.wolf;
                Horse.Population = _data.horse;
                Chicken.Population = _data.chicken;
                Boar.Population = _data.boar;
                Bear.Population = _data.bear;
                Bradley.enabled = _data.bradley;
                CargoShip.event_enabled = _data.ship;
                XMas.enabled = _data.xmas;
                Halloween.enabled = _data.halloween;
                Stag.Population = _data.stag;
                Polarbear.Population = _data.polabear;
                ModularCar.population = _data.car;
                Minicopter.population = _data.minicopter;
                ScrapTransportHelicopter.population = _data.scraptransporter;
                Server.Command($"decay.scale {_data.decay}");
                Server.Command($"server.globalchat {_data.globalChat}");
                Server.Command($"decay.upkeep_inside_decay_scale {_data.decayUnkeep}");
            });
        }

        private void Unload()
        {
            SaveData();
            foreach (var check in BasePlayer.activePlayerList) CuiHelper.DestroyUi(check, Layer + ".bg");
        }
        
        private void OnCrateHack(HackableLockedCrate crate)
        {
            NextTick(() =>
            {
                if (crate == null) return;
                crate.CancelInvoke(crate.HackProgress);
                var time = _data.codeLockTime / 900f;
                crate.InvokeRepeating(crate.HackProgress, time, time);
            });
        }

        private void OnEntitySpawned(CargoPlane plane)
        {
            NextTick(() =>
            {
                if (plane == null) return;
                if (!_data.plane)
                {
                    plane.Kill();
                    return;
                }
                plane.secondsToTake = _data.supplyDeliveryTime;
                plane.SendNetworkUpdateImmediate();
            });
        }
        
        private void OnPlayerRespawn(BasePlayer player, SleepingBag bag) => NextTick(() => { if (player == null || bag == null) return; Vector3 vector; Quaternion rotation; bag.GetSpawnPos(out vector, out rotation); foreach (var sleepingBag3 in SleepingBag.FindForPlayer(player.userID, true)) if (Vector3.Distance(vector, sleepingBag3.transform.position) <= ConVar.Server.respawnresetrange) sleepingBag3.SetUnlockTime(Time.realtimeSinceStartup + _data.respawnTime); });

        private object OnEntityTakeDamage(BasePlayer player, HitInfo info)
        {
            if (player == null) return null;
            if (InitiatorIsPlayer(info))
            {
                if (info.InitiatorPlayer.userID == player.userID)
                {
                    if (_data.suicide) return null;
                    return false;
                }

                if (IsRightTime(_data.startPvp, _data.endPvp)) return null;
                return false;
            }

            if (InitiatorIsHelicopter(info) && !_data.patrolHelicopterDamage) return false;
            return null;
        }
        
        private object OnEntityTakeDamage(BuildingPrivlidge block, HitInfo info)
        {
            if (block == null) return null;
            if (InitiatorIsPlayer(info))
            {
                if (IsRightTime(_data.startRaid, _data.endRaid)) return null;
                return false;
            }

            if (InitiatorIsHelicopter(info) && !_data.patrolHelicopterDamage) return false;
            return null;
        }
        
        private object OnEntityTakeDamage(Door block, HitInfo info)
        {
            if (block == null) return null;
            if (InitiatorIsPlayer(info))
            {
                if (IsRightTime(_data.startRaid, _data.endRaid)) return null;
                return false;
            }

            if (InitiatorIsHelicopter(info) && !_data.patrolHelicopterDamage) return false;
            return null;
        }
        
        private object OnEntityTakeDamage(IOEntity block, HitInfo info)
        {
            if (block == null) return null;
            if (InitiatorIsPlayer(info))
            {
                if (IsRightTime(_data.startRaid, _data.endRaid)) return null;
                return false;
            }

            if (InitiatorIsHelicopter(info) && !_data.patrolHelicopterDamage) return false;
            return null;
        }
        
        private object OnEntityTakeDamage(SimpleBuildingBlock block, HitInfo info)
        {
            if (block == null) return null;
            if (InitiatorIsPlayer(info))
            {
                if (IsRightTime(_data.startRaid, _data.endRaid)) return null;
                return false;
            }

            if (InitiatorIsHelicopter(info) && !_data.patrolHelicopterDamage) return false;
            return null;
        }

        private object OnEntityTakeDamage(BuildingBlock block, HitInfo info)
        {
            if (block == null) return null;
            if (InitiatorIsPlayer(info))
            {
                if (IsRightTime(_data.startRaid, _data.endRaid)) return null;
                return false;
            }

            if (InitiatorIsHelicopter(info) && !_data.patrolHelicopterDamage) return false;
            return null;
        }

        private void OnEntitySpawned(Minicopter copter)
        {
            NextTick(() =>
            {
                if (copter == null) return;
                var inv = copter.GetFuelSystem().GetFuelContainer().inventory.itemList;
                if (inv.Count < 1) return;
                var fuel = inv[0];
                var needFuel = _data.fuelSetting;
                if (needFuel == 0) fuel.DoRemove();
                else fuel.amount = needFuel;
            });
        }
        
        private void OnEntitySpawned(SantaSleigh entity)
        {
            if (_data.santa || entity == null) return;
            entity.Kill();
        }
        
        private void OnEntitySpawned(BaseHelicopter entity)
        {
            if (entity == null) return;
            if (_data.patrol || !entity.GetComponent<PatrolHelicopterAI>()) return;
            entity.Kill();
        }
        
        private void OnEntitySpawned(CH47Helicopter entity)
        {
            if (_data.chinook || entity == null) return;
            entity.Kill();
        }
        
        private void OnEntitySpawned(StabilityEntity entity)
        {
            if (!_data.serverstability || entity == null) return;
            entity.grounded = true;
        }
        #endregion

        #region Commands

        [ChatCommand("ss")]
        private void cmdChatmenu(BasePlayer player, string command, string[] args)
        {
            if (!player.IsAdmin) return;
            ShowUIMain(player);
            ShowUISettings(player);
        }

        [ConsoleCommand("UI_SERVERSETTINGS")]
        private void cmdConsoleUI_SERVERSETTINGS(ConsoleSystem.Arg arg)
        {
            if (arg?.Args == null) return;
            switch (arg.Args[0])
            {
                case "OPENSETTINGS":
                    ShowUISettings(arg.Player());
                    return;
                case "OPENSETTINGS2":
                    ShowUIPanel(arg.Player());
                    return;
                case "rh":
                    if (arg.Args[1] == "+") RidableHorse.Population++;
                    else RidableHorse.Population--;
                    _data.ridablehorse = RidableHorse.Population;
                    break; 
                case "wf":
                    if (arg.Args[1] == "+") Wolf.Population++;
                    else Wolf.Population--;
                    _data.wolf = Wolf.Population;
                    break;
                case "hr":
                    if (arg.Args[1] == "+") Horse.Population++;
                    else Horse.Population--;
                    _data.horse = Horse.Population;
                    break;
                case "pb":
                    if (arg.Args[1] == "+") Polarbear.Population++;
                    else Polarbear.Population--;
                    _data.polabear = Polarbear.Population;
                    break;
                case "ck":
                    if (arg.Args[1] == "+") Chicken.Population++;
                    else Chicken.Population--;
                    _data.chicken = Chicken.Population;
                    break;
                case "mc":
                    if (arg.Args[1] == "+") ModularCar.population++;
                    else ModularCar.population--;
                    _data.car = ModularCar.population;
                    break;
                case "mp":
                    if (arg.Args[1] == "+") Minicopter.population++;
                    else Minicopter.population--;
                    _data.minicopter = Minicopter.population;
                    break;
                case "st":
                    if (arg.Args[1] == "+") ScrapTransportHelicopter.population++;
                    else ScrapTransportHelicopter.population--;
                    _data.scraptransporter = ScrapTransportHelicopter.population;
                    break;
                case "bo":
                    if (arg.Args[1] == "+") Boar.Population++;
                    else Boar.Population--;
                    _data.boar = Boar.Population;
                    break;
                case "be":
                    if (arg.Args[1] == "+") Bear.Population++;
                    else Bear.Population--;
                    _data.bear = Bear.Population;
                    break;
                case "stag":
                    if (arg.Args[1] == "+") Stag.Population++;
                    else Stag.Population--;
                    _data.bear = Stag.Population;
                    break;
                case "bradley":
                    Bradley.enabled = !Bradley.enabled;
                    _data.bradley = Bradley.enabled;
                    break;
                case "cargoplane":
                    _data.plane = !_data.plane;
                    break;
                case "cargoship":
                    CargoShip.event_enabled = !CargoShip.event_enabled;
                    _data.ship = CargoShip.event_enabled;
                    break;
                case "chinook":
                    _data.chinook = !_data.chinook;
                    break;
                case "helicopter":
                    _data.patrol = !_data.patrol;
                    break;
                case "santasleigh":
                    _data.santa = !_data.santa;
                    break;
                case "christmas":
                    XMas.enabled = !XMas.enabled;
                    _data.xmas = XMas.enabled;
                    break;
                case "halloween":
                    Halloween.enabled = !Halloween.enabled;
                    _data.halloween = Halloween.enabled;
                    break;
                case "serverstability":
                    _data.serverstability = !_data.serverstability;
                    break;
                
            }
            ShowUISettings(arg.Player());
        }
        
        [ConsoleCommand("UI_SERVERSESTUP")]
        private void cmdConsole(ConsoleSystem.Arg arg)
        {
            if (arg?.Args == null || arg.Args.Length < 1) return;
            var player = arg.Player();
            switch (arg.GetString(0))
            {
                case "pvp":
                    if (arg.Args.Length < 3) return;
                    if (arg.GetString(1) == "from") _data.startPvp = arg.GetInt(2);
                    else _data.endPvp = arg.GetInt(2);
                    break;
                case "raid":
                    if (arg.Args.Length < 3) return;
                    if (arg.GetString(1) == "from") _data.startRaid = arg.GetInt(2);
                    else _data.endRaid = arg.GetInt(2);
                    break;
                case "sdt":
                    if (arg.Args.Length < 2) return;
                    _data.supplyDeliveryTime = arg.GetInt(1);
                    break;
                case "clt":
                    if (arg.Args.Length < 2) return;
                    _data.codeLockTime = arg.GetInt(1);
                    break;
                case "fs":
                    if (arg.Args.Length < 2) return;
                    _data.fuelSetting = arg.GetInt(1);
                    break;
                case "rt":
                    if (arg.Args.Length < 2) return;
                    _data.respawnTime = arg.GetInt(1);
                    break;
                case "decay":
                    if (arg.Args.Length < 2) return;
                    _data.decay = arg.GetFloat(1);
                    Server.Command($"decay.scale {_data.decay}");
                    break; 
                case "decayunkeep":
                    if (arg.Args.Length < 2) return;
                    _data.decayUnkeep = arg.GetFloat(1);
                    Server.Command($"decay.upkeep_inside_decay_scale {_data.decayUnkeep}");
                    break;
                case "gc":
                    _data.globalChat = !_data.globalChat;
                    Server.Command($"server.globalchat {_data.globalChat}");
                    break;
                case "vd":
                    _data.vehiclesExplosive = !_data.vehiclesExplosive;
                    break;
                case "ps":
                    _data.suicide = !_data.suicide;
                    break;
                case "phd":
                    _data.patrolHelicopterDamage = !_data.patrolHelicopterDamage;
                    break;
            }
            ShowUIPanel(player);
        }

        #endregion

        #region Functional

        private bool IsRightTime(int startTime, int endTime) => DateTime.UtcNow.Hour >= startTime && DateTime.UtcNow.Hour < endTime;

        private bool InitiatorIsPlayer(HitInfo info) => info?.InitiatorPlayer != null && info.InitiatorPlayer.userID.IsSteamId();
        
        private bool InitiatorIsHelicopter(HitInfo hitInfo)
        {
            if (hitInfo.Initiator is BaseHelicopter || (hitInfo.Initiator != null && (hitInfo.Initiator.ShortPrefabName.Equals("oilfireballsmall") || hitInfo.Initiator.ShortPrefabName.Equals("napalm"))))
            {
                return true;
            }
            return hitInfo.WeaponPrefab != null && (hitInfo.WeaponPrefab.ShortPrefabName.Equals("rocket_heli") || hitInfo.WeaponPrefab.ShortPrefabName.Equals("rocket_heli_napalm"));
        }


        #endregion

        #region UI
        
        private void ShowUIMain(BasePlayer player)
        {
            var container = new CuiElementContainer();

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Image = {Color = "0 0 0 0.95", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"}
            }, "Overlay", Layer + ".bg");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.948 0.907", AnchorMax = "0.99 0.98"},
                Button = {Color = "0 0 0 0", Close = Layer + ".bg"},
                Text =
                {
                    Text = "×", Font = "robotocondensed-regular.ttf", FontSize = 46, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer + ".bg", Layer + ".buttonClose");
            Outline(ref container, Layer + ".buttonClose");

            CuiHelper.DestroyUi(player, Layer + ".bg");
            CuiHelper.AddUi(player, container);
        }

        private void ShowUIPanel(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var posY = -75;
            var height = 25;
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.125 0.15", AnchorMax = "0.875 0.85"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".bg", Layer);
            Outline(ref container, Layer, "1 1 1 1", "2");

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.01 0.907", AnchorMax = "0.052 0.98"},
                Button = {Color = "0 0 0 0", Close = Layer + ".next", Command = "UI_SERVERSETTINGS OPENSETTINGS"},
                Text =
                {
                    Text = "<-", Font = "robotocondensed-regular.ttf", FontSize = 46, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer + ".bg", Layer + ".next");
            Outline(ref container, Layer + ".next");

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0.92", AnchorMax = "1 1"},
                Text =
                {
                    Text = "SERVER SETUP", Font = "robotocondensed-bold.ttf", FontSize = 25,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0.92", AnchorMax = "1 0.92", OffsetMin = "0 0", OffsetMax = "0 2"},
                Image = {Color = "1 1 1 1"}
            }, Layer);

            #region Column - 1

            #region Row - 1
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "PvP Time(UTC): ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.75 1", AnchorMax = "0.8 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "from", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.8 1", AnchorMax = "0.875 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".inputS");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.startPvp}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".inputS");
            container.Add(new CuiElement
            {
                Parent = Layer + ".inputS",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 2, FontSize = 16,
                        Command = "UI_SERVERSESTUP pvp from"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.875 1", AnchorMax = "0.9 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "to", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".inputE");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.endPvp}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".inputE");
            container.Add(new CuiElement
            {
                Parent = Layer + ".inputE",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 2, FontSize = 16,
                        Command = "UI_SERVERSESTUP pvp to"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            #endregion
            posY -= height + 5;
            
            #region Row - 2
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Raid Time(UTC): ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.75 1", AnchorMax = "0.8 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "from", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.8 1", AnchorMax = "0.875 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".inputS");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.startRaid}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".inputS");
            container.Add(new CuiElement
            {
                Parent = Layer + ".inputS",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 2, FontSize = 16,
                        Command = "UI_SERVERSESTUP raid from"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.875 1", AnchorMax = "0.9 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "to", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".inputE");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.endRaid}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".inputE");
            container.Add(new CuiElement
            {
                Parent = Layer + ".inputE",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 2, FontSize = 16,
                        Command = "UI_SERVERSESTUP raid to"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            #endregion
            posY -= height + 5;

            #region Row - 4
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Supply delivery time: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".input");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.supplyDeliveryTime}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 4, FontSize = 16,
                        Command = "UI_SERVERSESTUP sdt"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            #endregion
            posY -= height + 5;
            
            #region Row - 5
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Codelockcrate Time: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".input");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.codeLockTime}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 4, FontSize = 16,
                        Command = "UI_SERVERSESTUP clt"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            #endregion
            posY -= height + 5;

            #region Row - 6
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Fuel Setting(purchase minicopter and scraptransporter):  ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".input");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.fuelSetting}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 4, FontSize = 16,
                        Command = "UI_SERVERSESTUP fs"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            
            #endregion

            posY -= height + 5;
            
            #region Row - 7
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Player's sleeping bag cooldown: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".input");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.respawnTime}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 4, FontSize = 16,
                        Command = "UI_SERVERSESTUP rt"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            
            #endregion

            posY -= height + 5;

            #region Row - 8
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Decay: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".input");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.decay}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 5, FontSize = 16,
                        Command = "UI_SERVERSESTUP decay"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            #endregion

            posY -= height + 5;
            
            #region Row - 9
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Unkeep inside decay scale: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer, Layer + ".input");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1"},
                Text =
                {
                    Text = $"{_data.decayUnkeep}", Font = "robotocondensed-regular.ttf", FontSize = 14, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 0.35"
                }
            }, Layer + ".input");
            container.Add(new CuiElement
            {
                Parent = Layer + ".input",
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        Align = TextAnchor.MiddleCenter, CharsLimit = 5, FontSize = 16,
                        Command = "UI_SERVERSESTUP decayunkeep"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            #endregion

            posY -= height + 5;
            
            #region Row - 10
            container.Add(new CuiLabel 
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Global Chat: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSESTUP gc"},
                Text =
                {
                    Text = _data.globalChat ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = _data.globalChat ? "0 1 0 1" : "1 0 0 1"
                }
            }, Layer);
            #endregion
            
            posY -= height + 5;
            
            #region Row - 11
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Player Suicide: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSESTUP ps"},
                Text =
                {
                    Text = _data.suicide ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = _data.suicide ? "0 1 0 1" : "1 0 0 1"
                }
            }, Layer);
            #endregion

            posY -= height + 5;
                
            #region Row - 12
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Text =
                {
                    Text = "Patrol Helicopter Damage: ", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.9 1", AnchorMax = "0.975 1", OffsetMin = $"0 {posY}", OffsetMax = $"0 {posY + height}"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSESTUP phd"},
                Text =
                {
                    Text = _data.patrolHelicopterDamage ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = _data.patrolHelicopterDamage ? "0 1 0 1" : "1 0 0 1"
                }
            }, Layer);
            #endregion
            #endregion

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        
        private void ShowUISettings(BasePlayer player)
        {
            var container = new CuiElementContainer();
            var bradley = Bradley.enabled;
            var plane = _data.plane;
            var cargo = CargoShip.event_enabled;
            var chinook = _data.chinook;
            var helicopter = _data.patrol;
            var santa = _data.santa;
            var xmax = XMas.enabled;
            var halloween = Halloween.enabled;
            var stability = _data.serverstability;
            
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0.125 0.15", AnchorMax = "0.875 0.85"},
                Image = {Color = "0 0 0 0.8"}
            }, Layer + ".bg", Layer);
            Outline(ref container, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.01 0.907", AnchorMax = "0.052 0.98"},
                Button = {Color = "0 0 0 0", Close = Layer + ".next", Command = "UI_SERVERSETTINGS OPENSETTINGS2"},
                Text =
                {
                    Text = "->", Font = "robotocondensed-regular.ttf", FontSize = 46, Align = TextAnchor.MiddleCenter,
                    Color = "0.56 0.58 0.64 1.00"
                }
            }, Layer + ".bg", Layer + ".next");
            Outline(ref container, Layer + ".next");
            
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0 0.92", AnchorMax = "1 1"},
                Text =
                {
                    Text = "SERVER SETUP", Font = "robotocondensed-bold.ttf", FontSize = 25,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0.92", AnchorMax = "1 0.92", OffsetMin = "0 0", OffsetMax = "0 2"},
                Image = {Color = "1 1 1 1"}
            }, Layer);

            #region Column - 1

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.85", AnchorMax = "0.325 0.91"},
                Text =
                {
                    Text = "POPULATION", Font = "robotocondensed-bold.ttf", FontSize = 20,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0.025 0.86", AnchorMax = "0.325 0.86", OffsetMin = "0 -3", OffsetMax = "0 -1"},
                Image = {Color = "1 1 1 1"}
            }, Layer);

            #region C1 Row - 1

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.79", AnchorMax = "0.25 0.84"},
                Text =
                {
                    Text = "Ridablehorse", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.79", AnchorMax = "0.275 0.84"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS rh -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.79", AnchorMax = "0.3 0.84"},
                Text =
                {
                    Text = $"{(int)RidableHorse.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.79", AnchorMax = "0.325 0.84"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS rh +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            #endregion

            #region C1 Row - 2

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.72", AnchorMax = "0.25 0.77"},
                Text =
                {
                    Text = "Wolf", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.72", AnchorMax = "0.275 0.77"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS wf -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.72", AnchorMax = "0.3 0.77"},
                Text =
                {
                    Text = $"{(int)Wolf.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.72", AnchorMax = "0.325 0.77"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS wf +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            #endregion

            #region C1 Row - 3

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.65", AnchorMax = "0.25 0.7"},
                Text =
                {
                    Text = "Horse", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.65", AnchorMax = "0.275 0.7"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS hr -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.65", AnchorMax = "0.3 0.7"},
                Text =
                {
                    Text = $"{(int)Horse.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.65", AnchorMax = "0.325 0.7"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS hr +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            #endregion

            #region C1 Row - 4

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.58", AnchorMax = "0.25 0.63"},
                Text =
                {
                    Text = "Chicken", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.58", AnchorMax = "0.275 0.63"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS ck -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.58", AnchorMax = "0.3 0.63"},
                Text =
                {
                    Text = $"{(int)Chicken.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.58", AnchorMax = "0.325 0.63"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS ck +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            #endregion

            #region C1 Row - 5

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.51", AnchorMax = "0.25 0.56"},
                Text =
                {
                    Text = "Boar", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.51", AnchorMax = "0.275 0.56"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS bo -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.51", AnchorMax = "0.3 0.56"},
                Text =
                {
                    Text = $"{(int)Boar.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.51", AnchorMax = "0.325 0.56"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS bo +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            #endregion

            #region C1 Row - 6

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.44", AnchorMax = "0.25 0.49"},
                Text =
                {
                    Text = "Bear", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.44", AnchorMax = "0.275 0.49"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS be -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.44", AnchorMax = "0.3 0.49"},
                Text =
                {
                    Text = $"{(int)Bear.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.44", AnchorMax = "0.325 0.49"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS be +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            
            #endregion

            #region C1 Row - 7
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.37", AnchorMax = "0.25 0.42"},
                Text =
                {
                    Text = "Stag", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.37", AnchorMax = "0.275 0.42"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS stag -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.37", AnchorMax = "0.3 0.42"},
                Text =
                {
                    Text = $"{(int)Stag.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.37", AnchorMax = "0.325 0.42"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS stag +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            #endregion

            #region C1 Row - 8
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.3", AnchorMax = "0.25 0.35"},
                Text =
                {
                    Text = "Polarbear", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.3", AnchorMax = "0.275 0.35"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS pb -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.3", AnchorMax = "0.3 0.35"},
                Text =
                {
                    Text = $"{(int)Polarbear.Population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.3", AnchorMax = "0.325 0.35"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS pb +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            #endregion
            
            #region C1 Row - 9
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.23", AnchorMax = "0.25 0.28"},
                Text =
                {
                    Text = "ModularCar", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.23", AnchorMax = "0.275 0.28"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS mc -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.23", AnchorMax = "0.3 0.28"},
                Text =
                {
                    Text = $"{(int)ModularCar.population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.23", AnchorMax = "0.325 0.28"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS mc +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            #endregion
            
            #region C1 Row - 10
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.16", AnchorMax = "0.25 0.21"},
                Text =
                {
                    Text = "MiniCopter", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.16", AnchorMax = "0.275 0.21"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS mp -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.16", AnchorMax = "0.3 0.21"},
                Text =
                {
                    Text = $"{(int)Minicopter.population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.16", AnchorMax = "0.325 0.21"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS mp +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            #endregion
            
            #region C1 Row - 11
            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.025 0.09", AnchorMax = "0.25 0.14"},
                Text =
                {
                    Text = "ScrapTransportHelicopter", Font = "robotocondensed-regular.ttf", FontSize = 18, Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.25 0.09", AnchorMax = "0.275 0.14"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS st -"},
                Text =
                {
                    Text = "<", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.275 0.09", AnchorMax = "0.3 0.14"},
                Text =
                {
                    Text = $"{(int)ScrapTransportHelicopter.population}", Font = "robotocondensed-regular.ttf", FontSize = 16, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.3 0.09", AnchorMax = "0.325 0.14"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS st +"},
                Text =
                {
                    Text = ">", Font = "robotocondensed-bold.ttf", FontSize = 18, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);
            #endregion

            
            #endregion

            #region Column - 2

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.85", AnchorMax = "0.65 0.91"},
                Text =
                {
                    Text = "EVENTS", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0.35 0.86", AnchorMax = "0.65 0.86", OffsetMin = "0 -3", OffsetMax = "0 -1"},
                Image = {Color = "1 1 1 1"}
            }, Layer);

            #region C2 Row - 1

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.78", AnchorMax = "0.615 0.84"},
                Text =
                {
                    Text = "Bradley", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.78", AnchorMax = "0.65 0.84"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS bradley"},
                Text =
                {
                    Text = bradley ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = bradley ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #region C2 Row - 2

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.7", AnchorMax = "0.615 0.76"},
                Text =
                {
                    Text = "CargoPlane", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.7", AnchorMax = "0.65 0.76"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS cargoplane"},
                Text =
                {
                    Text = plane ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = plane ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #region C2 Row - 3

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.62", AnchorMax = "0.615 0.68"},
                Text =
                {
                    Text = "CargoShip", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.62", AnchorMax = "0.65 0.68"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS cargoship"},
                Text =
                {
                    Text = cargo ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = cargo ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #region C2 Row - 4

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.54", AnchorMax = "0.615 0.6"},
                Text =
                {
                    Text = "Chinook", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.54", AnchorMax = "0.65 0.6"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS chinook"},
                Text =
                {
                    Text = chinook ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = chinook ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #region C2 Row - 5

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.46", AnchorMax = "0.615 0.52"},
                Text =
                {
                    Text = "Helicopter", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.46", AnchorMax = "0.65 0.52"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS helicopter"},
                Text =
                {
                    Text = helicopter ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = helicopter ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #region C2 Row - 6

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.38", AnchorMax = "0.615 0.44"},
                Text =
                {
                    Text = "SantaSleigh", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.38", AnchorMax = "0.65 0.44"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS santasleigh"},
                Text =
                {
                    Text = santa ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = santa ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #region C2 Row - 7

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.3", AnchorMax = "0.615 0.36"},
                Text =
                {
                    Text = "Christmas", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.3", AnchorMax = "0.65 0.36"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS christmas"},
                Text =
                {
                    Text = xmax ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = xmax ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #region C2 Row - 8

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.35 0.22", AnchorMax = "0.615 0.28"},
                Text =
                {
                    Text = "Halloween", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.615 0.22", AnchorMax = "0.65 0.28"},
                Button = {Color = "0 0 0 0", Command = "UI_SERVERSETTINGS halloween"},
                Text =
                {
                    Text = halloween ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = halloween ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #endregion

            #region Column - 3

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.675 0.85", AnchorMax = "0.975 0.91"},
                Text =
                {
                    Text = "OTHER", Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.MiddleCenter,
                    Color = "1 1 1 1"
                }
            }, Layer);

            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0.675 0.86", AnchorMax = "0.975 0.86", OffsetMin = "0 -3", OffsetMax = "0 -1"},
                Image = {Color = "1 1 1 1"}
            }, Layer);

            #region C3 Row - 1

            container.Add(new CuiLabel
            {
                RectTransform = {AnchorMin = "0.675 0.78", AnchorMax = "0.94 0.84"},
                Text =
                {
                    Text = "Server Stability", Font = "robotocondensed-regular.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleLeft,
                    Color = "1 1 1 1"
                }
            }, Layer);
            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0.94 0.78", AnchorMax = "0.975 0.84"},
                Button = {Color = "0 0 0 0",Command = "UI_SERVERSETTINGS serverstability"},
                Text =
                {
                    Text = stability ? "ON" : "OFF", Font = "robotocondensed-bold.ttf", FontSize = 18,
                    Align = TextAnchor.MiddleCenter,
                    Color = stability ? "0.00 1.00 0.00 1.00" : "1.00 0.00 0.00 1.00"
                }
            }, Layer);

            #endregion

            #endregion

            CuiHelper.DestroyUi(player, Layer);
            CuiHelper.AddUi(player, container);
        }
        
        private void Outline(ref CuiElementContainer container, string parent, string color = "1 1 1 1", string size = "1")
        {
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = $"0 0", OffsetMax = $"0 {size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin = $"0 -{size}", OffsetMax = $"0 0"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "0 0", AnchorMax = "0 1", OffsetMin = $"0 {size}", OffsetMax = $"{size} -{size}"},
                Image = {Color = color}
            }, parent);
            container.Add(new CuiPanel
            {
                RectTransform =
                    {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = $"-{size} {size}", OffsetMax = $"0 -{size}"},
                Image = {Color = color}
            }, parent);
        }

        #endregion
    }
}