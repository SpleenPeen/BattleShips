namespace BattleShips
{
    internal class Menu
    {
        string[] _opts;
        int _sel;
        ConsoleColor _selCol;
        ConsoleColor _defCol;

        public Menu(string[] options, int startPos = 0)
        {
            _opts = options;
            _sel = Math.Clamp(startPos, 0, _opts.Length-1);
            _selCol = ConsoleColor.White;
            _defCol = ConsoleColor.DarkGray;
        }

        public int UpdateMenu(ConsoleKey key)
        {
            var move = Vector2.GetMovementVector(key);

            _sel = Math.Clamp(_sel + move.y, 0, _opts.Length-1);

            if (key == ConsoleKey.Spacebar || key == ConsoleKey.Enter)
                return _sel;
            return -1;
        }

        public void DrawMenu()
        {
            for (int i = 0; i < _opts.Length; i++)
            {
                Console.ForegroundColor = _defCol;
                if (i == _sel)
                    Console.ForegroundColor = _selCol;
                Console.WriteLine(_opts[i]);
                Console.ResetColor();
            }
        }
    }
}
