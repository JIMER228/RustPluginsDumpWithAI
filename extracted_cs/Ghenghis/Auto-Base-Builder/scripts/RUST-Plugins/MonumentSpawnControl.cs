using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    //  Changes in V1.0.7
    //  Null check fix.

    // To Do
    //  Give users the option to change prefab - UI list.
    //  Give users the option to add/remove spawn points.
    //  Have a 'set all' button for respawn time. 

    [Info("MonumentSpawnControl", "Steenamaroo", "1.0.8", ResourceId = 0)]
    [Description("Control default Rust monument spawn rates and amounts.")]

    class MonumentSpawnControl : RustPlugin
    {
        public static MonumentSpawnControl ins;
        Dictionary<string, Dictionary<string, List<SpawnGroup>>> GotMonuments = new Dictionary<string, Dictionary<string, List<SpawnGroup>>>();
        const string Font = "robotocondensed-regular.ttf";
        const string permAllowed = "monumentspawncontrol.allowed";
        bool HasPermission(string id, string perm) => permission.UserHasPermission(id, perm);

        bool newsave = false;
        void OnNewSave(string filename) => newsave = true; 

        #region SetupTakedown
        void Loaded() => permission.RegisterPermission(permAllowed, this); 

        void OnServerInitialized()
        {
            ins = this;
            foreach (BasePlayer player in BasePlayer.activePlayerList) 
                DestroyMenu(player, true); 
            LoadConfigVariables();
            GetMonuments();
            ApplySettings("", "");  
        }

        void Unload() 
        {
            ins = null;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestroyMenu(player, true);
            ApplySettings("", ""); 
        }

        void OnPlayerDisconnected(BasePlayer player) => DestroyMenu(player, true);
        void OnPlayerDeath(BasePlayer player, HitInfo info) => DestroyMenu(player, true);

        void DestroyMenu(BasePlayer player, bool all)
        {
            if (all)
                CuiHelper.DestroyUi(player, "MSCBgUI");
            CuiHelper.DestroyUi(player, "MSCMainUI");
        }
        #endregion

        [ChatCommand("MSC")]
        void MSCptions(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player.UserIDString, permAllowed) && !player.IsAdmin)
                return;

            DestroyMenu(player, true);

            MSCBgUI(player);
            MSCMainUI(player, "");
        }

        #region UI
        void MSCBgUI(BasePlayer player)
        {
            string guiString = string.Format("0.1 0.1 0.1 0.9");
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = guiString }, RectTransform = { AnchorMin = "0.2 0.05", AnchorMax = "0.8 0.95" }, CursorEnabled = true, FadeOut = 0.1f }, "Overlay", "MSCBgUI");
			elements.Add(new CuiPanel { Image = { Color = $"0 0 0 1" }, RectTransform = { AnchorMin = $"0 0.95", AnchorMax = $"0.999 1" }, CursorEnabled = true }, mainName);
			elements.Add(new CuiPanel { Image = { Color = $"0 0 0 1" }, RectTransform = { AnchorMin = $"0 0", AnchorMax = $"0.999 0.05" }, CursorEnabled = true }, mainName); 
            elements.Add(new CuiButton { Button = { Command = "CloseMSC", Color = configData.ButtonColour }, RectTransform = { AnchorMin = "0.955 0.96", AnchorMax = "0.99 0.99" }, Text = { Text = "X", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            elements.Add(new CuiLabel { Text = { Text = "Spawn Options", FontSize = 20, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.2 0.95", AnchorMax = "0.8 1" } }, mainName);
            CuiHelper.AddUi(player, elements);
        }

        void MSCMainUI(BasePlayer player, string monument)
        {
            var elements = new CuiElementContainer();
            var mainName = elements.Add(new CuiPanel { Image = { Color = "0 0 0 0" }, RectTransform = { AnchorMin = "0.2 0", AnchorMax = "0.8 0.9" }, CursorEnabled = true }, "Overlay", "MSCMainUI");
            elements.Add(new CuiElement { Parent = "MSCMainUI", Components = { new CuiRectTransformComponent { AnchorMin = "0 0", AnchorMax = "1 1" } } });

            float top = 0.86f;
            float bottom = 0.88f;

            int counter = 0;
            int groupCounter = 0;

            foreach (var entry in GotMonuments)
                groupCounter += entry.Value.Count;

            if (groupCounter == 0)
                elements.Add(new CuiLabel { Text = { Text = "No monuments were found.", FontSize = 16, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0 0.7", AnchorMax = "1 0.75" } }, mainName);
            else if (monument != string.Empty)
            {
                elements.Add(new CuiLabel { Text = { Text = $"{monument}", FontSize = 20, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = "0.2 0.95", AnchorMax = "0.8 1" } }, mainName);
                top -= 0.06f;
                bottom -= 0.06f;
                elements.Add(new CuiButton { Button = { Command = $"MSCFill {d(monument)}", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.22 {top}", AnchorMax = $"0.28 {bottom}" }, Text = { Text = "All", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"MSCEmpty {d(monument)}", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.29 {top}", AnchorMax = $"0.35 {bottom}" }, Text = { Text = "All", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"MSCDisable {d(monument)}", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.36 {top}", AnchorMax = $"0.42 {bottom}" }, Text = { Text = "All", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"MSCReset {d(monument)}", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.43 {top}", AnchorMax = $"0.49 {bottom}" }, Text = { Text = "All", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);


                elements.Add(new CuiButton { Button = { Command = $"MSCSetRespawn {d(monument)}", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.525 {top}", AnchorMax = $"0.625 {bottom}" }, Text = { Text = "Respawn Time", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"MSCSetAmount {d(monument)}", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.655 {top}", AnchorMax = $"0.755 {bottom}" }, Text = { Text = "Respawn Amount", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                elements.Add(new CuiButton { Button = { Command = $"MSCSetMax {d(monument)}", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.785 {top}", AnchorMax = $"0.885 {bottom}" }, Text = { Text = "Max Population", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

                top -= 0.03f;
                bottom -= 0.03f;

                foreach (var entry in GotMonuments[monument]) 
                {
                    top -= 0.03f;
                    bottom -= 0.03f;
                    var record = configData.GotMonuments[monument][entry.Key].CurrentSettings;
                    string color = record.enabled ? configData.ButtonColour : configData.ButtonColour2;
                    string text = record.enabled ? "Disable" : "Enable";
                    elements.Add(new CuiButton { Button = { Command = "", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.02 {top}", AnchorMax = $"0.2 {bottom}" }, Text = { Text = entry.Key, FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"MSCFill {d(monument)} {d(entry.Key)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.22 {top}", AnchorMax = $"0.28 {bottom}" }, Text = { Text = "Fill", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"MSCEmpty {d(monument)} {d(entry.Key)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.29 {top}", AnchorMax = $"0.35 {bottom}" }, Text = { Text = "Empty", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"MSCDisable {d(monument)} {d(entry.Key)}", Color = color }, RectTransform = { AnchorMin = $"0.36 {top}", AnchorMax = $"0.42 {bottom}" }, Text = { Text = text, FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"MSCReset {d(monument)} {d(entry.Key)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.43 {top}", AnchorMax = $"0.49 {bottom}" }, Text = { Text = "Reset", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

                    var respawntime = record.Population_Check_Interval;
                    if (respawntime >= Int16.MaxValue || respawntime < 0)
                        respawntime = record.Population_Check_Interval = 30;

                    elements.Add(new CuiButton { Button = { Command = $"MSCIncreaseDuration {d(monument)} {d(entry.Key)} true", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.525 {top}", AnchorMax = $"0.55 {bottom}" }, Text = { Text = "<", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiLabel { Text = { Text = $"{respawntime}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0.55 {top}", AnchorMax = $"0.6 {bottom}" } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"MSCIncreaseDuration {d(monument)} {d(entry.Key)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.6 {top}", AnchorMax = $"0.625 {bottom}" }, Text = { Text = ">", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

                    elements.Add(new CuiButton { Button = { Command = $"MSCIncreaseAmount {d(monument)} {d(entry.Key)} true", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.655 {top}", AnchorMax = $"0.68 {bottom}" }, Text = { Text = "<", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiLabel { Text = { Text = $"{record.Max_Number_To_Spawn_Per_Check}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0.68 {top}", AnchorMax = $"0.73 {bottom}" } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"MSCIncreaseAmount {d(monument)} {d(entry.Key)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.73 {top}", AnchorMax = $"0.755 {bottom}" }, Text = { Text = ">", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

                    elements.Add(new CuiButton { Button = { Command = $"MSCIncreaseMaxPop {d(monument)} {d(entry.Key)} true", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.785 {top}", AnchorMax = $"0.81 {bottom}" }, Text = { Text = "<", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                    elements.Add(new CuiLabel { Text = { Text = $"{record.Max_Population}", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter }, RectTransform = { AnchorMin = $"0.81 {top}", AnchorMax = $"0.86 {bottom}" } }, mainName);
                    elements.Add(new CuiButton { Button = { Command = $"MSCIncreaseMaxPop {d(monument)} {d(entry.Key)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.86 {top}", AnchorMax = $"0.885 {bottom}" }, Text = { Text = ">", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);

                    counter++;
                }
                elements.Add(new CuiButton { Button = { Command = $"MSCBack {d(monument)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"0.4 0.065", AnchorMax = $"0.6 0.095" }, Text = { Text = "Back", FontSize = 11, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
            }
            else
            {
                elements.Add(new CuiButton { Button = { Command = "MSCFillAll", Color = configData.ButtonColour2 }, RectTransform = { AnchorMin = $"0.4 0.86", AnchorMax = $"0.6 0.89" }, Text = { Text = "Fill All Active Spawngroups", FontSize = 14, Font = Font, Align = TextAnchor.MiddleCenter } }, mainName);
                top -= 0.06f;
                bottom -= 0.06f;
                float left = 0.07f, right = 0.18f;
                foreach (var entry in GotMonuments)
                {
                    if (counter % 4 != 0)
                    {
                        left += 0.2f;
                        right += 0.2f;
                    }
                    if (counter % 4 == 0)
                    {
                        left = 0.1f;
                        right = 0.29f;
                        top -= 0.03f;
                        bottom -= 0.03f;
                    }
                    elements.Add(new CuiButton { Button = { Command = $"MSCOpenMonument {d(entry.Key)}", Color = configData.ButtonColour }, RectTransform = { AnchorMin = $"{left} {top}", AnchorMax = $"{right} {bottom}" }, Text = { Text = entry.Key, FontSize = 11, Align = TextAnchor.MiddleCenter } }, mainName);
                    counter++;
                }
            }
            CuiHelper.AddUi(player, elements);
        }
        #endregion

        #region UICommands
        [ConsoleCommand("CloseMSC")]
        private void CloseMSC(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;
            DestroyMenu(player, true);
            ApplySettings("", "");
        }

        [ConsoleCommand("MSCOpenMonument")]
        private void MSCOpenMonument(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCOpenHandler")]
        private void MSCOpenHandler(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCBack")]
        private void MSCBack(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            DestroyMenu(player, false);
            if (arg.Args.Length == 1)
                MSCMainUI(player, "");
            else
                MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCDisable")]
        private void MSCDisable(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (arg.Args.Length == 1) 
            {
                string flag = null;
                foreach (var entry in configData.GotMonuments[u(arg.Args[0])])
                {
                    if (flag == null)
                        flag = (!entry.Value.CurrentSettings.enabled).ToString();

                    entry.Value.CurrentSettings.enabled = Convert.ToBoolean(flag);
                    if (!entry.Value.CurrentSettings.enabled)
                        ClearSpawnGroup(u(arg.Args[0]), "");
                    else
                        FillSpawnGroup(u(arg.Args[0]), "");
                }
                ApplySettings("", "");
            }
            else
            {
                bool value = configData.GotMonuments[u(arg.Args[0])][u(arg.Args[1])].CurrentSettings.enabled;
                configData.GotMonuments[u(arg.Args[0])][u(arg.Args[1])].CurrentSettings.enabled = !value;
                if (!value == false)
                    ClearSpawnGroup(u(arg.Args[0]), u(arg.Args[1]));
                else
                    FillSpawnGroup(u(arg.Args[0]), u(arg.Args[1]));
                ApplySettings(u(arg.Args[0]), u(arg.Args[1]));
            }
            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCIncreaseAmount")]
        private void MSCIncreaseAmount(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            var record = configData.GotMonuments[u(arg.Args[0])][u(arg.Args[1])].CurrentSettings;

            if (arg.Args.Length == 3)
                record.Max_Number_To_Spawn_Per_Check = Mathf.Max(record.Max_Number_To_Spawn_Per_Check - 1, 1);
            else
                record.Max_Number_To_Spawn_Per_Check++;

            record.Max_Number_To_Spawn_Per_Check = Mathf.Min(record.Max_Population, record.Max_Number_To_Spawn_Per_Check);

            ApplySettings(u(arg.Args[0]), u(arg.Args[1]));
            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCIncreaseMaxPop")]
        private void MSCIncreaseMaxPop(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            var record = configData.GotMonuments[u(arg.Args[0])][u(arg.Args[1])].CurrentSettings;

            if (arg.Args.Length == 3)
                record.Max_Population = Mathf.Max(record.Max_Population - 1, 1);
            else
                record.Max_Population++;

            record.Max_Population = Mathf.Min(record.SpawnPoints, record.Max_Population);
            record.Max_Number_To_Spawn_Per_Check = Mathf.Min(record.Max_Population, record.Max_Number_To_Spawn_Per_Check);

            ApplySettings(u(arg.Args[0]), u(arg.Args[1]));
            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCIncreaseDuration")]
        private void MSCIncreaseDuration(ConsoleSystem.Arg arg) 
        {
            var player = arg.Player();
            if (player == null)
                return;

            var record = configData.GotMonuments[u(arg.Args[0])][u(arg.Args[1])].CurrentSettings;

            if (arg.Args.Length == 3)
                record.Population_Check_Interval = Mathf.Max(record.Population_Check_Interval - 1, 1);
            else
                record.Population_Check_Interval++;

            if (record.Population_Check_Interval > Int16.MaxValue || record.Population_Check_Interval < 0)
                record.Population_Check_Interval = 30;

            ApplySettings(u(arg.Args[0]), u(arg.Args[1]));
            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCReset")]
        private void MSCReset(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (arg.Args.Length == 1)
            {
                foreach (var entry in configData.GotMonuments[u(arg.Args[0])])
                {
                    entry.Value.CurrentSettings = entry.Value.DefaultSettings.Clone();
                    ClearSpawnGroup(u(arg.Args[0]), "");
                    FillSpawnGroup(u(arg.Args[0]), "");
                }
                ApplySettings("", "");
            }
            else
            {
                configData.GotMonuments[u(arg.Args[0])][u(arg.Args[1])].CurrentSettings = configData.GotMonuments[u(arg.Args[0])][u(arg.Args[1])].DefaultSettings.Clone();
                ClearSpawnGroup(u(arg.Args[0]), u(arg.Args[1]));
                FillSpawnGroup(u(arg.Args[0]), u(arg.Args[1]));
                ApplySettings(u(arg.Args[0]), u(arg.Args[1]));
            }

            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCEmpty")]
        private void MSCEmpty(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) 
                return;

            if (arg.Args.Length == 1)
                ClearSpawnGroup(u(arg.Args[0]), "");
            else
                ClearSpawnGroup(u(arg.Args[0]), u(arg.Args[1]));

            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCFill")]
        private void MSCFill(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            if (arg.Args.Length == 1)
                FillSpawnGroup(u(arg.Args[0]), "");
            else
                FillSpawnGroup(u(arg.Args[0]), u(arg.Args[1]));

            DestroyMenu(player, false);
            MSCMainUI(player, u(arg.Args[0]));
        }

        [ConsoleCommand("MSCFillAll")]
        private void MSCFillAll(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null)
                return;

            foreach (var monument in GotMonuments)
                foreach (var set in monument.Value)
                    foreach (var group in set.Value.Where(x => x.enabled == true))
                        FillSpawnGroup(monument.Key, set.Key);
        }
        #endregion

        #region methods
        string d(string i) => i.Replace(" ", "-"); 
        string u(string i) => i.Replace("-", " ");

        void ClearSpawnGroup(string monument, string group)
        {
            if (GotMonuments.ContainsKey(monument))
                foreach (var entry in GotMonuments[monument].Where(x => group == string.Empty || x.Key == group))
                    foreach (var sg in entry.Value)
                        sg.Clear();
        }

        void FillSpawnGroup(string monument, string group)
        {
            if (GotMonuments.ContainsKey(monument))
                foreach (var entry in GotMonuments[monument].Where(x => group == string.Empty || x.Key == group)) 
                    foreach (var sg in entry.Value)
                        sg.Fill();
        }

        void GetMonuments()
        {
            var Resets = UnityEngine.Object.FindObjectsOfType<PuzzleReset>();
            foreach (var monumentInfo in TerrainMeta.Path.Monuments.OrderBy(x => x.displayPhrase.english)) 
            {
                var displayPhrase = monumentInfo.displayPhrase.english.Replace("\n", string.Empty);
                GameObject gobject = monumentInfo.gameObject;

                var pos = monumentInfo.gameObject.transform.position;
                var sgs = new List<SpawnGroup>();

                if (displayPhrase != string.Empty)
                {
                    if (displayPhrase.Contains("Rig") || displayPhrase.Contains("Lab"))
                        Vis.Components<SpawnGroup>(gobject.transform.position, 50, sgs);
                    else
                        sgs = monumentInfo.GetComponentsInChildren<SpawnGroup>().ToList();

                    foreach (var obje in sgs)
                    {
                        if (obje?.spawnPoints == null || obje.spawnPoints.Count() == 0)
                            continue;

                        string name = obje.name.ToLower();
                        if (Match(name))
                        {
                            if (obje.respawnDelayMax == Single.PositiveInfinity) 
                                continue;

                            foreach (var reset in Resets)
                                if (obje != null && reset?.respawnGroups != null && reset.respawnGroups.ToList().Contains(obje))
                                    continue;

                            if (!GotMonuments.ContainsKey(displayPhrase))
                            {
                                GotMonuments.Add(displayPhrase, new Dictionary<string, List<SpawnGroup>>());
                                if (!configData.GotMonuments.ContainsKey(displayPhrase))
                                    configData.GotMonuments.Add(displayPhrase, new Dictionary<string, MonumentSettings>());
                            }

                            var settings = new Settings();
                            if (!GotMonuments[displayPhrase].ContainsKey(obje.name))
                            {
                                GotMonuments[displayPhrase].Add(obje.name, new List<SpawnGroup>()); 
                                settings = new Settings()
                                {
                                    enabled = obje.enabled, 
                                    SpawnPoints = obje.spawnPoints.Count(),
                                    Max_Population = obje.maxPopulation,
                                    Max_Number_To_Spawn_Per_Check = obje.maxPopulation,
                                    Population_Check_Interval = Mathf.Max(Mathf.Floor(obje.respawnDelayMax / 60), 1)
                                };

                                if (!configData.GotMonuments[displayPhrase].ContainsKey(obje.name))
                                {
                                    configData.GotMonuments[displayPhrase].Add(obje.name, new MonumentSettings()
                                    {
                                        CurrentSettings = settings,
                                        DefaultSettings = settings
                                    });
                                }

                                if (newsave) 
                                    configData.GotMonuments[displayPhrase][obje.name].DefaultSettings = settings;
                            }
                            configData.GotMonuments[displayPhrase][obje.name].CurrentSettings.SpawnPoints = obje.spawnPoints.Count();
                            GotMonuments[displayPhrase][obje.name].Add(obje);
                        }
                    }
                }
            }
        }

        List<string> names = new List<string>() { "red", "blue", "green", "crate", "barrel", "food", "card", "ore", "elite", "diesel" };
        bool Match(string name)
        {
            foreach (var entry in names)
                if (name.Contains(entry))
                    return true;
            return false;
        }

        void ApplySettings(string m, string g, bool fix = false) 
        {
            if (configData == null)
                return;
            if (m != "")
            {
                var record = configData.GotMonuments[m][g]; 
                var s = GotMonuments[m][g];
                SetValues(record.CurrentSettings, s);
            }
            else
            { 
                foreach (var mon in configData.GotMonuments.ToDictionary(pair => pair.Key, pair => pair.Value))
                {
                    if (!GotMonuments.ContainsKey(mon.Key))
                        continue;

                    foreach (var handler in mon.Value)
                        if (GotMonuments[mon.Key].ContainsKey(handler.Key))
                            SetValues(configData.GotMonuments[mon.Key][handler.Key].CurrentSettings, GotMonuments[mon.Key][handler.Key], fix);
                }
                SaveConfig(configData);
            }
        }

        void SetValues(Settings s, List<SpawnGroup> gs, bool fix = false)
        {
            foreach (var sg in gs) 
            {
                if (sg == null) 
                    continue; 

                sg.enabled = s.enabled;
                sg.maxPopulation = s.Max_Population;
                sg.numToSpawnPerTickMin = sg.numToSpawnPerTickMax = s.Max_Number_To_Spawn_Per_Check;
                if (!fix)
                    sg.respawnDelayMin = sg.respawnDelayMax = s.Population_Check_Interval * 60;
                else
                {
                    sg.respawnDelayMin = sg.respawnDelayMax = 1800;
                    s.Population_Check_Interval = 30;
                }

                sg?.spawnClock?.events?.Clear();
                //sg?.spawnClock?.Add(sg.GetSpawnDelta(), sg.GetSpawnVariance(), new Action(sg.Spawn));
                sg?.spawnClock?.Add(sg.GetSpawnDelta(), sg.GetSpawnVariance(), new Action(() => Spawn(sg)));
            }
        }

        void Spawn(SpawnGroup sg)
        {
            Vector3 vector3;
            Quaternion quaternion;
            var numToSpawn = Mathf.Min(sg.numToSpawnPerTickMax, sg.maxPopulation - sg.currentPopulation);
            for (int i = 0; i < numToSpawn; i++)
            {
                GameObjectRef prefab = GetPrefab(sg);
                if (prefab != null && !string.IsNullOrEmpty(prefab.guid))
                {
                    BaseSpawnPoint spawnPoint = GetSpawnPoint(sg, prefab, out vector3, out quaternion);
                    if (spawnPoint)
                    {
                        BaseEntity baseEntity = GameManager.server.CreateEntity(prefab.resourcePath, vector3, quaternion, false);
                        if (baseEntity)
                        {
                            if (baseEntity.enableSaving && !(spawnPoint is SpaceCheckingSpawnPoint))
                            {
                                baseEntity.enableSaving = false;
                            }
                            baseEntity.gameObject.AwakeFromInstantiate();
                            baseEntity.Spawn();
                            //sg.PostSpawnProcess(baseEntity, spawnPoint);
                            SpawnPointInstance spawnPointInstance = baseEntity.gameObject.AddComponent<SpawnPointInstance>();
                            spawnPointInstance.parentSpawnPointUser = sg;
                            spawnPointInstance.parentSpawnPoint = spawnPoint;
                            spawnPointInstance.Notify();
                        }
                    }
                }
            }
        }

        protected GameObjectRef GetPrefab(SpawnGroup sg)
        {
            GameObjectRef gameObjectRef;
            float single = (float)sg.prefabs.Sum<SpawnGroup.SpawnEntry>((SpawnGroup.SpawnEntry x) => {
                if (sg.preventDuplicates && sg.HasSpawned(x.prefab.resourceID))
                {
                    return 0;
                }
                return x.weight;
            });
            if (single == 0f)
            {
                return null;
            }
            float single1 = UnityEngine.Random.Range(0f, single);
            List<SpawnGroup.SpawnEntry>.Enumerator enumerator = sg.prefabs.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    SpawnGroup.SpawnEntry current = enumerator.Current;
                    float single2 = single1 - (float)((!sg.preventDuplicates || !sg.HasSpawned(current.prefab.resourceID) ? current.weight : 0));
                    single1 = single2;
                    if (single2 > 0f)
                    {
                        continue;
                    }
                    gameObjectRef = current.prefab;
                    return gameObjectRef;
                }
                return sg.prefabs[sg.prefabs.Count - 1].prefab;
            }
            finally
            {
                ((IDisposable)enumerator).Dispose();
            }
            return gameObjectRef;
        }

        protected virtual BaseSpawnPoint GetSpawnPoint(SpawnGroup sg, GameObjectRef prefabRef, out Vector3 pos, out Quaternion rot)
        {
            BaseSpawnPoint baseSpawnPoint = null;
            pos = Vector3.zero;
            rot = Quaternion.identity;
            int num = UnityEngine.Random.Range(0, (int)sg.spawnPoints.Length);
            int num1 = 0;
            while (num1 < (int)sg.spawnPoints.Length)
            {
                BaseSpawnPoint baseSpawnPoint1 = sg.spawnPoints[(num + num1) % (int)sg.spawnPoints.Length];
                if (baseSpawnPoint1 == null || !baseSpawnPoint1.IsAvailableTo(prefabRef.Get()) || BaseNetworkable.HasCloseConnections(baseSpawnPoint1.transform.position, 30)) // replace with a height agnostic check.
                {
                    num1++;
                }
                else
                {
                    baseSpawnPoint = baseSpawnPoint1;
                    break;
                }
            }
            if (baseSpawnPoint)
            {
                baseSpawnPoint.GetLocation(out pos, out rot);
            }
            return baseSpawnPoint;
        }

        #endregion

        #region config
        private ConfigData configData;
        class ConfigData
        {
            public string ButtonColour = "0.7 0.32 0.17 1";
            public string ButtonColour2 = "0.4 0.1 0.1 1";
            public Dictionary<string, Dictionary<string, MonumentSettings>> GotMonuments = new Dictionary<string, Dictionary<string, MonumentSettings>>();
        }

        public class MonumentSettings
        {
            public Settings CurrentSettings = new Settings();
            public Settings DefaultSettings = new Settings();
        }
        public class Settings
        {
            public Settings Clone()
            {
                return MemberwiseClone() as Settings;
            }
            public bool enabled = true; 
            public int SpawnPoints = 0;
            public int Max_Population = 10;
            public int Max_Number_To_Spawn_Per_Check = 10;
            public float Population_Check_Interval = 30;
        }

        private void LoadConfigVariables()  
        {
            configData = Config.ReadObject<ConfigData>();
            SaveConfig(configData);
        }

        protected override void LoadDefaultConfig()
        {
            LoadConfigVariables();
            Puts("Creating new config file."); 
        }

        void SaveConfig(ConfigData config) => Config.WriteObject(config, true);
        #endregion
    }
}


 