using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Sprites")]
    [SerializeField] GameObject squareSprite;
    [SerializeField] GameObject pieceIndicator;
    [SerializeField] GameObject boardIndicator;
    [SerializeField] GameObject boardSprite;

    [Header("Scene Objects")]
    public GameObject menu;
    public GameObject gameOverScreen;
    [SerializeField] GameObject p1, p2;
    [SerializeField] RectTransform p1Button, p2Button;

    [Header("Board Settings")]
    [SerializeField] float gap;
    [SerializeField] float tileScale;

    bool[,] connections = new bool[81, 4]; // Square Config, 0=not connected, 1=connected
    // 0:left, 1:right, 2:down, 3:up
    int[] occupiedByBoard = new int[64]; // Board config, 0=no board, 1=vertical board, 2=horizonral board
    [HideInInspector] public bool p1Active, p2Active, bvActive, bhActive; //Indicates the object that was last clicked
    Vector2Int p1Pos, p2Pos; //Player piece board positions
    [HideInInspector] public int hPlayers; //Number of human players
    [HideInInspector] public bool p1Turn; 
    [HideInInspector] public int p1Boards, p2Boards; //How many boards left
    int currentDelay = 0; //Counter for time delays
    [SerializeField] int maxDelay; //Delay between player moves 

    [Header("Computer Player Choices")]
    [SerializeField] GameObject CPU1;
    [SerializeField] GameObject CPU2;

    List<KeyValuePair<int, int>> defaultBoardOrder;

    void Awake() //Ensures only one GameManager
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }
    void Start()
    {
        squareSprite.transform.localScale = new Vector3(tileScale, tileScale, 1); //Rescales tiles
        boardIndicator.transform.localScale = new Vector3(gap / 2.0f, (gap - tileScale), 1); //Rescales indicators
        boardSprite.transform.localScale = new Vector3(tileScale + gap, (gap - tileScale), 1); //Rescales boards

        //Sets player positions
        p1Pos = new Vector2Int(4, 0); 
        p2Pos = new Vector2Int(4, 8);
        p1.transform.position = new Vector3(CoordsToPos(p1Pos).x, CoordsToPos(p1Pos).y, p1.transform.position.z);
        p2.transform.position = new Vector3(CoordsToPos(p2Pos).x, CoordsToPos(p2Pos).y, p2.transform.position.z);
        p1Button.localPosition = p1.transform.position * 144;
        p2Button.localPosition = p2.transform.position * 144;

        //Intializes game state
        p1Boards = 10;
        p2Boards = 10;
        gameOverScreen.SetActive(false);
        menu.SetActive(true);
        SetDefaultBoardOrder();
        for (int i = 0; i < 64; i++) occupiedByBoard[i] = 0;
        //Creates board
        for (int i = 0; i < 9; i++)
        {
            for (int j = 0; j < 9; j++)
            {
                Instantiate(squareSprite, new Vector3(gap * (j - 4), gap * (i - 4), squareSprite.transform.position.z), Quaternion.identity, gameObject.transform);
                connections[i*9 + j, 0] = (j != 0);
                connections[i*9 + j, 1] = (j != 8);
                connections[i*9 + j, 2] = (i != 0);
                connections[i*9 + j, 3] = (i != 8);
            }
        }
    }

    void Update()
    {
        if (menu.activeInHierarchy) return;
        if (gameOverScreen.activeInHierarchy) return;
        
        //Deletes old indicators
        foreach (Transform t in gameObject.transform) if (t.gameObject.CompareTag("Indicator")) Destroy(t.gameObject);
        if (p1Turn && hPlayers != 0) // Player 1 Human Turn
        {
            if (p1Active)
            {
                List<Vector2Int> mvs = FindPieceMoves(p1Pos, p2Pos, connections);
                foreach (Vector2Int v in mvs) MakePieceIndicator(v);
            }
            if ((bvActive || bhActive) && p1Boards > 0)
            {
                List<Vector3Int> possibleBoards = UnblockedBoardPositions(connections, occupiedByBoard, p1Pos, p2Pos, defaultBoardOrder);
                int vorh = (bvActive? 1 : 2);
                foreach (Vector3Int v in possibleBoards) if (v.x == vorh) MakeBoardIndicator(new(v.y,v.z), bvActive);
            }
        } 
        else if (!p1Turn && hPlayers == 2)  // Player 2 Human Turn
        {
            if (p2Active)
            {
                List<Vector2Int> mvs = FindPieceMoves(p2Pos, p1Pos, connections);
                foreach (Vector2Int v in mvs) MakePieceIndicator(v);
            }
            else if ((bvActive || bhActive) && p2Boards > 0)
            {
                List<Vector3Int> possibleBoards = UnblockedBoardPositions(connections, occupiedByBoard, p1Pos, p2Pos, defaultBoardOrder);
                int vorh = (bvActive ? 1 : 2);
                foreach (Vector3Int v in possibleBoards) if (v.x == vorh) MakeBoardIndicator(new(v.y,v.z), bvActive);
            }
        }
        else if (!p1Turn && hPlayers != 2) // Player 2 CPU Turn
        {
            if (currentDelay >= maxDelay)
            {
                double tPassed = -Time.realtimeSinceStartupAsDouble;
                Vector3Int v = CPU2.GetComponent<CalcCPU2>().GetCPUMove(connections, occupiedByBoard, p2Pos, p1Pos, p2Boards, p1Boards, false);
                Debug.Log(tPassed + Time.realtimeSinceStartupAsDouble);
                if (v[0] == 0) MovePiece(new Vector2Int(v[1], v[2]));
                else
                {
                    if (v[0] == 1) bvActive = true;
                    else bhActive = true;
                    CreateBoard(new Vector2Int(v[1], v[2]));
                }
                currentDelay = 0;
            }
            else currentDelay++;
        } 
        else if (p1Turn && hPlayers == 0) // Player 1 CPU Turn
        {
            if (currentDelay >= maxDelay)
            {
                Vector3Int v = CPU1.GetComponent<CalcCPU1>().GetCPUMove(connections, occupiedByBoard, p1Pos, p2Pos, p1Boards, p2Boards, true);
                if (v[0] == 0) MovePiece(new Vector2Int(v[1], v[2]));
                else
                {
                    if (v[0] == 1) bvActive = true;
                    else bhActive = true;
                    CreateBoard(new Vector2Int(v[1], v[2]));
                }
                currentDelay = 0;
            }
            else currentDelay++;
        } 
    }

    //Converts Board Coordinates to screen coordinates
    public Vector2 CoordsToPos(Vector2Int coords)
    {
        return new Vector2((coords.x - 4) * gap, (coords.y - 4) * gap);
    }

    //Creates a piece movement indicator at a given board position
    public void MakePieceIndicator(Vector2Int v)
    {
        Vector2 w = CoordsToPos(v);
        GameObject o = Instantiate(pieceIndicator, new Vector3(w.x, w.y, pieceIndicator.transform.position.z), Quaternion.identity, gameObject.transform);
        o.GetComponent<MoveIndication>().pos = v;
    }

    //Creates a board indicator at a given board position and orientation
    public void MakeBoardIndicator(Vector2Int v, bool vert)
    {
        Vector2 w = CoordsToPos(v) + new Vector2(0.5f * gap, 0.5f * gap);
        GameObject o;
        if (vert) o = Instantiate(boardIndicator, new Vector3(w.x, w.y, boardIndicator.transform.position.z), Quaternion.Euler(0, 0, 90), gameObject.transform);
        else o = Instantiate(boardIndicator, new Vector3(w.x, w.y, boardIndicator.transform.position.z), Quaternion.identity, gameObject.transform);
        o.GetComponent<BoardIndication>().pos = v;
    }

    // Moves the piece to a given board position
    public void MovePiece(Vector2Int v)
    {
        if (p1Turn)
        {
            p1Pos = v;
            p1.transform.position = new Vector3(CoordsToPos(p1Pos).x, CoordsToPos(p1Pos).y, p1.transform.position.z);
            p1Button.localPosition = p1.transform.position * 144;
            if (v.y == 8) GameOver(true);
        }
        else
        {
            p2Pos = v;
            p2.transform.position = new Vector3(CoordsToPos(p2Pos).x, CoordsToPos(p2Pos).y, p2.transform.position.z);
            p2Button.localPosition = p2.transform.position * 144;
            if (v.y == 0) GameOver(false);
        }
        p1Turn = !p1Turn;
    }

    // Accordingly updates the game state when a new board is played
    public void CreateBoard(Vector2Int v)
    {
        if (p1Turn) p1Boards--;
        else p2Boards--;
        p1Turn = !p1Turn;
        if (bvActive) Instantiate(boardSprite, CoordsToPos(v) + new Vector2(0.5f * gap, 0.5f * gap), Quaternion.Euler(0, 0, 90), gameObject.transform);
        else if (bhActive) Instantiate(boardSprite, CoordsToPos(v) + new Vector2(0.5f * gap, 0.5f * gap), Quaternion.identity, gameObject.transform);
        // Update blockages
        connections = UpdatePaths(bvActive, connections, v);
        occupiedByBoard[v.y * 8 + v.x] = (bvActive? 1 : 2); 
        bvActive = false;
        bhActive = false;
    }

    // Finds all legal piece moves for a given game state
    public List<Vector2Int> FindPieceMoves(Vector2Int pos1, Vector2Int pos2, bool[,] connects)
    {
        List<Vector2Int> r = new();
        for (int i = 0; i < 2; i++) // left-right calculations
        {
            int x = (i == 0 ? -1 : 1);
            if (!connects[pos1.y * 9 + pos1.x, i]) continue;
            if (pos2.x != pos1.x + x || pos2.y != pos1.y) r.Add(new Vector2Int(pos1.x + x, pos1.y));
            else
            {
                if (connects[pos2.y * 9 + pos2.x, i]) r.Add(new Vector2Int(pos1.x + 2 * x, pos1.y));
                else
                {
                    if (connects[pos2.y * 9 + pos2.x, 2]) r.Add(new Vector2Int(pos1.x + x, pos1.y - 1));
                    if (connects[pos2.y * 9 + pos2.x, 3]) r.Add(new Vector2Int(pos1.x + x, pos1.y + 1));
                }
            }
        }
        for (int i = 2; i < 4; i++) // up-down calculations calculations
        {
            int x = (i == 2 ? -1 : 1);
            if (!connects[pos1.y * 9 + pos1.x, i]) continue;
            if (pos2.x != pos1.x || pos2.y != pos1.y + x) r.Add(new Vector2Int(pos1.x, pos1.y + x));
            else
            {
                if (connects[pos2.y * 9 + pos2.x, i]) r.Add(new Vector2Int(pos1.x, pos1.y + 2 * x));
                else
                {
                    if (connects[pos2.y * 9 + pos2.x, 0]) r.Add(new Vector2Int(pos1.x - 1, pos1.y + x));
                    if (connects[pos2.y * 9 + pos2.x, 1]) r.Add(new Vector2Int(pos1.x + 1, pos1.y + x));
                }
            }
        }
        return r;
    }

    //Returns whether or not player a board at a given position and orientation is illegal given the game state
    bool BlocksPath(Vector2Int v, bool vert, bool[,] arr, Vector2Int pos, bool p1)
    {
        bool[,] b = UpdatePaths(vert, arr, v);
        // Hypothetically update connections
        Queue<int> q = new();
        q.Enqueue(pos.x + pos.y * 9);
        bool[] used = new bool[81];
        used[q.Peek()] = true;
        // Perform a breadth-first search to see if the way gets blocked
        bool works = false;
        while (q.Count != 0)
        {
            int x = q.Dequeue();
            for (int j = 0; j < 4; j++)
            {
                int u;
                if (!b[x,j]) continue;
                if (j == 0) u = x - 1;
                else if (j == 1) u = x + 1;
                else if (j == 2) u = x - 9;
                else u = x + 9;
                if (used[u]) continue;
                q.Enqueue(u);
                used[u] = true;
                if (p1 && u >= 9 * 8) works = true;
                else if (!p1 && u < 9) works = true;
            }
            if (works) break;
        }
        return !works;
    }

    //Returns a list of all unobstructed board placement positions
    public List<Vector3Int> UnblockedBoardPositions(bool[,] connects, int[] occupied, Vector2Int pos1, Vector2Int pos2, List<KeyValuePair<int, int>> q)
    {
        List<Vector3Int> v = new();
        int i, j; // i is x-coordindate, j is y-coordinate
        bool[,,] keyVerts = new bool[2,8,8], keyHorz = new bool[2,8,8];
        GetKeyBoards(connects, ref keyVerts, ref keyHorz, pos1, true);
        GetKeyBoards(connects, ref keyVerts, ref keyHorz, pos2, false);
        foreach (var k in q){
            i = k.Key;
            j = k.Value;
            if (occupied[i + j * 8] != 0) continue;
            Vector2Int vec = new(i, j);
            bool blacklistV = false, blacklistH = false;
            if ((j == 0 || occupied[i + (j - 1) * 8] != 1) && (j == 7 || occupied[i + (j + 1) * 8] != 1))
            {
                if (keyVerts[0, i, j] && connects[i + j * 9, 1] && connects[i + j * 9 + 9, 1]) blacklistV = BlocksPath(vec, true, connects, pos1, true);
                if (keyVerts[1, i, j] && connects[i + j * 9, 1] && connects[i + j * 9 + 9, 1]) blacklistV = BlocksPath(vec, true, connects, pos2, false);
            }
            else blacklistV = true;
            if ((i == 0 || occupied[i - 1 + j * 8] != 2) && (i == 7 || occupied[i + 1 + j * 8] != 2))
            {
                if (keyHorz[0, i, j] && connects[i + j * 9, 3] && connects[i + j * 9 + 1, 3]) blacklistH = BlocksPath(vec, false, connects, pos1, true);
                if (keyHorz[1, i, j] && connects[i + j * 9, 3] && connects[i + j * 9 + 1, 3]) blacklistH = BlocksPath(vec, false, connects, pos2, false);
            }
            else blacklistH = true;
            if (!blacklistV) v.Add(new(1, i, j));
            if (!blacklistH) v.Add(new(2, i, j));
        }
        return v;
    }

    void GetKeyBoards(bool[,] board, ref bool[,,] arrV, ref bool[,,] arrH, Vector2Int pos, bool p1)
    {
        Queue<int> q = new();
        q.Enqueue(pos.x + pos.y * 9);
        List<Vector3Int>[] seqs = new List<Vector3Int>[81];
        bool[] used = new bool[81];
        for (int i = 0; i < 81; i++) seqs[i] = new();
        for (int i = 0; i < 81; i++) used[i] = false;
        used[q.Peek()] = true;
        List<Vector3Int> bestList = new();
        while (q.Count != 0)
        {
            int x = q.Peek();
            q.Dequeue();
            if ((p1 && x >= 8 * 9) || (!p1 && x < 9))
            {
                bestList = seqs[x];
                break;
            }
            for (int i = 0; i < 4; i++)
            {
                if (!board[x, i]) continue;
                int u;
                if (i == 0) u = x - 1; //left
                else if (i == 1) u = x + 1; //right
                else if (i == 2) u = x - 9; //down
                else u = x + 9; //up
                if (used[u]) continue;
                seqs[u].AddRange(seqs[x]);
                int xcoord = x % 9, ycoord = x / 9;
                if (i == 0 && ycoord != 0) seqs[u].Add(new Vector3Int(1, xcoord - 1, ycoord - 1));
                if (i == 0 && ycoord != 8) seqs[u].Add(new Vector3Int(1, xcoord - 1, ycoord));
                if (i == 1 && ycoord != 0) seqs[u].Add(new Vector3Int(1, xcoord, ycoord - 1));
                if (i == 1 && ycoord != 8) seqs[u].Add(new Vector3Int(1, xcoord, ycoord));
                if (i == 2 && xcoord != 0) seqs[u].Add(new Vector3Int(2, xcoord - 1, ycoord - 1));
                if (i == 2 && xcoord != 8) seqs[u].Add(new Vector3Int(2, xcoord, ycoord - 1));
                if (i == 3 && xcoord != 0) seqs[u].Add(new Vector3Int(2, xcoord - 1, ycoord));
                if (i == 3 && xcoord != 8) seqs[u].Add(new Vector3Int(2, xcoord, ycoord));
                used[u] = true;
                q.Enqueue(u);
            }
        }
        foreach (var vec in bestList)
        {
            int n = (p1 ? 0 : 1);
            if (vec.x == 1) arrV[n, vec.y, vec.z] = true;
            else arrH[n, vec.y, vec.z] = true;
        }
    }

    // Returns the default board position order
    void SetDefaultBoardOrder()
    {
        defaultBoardOrder = new();
        for (int i = 0; i < 8; i++){
            for (int j = 0; j < 8; j++){
                defaultBoardOrder.Add(new KeyValuePair<int, int>(i, j));
            }
        }
    }

    //Game Over logic
    void GameOver(bool p1Wins)
    {
        gameOverScreen.SetActive(true);
        foreach (Transform t in gameOverScreen.transform)
        {
            if (t.name == "P1 Win Text") t.gameObject.SetActive(p1Wins);
            else if (t.name == "P2 Win Text") t.gameObject.SetActive(!p1Wins);
        }
    }

    //Updates a connections array for a board placement
    public bool[,] UpdatePaths(bool vert, bool[,] b, Vector2Int v)
    {
        bool[,] connects = new bool[81,4];
        Array.Copy(b, connects, 81 * 4);
        if (vert){
            connects[v.y * 9 + v.x, 1] = false;
            connects[v.y * 9 + v.x + 1, 0] = false;
            connects[v.y * 9 + 9 + v.x, 1] = false;
            connects[v.y * 9 + 10 + v.x, 0] = false;
        }
        else{
            connects[v.y * 9 + v.x, 3] = false;
            connects[v.y * 9 + v.x + 1, 3] = false;
            connects[v.y * 9 + 9 + v.x, 2] = false;
            connects[v.y * 9 + 10 + v.x, 2] = false;
        }
        return connects;
    }
}

class Comp : IComparer<KeyValuePair<int, Vector3Int>>
{
    public int Compare(KeyValuePair<int, Vector3Int> x, KeyValuePair<int, Vector3Int> y)
    {
        if (x.Key < y.Key) return 1;
        else if (x.Key > y.Key) return -1;
        return 0;
    }
}