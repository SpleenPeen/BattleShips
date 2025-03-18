using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips
{
    //class for generally useful functions
    internal static class GeneralUtils
    {
        //Convert and array to a linked list
        public static LinkedList<type> ArrayToLinkedList<type>(type[] array)
        {
            LinkedList<type> list = new LinkedList<type>();

            //add each element of the array to a linked list and return the list
            foreach (var item in array)
            {
                list.AddLast(item);
            }
            return list;
        }

        //Writes the given string with padding-inpt.length
        public static void WritePadded(string inpt, int pad, bool nextLine = false)
        {
            //set output string to input
            var outStrng = inpt;

            //add a blank space to output string until output.length == pad
            for (int j = outStrng.Length; j < pad; j++)
                outStrng += " ";

            //write string and go to next line if specified
            Console.Write(outStrng);
            if (nextLine)
                Console.WriteLine();
        }
    }
}
