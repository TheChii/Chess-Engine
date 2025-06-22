using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ChessEngine
{
    public struct Move
    {
        public int From;  //plecare
        public int To;  //destinatie
        public int? Promotion;  //null daca nu e
        public bool IsEnPassant;
        public bool IsCastling;
        public int CaptureValue; // valoarea piesei capturate (0 daca nu e captura)

        public Move(int from, int to, int? promotion = null, bool isEnPassant = false, bool isCastling = false, int captureValue = 0)
        {
            From = from;
            To = to;
            Promotion = promotion;
            IsEnPassant = isEnPassant;
            IsCastling = isCastling;
            CaptureValue = captureValue;
        }
    }
    public static class MoveGenerator
    {
        // Pre-computed lookup tables for faster calculations
        private static readonly int[] KnightOffsets = { 17, 15, 10, 6, -6, -10, -15, -17 };
        private static readonly int[] KingOffsets = { 1, -1, 8, -8, 9, 7, -7, -9 };
        private static readonly int[] RookDirections = { 1, -1, 8, -8 };
        private static readonly int[] BishopDirections = { 9, 7, -7, -9 };
        private static readonly int[] QueenDirections = { 1, -1, 8, -8, 9, 7, -7, -9 };

        // Pre-computed piece values for capture ordering
        private static readonly int[] PieceValues = { 0, 1, 3, 3, 5, 9, 1000, 0, -1, -3, -3, -5, -9, -1000 };

        // Pre-computed knight attack patterns from each square
        private static readonly ulong[] KnightAttacks = new ulong[64];

        // Pre-computed king attack patterns from each square
        private static readonly ulong[] KingAttacks = new ulong[64];

        // Pre-computed ray attacks for sliding pieces
        private static readonly ulong[] RayAttacks = new ulong[64 * 8]; // 64 squares * 8 directions

        static MoveGenerator()
        {
            InitializeAttackTables();
        }

        private static void InitializeAttackTables()
        {
            // Initialize knight attacks
            for (int square = 0; square < 64; square++)
            {
                ulong attacks = 0UL;
                int file = square % 8;
                int rank = square / 8;

                int[] knightMoves = { -17, -15, -10, -6, 6, 10, 15, 17 };
                foreach (int move in knightMoves)
                {
                    int targetSquare = square + move;
                    if (targetSquare >= 0 && targetSquare < 64)
                    {
                        int targetFile = targetSquare % 8;
                        int targetRank = targetSquare / 8;

                        int fileDiff = Math.Abs(targetFile - file);
                        int rankDiff = Math.Abs(targetRank - rank);

                        if ((fileDiff == 1 && rankDiff == 2) || (fileDiff == 2 && rankDiff == 1))
                        {
                            attacks |= 1UL << targetSquare;
                        }
                    }
                }
                KnightAttacks[square] = attacks;
            }

            // Initialize king attacks
            for (int square = 0; square < 64; square++)
            {
                ulong attacks = 0UL;
                int file = square % 8;
                int rank = square / 8;

                for (int df = -1; df <= 1; df++)
                {
                    for (int dr = -1; dr <= 1; dr++)
                    {
                        if (df == 0 && dr == 0) continue;

                        int targetFile = file + df;
                        int targetRank = rank + dr;

                        if (targetFile >= 0 && targetFile < 8 && targetRank >= 0 && targetRank < 8)
                        {
                            int targetSquare = targetRank * 8 + targetFile;
                            attacks |= 1UL << targetSquare;
                        }
                    }
                }
                KingAttacks[square] = attacks;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetPieceValue(int piece)
        {
            return PieceValues[piece + 6]; // Offset by 6 to handle negative values
        }

        // Use pre-allocated move list to avoid GC pressure
        private static readonly List<Move> MoveList = new List<Move>(256);

        public static List<Move> GenerateMoves(GameState state)
        {
            MoveList.Clear();

            // Generate moves for all pieces of the current side
            for (int square = 0; square < 64; square++)
            {
                int piece = state.board[square];
                if (piece == 0) continue;

                bool isWhite = piece > 0;
                if (isWhite != state.whiteToMove) continue;

                int absPiece = Math.Abs(piece);

                switch (absPiece)
                {
                    case 1: // Pawn
                        GeneratePawnMoves(state, square, MoveList);
                        break;
                    case 2: // Knight
                        GenerateKnightMoves(state, square, MoveList);
                        break;
                    case 3: // Bishop
                        GenerateBishopMoves(state, square, MoveList);
                        break;
                    case 4: // Rook
                        GenerateRookMoves(state, square, MoveList);
                        break;
                    case 5: // Queen
                        GenerateQueenMoves(state, square, MoveList);
                        break;
                    case 6: // King
                        GenerateKingMoves(state, square, MoveList);
                        GenerateCastlingMoves(state, square, MoveList);
                        break;
                }
            }

            // Filter illegal moves (separate method for better performance)
            FilterIllegalMoves(state, MoveList);

            // Sort by capture value for better alpha-beta pruning
            MoveList.Sort((x, y) => y.CaptureValue.CompareTo(x.CaptureValue));

            return new List<Move>(MoveList); // Return copy to avoid mutation
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateKnightMoves(GameState state, int square, List<Move> moves)
        {
            ulong attacks = KnightAttacks[square];
            int piece = state.board[square];

            while (attacks != 0)
            {
                int targetSquare = BitOperations.TrailingZeroCount(attacks);
                attacks &= attacks - 1; // Clear the least significant bit

                int targetPiece = state.board[targetSquare];
                if (targetPiece * piece <= 0) // Empty or enemy
                {
                    int captureValue = targetPiece != 0 ? GetPieceValue(targetPiece) : 0;
                    moves.Add(new Move(square, targetSquare, captureValue: captureValue));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateKingMoves(GameState state, int square, List<Move> moves)
        {
            ulong attacks = KingAttacks[square];
            int piece = state.board[square];

            while (attacks != 0)
            {
                int targetSquare = BitOperations.TrailingZeroCount(attacks);
                attacks &= attacks - 1;

                int targetPiece = state.board[targetSquare];
                if (targetPiece * piece <= 0)
                {
                    int captureValue = targetPiece != 0 ? GetPieceValue(targetPiece) : 0;
                    moves.Add(new Move(square, targetSquare, captureValue: captureValue));
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateSlidingMoves(GameState state, int square, int[] directions, List<Move> moves)
        {
            int piece = state.board[square];

            foreach (int direction in directions)
            {
                int current = square;

                while (true)
                {
                    current += direction;

                    // Check bounds and wrapping
                    if (current < 0 || current >= 64) break;

                    int currentFile = current % 8;
                    int prevFile = (current - direction) % 8;

                    // Check for horizontal wrapping
                    if (Math.Abs(direction) == 1 && Math.Abs(currentFile - prevFile) > 1) break;

                    // Check for diagonal wrapping  
                    if (Math.Abs(direction) == 7 || Math.Abs(direction) == 9)
                    {
                        if (Math.Abs(currentFile - prevFile) != 1) break;
                    }

                    int targetPiece = state.board[current];

                    if (targetPiece == 0)
                    {
                        moves.Add(new Move(square, current));
                    }
                    else if (targetPiece * piece < 0) // Enemy piece
                    {
                        int captureValue = GetPieceValue(targetPiece);
                        moves.Add(new Move(square, current, captureValue: captureValue));
                        break;
                    }
                    else // Own piece
                    {
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateRookMoves(GameState state, int square, List<Move> moves)
        {
            GenerateSlidingMoves(state, square, RookDirections, moves);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateBishopMoves(GameState state, int square, List<Move> moves)
        {
            GenerateSlidingMoves(state, square, BishopDirections, moves);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateQueenMoves(GameState state, int square, List<Move> moves)
        {
            GenerateSlidingMoves(state, square, QueenDirections, moves);
        }

        private static void GeneratePawnMoves(GameState state, int square, List<Move> moves)
        {
            int file = square % 8;
            int rank = square / 8;
            int forward = state.whiteToMove ? -8 : 8;
            int startRank = state.whiteToMove ? 6 : 1;
            int promotionRank = state.whiteToMove ? 0 : 7;

            // Forward moves
            int oneStep = square + forward;
            if (oneStep >= 0 && oneStep < 64 && state.board[oneStep] == 0)
            {
                if (oneStep / 8 == promotionRank)
                {
                    // Promotions
                    moves.Add(new Move(square, oneStep, 5)); // Queen
                    moves.Add(new Move(square, oneStep, 4)); // Rook
                    moves.Add(new Move(square, oneStep, 3)); // Bishop
                    moves.Add(new Move(square, oneStep, 2)); // Knight
                }
                else
                {
                    moves.Add(new Move(square, oneStep));

                    // Double push from starting position
                    if (rank == startRank)
                    {
                        int twoStep = square + forward * 2;
                        if (twoStep >= 0 && twoStep < 64 && state.board[twoStep] == 0)
                        {
                            moves.Add(new Move(square, twoStep));
                        }
                    }
                }
            }

            // Captures
            int[] captureOffsets = state.whiteToMove ? new[] { -9, -7 } : new[] { 7, 9 };
            foreach (int offset in captureOffsets)
            {
                int target = square + offset;
                if (target < 0 || target >= 64) continue;

                int targetFile = target % 8;
                if (Math.Abs(targetFile - file) != 1) continue; // Not diagonal

                int targetPiece = state.board[target];
                int sourcePiece = state.board[square];

                if (targetPiece * sourcePiece < 0) // Enemy piece
                {
                    int captureValue = GetPieceValue(targetPiece);

                    if (target / 8 == promotionRank)
                    {
                        // Capture promotions
                        moves.Add(new Move(square, target, 5, captureValue: captureValue)); // Queen
                        moves.Add(new Move(square, target, 4, captureValue: captureValue)); // Rook
                        moves.Add(new Move(square, target, 3, captureValue: captureValue)); // Bishop
                        moves.Add(new Move(square, target, 2, captureValue: captureValue)); // Knight
                    }
                    else
                    {
                        moves.Add(new Move(square, target, captureValue: captureValue));
                    }
                }

                // En passant
                if (state.enPassantSquare.HasValue && state.enPassantSquare.Value == target)
                {
                    moves.Add(new Move(square, target, isEnPassant: true, captureValue: 1));
                }
            }
        }

        private static void GenerateCastlingMoves(GameState state, int kingSquare, List<Move> moves)
        {
            if (state.whiteToMove)
            {
                // White kingside castling
                if (state.whiteCanCastleKingside &&
                    state.board[61] == 0 && state.board[62] == 0 &&
                    !IsSquareAttacked(state, 60, false) &&
                    !IsSquareAttacked(state, 61, false) &&
                    !IsSquareAttacked(state, 62, false))
                {
                    moves.Add(new Move(60, 62, isCastling: true));
                }

                // White queenside castling
                if (state.whiteCanCastleQueenside &&
                    state.board[57] == 0 && state.board[58] == 0 && state.board[59] == 0 &&
                    !IsSquareAttacked(state, 60, false) &&
                    !IsSquareAttacked(state, 59, false) &&
                    !IsSquareAttacked(state, 58, false))
                {
                    moves.Add(new Move(60, 58, isCastling: true));
                }
            }
            else
            {
                // Black kingside castling
                if (state.blackCanCastleKingside &&
                    state.board[5] == 0 && state.board[6] == 0 &&
                    !IsSquareAttacked(state, 4, true) &&
                    !IsSquareAttacked(state, 5, true) &&
                    !IsSquareAttacked(state, 6, true))
                {
                    moves.Add(new Move(4, 6, isCastling: true));
                }

                // Black queenside castling
                if (state.blackCanCastleQueenside &&
                    state.board[1] == 0 && state.board[2] == 0 && state.board[3] == 0 &&
                    !IsSquareAttacked(state, 4, true) &&
                    !IsSquareAttacked(state, 3, true) &&
                    !IsSquareAttacked(state, 2, true))
                {
                    moves.Add(new Move(4, 2, isCastling: true));
                }
            }
        }

        // Optimized legality checking - only check moves that could be illegal
        private static void FilterIllegalMoves(GameState state, List<Move> moves)
        {
            for (int i = moves.Count - 1; i >= 0; i--)
            {
                if (!IsLegalMove(state, moves[i]))
                {
                    moves.RemoveAt(i);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLegalMove(GameState state, Move move)
        {
            // Make move on copy
            GameState newState = state.DeepCopy();
            newState.ApplyMove(move);

            // Find king
            int kingPiece = state.whiteToMove ? 6 : -6;
            int kingSquare = -1;

            for (int i = 0; i < 64; i++)
            {
                if (newState.board[i] == kingPiece)
                {
                    kingSquare = i;
                    break;
                }
            }

            return kingSquare != -1 && !IsSquareAttacked(newState, kingSquare, !state.whiteToMove);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSquareAttacked(GameState state, int square, bool byWhite)
        {
            // Check pawn attacks
            int[] pawnOffsets = byWhite ? new[] { -9, -7 } : new[] { 7, 9 };
            int pawnPiece = byWhite ? 1 : -1;

            foreach (int offset in pawnOffsets)
            {
                int pos = square + offset;
                if (pos >= 0 && pos < 64)
                {
                    int fromFile = square % 8;
                    int toFile = pos % 8;
                    if (Math.Abs(fromFile - toFile) == 1 && state.board[pos] == pawnPiece)
                        return true;
                }
            }

            // Check knight attacks using lookup table
            ulong knightMask = KnightAttacks[square];
            int knightPiece = byWhite ? 2 : -2;

            while (knightMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(knightMask);
                knightMask &= knightMask - 1;

                if (state.board[pos] == knightPiece)
                    return true;
            }

            // Check king attacks using lookup table
            ulong kingMask = KingAttacks[square];
            int kingPiece = byWhite ? 6 : -6;

            while (kingMask != 0)
            {
                int pos = BitOperations.TrailingZeroCount(kingMask);
                kingMask &= kingMask - 1;

                if (state.board[pos] == kingPiece)
                    return true;
            }

            // Check sliding piece attacks
            int rookPiece = byWhite ? 4 : -4;
            int bishopPiece = byWhite ? 3 : -3;
            int queenPiece = byWhite ? 5 : -5;

            // Rook/Queen horizontal and vertical
            foreach (int dir in RookDirections)
            {
                for (int current = square + dir; current >= 0 && current < 64; current += dir)
                {
                    // Check wrapping
                    int prevFile = (current - dir) % 8;
                    int currentFile = current % 8;
                    if (Math.Abs(dir) == 1 && Math.Abs(currentFile - prevFile) > 1) break;

                    int piece = state.board[current];
                    if (piece != 0)
                    {
                        if (piece == rookPiece || piece == queenPiece)
                            return true;
                        break;
                    }
                }
            }

            // Bishop/Queen diagonals
            foreach (int dir in BishopDirections)
            {
                for (int current = square + dir; current >= 0 && current < 64; current += dir)
                {
                    int prevFile = (current - dir) % 8;
                    int currentFile = current % 8;
                    if (Math.Abs(currentFile - prevFile) != 1) break;

                    int piece = state.board[current];
                    if (piece != 0)
                    {
                        if (piece == bishopPiece || piece == queenPiece)
                            return true;
                        break;
                    }
                }
            }

            return false;
        }
    }

    // Helper class for bit operations (if not available in your .NET version)
    public static class BitOperations
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeroCount(ulong value)
        {
            if (value == 0) return 64;

            int count = 0;
            while ((value & 1) == 0)
            {
                value >>= 1;
                count++;
            }
            return count;
        }
    }
}