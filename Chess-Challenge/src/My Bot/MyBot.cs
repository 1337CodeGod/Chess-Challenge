using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ChessChallenge.API;

// An extremely strong bot that can perform evaluation and pick the best move
public class MyBot : IChessBot
{
  
        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        public Move Think(Board board, Timer timer)
        {
            Move[] allMoves = board.GetLegalMoves();

            // Pick a random move to play if nothing better is found
            Random rng = new();
            Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
            int highestStrengthPosition = -1000000;

            foreach (Move move in allMoves)
            {
                // Always play checkmate in one
                if (MoveIsCheckmate(board, move))
                {
                    moveToPlay = move;
                    break;
                }

                // evaluate using Evaluate method after assuimg the move
                board.MakeMove(move);
                int strengthOfPosition = Evaluate(board);
                if (strengthOfPosition > highestStrengthPosition)
                {
                    highestStrengthPosition = strengthOfPosition;
                    moveToPlay = move;
                }
                board.UndoMove(move);
            }

            // debug log the chosen move out of all the possible moves
            Console.WriteLine("Chosen move: " + moveToPlay.ToString());
            // write the strength of the move
            Console.WriteLine("Strength of move: " + highestStrengthPosition.ToString());

            return moveToPlay;
        }

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        // Evaluate the current position
    int Evaluate(Board board)
    {
        int score = 0;

        // use GetAllPieceLists from Board and then determine Material balance
        
        PieceList[] pieceLists = board.GetAllPieceLists(); // there's a list for each type and color, so 12 at the start
        // Material balance
        foreach (PieceList pieceList in pieceLists)
        {
            if (pieceList.IsWhitePieceList == board.IsWhiteToMove)
            {
                foreach (Piece piece in pieceList)
                {
                    score += pieceValues[(int)piece.PieceType];

                    switch (piece.PieceType) // Positional heuristics
                    {
                    case PieceType.Knight:
                        score += EvaluateKnight(board, piece);
                        break;
                    case PieceType.Bishop:
                        score += EvaluateBishop(board, piece);
                        break;
                    case PieceType.Rook:
                        score += EvaluateRook(board, piece);
                        break;
                    case PieceType.Queen:
                        score += EvaluateQueen(board, piece);
                        break;
                    case PieceType.King:
                        score += EvaluateKing(board, piece);
                        break;
                    }
                }
            }
            else
            {
                foreach (Piece piece in pieceList)
                {
                    score -= pieceValues[(int)piece.PieceType];

                    switch (piece.PieceType) // Positional heuristics
                    {
                    case PieceType.Knight:
                        score -= EvaluateKnight(board, piece);
                        break;
                    case PieceType.Bishop:
                        score -= EvaluateBishop(board, piece);
                        break;
                    case PieceType.Rook:
                        score -= EvaluateRook(board, piece);
                        break;
                    case PieceType.Queen:
                        score -= EvaluateQueen(board, piece);
                        break;
                    case PieceType.King:
                        score -= EvaluateKing(board, piece);
                        break;
                    }
                }
            }
        }

        
        return score;
    }

    // Evaluate the position of a knight
    int EvaluateKnight(Board board, Piece knight)
    {
        int score = 0;

        // Centralization bonus
        // knight.Square.File and knight.Square.Rank are both in the range [0, 7]
        // so the maximum distance from the center is 7 + 7 = 14
        int distanceFromCenter = Math.Abs(knight.Square.File - 3) + Math.Abs(knight.Square.Rank - 3);
        int centralization = 14 - distanceFromCenter;
        score += centralization;

        return score;
    }

    // Evaluate the position of a bishop
    int EvaluateBishop(Board board, Piece bishop)
    {
        int score = 0;

        // Centralization bonus
        int distanceFromCenter = Math.Abs(bishop.Square.File - 3) + Math.Abs(bishop.Square.Rank - 3);
        int centralization = 14 - distanceFromCenter;
        score += centralization;

        // TODO: Bishop pair bonus

        return score;
    }

    // Evaluate the position of a rook
    int EvaluateRook(Board board, Piece rook)
    {
        int score = 0;

        // Centralization bonus
        int distanceFromCenter = Math.Abs(rook.Square.File - 3) + Math.Abs(rook.Square.Rank - 3);
        int centralization = 14 - distanceFromCenter;
        score += centralization;
        return score;
    }

    // Evaluate the position of a queen
    int EvaluateQueen(Board board, Piece queen)
    {
        int score = 0;

        // Centralization bonus
        int distanceFromCenter = Math.Abs(queen.Square.File - 3) + Math.Abs(queen.Square.Rank - 3);
        int centralization = 14 - distanceFromCenter;
        score += centralization;

        return score;
    }

    // Evaluate the position of a king
    int EvaluateKing(Board board, Piece king)
    {
        int score = 0;

        // Safety bonus
        if (board.IsInCheck())
        {
            score -= 50;
        }

        return score;
    }
}