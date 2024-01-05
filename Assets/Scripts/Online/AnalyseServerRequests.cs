using UnityEngine;

public class AnalyseServerRequests : MonoBehaviour
{
    Client client;
    ChessGameManager gameManager;

    // Start is called before the first frame update
    void Start()
    {
        client = GetComponent<Client>();
        gameManager = ChessGameManager.Instance;
    }

    public void AnalyseRequests(string message)
    {
        message = message.Split('\0', 2)[0];

        if (message.Contains("White"))
        {
            client.color = ChessGameManager.EChessTeam.White;
        }
        else if (message.Contains("Black"))
        {
            client.color = ChessGameManager.EChessTeam.Black;
        }

        if (message.Contains("BoardState@"))
        {
            string[] boardStateInfos = message.Split('@');
            gameManager.GetBoardState().SetupBoard(
                boardStateInfos[4],
                boardStateInfos[1][0] == 'W' ?
                    ChessGameManager.EChessTeam.White :
                boardStateInfos[1][0] == 'B' ?
                    ChessGameManager.EChessTeam.Black : ChessGameManager.EChessTeam.None,
                uint.Parse(boardStateInfos[2]),
                uint.Parse(boardStateInfos[3]));
            gameManager.UpdatePieces();
            return;
        }

        switch (message)
        {
            case "Win":
                gameManager.Win();
                break;

            case "Loose":
                gameManager.Lost();
                break;

            default:
                if (!message.Contains(','))
                    return;

                string[] strMoves = message.Split(',', 2);
                ChessGameManager.Move move;
                move.from = int.Parse(strMoves[0]);
                move.to = int.Parse(strMoves[1]);
                gameManager.PlayTurn(move);
                break;
        }
    }
}
