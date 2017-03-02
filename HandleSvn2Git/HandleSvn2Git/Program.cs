using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandleSvn2Git
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine("HelloAgain");

            string[] argsArray = System.Environment.GetCommandLineArgs();

            HandleSvn2Git hsGit = new HandleSvn2Git();
            hsGit.Run(argsArray);

            //Console.Read();
        }
    }
}
