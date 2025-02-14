namespace BattleShips
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Board example = new Board(5, 5);
            example.DrawBoard();
        }
    }

    public class Board
    {
        private int[,] _spaces;

        public Board(int x, int y)
        {
            _spaces = new int[x, y];
        }

        public void DrawBoard()
        {
            for (int y = 0; y < _spaces.GetLength(0); y++)
            {
                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    if (x == 0)
                        Console.Write("|");
                    Console.Write($" {_spaces[y,x]} |");
                }
                Console.Write("\n");

                for (int x = 0; x < _spaces.GetLength(1); x++)
                {
                    if (x == 0)
                        Console.Write("|");
                    Console.Write($" {_spaces[y, x]} |");
                }
                Console.Write("\n");
            }
        }
    }
}