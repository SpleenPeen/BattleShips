﻿using System;
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

        public enum Difficulty
        {
            easy,
            medium,
            hard
        }

        private SpaceStates[,] _spaces;
        int _shipSpaces;
        int _shipsHit;
        int _shotsFired;
        Difficulty _diff;

        static char[] _displaySymbols = ['X', '*', 'O'];

        public Board()
        {
            _diff = Difficulty.easy;
            _shotsFired = 0;
            _shipSpaces = 0;
            _shipsHit = 0;
        }

        public Board(int width, int height) : this()
        {
            SetSize(width, height);
        }

        public void SetSize(int width, int height, bool keepShips = false)
        {
            if (width == 0 || height == 0)
                return;

            var newSpaces = new SpaceStates[height, width];

            if (_spaces == null || !keepShips)
            {
                _spaces = newSpaces;
                return;
            }

            for (int y = 0; y < Math.Min(_spaces.GetLength(0), height); y++)
            {
                for (int x = 0; x < Math.Min(_spaces.GetLength(1), width); x++)
                {
                    newSpaces[x,y] = _spaces[x,y];
                }
            }

            _spaces = newSpaces;
        }

        public bool FireAt(int x, int y)
        {
            _shotsFired++; //increase shots fired
            //check if miss
            if (_spaces[y, x] == SpaceStates.empty)
            {
                _spaces[y, x] = SpaceStates.miss;
                return true;
            }
            //check if hit
            if (_spaces[y, x] == SpaceStates.ship)
            {
                _spaces[y, x] = SpaceStates.hit;
                _shipsHit++;
                return true;
            }
            _shotsFired--; //if didnt fire take away from shots fired (as it was added at the start)
            return false;
        }

        public void FireAi()
        {

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

        public void GenerateShips(List<int[]> ships) //ship == {3, 4} - meaning 3 ships of size 4
        {
            //calculate ship spaces - could be done while changing spaces, but it has a miniscule impact on performence
            foreach (int[] ship in ships)
                _shipSpaces += ship[0] * ship[1];

            //create a list with all available spaces
            List<Vector2> available = AllSpaces;

            //loop through all the ships to add
            for (int shipType = 0; shipType < ships.Count(); shipType++)
            {
                for (int count = 0; count < ships[shipType][0]; count++)
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
                                if (y + ships[shipType][1] - 1 >= Height)
                                    continue;
                            }
                            else
                            {
                                if (x + ships[shipType][1] - 1 >= Width)
                                    continue;
                            }

                            //check if all the spaces are not already taken up
                            valid = true;
                            for (int j = 0; j < ships[shipType][1]; j++)
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
                        for (int i = 0; i < ships[shipType][1]; i++)
                        {
                            int curX = x;
                            int curY = y;

                            if (vertical)
                                curY += i;
                            else
                                curX += i;

                            _spaces[curY, curX] = SpaceStates.ship;
                            available.Remove(new Vector2(curX, curY));
                        }
                        break;
                    }
                }
            }
        }

        private string GetInbetweenLine()
        {
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
            return 2 + 4 * x;
        }

        public int GetYPosStrng(int y)
        {
            return 1 + 2 * y;
        }

        public SpaceStates GetSpaceState(int x, int y)
        {
            return _spaces[y, x];
        }

        public void SetSpaceStatus(int x, int y, SpaceStates state)
        {
            _spaces[y, x] = state;
            if (state == SpaceStates.ship)
                _shipSpaces++;
        }

        public Difficulty Diff
        {
            get { return _diff; }
            set { _diff = value; }
        }

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
            get { return _shotsFired; }
        }

        public float HitRate
        {
            get { return (float)Math.Round( (float)_shipsHit / _shotsFired*100, 1); }
        }

        public int WidthString
        {
            get
            {
                return _spaces.GetLength(1) * 4 + 1;
            }
        }
    }
}
