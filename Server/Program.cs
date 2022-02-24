using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    enum MenuPage
    {
        Main,
        Init,
    }

    class Program
    {
        static string _status = "";
        static bool _showStatus = true;
        static int _currentMessage = 0;
        static int _maxMessage = 5;

        //static string serverIP = "localhost";
        static int _serverPort = 8087;
        static bool _serverRunning = false;

        static UdpClient _udpClient;

        static void Main(string[] args)
        {
            bool running = true;
            AddStatus("Ready.");

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
                        running = false;
                        break;
                }
            }
        }

        static void ClearStatus()
        {
            _status = "";
            _currentMessage = 0;
        }

        static void SwitchStatus()
        {
            _showStatus = !_showStatus;
        }

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

        static void PrintMenu(MenuPage page)
        {
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
                    Console.WriteLine("3 - Exit");
                    break;

                case MenuPage.Init:
                    Console.Clear();
                    Console.WriteLine("1 - Auto configuration");
                    Console.WriteLine("2 - Manual configuration");
                    Console.WriteLine("3 - Back");
                    break;
            }

            if (_showStatus)
            {
                Console.WriteLine("\n" + _status);
            }
        }

        static void StartServer()
        {
            bool running = true;
            PrintMenu(MenuPage.Init);

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

        static void StopServer()
        {
            _udpClient.Close();

            ClearStatus();
            AddStatus("Ready");
            _serverRunning = false;
        }

        static void ServerInit()
        {
            _udpClient = new UdpClient(_serverPort);

            ClearStatus();
            AddStatus($"Server running on port: {_serverPort}");
            _serverRunning = true;
        }
    }
}
