using System;
using System.Collections.Generic;
using System.Linq;

namespace ChessEngine
{
    public class Evaluator
    {
        // Transposition table for caching evaluations
        private static readonly Dictionary<ulong, int> TranspositionTable = new Dictionary<ulong, int>();
        private const int MaxTableSize = 1000000; // Limit table size to prevent excessive memory usage

        // Piece values
        private static readonly int[] PieceValues = {
            0,     // Empty
            100,   // Pawn
            320,   // Knight
            330,   // Bishop
            500,   // Rook
            900,   // Queen
            20000  // King
        };

        private static readonly int[] PawnTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] KnightTable = {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };

        private static readonly int[] BishopTable = {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };

        private static readonly int[] RookTable = {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10, 10, 10, 10, 10,  5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
            -5,  0,  0,  0,  0,  0,  0, -5,
             0,  0,  0,  5,  5,  0,  0,  0
        };

        private static readonly int[] QueenTable = {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        private static readonly int[] KingMiddlegameTable = {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20
        };

        private static readonly int[] KingEndgameTable = {
            -50,-40,-30,-20,-20,-30,-40,-50,
            -30,-20,-10,  0,  0,-10,-20,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-30,  0,  0,  0,  0,-30,-30,
            -50,-30,-30,-30,-30,-30,-30,-50
        };

        // Evaluation bonuses/penalties
        private const int BishopPairBonus = 30;
        private const int DoubledPawnPenalty = -10;
        private const int IsolatedPawnPenalty = -15;
        private const int PassedPawnBonus = 20;
        private const int RookOnOpenFileBonus = 15;
        private const int RookOnSemiOpenFileBonus = 10;
        private const int KnightOutpostBonus = 15;
        private const int CastlingBonus = 40;
        private const int DevelopmentBonus = 10;

        public static int EvaluateBoard(GameState state)
        {
            // Generate hash for the current position
            ulong positionHash = GeneratePositionHash(state);

            // Check if we've already evaluated this position
            if (TranspositionTable.TryGetValue(positionHash, out int cachedScore))
            {
                return cachedScore;
            }

            // Calculate evaluation
            int score = CalculateEvaluation(state);

            // Store in transposition table
            StoreEvaluation(positionHash, score);

            return score;
        }

        private static int CalculateEvaluation(GameState state)
        {
            if (IsCheckmate(state))
                return state.whiteToMove ? -30000 : 30000;

            if (IsStalemate(state) || IsInsufficientMaterial(state))
                return 0;

            int score = 0;
            bool isEndgame = IsEndgame(state);

            score += EvaluateMaterialAndPosition(state, isEndgame);
            score += EvaluatePawnStructure(state);
            score += EvaluatePieceActivity(state);

            //if (!isEndgame)
            //    score += EvaluateKingSafety(state);

            score += EvaluateSpecialBonuses(state);

            return state.whiteToMove ? score : -score;
        }

        private static ulong GeneratePositionHash(GameState state)
        {
            // Simple but effective hash function using FNV-1a algorithm
            const ulong FnvPrime = 1099511628211UL;
            const ulong FnvOffsetBasis = 14695981039346656037UL;

            ulong hash = FnvOffsetBasis;

            // Hash the board position
            for (int i = 0; i < 64; i++)
            {
                int piece = state.board[i];
                hash ^= (ulong)(piece + 128); // Offset to avoid negative values
                hash *= FnvPrime;
            }

            // Include whose turn it is in the hash
            hash ^= state.whiteToMove ? 1UL : 0UL;
            hash *= FnvPrime;

            // You might want to include castling rights, en passant, etc. if available
            // hash ^= (ulong)state.castlingRights;
            // hash *= FnvPrime;
            // hash ^= (ulong)state.enPassantSquare;
            // hash *= FnvPrime;

            return hash;
        }

        private static void StoreEvaluation(ulong hash, int score)
        {
            // Prevent unlimited growth of the transposition table
            if (TranspositionTable.Count >= MaxTableSize)
            {
                // Simple cache replacement: clear half the table when full
                // In a production engine, you'd use more sophisticated replacement schemes
                var keysToRemove = TranspositionTable.Keys.Take(MaxTableSize / 2).ToList();
                foreach (var key in keysToRemove)
                {
                    TranspositionTable.Remove(key);
                }
            }

            TranspositionTable[hash] = score;
        }

        public static void ClearTranspositionTable()
        {
            TranspositionTable.Clear();
        }

        public static int GetTranspositionTableSize()
        {
            return TranspositionTable.Count;
        }

        public static double GetCacheHitRate()
        {
            // You would need to add counters for hits and misses to implement this
            // This is just a placeholder for the method signature
            return 0.0;
        }

        private static int EvaluateMaterialAndPosition(GameState state, bool isEndgame)
        {
            int score = 0;

            for (int square = 0; square < 64; square++)
            {
                int piece = state.board[square];
                if (piece == 0) continue;

                int absPiece = Math.Abs(piece);
                int pieceValue = PieceValues[absPiece];
                int positionalValue = GetPositionalValue(absPiece, square, piece > 0, isEndgame);

                if (piece > 0)
                {
                    score += pieceValue + positionalValue;
                }
                else
                {
                    score -= pieceValue + positionalValue;
                }
            }

            return score;
        }

        private static int GetPositionalValue(int pieceType, int square, bool isWhite, bool isEndgame)
        {
            int adjustedSquare = isWhite ? square : (7 - square / 8) * 8 + (square % 8);

            return pieceType switch
            {
                1 => PawnTable[adjustedSquare],
                2 => KnightTable[adjustedSquare],
                3 => BishopTable[adjustedSquare],
                4 => RookTable[adjustedSquare],
                5 => QueenTable[adjustedSquare],
                6 => isEndgame ? KingEndgameTable[adjustedSquare] : KingMiddlegameTable[adjustedSquare],
                _ => 0
            };
        }

        private static int EvaluatePawnStructure(GameState state)
        {
            int score = 0;
            var whitePawns = GetPawnsByFile(state, true);
            var blackPawns = GetPawnsByFile(state, false);

            for (int file = 0; file < 8; file++)
            {
                var pawns = whitePawns[file];
                if (pawns.Count > 1)
                    score += DoubledPawnPenalty * (pawns.Count - 1);

                if (pawns.Count > 0)
                {
                    if (file == 0 ? whitePawns[1].Count == 0 : file == 7 ? whitePawns[6].Count == 0 :
                        whitePawns[file - 1].Count == 0 && whitePawns[file + 1].Count == 0)
                        score += IsolatedPawnPenalty;

                    int maxRank = pawns.Max(p => p / 8);
                    bool isPassed = true;
                    for (int checkFile = Math.Max(0, file - 1); checkFile <= Math.Min(7, file + 1); checkFile++)
                    {
                        if (blackPawns[checkFile].Any(p => p / 8 > maxRank))
                        {
                            isPassed = false;
                            break;
                        }
                    }
                    if (isPassed && maxRank > 1)
                        score += PassedPawnBonus * (maxRank - 1);
                }
            }

            for (int file = 0; file < 8; file++)
            {
                var pawns = blackPawns[file];
                if (pawns.Count > 1)
                    score -= DoubledPawnPenalty * (pawns.Count - 1);

                if (pawns.Count > 0)
                {
                    if (file == 0 ? blackPawns[1].Count == 0 : file == 7 ? blackPawns[6].Count == 0 :
                        blackPawns[file - 1].Count == 0 && blackPawns[file + 1].Count == 0)
                        score -= IsolatedPawnPenalty;
                    int minRank = pawns.Min(p => p / 8);
                    bool isPassed = true;
                    for (int checkFile = Math.Max(0, file - 1); checkFile <= Math.Min(7, file + 1); checkFile++)
                    {
                        if (whitePawns[checkFile].Any(p => p / 8 < minRank))
                        {
                            isPassed = false;
                            break;
                        }
                    }
                    if (isPassed && minRank < 6)
                        score -= PassedPawnBonus * (6 - minRank);
                }
            }

            return score;
        }

        private static int EvaluatePieceActivity(GameState state)
        {
            int score = 0;

            for (int square = 0; square < 64; square++)
            {
                int piece = state.board[square];
                if (Math.Abs(piece) == 4)
                {
                    int file = square % 8;
                    bool hasOwnPawn = false;
                    bool hasEnemyPawn = false;

                    for (int rank = 0; rank < 8; rank++)
                    {
                        int checkSquare = rank * 8 + file;
                        int checkPiece = state.board[checkSquare];
                        if (Math.Abs(checkPiece) == 1)
                        {
                            if ((piece > 0 && checkPiece > 0) || (piece < 0 && checkPiece < 0))
                                hasOwnPawn = true;
                            else
                                hasEnemyPawn = true;
                        }
                    }

                    if (!hasOwnPawn && !hasEnemyPawn)
                        score += piece > 0 ? RookOnOpenFileBonus : -RookOnOpenFileBonus;
                    else if (!hasOwnPawn)
                        score += piece > 0 ? RookOnSemiOpenFileBonus : -RookOnSemiOpenFileBonus;
                }
            }

            return score;
        }

        private static int EvaluateKingSafety(GameState state)
        {
            // todo
            int score = 0;

            return score;
        }

        private static int EvaluateSpecialBonuses(GameState state)
        {
            int score = 0;

            int whiteBishops = 0, blackBishops = 0;
            for (int i = 0; i < 64; i++)
            {
                if (state.board[i] == 3) whiteBishops++;
                else if (state.board[i] == -3) blackBishops++;
            }

            if (whiteBishops >= 2) score += BishopPairBonus;
            if (blackBishops >= 2) score -= BishopPairBonus;

            return score;
        }

        private static bool IsEndgame(GameState state)
        {
            int totalMaterial = 0;
            for (int i = 0; i < 64; i++)
            {
                int piece = Math.Abs(state.board[i]);
                if (piece > 1 && piece < 6)
                    totalMaterial += PieceValues[piece];
            }
            return totalMaterial < 1300;
        }

        private static List<int>[] GetPawnsByFile(GameState state, bool white)
        {
            var pawnsByFile = new List<int>[8];
            for (int i = 0; i < 8; i++)
                pawnsByFile[i] = new List<int>();

            for (int square = 0; square < 64; square++)
            {
                int piece = state.board[square];
                if ((white && piece == 1) || (!white && piece == -1))
                {
                    pawnsByFile[square % 8].Add(square);
                }
            }

            return pawnsByFile;
        }

        // Placeholder
        private static bool IsCheckmate(GameState state) => false;
        private static bool IsStalemate(GameState state) => false;
        private static bool IsInsufficientMaterial(GameState state) => false;
    }
}