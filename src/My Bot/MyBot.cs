using System;
using System.Collections.Generic;
using ChessChallenge.API;

public class MyBot : IChessBot
{

    private Board board;
    private bool isWhite;

    // hash set of previous moves
    private HashSet<Move> previousMoves = new HashSet<Move>();

    private static int aggression = 2;

    private static int defense = 3;

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.isWhite = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();

        Tuple<int, Move>[] moveWeights = new Tuple<int, Move>[moves.Length];

        for (int i = 0; i < moves.Length; i++) {
            moveWeights[i] = new Tuple<int, Move>(weight(moves[i]), moves[i]);
        }
        
        Array.Sort(moveWeights, sortMoves);

        // Console.WriteLine("MyBot: {0} moves found, weight of first: {1}", moves.Length, weight(moves[0]));

        // add move to previous moves
        this.previousMoves.Add(moveWeights[0].Item2);
        return moveWeights[0].Item2;
    }

    int sortMoves(Tuple<int, Move> lhs, Tuple<int, Move> rhs) {
        return rhs.Item1 - lhs.Item1;
    }

    int checkIfLethalSquare(Square s) {
        // get all the piece lists
        PieceList[] lists = board.GetAllPieceLists();
        // we will evaluate 0-5 if we're black or 6-11 if we're white
        bool hit = false;
        bool covered = false;
        for (int i = 0; i < 12; i++) {
            // if the list is empty, skip it
            if (lists[i].Count == 0) continue;
            // iterate
            for (int j = 0; j < lists[i].Count && !hit; j++) {
                Piece p = lists[i].GetPiece(j);
                if (i < 6) {
                    // if were white, check if the square is covered by our pieces
                    covered |= isWhite && BitboardHelper.SquareIsSet(attackBitboard(p, p.Square), s);
                    // if we're black, check if the square is covered by their pieces
                    hit |= !isWhite && BitboardHelper.SquareIsSet(attackBitboard(p, p.Square), s);
                } else {
                    // if were black, check if the square is covered by our pieces
                    covered |= !isWhite && BitboardHelper.SquareIsSet(attackBitboard(p, p.Square), s);
                    // if we're white, check if the square is covered by their pieces
                    hit |= isWhite && BitboardHelper.SquareIsSet(attackBitboard(p, p.Square), s);
                }
            }
        }
        return (hit ? 1 : 0) - (covered ? 2 : 0);
    }

    int weight(Move m) {
        // get the piece we're moving
        Piece p = board.GetPiece(m.StartSquare);
        // determine penalties
        // 1st penalty is if we're moving the king or queen
        // 2nd penalty is if we're repeating a move
        int penalty =   (p.IsKing ? 10 : 0)
                      + (p.IsQueen ? 5 : 0)
                      + (previousMoves.Contains(m) ? 50 : 0);
        // perform the weight calculation.
        // 1st, subtract the penalty
        // 2st, determine the attack weight of a piece as by evaluating 8 moves into the future
        // 3nd, if this move is an attack, calculate the value of the attack
        // 4rd, if the move would expose this piece to attack, subtract the value of the piece
        // 5th, if we're currently exposed to attack, add the value of the piece
        // 6th, check if the move would put the king in check (+) or out of check (-)
        return - penalty
               + recurseMoves(p, p.Square, 4)
               + (int) m.CapturePieceType
               - (checkIfLethalSquare(m.TargetSquare) * (int) p.PieceType)
               + (checkIfLethalSquare(m.StartSquare) * (int) p.PieceType)
               + 2 * checkWeight(p, m.TargetSquare);
    }

    int recurseMoves(Piece p, Square s, int level) {
        if (level == 0 || p.IsKing) return 0;
        // get the bitboard representing the moves we can make
        // then and the board with the bitboard of enemy pieces
        ulong bitboard = attackBitboard(p, s);
        ulong attackBoard = bitboard & (isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard);

        if (attackBoard != 0) return 1 + checkWeight(p, s);
        // if the bitboard is 0, there are no viable attacks. return the sum of recursing for each possible move
        for (int i = 0; i < 64; i++) {
            if ((bitboard & (1UL << i)) != 0) {
                int score = recurseMoves(p, new Square(i), level - 1);
                if (score != 0) return score;
            }
        }
        return 0;
   }

    int checkWeight(Piece p, Square s) {
        // get the bitboard of the enemy king
        ulong king = board.GetPieceBitboard(PieceType.King, !isWhite);
        // expand the king to include all adjacent squares
        king = king | (king << 1) | (king >> 1) | (king << 8) | (king >> 8);
        // bitboard of if we currently have the king in or near check
        ulong current = attackBitboard(p, p.Square) & king;
        // bitboard of if we would have the king in near check
        ulong future = attackBitboard(p, s) & king;
        // if we can check the king and arent, return 1
        // if we are checking the king but wouldnt be, return -1
        // else, return 0
        return (current == 0 && future != 0) ? 1 : (current != 0 && future == 0) ? -1 : 0;
    }

    ulong attackBitboard(Piece p, Square s) {
        // check if its a sliding peice
        if (p.IsBishop || p.IsRook || p.IsQueen) {
            return BitboardHelper.GetSliderAttacks(p.PieceType, s, board);
        } else if (p.IsKnight) { // check if its a knight
            return BitboardHelper.GetKnightAttacks(s);
        } else if (p.IsPawn) { // check pawns
            return BitboardHelper.GetPawnAttacks(s, p.IsWhite);
        } else if (p.IsKing) { // check king
            return BitboardHelper.GetKingAttacks(s);
        } else {
            return 0;
        }
    }

}