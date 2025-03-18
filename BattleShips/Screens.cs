using System;
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
        FileInfo[] _files;

        public History()
        {
            _timer = new Stopwatch();
            _shots = new Stack<Vector2>();
            _play = false;
            _shotsPS = 1f;
            var files = new DirectoryInfo(Program.SavePath).GetFiles().OrderBy(f => f.LastWriteTime).ToList();

            if (files.Count == 0)
                return;

            if (Program.GetGameSave(files[files.Count-1].Name).Ongoing)
                files.RemoveAt(files.Count - 1);

            if (files.Count == 0)
                return;

            _files = new FileInfo[files.Count];
            for (int i = 0; i < files.Count; i++)
                _files[i] = files[files.Count - i - 1];

            string[] names = new string[files.Count];
            for (int i = 0; i < files.Count; i++)
                names[i] = files[files.Count - i -1].Name;

            _selMenu = new Menu(names);

            _curState = State.selection;
        }

        public bool Update(ConsoleKey key)
        {
            //run the current states update
            switch (_curState)
            {
                case State.selection:
                    return SelectionUpdate(key);
                case State.replay:
                    return ReplayUpdate(key);
            }
            return false;
        }

        private bool SelectionUpdate(ConsoleKey key)
        {
            //if ESCAPE pressed, go back to main menu
            if (key == ConsoleKey.Escape)
            {
                Program.SwitchScreen(ScreenState.MainMenu);
                return true;
            }

            //if there were no valid savefiles, return
            if (_selMenu == null)
                return false;

            //update selection menu
            if (_selMenu.UpdateMenu(key))
            {
                //change state and reset variables
                _shots.Clear(); //in case something was previously replayed
                _curState = State.replay;

                //prepare boards for replay
                var selGame = SaveManager.Instance.GetGameSave(_selMenu.SelText);
                _enemyBoard = new Board(selGame.EnemySpaces, selGame.EShipSpaces, selGame.EShipsHit, GeneralUtils.ArrayToLinkedList(selGame.EShots));
                _playerBoard = new Board(selGame.PlayerSpaces, selGame.PShipSpaces, selGame.PShipsHit, GeneralUtils.ArrayToLinkedList(selGame.PShots));
                _enemyBoard.PrepForReplay();
                _playerBoard.PrepForReplay();
                return true;
            }
            return _selMenu.Draw; //draw if has move selection
        }

        private bool ReplayUpdate(ConsoleKey key)
        {
            var draw = false;

            //if ESCAPE pressed, go back to selection state
            if (key == ConsoleKey.Escape)
            {
                _curState = State.selection;
                return true;
            }

            //if SPACE pressed, set play to true
            if (key == ConsoleKey.Spacebar)
                _play = !_play;

            //Adjust shot speed and current shot using the move vector
            var move = Vector2.GetMovementVector(key);
            var curShot = _shotsPS;
            _shotsPS += Math.Max(-move.y * 0.1f, 0.1f); //UP/DOWN adjusts shot speed
            if (curShot != _shotsPS)
                draw = true;
            if (ManualShots(move.x)) //LEFT/RIGHT changes current shot 
                draw = true;

            //if not set to play return, otherwise
            if (!_play)
                return false;

            //reset to paused if the game is over
            if (_playerBoard.Won || _enemyBoard.Won)
            {
                _play = false;
                return true;
            }
            //restart the timer if its not running
            if (!_timer.IsRunning)
                _timer.Restart();

            //if timer has crossed the interval to fire, draw next shot and restart timer
            if ((float)_timer.ElapsedMilliseconds / 1000 >= (1 / _shotsPS))
            {
                _timer.Restart();
                NextShot();
                return true;
            }
            return draw;
        }

        private bool ManualShots(int xMove)
        {
            //if left arrow has been pressed, undo last shot
            if (xMove < 0)
            {
                //if there are no shots to undo return
                if (_shots.Count == 0)
                    return false;

                //otherwise undo last shot
                PreviousShot();
                return true;
            }
            //if right arrow pressed
            else if (xMove > 0)
            {
                //return if no more shots left
                if (_enemyBoard.Won || _playerBoard.Won)
                    return false;

                //otherwise shoot next shot
                NextShot();
                return true;
            }
            return false;
        }

        private void NextShot()
        {
            //fire at appropriate board, depending on turn order
            if (_shots.Count % 2 == 0)
                _shots.Push(_enemyBoard.FireReplay());
            else
                _shots.Push(_playerBoard.FireReplay());
        }

        private void PreviousShot()
        {
            //undo shot from appropriate board, depending on turn order
            if ((_shots.Count - 1) % 2 == 0)
                _enemyBoard.UndoShot(_shots.Pop());
            else
                _playerBoard.UndoShot(_shots.Pop());
        }

        public void Draw()
        {
            //draw screen depending on state
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
            string padding = "  "; //space inbetween boards

            //draw instructions
            Console.WriteLine("Space: play/pause replay.");
            Console.WriteLine("Left/Right: move back/forward 1 shot.");
            Console.WriteLine("Up/Down: alter playback speed.");
            Console.WriteLine("Escape: Go back to selection screen.");
            Console.WriteLine();

            //draw board tags
            GeneralUtils.WritePadded("Enemy Board", _enemyBoard.WidthString + padding.Length);
            Console.WriteLine("Your Board");

            //draw boards
            Board.DrawStrings(Board.CombineStrings(_enemyBoard.GetDrawLines(), _playerBoard.GetDrawLines(), padding));
            Console.WriteLine();

            //draw play status and shot speed
            Console.Write("Status: ");
            if (_play)
                Console.WriteLine("Playing");
            else
                Console.WriteLine("Paused");
            Console.WriteLine("ShotsPerSecond: " + Math.Round(_shotsPS, 1));
        }

        private void DrawSelection()
        {
            int padding = 10; //padding between file stats

            //draw instructions
            Console.WriteLine("Press Escape to return to main menu.");
            Console.WriteLine();

            //if no valid files were found, draw relevant message and return
            if (_selMenu == null)
            {
                Console.WriteLine("You currently have no past games!");
                return;
            }

            //draw descriptors
            Console.ForegroundColor = ConsoleColor.Gray;
            GeneralUtils.WritePadded("", padding*2);
            GeneralUtils.WritePadded("State", padding);
            GeneralUtils.WritePadded("Diff", padding);
            GeneralUtils.WritePadded("Timer", padding);
            GeneralUtils.WritePadded("Date", padding, true);
            Console.ResetColor();

            //draw menu
            _selMenu.DrawMenu();

            //draw file stats
            for (int i = 0; i < _files.Count(); i++)
            {
                //change draw colour (depending on if the file is selected)
                var curCol = _selMenu.DefCol;
                if (i == _selMenu.Selected)
                    curCol = _selMenu.SelCol;

                //deserialise save file and place cursor in right spot
                var save = SaveManager.Instance.GetGameSave(_selMenu.GetOptString(i));
                if (save == null)
                    continue;
                Console.CursorLeft = padding*2;
                Console.CursorTop = i+3;

                //draw win/loss
                var won = save.EShipsHit == save.EShipSpaces;
                if (won)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGreen;
                    GeneralUtils.WritePadded("Won", padding);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    GeneralUtils.WritePadded("Lost", padding);
                }

                var diffString = "Easy";
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                if (_selMenu.Selected == i)
                    Console.ForegroundColor = ConsoleColor.Green;
                if (save.Difficulty == 1)
                {
                    diffString = "Medium";
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    if (_selMenu.Selected == i)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                }
                else if (save.Difficulty == 2)
                {
                    diffString = "Hard";
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    if (_selMenu.Selected == i)
                        Console.ForegroundColor = ConsoleColor.Red;
                }
                Program.WritePadded(diffString, padding);
                Console.ForegroundColor = curCol;
                Program.WritePadded(Math.Round((float)save.Timer/1000, 2).ToString() + "s", padding);
                Program.WritePadded(_files[i].LastWriteTime.ToLongDateString(), padding);
            }
            Console.ResetColor();
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

                if (!Program.GetLatestSave().Ongoing)
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
            Console.WriteLine("Use UP/DOWN or W/S keys to navigate menus.");
            Console.WriteLine("Press SPACE/ENTER to confirm selection in menus.");
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
                if (!Program.GetLatestSave().Ongoing)
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
            var save = Program.GetLatestSave();

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
