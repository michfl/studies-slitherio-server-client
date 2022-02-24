using Common;
using Common.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Timers;

namespace Server
{
    /*
     * Every server tick calculates new player positions
     * based on data sent by clients.
     * 
     * After calculation completes, DataPacket is created
     * and sent to clients.
     */
    public class GameEngine
    {
        public static int MoveDistance { get; set; }
        public static int GameTicks { get; set; }
        public static int Width { get; set; }
        public static int Height { get; set; }
        public static int FoodAmount { get; set; }
        public static ConcurrentDictionary<IPEndPoint, ClientInfo> Clients { get; set; }
        public static List<Point> Food { get; set; }

        private static Random _rnd;
        private System.Timers.Timer _gameTimer;
        private static Dictionary<string, ClientData> _clientsData = new Dictionary<string, ClientData>();

        private static int _foodReg = 15;

        public GameEngine(int moveDistance, int calcTicks, int foodAmount, int width, int height, ConcurrentDictionary<IPEndPoint, ClientInfo> clients)
        {
            MoveDistance = moveDistance;
            GameTicks = calcTicks;
            Width = width;
            Height = height;
            _rnd = new Random();
            Clients = clients;
            Food = new List<Point>();

            FoodAmount = foodAmount;
        }

        //Starts game engine and game refresh clock
        public void Start()
        {
            _gameTimer = new System.Timers.Timer(GameTicks);
            _gameTimer.Elapsed += GameRunning;
            _gameTimer.AutoReset = true;
            _gameTimer.Enabled = true;
        }

        //Stops game engine and performs cleanup
        public void Stop()
        {
            _gameTimer.Stop();
            _gameTimer.Dispose();
        }

        //Performs set of actions every game tick
        private static void GameRunning(Object source, ElapsedEventArgs e)
        {
            int dist = (GameTicks / 1000) * MoveDistance;

            UpdatePlayers();
            SpawnFood();
            PlayersMove();
            CheckCollisions();
            SendData();
        }

        //Spawns food based on food limits
        private static void SpawnFood()
        {
            while (Food.Count < FoodAmount)
            {
                Food.Add(new Point(_rnd.Next(10, Width - 10), _rnd.Next(10, Height - 10)));
            }
        }

        //Prepares and sends data packet to clients 
        private static void SendData()
        {
            var snakes = new Dictionary<string, (byte, int, List<Point>)>();
            var food = Food;

            foreach (var client in _clientsData.Values)
            {
                snakes.Add(client.Name, (client.Status, client.Score, client.Snake));
            }

            var data = new DataPacket(snakes, food);

            Program.SendDataToClients(data);
        }

        //Updates player states and game side player database
        private static void UpdatePlayers()
        {
            foreach (var client in Clients)
            {
                if (_clientsData.ContainsKey(client.Value.Name)) continue;

                var data = new ClientData(client.Value.Name, client.Key, 0, 1);

                var startPoint = new Point(_rnd.Next(10, Width - 10), _rnd.Next(10, Height - 10));
                data.Snake.Add(startPoint);
                data.Snake.Add(new Point(startPoint.X - 15, startPoint.Y));

                _clientsData.Add(data.Name, data);
            }

            var toDel = new List<string>();

            foreach (var client in _clientsData)
            {
                if (!Clients.ContainsKey(client.Value.Ip))
                {
                    toDel.Add(client.Key);
                }
            }

            foreach (var del in toDel)
            {
                _clientsData.Remove(del);
            }
            toDel.Clear();
        }

        //Perfoms move of each players snake and checks for wall collision
        private static void PlayersMove()
        {
            foreach (var client in _clientsData.Values)
            {
                double dir = Clients[client.Ip].Direction;

                if (client.Status == 0 && dir == -10)
                {
                    Respawn(client);
                    continue;
                }
                if (client.Status == 0) continue;

                var points = client.Snake.ToArray();
                var last = new Point(points[points.Length - 1].X, points[points.Length - 1].Y);

                for (int i = points.Length - 1; i > 0; i--)
                {
                    points[i].X = points[i - 1].X;
                    points[i].Y = points[i - 1].Y;
                }
                MovePoint(points[0], dir, MoveDistance);

                if (client.ToAdd)
                {
                    client.ToAdd = false;
                    client.Snake.Add(last);
                }

                var firstPoint = client.Snake.First<Point>();
                if (firstPoint.X <= 0 || firstPoint.X >= Width || firstPoint.Y <= 0 || firstPoint.Y >= Height)
                {
                    PlayerDeath(client);
                }
            }
        }

        //Kills player and spawns food on the snake points 
        private static void PlayerDeath(ClientData client)
        {
            client.Status = 0;
            foreach (var point in client.Snake)
            {
                if (!(point.X <= 0 || point.X >= Width || point.Y <= 0 || point.Y >= Height))
                {
                    Food.Add(point);
                }
            }
            client.Snake.Clear();
        }

        //Respawns player
        private static void Respawn(ClientData client)
        {
            client.Score = 0;
            client.Snake.Clear();

            var startPoint = new Point(_rnd.Next(10, Width - 10), _rnd.Next(10, Height - 10));
            client.Snake.Add(startPoint);
            client.Snake.Add(new Point(startPoint.X - 15, startPoint.Y));

            client.Status = 1;
        }

        //Moves single point in a gives direction by given distance
        private static void MovePoint(Point point, double direction, int distance)
        {
            double angle = (Math.PI / 180) * direction;

            point.X = (int)(point.X + distance * Math.Cos(angle));
            point.Y = (int)(point.Y + distance * Math.Sin(angle));
        }

        //Checks for player collision with other snakes, itself and food
        private static void CheckCollisions()
        {
            foreach (var client in _clientsData.Values)
            {
                if (client.Status == 0) continue;
                var clientSnake = client.Snake.ToArray();

                var toDel = new List<Point>();
                foreach (var food in Food)
                {
                    for (int i = 0; i < clientSnake.Length; i++)
                    {
                        if (DoFoodIntersect(clientSnake[0], food))
                        {
                            toDel.Add(food);
                            client.Score += 10;
                            client.ToAdd = true;
                        }
                    }
                }
                foreach (var food in toDel)
                {
                    Food.Remove(food);
                }

                foreach (var otherClient in _clientsData.Values)
                {
                    if (client.Name == otherClient.Name) continue;

                    var otherClientSnake = otherClient.Snake.ToArray();

                    for (int i = 1; i < otherClientSnake.Length; i++)
                    {
                        if (DoIntersect(clientSnake[0], clientSnake[1], otherClientSnake[i - 1], otherClientSnake[i]))
                        {
                            PlayerDeath(client);
                            break;
                        }
                    }
                    if (client.Status == 0) break;
                }
                if (client.Status == 0) continue;

                for (int i = 3; i < clientSnake.Length; i++)
                {
                    if (DoIntersect(clientSnake[0], clientSnake[1], clientSnake[i - 1], clientSnake[i]))
                    {
                        PlayerDeath(client);
                        break;
                    }
                }
            }
        }

        //Part of intersect recognision
        private static bool OnSegment(Point p, Point q, Point r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;

            return false;
        }

        //Part of intersect recognision
        private static int Orientation(Point p, Point q, Point r)
        {
            int val = (q.Y - p.Y) * (r.X - q.X) -
                    (q.X - p.X) * (r.Y - q.Y);

            if (val == 0) return 0;

            return (val > 0) ? 1 : 2;
        }

        //Checks if two segments intersect
        static bool DoIntersect(Point p1, Point q1, Point p2, Point q2)
        {
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            if (o1 != o2 && o3 != o4)
                return true;

            if (o1 == 0 && OnSegment(p1, p2, q1)) return true;
            if (o2 == 0 && OnSegment(p1, q2, q1)) return true;
            if (o3 == 0 && OnSegment(p2, p1, q2)) return true;
            if (o4 == 0 && OnSegment(p2, q1, q2)) return true;

            return false;
        }

        //Checks for food intersection
        static bool DoFoodIntersect(Point head, Point food)
        {
            if (food.X - _foodReg < head.X &&
                food.X + _foodReg > head.X &&
                food.Y - _foodReg < head.Y &&
                food.Y + _foodReg > head.Y)
                return true;
            return false;
        }
    }
}
