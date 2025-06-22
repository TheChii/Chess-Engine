using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public struct Move
    {
        public int From;  //plecare
        public int To;  //destinatie
        public int? Promotion;  //null daca nu e
        public bool IsEnPassant;
        public bool IsCastling;

        public Move(int from, int to, int? promotion = null, bool isEnPassant = false, bool isCastling = false)
        {
            From = from;
            To = to;
            Promotion = promotion;
            IsEnPassant = isEnPassant;
            IsCastling = isCastling;
        }
    }

    public static class MoveGenerator
    {
        // pozitii pt piesele care sar: rege cal
        private static readonly int[] KnightOffsets = { 17, 15, 10, 6, -6, -10, -15, -17 };
        private static readonly int[] KingOffsets = { 1, -1, 8, -8, 9, 7, -7, -9 };

        // directii slide
        private static readonly int[] RookDirections = { 1, -1, 8, -8 };
        private static readonly int[] BishopDirections = { 9, 7, -7, -9 };
        private static readonly int[] QueenDirections = { 1, -1, 8, -8, 9, 7, -7, -9 };

        // mutari pseudo legale?

        public static List<Move> GenerateMoves(GameState state)
        {
            var moves = new List<Move>();

            for (int square = 0; square < 64; square++)
            {
                int piece = state.board[square];

                if (piece == (int)Piece.Empty) continue; // nu e piesă aici
                bool isWhite = piece > 0; //true daca e alba , false daca e neagra
                if (isWhite != state.whiteToMove) continue;  // daca e alba si muta albu sau daca e neagra si nu muta albu

                int absPiece = Math.Abs(piece);

                switch (absPiece)
                {
                    case (int)Piece.PawnWhite:
                        GeneratePawnMoves(state, square, moves);
                        break;
                    case (int)Piece.KnightWhite:
                        GenerateJumps(state, square, KnightOffsets, moves);
                        break;
                    case (int)Piece.BishopWhite:
                        GenerateSlides(state, square, BishopDirections, moves);
                        break;
                    case (int)Piece.RookWhite:
                        GenerateSlides(state, square, RookDirections, moves);
                        break;
                    case (int)Piece.QueenWhite:
                        GenerateSlides(state, square, QueenDirections, moves);
                        break;
                    case (int)Piece.KingWhite:
                        GenerateKingMoves(state, square, KingOffsets, moves);
                        break;
                }

            }

            // elimină mutările ilegale (care lasă regele atacat)
            moves.RemoveAll(move => !IsLegalMove(state, move));
       

            return moves;
        }

        public static void GeneratePawnMoves(GameState state, int square, List<Move> moves)
        {
            int srcFile = square % 8;
            int srcRank = square / 8;
            int forward = state.whiteToMove ? -8 : 8;
            int startRow = state.whiteToMove ? 6 : 1;
            int promotionRow = state.whiteToMove ? 0 : 7;

            // ——— Double push ———
            if (srcRank == startRow)
            {
                int midSquare = square + forward;
                int targetSquare = square + forward * 2;
                if (midSquare >= 0 && midSquare < 64 &&
                    targetSquare >= 0 && targetSquare < 64 &&
                    state.board[midSquare] == (int)Piece.Empty &&
                    state.board[targetSquare] == (int)Piece.Empty)
                {
                    // same file on double push
                    if ((targetSquare % 8) == srcFile)
                        moves.Add(new Move(square, targetSquare));
                }
            }

            // ——— Single push / promotion ———
            {
                int target = square + forward;
                if (target >= 0 && target < 64)
                {
                    int tgtFile = target % 8;
                    int tgtRank = target / 8;
                    // ensure same file
                    if (tgtFile == srcFile && state.board[target] == (int)Piece.Empty)
                    {
                        bool isPromo = (tgtRank == promotionRow);
                        if (isPromo)
                        {
                            moves.Add(new Move(square, target, (int)Piece.QueenWhite));
                            moves.Add(new Move(square, target, (int)Piece.RookWhite));
                            moves.Add(new Move(square, target, (int)Piece.BishopWhite));
                            moves.Add(new Move(square, target, (int)Piece.KnightWhite));
                        }
                        else
                        {
                            moves.Add(new Move(square, target));
                        }
                    }
                }
            }

            // ——— Captures & promotions ———
            foreach (var captureOffset in new[] { state.whiteToMove ? -9 : 7, state.whiteToMove ? -7 : 9 })
            {
                int target = square + captureOffset;
                if (target < 0 || target >= 64)
                    continue;

                int tgtFile = target % 8;
                int tgtRank = target / 8;
                int df = Math.Abs(tgtFile - srcFile);
                int dr = Math.Abs(tgtRank - srcRank);

                // must move exactly one file and one rank
                if (df == 1 && dr == 1)
                {
                    int srcPiece = state.board[square];
                    int tgtPiece = state.board[target];

                    // normal capture
                    if (tgtPiece * srcPiece < 0)
                    {
                        bool isPromo = (tgtRank == promotionRow);
                        if (isPromo)
                        {
                            moves.Add(new Move(square, target, (int)Piece.QueenWhite));
                            moves.Add(new Move(square, target, (int)Piece.RookWhite));
                            moves.Add(new Move(square, target, (int)Piece.BishopWhite));
                            moves.Add(new Move(square, target, (int)Piece.KnightWhite));
                        }
                        else
                        {
                            moves.Add(new Move(square, target));
                        }
                    }

                    //en passant
                    if (state.enPassantSquare.HasValue && state.enPassantSquare.Value == target)
                        moves.Add(new Move(square, target, isEnPassant: true));
                    
                }
            }
        }


        public static void GenerateJumps(GameState state, int square, int[] offsets, List<Move> moves)
        {
            int srcFile = square % 8;
            int srcRank = square / 8;

            foreach (var offset in offsets)
            {
                int targetSquare = square + offset;
                if (targetSquare < 0 || targetSquare >= 64)
                    continue;

                int tgtFile = targetSquare % 8;
                int tgtRank = targetSquare / 8;

                int df = Math.Abs(tgtFile - srcFile);
                int dr = Math.Abs(tgtRank - srcRank);

                // only allow legal knight jumps: (df,dr) == (1,2) or (2,1)
                if (!((df == 1 && dr == 2) || (df == 2 && dr == 1)))
                    continue;

                // now we know it's a properly-shaped L-jump on the board
                var srcPiece = state.board[square];
                var tgtPiece = state.board[targetSquare];
                if (tgtPiece * srcPiece <= 0)  // empty or enemy
                    moves.Add(new Move(square, targetSquare));
            }
        }


        public static void GenerateSlides(GameState state, int square, int[] directions, List<Move> moves)
        {
            int srcFile = square % 8;
            int srcRank = square / 8;

            foreach (var direction in directions)
            {
                int prevSquare = square;
                int prevFile = srcFile;
                int prevRank = srcRank;

                while (true)
                {
                    int targetSquare = prevSquare + direction;
                    if (targetSquare < 0 || targetSquare >= 64)
                        break;  // off-board by index

                    int tgtFile = targetSquare % 8;
                    int tgtRank = targetSquare / 8;

                    int df = Math.Abs(tgtFile - prevFile);
                    int dr = Math.Abs(tgtRank - prevRank);

                    // check that this single step is valid for our direction:
                    //  - horizontal (±1): dr == 0
                    //  - vertical (±8): df == 0
                    //  - diagonal (±7, ±9): df == 1 && dr == 1
                    bool stepValid;
                    switch (Math.Abs(direction))
                    {
                        case 1:   // east / west
                            stepValid = (dr == 0);
                            break;
                        case 8:   // north / south
                            stepValid = (df == 0);
                            break;
                        case 7:   // one diagonal
                        case 9:   // other diagonal
                            stepValid = (df == 1 && dr == 1);
                            break;
                        default:
                            stepValid = false;
                            break;
                    }
                    if (!stepValid)
                        break;  // would have wrapped around

                    int srcPiece = state.board[square];
                    int tgtPiece = state.board[targetSquare];
                    int collision = tgtPiece * srcPiece;

                    if (collision <= 0)
                    {
                        moves.Add(new Move(square, targetSquare));
                        if (collision != 0)
                            break;  // captured and stop in this direction
                    }
                    else
                    {
                        break;  // own piece blocks
                    }

                    // advance for next step
                    prevSquare = targetSquare;
                    prevFile = tgtFile;
                    prevRank = tgtRank;
                }
            }
        }


        public static void GenerateKingMoves(GameState state, int square, int[] offsets, List<Move> moves)
        {
            int srcFile = square % 8;
            int srcRank = square / 8;
            int srcPiece = state.board[square];

            foreach (var offset in offsets)
            {
                int targetSquare = square + offset;
                if (targetSquare < 0 || targetSquare >= 64)
                    continue;

                int tgtFile = targetSquare % 8;
                int tgtRank = targetSquare / 8;
                int df = Math.Abs(tgtFile - srcFile);
                int dr = Math.Abs(tgtRank - srcRank);

                // must move at most one square in any direction
                if (df <= 1 && dr <= 1 && (df + dr) > 0)
                {
                    int tgtPiece = state.board[targetSquare];
                    if (tgtPiece * srcPiece <= 0)  // empty or enemy
                    {
                        moves.Add(new Move(square, targetSquare));
                    }
                }
            }
        }

        public static bool IsLegalMove(GameState state, Move move)
        {
            // Make a deep copy of the game state
            GameState newState = state.DeepCopy();

            // Apply the move to the copied state
            newState.ApplyMove(move);

            // Find the king of the moving side (original side to move)
            int kingPiece = state.whiteToMove ? (int)Piece.KingWhite : (int)Piece.KingBlack;
            int kingSquare = -1;
            for (int i = 0; i < 64; i++)
            {
                if (newState.board[i] == kingPiece)
                {
                    kingSquare = i;
                    break;
                }
            }

            // If king not found (shouldn't happen), consider move illegal
            if (kingSquare == -1) return false;

            // Check if king is attacked by opponent after the move
            bool isKingAttacked = IsSquareAttacked(newState, kingSquare, !state.whiteToMove);

            return !isKingAttacked;
        }

        public static bool IsSquareAttacked(GameState state, int square, bool byWhite)
        {
            // Check pawn attacks
            int[] pawnOffsets = byWhite ? new int[] { -9, -7 } : new int[] { 7, 9 };
            int pawnPiece = byWhite ? (int)Piece.PawnWhite : (int)Piece.PawnBlack;
            foreach (int offset in pawnOffsets)
            {
                int pos = square + offset;
                if (pos >= 0 && pos < 64)
                {
                    // Check for board wrap
                    int fromFile = square % 8;
                    int toFile = pos % 8;
                    if (Math.Abs(fromFile - toFile) == 1) // Must be diagonal attack
                    {
                        if (state.board[pos] == pawnPiece)
                            return true;
                    }
                }
            }

            // Check knight attacks
            int knightPiece = byWhite ? (int)Piece.KnightWhite : (int)Piece.KnightBlack;
            foreach (int offset in KnightOffsets)
            {
                int pos = square + offset;
                if (pos >= 0 && pos < 64)
                {
                    int fromFile = square % 8;
                    int toFile = pos % 8;
                    int fromRank = square / 8;
                    int toRank = pos / 8;

                    int fileDiff = Math.Abs(toFile - fromFile);
                    int rankDiff = Math.Abs(toRank - fromRank);

                    // Valid knight move pattern
                    if ((fileDiff == 1 && rankDiff == 2) || (fileDiff == 2 && rankDiff == 1))
                    {
                        if (state.board[pos] == knightPiece)
                            return true;
                    }
                }
            }

            // Check king attacks
            int kingPiece = byWhite ? (int)Piece.KingWhite : (int)Piece.KingBlack;
            foreach (int offset in KingOffsets)
            {
                int pos = square + offset;
                if (pos >= 0 && pos < 64)
                {
                    int fromFile = square % 8;
                    int toFile = pos % 8;
                    int fromRank = square / 8;
                    int toRank = pos / 8;

                    int fileDiff = Math.Abs(toFile - fromFile);
                    int rankDiff = Math.Abs(toRank - fromRank);

                    // Valid king move (1 square in any direction)
                    if (fileDiff <= 1 && rankDiff <= 1)
                    {
                        if (state.board[pos] == kingPiece)
                            return true;
                    }
                }
            }

            // Check rook and queen attacks (straight lines)
            int[] rookDirections = { 1, -1, 8, -8 };
            int rookPiece = byWhite ? (int)Piece.RookWhite : (int)Piece.RookBlack;
            int queenPiece = byWhite ? (int)Piece.QueenWhite : (int)Piece.QueenBlack;

            foreach (int dir in rookDirections)
            {
                int current = square + dir;
                while (current >= 0 && current < 64)
                {
                    // Check board wrap
                    int prev = current - dir;
                    int prevFile = prev % 8;
                    int currentFile = current % 8;
                    int prevRank = prev / 8;
                    int currentRank = current / 8;

                    int fileDiff = Math.Abs(currentFile - prevFile);
                    int rankDiff = Math.Abs(currentRank - prevRank);

                    // Break if wrapped around board
                    if (fileDiff > 1 || rankDiff > 1)
                        break;

                    int piece = state.board[current];
                    if (piece != 0)
                    {
                        if (piece == rookPiece || piece == queenPiece)
                            return true;
                        break; // Blocked by piece
                    }
                    current += dir;
                }
            }

            // Check bishop and queen attacks (diagonals)
            int[] bishopDirections = { 9, 7, -7, -9 };
            int bishopPiece = byWhite ? (int)Piece.BishopWhite : (int)Piece.BishopBlack;

            foreach (int dir in bishopDirections)
            {
                int current = square + dir;
                while (current >= 0 && current < 64)
                {
                    // Check board wrap
                    int prev = current - dir;
                    int prevFile = prev % 8;
                    int currentFile = current % 8;
                    int prevRank = prev / 8;
                    int currentRank = current / 8;

                    int fileDiff = Math.Abs(currentFile - prevFile);
                    int rankDiff = Math.Abs(currentRank - prevRank);

                    // Break if wrapped around board
                    if (fileDiff > 1 || rankDiff > 1)
                        break;

                    int piece = state.board[current];
                    if (piece != 0)
                    {
                        if (piece == bishopPiece || piece == queenPiece)
                            return true;
                        break; // Blocked by piece
                    }
                    current += dir;
                }
            }

            return false;
        }
    }
}
