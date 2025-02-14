namespace BattleShips
{
    internal class Program
    {
        public static Random RNG = new Random();

        private static void Main(string[] args)
        {
            Board example = new Board(10, 10);
            example.GenerateShips(new int[,] { { 3, 1 }, { 2, 2 }, { 1, 3 } });
            example.DrawBoard();
        }
    }

    public class Board
    {
        private bool[,] _spaces;

        public Board(int width, int height)
        {
            _spaces = new bool[height, width];
        }

        public void GenerateShips(int[,] ships)
        {
            for (int ship = 0; ship < ships.GetLength(0); ship++)
            {
                for (int count = 0; count < ships[ship, 0]; count++)
                {
                    while (true)
                    {
                        int x = Program.RNG.Next(_spaces.GetLength(1));
                        int y = Program.RNG.Next(_spaces.GetLength(0));

                        bool vertical = false;
                        if (Program.RNG.Next(2) == 1)
                            vertical = true;
                        
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

                        //check if valid
                        bool valid = true;
                        for (int i = 0; i < ships[ship, 1]; i++)
                        {
                            if (vertical)
                            {
                                if(_spaces[y+i, x])
                                {
                                    valid = false;
                                    break;
                                }
                                continue;
                            }
                            if (_spaces[y, x+i])
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid)
                            continue;

                        //add to board
                        for (int i = 0; i < ships[ship, 1]; i++)
                        {
                            if (vertical)
                            {
                                _spaces[y + i, x] = true;
                                continue;
                            }
                            _spaces[y, x + i] = true;
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
                    string fieldContent;
                    if (_spaces[y, x])
                        fieldContent = "O";
                    else
                        fieldContent = " ";

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