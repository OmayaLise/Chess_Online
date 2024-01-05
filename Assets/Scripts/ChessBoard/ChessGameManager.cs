using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/*
 * This singleton manages the whole chess game
 *  - board data (see BoardState class)
 *  - piece models instantiation
 *  - player interactions (piece grab, drag and release)
 *  - AI update calls (see UpdateAITurn and ChessAI class)
 */

public partial class ChessGameManager : MonoBehaviour
{

    #region Singleton
    static ChessGameManager instance = null;
    public static ChessGameManager Instance {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<ChessGameManager>();
            return instance;
        }
    }
    #endregion

    private ChessAI chessAI = null;
    private Transform boardTransform = null;
    private static int BOARD_SIZE = 8;
    private int pieceLayerMask;
    private int boardLayerMask;

    [SerializeField] TMP_Text winGameText;
    [SerializeField] TMP_Text lossGameText;

    [SerializeField] Button nextGameButton;

    [SerializeField] GameObject server;

    #region Enums
    public enum EPieceType : uint
    {
        Pawn = 0,
        King,
        Queen,
        Rook,
        Knight,
        Bishop,
        NbPieces,
        None
    }

    public enum EChessTeam
    {
        White = 0,
        Black,
        None
    }

    public enum ETeamFlag : uint
    {
        None = 1 << 0,
        Friend = 1 << 1,
        Enemy = 1 << 2
    }
    #endregion

    #region Structs & Classes
    public struct BoardSquare
    {
        public EPieceType piece;
        public EChessTeam team;

        public BoardSquare(EPieceType p, EChessTeam t)
        {
            piece = p;
            team = t;
        }

        static public BoardSquare Empty()
        {
            BoardSquare res;
            res.piece = EPieceType.None;
            res.team = EChessTeam.None;
            return res;
        }
    }

    public struct Move
    {
        public int from;
        public int to;

        public override bool Equals(object o)
        {
            try
            {
                return (bool)(this == (Move)o);
            }
            catch
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return from + to;
        }

        public static bool operator ==(Move move1, Move move2)
        {
            return move1.from == move2.from && move1.to == move2.to;
        }

        public static bool operator !=(Move move1, Move move2)
        {
            return move1.from != move2.from || move1.to != move2.to;
        }
    }

    #endregion

    #region Chess Game Methods

    BoardState boardState = null;
    public BoardState GetBoardState() { return boardState; }

    EChessTeam teamTurn;

    List<uint> scores;

    public delegate void PlayerTurnEvent(bool isWhiteMove);
    public event PlayerTurnEvent OnPlayerTurn = null;

    public delegate void ScoreUpdateEvent(uint whiteScore, uint blackScore);
    public event ScoreUpdateEvent OnScoreUpdated = null;

    public void PrepareGame(bool resetScore = true)
    {
        chessAI = ChessAI.Instance;

        // Start game
        boardState.Reset();

        teamTurn = EChessTeam.White;
        if (scores == null)
        {
            scores = new List<uint>
            {
                0,
                0
            };
        }
        if (resetScore)
        {
            scores.Clear();
            scores.Add(0);
            scores.Add(0);
        }
    }

    public void PlayTurn(Move move)
    {
        if (boardState.IsValidMove(teamTurn, move))
        {
            BoardState.EMoveResult result = boardState.PlayUnsafeMove(move);
            if (result == BoardState.EMoveResult.Promotion)
            {
                // instantiate promoted queen gameobject
                AddQueenAtPos(move.to);
            }

            EChessTeam otherTeam = (teamTurn == EChessTeam.White) ? EChessTeam.Black : EChessTeam.White;
            if (boardState.DoesTeamLose(otherTeam))
            {
                // increase score and reset board
                scores[(int)teamTurn]++;
                if (OnScoreUpdated != null)
                    OnScoreUpdated(scores[0], scores[1]);

                PrepareGame(false);
                // remove extra piece instances if pawn promotions occured
                teamPiecesArray[0].ClearPromotedPieces();
                teamPiecesArray[1].ClearPromotedPieces();


                server.GetComponent<Server>().EndGameMessage(otherTeam);

                //if (teamTurn == EChessTeam.White)
                //    server.GetComponent<Server>().BroadcastMessage("Loose", server.GetComponent<Server>().clients[0]);
                //else
                //    server.GetComponent<Server>().BroadcastMessage("Loose", server.GetComponent<Server>().clients[1]);
            }
            else
            {
                teamTurn = otherTeam;
            }
            // raise event
            if (OnPlayerTurn != null)
                OnPlayerTurn(teamTurn == EChessTeam.White);
        }

        UpdatePieces();
    }

    public void OnResetClicked()
    {
        nextGameButton.gameObject.SetActive(false);
        winGameText.gameObject.SetActive(false);
        lossGameText.gameObject.SetActive(false);

        PrepareGame(false);
        // remove extra piece instances if pawn promotions occured
        teamPiecesArray[0].ClearPromotedPieces();
        teamPiecesArray[1].ClearPromotedPieces();

    }

    public void Lost()
    {
        winGameText.gameObject.SetActive(false);
        lossGameText.gameObject.SetActive(true);
        lossGameText.text = "YOU LOST";

        StartTimer();
    }

    public void Win()
    {
        winGameText.gameObject.SetActive(true);
        lossGameText.gameObject.SetActive(false);
        winGameText.text = "YOU WIN";

        StartTimer();
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(5);
        winGameText.gameObject.SetActive(false);
        lossGameText.gameObject.SetActive(false);
    }

    // Start the timer.
    private void StartTimer()
    {
        StartCoroutine(Timer());
    }

    // used to instantiate newly promoted queen
    private void AddQueenAtPos(int pos)
    {
        teamPiecesArray[(int)teamTurn].AddPiece(EPieceType.Queen);
        GameObject[] crtTeamPrefabs = (teamTurn == EChessTeam.White) ? whitePiecesPrefab : blackPiecesPrefab;
        GameObject crtPiece = Instantiate(crtTeamPrefabs[(uint)EPieceType.Queen]);
        teamPiecesArray[(int)teamTurn].StorePiece(crtPiece, EPieceType.Queen);
        crtPiece.transform.position = GetWorldPos(pos);
    }

    public bool IsPlayerTurn()
    {
        return teamTurn == client.color;
    }

    public EChessTeam GetTeamTurn()
    {
        return teamTurn;
    }

    public void SetTeamTurn(EChessTeam team)
    {
        teamTurn = team;
    }

    public BoardSquare GetSquare(int pos)
    {
        return boardState.squares[pos];
    }

    public uint GetScore(EChessTeam team)
    {
        return scores[(int)team];
    }

    public void SetScore(uint wScore, uint bScore)
    {
        scores[(int)EChessTeam.White] = wScore;
        scores[(int)EChessTeam.Black] = bScore;
    }

    private void UpdateBoardPiece(Transform pieceTransform, int destPos)
    {
        pieceTransform.position = GetWorldPos(destPos);
    }

    private Vector3 GetWorldPos(int pos)
    {
        Vector3 piecePos = boardTransform.position;
        piecePos.y += zOffset;
        piecePos.x = -widthOffset + pos % BOARD_SIZE;
        piecePos.z = -widthOffset + pos / BOARD_SIZE;

        return piecePos;
    }

    private int GetBoardPos(Vector3 worldPos)
    {
        int xPos = Mathf.FloorToInt(worldPos.x + widthOffset) % BOARD_SIZE;
        int zPos = Mathf.FloorToInt(worldPos.z + widthOffset);

        return xPos + zPos * BOARD_SIZE;
    }

    #endregion

    #region MonoBehaviour

    private TeamPieces[] teamPiecesArray = new TeamPieces[2];
    private float zOffset = 0.5f;
    private float widthOffset = 3.5f;

    Client client;

    void Start()
    {
        client = GameObject.Find("/ClientManager").GetComponent<Client>();

        pieceLayerMask = 1 << LayerMask.NameToLayer("Piece");
        boardLayerMask = 1 << LayerMask.NameToLayer("Board");

        boardTransform = GameObject.FindGameObjectWithTag("Board").transform;

        LoadPiecesPrefab();

        boardState = new BoardState();

        PrepareGame();

        teamPiecesArray[0] = null;
        teamPiecesArray[1] = null;

        CreatePieces();

        if (OnPlayerTurn != null)
            OnPlayerTurn(teamTurn == EChessTeam.White);
        if (OnScoreUpdated != null)
            OnScoreUpdated(scores[0], scores[1]);
    }

    void Update()
    {
        if (!client.client.Connected || client.color == EChessTeam.None)
        {
            if(winGameText != null && winGameText.gameObject.activeSelf ||
                (lossGameText != null && lossGameText.gameObject.activeSelf))
            {
                winGameText.gameObject.SetActive(false);
                lossGameText.gameObject.SetActive(false);
            }
            return;
        }

        if (IsPlayerTurn())
            UpdatePlayerTurn();
    }
    #endregion

    #region Pieces

    GameObject[] whitePiecesPrefab = new GameObject[6];
    GameObject[] blackPiecesPrefab = new GameObject[6];

    void LoadPiecesPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhitePawn");
        whitePiecesPrefab[(uint)EPieceType.Pawn] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteKing");
        whitePiecesPrefab[(uint)EPieceType.King] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteQueen");
        whitePiecesPrefab[(uint)EPieceType.Queen] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteRook");
        whitePiecesPrefab[(uint)EPieceType.Rook] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteKnight");
        whitePiecesPrefab[(uint)EPieceType.Knight] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/WhiteBishop");
        whitePiecesPrefab[(uint)EPieceType.Bishop] = prefab;

        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackPawn");
        blackPiecesPrefab[(uint)EPieceType.Pawn] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackKing");
        blackPiecesPrefab[(uint)EPieceType.King] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackQueen");
        blackPiecesPrefab[(uint)EPieceType.Queen] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackRook");
        blackPiecesPrefab[(uint)EPieceType.Rook] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackKnight");
        blackPiecesPrefab[(uint)EPieceType.Knight] = prefab;
        prefab = Resources.Load<GameObject>("Prefabs/Pieces/BlackBishop");
        blackPiecesPrefab[(uint)EPieceType.Bishop] = prefab;
    }

    void CreatePieces()
    {
        // Instantiate all pieces according to board data
        if (teamPiecesArray[0] == null)
            teamPiecesArray[0] = new TeamPieces();
        if (teamPiecesArray[1] == null)
            teamPiecesArray[1] = new TeamPieces();

        GameObject[] crtTeamPrefabs = null;
        int crtPos = 0;
        foreach (BoardSquare square in boardState.squares)
        {
            crtTeamPrefabs = (square.team == EChessTeam.White) ? whitePiecesPrefab : blackPiecesPrefab;
            if (square.piece != EPieceType.None)
            {
                GameObject crtPiece = Instantiate(crtTeamPrefabs[(uint)square.piece]);
                teamPiecesArray[(int)square.team].StorePiece(crtPiece, square.piece);

                // set position
                Vector3 piecePos = boardTransform.position;
                piecePos.y += zOffset;
                piecePos.x = -widthOffset + crtPos % BOARD_SIZE;
                piecePos.z = -widthOffset + crtPos / BOARD_SIZE;
                crtPiece.transform.position = piecePos;
            }
            crtPos++;
        }
    }

    public void UpdatePieces()
    {
        teamPiecesArray[0].Hide();
        teamPiecesArray[1].Hide();

        for (int i = 0; i < boardState.squares.Count; i++)
        {
            BoardSquare square = boardState.squares[i];
            if (square.team == EChessTeam.None)
                continue;

            int teamId = (int)square.team;
            EPieceType pieceType = square.piece;

            teamPiecesArray[teamId].SetPieceAtPos(pieceType, GetWorldPos(i));
        }
    }

    #endregion

    #region Gameplay

    Transform grabbed = null;
    float maxDistance = 100f;
    int startPos = 0;
    int destPos = 0;

    void UpdatePlayerTurn()
    {
        if (Input.GetMouseButton(0))
        {
            if (grabbed)
                ComputeDrag();
            else
                ComputeGrab();
        }
        else if (grabbed != null)
        {
            // find matching square when releasing grabbed piece
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance, boardLayerMask))
            {
                grabbed.root.position = hit.transform.position + Vector3.up * zOffset;
            }

            destPos = GetBoardPos(grabbed.root.position);
            if (startPos != destPos)
            {
                Move move = new Move();
                move.from = startPos;
                move.to = destPos;
                PlayTurn(move);
                client.SendNetworkMessage(move.from.ToString() + "," + move.to.ToString());
            }
            else
            {
                grabbed.root.position = GetWorldPos(startPos);
            }
            grabbed = null;
        }
    }

    void ComputeDrag()
    {
        // drag grabbed piece on board
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxDistance, boardLayerMask))
        {
            grabbed.root.position = hit.point;
        }
    }

    void ComputeGrab()
    {
        // grab a new chess piece from board
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit, maxDistance, pieceLayerMask))
        {
            grabbed = hit.transform;
            startPos = GetBoardPos(hit.transform.position);
        }
    }

    #endregion
}
