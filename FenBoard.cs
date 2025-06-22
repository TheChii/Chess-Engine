using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine 
{
    public class FenBoard
    {
        string fenString;
        public FenBoard(string fen)
        {
            fenString = fen;
        }

        public GameState GameStateFromFen(string fen) // https://en.wikipedia.org/wiki/Forsyth%E2%80%93Edwards_Notation
        {
            GameState gameState = new GameState();

            string[] parts = fen.Split(' ');

            if (parts.Length < 6)
            {
                throw new ArgumentException("Invalid FEN string: " + fen);
            }
            string position = parts[0];
            string activeColor = parts[1].ToLowerInvariant();
            string castlingAvailability = parts[2].ToLowerInvariant();
            string enPassantTarget = parts[3].ToLowerInvariant();
            string halfMoveClock = parts[4].ToLowerInvariant();
            string fullMoveNumber = parts[5].ToLowerInvariant();

            gameState.whiteToMove = activeColor == "w";

            /* 
             Castling availability: If neither side has the ability to castle,
             this field uses the character "-". Otherwise, this field contains 
             one or more letters: "K" if White can castle kingside, "Q" if White 
            can castle queenside, "k" if Black can castle kingside, and "q" if
            Black can castle queenside. A situation that temporarily prevents 
            castling does not prevent the use of this notation.
             */

            for (int i=0;i<castlingAvailability.Length; i++)
            {
                switch (castlingAvailability[i])
                {
                    case 'K':
                        gameState.whiteCanCastleKingside = true;
                        break;
                    case 'Q':
                        gameState.whiteCanCastleQueenside = true;
                        break;
                    case 'k':
                        gameState.blackCanCastleKingside = true;
                        break;
                    case 'q':
                        gameState.blackCanCastleQueenside = true;
                        break;
                    case '-':
                        // No castling rights
                        gameState.whiteCanCastleKingside = false;
                        gameState.whiteCanCastleQueenside = false;
                        gameState.blackCanCastleKingside = false;
                        gameState.blackCanCastleQueenside = false;
                        break;
                }
            }

            /*
             En passant target square: This is a square over which 
            a pawn has just passed while moving two squares; it is
            given in algebraic notation. If there is no en passant
            target square, this field uses the character "-". This 
            is recorded regardless of whether there is a pawn in
            position to capture en passant.[6] An updated version
            of the spec has since made it so the target square is 
            recorded only if a legal en passant capture is possible,
            but the old version of the standard is the one most commonly used.
             */

            gameState.enPassantSquare = -1; // Default to no en passant square
            if (enPassantTarget != "-")
            {
                if (enPassantTarget.Length == 2)
                {
                    char file = enPassantTarget[0];
                    char rank = enPassantTarget[1];
                    int fileIndex = file - 'a'; // 'a' is 0, 'b' is 1, ..., 'h' is 7
                    int rankIndex = 8 - (rank - '0'); // '1' is 7, '2' is 6, ..., '8' is 0
                    gameState.enPassantSquare = rankIndex * 8 + fileIndex;
                }
            }

            int placementIndex = 0;
            for(int i=0;i <position.Length; i++)
            {
                if (char.IsDigit(position[i]))
                {
                    for(int j = 0; j < (int)char.GetNumericValue(position[i]); j++)
                    {
                        gameState.board[placementIndex] = (int)Piece.Empty;
                        placementIndex++;
                    }
                }
                else
                {
                    Piece piece = Piece.Empty;
                    switch (position[i].ToString().ToLowerInvariant())
                    {
                        case "p":
                            piece = Piece.PawnBlack;
                            break;
                        case "r":
                            piece = Piece.RookBlack;
                            break;
                        case "n":
                            piece = Piece.KnightBlack;
                            break;
                        case "b":
                            piece = Piece.BishopBlack;
                            break;
                        case "q":
                            piece = Piece.QueenBlack;
                            break;
                        case "k":
                            piece = Piece.KingBlack;
                            break;
                        case "P":
                            piece = Piece.PawnWhite;
                            break;
                        case "R":
                            piece = Piece.RookWhite;
                            break;
                        case "N":
                            piece = Piece.KnightWhite;
                            break;
                        case "B":
                            piece = Piece.BishopWhite;
                            break;
                        case "Q":
                            piece = Piece.QueenWhite;
                            break;
                        case "K":
                            piece = Piece.KingWhite;
                            break;
                        case "/":
                            continue;
                            
                    }
                    gameState.board[placementIndex] = (int)piece;
                    placementIndex++;
                }
            }


            return gameState;
        }
    }
}
