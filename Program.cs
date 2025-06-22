using System;
using System.Collections.Generic;

namespace ChessEngine
{
    class HumanVsEngine
    {
        static GameState gameState = new GameState();
        static Search engine = new Search(3); // Depth 3 search

        static void Main(string[] args)
        {
            Console.WriteLine("Chess Engine - Human vs Computer");
            Console.WriteLine("Enter moves as 'from to' (e.g., '52 36' for e2-e4)");
            Console.WriteLine("=================================");

            gameState.InitializeStartingPosition();
            PrintBoard();

            while (!gameState.IsGameOver())
            {
                if (gameState.whiteToMove)
                {
                    HumanMove();
                }
                else
                {
                    EngineMove();
                }

                PrintBoard();
            }

            Console.WriteLine("Game over!");
            Console.ReadLine();
        }

        static void HumanMove()
        {
            while (true)
            {
                Console.Write("Your move (from to): ");
                string input = Console.ReadLine();
                string[] parts = input.Split(' ');

                if (parts.Length != 2 ||
                    !int.TryParse(parts[0], out int from) ||
                    !int.TryParse(parts[1], out int to))
                {
                    Console.WriteLine("Invalid input. Use format 'from to' (e.g., '52 36')");
                    continue;
                }

                // Validate move
                List<Move> legalMoves = MoveGenerator.GenerateMoves(gameState);
                Move? foundMove = null;

                foreach (var move in legalMoves)
                {
                    if (move.From == from && move.To == to)
                    {
                        foundMove = move;
                        break;
                    }
                }

                if (foundMove == null)
                {
                    Console.WriteLine("Illegal move. Try again.");
                    continue;
                }

                gameState.ApplyMove(foundMove.Value);
                Console.WriteLine($"You played: {SquareName(from)}-{SquareName(to)}");
                break;
            }
        }

        static void EngineMove()
        {
            Console.WriteLine("Engine thinking...");
            Move bestMove = engine.GetBestMove(gameState);
            gameState.ApplyMove(bestMove);
            Console.WriteLine($"Engine plays: {SquareName(bestMove.From)}-{SquareName(bestMove.To)}");
        }

        static void PrintBoard()
        {
            Console.WriteLine();
            Console.WriteLine("  +------------------------+");
            for (int rank = 0; rank < 8; rank++)
            {
                Console.Write($"{8 - rank} |");
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    int piece = gameState.board[square];
                    Console.Write($" {GetPieceSymbol(piece)}");
                }
                Console.WriteLine(" |");
            }
            Console.WriteLine("  +------------------------+");
            Console.WriteLine("    a b c d e f g h");
            Console.WriteLine();
            Console.WriteLine($"Turn: {(gameState.whiteToMove ? "White" : "Black")}");
        }

        static string GetPieceSymbol(int piece)
        {
            return piece switch
            {
                (int)Piece.PawnWhite => "P",
                (int)Piece.KnightWhite => "N",
                (int)Piece.BishopWhite => "B",
                (int)Piece.RookWhite => "R",
                (int)Piece.QueenWhite => "Q",
                (int)Piece.KingWhite => "K",
                (int)Piece.PawnBlack => "p",
                (int)Piece.KnightBlack => "n",
                (int)Piece.BishopBlack => "b",
                (int)Piece.RookBlack => "r",
                (int)Piece.QueenBlack => "q",
                (int)Piece.KingBlack => "k",
                _ => "."
            };
        }

        static string SquareName(int square)
        {
            char file = (char)('a' + (square % 8));
            int rank = 8 - (square / 8);
            return $"{file}{rank}";
        }
    }
}