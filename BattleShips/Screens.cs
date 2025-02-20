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
        List<int> _ships;

        public GameScreen()
        {
            _padding = "  ";
            _curState = GameState.Gameplay;
            _selected = [0, 0];
            _enemyBoard = new Board(10, 10);
            _playerBoard = new Board(10, 10);
            List<int[]> ships = new List<int[]>();
            ships.Add([2, 1]);
            ships.Add([2, 2]);
            ships.Add([3, 3]);
            _enemyBoard.GenerateShips(ships);
            _playerBoard.GenerateShips(ships); 
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
            //do this next
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
