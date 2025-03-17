﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BattleShips
{
    internal class History
    {
        enum State
        {
            selection,
            replay
        }

        State _curState;
        Menu _selMenu;
        Board _enemyBoard;
        Board _playerBoard;


        bool _play;
        float _shotsPS;
        Stopwatch _timer;
        Stack<Vector2> _shots;

        public History()
        {
            _timer = new Stopwatch();
            _shots = new Stack<Vector2>();
            _play = false;
            _shotsPS = 1f;
            var files = Directory.GetFiles(Program.SavePath).ToList();

            if (Program.GetGameSave().Ongoing)
                files.RemoveAt(files.Count - 1);

            for (int i = 0; i < files.Count; i++)
            {
                files[i] = files[i].Substring(Program.SavePath.Length, files[i].Length - Program.SavePath.Length - 4);
            }

            if (files.Count == 0)
                return;

            _selMenu = new Menu(files.ToArray());

            _curState = State.selection;
        }

        public void Update(ConsoleKey key)
        {
            switch (_curState)
            {
                case State.selection:
                    SelectionUpdate(key);
                    break;
                case State.replay:
                    ReplayUpdate(key);
                    break;
            }
        }

        private void SelectionUpdate(ConsoleKey key)
        {
            if (_selMenu.UpdateMenu(key))
            {
                _shots.Clear();
                _curState = State.replay;
                var selGame = Program.GetGameSave((short)_selMenu.Selected);
                _enemyBoard = new Board(selGame.EnemySpaces, selGame.EShipSpaces, selGame.EShipsHit, Program.ArrayToLinkedList(selGame.EShots));
                _playerBoard = new Board(selGame.PlayerSpaces, selGame.PShipSpaces, selGame.PShipsHit, Program.ArrayToLinkedList(selGame.PShots));

                _enemyBoard.PrepForReplay();
                _playerBoard.PrepForReplay();
            }
            else if (key == ConsoleKey.Escape)
                Program.SwitchScreen(ScreenState.MainMenu);
        }

        private void ReplayUpdate(ConsoleKey key)
        {
            if (key == ConsoleKey.Escape)
            {
                _curState = State.selection;
                return;
            }

            if (key == ConsoleKey.Spacebar)
                _play = !_play;

            var move = Vector2.GetMovementVector(key);
            _shotsPS += -move.y * 0.1f;
            ManualShots(move.x);

            if (_play)
            {
                if (_playerBoard.Won || _enemyBoard.Won)
                {
                    _play = false;
                    return;
                }
                if (!_timer.IsRunning)
                    _timer.Restart();
                if ((float)_timer.ElapsedMilliseconds/1000 >= (1/_shotsPS))
                {
                    Program.DrawFrame = true;
                    _timer.Restart();
                    NextShot();
                }
            }
        }

        private void ManualShots(int xMove)
        {
            if (xMove < 0)
            {
                if (_shots.Count == 0)
                    return;
                PreviousShot();
            }
            else if (xMove > 0)
            {
                if (_enemyBoard.Won || _playerBoard.Won)
                    return;
                NextShot();
            }
        }

        private void NextShot()
        {
            if (_shots.Count % 2 == 0)
            {
                _shots.Push(_enemyBoard.FireReplay());
            }
            else
            {
                _shots.Push(_playerBoard.FireReplay());
            }
        }

        private void PreviousShot()
        {
            if ((_shots.Count - 1) % 2 == 0)
                _enemyBoard.UndoShot(_shots.Pop());
            else
                _playerBoard.UndoShot(_shots.Pop());
        }

        public void Draw()
        {
            switch (_curState)
            {
                case State.selection:
                    DrawSelection();
                    break;
                case State.replay:
                    DrawReplay();
                    break;
            }
        }

        private void DrawReplay()
        {
            Console.WriteLine("Space: play/pause replay.");
            Console.WriteLine("Left/Right: move back/forward 1 shot.");
            Console.WriteLine("Up/Down: alter playback speed.");
            Console.WriteLine("Escape: Go back to selection screen.");
            Console.WriteLine();
            Program.PrintPadded("Enemy Board", "Your Board", _enemyBoard.WidthString + 2);
            Board.DrawStrings(Board.CombineStrings(_enemyBoard.GetDrawLines(), _playerBoard.GetDrawLines(), "  "));
            Console.WriteLine();
            Console.Write("Status: ");
            if (_play)
                Console.WriteLine("Playing");
            else
                Console.WriteLine("Paused");
            Console.WriteLine("ShotsPerSecond: " + Math.Round(_shotsPS, 1));
        }

        private void DrawSelection()
        {
            Console.WriteLine("Press Escape to return to main menu.");
            Console.WriteLine();
            _selMenu.DrawMenu();
        }
    }

    internal class MainMenu
    {
        Menu _mainMenu;
        bool _continue;

        public MainMenu()
        {
            List<String> opts;

            _continue = false;
            _mainMenu = new Menu(Opts, outline: true, centred: true);
        }

        private string[] Opts
        {
            get
            {
                List<string> opts = ["New Game", "History", "Exit"];

                if (!Directory.Exists(Program.SavePath))
                    return opts.ToArray();

                if (Directory.GetFiles(Program.SavePath).Length == 0)
                    return opts.ToArray();

                if (!Program.GetGameSave().Ongoing)
                    return opts.ToArray();

                opts.Insert(0, "Continue");

                _continue = true;
                return opts.ToArray();
            }
        }

        public void Update(ConsoleKey key)
        {
            if (_mainMenu.UpdateMenu(key))
            {
                var sel = _mainMenu.Selected;
                if (!_continue)
                    sel++;

                switch (sel)
                {
                    case 0:
                        Program.SwitchScreen(ScreenState.Game);
                        Program.LoadLatestSave();
                        break;
                    case 1:
                        Program.SwitchScreen(ScreenState.Game);
                        break;
                    case 2:
                        Program.SwitchScreen(ScreenState.History);
                        break;
                    case 3:
                        Environment.Exit(0);
                        break;
                }
            }
        }

        public void Draw()
        {
            _mainMenu.DrawMenu();
        }
    }

    internal class GameScreen
    {
        enum GameState
        {
            BoardAllocation,
            ShipAllocation,
            DifficultyAllocation,
            Gameplay,
            GameOver
        }

        Board _playerBoard;
        Board _enemyBoard;
        GameState _curState;
        Vector2 _selected;
        string _padding;
        List<Vector2> _ships;
        Vector2 _origin;
        Menu _diffAloMenu;
        List<Vector2> _shotTargets;
        List<Vector2> _checkAround;
        bool _paused;
        Menu _pauseMenu;
        BetterStopwatch _timer;
        
        public GameScreen()
        {
            _timer = new BetterStopwatch(0);
            _shotTargets = new List<Vector2>();
            _checkAround = new List<Vector2>();
            _origin = new Vector2(-1, 0);
            _padding = "  ";
            _curState = GameState.BoardAllocation;
            _selected = new Vector2();
            _ships = new List<Vector2>();
            _playerBoard = new Board(10, 10);
            _enemyBoard = new Board();
            _paused = false;
            _diffAloMenu = new Menu(
                options: ["Easy", "Medium", "Hard"], 
                desc: "Select your difficulty for this game.", 
                selected: 1, 
                width: 28,
                outline: true,
                centred: true);
            _pauseMenu = new Menu(
                options: ["Resume", "Main Menu", "Save and Exit"],
                outline: true,
                centred: true
                );
        }

        public void Update(ConsoleKey key)
        {
            //check whether to pause
            if (key == ConsoleKey.Escape)
                _paused = !_paused;
            //if not pause update current screen
            if (!_paused)
            {
                switch (_curState)
                {
                    case GameState.BoardAllocation:
                        BoardAlloUpdate(key);
                        break;
                    case GameState.ShipAllocation:
                        ShipAlloUpdate(key);
                        break;
                    case GameState.DifficultyAllocation:
                        DifficultyAlloUpdate(key);
                        break;
                    case GameState.Gameplay:
                        GameplayUpdate(key);
                        break;
                }
            }
            //otherwise update the pause menu
            else
            {
                _timer.Stop();
                if (_pauseMenu.UpdateMenu(key))
                {
                    switch (_pauseMenu.Selected)
                    {
                        case 0:
                            _paused = false;
                            break;
                        case 1:
                            if (Savable)
                                SaveGame();
                            Program.SwitchScreen(ScreenState.MainMenu);
                            break;
                        case 2:
                            if (Savable)
                                SaveGame();
                            Environment.Exit(0);
                            break;
                    }
                }
            }
        }

        private bool Savable
        {
            get
            {
                if (_curState == GameState.Gameplay || _curState == GameState.GameOver)
                    return true;
                return false;
            }
        }

        public void Draw()
        {
            switch (_curState)
            {
                case GameState.BoardAllocation:
                    BoardAlloDraw();
                    break;
                case GameState.ShipAllocation:
                    ShipAlloDraw();
                    break;
                case GameState.DifficultyAllocation:
                    DifficultyAlloDraw();
                    break;
                case GameState.Gameplay:
                    GameplayDraw();
                    break;
                case GameState.GameOver:
                    GameOverDraw();
                    break;
            }

            if (_paused)
                _pauseMenu.DrawMenu();
        }

        private void SaveGame()
        {
            var curSave = new GameSave();
            //player board
            curSave.PlayerSpaces = _playerBoard.SpacesNum;
            curSave.PShipSpaces = _playerBoard.ShipSpaces;
            curSave.PShipsHit = _playerBoard.ShipsHit;
            curSave.PShots = _playerBoard.Shots.ToArray();

            //enemy board
            curSave.EnemySpaces = _enemyBoard.SpacesNum;
            curSave.EShipSpaces = _enemyBoard.ShipSpaces;
            curSave.EShipsHit = _enemyBoard.ShipsHit;
            curSave.EShots = _enemyBoard.Shots.ToArray();

            //game state
            curSave.Timer = _timer.ElapsedMilliseconds;
            curSave.Difficulty = _diffAloMenu.Selected;
            curSave.ShotTargets = _shotTargets;
            curSave.CheckAround = _checkAround;

            string json = JsonSerializer.Serialize(curSave);
            int save = 0;

            if (!Directory.Exists(Program.SavePath))
                Directory.CreateDirectory(Program.SavePath);
            else if (Directory.GetFiles(Program.SavePath).Length > 0)
            {
                save = Program.GetLatestSaveNum();
                if (!Program.GetGameSave().Ongoing)
                {
                    save++;
                    Program.ClearOldSaves();
                }
            }
            
            File.WriteAllText(@$"{Program.SavePath}{Program.SaveName}{save}.txt", json);
        }

        public void LoadLatestSave()
        {
            _curState = GameState.Gameplay;
            var save = Program.GetGameSave();

            _playerBoard = new Board(save.PlayerSpaces, save.PShipSpaces, save.PShipsHit, Program.ArrayToLinkedList(save.PShots));
            _enemyBoard = new Board(save.EnemySpaces, save.EShipSpaces, save.EShipsHit, Program.ArrayToLinkedList(save.EShots));
            _checkAround = save.CheckAround;
            _shotTargets = save.ShotTargets;
            _timer = new BetterStopwatch(save.Timer);
            _diffAloMenu.Selected = save.Difficulty;
        }

        private void BoardAlloUpdate(ConsoleKey key)
        {
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

        private void BoardAlloDraw()
        {
            Console.WriteLine("Use arrow keys or WASD to scale the size of your board.");
            Console.WriteLine("Press enter to confirm size.");
            Console.WriteLine("You can press escape at anytime to pause.");
            Console.WriteLine();
            Board.DrawStrings(_playerBoard.GetDrawLines());
        }

        private void DifficultyAlloUpdate(ConsoleKey key)
        {
            if (_diffAloMenu.UpdateMenu(key))
            {
                PrepareShotTargets();
                _timer.Start();
                _curState = GameState.Gameplay;
            }
        }

        private void DifficultyAlloDraw()
        {
            GameplayDraw();
            _diffAloMenu.DrawMenu();
        }

        private void PrepareShotTargets()
        {
            if (_diffAloMenu.Selected == 0 || _diffAloMenu.Selected == 1)
                AllSpacesTargets();
            else
                ShipSpacesTargets();
        }

        private void AllSpacesTargets()
        {
            for (int y = 0; y < _playerBoard.Height; y++)
            {
                for (int x = 0; x < _playerBoard.Width; x++)
                {
                    _shotTargets.Add(new Vector2(x, y));
                }
            }
        }

        private void ShipSpacesTargets()
        {
            for (int y = 0; y < _playerBoard.Height; y++)
            {
                for (int x = 0; x < _playerBoard.Width; x++)
                {
                    if (_playerBoard.GetSpaceState(x, y) == Board.SpaceStates.ship)
                        _shotTargets.Add(new Vector2(x, y));
                }
            }
        }

        private void ShipAlloUpdate(ConsoleKey key)
        {
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
                        foreach (Vector2 ship in _ships)
                        {
                            if (ship.y == shipSize)
                            {
                                ship.x++;
                                found = true;
                            }
                        }
                        if (!found)
                            _ships.Add(new Vector2(1, shipSize));

                        //reset origin
                        _origin.x = -1;
                    }
                    break;
                //end selection
                case ConsoleKey.Enter:
                    if (_ships.Count() > 0)
                    {
                        _enemyBoard.GenerateShips(_ships);
                        _curState = GameState.DifficultyAllocation;
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

        private void ShipAlloDraw()
        {
            //draw instructions
            Console.WriteLine("Use arrow keys or WASD to move your cursor.");
            Console.WriteLine("Press space to begin and end ship placement.");
            Console.WriteLine("Press enter to confirm your placements.");
            Console.WriteLine();

            //draw player board
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
            Board.DrawStrings(drawLines);
        }

        private void GameOverDraw()
        {
            //display who won
            if (_enemyBoard.Won)
                Console.WriteLine("You Won!");
            else
                Console.WriteLine("You Lost...");
            Console.WriteLine();
            Program.PrintPadded("Enemy Board", "Your Board", _enemyBoard.WidthString + _padding.Length);

            //draw the board
            Board.DrawStrings(Board.CombineStrings(_enemyBoard.GetDrawLines(), _playerBoard.GetDrawLines(), _padding));

            //draw stats
            Console.WriteLine();
            Program.PrintPadded($"Shots Fired: {_enemyBoard.ShotsFired}", $"Shots Fired: {_playerBoard.ShotsFired}", _enemyBoard.WidthString + _padding.Length);
            Program.PrintPadded($"Hit Rate: {_enemyBoard.HitRate}%", $"Hit Rate: {_playerBoard.HitRate}%", _enemyBoard.WidthString + _padding.Length);
            Console.WriteLine();
            Console.WriteLine($"Game Duration: {Math.Round((float)_timer.ElapsedMilliseconds/1000, 2)} seconds");
        }

        private void GameplayUpdate(ConsoleKey key)
        {
            if (!_timer.IsRunning)
                _timer.Start();

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
                    _timer.Stop();
                    _curState = GameState.GameOver;
                    return;
                }

                //keep randomly firing at the player until a viable space is found
                FireWithDifficulty();

                if (_playerBoard.Won)
                {
                    _timer.Stop();
                    _curState = GameState.GameOver;
                    return;
                }
                return;
            }
        }

        private void GameplayDraw()
        {
            //draw instructions
            Console.WriteLine("Use arrow keys or WASD to move cursor.");
            Console.WriteLine("Press space to fire at cursor location.");
            Console.WriteLine();

            //draw boards
            Program.PrintPadded("Enemy Board", "Your Board", _enemyBoard.WidthString + _padding.Length);
            Board.DrawStrings(Board.CombineStrings(_enemyBoard.GetDrawLines(_selected, hidden: true), _playerBoard.GetDrawLines(), _padding), _enemyBoard, _selected);
        }

        private void FireWithDifficulty()
        {
            switch (_diffAloMenu.Selected)
            {
                case 0:
                    FireEasy();
                    break;
                case 1:
                    FireMedium();
                    break;
                case 2:
                    FireHard(Math.Max(_playerBoard.Width, _playerBoard.Height)/2);
                    break;
            }
        }

        private void FireHard(int radius = 5)
        {
            if (_checkAround.Count() > 0)
            {
                FireAtCheckAroundSpaces();
                return;
            }

            while(true)
            {
                var pos = new Vector2(_shotTargets[Program.RNG.Next(_shotTargets.Count())]);
                pos.x += Program.RNG.Next(-radius, radius);
                pos.y += Program.RNG.Next(-radius, radius);
                if (_playerBoard.FireAt(pos.x, pos.y))
                {
                    if (_playerBoard.GetSpaceState(pos.x, pos.y) == Board.SpaceStates.hit)
                    {
                        AddCheckSpaces(pos);
                        _shotTargets.RemoveAll(v => v.Equals(pos));
                    }
                    break;
                }
            }
        }

        private void FireMedium()
        {
            if (_checkAround.Count() > 0)
            {
                FireAtCheckAroundSpaces();
                return;
            }

            var firedAt = FireEasy();
            if (_playerBoard.GetSpaceState(firedAt.x, firedAt.y) == Board.SpaceStates.hit)
                AddCheckSpaces(firedAt);
        }

        private void FireAtCheckAroundSpaces()
        {
            int ind;
            Vector2 pos;
            ind = Program.RNG.Next(_checkAround.Count());
            pos = new Vector2(_checkAround[ind].x, _checkAround[ind].y);
            _playerBoard.FireAt(pos.x, pos.y);
            if (_playerBoard.GetSpaceState(pos.x, pos.y) == Board.SpaceStates.hit)
                AddCheckSpaces(pos);
            _checkAround.RemoveAll(v => v.Equals(pos));
            _shotTargets.RemoveAll(v => v.Equals(pos));
        }

        private void AddCheckSpaces(Vector2 spaceHit)
        {
            Vector2[] spacesToCheck =
                [
                    new Vector2(spaceHit.x-1, spaceHit.y),
                    new Vector2(spaceHit.x+1, spaceHit.y),
                    new Vector2(spaceHit.x, spaceHit.y-1),
                    new Vector2(spaceHit.x, spaceHit.y+1)
                ];

            foreach (var space in spacesToCheck)
            {
                var state = _playerBoard.GetSpaceState(space.x, space.y);
                if (state == Board.SpaceStates.empty || state == Board.SpaceStates.ship)
                    _checkAround.Add(space);
            }
        }

        private Vector2 FireEasy()
        {
            var ind = Program.RNG.Next(_shotTargets.Count());
            Vector2 posFired = _shotTargets[ind];
            _playerBoard.FireAt(posFired.x, posFired.y);
            _shotTargets.RemoveAt(ind);
            return posFired;
        }
    }
}
