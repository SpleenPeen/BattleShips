namespace BattleShips
{
    internal class Program
    {
        public static Random RNG = new Random();

        private static void Main(string[] args)
        {
            Board example = new Board(10, 10);
            example.GenerateShips(new int[,] { { 2, 1 }, { 2, 2 }, { 3, 3 } });
            example.DrawBoard();
        }
    }

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

        public Board(int width, int height)
        {
            _spaces = new SpaceStates[height, width];

            for (int y = 0; y < _spaces.GetLength(0); y++)
            {
                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    _spaces[y, x] = SpaceStates.empty;
                }
            }
        }

        public void GenerateShips(int[,] ships) //ship == {3, 4} - meaning 3 ships of size 4
        {
            //sort ships based on size - largest to smallest (less likely to get an impossible to fit sequence)


            //create a list with all avaialbe spaces
            List<int[]> available = new List<int[]>();

            for (int y = 0; y < _spaces.GetLength(0); y++)
            {
                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    available.Add([y,x]);
                }
            }

            //loop through all the ships to add
            for (int ship = 0; ship < ships.GetLength(0); ship++)
            {
                for (int count = 0; count < ships[ship, 0]; count++)
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
                            if (y+ships[ship,1] >= _spaces.GetLength(0))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (x + ships[ship, 1] >= _spaces.GetLength(1))
                            {
                                continue;
                            }
                        }

                        //check if all the spaces are not already taken up
                        bool valid = true;
                        for (int i = 0; i < ships[ship, 1]; i++)
                        {
                            //check for vertical spaces
                            if (vertical)
                            {
                                if(_spaces[y+i, x] != SpaceStates.empty)
                                {
                                    valid = false;
                                    break;
                                }
                                continue;
                            }
                            //otherwise check the horizontal spaces
                            if (_spaces[y, x+i] != SpaceStates.empty)
                            {
                                valid = false;
                                break;
                            }
                        }
                        //if wasnt valid, grab another position
                        if (!valid)
                            continue;

                        //add to board
                        for (int i = 0; i < ships[ship, 1]; i++)
                        {
                            if (vertical)
                            {
                                _spaces[y + i, x] = SpaceStates.ship;
                                available.Remove([y+i, x]);
                                continue;
                            }
                            _spaces[y, x + i] = SpaceStates.ship;
                            available.Remove([y, x+i]);
                        }
                        break;
                    }
                }
            }
        }

        private void DrawInbetweenLine()
        {
            for (int x = 0; x < _spaces.GetLength(1); x++)
            {
                if (x == 0)
                    Console.Write("+");
                Console.Write("---+");
            }
            Console.Write("\n");
        }

        public void DrawBoard()
        {
            for (int y = 0; y < _spaces.GetLength(0); y++)
            {
                if (y == 0)
                    DrawInbetweenLine();

                //draw fields
                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    //get string depending of space status
                    string fieldContent = "";
                    switch (_spaces[y,x])
                    {
                        case SpaceStates.empty:
                            fieldContent = " ";
                            break;
                        case SpaceStates.ship:
                            fieldContent = "O";
                            break;
                        case SpaceStates.miss:
                            fieldContent = "*";
                            break;
                        case SpaceStates.hit:
                            fieldContent = "X";
                            break;
                    }

                    //if its the first field print border first
                    if (x == 0)
                        Console.Write("|");
                    Console.Write($" {fieldContent} |");
                }
                Console.Write("\n");

                DrawInbetweenLine();
            }
        }
    }
}