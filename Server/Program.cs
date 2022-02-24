using Common.Messages;
using Common.Transport;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Server
{
    enum MenuPage
    {
        Main,
        Init,
        GameSettings,
        ServerSettings,
        Info
    }

    /*
     * Server logic and settings,
     * self contained server class without game logic.
     */
    public class Program
    {
        public const int SIO_UDP_CONNRESET = -1744830452;
        static MenuPage _currentPage;

        static GameEngine _gameEngine;

        static string _status = "";
        static bool _showStatus = true;
        static int _currentMessage = 0;
        static int _maxMessage = 5;

        static int _serverPort = 8087;
        static bool _serverRunning = false;

        static TcpListener _tcpConnection;
        static UdpClient _udpConnection;

        static Task _handleConnections;
        static System.Timers.Timer _clientsCheck;
        static int _maxClientsNumber = 5;
        static int _clientCheckTimer = 1000;

        static byte _boardID = 0;
        static int _boardWidth = 1280;
        static int _boardHeight = 720;
        static int _moveDistance = 15;
        static int _gameTicks = 150;
        static int _foodAmount = 100;

        static ConcurrentDictionary<IPEndPoint, ClientInfo> _clients =
            new ConcurrentDictionary<IPEndPoint, ClientInfo>();

        // Server main thread, interaction with user
        static void Main(string[] args)
        {
            bool running = true;
            AddStatus("Ready");

            while (running)
            {
                PrintMenu(MenuPage.Main);
                var input = Console.ReadKey();

                switch (input.KeyChar)
                {
                    case '1':
                        if (!_serverRunning)
                            StartServer();
                        else
                            StopServer();
                        break;
                    case '2':
                        SwitchStatus();
                        break;
                    case '3':
                        ClearStatus();
                        break;
                    case '4':
                        ChangeGameSettings();
                        break;
                    case '5':
                        ChangeServerSettings();
                        break;
                    case '6':
                        ShowServerInfo();
                        break;
                    case '7':
                        running = false;
                        break;
                }
            }

            if (_serverRunning) StopServer();
        }

        //Clears status
        static void ClearStatus()
        {
            _status = "";
            _currentMessage = 0;
        }

        //Switches status to be visible or invisible
        static void SwitchStatus()
        {
            _showStatus = !_showStatus;
        }

        //Adds new message to status log
        static void AddStatus(string message)
        {
            if (_currentMessage < _maxMessage)
            {
                if (_currentMessage == 0)
                    _status += message;
                else
                    _status += "\n" + message;
                _currentMessage += 1;
            }
            else
            {
                string[] messages = _status.Split(new string[] { "\n" }, StringSplitOptions.None);
                string[] newMessages = new string[messages.Length];
                for (int i = 1; i < messages.Length; i++)
                {
                    newMessages[i - 1] = messages[i];
                }
                newMessages[messages.Length - 1] = message;
                _status = string.Join("\n", newMessages);
            }
        }

        //Refreshes menu to show status
        static void RefreshStatus()
        {
            PrintMenu(_currentPage);
        }

        //Prints chosen menu page
        static void PrintMenu(MenuPage page)
        {
            _currentPage = page;
            Console.WriteLine("\n");
            switch (page)
            {
                case MenuPage.Main:
                    Console.Clear();
                    if (!_serverRunning)
                        Console.WriteLine("1 - Start server");
                    else
                        Console.WriteLine("1 - Stop server");
                    if (_showStatus)
                        Console.WriteLine("2 - Hide status");
                    else
                        Console.WriteLine("2 - Show status");
                    Console.WriteLine("3 - Clear status");
                    Console.WriteLine("4 - Game settings");
                    Console.WriteLine("5 - Server settings");
                    Console.WriteLine("6 - Server Info");
                    Console.WriteLine("7 - Exit");
                    break;

                case MenuPage.Init:
                    Console.Clear();
                    Console.WriteLine("1 - Auto configuration");
                    Console.WriteLine("2 - Manual configuration");
                    Console.WriteLine("3 - Back");
                    break;

                case MenuPage.GameSettings:
                    Console.Clear();
                    Console.WriteLine("1 - Board ID");
                    Console.WriteLine("2 - Board width and height");
                    Console.WriteLine("3 - Food amount");
                    Console.WriteLine("4 - Back");
                    break;

                case MenuPage.ServerSettings:
                    Console.Clear();
                    Console.WriteLine("1 - Server log max message number");
                    Console.WriteLine("2 - Server tick length");
                    Console.WriteLine("3 - Server max players number");
                    Console.WriteLine("4 - Server state check time");
                    Console.WriteLine("5 - Back");
                    break;

                case MenuPage.Info:
                    Console.Clear();
                    Console.WriteLine($"Log max messages number:\t{_maxMessage}");
                    Console.WriteLine($"Server max players number:\t{_maxClientsNumber}");
                    Console.WriteLine($"Server tick length:\t\t{_gameTicks}");
                    Console.WriteLine($"Server state check time:\t{_clientCheckTimer} ms");
                    string status = _serverRunning ? "online" : "offline";
                    Console.WriteLine($"\nServer status:\t\t\t{status}");
                    int playersNumber = _clients.Count;
                    Console.WriteLine($"Players number:\t\t\t{playersNumber}");
                    Console.WriteLine("Players:");
                    if (playersNumber > 0)
                    {
                        foreach (var clientIP in _clients.Keys)
                        {
                            var client = _clients[clientIP];
                            Console.WriteLine($"\t{clientIP} - Name: {client.Name}");
                        }
                    }
                    Console.WriteLine($"\nBoard ID:\t\t\t{_boardID}");
                    Console.WriteLine($"Board width:\t\t\t{_boardWidth} p");
                    Console.WriteLine($"Board height:\t\t\t{_boardHeight} p");
                    Console.WriteLine($"Food amount:\t\t\t{_foodAmount}");
                    Console.WriteLine("\n1 - Back");
                    break;
            }

            if (_showStatus)
            {
                Console.WriteLine($"\nLog (last {_maxMessage} messages):\n" + _status);
            }
        }

        //Shows server info
        static void ShowServerInfo()
        {
            bool running = true;

            while (running)
            {
                PrintMenu(MenuPage.Info);
                var input = Console.ReadKey();

                switch (input.KeyChar)
                {
                    case '1':
                        running = false;
                        break;
                }
            }
        }

        //Changing game parameters
        static void ChangeGameSettings()
        {
            bool running = true;

            while (running)
            {
                PrintMenu(MenuPage.GameSettings);
                var input = Console.ReadKey();

                switch (input.KeyChar)
                {
                    case '1':
                        Console.Clear();
                        try
                        {
                            Console.Write("ID: ");
                            _boardID = Byte.Parse(Console.ReadLine());
                            AddStatus("Changes saved.");
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        break;

                    case '2':
                        Console.Clear();
                        try
                        {
                            Console.Write("Width: ");
                            _boardWidth = Int32.Parse(Console.ReadLine());
                            Console.Write("Height: ");
                            _boardHeight = Int32.Parse(Console.ReadLine());
                            AddStatus("Changes saved.");
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        break;

                    case '3':
                        Console.Clear();
                        try
                        {
                            Console.Write("Food amount: ");
                            _foodAmount = Byte.Parse(Console.ReadLine());
                            AddStatus("Changes saved.");
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        break;

                    case '4':
                        running = false;
                        break;
                }
            }
        }

        //Changing server parameters
        static void ChangeServerSettings()
        {
            bool running = true;

            while (running)
            {
                PrintMenu(MenuPage.ServerSettings);
                var input = Console.ReadKey();

                switch (input.KeyChar)
                {
                    case '1':
                        Console.Clear();
                        try
                        {
                            Console.Write("Max message number: ");
                            _maxMessage = Int32.Parse(Console.ReadLine());
                            AddStatus("Changes saved.");
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        break;

                    case '2':
                        Console.Clear();
                        try
                        {
                            Console.Write("Server tick length: ");
                            _gameTicks = Int32.Parse(Console.ReadLine());
                            AddStatus("Changes saved.");
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        break;

                    case '3':
                        Console.Clear();
                        try
                        {
                            Console.Write("Server max players number: ");
                            _maxClientsNumber = Int32.Parse(Console.ReadLine());
                            AddStatus("Changes saved.");
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        break;

                    case '4':
                        Console.Clear();
                        try
                        {
                            Console.Write("Server state check time: ");
                            _clientCheckTimer = Int32.Parse(Console.ReadLine());
                            AddStatus("Changes saved.");
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        break;

                    case '5':
                        running = false;
                        break;
                }
            }
        }

        //Prepares for server start
        static void StartServer()
        {
            bool running = true;

            while (running)
            {
                PrintMenu(MenuPage.Init);
                var input = Console.ReadKey();

                switch (input.KeyChar)
                {
                    case '1':
                        ServerInit();
                        running = false;
                        break;

                    case '2':
                        Console.Clear();
                        try
                        {
                            Console.Write("Port: ");
                            _serverPort = Int32.Parse(Console.ReadLine());
                            ServerInit();
                        }
                        catch
                        {
                            AddStatus("Cannot parse input.");
                        }
                        running = false;
                        break;

                    case '3':
                        running = false;
                        break;
                }
            }
        }

        //Performs server stop and cleanup
        static void StopServer()
        {
            _serverRunning = false;
            _tcpConnection.Stop();
            _udpConnection.Close();
            _udpConnection.Dispose();

            Task.WaitAll(_handleConnections);

            _clientsCheck.Stop();
            _clientsCheck.Dispose();

            foreach (var clientInfo in _clients.Values)
            {
                clientInfo.Client.Close();
            }

            _clients.Clear();

            ClearStatus();
            AddStatus("Ready");

            _gameEngine.Stop();
        }

        //Performs server initiation
        static void ServerInit()
        {
            var ep = new IPEndPoint(IPAddress.Any, _serverPort);
            _tcpConnection = new TcpListener(ep);
            _udpConnection = new UdpClient(_serverPort);
            _udpConnection.Client.IOControl(
                (IOControlCode)SIO_UDP_CONNRESET,
                new byte[] { 0, 0, 0, 0 },
                null);

            _tcpConnection.Start();

            _serverRunning = true;
            _handleConnections = Task.Factory.StartNew(() => AcceptClientConnections(), TaskCreationOptions.LongRunning);

            _clientsCheck = new System.Timers.Timer(_clientCheckTimer);
            _clientsCheck.Elapsed += AutoCheckClients;
            _clientsCheck.AutoReset = true;
            _clientsCheck.Enabled = true;

            ClearStatus();
            AddStatus($"Server running on port: {_serverPort}");

            _gameEngine = new GameEngine(_moveDistance, _gameTicks, _foodAmount, _boardWidth, _boardHeight, _clients);
            _gameEngine.Start();
        }

        //Sends serialized data packet to clients
        public static void SendDataToClients(DataPacket packet)
        {
            string json = JsonConvert.SerializeObject(packet);
            byte[] message = Encoding.UTF8.GetBytes(json);

            foreach (var clientInfo in _clients.Values)
            {
                try
                {
                    clientInfo.Client.GetStream().SendAsync(packet);
                }
                catch { }
            }
        }

        //Opens clients tcp connection, starts client thread and saves clients to database
        static void AcceptClientConnections()
        {
            while (_serverRunning)
            {
                try
                {
                    var client = _tcpConnection.AcceptTcpClient();
                    var clientAdress = (IPEndPoint)client.Client.RemoteEndPoint;

                    var ns = client.GetStream();

                    string clientName = ns.Receive();

                    if (_clients.Count >= _maxClientsNumber)
                    {
                        ns.Send("full");
                        ns.Close();
                        client.Close();

                        AddStatus($"{clientAdress} could not connect. Server is full.");
                        RefreshStatus();
                        continue;
                    }
                    else
                    {
                        ns.Send("ready");
                    }

                    string clientStatus = ns.Receive();
                    if (clientStatus != "ready") continue;

                    var boardInfo = new BoardInfoPacket()
                    {
                        ID = _boardID,
                        Width = _boardWidth,
                        Height = _boardHeight
                    };

                    ns.Send(boardInfo);

                    _clients.TryAdd(clientAdress,
                        new ClientInfo(client, clientName));

                    Task.Factory.StartNew(() => HandleClient(client));

                    AddStatus($"{clientAdress} just connected with name {clientName}.");
                    RefreshStatus();
                }
                catch (SocketException e)
                {
                    CheckClients();
                }
            }
        }

        //Client thread for reading data from client
        static void HandleClient(TcpClient client)
        {
            int tries = 0;

            while (_serverRunning)
            {
                try
                {
                    double direction = client.GetStream().Receive<double>();
                    tries = 0;

                    ClientInfo clientInfo;
                    _clients.TryGetValue((IPEndPoint)client.Client.RemoteEndPoint, out clientInfo);
                    if (clientInfo != null)
                    {
                        clientInfo.Direction = direction;
                        clientInfo.LastUpdate = DateTime.Now;
                    }
                }
                catch
                {
                    CheckClients();
                    break;
                }
            }
        }

        //Forces client disconnection
        static void DisconnectClient(IPEndPoint clientAdress)
        {
            ClientInfo clientInfo;
            _clients.TryRemove(clientAdress, out clientInfo);
            clientInfo.Client.Close();
            AddStatus($"{clientAdress} disconnected.");
            RefreshStatus();
        }

        //Performs client check for disconnected clients
        static void CheckClients()
        {
            foreach (var client in _clients)
            {
                var state = client.Value.Client.GetState();
                if (state != TcpState.Established)
                {
                    ClientInfo ignored;
                    _clients.TryRemove(client.Key, out ignored);

                    AddStatus($"{client.Key} disconnected.");
                    RefreshStatus();

                    client.Value.Client.Close();
                }
            }
        }

        //Performs auto check for disconnected clients every certain time
        static void AutoCheckClients(Object source, ElapsedEventArgs e)
        {
            CheckClients();
        }
    }
}
