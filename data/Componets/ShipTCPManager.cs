using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using Unigine;

public class ShipTCPManager : Component
{
    public int port = 12345;
    private TcpListener tcpServer;
    private Thread listenThread;
    private static List<ShipRemoteControl> activeShips = new List<ShipRemoteControl>();
    public static void RegisterShip(ShipRemoteControl ship)
    {
        if (!activeShips.Contains(ship)) activeShips.Add(ship);
    }

    protected override void OnReady()
    {
        listenThread = new Thread(new ThreadStart(ListenForClients));
        listenThread.IsBackground = true;
        listenThread.Start();
        Log.Message($"TCP Server Aktif di Port {port} (Menunggu Delphi...)\n");
    }

    private void ListenForClients()
    {
        try
        {
            // Mendengarkan koneksi dari IP mana pun pada port 12345
            tcpServer = new TcpListener(IPAddress.Any, port);
            tcpServer.Start();

            while (true)
            {
                // Menunggu aplikasi Delphi melakukan "Connect"
                TcpClient client = tcpServer.AcceptTcpClient();

                // Buat thread baru untuk setiap koneksi klien agar tidak mengunci game loop utama
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
            }
        }
        catch (Exception) { /* Server Berhenti */ }
    }

    private void HandleClientComm(object clientObj)
    {
        TcpClient tcpClient = (TcpClient)clientObj;

        // Mendapatkan info IP pengirim (sangat berguna untuk monitoring debug log)
        string clientIP = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();

        using (StreamReader reader = new StreamReader(tcpClient.GetStream()))
        {
            try
            {
                while (tcpClient.Connected)
                {
                    string rawData = reader.ReadLine();
                    if (rawData == null) break;
                    Log.Message($"[TCP Masuk dari {clientIP}]: {rawData}\n");

                    string data = rawData.Trim().ToUpper();

                    if (!string.IsNullOrEmpty(data))
                    {
                        foreach (var ship in activeShips)
                        {
                            ship.ProcessNetworkData(data);
                        }
                        List<Node> allNodes = new List<Node>();
                        World.GetNodes(allNodes);

                        foreach (Node n in allNodes)
                        {
                            if (n != null && n.Enabled)
                            {
                                var gun76Comp = n.GetComponent<Gun76mmController>();
                                if (gun76Comp != null)
                                {
                                    gun76Comp.ProcessNetworkData(rawData);
                                }

                                var clws35Comp = n.GetComponent<Clws35mmController>();
                                if (clws35Comp != null)
                                {
                                    clws35Comp.ProcessNetworkData(rawData);
                                }

                                var exocetComp = n.GetComponent<MissileExocetMM40>();
                                if (exocetComp != null)
                                {
                                    exocetComp.ProcessNetworkData(rawData);
                                }

                                var vlmicaComp = n.GetComponent<MissileVLMICA>();
                                if (vlmicaComp != null)
                                {
                                    vlmicaComp.ProcessNetworkData(rawData);
                                }

                                var torpedoA244sComp = n.GetComponent<TorpedoA244s>();
                                if (torpedoA244sComp != null)
                                {
                                    torpedoA244sComp.ProcessNetworkData(rawData);
                                }

                                var gun127Comp = n.GetComponent<Gun127mmController>();
                                if (gun127Comp != null)
                                {
                                    gun127Comp.ProcessNetworkData(rawData);
                                }

                                var heliComp = n.GetComponent<helicoptercontroller>();
                                if (heliComp != null)
                                {
                                    heliComp.ProcessNetworkData(rawData);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Message($"Koneksi dengan {clientIP} terputus: {e.Message}\n");
            }
        }
        tcpClient.Close();
    }

    protected override void OnDisable()
    {
        if (tcpServer != null)
        {
            tcpServer.Stop();
            tcpServer = null;
        }
    }
}