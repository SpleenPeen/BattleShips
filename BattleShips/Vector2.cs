using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips
{
    public class Vector2
    {
        int _x, _y;

        public Vector2(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public Vector2() : this(0, 0) { }

        public void Add(Vector2 other)
        {
            _x += other.x;
            _y += other.y;
        }

        public void Minus(Vector2 other)
        {
            _x -= other.x;
            _y -= other.y;
        }

        public static Vector2 GetMovementVector(ConsoleKey key)
        {
            var move = new Vector2();

            if (key == ConsoleKey.UpArrow || key == ConsoleKey.W)
                move.y -= 1;
            else if (key == ConsoleKey.DownArrow || key == ConsoleKey.S)
                move.y += 1;

            if (key == ConsoleKey.LeftArrow || key == ConsoleKey.A)
                move.x -= 1;
            else if (key == ConsoleKey.RightArrow || key == ConsoleKey.D)
                move.x += 1;

            return move;
        }

        public bool Equals(Vector2 v)
        {
            if (v.x == x && v.y == y)
                return true;
            return false;
        }

        public int x
        {
            get { return _x; }
            set { _x = value; }
        }

        public int y
        {
            get { return _y; }
            set { _y = value; }
        }

        public int[] Get
        {
            get { return [x, y]; }
            set { _x = value[0]; _y = value[1]; }
        }
    }
}
