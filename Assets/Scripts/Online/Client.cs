using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Client : MonoBehaviour
{
    [SerializeField] GameObject menu;
    [SerializeField] GameObject inputFieldObject;
    TMP_InputField inputField;
    [SerializeField] TMP_Text errorText;
    [SerializeField] GameObject goBackButton;

    [HideInInspector] public ChessGameManager.EChessTeam color = ChessGameManager.EChessTeam.None;

    [HideInInspector] public TcpClient client = new();

    Task listenToBroadcast;
    CancellationTokenSource cancellationToken;

    AnalyseServerRequests requestsAnalyseScript;

    List<string> waitingMessages = new();

    void Awake()
    {
        color = ChessGameManager.EChessTeam.None;
    }

    void Start()
    {
        inputField = inputFieldObject.GetComponentInChildren<TMP_InputField>();   
        inputField.characterLimit = 15;
        Time.timeScale = 0;
        requestsAnalyseScript = GetComponent<AnalyseServerRequests>();
    }

    void Update()
    {
        if(waitingMessages.Count > 0)
        {
            string msg = waitingMessages.First();
            waitingMessages.RemoveAt(0);
            requestsAnalyseScript.AnalyseRequests(msg);
        }
    }

    public void OnStartClient()
    {
        Time.timeScale = 1;
        menu.SetActive(false);
        inputFieldObject.SetActive(true);
        goBackButton.SetActive(true);
    }

    public void OnBack()
    {
        menu.SetActive(true);
        inputFieldObject.SetActive(false);
        goBackButton.SetActive(false);
    }

    public void StartClient()
    {
        cancellationToken = new CancellationTokenSource();
        listenToBroadcast = new Task(ListenToBroadcast, cancellationToken.Token);

        errorText.text = "Connecting to server...";

        try
        {
            client.Connect(new IPEndPoint(IPAddress.Parse(inputField.text), 11000));
        }
        catch (SocketException)
        {
            errorText.text = "Error: Can't connect to server";
        }
        catch (FormatException)
        {
            errorText.text = "Error: Please write a valid IP";
        }
        catch (Exception e)
        {
            errorText.text = "Error: " + e;
        }
        
        if(client.Connected) 
        {
            menu.transform.parent.gameObject.SetActive(false);
            listenToBroadcast.Start();
        }
    }
     
    public void SendNetworkMessage(string message)
    {
        try
        {
            client.GetStream().Write(Encoding.UTF8.GetBytes(message));
        }
        catch (Exception e)
        {
            ShowMessage(e.ToString());
        }
    }
    void ListenToBroadcast()
    {
        while (true)
        {
            if (!client.Connected)
                return;

            var buffer = new byte[1024];
            try
            {
                client.GetStream().Read(buffer);
            }
            catch (Exception e)
            {
                ShowMessage(e.ToString());
            }
            waitingMessages.Add(Encoding.UTF8.GetString(buffer));
            
            if (buffer[0] == 0 || waitingMessages[0] == "" || waitingMessages[0][0] == '\0')
            {
                client.GetStream().Close();
            }
        }
    }

    static void ShowMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, [CallerFilePath] string path = null)
    {
        print(path.Split('\\').Last() + " [" + lineNumber + "]: " + message);
    }
}

