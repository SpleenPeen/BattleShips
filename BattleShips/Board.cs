﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips
{
    public class Board
    {
        //enem containing all space states
        public enum SpaceStates
        {
            empty,
            ship,
            miss,
            hit
        }

        private SpaceStates[,] _spaces;
        int _shipSpaces;
        int _shipsHit;
        LinkedList<Vector2> _shots;

        static char[] _displaySymbols = ['X', '*', 'O', 'H'];

        //Constructor for initialising from an ongoing savefile
        public Board(short[][] spaces, int shipSpaces, int shipsHit, LinkedList<Vector2> shots)
        {
            //initialise variables
            ConvertShortsToSpaces(spaces);
            _shipSpaces = shipSpaces;
            _shipsHit = shipsHit;
            _shots = shots;
        }

        //Default constructor
        public Board(int width = 10, int height = 10)
        {
            //initialise variables
            _shots = new LinkedList<Vector2>();
            _shipSpaces = 0;
            _shipsHit = 0;
            SetSize(width, height);
        }

        private void ConvertShortsToSpaces(short[][] spaces)
        {
            //set spaces array size to the array of shorts dimensions
            _spaces = new SpaceStates[spaces.Length, spaces[0].Length];

            //loop through each field, convert the short to the enum and add to spaces matrix
            for (int y = 0; y < spaces.Length; y++)
            {
                for (int x = 0; x < spaces[y].Length; x++)
                {
                    _spaces[y,x] = (SpaceStates)spaces[y][x];
                }
            }
        }

        public void SetSize(int width, int height)
        {
            //prevent changing to 0x0 matrix
            if (width == 0 || height == 0)
                return;

            //set new spaces to given dimensions
            _spaces = new SpaceStates[height, width];
        }

        public bool FireAt(int x, int y, bool replay = false)
        {
            //if trying to fire out of bounds, return false
            if (!WithinBounds(new Vector2(x,y)))
                return false;

            //check if miss
            if (_spaces[y, x] == SpaceStates.empty)
            {
                _spaces[y, x] = SpaceStates.miss;
                if (!replay)
                    _shots.AddLast(new Vector2(x, y));
                return true;
            }
            //check if hit
            if (_spaces[y, x] == SpaceStates.ship)
            {
                _spaces[y, x] = SpaceStates.hit;
                if (!replay)
                    _shots.AddLast(new Vector2(x, y));
                _shipsHit++;
                return true;
            }
            return false;
        }

        public static void DrawStrings(string[] strings, Board? selBoard = null, Vector2? sel = null)
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
                        if (c == Board.HitChar || c == Board.MissChar || c == Board.SelChar)
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
                        else if (seperated == Board.SelChar.ToString() && selBoard != null && sel != null)
                        {
                            var spaceState = selBoard.GetSpaceState(sel.x, sel.y);
                            if (spaceState == Board.SpaceStates.hit)
                                Console.ForegroundColor = ConsoleColor.Green;
                            else if (spaceState == Board.SpaceStates.miss)
                                Console.ForegroundColor = ConsoleColor.Red;
                        }
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

        public static string[] CombineStrings(string[] str1, string[] str2, string padding = "")
        {
            //combine the string arrays with padding inbetween
            string[] combined = new string[str1.Length];
            for (int i = 0; i < str1.Length; i++)
            {
                combined[i] = str1[i] + padding + str2[i];
            }
            return combined;
        }

        private List<Vector2> AllSpaces
        {
            get
            {
                //create a list with all available spaces
                List<Vector2> available = new List<Vector2>();

                for (int y = 0; y < _spaces.GetLength(0); y++)
                {
                    for (int x = 0; x < _spaces.GetLength(1); x++)
                    {
                        available.Add(new Vector2(x, y));
                    }
                }
                return available;
            }
        }

        private void SortShips(ref List<Vector2> ships)
        {
            //add first entry to sorted list
            List<Vector2> sortedList = [ships[0]];

            //loop through all ships
            for (int i = 1; i < ships.Count; i++)
            {
                //initialise variables
                int max = 0;
                int min = sortedList.Count() - 1;
                int curInd;

                //loop until you find a spot to add the ship
                while (true)
                {
                    //set curent index to halfway between min and max
                    if (min == max)
                        curInd = min;
                    else
                        curInd = max + (min - max) / 2;

                    //check if current ship is smaller than the entry in sorted ships at curind
                    if (ships[i].y < sortedList[curInd].y)
                    {
                        //if the difference between min and max is 1, increase max by 1
                        if (max == curInd && min != max)
                        {
                            max++;
                            continue;
                        }

                        //change max index
                        max = curInd;

                        //if max and min are equal, insert the ship in front of the current index
                        if (min == max)
                        {
                            sortedList.Insert(curInd+1, ships[i]);
                            break;
                        }
                    }

                    //cehck if current ship is larger than the sorted ships entry at curind
                    else if (ships[i].y > sortedList[curInd].y)
                    {
                        //set min to the current index
                        min = curInd;

                        //if min and max are equal, insert ship behind the current ship
                        if (min == max)
                        {
                            sortedList.Insert(curInd, ships[i]);
                            break;
                        }
                    }
                }
            }
            //set ships to the sorted list
            ships = sortedList;
        }

        public void GenerateShips(List<Vector2> ships) //ship.x == amount of ships that size, ships.y == size of ship
        {
            //sort ships
            SortShips(ref ships);

            //calculate ship spaces - could be done while changing spaces, but it has a miniscule impact on performence
            foreach (Vector2 ship in ships)
                _shipSpaces += ship.x * ship.y;

            //create a list with all available spaces
            List<Vector2> available = AllSpaces;

            //loop through all the ships to add
            for (int shipType = 0; shipType < ships.Count(); shipType++)
            {
                for (int count = 0; count < ships[shipType].x; count++)
                {
                    //check if any pos is valid
                    var currentlyAvailable = new List<Vector2>(available);
                    while (true)
                    {
                        //if there are no valid positions, wipe slate clean
                        if (currentlyAvailable.Count() == 0)
                        {
                            shipType = 0;
                            count = -1;
                            _spaces = new SpaceStates[Height, Width];
                            available = AllSpaces;
                            break;
                        }

                        //get random position
                        int availableInd = Program.RNG.Next(currentlyAvailable.Count());
                        int y = currentlyAvailable[availableInd].y;
                        int x = currentlyAvailable[availableInd].x;

                        //randomize whether ship is vertical or horizontal 
                        bool vertical = false;
                        if (Program.RNG.Next(2) == 1)
                            vertical = true;

                        //check if either vertical or non vertical is valid
                        bool valid = false;
                        for (int i = 0; i < 2; i++)
                        {
                            //check if this pos will work flipped
                            if (i == 1)
                                vertical = !vertical;

                            //if ships would run off the board, get another position
                            if (vertical)
                            {
                                if (y + ships[shipType].y - 1 >= Height)
                                    continue;
                            }
                            else
                            {
                                if (x + ships[shipType].y - 1 >= Width)
                                    continue;
                            }

                            //check if all the spaces are not already taken up
                            valid = true;
                            for (int j = 0; j < ships[shipType].y; j++)
                            {
                                //check for vertical spaces
                                if (vertical)
                                {
                                    if (_spaces[y + j, x] != SpaceStates.empty)
                                    {
                                        valid = false;
                                        break;
                                    }
                                }
                                //otherwise check the horizontal spaces
                                else if (_spaces[y, x + j] != SpaceStates.empty)
                                {
                                    valid = false;
                                    break;
                                }
                            }
                            if (valid)
                                break;
                        }

                        //if isn't valid remove from available spaces and choose a different pos
                        if (!valid)
                        {
                            currentlyAvailable.RemoveAt(availableInd);
                            continue;
                        }

                        //add to board
                        for (int i = 0; i < ships[shipType].y; i++)
                        {
                            int curX = x;
                            int curY = y;

                            if (vertical)
                                curY += i;
                            else
                                curX += i;

                            _spaces[curY, curX] = SpaceStates.ship;
                            available.RemoveAll(v => v.Equals(new Vector2(curX, curY)));
                        }
                        break;
                    }
                }
            }
        }

        private string GetInbetweenLine()
        {
            //returns a string for drawing lines inbetween spaces
            string str = "";
            for (int x = 0; x < _spaces.GetLength(1); x++)
            {
                if (x == 0)
                    str += "+";
                str += "---+";
            }
            return str;
        }

        public string[] GetDrawLines(Vector2? sel = null, bool hidden = false)
        {
            List<string> lines = new List<string>();

            for (int y = 0; y < _spaces.GetLength(0); y++)
            {
                var curLine = "";
                if (y == 0)
                    lines.Add(GetInbetweenLine());

                //draw fields
                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    //get string depending of space status
                    string fieldContent = "";
                    switch (_spaces[y, x])
                    {
                        case SpaceStates.empty:
                            fieldContent = " ";
                            break;
                        case SpaceStates.ship:
                            if (hidden)
                                fieldContent = " ";
                            else
                                fieldContent = ShipChar.ToString();
                            break;
                        case SpaceStates.miss:
                            fieldContent = MissChar.ToString();
                            break;
                        case SpaceStates.hit:
                            fieldContent = HitChar.ToString();
                            break;
                    }

                    //change space to H if selected
                    if (sel != null && sel.x == x && sel.y == y)
                        fieldContent = "H";

                    //if its the first field print border first
                    if (x == 0)
                        curLine += "|";
                    curLine += $" {fieldContent} |";
                }
                lines.Add(curLine);
                lines.Add(GetInbetweenLine());
            }

            return lines.ToArray();
        }

        public int GetXPosStrng(int x)
        {
            //returns the position of the position in drawstring based on space x position
            return 2 + 4 * x;
        }

        public int GetYPosStrng(int y)
        {
            //returns the position of the position in drawstring based on space y position
            return 1 + 2 * y;
        }

        public SpaceStates? GetSpaceState(int x, int y)
        {
            //if out of bounds return null
            if (!WithinBounds(new Vector2(x, y)))
                return null;
            //otherwise return space entry
            return _spaces[y, x];
        }

        public void SetSpaceStatus(int x, int y, SpaceStates state)
        {
            //add/reduce ship spaces accordingly
            if (state != SpaceStates.ship && _spaces[y, x] == SpaceStates.ship)
                _shipSpaces--;
            if (state == SpaceStates.ship && _spaces[y,x] != SpaceStates.ship)
                _shipSpaces++;

            //change space
            _spaces[y, x] = state;
        }

        public void PrepForReplay()
        {
            //reset all the hits and misses to empty and ship spaces
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                    UndoHit(x, y);
            }
        }

        public Vector2 FireReplay()
        {
            //fire shot in the bottom of queue
            var shot = _shots.First.Value;
            FireAt(shot.x, shot.y, true);
            _shots.RemoveFirst();
            return shot;
        }

        public void UndoShot(Vector2 shot)
        {
            //add shot to the bottom of queue
            _shots.AddFirst(shot);
            UndoHit(shot.x, shot.y);
        }

        private void UndoHit(int x, int y)
        {
            //change hit spaces to ship and misses to empty, changing shipshit accordingly
            var curState = _spaces[y, x];
            switch (curState)
            {
                case SpaceStates.hit:
                    _spaces[y, x] = SpaceStates.ship;
                    _shipsHit--;
                    break;
                case SpaceStates.miss:
                    _spaces[y, x] = SpaceStates.empty;
                    break;
            }
        }

        public bool WithinBounds(Vector2 pos)
        {
            //check if coordinates within bounds
            if (pos.x < 0 || pos.x >= Width)
                return false;
            if (pos.y < 0 || pos.y >= Height)
                return false;
            return true;
        }

        public short[][] SpacesNum
        {
            //returns shipspaces as an array of shorts
            get
            {
                short[][] outpt = new short[Height][];
                for (int y = 0; y < Height; y++)
                {
                    outpt[y] = new short[Width];
                    for (int x = 0; x < Width; x++)
                    {
                        outpt[y][x] = (short)_spaces[y,x];
                    }
                }
                return outpt;
            }
        }


        //getter and setters
        public static char HitChar
        {
            get { return _displaySymbols[0]; }
        }

        public static char MissChar
        {
            get { return _displaySymbols[1]; }
        }

        public static char ShipChar
        {
            get { return _displaySymbols[2]; }
        }

        public static char SelChar
        {
            get { return _displaySymbols[3]; }
        }

        public int Width
        {
            get { return _spaces.GetLength(1); }
        }

        public int Height
        {
            get { return _spaces.GetLength(0); }
        }

        public bool Won
        {
            get { return _shipsHit == _shipSpaces; }
        }

        public int ShotsFired
        {
            get { return _shots.Count; }
        }

        public float HitRate
        {
            get { return (float)Math.Round( (float)_shipsHit / ShotsFired*100, 1); }
        }

        public LinkedList<Vector2> Shots
        {
            get { return _shots; }
        }

        public SpaceStates[,] Spaces
        {
            get { return _spaces; }
        }

        public int WidthString
        {
            get
            {
                return _spaces.GetLength(1) * 4 + 1;
            }
        }

        public int ShipSpaces
        {
            get { return _shipSpaces; }
        }

        public int ShipsHit
        {
            get { return _shipsHit; }
        }
    }
}
