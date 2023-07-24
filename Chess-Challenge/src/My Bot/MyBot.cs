using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    private const int MaxDepth = 3;
    private Dictionary<string, (int, Move)> transpositionTable = new Dictionary<string, (int, Move)>();

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = new Move();
        for (int depth = 1; depth <= MaxDepth; depth++)
        {
            var (_, move) = Minimax(board, depth, int.MinValue, int.MaxValue, board.IsWhiteToMove);
            bestMove = move;
        }
        return bestMove;
    }

    private (int, Move) Minimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        string boardHash = board.GetFenString();
        if (transpositionTable.ContainsKey(boardHash))
        {
            return transpositionTable[boardHash];
        }

        if (depth == 0)
        {
            return (QuiescenceSearch(board, alpha, beta, maximizingPlayer), new Move());
        }

        Move bestMove = new Move();
        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (var move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                var (eval, _) = Minimax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            transpositionTable[boardHash] = (maxEval, bestMove);
            return (maxEval, bestMove);
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                var (eval, _) = Minimax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                if (eval < minEval)
                {
                    minEval = eval;
                    bestMove = move;
                }
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            transpositionTable[boardHash] = (minEval, bestMove);
            return (minEval, bestMove);
        }
    }

    private int QuiescenceSearch(Board board, int alpha, int beta, bool maximizingPlayer)
    {
        int standPat = EvaluateBoard(board);
        if (standPat >= beta && maximizingPlayer || standPat <= alpha && !maximizingPlayer)
            return standPat;

        var dangerousMoves = board.GetLegalMoves().Where(move =>
        {
            board.MakeMove(move);
            bool inCheck = board.IsInCheck();
            board.UndoMove(move);
            return move.IsCapture ||  // Capture
                   inCheck ||  // Check
                   (board.GetPiece(move.StartSquare).PieceType == PieceType.Pawn &&
                    (move.TargetSquare.Rank == 0 || move.TargetSquare.Rank == 7));  // Pawn promotion
        });

        foreach (var move in dangerousMoves)
        {
            board.MakeMove(move);
            int score = -QuiescenceSearch(board, -beta, -alpha, !maximizingPlayer);
            board.UndoMove(move);

            if (score >= beta && maximizingPlayer || score <= alpha && !maximizingPlayer)
                return score;
            if (score > alpha && maximizingPlayer || score < beta && !maximizingPlayer)
                alpha = beta = score;
        }

        return maximizingPlayer ? alpha : beta;
    }

    private int EvaluateBoard(Board board)
    {
        int score = 0;

        for (int rank = 0; rank < 8; rank++)
        {
            for (int file = 0; file < 8; file++)
            {
                var square = new Square(file, rank);
                var piece = board.GetPiece(square);

                int colorModifier = piece.IsWhite ? 1 : -1;
                score += colorModifier * GetPieceValue(piece);
                score += colorModifier * GetPieceSquareTableValue(piece, square);

                if (piece.PieceType == PieceType.Pawn && IsPassedPawn(board, square, piece.IsWhite))
                    score += colorModifier * 20;  // Passed pawns are a major advantage
                if (piece.PieceType == PieceType.King && IsKingExposed(board, square, piece.IsWhite))
                    score -= colorModifier * 50;  // Exposed kings are a major disadvantage
                
            }
        }

        // score += board.IsWhiteToMove ? board.GetLegalMoves().Count : -board.GetLegalMoves().Count;
        return score;
    }

    private int GetPieceValue(Piece piece)
    {
        return piece.PieceType switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 9,
            _ => 0,
        };
    }

    private int GetPieceSquareTableValue(Piece piece, Square square)
    {
        int[,] pieceSquareTable;

        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                pieceSquareTable = new int[,]
                {
                    {0, 0, 0, 0, 0, 0, 0, 0},
                    {5, 10, 10, -20, -20, 10, 10, 5},
                    {5, -5, -10, 0, 0, -10, -5, 5},
                    {0, 0, 0, 20, 20, 0, 0, 0},
                    {5, 5, 10, 25, 25, 10, 5, 5},
                    {10, 10, 20, 30, 30, 20, 10, 10},
                    {50, 50, 50, 50, 50, 50, 50, 50},
                    {0, 0, 0, 0, 0, 0, 0, 0}
                };
                break;
            // Add similar tables for other piece types
            default:
                pieceSquareTable = new int[8, 8];
                break;
        }

        return pieceSquareTable[square.File, square.Rank];
    }

    private bool IsPassedPawn(Board board, Square square, bool isWhite)
    {
        int direction = isWhite ? 1 : -1;

        // Check for enemy pawns in front
        for (int i = -1; i <= 1; i++)
        {
            for (int j = 1; j < 8; j++)
            {
                int newFile = square.File + i;
                int newRank = square.Rank + j * direction;
                if (newFile >= 0 && newFile < 8 && newRank >= 0 && newRank < 8)
                {
                    var piece = board.GetPiece(new Square(newFile, newRank));
                    if (piece.PieceType == PieceType.Pawn && piece.IsWhite != isWhite)
                    {
                        return false;
                    }
                }
            }
        }

        // Check for doubled pawns
        for (int j = 1; j < 8; j++)
        {
            int newRank = square.Rank + j * direction;
            if (newRank >= 0 && newRank < 8)
            {
                var piece = board.GetPiece(new Square(square.File, newRank));
                if (piece.PieceType == PieceType.Pawn && piece.IsWhite == isWhite)
                {
                    return false;
                }
            }
        }

        // Check for pawn chains
        for (int i = -1; i <= 1; i += 2)
        {
            int newFile = square.File + i;
            if (newFile >= 0 && newFile < 8)
            {
                var piece = board.GetPiece(new Square(newFile, square.Rank));
                if (piece.PieceType == PieceType.Pawn && piece.IsWhite == isWhite)
                {
                    return true;
                }
            }
        }

        return true;
    }

    private bool IsKingExposed(Board board, Square kingSquare, bool isWhite)
    {
        // Check for pawn shelter
        for (int i = -1; i <= 1; i++)
        {
            for (int j = 1; j <= 2; j++)
            {
                int newFile = kingSquare.File + i;
                int newRank = isWhite ? kingSquare.Rank - j : kingSquare.Rank + j;
                if (newFile >= 0 && newFile < 8 && newRank >= 0 && newRank < 8)
                {
                    var piece = board.GetPiece(new Square(newFile, newRank));
                    if (piece.PieceType != PieceType.Pawn || piece.IsWhite != isWhite)
                    {
                        return true;
                    }
                }
            }
        }

        // Check for open lines
        foreach (var direction in new[] { new { File = 0, Rank = 1 }, new { File = 1, Rank = 1 }, new { File = 1, Rank = 0 }, new { File = 1, Rank = -1 }, new { File = 0, Rank = -1 }, new { File = -1, Rank = -1 }, new { File = -1, Rank = 0 }, new { File = -1, Rank = 1 } })
        {
            for (int i = 1; i < 8; i++)
            {
                int newFile = kingSquare.File + i * direction.File;
                int newRank = kingSquare.Rank + i * direction.Rank;
                if (newFile >= 0 && newFile < 8 && newRank >= 0 && newRank < 8)
                {
                    var piece = board.GetPiece(new Square(newFile, newRank));
                    if (piece.IsWhite != isWhite && (piece.PieceType == PieceType.Queen || piece.PieceType == PieceType.Rook && direction.File * direction.Rank == 0 || piece.PieceType == PieceType.Bishop && direction.File * direction.Rank != 0))
                    {
                        return true;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        return false;
    }
}
