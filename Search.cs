using System;
using System.Collections.Generic;

namespace ChessEngine
{
    internal class Search
    {

        private int maxDepth;

        public Search(int maxDepth = 3)
        {
            this.maxDepth = maxDepth;
        }

   
        public Move GetBestMove(GameState state)
        {
            List<Move> moves = MoveGenerator.GenerateMoves(state);

            Move bestMove = default;
            int bestScore = state.whiteToMove ? int.MinValue : int.MaxValue;

            foreach (var move in moves)
            {
                GameState newState = state.DeepCopy(); 
                newState.ApplyMove(move);             

                int score = Minimax(newState, maxDepth - 1, !state.whiteToMove);

                if (state.whiteToMove)
                {
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }
                else
                {
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestMove = move;
                    }
                }
            }

            return bestMove;
        }

        private int Minimax(GameState state, int depth, bool maximizingPlayer)
        {
            if (depth == 0 || state.IsGameOver()) 
            {
                return Evaluator.EvaluateBoard(state);
            }

            List<Move> moves = MoveGenerator.GenerateMoves(state);

            if (maximizingPlayer)
            {
                int maxEval = int.MinValue;
                foreach (var move in moves)
                {
                    GameState newState = state.DeepCopy();
                    newState.ApplyMove(move);
                    int eval = Minimax(newState, depth - 1, false);
                    if (eval > maxEval) maxEval = eval;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                foreach (var move in moves)
                {
                    GameState newState = state.DeepCopy();
                    newState.ApplyMove(move);
                    int eval = Minimax(newState, depth - 1, true);
                    if (eval < minEval) minEval = eval;
                }
                return minEval;
            }
        }
    }
}
