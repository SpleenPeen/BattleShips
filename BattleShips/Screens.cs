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
            ShipAllocation,
            Gameplay,
            GameOver
        }

        Board _playerBoard;
        Board _enemyBoard;
        GameState _curState;
        int[] _selected;
        string _padding;
        List<int[]> _ships;
        int[] _origin;

        public GameScreen()
        {
            _origin = [-1, 0];
            _padding = "  ";
            _curState = GameState.ShipAllocation;
            _selected = [0, 0];
            _enemyBoard = new Board(10, 10);
            _playerBoard = new Board(10, 10);
        }

        public void Update()
        {
            switch (_curState)
            {
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

        private void ShipAlloUpdate()
        {
            //draw player board
            Console.WriteLine("Your Board");
            var drawLines = _playerBoard.GetDrawLines(_selected);
            if (_origin[0] >= 0)
            {
                for (int y = Math.Min(_origin[1], _selected[1]); y <= Math.Max(_selected[1], _origin[1]); y++)
                {
                    var curLine = drawLines[_playerBoard.GetYPosStrng(y)].ToCharArray();
                    for (int x = Math.Min(_origin[0], _selected[0]); x <= Math.Max(_selected[0], _origin[0]); x++)
                    {
                        curLine[_playerBoard.GetXPosStrng(x)] = 'O';
                    }
                    drawLines[_playerBoard.GetYPosStrng(y)] = new string(curLine);
                }
            }
            DrawStrings(drawLines);

            var key = Console.ReadKey().Key;

            //theres a lot of repeating code in the switch case for movement, come back to if you have time
            switch (key)
            {
                //setting origin point
                case ConsoleKey.Spacebar:
                    if (_origin[0] < 0)
                    {
                        if (_playerBoard.GetSpaceState(_selected[0], _selected[1]) == Board.SpaceStates.ship)
                            break;
                        _origin[0] = _selected[0];
                        _origin[1] = _selected[1];
                    }
                    else
                    {
                        int shipSize = 0;
                        for (int y = Math.Min(_origin[1], _selected[1]); y <= Math.Max(_selected[1], _origin[1]); y++)
                        {
                            for (int x = Math.Min(_origin[0], _selected[0]); x <= Math.Max(_selected[0], _origin[0]); x++)
                            {
                                _playerBoard.SetSpaceStatus(x, y, Board.SpaceStates.ship);
                                shipSize++;
                            }
                        }
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
                        _origin[0] = -1;
                    }
                    break;
                //horizontal movement
                case ConsoleKey.LeftArrow:
                    if (_origin[0] >= 0)
                    {
                        if (_origin[1] != _selected[1])
                            break;
                        if (_playerBoard.GetSpaceState(Math.Max(0, _selected[0] - 1), _selected[1]) == Board.SpaceStates.ship)
                            break;
                    }
                    _selected[0] = Math.Max(0, _selected[0]-1);
                    break;
                case ConsoleKey.RightArrow:
                    if (_origin[0] >= 0)
                    {
                        if (_origin[1] != _selected[1])
                            break;
                        if (_playerBoard.GetSpaceState(Math.Min(_playerBoard.Width - 1, _selected[0] + 1), _selected[1]) == Board.SpaceStates.ship)
                            break;
                    }
                    _selected[0] = Math.Min(_playerBoard.Width-1, _selected[0]+1);
                    break;
                //vertical movement
                case ConsoleKey.UpArrow:
                    if (_origin[0] >= 0)
                    {
                        if (_origin[0] != _selected[0])
                            break;
                        if (_playerBoard.GetSpaceState(_selected[0], Math.Max(0, _selected[1] - 1)) == Board.SpaceStates.ship)
                            break;
                    }
                    _selected[1] = Math.Max(0, _selected[1]-1);
                    break;
                case ConsoleKey.DownArrow:
                    if (_origin[0] >= 0)
                    {
                        if (_origin[0] != _selected[0])
                            break;
                        if (_playerBoard.GetSpaceState(_selected[0], Math.Min(_playerBoard.Height - 1, _selected[1] + 1)) == Board.SpaceStates.ship)
                            break;
                    }
                    _selected[1] = Math.Min(_playerBoard.Height-1, _selected[1]+1);
                    break;
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
            foreach (string str in strings)
                Console.WriteLine(str);
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

            switch (key)
            {
                //movement
                case ConsoleKey.UpArrow:
                    _selected[1] = Math.Max(_selected[1] - 1, 0);
                    break;
                case ConsoleKey.DownArrow:
                    _selected[1] = Math.Min(_selected[1] + 1, _enemyBoard.Height - 1);
                    break;
                case ConsoleKey.LeftArrow:
                    _selected[0] = Math.Max(_selected[0] - 1, 0);
                    break;
                case ConsoleKey.RightArrow:
                    _selected[0] = Math.Min(_selected[0] + 1, _enemyBoard.Width - 1);
                    break;
                //fire
                case ConsoleKey.Spacebar:
                    //fire where player has selected
                    if (!_enemyBoard.FireAt(_selected[0], _selected[1]))
                        break;
                    if (_enemyBoard.Won)
                    {
                        _curState = GameState.GameOver;
                        break;
                    }

                    //keep randomly firing at the player until a viable space is found
                    while (!_playerBoard.FireAt(Program.RNG.Next(_playerBoard.Width), Program.RNG.Next(_playerBoard.Height)))
                    {
                    }

                    if (_playerBoard.Won)
                    {
                        _curState = GameState.GameOver;
                        break;
                    }
                    break;
            }
        }
    }
}
