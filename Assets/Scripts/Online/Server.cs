using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class Server : MonoBehaviour
{

    [SerializeField] GameObject menu;

    public List<TcpClient> clients = new();
    List<Task> messageTransfertTasks = new();
    TcpListener listener;

    TMP_InputField inputField;
    Client client;
    Task clientAccept;

    [SerializeField] TMP_Text serverAddress;

    bool serverStarted = false;

    private void Start()
    {
        inputField = GameObject.Find("/Canvas/StartUI/EnterIP/ServerIP").GetComponent<TMP_InputField>();
        client = GameObject.Find("/ClientManager").GetComponent<Client>();
        Time.timeScale = 0; 
    }

    void OnDestroy()
    {
        if (!serverStarted)
            return;

        foreach (TcpClient client in clients)
        {
            if (!client.Connected)
                continue;

            try
            {
                client.GetStream().Close();
                client.Close();
            }
            catch (Exception e)
            {
                ShowMessage(e.ToString());
            }
        }
        try
        {
            if(listener != null)
            {
                listener.Stop();
            }
        }
        catch (Exception e)
        {
            ShowMessage(e.ToString());
        }
    }

    public void OnStartServer()
    {
        serverStarted = true;
        Time.timeScale = 1;
        menu.transform.parent.gameObject.SetActive(false);
        StartServer();
    }

    public void StartServer()
    {
        clientAccept = new Task(AcceptClients);
        try
        {
            IPAddress[] ipv4Addresses = Array.FindAll(
            Dns.GetHostEntry(string.Empty).AddressList,
            a => a.AddressFamily == AddressFamily.InterNetwork);
            IPAddress ipadress = ipv4Addresses.Last();
            serverAddress.text = "Your IP is " + ipadress.ToString();
            inputField.text = ipadress.ToString();
            listener = new TcpListener(ipadress, 11000);
            listener.Start();
        }
        catch (Exception e)
        {
            ShowMessage(e.ToString());
        }

        clientAccept.Start();
        client.StartClient();
    }

    void AcceptClients()
    {
        while (true)
        {
            try
            {
                TcpClient newClient = listener.AcceptTcpClient();
                clients.Add(newClient);
                messageTransfertTasks.Add(new Task(() => TransfertMessages(clients.Last())));
                messageTransfertTasks.Last().Start();

                if(clients.Count == 1)
                {
                    clients[0].GetStream().Write(Encoding.ASCII.GetBytes("White"));
                    continue;
                }
                else if(clients.Count == 2)
                {
                    clients[1].GetStream().Write(Encoding.ASCII.GetBytes("Black"));
                }

                clients.Last().GetStream().Write(Encoding.ASCII.GetBytes(ChessGameManager.Instance.GetBoardState().BoardToString()));
            }
            catch (SocketException)
            {
                return;
            }
            catch (Exception e)
            {
                ShowMessage(e.ToString());
            }
        }
    }

    public void BroadcastMessage(string message, TcpClient clientToExclude = null)
    {
        for (int i = 0; i < clients.Count; ++i)
        {
            if (!clients[i].Connected)
                continue;

            try
            {
                if (clientToExclude == null)
                {
                    clients[i].GetStream().Write(Encoding.ASCII.GetBytes(message));
                    continue;
                }

                if (clients[i] != clientToExclude)
                    clients[i].GetStream().Write(Encoding.ASCII.GetBytes(message));
            }
            catch (Exception e)
            {
                ShowMessage(e.ToString());
            }
        }
    }

    public void TransfertMessages(TcpClient client)
    {
        while (true)
        {
            if (!client.Connected)
                return;

            byte[] buffer = new byte[1024];
            try
            {
                client.GetStream().Read(buffer);
                if (buffer[0] == 0)
                {
                    if (!client.Connected)
                        continue;

                    client.GetStream().Close();
                    client.Close();
                    return;
                }
            }
            catch (Exception e)
            {
                ShowMessage(e.ToString());
            }
            BroadcastMessage(Encoding.UTF8.GetString(buffer).Split('\0', 2)[0], client);
        }
    }

    static void ShowMessage(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, [CallerFilePath] string path = null)
    {
        print(path.Split('\\').Last() + " [" + lineNumber + "]: " + message);
    }

    public void EndGameMessage(ChessGameManager.EChessTeam team)
    {
        if(clients.Count> 1)
        {
            Debug.Log(team);
            if (team == ChessGameManager.EChessTeam.Black)
            {
                BroadcastMessage("Loose", clients[0]);
                BroadcastMessage("Win", clients[1]);
            }
            else if (team == ChessGameManager.EChessTeam.White)
            {
                BroadcastMessage("Loose", clients[1]);
                BroadcastMessage("Win", clients[0]);
            }
        }
    }

}

