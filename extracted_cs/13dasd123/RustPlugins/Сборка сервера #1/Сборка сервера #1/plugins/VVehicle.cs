using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using System;
using Rust.Modular;

namespace Oxide.Plugins
{
    [Info("VVehicle", "https://discord.gg/9vyTXsJyKR", "1.0.0")]
    [Description("Discord: fermens#8767")]
    class VVehicle : RustPlugin
    {
        #region [F] - Конфиг
        private PluginConfig config;
        protected override void LoadDefaultConfig()
        {
            config = PluginConfig.DefaultConfig();
        }
        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>();
        }
        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        class VV
        {
            [JsonProperty("Название")]
            public string name;

            [JsonProperty("Префаб")]
            public string prefab;

            [JsonProperty("Картинка")]
            public string image;

            [JsonProperty("Цена")]
            public int price;

            [JsonProperty("Спавн на воде?")]
            public bool water;

            [JsonProperty("Топливо")]
            public int fuel;

            [JsonProperty("Дистанция спавна")]
            public float distancespawn;

            [JsonProperty("Модули (для модульной машины)")]
            public List<string> moduls;

            [JsonProperty("Компоненты (для модульной машины)")]
            public List<string> components;

        }

        class UI
        {
            [JsonProperty("Фон - товар")]
            public string background;

            [JsonProperty("Фон - картинка товара")]
            public string backgroundimg;

            [JsonProperty("Цвет текста - название меню")]
            public string colorheader;

            [JsonProperty("Размер текста - название меню")]
            public string sizeheader;

            [JsonProperty("Размер текста - название товара")]
            public string sizea;

            [JsonProperty("Размер текста - цена")]
            public string sizeb;

            [JsonProperty("Цвет текста - название товара")]
            public string colora;

            [JsonProperty("Цвет текста - цена")]
            public string colorb;

            [JsonProperty("Цвет текста - обменять")]
            public string colorс;

            [JsonProperty("Цвет кнопки - обменять")]
            public string buttonc;

            [JsonProperty("Цвет кнопки - стрелка влево/вправо")]
            public string buttond;

            [JsonProperty("Цвет фона - успешная покупка")]
            public string backgrounds;

            [JsonProperty("Цвет обводки - успешная покупка")]
            public string colors;

            [JsonProperty("Цвет фона - неудачная покупка")]
            public string backgroundf;

            [JsonProperty("Цвет обводки -  неудачная покупка")]
            public string colorf;
        }

        private class PluginConfig
        {
            [JsonProperty("Транспортные средства")]
            public List<VV> vvs;

            [JsonProperty("Скидки (на это число умножается цена) [пермишены]")]
            public Dictionary<string, float> discounts;

            [JsonProperty("UI")]
            public UI uI;

            public static PluginConfig DefaultConfig()
            {
                return new PluginConfig()
                {
                    vvs = new List<VV>
                    {
                        new VV { distancespawn = 4f, fuel = 100, moduls = new List<string>(), name = "Мерседес", prefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab", price = 1, image = "http://www.clipartbest.com/cliparts/Kij/eR8/KijeR8jXT.png" },
                        new VV { distancespawn = 4f, fuel = 101, moduls = new List<string>(), name = "Кайот", prefab = "assets/content/vehicles/submarine/submarinesolo.entity.prefab", price = 2, water = true, image = "http://www.clipartbest.com/cliparts/Kij/eR8/KijeR8jXT.png" },
                        new VV { distancespawn = 4f, fuel = 102, moduls = new List<string>(), name = "Уррр", prefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab", price = 1, image = "https://gspics.org/images/2020/01/24/5MfnD.png" },
                        new VV { distancespawn = 5f, fuel = 100, moduls = new List<string> { { "vehicle.1mod.cockpit.with.engine" }, { "vehicle.1mod.passengers.armored" } }, name = "Журавель", prefab = "assets/content/vehicles/modularcar/2module_car_spawned.entity.prefab", components = new List<string>{ { "crankshaft3" }, { "sparkplug3" }, { "piston3" }, { "valve3" }, { "carburetor3" } }, price = 10, image = "http://www.clipartbest.com/cliparts/Kij/eR8/KijeR8jXT.png" },
                        new VV { distancespawn = 4f, fuel = 104, moduls = new List<string>(), name = "Мерседес4", prefab = "assets/content/vehicles/modularcar/3module_car_spawned.entity.prefab", price = 1, image = "http://www.clipartbest.com/cliparts/Kij/eR8/KijeR8jXT.png" },
                        new VV { distancespawn = 4f, fuel = 100, moduls = new List<string>(), name = "Мерседес5", prefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab", price = 100, image = "http://www.clipartbest.com/cliparts/Kij/eR8/KijeR8jXT.png" },
                        new VV { distancespawn = 4f, fuel = 19, moduls = new List<string>(), name = "Мерседес6", prefab = "assets/content/vehicles/modularcar/4module_car_spawned.entity.prefab", price = 1, image = "http://www.clipartbest.com/cliparts/Kij/eR8/KijeR8jXT.png" }
                    },
                    discounts = new Dictionary<string, float>
                    {
                        { "vvehicle.50", 0.5f },
                        { "vvehicle.30", 0.7f },
                        { "vvehicle.10", 0.9f }
                    },
                    uI = new UI
                    {
                        background = "0 0 0 1",
                        colorheader = "0.7490196 0.7490196 0.7490196 1",
                        sizeheader = "20",
                        backgroundimg = "0.1853514 0.1853514 0.1853514 1",
                        buttonc = "0.2356492 0.2356492 0.2356492 1",
                        colorс = "1 1 1 0.3929068",
                        buttond = "0.6 0.6 0.6 1",
                        colora = "0.7490196 0.7490196 0.7490196 1",
                        colorb = "0.7490196 0.7490196 0.7490196 1",
                        sizea = "12",
                        sizeb = "10",
                        backgroundf = "0.8 0 0 0.5",
                        backgrounds = "0 0.8 0 0.5",
                        colors = "0 0.8 0 1",
                        colorf = "0.8 0 0 1"

                    }
                };
            }
        }
        #endregion
        //\"anchormin\":\"0.21 0.2\",\"anchormax\":\"0.95 0.9\"
        #region [E]
        string GUI = "[{\"name\":\"MenuZ\",\"parent\":\"Overlay\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmax\":\"640 288.8\",\"offsetmin\":\"-640 -360\"}]},{\"name\":\"ButtonZ\",\"parent\":\"MenuZ\",\"components\":[{\"type\":\"UnityEngine.UI.Button\", \"command\":\"vmenu.close\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.048 0.51\",\"anchormax\":\"0.203 0.57\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Z1\",\"parent\":\"ButtonZ\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2588235 0.2588235 0.2588235 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 0\",\"offsetmax\":\"0 1\"}]},{\"name\":\"Z2\",\"parent\":\"ButtonZ\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2588395 0.2588395 0.2588395 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 1\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 -1\",\"offsetmax\":\"0 0\"}]},{\"name\":\"Z3\",\"parent\":\"ButtonZ\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2588395 0.2588395 0.2588395 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"1 0\"}]},{\"name\":\"Z4\",\"parent\":\"ButtonZ\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"0.2588395 0.2588395 0.2588395 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"1 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"-1 0\",\"offsetmax\":\"0 0\"}]},{\"name\":\"ZT\",\"parent\":\"ButtonZ\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"fontSize\":16,\"align\":\"MiddleCenter\",\"color\":\"0.254902 0.254902 0.254902 1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]}]";
        string menuV = "[{\"name\":\"SubContent_UI\",\"parent\":\"Main_UI\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.5 0.5\",\"anchormax\":\"0.5 0.5\",\"offsetmax\":\"640 288\",\"offsetmin\":\"-307.2 -216\"}]},{\"name\":\"MenuV\",\"parent\":\"SubContent_UI\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"1 1 1 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\"}]},{\"name\":\"HeaderV\",\"parent\":\"MenuV\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{main}\",\"fontSize\":{size},\"align\":\"MiddleCenter\",\"color\":\"{colorheader}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0.93\",\"anchormax\":\"1 0.99\",\"offsetmax\":\"0 0\"}]}{elements}]";
        string menuVelements = ",{\"name\":\"ElementV{num}\",\"parent\":\"MenuV\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{background}\", \"sprite\": \"assets/content/ui/ui.background.transparent.radial.psd\"},{\"type\":\"RectTransform\",\"anchormin\":\"{min}\",\"anchormax\":\"{max}\",\"offsetmax\":\"0 0\"}]},{\"name\":\"FonkV\",\"parent\":\"ElementV{num}\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{backgroundimg}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"0.3386057 1\",\"offsetmin\":\"4 4\",\"offsetmax\":\"-4 -4\"}]},{\"name\":\"LogoV\",\"parent\":\"FonkV\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"4 4\",\"offsetmax\":\"-4 -4\"}]},{\"name\":\"HeaderelementV\",\"parent\":\"ElementV{num}\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{header}\", \"fontSize\":{sizea},\"align\":\"UpperLeft\",\"color\":\"{colora}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.38 0.6\",\"anchormax\":\"0.95 0.93\",\"offsetmax\":\"0 0\"}]},{\"name\":\"PriceV\",\"parent\":\"ElementV{num}\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{price}\", \"fontSize\":{sizeb},\"align\":\"UpperLeft\",\"color\":\"{colorb}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.38 0.3\",\"anchormax\":\"0.95 0.55\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BuyV\",\"parent\":\"ElementV{num}\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"vmenu.buy {i} {num}\",\"color\":\"{buttonc}\", \"sprite\": \"assets/content/ui/ui.background.tiletex.psd\"},{\"type\":\"RectTransform\",\"anchormin\":\"0.3386057 0\",\"anchormax\":\"1 0.3\",\"offsetmin\":\"0 4\",\"offsetmax\":\"-4 -4\"}]},{\"name\":\"BtextV\",\"parent\":\"BuyV\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{btext}\",\"fontSize\":16,\"align\":\"MiddleCenter\",\"color\":\"{colorc}\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]}";
        string menuVmessage = "[{\"name\":\"ElementVM{num}\",\"parent\":\"MenuV\",\"components\":[{\"type\":\"UnityEngine.UI.Image\",\"color\":\"{color2}\", \"sprite\": \"assets/content/ui/ui.background.transparent.radial.psd\"},{\"type\":\"RectTransform\",\"anchormin\":\"{min}\",\"anchormax\":\"{max}\",\"offsetmax\":\"0 0\"}]},{\"name\":\"BtextVM\",\"parent\":\"ElementVM{num}\",\"components\":[{\"type\":\"UnityEngine.UI.Text\",\"text\":\"{text}\",\"align\":\"MiddleCenter\",\"fontSize\":18,\"color\":\"1 1 1 0.9\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"},{\"type\":\"UnityEngine.UI.Outline\",\"color\":\"{color}\",\"distance\":\"-1 -1\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmin\":\"0 0\",\"offsetmax\":\"0 0\"}]}]}]";
        string pages = ",{\"name\":\"btnpage\",\"parent\":\"MenuV\",\"components\":[{\"type\":\"UnityEngine.UI.RawImage\",\"sprite\":\"assets/content/textures/generic/fulltransparent.tga\",\"color\":\"{color}\",\"png\":\"{png}\"},{\"type\":\"RectTransform\",\"anchormin\":\"{min}\",\"anchormax\":\"{max}\",\"offsetmax\":\"0 0\"}]},{\"name\":\"btnpage1\",\"parent\":\"btnpage\",\"components\":[{\"type\":\"UnityEngine.UI.Button\",\"command\":\"{page}\",\"color\":\"0 0 0 0\"},{\"type\":\"RectTransform\",\"anchormin\":\"0 0\",\"anchormax\":\"1 1\",\"offsetmax\":\"0 0\"}]}";
        class Coord
        {
            public string min;
            public string max;
        }

        Dictionary<int, Coord> _coordinates = new Dictionary<int, Coord>();

        private void GenerateCoordinates()
        {
            float startx = 0.08f;

            float stepx = 0.275f;
            float stepy = 0.18f;

            float gap = 0.01f;
            float gapy = 0.01f;

            float x = startx;
            float y = 1.11f;

            for (int i = 0; i < 18; i++)
            {
                if (i % 3 == 0)
                {
                    x = startx;
                    y -= stepy + gapy;
                }

                float minx = x;
                float miny = y - stepy;
                x += stepx;
                _coordinates[i] = new Coord { min = $"{minx} {miny}", max = $"{x} {y}" };
                x += gap;
            }
        }


        private Dictionary<ulong, Dictionary<int, Timer>> timal = new Dictionary<ulong, Dictionary<int, Timer>>();
        private void CreateMessage(BasePlayer player, int num, string message, bool succes = false)
        {
            if(!timal.ContainsKey(player.userID)) timal.Add(player.userID, new Dictionary<int, Timer>());

            Dictionary<int, Timer> dic;
            if (timal.TryGetValue(player.userID, out dic))
            {
                Timer ss;
                if (dic.TryGetValue(num, out ss))
                {
                    if (!ss.Destroyed) ss.Destroy();
                }
            }

            string numstr = num.ToString();
            string name = $"ElementVM{numstr}";
            Coord coord = _coordinates[num];
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", name);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", menuVmessage.Replace("{text}", message).Replace("{min}", coord.min).Replace("{color2}", succes ? config.uI.backgrounds : config.uI.backgroundf).Replace("{color}", succes ? config.uI.colors : config.uI.colorf).Replace("{max}", coord.max).Replace("{num}", numstr));
            EffectNetwork.Send(new Effect(succes ? "assets/prefabs/deployable/vendingmachine/effects/vending-machine-purchase-human.prefab" : "assets/bundled/prefabs/fx/invite_notice.prefab", player, 0, Vector3.up, Vector3.zero) { scale = 1f }, player.net.connection);

            timal[player.userID][num] = timer.Once(3f, () => CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", name));
        }

       // private List<ulong> activeopen = new List<ulong>();

        private void OpenMenu(BasePlayer player, int page  = 0)
        {
           /* if (!activeopen.Contains(player.userID))
            {
                BMenu.Call("MainGUI", player);
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "SubContent_UI");
                CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "SubMenu_UI");
                BMenu.Call("SetPage", player.userID, "fermens");
                BMenu.Call("SetActiveButton", player, "fermens");
                activeopen.Add(player.userID);
            }*/

            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "SubContent_UI");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "SubMenu_UI");
            BMenu.Call("MainGUI", player);
            BMenu.Call("SetPage", player.userID, "merc");
            BMenu.Call("SetActiveButton", player, "Mercenaries");
            BMenu.Call("SetActiveSubButton", player, "vmenu");

            NextTick(() => CreateUI(player, page));
            
        }
        private void CreateUI(BasePlayer player, int page = 0)
        {
            string elemets = "";

            if (_coordinates.Count != 18) GenerateCoordinates();

            int count = config.vvs.Count;
            int max = (page + 1) * 18;
            if (max > count) max = count;
            int start = page * 18;

            int razn = max - start;
            if (razn > 18 || razn <= 0)
            {
                start = 0;
                max = 18;
                page = 0;
            }

            int a = 0;
            for (int i = start; i < max; i++)
            {
                VV vV = config.vvs[i];
                Coord coord = _coordinates[a];
                elemets += menuVelements.Replace("{buttonc}", config.uI.buttonc).Replace("{sizeb}", config.uI.sizeb).Replace("{sizea}", config.uI.sizea).Replace("{colorb}", config.uI.colorb).Replace("{colora}", config.uI.colora).Replace("{colorc}", config.uI.colorс).Replace("{background}", config.uI.background).Replace("{backgroundimg}", config.uI.backgroundimg).Replace("{min}", coord.min).Replace("{max}", coord.max).Replace("{i}", i.ToString()).Replace("{num}", a.ToString()).Replace("{png}", GetImage(vV.image)).Replace("{header}", vV.name).Replace("{price}", GetMessage("price", player.UserIDString).Replace("{price}", GetDiscount(player, vV.price).ToString())).Replace("{btext}", GetMessage("exchange", player.UserIDString));
                a++;
            }
            if (count > max) elemets += pages.Replace("{min}", "0.938 0.325").Replace("{max}", "0.962 0.405").Replace("{png}", GetImage("vv_arrow4")).Replace("{color}", config.uI.buttond).Replace("{page}", $"vmenu.open {page + 1}");
            if (page > 0) elemets += pages.Replace("{min}", "0.043 0.325").Replace("{max}", "0.067 0.405").Replace("{png}", GetImage("vv_arrow4l")).Replace("{color}", config.uI.buttond).Replace("{page}", $"vmenu.open {page - 1}");
            string gui = menuV.Replace("{colorheader}", config.uI.colorheader).Replace("{main}", GetMessage("namemenu", player.UserIDString)).Replace("{size}", config.uI.sizeheader).Replace("{elements}", elemets);
            CloseMenu(player);
            //CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", GUI);
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "AddUI", gui);
        }
        private void CloseMenu(BasePlayer player)
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "MenuZ");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connection = player.net.connection }, null, "DestroyUI", "SubContent_UI");
        }
        #endregion

        #region [R]
        [ConsoleCommand("vmenu.open")]
        private void cmdvmenu(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            int page = 0;
            if (arg.HasArgs()) int.TryParse(arg.Args[0], out page);
            OpenMenu(player, page);
        }

        [ConsoleCommand("vmenu.close")]
        private void cmdvmenuclose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;
            CloseMenu(player);
           // if (activeopen.Contains(player.userID)) activeopen.Remove(player.userID);
        }

        [ConsoleCommand("vmenu.buy")]
        private void cmdvmenubuy(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            if (!arg.HasArgs(2)) return;

            int num;
            int num2;
            if (!int.TryParse(arg.Args[0], out num) || num < 0 || num >= config.vvs.Count || !int.TryParse(arg.Args[1], out num2) || num2 < 0 || num2 > 18) return;

            Buy(player, config.vvs[num], num2);
        }
        #endregion

        #region [M]
         [ChatCommand("vmenu")]
         private void cmmenu(BasePlayer player, string cmd, string[] args)
         {
             OpenMenu(player);
         }
        #endregion

        #region [E]
       /* void OnPlayerDisconnected(BasePlayer player)
        {
            if (activeopen.Contains(player.userID)) activeopen.Remove(player.userID);
        }*/

        private void OnServerInitialized()
        {
           // activeopen.Clear();
            lang.RegisterMessages(messages, this);

            foreach (var x in config.vvs)
            {
                if(x.moduls != null && x.moduls.Count > 0 && x.components == null)
                {
                    x.components = new List<string> { { "crankshaft3" }, { "sparkplug3" }, { "piston3" }, { "valve3" }, { "carburetor3" } };
                    SaveConfig();
                }
                AddImage(x.image, x.image);
            }

            foreach (var x in config.discounts) permission.RegisterPermission(x.Key, this);

            AddImage("https://i.ibb.co/b7zSknN/play-button-3.png", "vv_arrow4");
            AddImage("https://i.ibb.co/nB5yHYf/play-button-3-1.png", "vv_arrow4l");
        }

        private void Unload()
        {
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "MenuZ");
            CommunityEntity.ServerInstance.ClientRPCEx(new Network.SendInfo { connections = Network.Net.sv.connections }, null, "DestroyUI", "MenuV");
        }
        #endregion

        #region [N]
        private int GetDiscount(BasePlayer player, int price)
        {
            float disc = 1f;
            foreach(var x in config.discounts)
            {
                if (permission.UserHasPermission(player.UserIDString, x.Key) && disc > x.Value) disc = x.Value;
            }
            return (int)Math.Ceiling(price * disc);
        }

        private void Buy(BasePlayer player, VV element, int num2)
        {
            double balance = (double)Economics.Call("Balance", player.userID);
            int price = GetDiscount(player, element.price);
            if (balance < price)
            {
                CreateMessage(player, num2, GetMessage("nomoney", player.UserIDString));
                return;
            }

            if (player.IsBuildingBlocked())
            {
                CreateMessage(player, num2, GetMessage("blockbuilding", player.UserIDString));
                return;
            }

            if (element.water && player.modelState != null && player.modelState.waterLevel <= 0f)
            {
                CreateMessage(player, num2, GetMessage("noterrain", player.UserIDString));
                return;
            }
            Vector3 vector = player.transform.position + (player.eyes.MovementForward() * element.distancespawn) + Vector3.up * 2f;
            RaycastHit hit;
            if (!Physics.Raycast(vector, Vector3.down, out hit, 5f, LayerMask.GetMask("Terrain", "World", "Construction"), QueryTriggerInteraction.Ignore))
            {
                CreateMessage(player, num2, GetMessage("badlocation", player.UserIDString));
                return;
            }
            Vector3 spawnpos = hit.point;
            float water = TerrainMeta.WaterMap.GetHeight(vector);
            if (element.water) spawnpos.y = TerrainMeta.WaterMap.GetHeight(vector) + 2f;
            else if(water > hit.point.y)
            {
                CreateMessage(player, num2, GetMessage("nowater", player.UserIDString));
                return;
            }

            BaseEntity entity = GameManager.server.CreateEntity(element.prefab, spawnpos);
            if (entity == null)
            {
                Debug.LogError($"[VVehicle] Префаб для {element.name} не существует!");
                return;
            }

            entity.OwnerID = player.userID;

            if (entity is ModularCar) (entity as ModularCar).spawnSettings.useSpawnSettings = false;

            entity.Spawn();

            if (entity is ModularCar)
            {
                ModularCar modularCar = entity as ModularCar;
                foreach (var x in element.moduls)
                {
                    Item moduleItem = ItemManager.CreateByName(x);
                    if (moduleItem == null) continue;
                    if (!modularCar.TryAddModule(moduleItem)) moduleItem.Remove();
                }

                NextTick(() =>
                {
                    EngineStorage engineStorage = null;
                    for (int index = 0; index < modularCar.AttachedModuleEntities.Count; ++index)
                    {
                        engineStorage = GetEngineStorage(modularCar.AttachedModuleEntities[index]);
                        if (engineStorage != null) break;
                    }

                    // Debug.Log("baseVehicleModule??");
                    if (engineStorage != null)
                    {
                        engineStorage.AdminAddParts(3);
                        /*
                        foreach (var x in element.components)
                        {
                            Item item = ItemManager.CreateByName(x);
                            if (item != null)
                            {
                                int slot = 0;
                                if (x.Contains("carburetor")) slot = 1;
                                else if (x.Contains("plug")) slot = 2;
                                else if (x.Contains("piston")) slot = 4;
                                else if (x.Contains("valve")) slot = 3;
                                item.MoveToContainer(engineStorage.inventory, slot, false);
                            }
                        }*/
                    }
                });
            }

            foreach (var baseEntity in entity.children)
            {
                //Debug.Log(baseEntity.PrefabName);
                if (baseEntity.PrefabName.Contains("fuel"))
                {
                    StorageContainer storageContainer = baseEntity as StorageContainer;
                    if (storageContainer != null)
                    {
                        storageContainer.inventory.Clear();
                        FillTheTank(storageContainer.inventory, element.fuel);
                    }
                }
            }


            Economics.Call("SetBalance", player.UserIDString, balance - price);
            BMenu.Call("UpdateBalance", player);
            CreateMessage(player, num2, GetMessage("succes", player.UserIDString), true);

        }
        private static EngineStorage GetEngineStorage(BaseVehicleModule module)
        {
            var engineModule = module as VehicleModuleEngine;
            if (engineModule == null) return null;

            return engineModule.GetContainer() as EngineStorage;
        }

        private void FillTheTank(ItemContainer container, int amount) => ItemManager.CreateByItemID(-946369541, amount).MoveToContainer(container);
        #endregion

        #region [S]
        [PluginReference] Plugin ImageLibrary, Economics, BMenu;
        string GetImage(string shortname, ulong skin = 0) => (string)ImageLibrary.Call("GetImage", shortname, skin);
        void AddImage(string url, string shortname, ulong skin = 0) => ImageLibrary?.Call("AddImage", url, shortname, skin);
        #endregion

        #region [#]
        Dictionary<string, string> messages = new Dictionary<string, string>()
        {
            { "succes", "Сделка прошла успешно" },
            { "nowater" , "Этот транспорт не плавает по воде!" },
            { "badlocation", "Выберите более подходящее место для спавна!"},
            { "noterrain", "Этот транспорт не ездит по суше!" },
            { "blockbuilding", "Вы находитесь на чужой територии!" },
            { "nomoney", "Недостаточно валюты!" },
            { "namemenu", "АВТОСАЛОН"},
            { "exchange", "ОБМЕНЯТЬ" },
            { "price",  "ПОКУПКА ЗА <color=#EBD4AE>{price} ПЛАТИНЫ</color>"}
        };

        private string GetMessage(string key, string userId) => lang.GetMessage(key, this, userId);
        #endregion
    }
}