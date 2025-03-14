﻿using System.Text.Json;

namespace BattleShips
{
    public enum ScreenState
    {
        MainMenu,
        Game,
        History
    }

    internal class Program
    {
        public static string SavePath { get; private set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BattleShips\";
        public static string SaveName { get; private set; } = "GameSave";
        public static Random RNG { get; private set; } = new Random();
        public static ConsoleKey Key;
        static GameScreen _gameScreen;
        static MainMenu _mainScreen;
        static History _history;
        static ScreenState _curScrn;
        static short _saveSlots;

        public static void SwitchScreen(ScreenState screen)
        {
            //switch screen
            _curScrn = screen;

            //reset the screen after switching back to it
            switch (screen)
            {
                case ScreenState.MainMenu:
                    _mainScreen = new MainMenu();
                    break;
                case ScreenState.Game:
                    _gameScreen = new GameScreen();
                    break;
                case ScreenState.History:
                    _history = new History();
                    break;
            }
        }

        public static Queue<type> ArrayToQueue<type>(type[] array)
        {
            Queue<type> queue = new Queue<type>();

            foreach (var item in array)
            {
                queue.Enqueue(item);
            }
            return queue;
        }

        public static GameSave? GetGameSave(short ind = -1)
        {
            var files = Directory.GetFiles(SavePath);
            if (ind == -1)
                return JsonSerializer.Deserialize<GameSave>(File.ReadAllText(files[files.Length - 1]));
            else
                return JsonSerializer.Deserialize<GameSave>(File.ReadAllText(files[ind]));
        }

        public static void ClearOldSaves()
        {
            var files = Directory.GetFiles(Program.SavePath);
            if (files.Length >= _saveSlots)
            {
                for (int i = 0; i < files.Length - (_saveSlots-1); i++)
                {
                    File.Delete(files[i]);
                }
            }
        }

        public static short GetLatestSaveNum()
        {
            var files = Directory.GetFiles(SavePath);
            var file = files[files.Length - 1];
            short cur;
            short outpt = 0;
            int i = 1;
            while (short.TryParse(file.AsSpan(file.Length-(4+i), i), out cur))
            {
                outpt = cur;
                i++;
            }
            return outpt;
        }

        public static void LoadLatestSave()
        {
            _gameScreen.LoadLatestSave();
        }

        private static void Main(string[] args)
        {
            _saveSlots = 3;
            _curScrn = ScreenState.MainMenu;
            _mainScreen = new MainMenu();
            _gameScreen = new GameScreen();
            _history = new History();

            while (true)
            {
                switch (_curScrn)
                {
                    case ScreenState.MainMenu:
                        _mainScreen.Update();
                        break;
                    case ScreenState.Game:
                        _gameScreen.Update();
                        break;
                    case ScreenState.History:
                        _history.Update();
                        break;
                }

                switch (_curScrn)
                {
                    case ScreenState.MainMenu:
                        _mainScreen.Draw();
                        break;
                    case ScreenState.Game:
                        _gameScreen.Draw();
                        break;
                    case ScreenState.History:
                        _history.Draw();
                        break;
                }
                Key = Console.ReadKey(intercept: true).Key;
                Console.Clear();
            }
        }
    }
}