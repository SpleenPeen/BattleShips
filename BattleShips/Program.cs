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
        public static Random RNG { get; private set; } = new Random();
        static ConsoleKey _key;
        static GameScreen _gameScreen;
        static MainMenu _mainScreen;
        static History _history;
        static ScreenState _curScrn;
        static bool _drawFrame;

        public static void SwitchScreen(ScreenState screen, bool loadSave = false)
        {
            //switch screen
            _curScrn = screen;

            //reset the screen its switching to
            switch (screen)
            {
                case ScreenState.MainMenu:
                    _mainScreen = new MainMenu();
                    break;
                case ScreenState.Game:
                    _gameScreen = new GameScreen();
                    //load ongoing gamesave, if valid
                    if (!loadSave)
                        break;
                    var save = SaveManager.Instance.GetOngoingSave;
                    if (save == null)
                        break;
                    _gameScreen.LoadSave(save);
                    break;
                case ScreenState.History:
                    _history = new History();
                    break;
            }
        }

        private static void UpdateKey()
        {
            //sets key to the most recently pressed key
            while (true)
                _key = Console.ReadKey(true).Key;
        }

        private static void Main(string[] args)
        {
            //create a separate thread for getting key inputs
            Thread thread = new Thread(new ThreadStart(UpdateKey));
            thread.Start();

            //set up save manager
            SaveManager.Instance.MaxSaves = 20;
            SaveManager.Instance.SaveName = "GameSave";
            SaveManager.Instance.SavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\BattleShips\";
            
            //set initial state
            _curScrn = ScreenState.MainMenu;

            //initialise scren
            _mainScreen = new MainMenu();

            //draw the first frame
            _drawFrame = true;

            while (true)
            {
                //set curKey to the current _key, in order to prevent the key switching mid update
                var curKey = _key;

                //run update methods for current screen
                switch (_curScrn)
                {
                    case ScreenState.MainMenu:
                        DrawFrame = _mainScreen.Update(curKey);
                        break;
                    case ScreenState.Game:
                        DrawFrame = _gameScreen.Update(curKey);
                        break;
                    case ScreenState.History:
                        DrawFrame = _history.Update(curKey);
                        break;
                }

                //reset key press after it's been processed
                if (curKey != ConsoleKey.None)
                    _key = ConsoleKey.None;

                //continue to next frame if not set to draw
                if (!_drawFrame)
                    continue;
                _drawFrame = false; //reset drawframe

                //clear screen and run the current screens draw method
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

        private static bool DrawFrame
        {
            set
            {
                if (value)
                    _drawFrame = true;
            }
        }
    }
}