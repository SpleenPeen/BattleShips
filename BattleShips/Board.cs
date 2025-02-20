using System;
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
        int _shotsFired;

        public Board(int width, int height)
        {
            _shotsFired = 0;
            _shipSpaces = 0;
            _shipsHit = 0;
            _spaces = new SpaceStates[height, width];

            for (int y = 0; y < _spaces.GetLength(0); y++)
            {
                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    _spaces[y, x] = SpaceStates.empty;
                }
            }
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

        public void GenerateShips(List<int[]> ships) //ship == {3, 4} - meaning 3 ships of size 4
        {
            //calculate ship spaces - could be done while chaning spaces, but it doesnt really matter
            foreach (int[] ship in ships)
                _shipSpaces += ship[0] * ship[1];

            //create a list with all avaialbe spaces
            List<int[]> available = new List<int[]>();

            for (int y = 0; y < _spaces.GetLength(0); y++)
            {
                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    available.Add([y, x]);
                }
            }

            //loop through all the ships to add
            for (int shipType = 0; shipType < ships.Count(); shipType++)
            {
                for (int count = 0; count < ships[shipType][0]; count++)
                {
                    while (true)
                    {
                        //get random position
                        int availableInd = Program.RNG.Next(available.Count());
                        int y = available[availableInd][0];
                        int x = available[availableInd][1];

                        //randomize whether ship is vertical or horizontal 
                        bool vertical = false;
                        if (Program.RNG.Next(2) == 1)
                            vertical = true;

                        //if ships would run off the board, get another position
                        if (vertical)
                        {
                            if (y + ships[shipType][1] >= _spaces.GetLength(0))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (x + ships[shipType][1] >= _spaces.GetLength(1))
                            {
                                continue;
                            }
                        }

                        //check if all the spaces are not already taken up
                        bool valid = true;
                        for (int i = 0; i < ships[shipType][1]; i++)
                        {
                            //check for vertical spaces
                            if (vertical)
                            {
                                if (_spaces[y + i, x] != SpaceStates.empty)
                                {
                                    valid = false;
                                    break;
                                }
                                continue;
                            }
                            //otherwise check the horizontal spaces
                            if (_spaces[y, x + i] != SpaceStates.empty)
                            {
                                valid = false;
                                break;
                            }
                        }
                        //if wasnt valid, grab another position
                        if (!valid)
                            continue;

                        //add to board
                        for (int i = 0; i < ships[shipType][1]; i++)
                        {
                            if (vertical)
                            {
                                _spaces[y + i, x] = SpaceStates.ship;
                                available.Remove([y + i, x]);
                                continue;
                            }
                            _spaces[y, x + i] = SpaceStates.ship;
                            available.Remove([y, x + i]);
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

        public string[] GetDrawLines(int[]? sel = null, bool hidden = false)
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
                                fieldContent = "O";
                            break;
                        case SpaceStates.miss:
                            fieldContent = "*";
                            break;
                        case SpaceStates.hit:
                            fieldContent = "X";
                            break;
                    }

                    if (sel != null && sel[0] == x && sel[1] == y)
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
