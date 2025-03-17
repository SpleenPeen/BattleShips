namespace BattleShips
{
    internal class Menu
    {
        string[] _opts;
        int _sel;
        ConsoleColor _selCol;
        ConsoleColor _defCol;
        bool _outline;
        bool _centred;
        string _desc;
        int _width;

        public Menu(string[] options, string desc = "", int selected = 0, int width = 0, bool outline = false, bool centred = false)
        {
            _centred = centred;
            _outline = outline;
            _opts = options;
            _sel = Math.Clamp(selected, 0, _opts.Length-1);
            _selCol = ConsoleColor.White;
            _defCol = ConsoleColor.DarkGray;
            _desc = desc;
            
            //get width of menu
            foreach (var item in _opts)
                _width = Math.Max(_width, item.Length);
            _width += 2; //for selection indicator
            if (_outline)
                _width += 2; //for padding from outline
            _width = Math.Max(width, _width);
        }

        public bool UpdateMenu(ConsoleKey key)
        {
            var move = Vector2.GetMovementVector(key);

            _sel = Math.Clamp(_sel + move.y, 0, _opts.Length-1);

            if (key == ConsoleKey.Spacebar || key == ConsoleKey.Enter)
                return true;
            return false;
        }

        private void CentreLeftPos()
        {
            if (!_centred)
                return;
            Console.SetCursorPosition(Console.WindowWidth/2-(_width+2)/2, Console.GetCursorPosition().Top);
        }

        public void DrawMenu()
        {
            var lastCursorPos = Console.GetCursorPosition();
            var curLength = 1;

            if (_centred)
            {
                int topPos = Console.WindowHeight / 2 - (_opts.Length + 2 + (int)Math.Ceiling((float)_desc.Length / (_width - 2))) / 2 - 2;
                if (_desc.Length > 0)
                    topPos -= 1;
                CentreLeftPos();

                Console.SetCursorPosition(Console.GetCursorPosition().Left, topPos);
            }

            if (_outline)
            {
                DrawEdge();
                DrawInbetween();
            }

            //draw desc
            if (_desc.Length > 0)
            {
                var words = _desc.Split(' ');

                if (_outline)
                    Console.Write("| ");
                for (int i = 0; i < words.Length; i++)
                {
                    if (curLength + words[i].Length < _width)
                        Console.Write(words[i] + " ");
                    else
                    {
                        if (_outline)
                        {
                            FinishOffLine(curLength);
                            Console.Write("| ");
                        }
                        else
                        {
                            Console.WriteLine("");
                            CentreLeftPos();
                        }
                        Console.Write(words[i] + " ");
                        curLength = 1;
                    }
                    curLength += words[i].Length + 1;
                }
                if (_outline)
                    FinishOffLine(curLength);

                if (_outline)
                    DrawInbetween();
                else
                {
                    Console.WriteLine("");
                    Console.WriteLine("");
                    CentreLeftPos();
                }
            }

            //draw opts
            for (int i = 0; i < _opts.Length; i++)
            {
                if (_outline) 
                    Console.Write("| ");
                Console.ForegroundColor = _defCol;
                if (i == _sel)
                {
                    Console.ForegroundColor = _selCol;
                    Console.Write("> ");
                }
                Console.Write(_opts[i]);
                Console.ResetColor();

                if (_outline)
                {
                    curLength = _opts[i].Length + 1;
                    if (i == _sel)
                        curLength += 2;
                    FinishOffLine(curLength);
                }
                else
                {
                    Console.WriteLine("");
                    CentreLeftPos();
                }
            }

            if (_outline)
            {
                DrawInbetween();
                DrawEdge();
            }

            Console.SetCursorPosition(lastCursorPos.Left, lastCursorPos.Top);
        }

        private void FinishOffLine(int curLength)
        {
            for (int j = curLength; j < _width; j++)
                Console.Write(" ");
            Console.WriteLine("|");
            CentreLeftPos();
        }

        private void DrawInbetween()
        {
            Console.Write("|");
            for (int i = 0; i < _width; i++)
                Console.Write(" ");
            Console.WriteLine("|");
            CentreLeftPos();
        }

        private void DrawEdge()
        {
            Console.Write("+");
            for (int i = 0; i < _width; i++)
                Console.Write("-");
            Console.WriteLine("+");
            CentreLeftPos();
        }

        public int Count
        {
            get { return _opts.Length; }
        }

        public string GetOptString(int ind)
        {
            return _opts[ind];
        }

        public ConsoleColor DefCol
        {
            get { return _defCol; }
        }

        public ConsoleColor SelCol
        {
            get { return _selCol; }
        }

        public string SelText
        {
            get { return _opts[_sel]; }
        }

        public int Selected
        {
            get { return _sel; }
            set { _sel = value; }
        }
    }
}
