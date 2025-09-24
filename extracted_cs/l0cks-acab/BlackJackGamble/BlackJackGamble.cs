using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("BlackjackGamble", "herbs.acab", "1.8.1")]
    [Description("Allows players to gamble their RP in a game of Blackjack using the Economics plugin")]

    public class BlackjackGamble : RustPlugin
    {
        [PluginReference]
        private Plugin Economics;

        private Dictionary<ulong, BlackjackGame> activeGames = new Dictionary<ulong, BlackjackGame>();
        private Dictionary<ulong, string> newBetInputs = new Dictionary<ulong, string>();

        private static Configuration config;

        private class BlackjackGame
        {
            public List<int> playerCards;
            public List<int> dealerCards;
            public int bet;
            public bool isActive;
            public string resultMessage;
            public string resultColor;

            public BlackjackGame(int bet)
            {
                this.bet = bet;
                playerCards = new List<int>();
                dealerCards = new List<int>();
                isActive = true;
                resultMessage = string.Empty;
                resultColor = "1 1 1 1";
                DealInitialCards();
            }

            private void DealInitialCards()
            {
                playerCards.Add(DrawCard());
                playerCards.Add(DrawCard());
                dealerCards.Add(DrawCard());
                dealerCards.Add(DrawCard());
            }

            public int DrawCard()
            {
                System.Random rnd = new System.Random();
                return rnd.Next(1, 11);
            }

            public int GetPlayerTotal()
            {
                return playerCards.Sum();
            }

            public int GetDealerTotal()
            {
                return dealerCards.Sum();
            }
        }

        private class Configuration
        {
            public int MinBet { get; set; } = 10;
            public int MaxBet { get; set; } = 1000;
        }

        protected override void LoadDefaultConfig()
        {
            config = new Configuration();
            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void Init()
        {
            permission.RegisterPermission("blackjackgamble.use", this);
            permission.RegisterPermission("blackjackgamble.admin", this);
        }

        [ChatCommand("blackjack")]
        private void StartBlackjack(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "blackjackgamble.use"))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            if (args.Length != 1 || !int.TryParse(args[0], out int bet))
            {
                player.ChatMessage($"Usage: /blackjack <bet> (Min: {config.MinBet}, Max: {config.MaxBet})");
                return;
            }

            if (bet < config.MinBet || bet > config.MaxBet)
            {
                player.ChatMessage($"Bet amount must be between {config.MinBet} and {config.MaxBet}.");
                return;
            }

            var balance = (double)Economics.Call("Balance", player.UserIDString);
            if (balance < bet)
            {
                player.ChatMessage("You don't have enough RP to place that bet.");
                return;
            }

            if (activeGames.ContainsKey(player.userID))
            {
                player.ChatMessage("You are already in a game of Blackjack.");
                return;
            }

            Economics.Call("Withdraw", player.UserIDString, (double)bet);
            activeGames[player.userID] = new BlackjackGame(bet);
            ShowBlackjackUI(player);
        }

        [ChatCommand("setminbet")]
        private void SetMinBetCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "blackjackgamble.admin"))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            if (args.Length != 1 || !int.TryParse(args[0], out int minBet))
            {
                player.ChatMessage("Usage: /setminbet <amount>");
                return;
            }

            config.MinBet = minBet;
            SaveConfig();
            player.ChatMessage($"Minimum bet set to {minBet}.");
        }

        [ChatCommand("setmaxbet")]
        private void SetMaxBetCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, "blackjackgamble.admin"))
            {
                player.ChatMessage("You don't have permission to use this command.");
                return;
            }

            if (args.Length != 1 || !int.TryParse(args[0], out int maxBet))
            {
                player.ChatMessage("Usage: /setmaxbet <amount>");
                return;
            }

            config.MaxBet = maxBet;
            SaveConfig();
            player.ChatMessage($"Maximum bet set to {maxBet}.");
        }

        private void ShowBlackjackUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BlackjackUI");

            var game = activeGames[player.userID];
            var container = new CuiElementContainer();

            var panel = container.Add(new CuiPanel
            {
                Image = { Color = "0.1 0.1 0.1 0.9" },
                RectTransform = { AnchorMin = "0.3 0.3", AnchorMax = "0.7 0.7" },
                CursorEnabled = true
            }, "Overlay", "BlackjackUI");

            container.Add(new CuiLabel
            {
                Text = { Text = "Blackjack", FontSize = 20, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0.85", AnchorMax = "1 1" }
            }, panel);

            container.Add(new CuiLabel
            {
                Text = { Text = "Try to get 21!", FontSize = 16, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0.8", AnchorMax = "1 0.85" }
            }, panel);

            container.Add(new CuiLabel
            {
                Text = { Text = $"Your Cards: {string.Join(", ", game.playerCards)} (Total: {game.GetPlayerTotal()})", FontSize = 16, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "0 1 0 1" },
                RectTransform = { AnchorMin = "0.1 0.7", AnchorMax = "0.9 0.8" }
            }, panel);

            container.Add(new CuiLabel
            {
                Text = { Text = $"Dealer's Cards: {string.Join(", ", game.dealerCards)} (Total: {game.GetDealerTotal()})", FontSize = 16, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 0 1" },
                RectTransform = { AnchorMin = "0.1 0.5", AnchorMax = "0.9 0.6" }
            }, panel);

            if (!string.IsNullOrEmpty(game.resultMessage))
            {
                container.Add(new CuiLabel
                {
                    Text = { Text = game.resultMessage, FontSize = 16, Align = UnityEngine.TextAnchor.MiddleCenter, Color = game.resultColor },
                    RectTransform = { AnchorMin = "0 0.35", AnchorMax = "1 0.45" }
                }, panel);

                container.Add(new CuiButton
                {
                    Button = { Command = "blackjack.replay", Color = "0.2 0.6 0.2 0.8" },
                    RectTransform = { AnchorMin = "0.45 0.2", AnchorMax = "0.55 0.3" },
                    Text = { Text = "Replay", FontSize = 14, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, panel);
            }

            if (game.isActive)
            {
                container.Add(new CuiButton
                {
                    Button = { Command = "blackjack.hit", Color = "0.2 0.6 0.2 0.8" },
                    RectTransform = { AnchorMin = "0.3 0.2", AnchorMax = "0.45 0.3" },
                    Text = { Text = "Hit", FontSize = 14, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, panel);

                container.Add(new CuiButton
                {
                    Button = { Command = "blackjack.stand", Color = "1.0 0.5 0.0 0.8" }, // Changed to orange
                    RectTransform = { AnchorMin = "0.55 0.2", AnchorMax = "0.7 0.3" },
                    Text = { Text = "Stand", FontSize = 14, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, panel);
            }

            container.Add(new CuiButton
            {
                Button = { Command = "blackjack.quit", Color = "0.6 0.2 0.2 0.8" },
                RectTransform = { AnchorMin = "0.45 0.05", AnchorMax = "0.55 0.15" },
                Text = { Text = "Quit", FontSize = 14, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, panel);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("blackjack.hit")]
        private void HitCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !activeGames.ContainsKey(player.userID)) return;

            var game = activeGames[player.userID];
            game.playerCards.Add(game.DrawCard());

            if (game.GetPlayerTotal() > 21)
            {
                game.resultMessage = "You lose.";
                game.resultColor = "1 0 0 1";
                game.isActive = false;
            }

            ShowBlackjackUI(player);
        }

        [ConsoleCommand("blackjack.stand")]
        private void StandCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !activeGames.ContainsKey(player.userID)) return;

            var game = activeGames[player.userID];

            while (game.GetDealerTotal() < 17)
            {
                game.dealerCards.Add(game.DrawCard());
            }

            DetermineWinner(player);
            ShowBlackjackUI(player);
        }

        [ConsoleCommand("blackjack.quit")]
        private void QuitCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !activeGames.ContainsKey(player.userID)) return;

            player.ChatMessage("You have quit the game.");
            CuiHelper.DestroyUi(player, "BlackjackUI");
            activeGames.Remove(player.userID);
        }

        [ConsoleCommand("blackjack.replay")]
        private void ReplayCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            ShowReplayUI(player);
        }

        private void ShowReplayUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "BlackjackReplayUI");

            var container = new CuiElementContainer();

            var panel = container.Add(new CuiPanel
            {
                Image = { Color = "0.05 0.05 0.05 0.9" },
                RectTransform = { AnchorMin = "0.4 0.4", AnchorMax = "0.6 0.6" },
                CursorEnabled = true
            }, "Overlay", "BlackjackReplayUI");

            container.Add(new CuiLabel
            {
                Text = { Text = "Enter your new bet amount", FontSize = 16, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0.6", AnchorMax = "1 0.8" }
            }, panel);

            container.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiInputFieldComponent
                    {
                        FontSize = 16,
                        Align = TextAnchor.MiddleCenter,
                        Command = "blackjack.input",
                        Text = newBetInputs.ContainsKey(player.userID) ? newBetInputs[player.userID] : string.Empty,
                        Color = "1 1 1 1"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2 0.4",
                        AnchorMax = "0.8 0.5"
                    }
                }
            });

            container.Add(new CuiButton
            {
                Button = { Command = "blackjack.submit", Color = "0.2 0.6 0.2 0.8" },
                RectTransform = { AnchorMin = "0.6 0.1", AnchorMax = "0.8 0.3" },
                Text = { Text = "Enter", FontSize = 14, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, panel);

            container.Add(new CuiButton
            {
                Button = { Command = "blackjack.cancel", Color = "0.6 0.2 0.2 0.8" },
                RectTransform = { AnchorMin = "0.2 0.1", AnchorMax = "0.4 0.3" },
                Text = { Text = "Cancel", FontSize = 14, Align = UnityEngine.TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, panel);

            CuiHelper.AddUi(player, container);
        }

        [ConsoleCommand("blackjack.input")]
        private void InputCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || arg.Args.Length == 0) return;

            newBetInputs[player.userID] = arg.Args[0];
        }

        [ConsoleCommand("blackjack.submit")]
        private void SubmitCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null || !newBetInputs.ContainsKey(player.userID)) return;

            if (!int.TryParse(newBetInputs[player.userID], out int newBet) || newBet <= 0)
            {
                player.ChatMessage("Invalid bet amount. Please enter a valid number.");
                return;
            }

            if (newBet < config.MinBet || newBet > config.MaxBet)
            {
                player.ChatMessage($"Bet amount must be between {config.MinBet} and {config.MaxBet}.");
                return;
            }

            var balance = (double)Economics.Call("Balance", player.UserIDString);
            if (balance < newBet)
            {
                player.ChatMessage("You don't have enough RP to place that bet.");
                return;
            }

            Economics.Call("Withdraw", player.UserIDString, (double)newBet);
            activeGames[player.userID] = new BlackjackGame(newBet);
            ShowBlackjackUI(player);
            CuiHelper.DestroyUi(player, "BlackjackReplayUI");
            newBetInputs.Remove(player.userID);
        }

        [ConsoleCommand("blackjack.cancel")]
        private void CancelCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            if (player == null) return;

            CuiHelper.DestroyUi(player, "BlackjackReplayUI");
            newBetInputs.Remove(player.userID);
        }

        private void DetermineWinner(BasePlayer player)
        {
            var game = activeGames[player.userID];

            if (game.GetDealerTotal() > 21 || game.GetPlayerTotal() > game.GetDealerTotal())
            {
                game.resultMessage = "You win!";
                game.resultColor = "0 1 0 1";
                Economics.Call("Deposit", player.UserIDString, (double)(game.bet * 2));
            }
            else if (game.GetPlayerTotal() == game.GetDealerTotal())
            {
                game.resultMessage = "It's a draw!";
                game.resultColor = "1 1 0 1";
                Economics.Call("Deposit", player.UserIDString, (double)game.bet);
            }
            else
            {
                game.resultMessage = "You lose.";
                game.resultColor = "1 0 0 1";
            }

            game.isActive = false;
        }
    }
}
