using System;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;


// ReSharper disable SpecifyACultureInStringConversionExplicitly

// ReSharper disable once CheckNamespace
namespace Oxide.Plugins
{
    [Info("Survey Info", "MJSU", "0.0.1")]
    [Description("Displays Loot from survey charges")]
    // ReSharper disable once UnusedMember.Global
    class SurveyInfo : RustPlugin
    {
        [PluginReference]
        // ReSharper disable once InconsistentNaming
        Plugin GatherManager;

        #region Class Fields
        private StoredData _storedData; //Data File
        private PluginConfig _pluginConfig; //Config File

        private const string UsePermission = "surveyinfo.use";

        private readonly Hash<ulong, int> _playerSurveyDataUiPage = new Hash<ulong, int>(); //Determines what page on the survey data ui the player is on
        private readonly Hash<ulong, int> _playerGiveToUiPage = new Hash<ulong, int>(); //Determines what page on the give to ui the player is on
        private readonly Hash<int, SurveyData> _activeSurveyCharges = new Hash<int, SurveyData>();

        private Timer _saveTimer; //Timer to save the data

        private double _bestPossibleSurveyScore = 25; //Default is 25 unless GatherManager is used
        private enum SurveyLootItemIdEnum { Stones = -892070738, MetalOre = -1059362949, MetalFrag = 688032252, SulfurOre = 889398893, HighQualityMetal = 2133577942 }
        #endregion

        #region Plugin Loading & Setup
        // ReSharper disable once UnusedMember.Local
        private void Loaded()
        {
            LoadLang();

            _pluginConfig = ConfigOrDefault(Config.ReadObject<PluginConfig>());
            if (_pluginConfig.Prefix == null) PrintError("Loading config file failed. Using default config");
            else Config.WriteObject(_pluginConfig, true);

            _storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("SurveyInfo");

            permission.RegisterPermission(UsePermission, this);

            //Assigned so it can be destoryed at unload. Repeats max integer times
            _saveTimer = timer.Repeat(_pluginConfig.SaveIntervalInSeconds, int.MaxValue, () =>
            {
                Puts("Saving SurveyInfo Data");
                Interface.Oxide.DataFileSystem.WriteObject("SurveyInfo", _storedData);
            });
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Register the Lang Dictionary
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void LoadLang()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "You do not have permission to use this command",
                ["Lookup"] = "Lookup",
                ["Remove"] = "Remove",
                ["GiveTo"] = "Give To",
                ["Close"] = "Close",
                ["PrevPage"] = "Prev Page",
                ["NextPage"] = "Next Page",
                ["Id"] = "ID",
                ["Stones"] = "Stones",
                ["Metal"] = "Metal",
                ["Sulfur"] = "Sulfur",
                ["High"] = "High",
                ["Quality"] = "Quality",
                ["Score"] = "Score",
                ["Ore"] = "Ore",
                ["Frags"] = "Frags",
                ["Survey"] = "Survey"
            }, this);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// load the default config for this plugin
        /// </summary>
        /// ////////////////////////////////////////////////////////////////////////
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
            Config.WriteObject(ConfigOrDefault(null), true);
        }

        ////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Uses the values passed in from config. If any values are null it updates them with default values
        /// </summary>
        /// <param name="config">Config that has been loaded or null</param>
        /// <returns>Config using values passed in from config default values</returns>
        /// ////////////////////////////////////////////////////////////////////////
        private PluginConfig ConfigOrDefault(PluginConfig config)
        {
            return new PluginConfig
            {
                Prefix = config?.Prefix ?? "[<color=yellow>Survey Info</color>]",
                SaveIntervalInSeconds = config?.SaveIntervalInSeconds ?? 600f,
                SurveyIdDisplayLengthInSeconds = config?.SurveyIdDisplayLengthInSeconds ?? 150f,
                UiColors = config?.UiColors ?? new UIColors(),
                SurveyUiConfig = config?.SurveyUiConfig ??  new SurveyDataUIConfig(),
                GiveToUiConfig = config?.GiveToUiConfig ?? new GiveToUIConfig(),
            };
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// New Map save. Wipe all the Survey Data
        /// </summary>
        /// <param name="name"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void OnNewSave(string name)
        {
            PrintWarning("Map wipe detected - Wiping Survey Info Data");
            _storedData = new StoredData();
            Interface.Oxide.DataFileSystem.WriteObject("SurveyInfo", _storedData);
        }

        // ReSharper disable once UnusedMember.Local
        /////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Player left the server remove their page data and destroy their UI
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        /// <param name="player"></param>
        private void OnPlayerDisconnected(BasePlayer player)
        {
            _playerGiveToUiPage.Remove(player.userID);
            _playerSurveyDataUiPage.Remove(player.userID);
            CuiHelper.DestroyUi(player, SurveyContainerName);
            CuiHelper.DestroyUi(player, GiveToContainerName);
            CuiHelper.DestroyUi(player, MouseBugFixContainerName);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload() => RemovePluginResources();

        // ReSharper disable once UnusedMember.Local
        private void OnServerShutdown() => RemovePluginResources();

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// On Unload or server shutdown
        /// Destroy the plugin timer, and any open GUI and save the data file
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void RemovePluginResources()
        {
            _saveTimer?.Destroy();
            Interface.Oxide.DataFileSystem.WriteObject("SurveyInfo", _storedData);
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, SurveyContainerName);
                CuiHelper.DestroyUi(player, GiveToContainerName);
                CuiHelper.DestroyUi(player, MouseBugFixContainerName);
            }
        }

        #region Gather Manager Setup
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Recalculates the BestPossibleSurveyScore using the data contained in gather manager
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            // If gather manager exists using it to calculate the best score. Load the Survey Resource Modifiers from it's config
            if (GatherManager == null) return;

            Dictionary<string, object> defaultSurveyResourceModifiers = new Dictionary<string, object>();
            Dictionary<string, object> configSurveyResourceModifiers = GetConfigValue(GatherManager, "Options", "SurveyResourceModifiers", defaultSurveyResourceModifiers);
            Dictionary<string, float> surveyResourceModifiers = new Dictionary<string, float>();

            //Load the Data from the GatherManager config into SurveyResourceModifiers
            foreach (var entry in configSurveyResourceModifiers)
            {
                float rate;
                if (!float.TryParse(entry.Value.ToString(), out rate)) continue;
                surveyResourceModifiers.Add(entry.Key, rate);
            }

            //If GatherManager is present but the SurveyResourceModifier contained no entrys don't update the score
            if (surveyResourceModifiers.Count == 0) return;

            //Loop over all the items and calculate their new survey score
            int newBestScore = 0;
            foreach (SurveyLootItemIdEnum item in Enum.GetValues(typeof(SurveyLootItemIdEnum)))
            {
                double gatherManagerMulitiplier = 1;
                float val;
                string itemName = ItemManager.FindItemDefinition((int)item).displayName.english;

                if (surveyResourceModifiers.TryGetValue(itemName, out val))
                    gatherManagerMulitiplier = val;
                else if (surveyResourceModifiers.TryGetValue("*", out val))
                    gatherManagerMulitiplier = val;

                if (item != SurveyLootItemIdEnum.HighQualityMetal) //default max amount of 5
                {
                    newBestScore += (int)(gatherManagerMulitiplier * 5);
                }
                else //HQM default max of 1
                {
                    newBestScore += (int)(gatherManagerMulitiplier) * 5;
                }

                //All items best score is 5 by default
            }

            _bestPossibleSurveyScore = newBestScore;
        }

        #endregion

        #endregion

        #region Oxide Hooks
        // ReSharper disable once UnusedMember.Local
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add Item from survey charge to the data file based on the survey charge intance id
        /// </summary>
        /// <param name="survey">Charge that was thrown</param>
        /// <param name="item">Item that came from the charge</param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void OnSurveyGather(SurveyCharge survey, Item item)
        {
            Hash<int, SurveyItem> surveyItems = _activeSurveyCharges[survey.GetInstanceID()].Items;

            int itemId = item.info.itemid;
            if (surveyItems[itemId] != null) //The item exists in the items hash
            {
                surveyItems[itemId].Amount += item.amount;
            }
            else //Item doesn't exist. Add the items
            {
                surveyItems[itemId] = new SurveyItem(item.info.displayName.translated, item.amount);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Detect when a survey charge is thrown and setup the data file with an entry for it
        /// Set the final location of the charge 4.75 seconds after throws
        /// Calculate the final numbers and display the items from the charge 5.25 seconds after thrown
        /// </summary>
        /// <param name="player"></param>
        /// <param name="entity"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        // ReSharper disable once UnusedMember.Local
        private void OnExplosiveThrown(BasePlayer player, BaseEntity entity)
        {
            if (!(entity is SurveyCharge)) return; //Make sure we are dealing with a survey charge
            SurveyData data = new SurveyData(entity.GetInstanceID()); //Create a SurveyData
            _activeSurveyCharges.Add(entity.GetInstanceID(), data);

            data.Location = new Location(entity.CenterPoint()); //Save where it was throw so if the server lags it Prevents Null Pointer Exception
            int entityInstanceId = entity.GetInstanceID(); //Entity will be null later. Save the instance id to remove from _activeSurveyCharges

            timer.Once(4.5f, () => // Set the SurveyData survey location to the location of the survey charge before exploding
            {
                if (!entity.isDestroyed) data.Location = new Location(entity.transform.position);
            });

            timer.Once(5.5f, () => //After the survey charge explodes display all the items that came out of it
            {
                if (data.Items.Count > 0) //Check if charge produced any tiems
                {
                    data.SurveyId = _storedData.NextChargeId++; //Set survey id to the SurveyData internal id system

                    //Calculate the score of the survey charge
                    float score = 0f;
                    foreach (KeyValuePair<int, SurveyItem> item in data.Items)
                    {
                        if (item.Key != (int)SurveyLootItemIdEnum.HighQualityMetal)
                        {
                            score += item.Value.Amount;
                        }
                        else //HQM has a score multiplier of 5
                        {
                            score += 5 * item.Value.Amount;
                        }
                    }
                    data.Score = (float)((score / _bestPossibleSurveyScore) * 100);

                    if (HasPermission(player, UsePermission)) //If the player has permission display info to the user
                    {
                        DisplaySurveyLoot(player, data);
                        DrawSurveyIdAtSurveyLocation(data, player);
                    }

                    if (!_storedData.SurveyInfo.ContainsKey(player.userID)) _storedData.SurveyInfo.Add(player.userID, new Hash<int, SurveyData>());
                    _storedData.SurveyInfo[player.userID][data.SurveyId] = data; //Save survey charge to the data hash
                }

                _activeSurveyCharges.Remove(entityInstanceId); //remove the survey data
            });
        }
        #endregion

        #region Chat Command
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Open the Survey Info GUI if the player has permission
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        //////////////////////////////////////////////////////////////////////////////////////
        [ChatCommand("si")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void SurveyInfoChatCommand(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, UsePermission))
            {
                PrintToChat(player, Lang("NoPermission", player.UserIDString));
                return;
            }
            OpenUi(player);
        }
        #endregion

        #region Survey Chat Display Methods
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Display the information from the survey charge in chat
        /// </summary>
        /// <param name="player"></param>
        /// <param name="data"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void DisplaySurveyLoot(BasePlayer player, SurveyData data)
        {
            PrintToChat(player, $"{_pluginConfig.Prefix} {Lang("Id", player.UserIDString)}: {data.SurveyId} - {Lang("Score", player.UserIDString)}: {data.Score}% {new string('-', 50)}");

            foreach (KeyValuePair<int, SurveyItem> item in data.Items)
            {
                PrintToChat(player, $"{item.Value.Amount}x {item.Value.DisplayName}");
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Draw the survey id on the players screen at the set location of the survey charge
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="player"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void DrawSurveyIdAtSurveyLocation(SurveyData owner, BasePlayer player)
        {
            Color color = ColorEx.Parse(_pluginConfig.UiColors.IdDisplay);
            player.SendConsoleCommand("ddraw.text", _pluginConfig.SurveyIdDisplayLengthInSeconds, color, owner.Location.GetLocation(), owner.SurveyId.ToString());
        }
        #endregion

        #region Helper Methods
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to Exel80 - code from EasyVote
        /// </summary>
        /// <param name="key"></param>
        /// <param name="id"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ///  //////////////////////////////////////////////////////////////////////////////////////
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Checks if the user has the given permissions
        /// </summary>
        /// <param name="player">Player to be checked</param>
        /// <param name="perm">Permission to check for</param>
        /// <returns></returns>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private bool HasPermission(BasePlayer player, string perm) => permission.UserHasPermission(player.UserIDString, perm);
        #endregion

        #region Gather Manager Config Loading
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Get's the GatherManager config for use in SurveyInfo
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="plugin"></param>
        /// <param name="category"></param>
        /// <param name="setting"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private T GetConfigValue<T>(Plugin plugin, string category, string setting, T defaultValue)
        {
            var data = plugin.Config[category] as Dictionary<string, object>;
            object value;
            if (data == null)
            {
                data = new Dictionary<string, object>();
                plugin.Config[category] = data;
            }
            if (data.TryGetValue(setting, out value)) return (T)Convert.ChangeType(value, typeof(T));
            value = defaultValue;
            data[setting] = value;
            return (T)Convert.ChangeType(value, typeof(T));
        }
        #endregion

        #region UI
        private const string SurveyContainerName = "SurveyInfo_SurveyContainer";
        private const string GiveToContainerName = "SurveyInfo_GiveToContainer";
        private const string MouseBugFixContainerName = "SurveyInfo_KeepMouseFromResetting";

        #region Survey Data UI
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Opens the Data UI for the player
        /// </summary>
        /// <param name="player"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void OpenUi(BasePlayer player)
        {
            _playerSurveyDataUiPage[player.userID] = 0;

            InitializeDataUi(player);

            CuiElementContainer mouseBugFixContainer = UICreator.CreateElementContainer(MouseBugFixContainerName, "0 0 0 0", "0 0", ".01 .01", 0f, 0f);
            CuiHelper.AddUi(player, mouseBugFixContainer);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Initialize and Load the GUI and destroy the old one if present
        /// </summary>
        /// <param name="player">Player who executed /si</param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void InitializeDataUi(BasePlayer player)
        {
            CuiElementContainer container = UICreator.CreateElementContainer(SurveyContainerName, _pluginConfig.UiColors.SurveyDataContainer, _pluginConfig.SurveyUiConfig.SurveyDataContainerMin, _pluginConfig.SurveyUiConfig.SurveyDataContainerMax, .05f, .05f);


            CreateSurveyDataUiHeader(ref container, player);
            CreateSurveyDataUi(ref container, player);

            CuiHelper.DestroyUi(player, GiveToContainerName);
            CuiHelper.DestroyUi(player, SurveyContainerName);

            CuiHelper.AddUi(player, container);  
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Create the header for the data UI
        /// </summary>
        /// <param name="container">Container for the header</param>
        /// <param name="player">player who /si</param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void CreateSurveyDataUiHeader(ref CuiElementContainer container, BasePlayer player)
        {
            SurveyDataUIConfig conf = _pluginConfig.SurveyUiConfig; //Provide quick access to the SurveyDataUIConfig
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Id", player.UserIDString), conf.SurveyDataTextSize, conf.LabelIdXMin + " " + conf.SurveyDataTopHeadingYMin, conf.LabelIdXMax + " " + conf.SurveyDataTopHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Stones", player.UserIDString), conf.SurveyDataTextSize, conf.LabelStonesXMin + " " + conf.SurveyDataTopHeadingYMin, conf.LabelStonesXMax + " " + conf.SurveyDataTopHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Metal", player.UserIDString), conf.SurveyDataTextSize, conf.LabelMetalOreXMin + " " + conf.SurveyDataTopHeadingYMin, conf.LabelMetalOreXMax + " " + conf.SurveyDataTopHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Ore", player.UserIDString), conf.SurveyDataTextSize, conf.LabelMetalOreXMin + " " + conf.SurveyDataMiddleHeadingYMin, conf.LabelMetalOreXMax + " " + conf.SurveyDataMiddleHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Metal", player.UserIDString), conf.SurveyDataTextSize, conf.LabelMetalFragXMin + " " + conf.SurveyDataTopHeadingYMin, conf.LabelMetalFragXMax + " " + conf.SurveyDataTopHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Frags", player.UserIDString), conf.SurveyDataTextSize, conf.LabelMetalFragXMin + " " + conf.SurveyDataMiddleHeadingYMin, conf.LabelMetalFragXMax + " " + conf.SurveyDataMiddleHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Sulfur", player.UserIDString), conf.SurveyDataTextSize, conf.LabelSulfurOreXMin + " " + conf.SurveyDataTopHeadingYMin, conf.LabelSulfurOreXMax + " " + conf.SurveyDataTopHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Ore", player.UserIDString), conf.SurveyDataTextSize, conf.LabelSulfurOreXMin + " " + conf.SurveyDataMiddleHeadingYMin, conf.LabelSulfurOreXMax + " " + conf.SurveyDataMiddleHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("High", player.UserIDString), conf.SurveyDataTextSize, conf.LabelHqmXMin + " " + conf.SurveyDataTopHeadingYMin, conf.LabelHqmXMax + " " + conf.SurveyDataTopHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Quality", player.UserIDString), conf.SurveyDataTextSize, conf.LabelHqmXMin + " " + conf.SurveyDataMiddleHeadingYMin, conf.LabelHqmXMax + " " + conf.SurveyDataMiddleHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Metal", player.UserIDString), conf.SurveyDataTextSize, conf.LabelHqmXMin + " " + conf.SurveyDataBottomHeadingYMin, conf.LabelHqmXMax + " " + conf.SurveyDataBottomHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Survey", player.UserIDString), conf.SurveyDataTextSize, conf.LabelScoreXMin + " " + conf.SurveyDataTopHeadingYMin, conf.LabelScoreXMax + " " + conf.SurveyDataTopHeadingYMax);
            UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, Lang("Score", player.UserIDString), conf.SurveyDataTextSize, conf.LabelScoreXMin + " " + conf.SurveyDataMiddleHeadingYMin, conf.LabelScoreXMax + " " + conf.SurveyDataMiddleHeadingYMax);
            UICreator.CreateButton(ref container, SurveyContainerName, _pluginConfig.UiColors.ButtonClose, Lang("Close", player.UserIDString), conf.SurveyDataTextSize, conf.ButtonCloseMin, conf.ButtonCloseMax, "si.close");
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Create the survey data inside the data UI
        /// Add buttons as necessary
        /// </summary>
        /// <param name="container">Container for the data</param>
        /// <param name="player">player who /si</param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void CreateSurveyDataUi(ref CuiElementContainer container, BasePlayer player)
        {
            List<SurveyData> playerData = _storedData.SurveyInfo[player.userID]?.Values.ToList();
            if (playerData == null) return;
            playerData.Sort((x, y) => x.Score.CompareTo(y.Score) * -1); //Sort the data by score. Highest first
            SurveyDataUIConfig conf = _pluginConfig.SurveyUiConfig; //Provide quick access to the SurveyDataUIConfig
            int recordStartIndex = _playerSurveyDataUiPage[player.userID] * conf.SurveyDataRecordsPerPage;
            int recordEndIndex = playerData.Count >= recordStartIndex + conf.SurveyDataRecordsPerPage ? recordStartIndex + conf.SurveyDataRecordsPerPage : playerData.Count;
            int row = 0;

            for (int recordIndex = recordStartIndex; recordIndex < recordEndIndex; recordIndex++)
            {
                string yMin = (conf.SurveyDataRecordStartingYMin - row * conf.SurveyDataRecordYSpacing).ToString();
                string yMax = (conf.SurveyDataRecordStartingYMax - row * conf.SurveyDataRecordYSpacing).ToString();
                UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, playerData[recordIndex].SurveyId.ToString(), conf.SurveyDataTextSize, conf.LabelIdXMin + " " + yMin, conf.LabelIdXMax + " " + yMax);
                UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, playerData[recordIndex].GetAmountByItemId((int)SurveyLootItemIdEnum.Stones).ToString(), conf.SurveyDataTextSize, conf.LabelStonesXMin + " " + yMin, conf.LabelStonesXMax + " " + yMax);
                UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, playerData[recordIndex].GetAmountByItemId((int)SurveyLootItemIdEnum.MetalOre).ToString(), conf.SurveyDataTextSize, conf.LabelMetalOreXMin + " " + yMin, conf.LabelMetalOreXMax + " " + yMax);
                UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, playerData[recordIndex].GetAmountByItemId((int)SurveyLootItemIdEnum.MetalFrag).ToString(), conf.SurveyDataTextSize, conf.LabelMetalFragXMin + " " + yMin, conf.LabelMetalFragXMax + " " + yMax);
                UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, playerData[recordIndex].GetAmountByItemId((int)SurveyLootItemIdEnum.SulfurOre).ToString(), conf.SurveyDataTextSize, conf.LabelSulfurOreXMin + " " + yMin, conf.LabelSulfurOreXMax + " " + yMax);
                UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, playerData[recordIndex].GetAmountByItemId((int)SurveyLootItemIdEnum.HighQualityMetal).ToString(), conf.SurveyDataTextSize, conf.LabelHqmXMin + " " + yMin, conf.LabelHqmXMax + " " + yMax);
                UICreator.CreateLabel(ref container, SurveyContainerName, _pluginConfig.UiColors.Label, playerData[recordIndex].Score + "%", conf.SurveyDataTextSize, conf.LabelScoreXMin + " " + yMin, conf.LabelScoreXMax + " " + yMax);
                UICreator.CreateButton(ref container, SurveyContainerName, _pluginConfig.UiColors.ButtonLookup, Lang("Lookup", player.UserIDString), conf.SurveyDataTextSize, conf.ButtonLookupXMin + " " + yMin, conf.ButtonLookupXMax + " " + yMax, $"si.lookup " + playerData[recordIndex].SurveyId);
                UICreator.CreateButton(ref container, SurveyContainerName, _pluginConfig.UiColors.ButtonRemove, Lang("Remove", player.UserIDString), conf.SurveyDataTextSize, conf.ButtonRemoveXMin + " " + yMin, conf.ButtonRemoveXMax + " " + yMax, $"si.remove " + playerData[recordIndex].SurveyId);
                UICreator.CreateButton(ref container, SurveyContainerName, _pluginConfig.UiColors.ButtonGiveTo, Lang("GiveTo", player.UserIDString), conf.SurveyDataTextSize, conf.ButtonGiveToXMin + " " + yMin, conf.ButtonGiveToXMax + " " + yMax, $"si.opengiveto  " + playerData[recordIndex].SurveyId);
                row++;
            }

            if (recordStartIndex != 0)
            {
                UICreator.CreateButton(ref container, SurveyContainerName, _pluginConfig.UiColors.ButtonPrevPage, Lang("PrevPage", player.UserIDString), conf.SurveyDataTextSize, conf.ButtonDataPrevPageMin, conf.ButtonDataPrevPageMax, "si.prevpage");
            }

            if (recordEndIndex != playerData.Count)
            {
                UICreator.CreateButton(ref container, SurveyContainerName, _pluginConfig.UiColors.ButtonNextPage, Lang("NextPage", player.UserIDString), conf.SurveyDataTextSize, conf.ButtonDataNextPageMin, conf.ButtonDataNextPageMax, "si.nextpage");
            }
        }
        #endregion

        #region Give To UI
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Open the Give To UI
        /// Destroy the old one if present
        /// </summary>
        /// <param name="player">player who opened it</param>
        /// <param name="recordId">Record that is to be given</param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void InitializeGiveToUi(BasePlayer player, int recordId)
        {
            _playerGiveToUiPage[player.userID] = 0;

            CuiElementContainer giveToContainer = UICreator.CreateElementContainer(GiveToContainerName, _pluginConfig.UiColors.GiveToContainer, _pluginConfig.GiveToUiConfig.GiveToContainerMin, _pluginConfig.GiveToUiConfig.GiveToContainerMax, .05f, .05f);

            CreatePlayerListUi(ref giveToContainer, player, recordId);

            CuiHelper.DestroyUi(player, GiveToContainerName);
            CuiHelper.AddUi(player, giveToContainer);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Add the list of online players to the give to ui container
        /// </summary>
        /// <param name="container">Container for the UI</param>
        /// <param name="player">Player who opened it</param>
        /// <param name="recordId">Record Id to be given</param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private void CreatePlayerListUi(ref CuiElementContainer container, BasePlayer player, int recordId)
        {
            List<BasePlayer> activePlayerList = BasePlayer.activePlayerList;
            GiveToUIConfig conf = _pluginConfig.GiveToUiConfig; //Provide easy access to the GiveToUIConfig
            int playerStartIndex = _playerGiveToUiPage[player.userID] * conf.GiveToPlayersPerPage;
            int playerEndIndex = activePlayerList.Count >= playerStartIndex + conf.GiveToPlayersPerPage ? playerStartIndex + conf.GiveToPlayersPerPage : activePlayerList.Count;
            int row = 0;
            int col = 0;

            for (int playerIndex = playerStartIndex; playerIndex < playerEndIndex; playerIndex++)
            {
                string yMin = (conf.PlayerButtonStartPositionYMin - (row * conf.PlayerButtonYSpacing)).ToString();
                string yMax = (conf.PlayerButtonStartPositionYMax - (row * conf.PlayerButtonYSpacing)).ToString();
                string xMin = (conf.PlayerButtonStartPositionXMin + (col * conf.PlayerXSpacing)).ToString();
                string xMax = (conf.PlayerButtonStartPositionXMax + (col * conf.PlayerXSpacing)).ToString();

                UICreator.CreateButton(ref container, GiveToContainerName, _pluginConfig.UiColors.ButtonPlayers, activePlayerList[playerIndex].displayName, conf.GiveToPlayersTextSize, xMin + " " + yMin, xMax + " " + yMax, "si.sigiveto " + activePlayerList[playerIndex].userID + " " + recordId);

                col = ++col % conf.GiveToPlayersPerRow; 
                if (col == 0) row++; //increment row each time col == 0
            }

            if (playerStartIndex != 0)
            {
                UICreator.CreateButton(ref container, GiveToContainerName, _pluginConfig.UiColors.ButtonPrevPage, Lang("PrevPage", player.UserIDString), conf.GiveToPlayerPageTextSize, conf.ButtonGiveToPrevPageMin, conf.ButtonGiveToPrevPageMax, "si.giverevpage " + recordId);
            }

            if (playerEndIndex != activePlayerList.Count)
            {
                UICreator.CreateButton(ref container, GiveToContainerName, _pluginConfig.UiColors.ButtonNextPage, Lang("NextPage", player.UserIDString), conf.GiveToPlayerPageTextSize, conf.ButtonGiveToNextPageMin, conf.ButtonGiveToNextPageMax, "si.givenextpage " + recordId);
            }
        }
        #endregion

        #endregion

        #region UI Console Commands
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Looks up the survey id and displays it to the player
        /// </summary>
        /// <param name="arg"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.lookup")]
        // ReSharper disable once UnusedMember.Local
        private void SurveyInfoLookupConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length != 1) return; //Invalid command args length format

            BasePlayer player = arg.connection.player as BasePlayer;
            if (!player) return; //Not a player
            if (!HasPermission(player, UsePermission))
            {
                PrintToChat(player, Lang("NoPermission", player.UserIDString));
                return;
            }

            int surveyId;
            if (!int.TryParse(arg.Args[0], out surveyId)) return; //Not an integer

            SurveyData data = _storedData.SurveyInfo[player.userID]?[surveyId];

            if (data == null) return;

            DisplaySurveyLoot(player, data);
            DrawSurveyIdAtSurveyLocation(data, player);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Close all the GUIs
        /// </summary>
        /// <param name="arg"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.close")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiClose(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return;
            CuiHelper.DestroyUi(player, SurveyContainerName);
            CuiHelper.DestroyUi(player, GiveToContainerName);
            CuiHelper.DestroyUi(player, MouseBugFixContainerName);
            _playerSurveyDataUiPage.Remove(player.userID);
            _playerGiveToUiPage.Remove(player.userID);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Goes to the next page for the Survey Data UI
        /// </summary>
        /// <param name="arg"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.nextpage")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiNextPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return;
            if (_playerSurveyDataUiPage.ContainsKey(player.userID))
            {
                _playerSurveyDataUiPage[player.userID]++;
            }
            InitializeDataUi(player);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Goes to the previous page for the Survey Data UI
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.prevpage")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiPrevPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return;
            if (_playerSurveyDataUiPage.ContainsKey(player.userID))
            {
                _playerSurveyDataUiPage[player.userID]--;
            }
            InitializeDataUi(player);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Goes to the next page for the Give To UI
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.givenextpage")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiGiveNextPage(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return;
            if (arg.Args.Length != 1) return;
            if (!_playerGiveToUiPage.ContainsKey(player.userID)) return;

            int surveyId;
            if (!int.TryParse(arg.Args[0], out surveyId)) return;

            _playerGiveToUiPage[player.userID]++;
            InitializeGiveToUi(player, surveyId);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Goes to the previous page for the Give To UI
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.giveprevpage")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiGivePrevPage(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length != 1) return; //Invalid command args length

            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return; //Not a player

            if (!_playerGiveToUiPage.ContainsKey(player.userID)) return; //Player does not have the give to ui open

            int surveyId;
            if (!int.TryParse(arg.Args[0], out surveyId)) return; //Survey id not a valid integer

            _playerGiveToUiPage[player.userID]--;

            InitializeGiveToUi(player, surveyId);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Remove the record from the user and delete from the data file
        /// </summary>
        /// <param name="arg"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.remove")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiRemove(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length != 1) return; //Invalid command args length

            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return; //Not a player

            int surveyId;
            if (!int.TryParse(arg.Args[0], out surveyId)) return; //Survey id not a valid integer

            SurveyData surveyData = _storedData.SurveyInfo[player.userID]?[surveyId];
            if (surveyData == null) return; //Player or survey data does not exist

            _storedData.SurveyInfo[player.userID].Remove(surveyId);

            InitializeDataUi(player);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Opens the Give to GUI
        /// </summary>
        /// <param name="arg"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.opengiveto")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiOpenGiveTo(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length != 1) return; //Invalid command args length

            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return; //Not a player

            int surveyId;
            if (!int.TryParse(arg.Args[0], out surveyId)) return; //survey id not a valid integer

            SurveyData surveyData = _storedData.SurveyInfo[player.userID]?[surveyId];
            if (surveyData == null) return; //The player or survey id does not exist

            InitializeGiveToUi(player, surveyId);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Give the survey record to the given player user id
        /// </summary>
        /// <param name="arg"></param>
        /// //////////////////////////////////////////////////////////////////////////////////////
        [ConsoleCommand("si.sigiveto")]
        // ReSharper disable once UnusedMember.Local
        private void CmdUiGiveTo(ConsoleSystem.Arg arg)
        {
            if (arg.Args.Length != 2) return; //Invalid command args length

            BasePlayer player = arg.connection.player as BasePlayer;
            if (player == null) return; //Not a player

            ulong playerId;
            if (!ulong.TryParse(arg.Args[0], out playerId)) return; //player id not a valid ulong

            int surveyId;
            if (!int.TryParse(arg.Args[1], out surveyId)) return; //Survey id not a valid integer

            SurveyData surveyData = _storedData.SurveyInfo[player.userID]?[surveyId];
            if (surveyData == null) return; //The player or survey id does not exist

            _storedData.SurveyInfo[player.userID].Remove(surveyId);

            if (!_storedData.SurveyInfo.ContainsKey(playerId)) _storedData.SurveyInfo.Add(playerId, new Hash<int, SurveyData>());
            _storedData.SurveyInfo[playerId].Add(surveyId, surveyData);

            InitializeDataUi(player);
        }
        #endregion

        #region Classes
        #region Survey Data
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// a class to save the Vector3 cord in the json file
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private class Location
        {
            public float x;
            public float y;
            public float z;

            public Location(Vector3 loc)
            {
                x = loc.x;
                y = loc.y;
                z = loc.z;
            }

            public Vector3 GetLocation()
            {
                return new Vector3(x, y, z);
            }
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Record of a survey charge
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private class SurveyData
        {
            #region Class Fields
            public int SurveyId { get; set; } //Id of the survey charge
            public Location Location { get; set; }
            public float Score { get; set; } //Score for the survey charge
            public Hash<int, SurveyItem> Items { get; } //Hash of all the items from the survey charge

            public SurveyData(int surveyId)
            {
                SurveyId = surveyId;
                Items = new Hash<int, SurveyItem>();
            }

            #endregion

            #region Class Methods
            //////////////////////////////////////////////////////////////////////////////////////
            /// <summary>
            /// Returns the amount of an item based on the item id
            /// </summary>
            /// <param name="id">Rust item id</param>
            /// <returns>the item amount or 0 if the item does not exist</returns>
            /// //////////////////////////////////////////////////////////////////////////////////////
            public int GetAmountByItemId(int id)
            {
                return Items[id]?.Amount ?? 0;
            }
            #endregion
        }

        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Data file for SurveyInfo
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private class SurveyItem
        {
            #region Class Fields
            public string DisplayName { get; }
            public int Amount { get; set; }

            public SurveyItem(string displayName, int amount)
            {
                DisplayName = displayName;
                Amount = amount;
            }
            #endregion
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class StoredData
        {
            #region Class Fields
            //Hash<PlayerUserId, Hash<SurveyId, SurveyData>>
            public Hash<ulong, Hash<int, SurveyData>> SurveyInfo = new Hash<ulong, Hash<int, SurveyData>>();
            public int NextChargeId = 1;
            #endregion
        }
        #endregion

        #region UI Config
        // ReSharper disable once InconsistentNaming
        private class SurveyDataUIConfig
        {
            public int SurveyDataTextSize = 13;
            public int SurveyDataRecordsPerPage = 18;
            public double SurveyDataRecordYSpacing = .05;

            public string SurveyDataContainerMin = ".15 .15";
            public string SurveyDataContainerMax = ".6 .9";

            public double SurveyDataTopHeadingYMin = .96;
            public double SurveyDataTopHeadingYMax = .99;
            public double SurveyDataMiddleHeadingYMin = .935;
            public double SurveyDataMiddleHeadingYMax = .965;
            public double SurveyDataBottomHeadingYMin = .91;
            public double SurveyDataBottomHeadingYMax = .94;

            public double SurveyDataRecordStartingYMin = .87;
            public double SurveyDataRecordStartingYMax = .91;

            public double LabelIdXMin = .01;
            public double LabelIdXMax = .12;
            public double LabelStonesXMin = .13;
            public double LabelStonesXMax = .20;
            public double LabelMetalOreXMin = .21;
            public double LabelMetalOreXMax = .28;
            public double LabelMetalFragXMin = .29;
            public double LabelMetalFragXMax = .36;
            public double LabelSulfurOreXMin = .37;
            public double LabelSulfurOreXMax = .44;
            public double LabelHqmXMin = .45;
            public double LabelHqmXMax = .52;
            public double LabelScoreXMin = .53;
            public double LabelScoreXMax = .6;

            public double ButtonLookupXMin = .67;
            public double ButtonLookupXMax = .77;
            public double ButtonRemoveXMin = .78;
            public double ButtonRemoveXMax = .88;
            public double ButtonGiveToXMin = .89;
            public double ButtonGiveToXMax = .99;

            public string ButtonCloseMin = ".89 .94";
            public string ButtonCloseMax = ".99 .99";
            public string ButtonDataPrevPageMin = ".625 .94";
            public string ButtonDataPrevPageMax = ".725 .99";
            public string ButtonDataNextPageMin = ".75 .94";
            public string ButtonDataNextPageMax = ".85 .99";
        }

        // ReSharper disable once InconsistentNaming
        private class GiveToUIConfig
        {
            public int GiveToPlayersTextSize = 10;
            public int GiveToPlayerPageTextSize = 12;
            public int GiveToPlayersPerRow = 4;
            public int GiveToPlayersPerPage = 72;

            public string GiveToContainerMin = ".62 .15";
            public string GiveToContainerMax = ".92 .9";

            public string ButtonGiveToPrevPageMin = ".84 .01";
            public string ButtonGiveToPrevPageMax = ".91 .06";
            public string ButtonGiveToNextPageMin = ".92 .01";
            public string ButtonGiveToNextPageMax = ".99 .06";

            public double PlayerButtonStartPositionXMin = .01;
            public double PlayerButtonStartPositionXMax = .24;
            public double PlayerButtonStartPositionYMin = .95;
            public double PlayerButtonStartPositionYMax = .99;

            public double PlayerXSpacing = .25;
            public double PlayerButtonYSpacing = .05; //Y Spacing between each player button
        }

        // ReSharper disable once InconsistentNaming
        private class UIColors
        {
            public string SurveyDataContainer = ".3 .3 .3 .825";
            public string GiveToContainer = ".3 .3 .3 .825";
            public string Label = ".9 .9 .9 .9";
            public string ButtonClose = ".66 .66 .66 .9";
            public string ButtonNextPage = ".66 .66 .66 .9";
            public string ButtonPrevPage = ".77 .77 .77 .9";
            public string ButtonLookup = "0 .8 0 .9";
            public string ButtonRemove = ".8 0 0 .9";
            public string ButtonGiveTo = ".66 .66 .66 .9";
            public string ButtonPlayers = ".66 .66 .66 .9";
            public string IdDisplay = "1 0 0 1";
        }
        #endregion

        #region Plugin Config
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Config for plugin
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private class PluginConfig
        {
            public string Prefix { get; set; }
            public float SaveIntervalInSeconds { get; set; }
            public float SurveyIdDisplayLengthInSeconds { get; set; }
            public UIColors UiColors { get; set; }
            public SurveyDataUIConfig SurveyUiConfig { get; set; }
            public GiveToUIConfig GiveToUiConfig { get; set; }
        }
        #endregion
        #endregion

        #region UICreator Class
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once ClassNeverInstantiated.Local
        //////////////////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// @credit to k1lly0u - code from ServerRewards
        /// UI class for SurveyInfo
        /// </summary>
        /// //////////////////////////////////////////////////////////////////////////////////////
        private class UICreator
        {
            public static CuiElementContainer CreateElementContainer(string panelName, string color, string aMin, string aMax, float fadeIn, float fadeOut)
            {
                var newElement = new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = color , FadeIn = fadeIn},
                            RectTransform = {AnchorMin = aMin, AnchorMax = aMax},
                            CursorEnabled = true,
                            FadeOut = fadeOut
                        },
                        new CuiElement().Parent = "Hud",
                        panelName
                    }
                };
                return newElement;
            }
            public static void CreateLabel(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                {
                    Text = { Color = color, FontSize = size, Align = align, Text = text },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax }
                },
                panel, CuiHelper.GetGuid());

            }
            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = color, Command = command },
                    RectTransform = { AnchorMin = aMin, AnchorMax = aMax },
                    Text = { Text = text, FontSize = size, Align = align }
                },
                panel, CuiHelper.GetGuid());
            }
        }
        #endregion

        #region Help Text
        // ReSharper disable once UnusedMember.Local
        private void SendHelpText(BasePlayer player)
        {
            PrintToChat(player, @"[<color=yellow>Survey Info</color>] Help Text:\n
                                When a player throws a survey charge it and it produces items i gets recorded and saved\n
                                Commands:\n
                                <color=yellow>/si</color> - Opens the Survey Info GUI\n
                                Supports looking up / removing / giving your survey charge info");
        }
        #endregion
    }
}
