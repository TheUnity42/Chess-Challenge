using System;
using ChessChallenge.API;

public class MyBot : IChessBot
{

    private Board board;
    private bool isWhite;

    private static int aggression = 2;

    private static int defense = 3;

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.isWhite = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();

        Array.Sort(moves, sortMoves);

        return moves[0];
    }

    int checkIfLethalSquare(Square s) {
        // get all the piece lists
        PieceList[] lists = board.GetAllPieceLists();
        // we will evaluate 0-5 if we're black or 6-11 if we're white
        bool hit = false;
        for(int i = isWhite ? 6 : 0; i < (isWhite ? 12 : 6) && !hit; i++) {
            // if the list is empty, skip it
            if(lists[i].Count == 0) continue;
            // iterate
            for (int j = 0; j < lists[i].Count && !hit; j++){
                Piece p = lists[i].GetPiece(j);
                ulong attack_bitboard;
                // check if its a sliding peice
                if (p.IsBishop || p.IsRook || p.IsQueen) {
                    attack_bitboard = BitboardHelper.GetSliderAttacks(p.PieceType, p.Square, board);
                } else if (p.IsKnight) { // check if its a knight
                    attack_bitboard = BitboardHelper.GetKnightAttacks(p.Square);
                } else if (p.IsPawn) { // check pawns
                    attack_bitboard = BitboardHelper.GetPawnAttacks(p.Square, p.IsWhite);
                } else { // check king
                    attack_bitboard = BitboardHelper.GetKingAttacks(p.Square);
                }
                hit = BitboardHelper.SquareIsSet(attack_bitboard, s);
            }
        }
        return hit ? 1 : 0;
    }

    int weight(Move m) {
        return (aggression - defense + 1) * (m.MovePieceType == PieceType.King ? 0 : 1) * (isWhite ? m.TargetSquare.Rank : 8 - m.TargetSquare.Rank)  // try to move forward
                + aggression * (int) m.CapturePieceType // try to capture
                + 4 * (m.IsPromotion ? 1 : 0) // try to promote
                - (5 - aggression) * checkIfLethalSquare(m.TargetSquare) // try to avoid lethal squares
                + 2 * defense * checkIfLethalSquare(m.StartSquare); // try to move out of lethal squares
    }

    int sortMoves(Move lhs, Move rhs) {
        return weight(rhs) - weight(lhs);
    }
}