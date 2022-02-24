using Common.Messages;
using Common.Transport;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Client
{
    /*
     * Game class responsible for client side game
     * render and frame preparation.
     * 
     * Sends data to the server based on 
     * user input.
     */
    public class Game
    {
        public string ServerIP { get; set; }
        public int ServerPort { get; set; }
        public string PlayerName { get; set; }
        public MainWindow MainWindow { get; set; }

        public int BoardWidth { get; set; }
        public int BoardHeight { get; set; }

        public double PlayerDirection { get; set; }
        public bool PlayerAlive { get; set; }
        public double Sensitivity { get; set; }
        public bool ClientRunning { get; set; }

        private readonly Task _handleDataReceive;

        private GameWindow _gameWindow;
        private TcpClient _tcpClient;

        private BufferBlock<DataPacket> _gameData;

        public Game(string serverIP, int serverPort, string playerName, MainWindow mainWindow)
        {
            ServerIP = serverIP;
            ServerPort = serverPort;
            PlayerName = playerName;
            MainWindow = mainWindow;
            PlayerDirection = 0;
            ClientRunning = false;
            Sensitivity = 5;

            PlayerAlive = true;
        }

        //Starts the game and connects to the server
        public async Task Start()
        {
            try
            {
                _tcpClient = new TcpClient(ServerIP, ServerPort);
                var ns = _tcpClient.GetStream();

                ns.Send(PlayerName);

                string serverStatus = ns.Receive();
                ns.Send("ready");

                _gameWindow = new GameWindow(this);
                switch (serverStatus)
                {
                    case "ready":
                        var boardInfo = ns.Receive<BoardInfoPacket>();
                        BoardWidth = boardInfo.Width;
                        BoardHeight = boardInfo.Height;
                        _gameWindow.gameBoard.Width = BoardWidth;
                        _gameWindow.gameBoard.Height = BoardHeight;
                        _gameWindow.textBlock_status.Text = "Connected";

                        ClientRunning = true;
                        Task.Factory.StartNew(() => HandleDataReceive(), TaskCreationOptions.LongRunning);
                        break;

                    case "full":
                        _gameWindow.textBlock_status.Text = "Server is full";
                        break;
                }

                _gameData = new BufferBlock<DataPacket>();

                _gameWindow.Owner = MainWindow;
                _gameWindow.Show();

                while (ClientRunning)
                {
                    var data = await _gameData.ReceiveAsync<DataPacket>();
                    var dataToRender = PrepareRenderData(data);
                    RenderGame(dataToRender);
                }
            }
            catch (SocketException e)
            {
                MainWindow.WrongIP();
            }
            catch (Exception e)
            {
                //Console.WriteLine("");
            }
        }

        //Stops the game and disconnects from the server
        public void Stop()
        {
            ClientRunning = false;
            if (_handleDataReceive != null)
                Task.WaitAll(_handleDataReceive);

            try
            {
                if (_tcpClient != null)
                {
                    _tcpClient.Close();
                    _tcpClient.Dispose();
                }
            }
            catch { }
            try
            {
                if (_gameWindow != null)
                    _gameWindow.Close();
            }
            catch { }
        }

        //Handles data receive from the server and adds it to the buffer
        private void HandleDataReceive()
        {
            int tries = 0;

            while (ClientRunning)
            {
                try
                {
                    var data = _tcpClient.GetStream().Receive<DataPacket>();
                    if (data == null) throw new SocketException();

                    _gameData.Post<DataPacket>(data);
                    tries = 0;
                }
                catch (SocketException e)
                {
                    tries += 1;
                    _gameWindow.Dispatcher.Invoke(() =>
                    {
                        ChangeStatus($"Server not responding [{tries}/3]");
                    });

                    if (tries > 3)
                    {
                        _gameWindow.Dispatcher.Invoke(() =>
                        {
                            ChangeStatus("Server offline");
                        });
                        Stop();
                        break;
                    }
                    Thread.Sleep(3000);
                }
            }
        }

        //Changes visible server status
        private void ChangeStatus(string status)
        {
            _gameWindow.textBlock_status.Text = status;
        }

        //Sends direction change to the server
        public void DirectionLeft()
        {
            if (!PlayerAlive) return;
            PlayerDirection -= Sensitivity;
            if (PlayerDirection < 0)
            {
                PlayerDirection += 360;
            }
            try
            {
                _tcpClient.GetStream().Send(PlayerDirection);
            }
            catch { }
        }

        //Sends direction change to the server
        public void DirectionRight()
        {
            if (!PlayerAlive) return;
            PlayerDirection += Sensitivity;
            if (PlayerDirection > 360)
            {
                PlayerDirection -= 360;
            }
            try
            {
                _tcpClient.GetStream().Send(PlayerDirection);
            }
            catch { }
        }

        //Sends spawn request to the server
        public void RequesSpawn()
        {
            if (PlayerAlive) return;
            _tcpClient.GetStream().Send((double)-10);
            PlayerDirection = 0;
        }

        //Prepares data to be rendered by the client
        public RenderData PrepareRenderData(DataPacket data)
        {
            if (data == null) return null;
            var renderData = new RenderData();

            string stats = "";
            var statsData = new List<KeyValuePair<int, string>>();

            foreach (var snakeList in data.Snakes.Keys)
            {
                statsData.Add(new KeyValuePair<int, string>(data.Snakes[snakeList].Item2, snakeList));

                var snakeArr = data.Snakes[snakeList].Item3;
                var snake = new Polyline();
                var coll = new PointCollection();
                if (snakeList == PlayerName)
                {
                    snake.Stroke = Brushes.Blue;
                    if (data.Snakes[snakeList].Item1 == 1)
                    {
                        PlayerAlive = true;
                    }
                    else
                    {
                        PlayerAlive = false;
                    }
                }
                else
                {
                    snake.Stroke = Brushes.Black;
                }
                snake.StrokeThickness = 10;
                foreach (var point in snakeArr)
                {
                    coll.Add(new System.Windows.Point(point.X, point.Y));
                }
                snake.Points = coll;
                renderData.Snakes.Add(snake);
            }

            foreach (var food in data.Food)
            {
                var line = new Line();
                line.Stroke = Brushes.Red;
                line.StrokeThickness = 10;
                line.X1 = food.X;
                line.Y1 = food.Y - 5;
                line.X2 = food.X;
                line.Y2 = food.Y + 5;
                renderData.Food.Add(line);
            }

            statsData.Sort((x, y) => (y.Key.CompareTo(x.Key)));
            foreach (var player in statsData)
            {
                stats += $"{player.Key} {player.Value}\n";
            }
            renderData.Statistics = stats;

            return renderData;
        }

        //Clears current canvas state and draws new state
        public void RenderGame(RenderData data)
        {
            if (data == null) return;

            _gameWindow.textBlock_statistics.Text = data.Statistics;
            _gameWindow.gameBoard.Children.Clear();

            foreach (var snake in data.Snakes)
            {
                _gameWindow.gameBoard.Children.Add(snake);
            }
            foreach (var food in data.Food)
            {
                _gameWindow.gameBoard.Children.Add(food);
            }

            if (!PlayerAlive)
            {
                var textBlock = new TextBlock();
                textBlock.Text = "Game Over";
                textBlock.Foreground = Brushes.Black;
                textBlock.FontSize = 72;
                textBlock.FontFamily = new FontFamily("Papyrus");
                Canvas.SetLeft(textBlock, BoardWidth / 2 - 190);
                Canvas.SetTop(textBlock, BoardHeight / 2 - 57);
                _gameWindow.gameBoard.Children.Add(textBlock);
            }
        }
    }
}
