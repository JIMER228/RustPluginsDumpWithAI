using ConVar;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEngine;
using WebSocketSharp;
using static ConsoleSystem;

namespace Oxide.Plugins
{
    [Info("Terminal Hack Plugin", "Mooli", "1.0.0")]
    public class TerminalHackPlugin : RustPlugin
    {
        [PluginReference]
        private Plugin ImageLibrary;

        private static int numberOfLines = 14;
        private string[] terminalUsedCommands = new string[numberOfLines];
        private string terminalPath = @"C:\User\Brinda>";
        Dictionary<string, string> imageUrlMap = new Dictionary<string, string>();



        private void OnServerInitialized()
        {
            imageUrlMap.Add("scanner_gg", "https://i.imgur.com/U3z9e9S.png");

            if (ImageLibrary == null)
            {
                PrintError("ImageLibrary plugin is not loaded. Please install ImageLibrary plugin.");
                return;
            }

            foreach (var pair in imageUrlMap)
            {
                ImageLibrary.Call("AddImage", pair.Value, pair.Key);
            }
        }

        private string getImage(string name)
        {
            return (string)ImageLibrary.Call("GetImage", imageUrlMap[name], (ulong) 0);
        }

        private void clearLastCommands()
        {
            terminalUsedCommands = new string[numberOfLines];
        }

        private void updateLastCommand(string cmd)
        {
            for (int i = 0; i < numberOfLines; i++)
            {
                (cmd, terminalUsedCommands[i]) = (terminalUsedCommands[i], cmd);
            }
        }

        void runTerminalCommand(string command, BasePlayer player)
        {
            switch (command)
            {
                case "dir":
                    updateLastCommand("hack.exe");
                    break;

                case "run":
                    ScannerOpen(player);
                    break;

                case "clear":
                    clearLastCommands();
                    break;

                default:
                    updateLastCommand("Invalid Command");
                    break;
            }
        }


        void StartScanning(BasePlayer player)
        {
            Ray ray = new Ray(player.eyes.position, player.eyes.HeadForward());
            RaycastHit hit;

            if (UnityEngine.Physics.Raycast(ray, out hit, 5f)) 
            {
                BaseEntity entity = hit.GetEntity();
                if (entity != null && entity.ShortPrefabName == "lock.code")
                {
                    CodeLock codeLock = entity.GetComponent<CodeLock>();
                    if (codeLock != null)
                    {
                        codeLock.SetFlag(BaseEntity.Flags.Locked, false);
                        codeLock.SendNetworkUpdateImmediate();
                    }
                }
            }
        }

        //hooks
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (input.WasJustPressed(BUTTON.JUMP))
            {
                CloseScanner(player);
            }

            if (input.WasJustPressed(BUTTON.DUCK))
            {
                player.ConsoleMessage("Scanned");
                StartScanning(player);
            }
        }

        //UI helpers
        private void CreateScannerUI(BasePlayer player)
        {
             Puts("Created Scanner!");
            var uiElements = new CuiElementContainer();
            string imageId = (string)ImageLibrary.Call("GetImage", "scanner2");

            var mainPanel = uiElements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 0" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" },
                CursorEnabled = true
            }, "Overlay", "ScannerPanel");

            uiElements.Add(new CuiButton
            {
                Button = { Command = "scanner.close"/*Close = mainPanel*/, Color = "0 0 0 1" },
                Text = { Text = "X", FontSize = 14 },
                RectTransform = { AnchorMin = "0.915827 0.9022043", AnchorMax = "0.9798004 0.9798004" }

            }, mainPanel);

            uiElements.Add(new CuiElement
            {
                Parent = mainPanel,
                Components =
                {
                    new CuiRawImageComponent {/* Png = getImage("scanner_gg")*/ Url = imageUrlMap["scanner_gg"] },
                    new CuiRectTransformComponent { AnchorMin = $"0.4226196 0.16857", AnchorMax = $"0.5791665 0.6447623"  }
                }
            });

            CuiHelper.AddUi(player, uiElements);
        }

        private void CreateTerminalUI(BasePlayer player)
        {
            Puts("Created Terminal UI");
            var uiElements = new CuiElementContainer();

            var mainPanel = uiElements.Add(new CuiPanel
            {
                Image = { Color = "0 0 0 1" },
                RectTransform = { AnchorMin = "0.3178574 0.3257154", AnchorMax = "0.6714277 0.7152363" },
                CursorEnabled = true
            }, "Overlay", "TerminalPanel");

            double[,] commandanchors = { { 0.03030238, 0.1149155, 1.210441, 0.1809276 },
                                         { 0.03030238, 0.1687056, 1.210441, 0.2347180 },
                                         { 0.03030238, 0.2224961, 1.210441, 0.2885089} };

            double[] commandanchor = { 0.1149155, 0.1809276 };

            for (int i = 0; i < numberOfLines; i++)
            {
                var lastcommand = terminalUsedCommands[i];

                if (lastcommand == null)
                {
                    lastcommand = "";
                }

                uiElements.Add(new CuiLabel
                {

                    Text = { Text = lastcommand, FontSize = 14 },
                    RectTransform = {
                        AnchorMin = $"0.03030238 {commandanchor[0] + i*0.0537905}",
                        AnchorMax = $"1.21044100 {commandanchor[1] + i*0.0537905}"
                    }
                }, mainPanel);
            };

            uiElements.Add(new CuiLabel
            {

                Text = { Text = $"{terminalPath}", FontSize = 14 },
                RectTransform = {
                        AnchorMin = "0.03030238 0.069155",
                        AnchorMax = "1.210441 0.1269276"
                    }
            }, mainPanel);

            uiElements.Add(new CuiElement
            {
                Parent = mainPanel,
                Components =
                {
                    new CuiInputFieldComponent { Command = "terminal.enter", FontSize = 14, Text = "" },
                    new CuiRectTransformComponent { AnchorMin = "0.25930238 0.069155", AnchorMax = "1.502441 0.1269276" },
                    new CuiNeedsCursorComponent()
                }
            });

            uiElements.Add(new CuiButton
            {
                Button = { Close = mainPanel, Color = "1 1 1 1"},
                Text = { Text = "X", FontSize = 14 },
                RectTransform = { AnchorMin = "0.915827 0.9022043", AnchorMax = "0.9798004 0.9798004" }

            }, mainPanel);

            CuiHelper.AddUi(player, uiElements);
        }


        //chat commands
        [ChatCommand("laptop")]
        private void OpenLaptop(BasePlayer player)
        {
            clearLastCommands();
            CreateTerminalUI(player);
        }

        [ChatCommand("scanner")]
        private void OpenScanner(BasePlayer player)
        {
            CreateScannerUI(player);
            timer.Every(0.5f, () =>
            {

            });
        }
        
        private void CloseScanner(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ScannerPanel");
        }
        
        [ConsoleCommand("scanner.close")]
        private void CloseScannerConsole(ConsoleSystem.Arg arg)  
        {
            CloseScanner(arg.Player());
        }


        [ChatCommand("test")]
        private void test(BasePlayer player)
        {
            player.ChatMessage("Hi");
        }

        //console commands
        [ConsoleCommand("terminal.enter")]
        private void TerminalEnter(ConsoleSystem.Arg arg)
        {
            string userInput = arg.GetString(0, "");
            updateLastCommand(terminalPath + userInput);
            CuiHelper.DestroyUi(arg.Player(), "TerminalPanel");
            runTerminalCommand(userInput, arg.Player());
            CreateTerminalUI(arg.Player());
        }


        private void ScannerOpen(BasePlayer player)
        {
            updateLastCommand("Now opening Scanner");

            if (player == null)
            {
                Puts("Player not found");
            }
            Puts(player.displayName);
            CreateScannerUI(player);
        }

        [ConsoleCommand("scanner.close")]
        private void ScannerClose(ConsoleSystem.Arg arg)
        {
            CuiHelper.DestroyUi(arg.Player(), "ScannerPanel");
        }




    }
}
