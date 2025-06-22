using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessEngine
{
    public class Evaluator
    {
        private static readonly int[] PieceValues = new int[]
        {
        0,    // Empty square
        100,  // Pawn
        300,  // Knight
        300,  // Bishop
        500,  // Rook
        900,  // Queen
        20000 // King (not usually scored)
        };

        public static int EvaluateBoard(GameState state)
        {
            int score = 0;
            for (int i = 0; i < 64; i++)
            {
                int piece = state.board[i];
                if (piece == 0) continue;

                int absPiece = Math.Abs(piece);
                int pieceValue = PieceValues[absPiece];

                if (piece > 0)
                {
                    score += pieceValue;
                }
                else
                {
                    score -= pieceValue;
                }
            }
            return score;
        }
    }

}
