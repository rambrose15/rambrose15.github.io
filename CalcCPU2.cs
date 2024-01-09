using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CalcCPU2 : MonoBehaviour
{
    List<Vector3Int> rankedMoves;
    List<KeyValuePair<int, Vector3Int>> newRankedMoves;
    readonly int inf = 2147483647;
    readonly int neginf = -2147483647;
    [SerializeField] int targetDepth;
    Comp listComp = new();

    public Vector3Int GetCPUMove(bool[,] board, int[] occupied, Vector2Int myPos, Vector2Int oPos, int myBoardNum, int oBoardNum, bool p1)
    {
        rankedMoves = AllMoves(board, occupied, myPos, oPos, myBoardNum, p1);
        newRankedMoves = new();
        for (int i = 1; i <= targetDepth; i++)
        {
            EvaluateMoves(board, occupied, myPos, oPos, myBoardNum, oBoardNum, p1, 0, i, neginf, inf);
            newRankedMoves.Sort(listComp);
            rankedMoves.Clear();
            foreach (var x in newRankedMoves) rankedMoves.Add(x.Value);
            newRankedMoves.Clear();
        }
        return rankedMoves[0];
    }

    int EvaluateMoves(bool[,] board, int[] occupied, Vector2Int myPos, Vector2Int oPos, int myBoardNum, int oBoardNum, bool p1, int currentDepth, int maxDepth, int alpha, int beta)
    {
        if ((p1 && myPos.y == 8) || (!p1 && myPos.y == 0)) return inf - currentDepth; // If we win
        if ((p1 && oPos.y == 0) || (!p1 && oPos.y == 8)) return neginf + currentDepth; // If we lose
        if (currentDepth == maxDepth) return EvaluatePosition(board, myPos, oPos, myBoardNum, oBoardNum, p1);

        // Piece Moves
        List<Vector3Int> moves;
        if (currentDepth == 0) moves = rankedMoves;
        else moves = AllMoves(board, occupied, myPos, oPos, myBoardNum, p1);
        foreach (Vector3Int vec in moves)
        {
            Vector2Int v = new(vec.y, vec.z);
            int eval;
            //Move is a piece move
            if (vec.x == 0) eval = -EvaluateMoves(board, occupied, oPos, v, oBoardNum, myBoardNum, !p1, currentDepth + 1, maxDepth, -beta, -alpha);
            else // Move is a board placement
            {
                occupied[v.x + v.y * 8] = vec.x;
                bool[,] newboard = GameManager.instance.UpdatePaths(vec.x == 1, board, v); //Updates board
                eval = -EvaluateMoves(newboard, occupied, oPos, myPos, oBoardNum, myBoardNum - 1, !p1, currentDepth + 1, maxDepth, -beta, -alpha);
                occupied[v.x + v.y * 8] = 0;
            }

            if (eval >= beta) return eval + 1; // Our opponent can do better, so they aren't choosing this move
            if (currentDepth == 0) newRankedMoves.Add(new KeyValuePair<int, Vector3Int>(eval, vec));
            alpha = Math.Max(alpha, eval); // We can now guarentee a better score
        }
        return alpha;
    }

    public int EvaluatePosition(bool[,] board, Vector2Int myPos, Vector2Int oPos, int myBoardNum, int oBoardNum, bool p1)
    {
        return 5 * (DistToGoal(oPos, board, !p1) - DistToGoal(myPos, board, p1)) + (myBoardNum - oBoardNum);
    }

    int DistToGoal(Vector2Int pos, bool[,] board, bool p1)
    {
        Queue<KeyValuePair<int, int>> q = new();
        q.Enqueue(new KeyValuePair<int, int>(pos.x + pos.y * 9, 0));
        int[] dist = new int[81];
        for (int i = 0; i < 81; i++) dist[i] = -1;
        dist[q.Peek().Key] = 0;
        while (q.Count != 0)
        {
            int x = q.Peek().Key, d = q.Peek().Value;
            q.Dequeue();
            if (p1 && x >= 8 * 9) return d;
            if (!p1 && x < 9) return d;
            for (int i = 0; i < 4; i++)
            {
                if (!board[x, i]) continue;
                int u;
                if (i == 0) u = x - 1;
                else if (i == 1) u = x + 1;
                else if (i == 2) u = x - 9;
                else u = x + 9;
                if (dist[u] != -1) continue;
                dist[u] = d + 1;
                q.Enqueue(new KeyValuePair<int, int>(u, d + 1));
            }
        }
        Debug.LogWarning("DistToGoal function failed to return correct value");
        return 0;
    }

    // Returns all legal moves in a position, in the form (x, i, j)
    //x=0 for piece move, x=1 for vertical board, x=2 for horizontal board
    // Then i,j are just the board coordinates of the placement
    List<Vector3Int> AllMoves(bool[,] board, int[] occupied, Vector2Int myPos, Vector2Int oPos, int b1, bool p1) //pos1: myPos, pos2: oPos
    {
        Vector2Int p1Pos = (p1 ? myPos : oPos), p2Pos = (p1 ? oPos : myPos);
        List<Vector3Int> r = new();

        //Get all piece moves and add to r
        List<Vector2Int> pieceMoves = GameManager.instance.FindPieceMoves(myPos, oPos, board); // myPos must be first arguement
        foreach (var v in pieceMoves) r.Add(new Vector3Int(0, v.x, v.y)); // Add all piece moves

        //Get all board moves and add to r
        if (b1 == 0) return r; //If no boards left
        // Add all board moves
        r.AddRange(GameManager.instance.UnblockedBoardPositions(board, occupied, p1Pos, p2Pos, CPUBoardOrder(oPos)));
        return r;
    }

    // Returns the board positions in the order they should be looked at
    List<KeyValuePair<int, int>> CPUBoardOrder(Vector2Int oPos)
    {
        List<KeyValuePair<int, int>> q = new();
        int a = oPos.x, b = oPos.y;
        bool aorb = true;
        int num = 1, count = 0, dir = 1;
        while (q.Count < 64)
        {
            if (a >= 0 && a < 8 && b >= 0 && b < 8) q.Add(new KeyValuePair<int, int>(a, b));
            if (aorb) a += dir;
            else b += dir;
            count++;
            if (count == num)
            {
                count = 0;
                aorb = !aorb;
                if (aorb)
                {
                    num++;
                    dir *= -1;
                }
            }
        }
        return q;
    }
}
