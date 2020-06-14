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
            Console.WriteLine(
                "┌─────────────────────────────────────────────────┐");
            Console.WriteLine(
                "│          Eterra Demo Application 0.1.1          │");
            Console.WriteLine(
                "└─────────────────────────────────────────────────┘");

            Console.WriteLine();

            int selection = 7;

            if (args.Length > 0)
            {
                string argument =
                    args[0].Trim().TrimStart('/', '-').ToLowerInvariant();

                if (argument == "help" || argument == "?" || argument == "h")
                {
                    Console.WriteLine(
                        "/help\tShow this information.");
                    Console.WriteLine(
                        "/1\tFirst steps: Static Rectangles Simulator");
                    Console.WriteLine(
                        "/2\tAnimations: Moving Rectangles Simulator");
                    Console.WriteLine(
                        "/3\tResources: Frame Works");
                    Console.WriteLine(
                        "/4\tUser Input: Take CTRL");
                    Console.WriteLine(
                        "/5\t2D game: *flirtly* Do you like... chess?");
                    Console.WriteLine(
                        "/6\tShading: Moving Box Simulator - in 3D!1!");
                    Console.WriteLine(
                        "/7\t3D game: Shiver with antici...pation!");
                    return;
                }
                else if (int.TryParse(argument, out int argumentSelection)
                    && argumentSelection >= 1 && argumentSelection <= 7)
                    selection = argumentSelection;
                else
                {
                    Console.WriteLine("Invalid command line parameter. Use " +
                    "\"/help\" for more information.");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Hint: See the command line parameter " +
                    "\"/help\" for the other demos included.");
                Console.WriteLine("Starting default example #7...");
                Console.WriteLine();
            }

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
