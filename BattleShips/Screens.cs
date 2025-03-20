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
            //initialise variables
            _timer = new Stopwatch();
            _shots = new Stack<Vector2>();
            _play = false;
            _shotsPS = 1f;

            //get all save files, ordered by creation date
            var files = SaveManager.Instance.FilesByDate.ToList();

            //if there are no files return
            if (files.Count == 0)
                return;

            //if latest file is ongoing, remove it
            if (SaveManager.Instance.IsOngoing(SaveManager.Instance.GetGameSave(files[0].Name)))
                files.RemoveAt(0);

            //check if at 0 again
            if (files.Count == 0)
                return;

            _files = files.ToArray();

            //get all file names and send them into menu options
            string[] names = new string[files.Count];
            for (int i = 0; i < files.Count; i++)
                names[i] = files[i].Name;

            _selMenu = new Menu(names);

            //set state to selection screen
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
            {
                _play = !_play;
                draw = true;
            }

            //Adjust shot speed and current shot using the move vector
            var move = Vector2.GetMovementVector(key);
            var curShot = _shotsPS;
            _shotsPS += -move.y * 0.1f; //UP/DOWN adjusts shot speed
            _shotsPS = Math.Max(0.1f, _shotsPS); //clamp speed
            if (!curShot.Equals(_shotsPS))
                draw = true;
            if (ManualShots(move.x)) //LEFT/RIGHT changes current shot 
                draw = true;

            //if not set to play return, otherwise
            if (!_play)
                return draw;

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
                Console.WriteLine("You currently have no past games");
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
                GeneralUtils.WritePadded(diffString, padding);
                Console.ForegroundColor = curCol;
                GeneralUtils.WritePadded(Math.Round((float)save.Timer/1000, 2).ToString() + "s", padding);
                GeneralUtils.WritePadded(_files[i].LastWriteTime.ToLongDateString(), padding);
            }
            Console.ResetColor();
        }
    }

    internal class MainMenu
    {
        Menu _mainMenu;

        public MainMenu()
        {
            //set initial variables
            _mainMenu = new Menu(Opts, outline: true, centred: true);
        }

        private string[] Opts
        {
            get
            {
                //set basic options
                List<string> opts = ["New Game", "History", "Exit"];

                //check if there are any save files
                if (SaveManager.Instance.GetOngoingSave == null)
                    return opts.ToArray();

                //otherwise add a continue option and return options
                opts.Insert(0, "Continue");
                return opts.ToArray();
            }
        }

        public bool Update(ConsoleKey key)
        {
            //if one of the main menu options was selected
            if (_mainMenu.UpdateMenu(key))
            {
                //adjust selected based on whether continue is one of the options
                var sel = _mainMenu.Selected;
                if (_mainMenu.Count != 4)
                    sel++;

                //switch to correct screen, or exit, based on selected option
                switch (sel)
                {
                    case 0:
                        Program.SwitchScreen(ScreenState.Game, true);
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
                return true;
            }

            //draw menu when switching between options
            return _mainMenu.Draw;
        }

        public void Draw()
        {
            //draw instruction and menu
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
            //initialise variables
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

        public bool Update(ConsoleKey key)
        {
            //check whether to pause
            var draw = false;
            if (key == ConsoleKey.Escape)
            {
                _paused = !_paused;
                draw = true;
            }
            //if not pause update current screen
            if (!_paused)
            {
                switch (_curState)
                {
                    case GameState.BoardAllocation:
                        return BoardAlloUpdate(key);
                    case GameState.ShipAllocation:
                        return ShipAlloUpdate(key);
                    case GameState.DifficultyAllocation:
                        return DifficultyAlloUpdate(key);
                    case GameState.Gameplay:
                        return GameplayUpdate(key);
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
                if (draw)
                    return true;
                return _pauseMenu.Draw;
            }
            return false;
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
            //call current screens draw method, and draw the pause menu (if games paused)
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
            //send all relevant variables to savemanager
            SaveManager.Instance.SaveFile(
            _playerBoard.SpacesNum,
            _playerBoard.ShipSpaces,
            _playerBoard.ShipsHit,
            _playerBoard.Shots.ToArray(),
            _enemyBoard.SpacesNum,
            _enemyBoard.ShipSpaces,
            _enemyBoard.ShipsHit,
            _enemyBoard.Shots.ToArray(),
            _timer.ElapsedMilliseconds,
            _diffAloMenu.Selected,
            _shotTargets,
           _checkAround
            );

            SaveManager.Instance.ClearOldSaves();
        }

        public void LoadSave(GameSave save)
        {
            //set state to gameplay and load all relevant variables
            _curState = GameState.Gameplay;
            _playerBoard = new Board(save.PlayerSpaces, save.PShipSpaces, save.PShipsHit, GeneralUtils.ArrayToLinkedList(save.PShots));
            _enemyBoard = new Board(save.EnemySpaces, save.EShipSpaces, save.EShipsHit, GeneralUtils.ArrayToLinkedList(save.EShots));
            _checkAround = save.CheckAround;
            _shotTargets = save.ShotTargets;
            _timer = new BetterStopwatch(save.Timer);
            _diffAloMenu.Selected = save.Difficulty;
        }

        private bool BoardAlloUpdate(ConsoleKey key)
        {
            var newSize = Vector2.GetMovementVector(key);
            newSize.x = Math.Max(1, _playerBoard.Width + newSize.x);
            newSize.y = Math.Max(1, _playerBoard.Height + newSize.y);

            var draw = false;
            if (newSize.x != _playerBoard.Width || newSize.y != _playerBoard.Height)
                draw = true;

            _playerBoard.SetSize(newSize.x, newSize.y);

            if (key == ConsoleKey.Enter)
            {
                _enemyBoard.SetSize(_playerBoard.Width, _playerBoard.Height);
                _curState = GameState.ShipAllocation;
                draw = true;
            }
            return draw;
        }

        private void BoardAlloDraw()
        {
            //draw instructions and player board
            Console.WriteLine("Use arrow keys or WASD to scale the size of your board.");
            Console.WriteLine("Press enter to confirm size.");
            Console.WriteLine("You can press escape at anytime to pause.");
            Console.WriteLine();
            Board.DrawStrings(_playerBoard.GetDrawLines());
        }

        private bool DifficultyAlloUpdate(ConsoleKey key)
        {
            //update difficulty menu
            if (_diffAloMenu.UpdateMenu(key))
            {
                PrepareShotTargets();
                _timer.Start();
                _curState = GameState.Gameplay;
            }
            return _diffAloMenu.Draw;
        }

        private void DifficultyAlloDraw()
        {
            //draw gameplay screen and the difficulty menu on top
            GameplayDraw();
            _diffAloMenu.DrawMenu();
        }

        private void PrepareShotTargets()
        {
            //get appropriate shot targets, depending on difficulty
            if (_diffAloMenu.Selected == 0 || _diffAloMenu.Selected == 1)
                AllSpacesTargets();
            else
                ShipSpacesTargets();
        }

        private void AllSpacesTargets()
        {
            //set all spaces as targets
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
            //set shit spaces as targets
            for (int y = 0; y < _playerBoard.Height; y++)
            {
                for (int x = 0; x < _playerBoard.Width; x++)
                {
                    if (_playerBoard.GetSpaceState(x, y) == Board.SpaceStates.ship)
                        _shotTargets.Add(new Vector2(x, y));
                }
            }
        }

        private bool ShipAlloUpdate(ConsoleKey key)
        {
            bool draw = false;
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
                        draw = true;
                    }
                    else
                    {
                        draw = true;
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
                        draw = true;
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
                {
                    if (_selected.x != newSel.x)
                        draw = true;
                    _selected.x = newSel.x;
                }

                //vertical
                if (_selected.x == _origin.x && _playerBoard.GetSpaceState(_selected.x, newSel.y) == Board.SpaceStates.empty)
                {
                    if (_selected.y != newSel.y)
                        draw = true;
                    _selected.y = newSel.y;
                }
            }
            else
            {
                //move selected
                if (!_selected.Equals(newSel))
                    draw = true;
                _selected = newSel;
            }
            return draw;
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
            //draw altered board
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

            //label boards
            GeneralUtils.WritePadded("Enemy Board", Padding);
            Console.WriteLine("Your Board");

            //draw the board
            Board.DrawStrings(Board.CombineStrings(_enemyBoard.GetDrawLines(), _playerBoard.GetDrawLines(), _padding));

            //draw stats
            Console.WriteLine();
            GeneralUtils.WritePadded($"Shots Fired: {_enemyBoard.ShotsFired}", Padding);
            Console.WriteLine($"Shots Fired: {_playerBoard.ShotsFired}");
            GeneralUtils.WritePadded($"Hit Rate: {_enemyBoard.HitRate}%", Padding);
            Console.WriteLine($"Hit Rate: {_playerBoard.HitRate}%");
            Console.WriteLine();
            Console.WriteLine($"Game Duration: {Math.Round((float)_timer.ElapsedMilliseconds/1000, 2)} seconds");
        }

        private bool GameplayUpdate(ConsoleKey key)
        {
            var draw = false;

            //start timer if not already running
            if (!_timer.IsRunning)
                _timer.Start();

            //movement
            var move = Vector2.GetMovementVector(key);
            var curSel = new Vector2(_selected);
            _selected.Add(move);
            _selected.x = Math.Clamp(_selected.x, 0, _enemyBoard.Width-1);
            _selected.y = Math.Clamp(_selected.y, 0, _enemyBoard.Height-1);
            if (!_selected.Equals(curSel))
                draw = true;

            //fire
            if (key == ConsoleKey.Spacebar)
            {
                //fire where player has selected
                if (!_enemyBoard.FireAt(_selected.x, _selected.y))
                    return draw;

                //check if player won
                if (_enemyBoard.Won)
                {
                    _timer.Stop();
                    _curState = GameState.GameOver;
                    return true;
                }

                //fire at player board
                FireWithDifficulty();
                draw = true;

                //check if enemy won
                if (_playerBoard.Won)
                {
                    _timer.Stop();
                    _curState = GameState.GameOver;
                    return true;
                }
            }
            return draw;
        }

        private void GameplayDraw()
        {
            //draw instructions
            Console.WriteLine("Use arrow keys or WASD to move cursor.");
            Console.WriteLine("Press space to fire at cursor location.");
            Console.WriteLine();

            //draw boards
            GeneralUtils.WritePadded("Enemy Board", Padding);
            Console.WriteLine("Your Board");
            Board.DrawStrings(Board.CombineStrings(_enemyBoard.GetDrawLines(_selected, hidden: true), _playerBoard.GetDrawLines(), _padding), _enemyBoard, _selected);
        }

        private void FireWithDifficulty()
        {
            //choose which method to run, depending on difficulty
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
            //if there are check around spaces, fire at them
            if (_checkAround.Count() > 0)
            {
                FireAtCheckAroundSpaces();
                return;
            }

            //if there aren't, fire a shot around one of the ship spaces
            while(true)
            {
                //get a random ship position
                var pos = new Vector2(_shotTargets[Program.RNG.Next(_shotTargets.Count())]);

                //add a random amount to the x and y axis, depending on the provided radius
                pos.x += Program.RNG.Next(-radius, radius);
                pos.y += Program.RNG.Next(-radius, radius);

                //after the enemy makes a valid shot
                if (_playerBoard.FireAt(pos.x, pos.y))
                {
                    //check if the hit was a ship
                    if (_playerBoard.GetSpaceState(pos.x, pos.y) == Board.SpaceStates.hit)
                    {
                        //if so, add check around spaces and remove current shot from targets
                        AddCheckSpaces(pos);
                        _shotTargets.RemoveAll(v => v.Equals(pos));
                    }
                    //after a successful shot break the loop
                    break;
                }
            }
        }

        private void FireMedium()
        {
            //fire at check around spaces, if there are an
            if (_checkAround.Count() > 0)
            {
                FireAtCheckAroundSpaces();
                return;
            }

            //fire at a random space, and if hit add check around spaces
            var firedAt = FireEasy();
            if (_playerBoard.GetSpaceState(firedAt.x, firedAt.y) == Board.SpaceStates.hit)
                AddCheckSpaces(firedAt);
        }

        private void FireAtCheckAroundSpaces()
        {
            //get a random position from check around spaces
            Vector2 pos;
            pos = new Vector2(_checkAround[Program.RNG.Next(_checkAround.Count())]);

            //fire at the selected pos, and add check around spaces if hits a ship
            _playerBoard.FireAt(pos.x, pos.y);
            if (_playerBoard.GetSpaceState(pos.x, pos.y) == Board.SpaceStates.hit)
                AddCheckSpaces(pos);

            //remove shot position from targets and check around spaces
            _checkAround.RemoveAll(v => v.Equals(pos));
            _shotTargets.RemoveAll(v => v.Equals(pos));
        }

        private void AddCheckSpaces(Vector2 spaceHit)
        {
            //get spaces around a hit space
            Vector2[] spacesToCheck =
                [
                    new Vector2(spaceHit.x-1, spaceHit.y),
                    new Vector2(spaceHit.x+1, spaceHit.y),
                    new Vector2(spaceHit.x, spaceHit.y-1),
                    new Vector2(spaceHit.x, spaceHit.y+1)
                ];

            //check if space is empty or a ship, and if so add them to check around spaces
            foreach (var space in spacesToCheck)
            {
                var state = _playerBoard.GetSpaceState(space.x, space.y);
                if (state == Board.SpaceStates.empty || state == Board.SpaceStates.ship)
                    _checkAround.Add(space);
            }
        }

        private Vector2 FireEasy()
        {
            //fire at a random shot target and return its position
            var ind = Program.RNG.Next(_shotTargets.Count());
            Vector2 posFired = _shotTargets[ind];
            _playerBoard.FireAt(posFired.x, posFired.y);
            _shotTargets.RemoveAt(ind);
            return posFired;
        }

        //getters and setters
        public int Padding
        {
            get { return _enemyBoard.WidthString + _padding.Length; }
        }
    }
}
