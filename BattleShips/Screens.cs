using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips
{
    internal class GameScreen
    {
        enum GameState
        {
            BoardAllocation,
            ShipAllocation,
            Gameplay,
            GameOver
        }

        Board _playerBoard;
        Board _enemyBoard;
        GameState _curState;
        Vector2 _selected;
        string _padding;
        List<int[]> _ships;
        Vector2 _origin;

        public GameScreen()
        {
            _origin = new Vector2(-1, 0);
            _padding = "  ";
            _curState = GameState.BoardAllocation;
            _selected = new Vector2();
            _ships = new List<int[]>();
            _playerBoard = new Board(10, 10);
            _enemyBoard = new Board();
        }

        public void Update()
        {
            switch (_curState)
            {
                case GameState.BoardAllocation:
                    BoardAlloUpdate();
                    break;
                case GameState.ShipAllocation:
                    ShipAlloUpdate();
                    break;
                case GameState.Gameplay:
                    GameplayUpdate();
                    break;
                case GameState.GameOver:
                    GameoverUpdate();
                    break;
            }
        }

        private void BoardAlloUpdate()
        {
            DrawStrings(_playerBoard.GetDrawLines());

            var key = Console.ReadKey().Key;

            var newSize = Vector2.GetMovementVector(key);
            newSize.x = Math.Max(1, _playerBoard.Width + newSize.x);
            newSize.y = Math.Max(1, _playerBoard.Height + newSize.y);

            _playerBoard.SetSize(newSize.x, newSize.y);

            if (key == ConsoleKey.Enter)
            {
                _enemyBoard.SetSize(_playerBoard.Width, _playerBoard.Height);
                _curState = GameState.ShipAllocation;
            }
        }

        private void ShipAlloUpdate()
        {
            //draw player board
            Console.WriteLine("Your Board");
            var drawLines = _playerBoard.GetDrawLines(_selected);
            //change currently selected space fields
            if (_origin.x >= 0)
            {
                for (int y = Math.Min(_origin.y, _selected.y); y <= Math.Max(_selected.y, _origin.y); y++)
                {
                    var curLine = drawLines[_playerBoard.GetYPosStrng(y)].ToCharArray();
                    for (int x = Math.Min(_origin.x, _selected.x); x <= Math.Max(_selected.x, _origin.x); x++)
                    {
                        curLine[_playerBoard.GetXPosStrng(x)] = 'O';
                    }
                    drawLines[_playerBoard.GetYPosStrng(y)] = new string(curLine);
                }
            }
            DrawStrings(drawLines);

            var key = Console.ReadKey().Key;

            //handle keypress
            //handle starting selection, ending selection and confirming selection
            switch (key)
            {
                //setting origin point
                case ConsoleKey.Spacebar:
                    if (_origin.x < 0)
                    {
                        if (_playerBoard.GetSpaceState(_selected.x, _selected.y) == Board.SpaceStates.ship)
                            break;
                        _origin.x = _selected.x;
                        _origin.y = _selected.y;
                    }
                    else
                    {
                        //change all selected spaces to ship spaces
                        int shipSize = 0;
                        for (int y = Math.Min(_origin.y, _selected.y); y <= Math.Max(_selected.y, _origin.y); y++)
                        {
                            for (int x = Math.Min(_origin.x, _selected.x); x <= Math.Max(_selected.x, _origin.x); x++)
                            {
                                _playerBoard.SetSpaceStatus(x, y, Board.SpaceStates.ship);
                                shipSize++;
                            }
                        }

                        //add current ship size to ships
                        bool found = false;
                        foreach (int[] ship in _ships)
                        {
                            if (ship[1] == shipSize)
                            {
                                ship[0]++;
                                found = true;
                            }
                        }
                        if (!found)
                            _ships.Add([1, shipSize]);

                        //reset origin
                        _origin.x = -1;
                    }
                    break;
                //end selection
                case ConsoleKey.Enter:
                    if (_ships.Count() > 0)
                    {
                        _enemyBoard.GenerateShips(_ships);
                        _curState = GameState.Gameplay;
                    }
                    break;
            }

            //movement
            //get new selected position
            var move = Vector2.GetMovementVector(key);
            Vector2 newSel = new Vector2(_selected.x, _selected.y);
            newSel.Add(move);

            //keep in bounds
            newSel.x = Math.Clamp(newSel.x, 0, _playerBoard.Width - 1);
            newSel.y = Math.Clamp(newSel.y, 0, _playerBoard.Height - 1);

            if (_origin.x >= 0)
            {
                //horizontal
                if (_selected.y == _origin.y && _playerBoard.GetSpaceState(newSel.x, _selected.y) == Board.SpaceStates.empty)
                    _selected.x = newSel.x;

                //vertical
                if (_selected.x == _origin.x && _playerBoard.GetSpaceState(_selected.x, newSel.y) == Board.SpaceStates.empty)
                    _selected.y = newSel.y;
            }
            else
            {
                //move selected
                _selected = newSel;
            }
        }

        private void GameoverUpdate()
        {
            //display who won
            if (_enemyBoard.Won)
                Console.WriteLine("You Won!");
            else
                Console.WriteLine("You Lost...");
            Console.WriteLine();
            PrintPadded("Enemy Board", "Your Board");

            //draw the board
            DrawStrings(CombineStrings(_enemyBoard.GetDrawLines(), _playerBoard.GetDrawLines(), _padding));

            //draw stats
            Console.WriteLine();
            PrintPadded($"Shots Fired: {_enemyBoard.ShotsFired}", $"Shots Fired: {_playerBoard.ShotsFired}");
            PrintPadded($"Hit Rate: {_enemyBoard.HitRate}%", $"Hit Rate: {_playerBoard.HitRate}%");

            Console.ReadKey();
        }

        private void PrintPadded(string strng1, string strng2)
        {
            var output = strng1;
            var width = _playerBoard.WidthString;

            for (int i = output.Length; i < width; i++)
                output += " ";
            output += _padding;
            output += strng2;

            Console.WriteLine(output);
        }

        private void DrawStrings(string[] strings)
        {
            //loop though all the lines
            for (int i = 0; i < strings.Length; i++)
            {
                //if not an inbetween line
                if (i % 2 > 0)
                {
                    //split string when there is a hit or miss character
                    List<string> output = new List<string>();
                    output.Add("");
                    foreach (char c in strings[i].ToCharArray())
                    {
                        if (c == Board.HitChar || c == Board.MissChar)
                        {
                            output.Add(c.ToString());
                            output.Add("");
                            continue;
                        }
                        output[output.Count() - 1] += c.ToString();
                    }
                    //write all the split strings, changing colour when its a miss or a hit
                    foreach (string seperated in output)
                    {
                        if (seperated == Board.HitChar.ToString())
                            Console.ForegroundColor = ConsoleColor.Green;
                        else if (seperated == Board.MissChar.ToString())
                            Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(seperated);
                        Console.ResetColor();
                    }
                    Console.WriteLine();
                    continue;
                }
                //if its an inbetween line, just print the line
                Console.WriteLine(strings[i]);
            }
        }

        private string[] CombineStrings(string[] str1, string[] str2, string padding = "")
        {
            string[] combined = new string[str1.Length];
            for (int i = 0; i < str1.Length; i++)
            {
                combined[i] = str1[i] + padding + str2[i];
            }
            return combined;
        }

        private void GameplayUpdate()
        {
            //draw boards
            PrintPadded("Enemy Board", "Your Board");
            DrawStrings(CombineStrings(_enemyBoard.GetDrawLines(_selected, hidden: true), _playerBoard.GetDrawLines(), _padding));

            var key = Console.ReadKey().Key;

            //movement
            var move = Vector2.GetMovementVector(key);
            _selected.Add(move);
            _selected.x = Math.Clamp(_selected.x, 0, _enemyBoard.Width-1);
            _selected.y = Math.Clamp(_selected.y, 0, _enemyBoard.Height-1);

            //fire
            if (key == ConsoleKey.Spacebar)
            {
                //fire where player has selected
                if (!_enemyBoard.FireAt(_selected.x, _selected.y))
                    return;
                if (_enemyBoard.Won)
                {
                    _curState = GameState.GameOver;
                    return;
                }

                //keep randomly firing at the player until a viable space is found
                while (!_playerBoard.FireAt(Program.RNG.Next(_playerBoard.Width), Program.RNG.Next(_playerBoard.Height)))
                {
                }

                if (_playerBoard.Won)
                {
                    _curState = GameState.GameOver;
                    return;
                }
                return;
            }
        }
    }
}
