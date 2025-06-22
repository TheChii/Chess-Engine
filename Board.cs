using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public enum Piece
    {
        Empty = 0,
        PawnWhite = 1,
        KnightWhite = 2,
        BishopWhite = 3,
        RookWhite = 4,
        QueenWhite = 5,
        KingWhite = 6,
        PawnBlack = -1,
        KnightBlack = -2,
        BishopBlack = -3,
        RookBlack = -4,
        QueenBlack = -5,
        KingBlack = -6
    }

    public class GameState
    {
        public int[] board = new int[64];

        public bool whiteToMove = true;

        public bool whiteCanCastleKingside = true;
        public bool whiteCanCastleQueenside = true;
        public bool blackCanCastleKingside = true;
        public bool blackCanCastleQueenside = true;

        public int? enPassantSquare = null;

        public void InitializeStartingPosition()
        {
            board = new int[] {
                (int)Piece.RookBlack, (int)Piece.KnightBlack, (int)Piece.BishopBlack, (int)Piece.QueenBlack,
                (int)Piece.KingBlack, (int)Piece.BishopBlack, (int)Piece.KnightBlack, (int)Piece.RookBlack,
                (int)Piece.PawnBlack, (int)Piece.PawnBlack, (int)Piece.PawnBlack, (int)Piece.PawnBlack,
                (int)Piece.PawnBlack, (int)Piece.PawnBlack, (int)Piece.PawnBlack, (int)Piece.PawnBlack,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                (int)Piece.PawnWhite, (int)Piece.PawnWhite, (int)Piece.PawnWhite, (int)Piece.PawnWhite,
                (int)Piece.PawnWhite, (int)Piece.PawnWhite, (int)Piece.PawnWhite, (int)Piece.PawnWhite,
                (int)Piece.RookWhite, (int)Piece.KnightWhite, (int)Piece.BishopWhite, (int)Piece.QueenWhite,
                (int)Piece.KingWhite, (int)Piece.BishopWhite, (int)Piece.KnightWhite, (int)Piece.RookWhite
            };
        }

        public void ApplyMove(Move move)
        {
            // Handle en passant capture
            int capturedPiece = 0;
            int capturedSquare = move.To;

            if (move.IsEnPassant)
            {
                // Calculate position of the captured pawn
                capturedSquare = move.To + (whiteToMove ? 8 : -8);
                capturedPiece = board[capturedSquare];
                board[capturedSquare] = (int)Piece.Empty;
            }
            else
            {
                capturedPiece = board[move.To];
            }

            int movingPiece = board[move.From];
            int absMovingPiece = Math.Abs(movingPiece);

            // Update castling rights for moving piece
            if (absMovingPiece == (int)Piece.KingWhite)
            {
                if (whiteToMove)
                {
                    whiteCanCastleKingside = false;
                    whiteCanCastleQueenside = false;
                }
                else
                {
                    blackCanCastleKingside = false;
                    blackCanCastleQueenside = false;
                }
            }
            else if (absMovingPiece == (int)Piece.RookWhite)
            {
                if (whiteToMove)
                {
                    if (move.From == 56) whiteCanCastleQueenside = false;
                    else if (move.From == 63) whiteCanCastleKingside = false;
                }
                else
                {
                    if (move.From == 0) blackCanCastleQueenside = false;
                    else if (move.From == 7) blackCanCastleKingside = false;
                }
            }

            // Update castling rights for captured piece
            if (capturedPiece != 0)
            {
                int absCaptured = Math.Abs(capturedPiece);
                if (absCaptured == (int)Piece.RookWhite)
                {
                    if (whiteToMove)
                    {
                        if (capturedSquare == 0) blackCanCastleQueenside = false;
                        else if (capturedSquare == 7) blackCanCastleKingside = false;
                    }
                    else
                    {
                        if (capturedSquare == 56) whiteCanCastleQueenside = false;
                        else if (capturedSquare == 63) whiteCanCastleKingside = false;
                    }
                }
            }

            // Move the piece
            board[move.To] = board[move.From];
            board[move.From] = (int)Piece.Empty;

            // Handle promotion
            if (move.Promotion.HasValue)
            {
                board[move.To] = whiteToMove ? move.Promotion.Value : -move.Promotion.Value;
            }

            // Handle castling
            if (move.IsCastling)
            {
                int rookFrom, rookTo;
                if (move.To == move.From + 2) // Kingside
                {
                    rookFrom = move.To + 1;
                    rookTo = move.To - 1;
                }
                else // Queenside
                {
                    rookFrom = move.To - 2;
                    rookTo = move.To + 1;
                }

                board[rookTo] = board[rookFrom];
                board[rookFrom] = (int)Piece.Empty;
            }

            // Set en passant square for next move
            enPassantSquare = null;
            if (absMovingPiece == (int)Piece.PawnWhite)
            {
                int rankFrom = move.From / 8;
                int rankTo = move.To / 8;

                if (Math.Abs(rankFrom - rankTo) == 2)
                {
                    enPassantSquare = (move.From + move.To) / 2;
                }
            }

            // Toggle turn
            whiteToMove = !whiteToMove;
        }
        
        public GameState DeepCopy()
        {
            return new GameState
            {
                board = (int[])this.board.Clone(),
                whiteToMove = this.whiteToMove,
                whiteCanCastleKingside = this.whiteCanCastleKingside,
                whiteCanCastleQueenside = this.whiteCanCastleQueenside,
                blackCanCastleKingside = this.blackCanCastleKingside,
                blackCanCastleQueenside = this.blackCanCastleQueenside,
                enPassantSquare = this.enPassantSquare
            };
        }

        public bool IsGameOver()
        {
            if (HasInsufficientMaterial())
                return true;

            List<Move> legalMoves = MoveGenerator.GenerateMoves(this);

            if (legalMoves.Count == 0)
            {
                return true;
            }

            return false;
        }

        private int FindKingSquare(bool findWhiteKing)
        {
            int kingPiece = findWhiteKing ? (int)Piece.KingWhite : (int)Piece.KingBlack;
            for (int i = 0; i < 64; i++)
            {
                if (board[i] == kingPiece)
                    return i;
            }
            return -1; 
        }

        private bool HasInsufficientMaterial()
        {
            int whitePieces = 0;
            int blackPieces = 0;
            bool whiteHasPawnOrMajor = false;
            bool blackHasPawnOrMajor = false;
            int whiteBishops = 0;
            int blackBishops = 0;
            int whiteKnights = 0;
            int blackKnights = 0;

            for (int i = 0; i < 64; i++)
            {
                int piece = board[i];
                if (piece == (int)Piece.Empty) continue;

                int absPiece = Math.Abs(piece);

                // Count pieces
                if (piece > 0)
                {
                    whitePieces++;
                    if (absPiece == (int)Piece.PawnWhite ||
                        absPiece == (int)Piece.RookWhite ||
                        absPiece == (int)Piece.QueenWhite)
                    {
                        whiteHasPawnOrMajor = true;
                    }
                    if (absPiece == (int)Piece.BishopWhite) whiteBishops++;
                    if (absPiece == (int)Piece.KnightWhite) whiteKnights++;
                }
                else
                {
                    blackPieces++;
                    if (absPiece == (int)Piece.PawnBlack ||
                        absPiece == (int)Piece.RookBlack ||
                        absPiece == (int)Piece.QueenBlack)
                    {
                        blackHasPawnOrMajor = true;
                    }
                    if (absPiece == (int)Piece.BishopBlack) blackBishops++;
                    if (absPiece == (int)Piece.KnightBlack) blackKnights++;
                }
            }

            if (whiteHasPawnOrMajor || blackHasPawnOrMajor)
                return false;

            // Only kings left
            if (whitePieces == 1 && blackPieces == 1)
                return true;

            // King + bishop vs king
            if (whitePieces == 2 && whiteBishops == 1 && blackPieces == 1)
                return true;
            if (blackPieces == 2 && blackBishops == 1 && whitePieces == 1)
                return true;

            // King + knight vs king
            if (whitePieces == 2 && whiteKnights == 1 && blackPieces == 1)
                return true;
            if (blackPieces == 2 && blackKnights == 1 && whitePieces == 1)
                return true;

            // King + bishop vs king + bishop with same color bishops
            if (whitePieces == 2 && blackPieces == 2 &&
                whiteBishops == 1 && blackBishops == 1)
            {
                int whiteBishopSquare = -1;
                int blackBishopSquare = -1;

                for (int i = 0; i < 64; i++)
                {
                    if (board[i] == (int)Piece.BishopWhite) whiteBishopSquare = i;
                    else if (board[i] == (int)Piece.BishopBlack) blackBishopSquare = i;
                }

                if (whiteBishopSquare != -1 && blackBishopSquare != -1)
                {
                    // Check if bishops are on same color squares
                    bool isWhiteBishopLight = IsLightSquare(whiteBishopSquare);
                    bool isBlackBishopLight = IsLightSquare(blackBishopSquare);

                    if (isWhiteBishopLight == isBlackBishopLight)
                        return true;
                }
            }

            return false;
        }

        private bool IsLightSquare(int square)
        {
            int row = square / 8;
            int col = square % 8;
            return (row + col) % 2 == 1;
        }
    }
}
