using System;
using System.Collections.Generic;
using System.Text;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Rust;
using Newtonsoft.Json;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("ChessPlugin", "GrndThftJono & Grok", "1.2.2")]
    [Description("A chess game integrated into Rust with a clickable GUI and spectator mode.")]
    public class ChessPlugin : CSharpPlugin
    {
        private Dictionary<string, ChessGame> games = new Dictionary<string, ChessGame>();
        private Dictionary<string, PlayerStats> playerStats = new Dictionary<string, PlayerStats>();
        private const string DataFilePath = "ChessPlugin/games";
        private const string StatsFilePath = "ChessPlugin/playerstats";
        private const string PlayerPermission = "chessplugin.use";
        private const string AdminPermission = "chessplugin.admin";
        private readonly Dictionary<string, string> playerGUIs = new Dictionary<string, string>();
        private Configuration config;
        private bool debugMode = true;
        private readonly Dictionary<string, float> lastClickTimes = new Dictionary<string, float>(); // For click debouncing

        [PluginReference] private Plugin Economics, ServerRewards;

        private class Configuration
        {
            public Dictionary<string, string> Colors { get; set; } = new Dictionary<string, string>
            {
                { "Background", "0.1 0.1 0.1 0.8" },
                { "LightSquare", "0.8 0.8 0.8 1" },
                { "DarkSquare", "0.4 0.4 0.4 1" },
                { "Highlight", "0 1 0 0.5" },
                { "Selected", "1 1 0 0.5" },
                { "Button", "0.2 0.5 0.2 0.8" },
                { "ButtonClose", "0.5 0.2 0.2 0.8" },
                { "UndoLabel", "1 1 0 1" },
                { "UndoActiveLabel", "0 1 0 1" }
            };
        }

        private enum PieceType
        {
            Empty,
            Pawn,
            Knight,
            Bishop,
            Rook,
            Queen,
            King
        }

        private class Piece
        {
            public PieceType Type { get; set; }
            public bool IsWhite { get; set; }
        }

        private class KingPositions
        {
            public int WhiteRow { get; set; }
            public int WhiteCol { get; set; }
            public int BlackRow { get; set; }
            public int BlackCol { get; set; }
        }

        private class LastMove
        {
            public int RowFrom { get; set; }
            public int ColFrom { get; set; }
            public int RowTo { get; set; }
            public int ColTo { get; set; }
            public Piece MovedPiece { get; set; }
            public Piece CapturedPiece { get; set; }
            public (bool WhiteKingside, bool WhiteQueenside, bool BlackKingside, bool BlackQueenside) CastlingBeforeMove { get; set; }
            public (int Row, int Col)? EnPassantBeforeMove { get; set; }
            public bool WasCastling { get; set; }
            public bool WasEnPassant { get; set; }
            public bool WhiteToMoveBefore { get; set; }
        }

        private class ChessGame
        {
            public Piece[,] Board { get; set; } = new Piece[8, 8];
            public bool WhiteToMove { get; set; } = true;
            public (bool WhiteKingside, bool WhiteQueenside, bool BlackKingside, bool BlackQueenside) Castling { get; set; } = (true, true, true, true);
            public (int Row, int Col)? EnPassant { get; set; }
            public KingPositions KingPos { get; set; } = new KingPositions { WhiteRow = 0, WhiteCol = 3, BlackRow = 7, BlackCol = 3 };
            public string BluePlayerId { get; set; }
            public string RedPlayerId { get; set; }
            public (int Row, int Col)? SelectedSquare { get; set; }
            public HashSet<(int Row, int Col)> LegalMoves { get; set; } = new HashSet<(int Row, int Col)>();
            public bool IsCastlingSelected { get; set; } = false;
            public string Status { get; set; } = "Waiting for players";
            public string GameId { get; set; }
            public HashSet<string> SpectatorIds { get; set; } = new HashSet<string>();
            public bool IsSolo { get; set; } = false;
            public bool GameEnded { get; set; } = false;
            public LastMove LastMove { get; set; }
            public bool UndoRequested { get; set; } = false;
            public bool UndoUsedThisTurn { get; set; } = false;
            public int LastUndoTurn { get; set; } = -1;
            public int TurnNumber { get; set; } = 0;

            public ChessGame(string gameId)
            {
                GameId = gameId;
                InitializeBoard();
            }

            private void InitializeBoard()
            {
                for (int i = 0; i < 8; i++)
                    for (int j = 0; j < 8; j++)
                        Board[i, j] = new Piece { Type = PieceType.Empty };

                for (int i = 0; i < 8; i++)
                {
                    Board[1, i] = new Piece { Type = PieceType.Pawn, IsWhite = true };
                    Board[6, i] = new Piece { Type = PieceType.Pawn, IsWhite = false };
                }

                PieceType[] backRank = { PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.King, PieceType.Queen, PieceType.Bishop, PieceType.Knight, PieceType.Rook };
                for (int i = 0; i < 8; i++)
                {
                    Board[0, i] = new Piece { Type = backRank[i], IsWhite = true };
                    Board[7, i] = new Piece { Type = backRank[i], IsWhite = false };
                }

                KingPos.WhiteRow = 0;
                KingPos.WhiteCol = 3;
                KingPos.BlackRow = 7;
                KingPos.BlackCol = 3;
            }
        }

        private class PlayerStats
        {
            public int Wins { get; set; }
            public int Losses { get; set; }
            public int Draws { get; set; }
        }

        private void Init()
        {
            permission.RegisterPermission(PlayerPermission, this);
            permission.RegisterPermission(AdminPermission, this);
            LoadConfig();
            LoadData();
            LoadStats();
            RegisterCommands();
            Puts("ChessPlugin loaded.");
        }

        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                DestroyGUI(player);
            SaveData();
            SaveStats();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    LogDebug("Configuration file not found, creating new one.");
                    config = new Configuration();
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Puts($"Error loading configuration: {ex.Message}. Creating default configuration.");
                config = new Configuration();
                SaveConfig();
            }
        }

        protected override void SaveConfig()
        {
            try
            {
                Config.WriteObject(config);
                LogDebug("Configuration saved.");
            }
            catch (Exception ex)
            {
                Puts($"Error saving configuration: {ex.Message}");
            }
        }

        private void LoadData()
        {
            try
            {
                games = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, ChessGame>>(DataFilePath);
                if (games == null)
                {
                    LogDebug("Chess game data not found, initializing empty games.");
                    games = new Dictionary<string, ChessGame>();
                    SaveData();
                }
                Puts($"Loaded {games.Count} chess games.");
            }
            catch (Exception ex)
            {
                Puts($"Error loading chess data: {ex.Message}. Initializing empty games.");
                games = new Dictionary<string, ChessGame>();
                SaveData();
            }
        }

        private void SaveData()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject(DataFilePath, games);
                LogDebug("Saved chess game data.");
            }
            catch (Exception ex)
            {
                Puts($"Error saving chess data: {ex.Message}");
            }
        }

        private void LoadStats()
        {
            try
            {
                playerStats = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerStats>>(StatsFilePath);
                if (playerStats == null)
                {
                    LogDebug("Player stats data not found, initializing empty stats.");
                    playerStats = new Dictionary<string, PlayerStats>();
                    SaveStats();
                }
                Puts($"Loaded {$"{playerStats.Count} player stats"}.");
            }
            catch (Exception ex)
            {
                Puts($"Error loading player stats: {ex.Message}. Initializing empty stats.");
                playerStats = new Dictionary<string, PlayerStats>();
                SaveStats();
            }
        }

        private void SaveStats()
        {
            try
            {
                Interface.Oxide.DataFileSystem.WriteObject(StatsFilePath, playerStats);
                LogDebug("Saved player stats data.");
            }
            catch (Exception ex)
            {
                Puts($"Error saving player stats: {ex.Message}");
            }
        }

        private void LogDebug(string message)
        {
            if (debugMode)
                Puts($"[DEBUG] {message}");
        }

        private HashSet<(int Row, int Col)> GetLegalMoves(ChessGame game, int row, int col)
        {
            var moves = new HashSet<(int Row, int Col)>();
            var piece = game.Board[row, col];
            if (piece.Type == PieceType.Empty || piece.IsWhite != game.WhiteToMove)
            {
                LogDebug($"No legal moves: Empty or wrong color at {row},{col}");
                return moves;
            }

            bool inCheck = IsInCheck(game, game.WhiteToMove);
            LogDebug($"Checking moves for {piece.Type} at {row},{col}. In check: {inCheck}");

            var tempMoves = new HashSet<(int Row, int Col)>();

            switch (piece.Type)
            {
                case PieceType.Pawn:
                    int direction = piece.IsWhite ? 1 : -1;
                    int startRow = piece.IsWhite ? 1 : 6;
                    int newRow = row + direction;
                    if (newRow >= 0 && newRow < 8 && game.Board[newRow, col].Type == PieceType.Empty)
                    {
                        tempMoves.Add((newRow, col));
                        if (row == startRow && game.Board[newRow + direction, col].Type == PieceType.Empty)
                            tempMoves.Add((newRow + direction, col));
                    }
                    foreach (int dc in new int[] { -1, 1 })
                    {
                        int newCol = col + dc;
                        if (newRow >= 0 && newRow < 8 && newCol >= 0 && newCol < 8)
                        {
                            if (game.Board[newRow, newCol].Type != PieceType.Empty && game.Board[newRow, newCol].IsWhite != piece.IsWhite)
                                tempMoves.Add((newRow, newCol));
                            if (game.EnPassant == (newRow, newCol))
                                tempMoves.Add((newRow, newCol));
                        }
                    }
                    break;
                case PieceType.Knight:
                    var knightOffsets = new (int, int)[] { (-2, -1), (-2, 1), (-1, -2), (-1, 2), (1, -2), (1, 2), (2, -1), (2, 1) };
                    foreach (var (dr, dc) in knightOffsets)
                    {
                        int r = row + dr, c = col + dc;
                        if (r >= 0 && r < 8 && c >= 0 && c < 8 && (game.Board[r, c].Type == PieceType.Empty || game.Board[r, c].IsWhite != piece.IsWhite))
                            tempMoves.Add((r, c));
                    }
                    break;
                case PieceType.Bishop:
                    AddSlidingMoves(game, tempMoves, row, col, piece.IsWhite, new (int, int)[] { (1, 1), (1, -1), (-1, 1), (-1, -1) });
                    break;
                case PieceType.Rook:
                    AddSlidingMoves(game, tempMoves, row, col, piece.IsWhite, new (int, int)[] { (1, 0), (0, 1), (-1, 0), (0, -1) });
                    break;
                case PieceType.Queen:
                    AddSlidingMoves(game, tempMoves, row, col, piece.IsWhite, new (int, int)[] { (1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) });
                    break;
                case PieceType.King:
                    var kingOffsets = new (int, int)[] { (1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) };
                    foreach (var (dr, dc) in kingOffsets)
                    {
                        int r = row + dr, c = col + dc;
                        if (r >= 0 && r < 8 && c >= 0 && c < 8 && (game.Board[r, c].Type == PieceType.Empty || game.Board[r, c].IsWhite != piece.IsWhite))
                            tempMoves.Add((r, c));
                    }
                    if (!inCheck)
                    {
                        if (piece.IsWhite)
                        {
                            if (game.Castling.WhiteKingside && game.Board[0, 4].Type == PieceType.Empty && game.Board[0, 5].Type == PieceType.Empty && game.Board[0, 6].Type == PieceType.Empty)
                                tempMoves.Add((0, 5));
                            if (game.Castling.WhiteQueenside && game.Board[0, 1].Type == PieceType.Empty && game.Board[0, 2].Type == PieceType.Empty)
                                tempMoves.Add((0, 1));
                        }
                        else
                        {
                            if (game.Castling.BlackKingside && game.Board[7, 4].Type == PieceType.Empty && game.Board[7, 5].Type == PieceType.Empty && game.Board[7, 6].Type == PieceType.Empty)
                                tempMoves.Add((7, 5));
                            if (game.Castling.BlackQueenside && game.Board[7, 1].Type == PieceType.Empty && game.Board[7, 2].Type == PieceType.Empty)
                                tempMoves.Add((7, 1));
                        }
                    }
                    break;
            }

            foreach (var move in tempMoves)
            {
                if (IsLegalMove(game, row, col, move.Row, move.Col))
                {
                    moves.Add(move);
                    LogDebug($"Legal move for {piece.Type} from {row},{col} to {move.Row},{move.Col}");
                }
                else
                {
                    LogDebug($"Illegal move for {piece.Type} from {row},{col} to {move.Row},{move.Col} (leaves king in check)");
                }
            }

            return moves;
        }

        private bool IsLegalMove(ChessGame game, int rowFrom, int colFrom, int rowTo, int colTo)
        {
            var originalPiece = game.Board[rowFrom, colFrom];
            var targetPiece = game.Board[rowTo, colTo];
            if (targetPiece.Type != PieceType.Empty && targetPiece.IsWhite == originalPiece.IsWhite)
            {
                LogDebug($"Invalid move: Cannot capture own piece at {rowTo},{colTo} from {rowFrom},{colFrom}");
                return false;
            }

            var originalEnPassant = game.EnPassant;
            var originalKingPos = new KingPositions
            {
                WhiteRow = game.KingPos.WhiteRow,
                WhiteCol = game.KingPos.WhiteCol,
                BlackRow = game.KingPos.BlackRow,
                BlackCol = game.KingPos.BlackCol
            };
            var originalCastling = game.Castling;

            game.Board[rowTo, colTo] = originalPiece;
            game.Board[rowFrom, colFrom] = new Piece { Type = PieceType.Empty };

            if (originalPiece.Type == PieceType.King)
            {
                bool isCastling = (rowFrom == rowTo) && (colFrom == 3) && (colTo == 5 || colTo == 1);
                if (isCastling)
                {
                    int rookFromCol = colTo == 5 ? 7 : 0;
                    int rookToCol = colTo == 5 ? 4 : 2;
                    var rookPiece = game.Board[rowTo, rookFromCol];
                    if (rookPiece == null || rookPiece.Type != PieceType.Rook || rookPiece.IsWhite != originalPiece.IsWhite)
                    {
                        LogDebug($"Invalid castling: No valid rook at {rowTo},{rookFromCol}");
                        game.Board[rowFrom, colFrom] = originalPiece;
                        game.Board[rowTo, colTo] = targetPiece;
                        return false;
                    }
                    game.Board[rowTo, rookToCol] = rookPiece;
                    game.Board[rowTo, rookFromCol] = new Piece { Type = PieceType.Empty };
                }

                if (originalPiece.IsWhite)
                {
                    game.KingPos.WhiteRow = rowTo;
                    game.KingPos.WhiteCol = colTo;
                    game.Castling = (false, false, game.Castling.BlackKingside, game.Castling.BlackQueenside);
                }
                else
                {
                    game.KingPos.BlackRow = rowTo;
                    game.KingPos.BlackCol = colTo;
                    game.Castling = (game.Castling.WhiteKingside, game.Castling.WhiteQueenside, false, false);
                }
            }
            else if (originalPiece.Type == PieceType.Pawn)
            {
                if ((originalPiece.IsWhite && rowTo == 7) || (!originalPiece.IsWhite && rowTo == 0))
                    game.Board[rowTo, colTo] = new Piece { Type = PieceType.Queen, IsWhite = originalPiece.IsWhite };
                game.EnPassant = null;
                if (Math.Abs(rowFrom - rowTo) == 2)
                    game.EnPassant = (rowFrom + (originalPiece.IsWhite ? 1 : -1), colFrom);
                else if (originalEnPassant == (rowTo, colTo))
                    game.Board[originalPiece.IsWhite ? rowTo - 1 : rowTo + 1, colTo] = new Piece { Type = PieceType.Empty };
            }
            else if (originalPiece.Type == PieceType.Rook)
            {
                if (originalPiece.IsWhite)
                {
                    if (rowFrom == 0 && colFrom == 0)
                        game.Castling = (game.Castling.WhiteKingside, false, game.Castling.BlackKingside, game.Castling.BlackQueenside);
                    else if (rowFrom == 0 && colFrom == 7)
                        game.Castling = (false, game.Castling.WhiteQueenside, game.Castling.BlackKingside, game.Castling.BlackQueenside);
                }
                else
                {
                    if (rowFrom == 7 && colFrom == 0)
                        game.Castling = (game.Castling.WhiteKingside, game.Castling.WhiteQueenside, game.Castling.BlackKingside, false);
                    else if (rowFrom == 7 && colFrom == 7)
                        game.Castling = (game.Castling.WhiteKingside, game.Castling.WhiteQueenside, false, game.Castling.BlackQueenside);
                }
            }

            bool isInCheck = IsInCheck(game, originalPiece.IsWhite);

            game.Board[rowFrom, colFrom] = originalPiece;
            game.Board[rowTo, colTo] = targetPiece;
            game.EnPassant = originalEnPassant;
            game.KingPos = originalKingPos;
            game.Castling = originalCastling;

            return !isInCheck;
        }

        private bool MakeMove(ChessGame game, int rowFrom, int colFrom, int rowTo, int colTo)
        {
            try
            {
                var piece = game.Board[rowFrom, colFrom];
                if (piece.Type == PieceType.Empty || piece.IsWhite != game.WhiteToMove)
                {
                    LogDebug($"Invalid move attempt in game {game.GameId}: {rowFrom},{colFrom} to {rowTo},{colTo} (empty or wrong color)");
                    return false;
                }

                if (!game.LegalMoves.Contains((rowTo, colTo)))
                {
                    LogDebug($"Illegal move in game {game.GameId}: {rowFrom},{colFrom} to {rowTo},{colTo} (not in legal moves)");
                    return false;
                }

                var targetPiece = game.Board[rowTo, colTo];
                bool isCastling = piece.Type == PieceType.King && rowFrom == rowTo && colFrom == 3 && (colTo == 5 || colTo == 1);
                bool isEnPassant = piece.Type == PieceType.Pawn && game.EnPassant == (rowTo, colTo);

                var lastMove = new LastMove
                {
                    RowFrom = rowFrom,
                    ColFrom = colFrom,
                    RowTo = rowTo,
                    ColTo = colTo,
                    MovedPiece = new Piece { Type = piece.Type, IsWhite = piece.IsWhite },
                    CapturedPiece = targetPiece.Type != PieceType.Empty ? new Piece { Type = targetPiece.Type, IsWhite = targetPiece.IsWhite } : null,
                    CastlingBeforeMove = game.Castling,
                    EnPassantBeforeMove = game.EnPassant,
                    WasCastling = isCastling,
                    WasEnPassant = isEnPassant,
                    WhiteToMoveBefore = game.WhiteToMove
                };

                if (targetPiece.Type == PieceType.King)
                {
                    game.GameEnded = true;
                    game.Status = piece.IsWhite ? "Blue wins!" : "Red wins!";
                    game.LastMove = lastMove;
                    RecordGameResult(game, piece.IsWhite ? "Blue" : "Red");
                    ShowBannerToAll($"Game {game.GameId}: {game.Status} (King captured!)");
                    SaveData();
                    return true;
                }

                LogDebug($"Pre-move board state for game {game.GameId}:\n{DumpBoardState(game)}");

                if (isCastling)
                {
                    LogDebug($"Executing castling move in game {game.GameId}: King from {rowFrom},{colFrom} to {rowTo},{colTo}");
                    int rookFromCol = colTo == 5 ? 7 : 0;
                    int rookToCol = colTo == 5 ? 4 : 2;
                    var rookPiece = game.Board[rowTo, rookFromCol];

                    if (rookPiece == null || rookPiece.Type != PieceType.Rook || rookPiece.IsWhite != piece.IsWhite)
                    {
                        LogDebug($"Invalid rook at {rowTo},{rookFromCol} for castling in game {game.GameId}");
                        return false;
                    }

                    // Move rook
                    game.Board[rowTo, rookToCol] = rookPiece;
                    game.Board[rowTo, rookFromCol] = new Piece { Type = PieceType.Empty };
                    LogDebug($"Castling: Rook moved from {rowTo},{rookFromCol} to {rowTo},{rookToCol}");

                    // Move king
                    game.Board[rowTo, colTo] = piece;
                    game.Board[rowFrom, colFrom] = new Piece { Type = PieceType.Empty };
                    LogDebug($"Castling: King moved from {rowFrom},{colFrom} to {rowTo},{colTo}");

                    if (piece.IsWhite)
                    {
                        game.KingPos.WhiteRow = rowTo;
                        game.KingPos.WhiteCol = colTo;
                        game.Castling = (false, false, game.Castling.BlackKingside, game.Castling.BlackQueenside);
                    }
                    else
                    {
                        game.KingPos.BlackRow = rowTo;
                        game.KingPos.BlackCol = colTo;
                        game.Castling = (game.Castling.WhiteKingside, game.Castling.WhiteQueenside, false, false);
                    }
                }
                else
                {
                    game.Board[rowTo, colTo] = piece;
                    game.Board[rowFrom, colFrom] = new Piece { Type = PieceType.Empty };

                    if (piece.Type == PieceType.King)
                    {
                        if (piece.IsWhite)
                        {
                            game.KingPos.WhiteRow = rowTo;
                            game.KingPos.WhiteCol = colTo;
                            game.Castling = (false, false, game.Castling.BlackKingside, game.Castling.BlackQueenside);
                        }
                        else
                        {
                            game.KingPos.BlackRow = rowTo;
                            game.KingPos.BlackCol = colTo;
                            game.Castling = (game.Castling.WhiteKingside, game.Castling.WhiteQueenside, false, false);
                        }
                    }
                    else if (piece.Type == PieceType.Rook)
                    {
                        if (piece.IsWhite)
                        {
                            if (rowFrom == 0 && colFrom == 0)
                                game.Castling = (game.Castling.WhiteKingside, false, game.Castling.BlackKingside, game.Castling.BlackQueenside);
                            else if (rowFrom == 0 && colFrom == 7)
                                game.Castling = (false, game.Castling.WhiteQueenside, game.Castling.BlackKingside, game.Castling.BlackQueenside);
                        }
                        else
                        {
                            if (rowFrom == 7 && colFrom == 0)
                                game.Castling = (game.Castling.WhiteKingside, game.Castling.WhiteQueenside, game.Castling.BlackKingside, false);
                            else if (rowFrom == 7 && colFrom == 7)
                                game.Castling = (game.Castling.WhiteKingside, game.Castling.WhiteQueenside, false, game.Castling.BlackQueenside);
                        }
                    }
                    else if (piece.Type == PieceType.Pawn)
                    {
                        if ((piece.IsWhite && rowTo == 7) || (!piece.IsWhite && rowTo == 0))
                            game.Board[rowTo, colTo] = new Piece { Type = PieceType.Queen, IsWhite = piece.IsWhite };
                        game.EnPassant = null;
                        if (Math.Abs(rowFrom - rowTo) == 2)
                            game.EnPassant = (rowFrom + (piece.IsWhite ? 1 : -1), colFrom);
                        else if (game.EnPassant == (rowTo, colTo))
                            game.Board[piece.IsWhite ? rowTo - 1 : rowTo + 1, colTo] = new Piece { Type = PieceType.Empty };
                    }
                }

                game.LastMove = lastMove;

                LogDebug($"Post-move board state for game {game.GameId}:\n{DumpBoardState(game)}");
                if (isCastling)
                    LogDebug($"Castling completed: Board[{rowTo},{(colTo == 5 ? 4 : 2)}]={game.Board[rowTo, (colTo == 5 ? 4 : 2)]?.Type}, Board[{rowTo},{(colTo == 5 ? 7 : 0)}]={game.Board[rowTo, (colTo == 5 ? 7 : 0)]?.Type}");

                game.WhiteToMove = !game.WhiteToMove;
                game.TurnNumber++;
                game.UndoUsedThisTurn = false;
                game.IsCastlingSelected = false;
                SaveData();
                LogDebug($"Board state saved for game {game.GameId} after move");

                if (IsCheckmate(game))
                {
                    game.GameEnded = true;
                    game.Status = game.WhiteToMove ? "Red wins!" : "Blue wins!";
                    RecordGameResult(game, game.WhiteToMove ? "Red" : "Blue");
                    ShowBannerToAll($"Game {game.GameId}: {game.Status}");
                }
                else if (IsStalemate(game))
                {
                    game.GameEnded = true;
                    game.Status = "Draw by stalemate!";
                    RecordGameResult(game, "Draw");
                    ShowBannerToAll($"Game {game.GameId}: {game.Status}");
                }
                else if (IsInsufficientMaterial(game))
                {
                    game.GameEnded = true;
                    game.Status = "Draw by insufficient material!";
                    RecordGameResult(game, "Draw");
                    ShowBannerToAll($"Game {game.GameId}: {game.Status}");
                }

                LogDebug($"Move completed in game {game.GameId}: {rowFrom},{colFrom} to {rowTo},{colTo}");
                return true;
            }
            catch (Exception ex)
            {
                Puts($"Error in MakeMove for game {game.GameId}: {ex.Message}");
                return false;
            }
        }

        private void RequestUndo(IPlayer player, ChessGame game)
        {
            if (game.GameEnded)
            {
                player.Reply("Cannot request undo after the game has ended!");
                return;
            }

            if (game.IsSolo)
            {
                game.UndoRequested = true;
                SaveData();
                player.Reply("Undo request accepted (solo mode). Click 'Undo' to revert the last move.");
            }
            else
            {
                if (game.UndoRequested)
                {
                    player.Reply("An undo request is already pending!");
                    return;
                }

                game.UndoRequested = true;
                SaveData();

                var otherPlayerId = game.WhiteToMove ? game.RedPlayerId : game.BluePlayerId;
                var otherPlayer = covalence.Players.FindPlayerById(otherPlayerId);
                if (otherPlayer != null && otherPlayer.IsConnected)
                {
                    otherPlayer.Reply($"{player.Name} has requested to undo the last move. Click 'Undo Requested (Accept?)' to accept.");
                }

                player.Reply("Undo request sent to the other player.");
            }

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.UserIDString == game.BluePlayerId || p.UserIDString == game.RedPlayerId)
                    ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, false);
                else if (game.SpectatorIds.Contains(p.UserIDString))
                    ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, true);
            }
        }

        private void AcceptUndo(IPlayer player, ChessGame game)
        {
            if (!game.UndoRequested)
            {
                player.Reply("No undo request is pending!");
                return;
            }

            if (game.IsSolo)
            {
                player.Reply("Undo requests are not needed in solo mode!");
                return;
            }

            var requestingPlayerId = game.WhiteToMove ? game.RedPlayerId : game.BluePlayerId;
            if (player.Id == requestingPlayerId)
            {
                player.Reply("You cannot accept your own undo request!");
                return;
            }

            game.LastUndoTurn = game.TurnNumber;
            SaveData();

            player.Reply("Undo request accepted. The 'Undo' label is now available for one use this turn.");

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.UserIDString == game.BluePlayerId || p.UserIDString == game.RedPlayerId)
                    ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, false);
                else if (game.SpectatorIds.Contains(p.UserIDString))
                    ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, true);
            }
        }

        private void PerformUndo(ChessGame game, IPlayer player)
        {
            if (game.GameEnded)
            {
                player.Reply("Cannot undo after the game has ended!");
                return;
            }

            if (game.LastMove == null)
            {
                player.Reply("No moves to undo!");
                return;
            }

            if (!game.IsSolo && game.LastUndoTurn != game.TurnNumber)
            {
                player.Reply("Undo is only available after an accepted undo request!");
                return;
            }

            if (game.UndoUsedThisTurn)
            {
                player.Reply("Undo has already been used this turn!");
                return;
            }

            var lastMove = game.LastMove;

            game.Board[lastMove.RowFrom, lastMove.ColFrom] = lastMove.MovedPiece;
            game.Board[lastMove.RowTo, lastMove.ColTo] = lastMove.CapturedPiece ?? new Piece { Type = PieceType.Empty };

            if (lastMove.WasCastling)
            {
                if (lastMove.ColTo == 5) // Kingside
                {
                    game.Board[lastMove.RowTo, 7] = new Piece { Type = PieceType.Rook, IsWhite = lastMove.MovedPiece.IsWhite };
                    game.Board[lastMove.RowTo, 4] = new Piece { Type = PieceType.Empty };
                    LogDebug($"Undoing castling: Rook restored to {lastMove.RowTo},7");
                }
                else if (lastMove.ColTo == 1) // Queenside
                {
                    game.Board[lastMove.RowTo, 0] = new Piece { Type = PieceType.Rook, IsWhite = lastMove.MovedPiece.IsWhite };
                    game.Board[lastMove.RowTo, 2] = new Piece { Type = PieceType.Empty };
                    LogDebug($"Undoing castling: Rook restored to {lastMove.RowTo},0");
                }
            }

            if (lastMove.WasEnPassant)
            {
                game.Board[lastMove.MovedPiece.IsWhite ? lastMove.RowTo - 1 : lastMove.RowTo + 1, lastMove.ColTo] = lastMove.CapturedPiece;
            }

            if (lastMove.MovedPiece.Type == PieceType.King)
            {
                if (lastMove.MovedPiece.IsWhite)
                {
                    game.KingPos.WhiteRow = lastMove.RowFrom;
                    game.KingPos.WhiteCol = lastMove.ColFrom;
                }
                else
                {
                    game.KingPos.BlackRow = lastMove.RowFrom;
                    game.KingPos.BlackCol = lastMove.ColFrom;
                }
            }

            game.Castling = lastMove.CastlingBeforeMove;
            game.EnPassant = lastMove.EnPassantBeforeMove;
            game.WhiteToMove = lastMove.WhiteToMoveBefore;
            game.UndoUsedThisTurn = true;
            game.LastMove = null;
            game.UndoRequested = false;
            game.IsCastlingSelected = false;
            SaveData();

            player.Reply("Last move undone.");

            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.UserIDString == game.BluePlayerId || p.UserIDString == game.RedPlayerId)
                    ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, false);
                else if (game.SpectatorIds.Contains(p.UserIDString))
                    ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, true);
            }
        }

        private void ShowGUI(IPlayer player, ChessGame game, bool isSpectator)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            DestroyGUI(basePlayer);

            var container = new CuiElementContainer();
            string panelName = $"ChessBoard_{player.Id}_{game.GameId}";
            playerGUIs[player.Id] = panelName;

            container.Add(new CuiPanel
            {
                Image = { Color = config.Colors["Background"] },
                RectTransform = { AnchorMin = "0.2 0.2", AnchorMax = "0.8 0.8" },
                CursorEnabled = true
            }, "Overlay", panelName);

            string playerStatus = isSpectator ? "Spectating" : (game.BluePlayerId == player.Id ? "Blue" : game.RedPlayerId == player.Id ? "Red" : "Spectating");
            string displayStatus = game.Status.Replace("Blue", "Blue").Replace("Red", "Red");
            container.Add(new CuiLabel
            {
                Text = { Text = $"Chess (Game {game.GameId}) - {displayStatus} [{playerStatus}]", FontSize = 18, Align = TextAnchor.MiddleCenter, Color = config.Colors["Highlight"] },
                RectTransform = { AnchorMin = "0.1 0.9", AnchorMax = "0.9 1.0" }
            }, panelName);

            if (!isSpectator && !game.GameEnded)
            {
                string instructionText = game.IsCastlingSelected ? "Select castling destination" : "Click to select and move";
                container.Add(new CuiLabel
                {
                    Text = { Text = instructionText, FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                    RectTransform = { AnchorMin = "0.1 0.85", AnchorMax = "0.9 0.9" }
                }, panelName);

                string undoLabelText = game.UndoRequested ? "Undo Requested (Accept?)" : (game.IsSolo || game.LastUndoTurn == game.TurnNumber && !game.UndoUsedThisTurn ? "Undo" : "Request Undo");
                string undoCommand = game.UndoRequested ? $"chess acceptundo {game.GameId}" : (game.IsSolo || game.LastUndoTurn == game.TurnNumber && !game.UndoUsedThisTurn ? $"chess undo {game.GameId}" : $"chess requestundo {game.GameId}");
                string undoLabelColor = (game.IsSolo || game.LastUndoTurn == game.TurnNumber && !game.UndoUsedThisTurn) ? config.Colors["UndoActiveLabel"] : config.Colors["UndoLabel"];
                container.Add(new CuiButton
                {
                    Button = { Color = "0 0 0 0", Command = undoCommand },
                    RectTransform = { AnchorMin = "0.4 0.85", AnchorMax = "0.6 0.9" },
                    Text = { Text = undoLabelText, FontSize = 12, Align = TextAnchor.MiddleCenter, Color = undoLabelColor, Font = "robotocondensed-bold.ttf" }
                }, panelName);
            }

            float squareSize = 0.0875f;
            for (int row = 0; row < 8; row++)
            for (int col = 0; col < 8; col++)
            {
                string squareName = $"Square_{row}_{col}";
                string squareColor = (row + col) % 2 == 0 ? config.Colors["LightSquare"] : config.Colors["DarkSquare"];
                if (!isSpectator && game.SelectedSquare == (row, col))
                    squareColor = config.Colors["Selected"];
                else if (!isSpectator && game.LegalMoves.Contains((row, col)))
                    squareColor = config.Colors["Highlight"];

                var piece = game.Board[row, col];
                string pieceColor = piece.Type == PieceType.Empty ? "1 1 1 1" : (piece.IsWhite ? "0 0 1 1" : "1 0 0 1");

                container.Add(new CuiButton
                {
                    Button = { Color = squareColor, Command = isSpectator || game.GameEnded ? "" : $"chess click {game.GameId} {row} {col}" },
                    RectTransform = { AnchorMin = $"{0.1 + col * squareSize} {0.1 + (7 - row) * squareSize}", AnchorMax = $"{0.1 + (col + 1) * squareSize} {0.1 + (8 - row) * squareSize}" },
                    Text = { Text = GetPieceSymbol(piece), FontSize = 24, Align = TextAnchor.MiddleCenter, Color = pieceColor }
                }, panelName, squareName);
            }

            container.Add(new CuiButton
            {
                Button = { Color = config.Colors["ButtonClose"], Close = panelName },
                RectTransform = { AnchorMin = "0.7 0.05", AnchorMax = "0.9 0.1" },
                Text = { Text = "Close", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
            }, panelName);

            if (!isSpectator && (string.IsNullOrEmpty(game.BluePlayerId) || string.IsNullOrEmpty(game.RedPlayerId)) && !game.GameEnded)
            {
                container.Add(new CuiButton
                {
                    Button = { Color = config.Colors["Button"], Command = $"chess join {game.GameId}" },
                    RectTransform = { AnchorMin = "0.1 0.05", AnchorMax = "0.3 0.1" },
                    Text = { Text = "Join Game", FontSize = 12, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" }
                }, panelName);
            }

            CuiHelper.AddUi(basePlayer, container);
            LogDebug($"Showing Chess GUI for {player.Name} (ID: {player.Id}) in game {game.GameId} ({(isSpectator ? "Spectator" : "Player")})");
        }

        private void RegisterCommands()
        {
            AddCovalenceCommand("chess", nameof(ChessCommand));
            AddCovalenceCommand("spectate", nameof(SpectateCommand));
            AddCovalenceCommand("chesshelp", nameof(HelpCommand));
        }

        private void ChessCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(PlayerPermission))
            {
                player.Reply("You don't have permission to use this command!");
                return;
            }

            if (args.Length == 0)
            {
                player.Reply("Usage: /chess [join <gameid>|create [solo]|reset <gameid>|clearall|stats|requestundo <gameid>|acceptundo <gameid>|undo <gameid>]\nFor rules and info, use /chesshelp");
                return;
            }

            switch (args[0].ToLower())
            {
                case "join":
                    if (args.Length != 2)
                    {
                        player.Reply("Usage: /chess join <gameid>");
                        return;
                    }
                    JoinGame(player, args[1]);
                    break;
                case "create":
                    bool isSolo = args.Length > 1 && args[1].ToLower() == "solo";
                    CreateGame(player, isSolo);
                    break;
                case "reset":
                    if (args.Length != 2)
                    {
                        player.Reply("Usage: /chess reset <gameid>");
                        return;
                    }
                    if (player.HasPermission(AdminPermission))
                        ResetGame(args[1], player);
                    else
                        player.Reply("You don't have permission to reset games!");
                    break;
                case "clearall":
                    if (player.HasPermission(AdminPermission))
                        ClearAllGames(player);
                    else
                        player.Reply("You don't have permission to clear all games!");
                    break;
                case "stats":
                    ShowStats(player);
                    break;
                case "requestundo":
                    if (args.Length != 2)
                    {
                        player.Reply("Usage: /chess requestundo <gameid>");
                        return;
                    }
                    if (!games.TryGetValue(args[1], out var gameRequestUndo))
                    {
                        player.Reply("Game not found!");
                        return;
                    }
                    RequestUndo(player, gameRequestUndo);
                    break;
                case "acceptundo":
                    if (args.Length != 2)
                    {
                        player.Reply("Usage: /chess acceptundo <gameid>");
                        return;
                    }
                    if (!games.TryGetValue(args[1], out var gameAcceptUndo))
                    {
                        player.Reply("Game not found!");
                        return;
                    }
                    AcceptUndo(player, gameAcceptUndo);
                    break;
                case "undo":
                    if (args.Length != 2)
                    {
                        player.Reply("Usage: /chess undo <gameid>");
                        return;
                    }
                    if (!games.TryGetValue(args[1], out var gameUndo))
                    {
                        player.Reply("Game not found!");
                        return;
                    }
                    PerformUndo(gameUndo, player);
                    break;
                default:
                    player.Reply("Usage: /chess [join <gameid>|create [solo]|reset <gameid>|clearall|stats|requestundo <gameid>|acceptundo <gameid>|undo <gameid>]\nFor rules and info, use /chesshelp");
                    break;
            }
        }

        private void HelpCommand(IPlayer player, string command, string[] args)
        {
            var helpMessage = @"Welcome to Chess in Rust! Here’s a simple guide for you, like explaining to a super smart third grader!

=== How to Play Chess ===
Chess is a fun game on an 8x8 board with 6 types of pieces. Your goal is to trap the other player’s king so it can’t escape (called checkmate) or capture it. You take turns moving one piece at a time. Blue pieces move first!

**Pieces and How They Move**:
- **Pawn (♙ or ♟)**: Like a brave little soldier! Moves forward 1 square (or 2 from its starting row). Captures diagonally 1 square. If it reaches the other side, it becomes a queen!
- **Rook (♖ or ♜)**: Like a tank! Moves straight up, down, left, or right as far as it wants, unless blocked.
- **Knight (♘ or ♞)**: Like a sneaky horse! Jumps in an L-shape: 2 squares one way, then 1 to the side. Can jump over other pieces!
- **Bishop (♗ or ♝)**: Like a sharp shooter! Moves diagonally as far as it wants, unless blocked.
- **Queen (♕ or ♛)**: The superstar! Moves any direction (straight or diagonal) as far as it wants, unless blocked.
- **King (♔ or ♚)**: The big boss! Moves 1 square any direction. Protect it! If it’s trapped (checkmate) or captured, you lose!

**Special Rules**:
- **Check**: If a piece can attack the king, it’s in check. You *must* move to save the king (block, capture, or move the king).
- **Checkmate**: If the king can’t escape check, the game ends, and the other player wins!
- **Draw**: If no one can win (like stalemate or not enough pieces), it’s a tie.
- **Castling**: A special move where the king slides 2 squares toward a rook, and the rook jumps to the other side. Only if neither has moved and the path is clear!

**Winning**: Trap the enemy king in checkmate or capture it! You’ll see a big “Blue wins!” or “Red wins!” message.

=== Playing Chess in Rust ===
Here’s how to play chess in this game:
- **Create a Game**: Type `/chess create` to start a new game. You’ll get a game ID (like a secret code). Others can join it!
- **Solo Game**: Type `/chess create solo` to practice by yourself, playing both sides.
- **Join a Game**: Type `/chess join <gameid>` to jump into someone’s game. You’ll be Blue (first move) or Red.
- **Spectate**: Type `/spectate <gameid>` to watch a game without playing.
- **Check Stats**: Type `/chess stats` to see your wins, losses, draws, and the top 5 players.
- **Admin Stuff**: If you’re a server boss, use `/chess reset <gameid>` to restart a game or `/chess clearall` to wipe all games.
- **Undo a Move**: Click the 'Request Undo' button (yellow) to ask the other player to undo the last move. If they accept, it turns green ('Undo') and clicking it undoes the move (once per turn). In solo mode, click twice to undo.

**How to Move**:
- Click a piece to select it (it turns yellow). Green squares show where it can go.
- Click a green square to move. For castling, select the king, then click the highlighted destination square. If it’s wrong, it’ll say “Invalid move!”
- Win and you’ll earn 100 ECON and 160 RP (cool points for the server)!

Have fun playing chess! Type `/chess` to start, or ask an adult for help if you’re stuck. 😊";

            player.Reply(helpMessage);
        }

        private void AddSlidingMoves(ChessGame game, HashSet<(int Row, int Col)> moves, int row, int col, bool isWhite, (int, int)[] directions)
        {
            foreach (var (dr, dc) in directions)
            {
                int r = row, c = col;
                while (true)
                {
                    r += dr;
                    c += dc;
                    if (r < 0 || r >= 8 || c < 0 || c >= 8) break;
                    if (game.Board[r, c].Type == PieceType.Empty)
                        moves.Add((r, c));
                    else
                    {
                        if (game.Board[r, c].IsWhite != isWhite)
                            moves.Add((r, c));
                        break;
                    }
                }
            }
        }

        private bool IsCheckmate(ChessGame game)
        {
            if (!IsInCheck(game, game.WhiteToMove)) return false;
            for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = game.Board[r, c];
                if (piece.Type != PieceType.Empty && piece.IsWhite == game.WhiteToMove)
                {
                    if (GetLegalMoves(game, r, c).Count > 0)
                        return false;
                }
            }
            return true;
        }

        private bool IsStalemate(ChessGame game)
        {
            if (IsInCheck(game, game.WhiteToMove)) return false;
            for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = game.Board[r, c];
                if (piece.Type != PieceType.Empty && piece.IsWhite == game.WhiteToMove)
                {
                    if (GetLegalMoves(game, r, c).Count > 0)
                        return false;
                }
            }
            return true;
        }

        private bool IsInsufficientMaterial(ChessGame game)
        {
            int whiteKnights = 0, whiteBishops = 0, blackKnights = 0, blackBishops = 0;
            bool whiteHasOther = false, blackHasOther = false;

            for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = game.Board[r, c];
                if (piece.Type == PieceType.Empty) continue;

                if (piece.IsWhite)
                {
                    if (piece.Type == PieceType.Knight) whiteKnights++;
                    else if (piece.Type == PieceType.Bishop) whiteBishops++;
                    else if (piece.Type != PieceType.King) whiteHasOther = true;
                }
                else
                {
                    if (piece.Type == PieceType.Knight) blackKnights++;
                    else if (piece.Type == PieceType.Bishop) blackBishops++;
                    else if (piece.Type != PieceType.King) blackHasOther = true;
                }
            }

            if (!whiteHasOther && !blackHasOther && whiteKnights == 0 && whiteBishops == 0 && blackKnights == 0 && blackBishops == 0)
                return true;

            if (!whiteHasOther && !blackHasOther &&
                ((whiteKnights == 1 && whiteBishops == 0 && blackKnights == 0 && blackBishops == 0) ||
                 (whiteBishops == 1 && whiteKnights == 0 && blackKnights == 0 && blackBishops == 0) ||
                 (blackKnights == 1 && blackBishops == 0 && whiteKnights == 0 && whiteBishops == 0) ||
                 (blackBishops == 1 && blackKnights == 0 && whiteKnights == 0 && whiteBishops == 0)))
                return true;

            return false;
        }

        private bool IsInCheck(ChessGame game, bool isWhite)
        {
            var kingPos = isWhite ? (game.KingPos.WhiteRow, game.KingPos.WhiteCol) : (game.KingPos.BlackRow, game.KingPos.BlackCol);
            for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
            {
                var piece = game.Board[r, c];
                if (piece.Type != PieceType.Empty && piece.IsWhite != isWhite)
                {
                    var moves = GetLegalMoves(game, r, c);
                    foreach (var (moveRow, moveCol) in moves)
                    {
                        if (moveRow == kingPos.Item1 && moveCol == kingPos.Item2)
                        {
                            LogDebug($"King {(isWhite ? "White" : "Black")} in check by {piece.Type} at {r},{c}");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void RecordGameResult(ChessGame game, string result)
        {
            if (game.IsSolo) return;

            if (!playerStats.ContainsKey(game.BluePlayerId))
                playerStats[game.BluePlayerId] = new PlayerStats();
            if (!playerStats.ContainsKey(game.RedPlayerId))
                playerStats[game.RedPlayerId] = new PlayerStats();

            if (result == "Blue")
            {
                playerStats[game.BluePlayerId].Wins++;
                playerStats[game.RedPlayerId].Losses++;
                AwardRewards(game.BluePlayerId);
            }
            else if (result == "Red")
            {
                playerStats[game.RedPlayerId].Wins++;
                playerStats[game.BluePlayerId].Losses++;
                AwardRewards(game.RedPlayerId);
            }
            else if (result == "Draw")
            {
                playerStats[game.BluePlayerId].Draws++;
                playerStats[game.RedPlayerId].Draws++;
            }

            SaveStats();
        }

        private void AwardRewards(string playerId)
        {
            var player = covalence.Players.FindPlayerById(playerId);
            if (player == null) return;

            if (Economics != null)
            {
                Economics.Call("Deposit", playerId, 100.0);
                player.Reply("You earned 100 ECON for your chess victory!");
            }

            if (ServerRewards != null)
            {
                ServerRewards.Call("AddPoints", playerId, 160);
                player.Reply("You earned 160 RP for your chess victory!");
            }
        }

        private string GetPieceSymbol(Piece piece)
        {
            if (piece.Type == PieceType.Empty) return "";
            var symbols = piece.IsWhite ?
                new Dictionary<PieceType, string>
                {
                    { PieceType.Pawn, "♙" },
                    { PieceType.Knight, "♘" },
                    { PieceType.Bishop, "♗" },
                    { PieceType.Rook, "♖" },
                    { PieceType.Queen, "♕" },
                    { PieceType.King, "♔" }
                } :
                new Dictionary<PieceType, string>
                {
                    { PieceType.Pawn, "♟" },
                    { PieceType.Knight, "♞" },
                    { PieceType.Bishop, "♝" },
                    { PieceType.Rook, "♜" },
                    { PieceType.Queen, "♛" },
                    { PieceType.King, "♚" }
                };
            return symbols[piece.Type];
        }

        private void DestroyGUI(BasePlayer player)
        {
            if (player == null) return;
            if (playerGUIs.TryGetValue(player.UserIDString, out string panelName))
            {
                CuiHelper.DestroyUi(player, panelName);
                playerGUIs.Remove(player.UserIDString);
            }
        }

        private void ShowBannerToAll(string message)
        {
            var container = new CuiElementContainer();
            string panelName = "ChessBanner";

            container.Add(new CuiPanel
            {
                Image = { Color = config.Colors["Background"] },
                RectTransform = { AnchorMin = "0 0.9", AnchorMax = "1 0.95" },
                CursorEnabled = false
            }, "Overlay", panelName);

            container.Add(new CuiLabel
            {
                Text = { Text = message, FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "1 1 1 1" },
                RectTransform = { AnchorMin = "0 0", AnchorMax = "1 1" }
            }, panelName);

            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player != null && player.IsConnected)
                {
                    CuiHelper.DestroyUi(player, panelName);
                    CuiHelper.AddUi(player, container);
                }
            }

            timer.Once(10f, () =>
            {
                foreach (var player in BasePlayer.activePlayerList)
                {
                    if (player != null && player.IsConnected)
                        CuiHelper.DestroyUi(player, panelName);
                }
            });
        }

        private void SpectateCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(PlayerPermission))
            {
                player.Reply("You don't have permission to use this command!");
                return;
            }

            if (args.Length != 1)
            {
                player.Reply("Usage: /spectate <gameid>");
                return;
            }

            if (!games.TryGetValue(args[0], out var game))
            {
                player.Reply("Game not found!");
                return;
            }

            game.SpectatorIds.Add(player.Id);
            ShowGUI(player, game, true);
            player.Reply($"Now spectating game {args[0]}.");
            SaveData();
        }

        private void ShowStats(IPlayer player)
        {
            var stats = playerStats.GetValueOrDefault(player.Id, new PlayerStats());
            var message = $"Chess Stats for {player.Name}:\n" +
                          $"Wins: {stats.Wins}\n" +
                          $"Losses: {stats.Losses}\n" +
                          $"Draws: {stats.Draws}\n\n" +
                          "Leaderboard (Top 5):\n";

            var topPlayers = playerStats.OrderByDescending(x => x.Value.Wins).Take(5).ToList();
            int rank = 1;
            foreach (var entry in topPlayers)
            {
                var p = covalence.Players.FindPlayerById(entry.Key);
                message += $"{rank}. {p?.Name ?? "Unknown"} - {entry.Value.Wins} Wins\n";
                rank++;
            }

            player.Reply(message);
        }

        private void CreateGame(IPlayer player, bool isSolo = false)
        {
            string gameId = Guid.NewGuid().ToString("N").Substring(0, 8);
            var game = new ChessGame(gameId) { IsSolo = isSolo };
            games[gameId] = game;
            if (isSolo)
            {
                game.BluePlayerId = player.Id;
                game.RedPlayerId = player.Id;
                game.Status = "Solo Game in progress";
            }
            SaveData();
            player.Reply($"Created new chess game with ID: {gameId}. Join with /chess join {gameId}");
            ShowBannerToAll($"{player.Name} created a new chess game ({gameId})!");
        }

        private void JoinGame(IPlayer player, string gameId)
        {
            if (!games.TryGetValue(gameId, out var game))
            {
                player.Reply("Game not found!");
                return;
            }

            if (game.IsSolo)
            {
                if (game.BluePlayerId == player.Id)
                {
                    player.Reply("Opening your solo game!");
                    ShowGUI(player, game, false);
                    return;
                }
                else
                {
                    player.Reply("This is a solo game! Create a new game with /chess create or /chess create solo.");
                    return;
                }
            }

            if (game.BluePlayerId == player.Id || game.RedPlayerId == player.Id)
            {
                player.Reply("You're already in this game!");
                ShowGUI(player, game, false);
                return;
            }

            if (string.IsNullOrEmpty(game.BluePlayerId))
            {
                game.BluePlayerId = player.Id;
                player.Reply("Joined as Blue!");
                game.Status = string.IsNullOrEmpty(game.RedPlayerId) ? "Waiting for Red" : "Game in progress";
                SaveData();
            }
            else if (string.IsNullOrEmpty(game.RedPlayerId))
            {
                game.RedPlayerId = player.Id;
                player.Reply("Joined as Red!");
                game.Status = "Game in progress";
                SaveData();
            }
            else
            {
                player.Reply("Game is full! Spectate with /spectate <gameid> or create a new game with /chess create.");
                return;
            }

            ShowGUI(player, game, false);
            ShowBannerToAll($"{player.Name} joined chess game {gameId}!");
        }

        private void ResetGame(string gameId, IPlayer player)
        {
            if (!games.ContainsKey(gameId))
            {
                player.Reply("Game not found!");
                return;
            }

            var game = games[gameId];
            foreach (var p in BasePlayer.activePlayerList)
            {
                if (p.UserIDString == game.BluePlayerId || p.UserIDString == game.RedPlayerId || game.SpectatorIds.Contains(p.UserIDString))
                    DestroyGUI(p);
            }
            games.Remove(gameId);
            SaveData();
            player.Reply($"Chess game {gameId} reset!");
            ShowBannerToAll($"Chess game {gameId} has been reset!");
        }

        private void ClearAllGames(IPlayer player)
        {
            foreach (var game in games.Values)
            {
                foreach (var p in BasePlayer.activePlayerList)
                {
                    if (p.UserIDString == game.BluePlayerId || p.UserIDString == game.RedPlayerId || game.SpectatorIds.Contains(p.UserIDString))
                        DestroyGUI(p);
                }
            }
            games.Clear();
            SaveData();
            player.Reply("All chess games have been cleared!");
            ShowBannerToAll("All chess games have been cleared!");
        }

        private string DumpBoardState(ChessGame game)
        {
            var sb = new StringBuilder();
            for (int row = 7; row >= 0; row--)
            {
                for (int col = 0; col < 8; col++)
                {
                    var piece = game.Board[row, col];
                    string symbol = piece.Type == PieceType.Empty ? "." : GetPieceSymbol(piece);
                    sb.Append(symbol + " ");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player == null) return null;

            var command = arg.cmd.Name;
            var args = arg.Args;

            if (command != "chess" || args == null || args.Length < 3 || args[0] != "click") return null;

            string playerId = player.UserIDString;
            float currentTime = Time.realtimeSinceStartup;
            if (lastClickTimes.TryGetValue(playerId, out float lastClickTime) && (currentTime - lastClickTime) < 0.75f)
            {
                LogDebug($"Click debounced for player {playerId} in game {args[1]}");
                return null;
            }
            lastClickTimes[playerId] = currentTime;

            LogDebug($"Received command: {command} with args: {string.Join(", ", args)}");

            string gameId = args[1];
            if (!games.TryGetValue(gameId, out var game))
            {
                covalence.Players.FindPlayerById(playerId)?.Reply("Game not found!");
                return null;
            }

            if (game.GameEnded)
            {
                covalence.Players.FindPlayerById(playerId)?.Reply($"Game {gameId} has ended: {game.Status}. Start a new game with /chess create.");
                return null;
            }

            if (!int.TryParse(args[2], out int row) || !int.TryParse(args[3], out int col) || row < 0 || row >= 8 || col < 0 || col >= 8)
            {
                covalence.Players.FindPlayerById(playerId)?.Reply("Invalid square selection!");
                return null;
            }

            var iPlayer = covalence.Players.FindPlayerById(playerId);
            if (iPlayer == null) return null;

            if (!game.IsSolo && game.BluePlayerId != iPlayer.Id && game.RedPlayerId != iPlayer.Id)
            {
                iPlayer.Reply("You must join the game first! Use /chess join <gameid>");
                return null;
            }

            if (!game.IsSolo && ((game.WhiteToMove && game.BluePlayerId != iPlayer.Id) || (!game.WhiteToMove && game.RedPlayerId != iPlayer.Id)))
            {
                iPlayer.Reply("It's not your turn!");
                return null;
            }

            LogDebug($"Processing click at {row},{col} in game {game.GameId}. SelectedSquare: {game.SelectedSquare}, LegalMoves: {string.Join(", ", game.LegalMoves)}");

            if (game.SelectedSquare == null)
            {
                var piece = game.Board[row, col];
                if (piece.Type != PieceType.Empty && piece.IsWhite == game.WhiteToMove)
                {
                    LogDebug($"First click: Selecting piece {piece.Type} at {row},{col} in game {game.GameId}");
                    game.SelectedSquare = (row, col);
                    game.LegalMoves = GetLegalMoves(game, row, col);
                    game.IsCastlingSelected = piece.Type == PieceType.King && game.LegalMoves.Any(m => m.Row == row && (m.Col == 5 || m.Col == 1));
                    SaveData();
                    ShowGUI(iPlayer, game, false);
                    LogDebug($"Piece selected. LegalMoves updated: {string.Join(", ", game.LegalMoves)}, IsCastlingSelected: {game.IsCastlingSelected}");
                }
                else
                {
                    iPlayer.Reply("Cannot select this piece!");
                    LogDebug($"Invalid selection attempt at {row},{col} in game {game.GameId}");
                }
            }
            else
            {
                var (fromRow, fromCol) = game.SelectedSquare.Value;
                var selectedPiece = game.Board[fromRow, fromCol];
                var targetPiece = game.Board[row, col];

                if (row == fromRow && col == fromCol)
                {
                    LogDebug($"Clicked same square {row},{col} in game {game.GameId}. Deselecting.");
                    game.SelectedSquare = null;
                    game.LegalMoves.Clear();
                    game.IsCastlingSelected = false;
                    SaveData();
                    ShowGUI(iPlayer, game, false);
                }
                else if (game.LegalMoves.Contains((row, col)))
                {
                    LogDebug($"Second click: Attempting move from {fromRow},{fromCol} to {row},{col} in game {game.GameId}");
                    if (MakeMove(game, fromRow, fromCol, row, col))
                    {
                        game.SelectedSquare = null;
                        game.LegalMoves.Clear();
                        game.IsCastlingSelected = false;
                        SaveData();
                        foreach (var p in BasePlayer.activePlayerList)
                        {
                            if (p.UserIDString == game.BluePlayerId || p.UserIDString == game.RedPlayerId)
                                ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, false);
                            else if (game.SpectatorIds.Contains(p.UserIDString))
                                ShowGUI(covalence.Players.FindPlayerById(p.UserIDString), game, true);
                        }
                    }
                    else
                    {
                        iPlayer.Reply("Invalid move!");
                        game.SelectedSquare = null;
                        game.LegalMoves.Clear();
                        game.IsCastlingSelected = false;
                        SaveData();
                        ShowGUI(iPlayer, game, false);
                        LogDebug($"Invalid move attempted from {fromRow},{fromCol} to {row},{col} in game {game.GameId}");
                    }
                }
                else
                {
                    iPlayer.Reply("Invalid destination! Select a highlighted square or click the piece again to deselect.");
                    LogDebug($"Invalid destination {row},{col} clicked in game {game.GameId}. LegalMoves: {string.Join(", ", game.LegalMoves)}");
                }
            }

            return null;
        }
    }
}