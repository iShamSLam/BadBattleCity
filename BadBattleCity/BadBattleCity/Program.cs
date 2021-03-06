﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace BadBattleCity
{
    static class Game
    {
        //public enum ServerCommands
        //{
        //    InvitationToServer,
        //    ConnectionRequest,
        //    ConnectionAgreement,
        //    SendingField
        //}

        public enum Direction
        {
            left,
            right,
            up,
            down
        }

        public const int ServerPort = 15000;
        public const int ClientPort = 14000;
        public const int NumberOfTeams = 2;
        public const int GameSpeed = 250;

        static bool IsServerRunning = false;
        static bool GameStarted = false;
        static int NumberOfPlayers;

        static Connector Client = new Connector(new IPEndPoint(IPAddress.Broadcast, ServerPort), ClientPort);
        static Connector Server;
        //Это надо куда-то переместить
        static public List<Map.Point> Spawners = new List<Map.Point>();

        static void Main()
        {
            Thread offerCreateServerThread = new Thread(OfferCreateServer);
            offerCreateServerThread.Start();
            BeginSearchServer();
            if (!IsServerRunning)
                if (offerCreateServerThread.IsAlive)
                    offerCreateServerThread.Abort();

            Console.CursorVisible = false;
            StartClientGame();
        }

        private static void OfferCreateServer()
        {
            do
            {
                Console.Clear();
                Console.WriteLine("Enter the number of players to create the server");
            } while (!int.TryParse(Console.ReadLine(), out NumberOfPlayers));
            Console.WriteLine("Server start");
            IsServerRunning = true;
            StartServer();
        }

        private static void StartServer()
        {
            Server = new Connector(new IPEndPoint(IPAddress.Broadcast, ClientPort), ServerPort);
            Server.Start();

            while (GameStarted == false)
            {
                Server.Send("hi", Server.SenderDefaultEndPoint);
                for (int i = 0; i < Server.AllMessages.Count;)
                {
                    string[] message = Encoding.UTF8.GetString(Server.AllMessages[0].Message).Split(' ');
                    if (message[0] == "new")
                    {
                        Server.Send("+new", Server.AllMessages[0].Address);
                        Server.Clients.Add(Server.AllMessages[0].Address);
                        Console.WriteLine("К серверу добавлен новый клиент");
                    }
                    Server.AllMessages.RemoveAt(0);
                    if (Server.Clients.Count >= NumberOfPlayers)
                    {
                        GameStarted = true;
                        Server.AllMessages.Clear();
                        break;
                    }
                }
                Thread.Sleep(500);
            }
            Console.WriteLine("Сервер запустил игру");
            StartServerGame();
        }

        private static void StartServerGame()
        {
            Map.DownloadMap();
            SendMessageToAllClients("map" + " " + GetStringMap());
            //for (int i = 0; i < Server.Clients.Count; i++)
            //    Server.Send("command" + " " + i % NumberOfTeams, Server.Clients[i]);
            //FindSpawners();
            //ServerGamingCycle();
        }

        private static void ServerGamingCycle()
        {
            DateTime time = DateTime.Now;
            while (GameStarted)
            {
                CreatePlayers();
                ExecuteClientsCommands();
                MoveObjects();
                UpdateClientsData();

                Thread.Sleep(Math.Max(GameSpeed - DateTime.Now.Millisecond - time.Millisecond, 0));
                time = DateTime.Now;
            }
        }

        private static void CreatePlayers()
        {
            throw new NotImplementedException();
        }

        private static void UpdateClientsData()
        {
            throw new NotImplementedException();
        }

        private static void MoveObjects()
        {
            throw new NotImplementedException();
        }

        private static void ExecuteClientsCommands()
        {
            throw new NotImplementedException();
        }

        private static void FindSpawners()
        {
            char[] spawnersChars = { 'z', 'x', 'c', 'v' };
            for (int i = 0; i < Map.MapWidth; i++)
            {
                for (int j = 0; j < Map.MapWidth; j++)
                {
                    if (Array.IndexOf(spawnersChars, Map.Field[i, j]) >= 0)
                        Spawners.Add(new Map.Point(j, i));
                }
            }
        }

        private static void StartClientGame()
        {
            //Дальше подключаемся к серверу
            //Тут должна быть обработка команд полученных от сервера
            Thread HandlingPlayerActionsThread = new Thread(Player.HandlingPlayerActions);
            HandlingPlayerActionsThread.Start();
            while (true)
            {
                CommandProcessing();
            }
        }

        private static void CommandProcessing()
        {
            for (int i = 0; i < Client.AllMessages.Count;)
            {
                string[] message = Encoding.UTF8.GetString(Client.AllMessages[0].Message).Split(' ');
                Client.AllMessages.RemoveAt(0);

                switch (message[0])
                {
                    case "setcommand":
                        Player.Command = int.Parse(message[1]);
                        break;
                    case "nexttick":
                        Player.TickTreatment();
                        break;
                    case "map":
                        Map.RedrawMap(message);
                        break;
                    case "updatemap":
                        Map.UpdateMap(message);
                        break;
                    default:
                        break;
                }
            }
        }

        private static void SendMessageToAllClients(string message)
        {

            for (int i = 0; i < Server.Clients.Count; i++)
            {
                Server.Send(message, Server.Clients[i]);
            }
        }

        private static string GetStringMap()
        {
            StringBuilder MapString = new StringBuilder();
            for (int i = 0; i < Map.MapWidth; i++)
                for (int j = 0; j < Map.MapWidth; j++)
                {
                    MapString.Append((char)Map.Field[i, j]);
                }
            return MapString.ToString();
        }

        private static void BeginSearchServer()
        {
            bool StopSearchingServer = false;

            Client.Start();
            while (!StopSearchingServer || !CheckConnect())
            {
                StopSearchingServer = false;
                for (int i = 0; i < Client.AllMessages.Count;)
                {
                    string[] message = Encoding.UTF8.GetString(Client.AllMessages[0].Message).Split(' ');
                    Client.AllMessages.RemoveAt(0);
                    if (message[0] == "hi")
                    {
                        StopSearchingServer = true;
                        Client.Stop();
                        Client = new Connector(Client.LastReseivePoint, ClientPort);
                        Client.Start();
                        Thread.Sleep(100);
                        Client.AllMessages.Clear();
                        Client.Send("new", Client.SenderDefaultEndPoint);
                    }
                }
                Thread.Sleep(1000);
            }
        }

        private static bool CheckConnect()
        {
            for (int i = 0; i < Client.AllMessages.Count; i++)
            {
                string[] message = Encoding.UTF8.GetString(Client.AllMessages[i].Message).Split(' ');
                if (message[0] == "+new")
                {
                    Console.WriteLine("Подключение подтверждено, ожидание запросов сервера");
                    Client.AllMessages.RemoveAt(i);
                    return true;
                }
            }
            Client.Stop();
            Client = new Connector(new IPEndPoint(IPAddress.Broadcast, ServerPort), ClientPort);
            return false;
        }
    }

    static class Map
    {
        public static Cells[,] Field;
        public enum Cells
        {
            empty = '0',
            flag = '1',
            spawner = '2',
            booster = '3',
            water = '4',
            brick = '5',
            wall = '6',
            tank = '7',
            bullet = '8',
            boom = '9'
        }

        public struct Point
        {
            public int X, Y;
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
        }
        public static int LineWidth = 1;
        public static int MapWidth;

        internal static void UpdateMap(string[] message)
        {
            for (int i = 1; i < message.Length / 3;)
            {
                Point point = new Point(int.Parse(message[i++]), int.Parse(message[i++]));
                char type = message[i++][0];
                if (type < 10)
                    DrawCell(point, GetColor((Cells)type));
                else
                    DrawCell(point, GetColor((Cells)type), type);
            }
        }

        public static void DownloadMap()
        {
            string fileName = "";
            Console.WriteLine("Enter the name of the map");

            do
            {
                fileName = Console.ReadLine();
                if (File.Exists(fileName))
                    break;
                else
                    Console.WriteLine("Error. There is no map");
            } while (true);
                try
            {
                string[] textMap = File.ReadAllLines(fileName);
                MapWidth = textMap[0].Length;
                Field = new Cells[MapWidth, MapWidth];

                for (int i = 0; i < MapWidth; i++)
                    for (int j = 0; j < MapWidth; j++)
                    {
                        Field[i, j] = (Cells)textMap[i][j];
                    }
            }
            catch (Exception error)
            {
                Console.WriteLine("Map read error: \n{0}", error);
            }
        }

        public static void RedrawMap(string[] message)
        {
            Console.Clear();
            MapWidth = (int)Math.Sqrt(message[1].Length);

            for (int i = 0; i < MapWidth; i++)
            {
                for (int j = 0; j < MapWidth; j++)
                {
                    Console.ForegroundColor = GetColor((Cells)message[1][MapWidth * i + j]);
                    for (int k = 0; k < 2 * LineWidth; k++)
                        Console.Write('█');
                }
                Console.WriteLine();
            }
        }

        public static ConsoleColor GetColor(Cells cell)
        {
            switch (cell)
            {
                case Cells.empty:
                    return ConsoleColor.Black;
                case Cells.flag:
                    return ConsoleColor.Magenta;
                case Cells.spawner:
                    return ConsoleColor.White;
                case Cells.booster:
                    return ConsoleColor.Yellow;
                case Cells.water:
                    return ConsoleColor.Blue;
                case Cells.brick:
                    return ConsoleColor.DarkRed;
                case Cells.wall:
                    return ConsoleColor.Gray;
                case Cells.bullet:
                    return ConsoleColor.DarkRed;
                default:
                    return ConsoleColor.Red;
            }
        }

        internal static void DrawCell(Point coords, ConsoleColor color, char c = '█')
        {
            Console.ForegroundColor = color;
            int maxY = coords.Y * LineWidth + LineWidth;
            int maxX = coords.X * LineWidth * 2 + LineWidth * 2;
            for (int i = coords.Y * LineWidth; i < maxY; i++)
                for (int j = coords.X * LineWidth * 2; j < maxX; j++)
                {
                    Console.SetCursorPosition(j, i);
                    Console.Write(c);
                }
        }
    }

    static class Player
    {
        static public int Command;
        static public Game.Direction Direction = Game.Direction.left;
        static public Map.Point Coords;
        static public int ShotFrequency = 3;
        static public int MoveFrequency = 5;

        static public int IsReadyToShot = 0;
        static public int IsReadyToMove = 0;

        static public bool Moved = false;
        static public bool Fired = false;

        public static void TickTreatment()
        {
            if (IsReadyToShot > 0) IsReadyToShot--;
            if (IsReadyToMove > 0) IsReadyToMove--;
            Moved = false;
            Fired = false;
        }

        public static void HandlingPlayerActions()
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.UpArrow:
                        TryToMove(Game.Direction.up);
                        break;
                    case ConsoleKey.DownArrow:
                        TryToMove(Game.Direction.down);
                        break;
                    case ConsoleKey.LeftArrow:
                        TryToMove(Game.Direction.left);
                        break;
                    case ConsoleKey.RightArrow:
                        TryToMove(Game.Direction.right);
                        break;
                    case ConsoleKey.Spacebar:
                        TryToShot();
                        break;
                }
            }
        }

        private static void TryToShot()
        {
            if (IsReadyToShot == 0)
            {
                Fired = true;
                IsReadyToShot = ShotFrequency;
            }
        }

        private static void TryToMove(Game.Direction direction)
        {
            if (IsReadyToMove == 0)
            {
                if (Direction != direction)
                    Direction = direction;
                else
                    Moved = true;
                IsReadyToMove = MoveFrequency;
            }
        }
    }
}


//String host = ;
// Получение ip-адреса.
//System.Net.IPAddress ip = System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList[0];




//public enum Direction
//{
//    left,
//    right,
//    up,
//    down
//}

//struct Point
//{
//    public int X, Y;
//    public Point(int x, int y)
//    {
//        X = x;
//        Y = y;
//    }
//}

//class Bullet
//{
//    private Map.Cells ReplacedСell = Map.Cells.empty;
//    public Point Coords;
//    public int Speed = 1;
//    Point OldCoords;
//    Direction Direction;
//    public bool IsMobile = true;
//    public bool Removable = false;

//    public Bullet(Point coords, Direction direction)
//    {
//        Coords = coords;
//        Direction = direction;
//        OldCoords = new Point(coords.X, coords.Y);
//    }

//    public static void RemoveInactiveBullets(List<Bullet> bullets)
//    {
//        for (int i = 0; i < bullets.Count; i++)
//        {
//            if (bullets[i].Removable)
//            {
//                bullets.RemoveAt(i--);
//            }
//        }
//    }

//    public void Move()
//    {
//        OldCoords.X = Coords.X;
//        OldCoords.Y = Coords.Y;

//        if (IsMobile)
//            switch (Direction)
//            {
//                case Direction.left:
//                    Coords.X -= 1;
//                    break;
//                case Direction.right:
//                    Coords.X += 1;
//                    break;
//                case Direction.up:
//                    Coords.Y -= 1;
//                    break;
//                case Direction.down:
//                    Coords.Y += 1;
//                    break;
//            }
//        HandlingPassedCell();
//    }

//    public void DrawBullet()
//    {
//        if (IsMobile)
//        {
//            Map.DrawCell(OldCoords, Map.GetColor(ReplacedСell));
//            ReplacedСell = Map.Field[Coords.Y, Coords.X];
//            Map.DrawCell(Coords, Map.GetColor(Map.Cells.bullet));
//        }
//        else if (!Removable)
//        {
//            // TODO 
//            // Сделать обработку взрывов
//            Map.DrawCell(OldCoords, Map.GetColor(ReplacedСell));
//            Map.DrawCell(Coords, Map.GetColor(Map.Cells.boom));
//            Removable = true;
//        }
//        else
//        {
//            Map.DrawCell(OldCoords, Map.GetColor(ReplacedСell));
//        }
//    }

//    private void HandlingPassedCell()
//    {
//        switch (Map.Field[Coords.Y, Coords.X])
//        {
//            case Map.Cells.flag:
//                break;
//            case Map.Cells.spawner:
//                break;
//            case Map.Cells.booster:
//                break;
//            case Map.Cells.water:
//                break;
//            case Map.Cells.brick:
//                IsMobile = false;
//                break;
//            case Map.Cells.wall:
//                IsMobile = false;
//                Removable = true;
//                break;
//            case Map.Cells.tank:
//                break;
//            case Map.Cells.bullet:
//                break;
//            case Map.Cells.boom:
//                break;
//            default:
//                break;
//        }
//    }
//}

//static class Map
//{
//    public enum Cells
//    {
//        empty = '0',
//        flag = '1',
//        spawner = '2',
//        booster = '3',
//        water = '4',
//        brick = '5',
//        wall = '6',
//        tank = '7',
//        bullet = '8',
//        boom = '9'
//    }

//    public static int LineWidth = 1;
//    public static Cells[,] Field;
//    public static int MapWidth;

//    public static bool DownloadMap()
//    {
//        if (!File.Exists("map1.txt"))
//        {
//            Console.WriteLine("Error. There is no map");
//            return false;
//        }
//        else
//        {
//            try
//            {
//                string[] textMap = File.ReadAllLines("map1.txt");
//                MapWidth = textMap[0].Length;
//                Field = new Cells[MapWidth, MapWidth];

//                for (int i = 0; i < MapWidth; i++)
//                    for (int j = 0; j < MapWidth; j++)
//                    {
//                        Field[i, j] = (Cells)textMap[i][j];
//                    }
//                return true;
//            }
//            catch (Exception error)
//            {
//                Console.WriteLine("Map read error: \n{0}", error);
//                return false;
//            }
//        }
//    }

//    public static void FirstDrawMap()
//    {
//        for (int item = 0; item < MapWidth; item++)
//        {
//            for (int i = 0; i < LineWidth; i++)
//            {
//                for (int item2 = 0; item2 < MapWidth; item2++)
//                {
//                    if (item2 != ' ')
//                    {
//                        Console.ForegroundColor = GetColor(Field[item, item2]);
//                        for (int j = 0; j < 2 * LineWidth; j++)
//                            Console.Write('█');
//                    }
//                }
//                Console.WriteLine();
//            }
//        }
//    }

//    public static ConsoleColor GetColor(Cells cell)
//    {
//        switch (cell)
//        {
//            case Cells.empty:
//                return ConsoleColor.Black;
//            case Cells.flag:
//                return ConsoleColor.Magenta;
//            case Cells.spawner:
//                return ConsoleColor.White;
//            case Cells.booster:
//                return ConsoleColor.Yellow;
//            case Cells.water:
//                return ConsoleColor.Blue;
//            case Cells.brick:
//                return ConsoleColor.DarkRed;
//            case Cells.wall:
//                return ConsoleColor.Gray;
//            case Cells.bullet:
//                return ConsoleColor.DarkRed;
//            default:
//                return ConsoleColor.Red;
//        }
//    }

//    internal static void DrawCell(Point coords, ConsoleColor color, char c = '█')
//    {
//        Console.ForegroundColor = color;
//        int maxY = coords.Y * LineWidth + LineWidth;
//        int maxX = coords.X * LineWidth * 2 + LineWidth * 2;
//        for (int i = coords.Y * LineWidth; i < maxY; i++)
//            for (int j = coords.X * LineWidth * 2; j < maxX; j++)
//            {
//                Console.SetCursorPosition(j, i);
//                Console.Write(c);
//            }
//    }
//}

//static class Game
//{
//    public static List<Bullet> ActiveBullets = new List<Bullet>();

//    public static bool GameStarted = false;


//    static void Main(string[] args)
//    {
//        Console.CursorVisible = false;
//        Console.SetWindowSize(Map.LineWidth * 30 * 2, Math.Min(30 * Map.LineWidth, 44));


//        //StartServer();
//        //ConnectToServer();

//        //StartGameCycle();
//        //Initialization();
//        ////////////////////////////////////////////////////////////////////////////
//        if (!Map.DownloadMap())
//        {
//            Console.Read();
//            return;
//        }
//        Map.FirstDrawMap();
//        ActiveBullets.Add(new Bullet(new Point(10, 12), Direction.right));

//        DateTime time = DateTime.Now;
//        while(true)
//        {
//            if ((DateTime.Now - time).Milliseconds > 100)
//            {
//                time = DateTime.Now;
//                for (int i = 0; i < ActiveBullets.Count; i++)
//                {
//                    if (ActiveBullets[i].IsMobile)
//                        ActiveBullets[i].Move();
//                    ActiveBullets[i].DrawBullet();

//                    // TODO 
//                    // Сделать удаление взорвавшихся снарядов
//                    // Учитывать двойную толщину вертикальных линий
//                }
//                Bullet.RemoveInactiveBullets(ActiveBullets);
//            }
//        }
//        ////////////////////////////////////////////////////////////////////////////

//        //DrawMap();
//        Console.Read();
//        //ClearMap();

//        //SendData();
//        //GetData();
//    }


//}


//class NewAr
//{
//    List<Bullet> AllBullets = new List<Bullet>();

//}

// █





/*
 * Классы: 
 * Карта
 * Объект карты
 * Игрок, пуля, 
 */


//ConnectToServer
//Init
//GetTheMap
//