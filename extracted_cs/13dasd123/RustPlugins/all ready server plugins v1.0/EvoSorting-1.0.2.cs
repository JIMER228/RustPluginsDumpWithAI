using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Physics = UnityEngine.Physics;

namespace Oxide.Plugins
{
    [Info("EvoSorting", "Hougan", "1.0.2")]
      //  Слив плагинов server-rust by Apolo YouGame
    [Description("Революционная сортировка предметов по ящикам")]
    public class EvoSorting : RustPlugin
    {
        #region Variables

        // Значения для конфига

        // Интервал сохранения информации
        private int SaveTime = 300;
        // Необходимо ли быть авторизованым в замке для использования быстрого перемещения
        private bool NeedCodeAuth = true;
        // Необходимо ли разрешение для быстрого перемещения
        private bool NeedPermToMove = true;
        // Необходимо ли разрешение для выбора типа ящика
        private bool NeedPermToChoose = true;
        
        // Статические значения для ГУИ
        static string layer = "GUI_Layer";
        
        // --------------------
        
        private class SortBox
        {
            [JsonProperty("Название ящика в ГУИ")]
            public string Name;
            [JsonProperty("Описание ящика в ГУИ")]
            public string Description;
            [JsonProperty("Скин ящика в ГУИ")]
            public ulong SkinID;
            
            [JsonProperty("Категории предметов которые принимает ящик")]
            public List<string> Categories;
            [JsonProperty("Разрешение на использование этого типа ящика")]
            public string Permission;

            public SortBox(string Name, string Description, ulong SkinID, List<string> Categories, string Permission)
            {
                this.Name = Name;
                this.Description = Description;
                this.SkinID = SkinID;
                this.Categories = Categories;
                this.Permission = Permission;
                
                Interface.Oxide.LogWarning($"Сформирован ящик: {Name}");
            }
        }
        
        // Список хранящий список всех ящиков
        List<SortBox> sortBoxesList = new List<SortBox>();
        
        // Словарь связки ключей ID -> Список категорий
        Dictionary<uint, List<string>> sortBoxes = new Dictionary<uint, List<string>>();
        
        #endregion

        #region Initialization

        private void OnServerInitialized()
        {
            LoadDefaultConfig();
            
            // Загружаем стандартные типы ящиков
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("EvoSorting/SortBoxesList"))
            {
                PrintWarning("Сортировочные ящики не созданы, формируем их!");
                sortBoxesList.Add(new SortBox("Оружие", "Данный ящик будет хранить оружие и аммуницию к нему", 123456, new List<string> { "Weapon", "Ammo" }, "EvoSorting.Weapon"));
                sortBoxesList.Add(new SortBox("Ресурсы", "Данный ящик будет хранить ресурсы", 123456, new List<string> { "Resources" }, "EvoSorting.Resources"));
                sortBoxesList.Add(new SortBox("Одежда", "Данный ящик будет хранить одежду", 123456, new List<string> { "Attire" }, "EvoSorting.Attire"));
                sortBoxesList.Add(new SortBox("Инструменты", "Данный ящик будет хранить инструменты", 123456, new List<string> { "Tool" }, "EvoSorting.Tools"));
                sortBoxesList.Add(new SortBox("Строительство", "Данный ящик будет хранить строительные материалы", 123456,  new List<string> { "Construction" }, "EvoSorting.Build"));
                sortBoxesList.Add(new SortBox("Медицина", "Данный ящик будет хранить медицинские приборы", 123456,  new List<string> { "Medical" }, "EvoSorting.Medicine"));
                sortBoxesList.Add(new SortBox("Компоненты", "Данный ящик будет помещены все компоненты", 123456,  new List<string> { "Components" } , "EvoSorting.Components"));
                sortBoxesList.Add(new SortBox("Всё сразу", "Данный ящик будет помещены все предметы", 123456,  new List<string> { "All" } , "EvoSorting.All"));
                
                Interface.Oxide.DataFileSystem.WriteObject("EvoSorting/SortBoxesList", sortBoxesList);
            }
            else
            {
                PrintWarning("Сортировочные ящики найдены, загружаем!");
                sortBoxesList = Interface.Oxide.DataFileSystem.ReadObject<List<SortBox>>("EvoSorting/SortBoxesList");
            }
            
            // Загружаем уже сохраненные ящики
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("EvoSorting/SortBoxes"))
            {
                PrintWarning("Информация о существующих ящиках загружена");
                sortBoxes = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<uint, List<string>>>("EvoSorting/SortBoxes");
            }
            
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["RAYCAST.NULL"] = "При использовании этой команды вы должны смотреть на ящик.",
                ["RAYCAST.NOT.BOX"] = "Вы смотрите не на ящик!",
                ["RAYCAST.NOT.SORT.BOX"] = "Данному ящику не присвоена категория принимаемых предметов.",
                
                ["BOX.CODE.ERROR"] = "Для быстрого перемещения вам необходимо быть авторизованным в замке!",
                
                
                ["BOX.MOVE.PERMISSION"] = "У вас нету доступа для перемещения предметов данного типа!",
                ["BOX.MOVE.FULL"] = "Инвентарь ящика переполненн, вы переместили {0} предметов!",
                
                ["BOX.HELP.TEXT"] = "Теперь вы можете быстро перемещать предметы в ящики при помощи взгляда!\nВам достаточно посмотреть на ящик и написать: /fast\nДля удобства вы можете забиндить это на любую клавишу!",
                
                
                ["BOX.MOVE.SUCCESSFUL"] = "Вы успешно переместили все предметы данного типа: {0}!",
                ["BOX.MOVE.SUCCESSFUL.0"] = "У вас нету предметов данного типа: {0}!",
                ["BOX.SET.SUCCESSFUL"] = "Вы успешно установили тип для данного ящика: {0}!\nТеперь вы можете перемещать предметы быстро!\nПодробнее: /fast help!",
                
                
                
                ["GUI.HEADER"] = "ВЫБЕРИТЕ ТИП ЯЩИКА",
                ["GUI.BUY"] = "Приобретите данную возможность на RustPlugin.ru",
                
                
                ["ERROR.1"] = "Ошибка в плагине. Код: #1",
                ["ERROR.2"] = "Ошибка в плагине. Код: #2",
                
            }, this, "en");
            
            PrintWarning("Сообщения из LANG файла загружены");

            foreach (var check in sortBoxesList)
                permission.RegisterPermission(check.Permission, this);
            
            PrintWarning("Разрешения зарегистрированы в системе");

            timer.Every(SaveTime, SaveData);
        }
        
        protected override void LoadDefaultConfig()
        {
            Config["Время сохранения файла с информацией"] = SaveTime = GetConfig("Время сохранения файла с информацией", 300);
            Config["Необходимо ли быть авторизованным в замке для быстрого перемещения"] = NeedCodeAuth = GetConfig("Необходимо ли быть авторизованным в замке для быстрого перемещения", true);
            Config["Необходимо ли разрешение для перемещения"] = NeedPermToMove = GetConfig("Необходимо ли разрешение для перемещения", true);
            Config["Необходимо ли разрешение для выбора"] = NeedPermToChoose = GetConfig("Необходимо ли разрешение для выбора", true);
            
            SaveConfig();
        }
        private T GetConfig<T>(string name, T value) => Config[name] == null ? value : (T)Convert.ChangeType(Config[name], typeof(T));
        
        #endregion

        #region Hook

        private void Unload() => SaveData();
        
        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            BaseEntity entity = go.ToBaseEntity();
            BasePlayer player = plan.GetOwnerPlayer();

            if (entity is BoxStorage)
            {
                GUI_Choose(player, entity.net.ID);
            }
        }
        
        private void OnPlayerInput(BasePlayer player, InputState state)
        {
            if (state.IsDown(BUTTON.FIRE_SECONDARY) && state.WasJustPressed(BUTTON.RELOAD))
            {
                player.SendConsoleCommand("consoleFast");
                return;
            }
        }

        #endregion

        #region Functions
        
        private void ApplySkin(BaseEntity entity)
        {
            SortBox sortBox = sortBoxesList.Find(p => p.Categories.Contains(sortBoxes[entity.net.ID][0]));

            if (sortBox == null)
            {
                PrintWarning($"Произошла ошибка #3. Такого ящика не существует (СКИН НЕ БЫЛ УСТАНОВЛЕН)");
                return;
            }

            entity.skinID = sortBox.SkinID;
            entity.SendNetworkUpdate();
        }
        #endregion

        #region Commands

        [ConsoleCommand("consoleFast")]
        private void cmdEvoConsole(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null)
                return;
            
            RaycastHit hitinfo;
            if (!Physics.Raycast(player.eyes.position, player.GetNetworkRotation() * Vector3.forward, out hitinfo, 5f, LayerMask.GetMask(new string[] {"Deployed"})))
            {
                return;
            }

            if (!(hitinfo.GetEntity() is BoxStorage))
            {
                return;
            }
    
            BaseEntity boxEntity = hitinfo.GetEntity();
    
            if (!sortBoxes.ContainsKey(boxEntity.net.ID))
            {
                return;
            }
        }
        
        [ChatCommand("fast")]
        private void cmdEvoMove(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                RaycastHit hitinfo;
                if (!Physics.Raycast(player.eyes.position, player.GetNetworkRotation() * Vector3.forward, out hitinfo, 5f, LayerMask.GetMask(new string[] {"Deployed"})))
                {
                    SendReply(player, GetMessage("RAYCAST.NULL"));
                    return;
                }
                
                if (!(hitinfo.GetEntity() is BoxStorage)) { SendReply(player, GetMessage("RAYCAST.NOT.BOX")); return; }
    
                BaseEntity boxEntity = hitinfo.GetEntity();
    
                if (!sortBoxes.ContainsKey(boxEntity.net.ID))
                {
                    SendReply(player, GetMessage("RAYCAST.NOT.SORT.BOX"));
                    return;
                }
                
                SortBox sortBox = sortBoxesList.Find(p => p.Categories.Contains(sortBoxes[boxEntity.net.ID][0]));
    
                if (sortBox == null)
                {
                    SendReply(player, GetMessage("ERROR.1"));
                    PrintWarning($"Произошла ошибка #1. ID Ящика: {boxEntity.net.ID}");
                    return;
                }
    
                if (NeedPermToMove && !permission.UserHasPermission(player.UserIDString, sortBox.Permission))
                {
                    SendReply(player, GetMessage("BOX.MOVE.PERMISSION"));
                    return;
                }
    
                if (NeedCodeAuth)
                {
                    if (boxEntity.GetSlot(BaseEntity.Slot.Lock) != null)
                    {
                        if (!boxEntity.GetSlot(BaseEntity.Slot.Lock).GetComponent<CodeLock>().whitelistPlayers.Contains(player.userID))
                        {
                            SendReply(player, GetMessage("BOX.CODE.ERROR"));
                            return;
                        }
                    }
                }
                
                // Начинаем перемещать предметы
                int moved = 0;
                foreach (var check in player.inventory.AllItems())
                {
                    if ((sortBox.Categories[0] != "All" && !sortBox.Categories.Contains(check.info.category.ToString())) || check.GetRootContainer().capacity != 24)
                        continue;
    
                    if (!boxEntity.GetComponent<StorageContainer>().inventory.CanTake(check))
                    {
                        SendReply(player, GetMessage("BOX.MOVE.FULL"), moved);
                        return;
                    }
    
                    check.MoveToContainer(boxEntity.GetComponent<StorageContainer>().inventory);
    
                    moved++;
                }
    
                SendReply(player, moved != 0 ? GetMessage("BOX.MOVE.SUCCESSFUL") : GetMessage("BOX.MOVE.SUCCESSFUL.0"), sortBox.Categories[0]);
                return;
            }

            if (args[0].ToLower() == "help")
            {
                SendReply(player, GetMessage("BOX.HELP.TEXT"));
            }
            
            if (args[0].ToLower() == "set")
            {
                RaycastHit hitinfo;
                if (!Physics.Raycast(player.eyes.position, player.GetNetworkRotation() * Vector3.forward, out hitinfo, 5f, LayerMask.GetMask(new string[] {"Deployed"})))
                {
                    SendReply(player, GetMessage("RAYCAST.NULL"));
                    return;
                }
                
                if (!(hitinfo.GetEntity() is BoxStorage)) { SendReply(player, GetMessage("RAYCAST.NOT.BOX")); return; }
    
                BaseEntity boxEntity = hitinfo.GetEntity();
                
                GUI_Choose(player, boxEntity.net.ID);
            }
        }
        
        #endregion

        #region Helpers

        private string GetMessage(string key) => lang.GetMessage(key, this);
        
        private static string HexToRustFormat(string hex)
        {
            if (string.IsNullOrEmpty(hex))
            {
                hex = "#FFFFFFFF";
            }

            var str = hex.Trim('#');

            if (str.Length == 6)
                str += "FF";

            if (str.Length != 8)
            {
                throw new Exception(hex);
                throw new InvalidOperationException("Cannot convert a wrong format.");
            }

            var r = byte.Parse(str.Substring(0, 2), NumberStyles.HexNumber);
            var g = byte.Parse(str.Substring(2, 2), NumberStyles.HexNumber);
            var b = byte.Parse(str.Substring(4, 2), NumberStyles.HexNumber);
            var a = byte.Parse(str.Substring(6, 2), NumberStyles.HexNumber);

            Color color = new Color32(r, g, b, a);

            return string.Format("{0:F2} {1:F2} {2:F2} {3:F2}", color.r, color.g, color.b, color.a);
        }

        #endregion

        #region GUI

        [ConsoleCommand("UI_BUY")]
        private void GUIConsoleCommandBuy(ConsoleSystem.Arg args)
        {
            args.Player().ChatMessage(GetMessage("GUI.BUY"));
        }
        
        [ConsoleCommand("UI_EDIT")]
        private void GUIConsoleCommandEdit(ConsoleSystem.Arg args)
        {
            GUI_Choose(args.Player(), uint.Parse(args.FullString));
        }
        
        [ConsoleCommand("UI_SETTYPE")]
        private void GUIConsoleCommandSetUp(ConsoleSystem.Arg args)
        {
            uint ID = uint.Parse(args.FullString.Split(':')[0]);
            string Type = args.FullString.Split(':')[1];


            BasePlayer player = args.Player();
            BaseEntity boxEnity = BaseNetworkable.serverEntities.Find(ID) as BaseEntity;
            
            CuiHelper.DestroyUi(player, layer);
            
            if (boxEnity == null || boxEnity.IsDestroyed)
            {
                player.ChatMessage(GetMessage("ERROR.2"));
                CuiHelper.DestroyUi(player, layer);
                return;
            }

            if (sortBoxes.ContainsKey(boxEnity.net.ID))
                sortBoxes[boxEnity.net.ID] = new List<string> { Type };
            else
                sortBoxes.Add(boxEnity.net.ID, new List<string> { Type });
            
            
            SendReply(player, GetMessage("BOX.SET.SUCCESSFUL"), Type);
            
            ApplySkin(boxEnity);
        }
        
        private void GUI_Choose(BasePlayer player, uint ID)
        {
            CuiHelper.DestroyUi(player, layer);
            
            var container = new CuiElementContainer();

            var Panel = container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = { AnchorMin = "0.3988118 0.3083189", AnchorMax = "0.6011882 0.8916812" },
                Image = { Color="0 0 0 0" }
            }, "Overlay", layer);

            container.Add(new CuiButton
            {
                /* Закрываем по клику снаружи */
                RectTransform = { AnchorMin = "-100 -100", AnchorMax = "100 100" },
                Button = { Color = "0 0 0 0", Close = layer},
                Text = { Text = "" }
            }, layer);
            
            container.Add(new CuiButton
            {
                /* Отрисовываем хеадер */
                RectTransform = { AnchorMin = "0.01101831 0.9285501", AnchorMax = "0.9889818 0.9929036" },
                Button = { Color = HexToRustFormat("#2A2A2AFF") },
                Text = { Text = GetMessage("GUI.HEADER"), Font = "robotocondensed-regular.ttf", FontSize = 21, Align = TextAnchor.MiddleCenter }
            }, layer, layer + ".header");

            container.Add(new CuiButton
            {
                /* Отрисовываем тело */
                RectTransform = { AnchorMin = "0.01101831 0.1143049", AnchorMax = "0.9889818 0.9207283" },
                Button = { Color = "0 0 0 0" },
                Text = { Text = "" }
            }, layer, layer + ".body");

            Vector2 currentAnchor = new Vector2
            {
                x = 0.8339893f,
                y = 0.9960635f
            };

            foreach (var check in sortBoxesList)
            {
                container.Add(new CuiButton
                {
                    /* Отрисовываем задник */
                    RectTransform = { AnchorMin = $"0.005208333 {currentAnchor.x}", AnchorMax = $"0.9947917 {currentAnchor.y}" },
                    Button = { Color = HexToRustFormat("#000000AA")},
                    Text = { Text = "" }
                }, layer + ".body", layer + ".body" + ".item." + check.Name);
                
                container.Add(new CuiButton
                {
                    /* Отрисовываем название */
                    RectTransform = { AnchorMin = "0 0.3800711", AnchorMax = "1 0.952418" },
                    Button = { Color = "0 0 0 0" },
                    Text = { Text = check.Name.ToUpper(), FontSize = 28, Font = "robotocondensed-bold.ttf", Align = TextAnchor.UpperCenter}
                }, layer+ ".body" + ".item." + check.Name);
                
                container.Add(new CuiButton
                {
                    /* Отрисовываем описание */
                    RectTransform = { AnchorMin = "0.04736835 0.05001945", AnchorMax = "0.9578946 0.4642217" },
                    Button = { Color = "0 0 0 0"},
                    Text = { Text = check.Description, FontSize = 10, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter}
                }, layer+ ".body" + ".item." + check.Name);

                if (permission.UserHasPermission(player.UserIDString, check.Permission))
                {
                    container.Add(new CuiButton
                    {
                        /* Отрисовываем кнопку выбора */
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                        Button = { Color = "0 0 0 0", Command = $"UI_SetType {ID}:{check.Categories[0]}" },
                        Text = { Text = "" }
                    }, layer+ ".body" + ".item." + check.Name);
                }
                else
                {
                    container.Add(new CuiButton
                    {
                        /* Отрисовываем поверхностный цвет */
                        RectTransform = { AnchorMin = "0 0.001", AnchorMax = "0.999 0.999" },
                        Button = { Color = HexToRustFormat("#0000005D"), Command = $"UI_BUY" },
                        Text = { Text = "" }
                    }, layer+ ".body" + ".item." + check.Name);
                    
                    container.Add(new CuiButton
                    {
                        /* Отрисовываем дополнительный задник */
                        RectTransform = { AnchorMin = "0.2578951 0.2212245", AnchorMax = "0.7447369 0.7513472" },
                        Button = { Color = HexToRustFormat("#DD2121FF"), Command = $"UI_BUY" },
                        Text = { Text = "" }
                    }, layer+ ".body" + ".item." + check.Name, layer+ ".body" + ".item." + check.Name + ".blocked");
                    
                    container.Add(new CuiButton
                    {
                        /* Отрисовываем НЕДОСТУПНО */
                        RectTransform = { AnchorMin = "0 0.3013504", AnchorMax = "1 0.9822022" },
                        Button = { Color = "0 0 0 0", Command = $"UI_BUY" },
                        Text = { Text = "НЕДОСТУПНО", FontSize = 18, Font = "robotocondensed-bold.ttf", Align = TextAnchor.MiddleCenter }
                    }, layer+ ".body" + ".item." + check.Name + ".blocked");
                    
                    container.Add(new CuiButton
                    {
                        /* Отрисовываем НЕОБХОДИМО ПРИОБРЕСТИ */
                        RectTransform = { AnchorMin = "0 0", AnchorMax = "1 0.3087809" },
                        Button = { Color = "0 0 0 0", Command = $"UI_BUY" },
                        Text = { Text = "НЕОБХОДИМО ПРИОБРЕСТИ", FontSize = 8, Font = "robotocondensed-regular.ttf", Align = TextAnchor.MiddleCenter }
                    }, layer+ ".body" + ".item." + check.Name + ".blocked");
                }

                currentAnchor.x -= 0.172011f;
                currentAnchor.y -= 0.172011f;
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion

        #region Data

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("EvoSorting/SortBoxes", sortBoxes);
        }

        #endregion
    }
}
