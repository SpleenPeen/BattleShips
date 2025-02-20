namespace BattleShips
{
    internal class Program
    {
        enum ScreenState
        {
            MainMenu,
            Game,
            History
        }
        public static Random RNG = new Random();
        static GameScreen _gameScreen;
        static ScreenState _curScrn;

        private static void Main(string[] args)
        {
            _curScrn = ScreenState.Game;
            _gameScreen = new GameScreen();

            while (true)
            {
                switch (_curScrn)
                {
                    case ScreenState.Game:
                        _gameScreen.Update();
                        break;
                }
                Console.Clear();
            }
        }
    }
}