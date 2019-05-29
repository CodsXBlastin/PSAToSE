using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSAToSE
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "PSAToSE - (DTZxPorter)";
            Console.WriteLine("PSAToSE - DTZxporter");


            if (args.Length <= 0)
            {
                Console.WriteLine("Usage: PSAToSE.exe <file>.psa");
                return;
            }

            PsaReader.SerializePSA(args[0]);
            Console.WriteLine("Processed {0}!", args[0]);
        }
    }
}
