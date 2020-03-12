/*
 * Eterra Demo
 * A set of examples on how the Eterra framework can be used.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using Eterra.Platforms.Windows;
using System;
using System.IO;
using System.Threading;

namespace Eterra.Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("┌───────────────────────────────────────────┐");
            Console.WriteLine("│          Eterra Demo Application          │");
            Console.WriteLine("└───────────────────────────────────────────┘");
            Console.WriteLine();
            Console.WriteLine("(1) First steps: Static Rectangles Simulator");
            Console.WriteLine("(2) Animations: Moving Rectangles Simulator");
            Console.WriteLine("(3) Resources: Frame Works");
            Console.WriteLine("(4) User Input: Take CTRL");
            Console.WriteLine("(5) 2D game: *flirtly* Do you like... chess?");
            Console.WriteLine("(6) Shading: Moving Box Simulator - in 3D!1!");
            Console.WriteLine("(7) 3D game: The big finale - brace yourself!");
            Console.WriteLine();
            Console.Write("Please enter the number of the demo to run: ");

            int selection = 0;
            int attempts = 0;

            while (selection == 0)
            {
                attempts++;
                string inputString = (Console.ReadLine() ?? "").Trim();

                if (int.TryParse(inputString, out int input) &&
                    input > 0 && input < 8) selection = input;
                else if (attempts == 1)
                {
                    Console.Write("The input is not a valid number " +
                           "between 1 and 7. Please try again: ");
                }
                else if (attempts == 2)
                {
                    Console.Write("The input is still not a valid number " +
                           "between 1 and 7. Please try again: ");
                }
                else if (attempts == 3)
                {
                    Console.Write("Seriously. Just give me a number between " +
                        "1 and 7, like... 1, or 4: ");
                }
                else if (attempts == 4)
                {
                    Console.Write("You got to be shitting me... NUMBER! " +
                        "BETWEEN 1 AND 7! TYPE ONE AND PRESS RETURN: ");
                }
                else if (attempts == 5 && 
                    inputString.Trim().ToLower() == "one")
                {
                    Thread.Sleep(2000);
                    Console.Write("Thats... ");
                    Thread.Sleep(1000);
                    Console.Write("close enough. ");
                    Thread.Sleep(1400);
                    Console.WriteLine("Here you go.");
                    Thread.Sleep(200);
                    selection = 1;
                }
                else if (attempts >= 5)
                {
                    Thread.Sleep(500); Console.Write('.');
                    Thread.Sleep(500); Console.Write('.');
                    Thread.Sleep(500); Console.Write('.');
                    Thread.Sleep(1000);
                    Console.WriteLine(" Bye.");
                    Thread.Sleep(1000);
                    return;
                }
            }

            Console.WriteLine();
            for (int i = 0; i < (Console.BufferWidth); i++) Console.Write('─');
            Console.WriteLine();

            EterraApplicationBase app = null;
            StreamWriter logWriter = null;
            try { logWriter = new StreamWriter("debug.log", true); }
            catch (Exception exc)
            {
                Log.Error("The logfile 'debug.log' couldn't be created - " +
                    "logging to file disabled.", exc);
            }

            Log.UseEnglishExceptionMessages = true;
            Log.MessageLogged += delegate (object s, Log.EventArgs a)
            {
                Console.WriteLine(a.ToString(Console.BufferWidth -
                    Environment.NewLine.Length));
                if (logWriter != null) logWriter.WriteLine(a.ToString());
            };

            try
            {
                switch (selection)
                {
                    case 1: app = new Demo01FirstSteps(); break;
                    case 2: app = new Demo02Animation(); break;
                    case 3: app = new Demo03Resources(); break;
                    case 4: app = new Demo04Controls(); break;
                    case 5: app = new Demo05Chess(); break;
                    case 6: app = new Demo06Shading(); break;
                    case 7: app = new Demo07Game(); break;
                }
            }
            catch (Exception exc)
            {
                Log.Error("The application couldn't be created.", exc);
            }

            if (app != null)
            {
                try { app.Run(new PlatformProvider()); }
                catch (Exception exc) { Log.Error(exc); }
            }

            Console.WriteLine();
            for (int i = 0; i < (Console.BufferWidth); i++)
                Console.Write('─');

            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Close();
            }

            if (Log.HighestLogMessageLevel > Log.MessageLevel.Information)
            {
                Console.WriteLine("Program executed with warnings or " +
                    "errors. Press any key to exit...");
                Console.ReadKey(true);
            }
            else
            {
                Console.WriteLine("Program closing in 2 seconds...");
                Thread.Sleep(2000);
            }
        }
    }
}
