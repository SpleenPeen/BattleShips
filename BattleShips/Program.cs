using System.Collections;
using System.Text.Json;

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
        static ConsoleKey _key;
        static GameScreen _gameScreen;
        static MainMenu _mainScreen;
        static History _history;
        static ScreenState _curScrn;
        static short _saveSlots;
        public static bool DrawFrame;

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

        public static LinkedList<type> ArrayToLinkedList<type>(type[] array)
        {
            LinkedList<type> list = new LinkedList<type>();

            foreach (var item in array)
            {
                list.AddLast(item);
            }
            return list;
        }

        public static GameSave? GetGameSave(short ind = -1)
        {
            var files = Directory.GetFiles(SavePath);
            if (ind == -1)
                return JsonSerializer.Deserialize<GameSave>(File.ReadAllText(new DirectoryInfo(SavePath).GetFiles().OrderBy(f => f.LastWriteTime).ToList()[files.Length-1].FullName));
            else
                return JsonSerializer.Deserialize<GameSave>(File.ReadAllText(files[ind]));
        }

        public static GameSave? GetGameSave(string name)
        {
            var file = File.ReadAllText(Program.SavePath + name);
            return JsonSerializer.Deserialize<GameSave>(file);
        }

        public static void PrintPadded(string strng1, string strng2, int pad)
        {
            var output = strng1;

            for (int i = output.Length; i < pad; i++)
                output += " ";
            output += strng2;

            Console.WriteLine(output);
        }

        public static void WritePadded(string inpt, int pad)
        {
            var outpt = "";
            var curOut = inpt;
            for (int j = curOut.Length; j < pad; j++)
                curOut += " ";
            outpt += curOut;
            Console.Write(outpt);
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

        private static void UpdateKey()
        {
            while (true)
                _key = Console.ReadKey(true).Key;
        }

        private static void Main(string[] args)
        {
            Thread thread = new Thread(new ThreadStart(UpdateKey));
            thread.Start();

            _saveSlots = 20;
            _curScrn = ScreenState.MainMenu;
            _mainScreen = new MainMenu();
            _gameScreen = new GameScreen();
            _history = new History();
            DrawFrame = true;

            while (true)
            {
                var curKey = _key;
                switch (_curScrn)
                {
                    case ScreenState.MainMenu:
                        _mainScreen.Update(curKey);
                        break;
                    case ScreenState.Game:
                        _gameScreen.Update(curKey);
                        break;
                    case ScreenState.History:
                        _history.Update(curKey);
                        break;
                }

                //reset key
                if (curKey != ConsoleKey.None)
                {
                    _key = ConsoleKey.None;
                    DrawFrame = true;
                }

                if (!DrawFrame)
                {
                    DrawFrame = false;
                    continue;
                }
                DrawFrame = false;

                Console.Clear();
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
            }
        }
    }
}