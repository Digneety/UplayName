﻿using NameChecker.Tasks;


Console.ForegroundColor = ConsoleColor.DarkBlue;
Console.WriteLine("   ");
Console.WriteLine("   ");
Console.WriteLine(" _   _                           ___    _                    _     ");
Console.WriteLine("( ) ( )                         (  _`\\ ( )                  ( )    ");
Console.WriteLine("| `\\| |   _ _   ___ ___     __  | ( (_)| |__     __     ___ | |/') ");
Console.WriteLine("| , ` | /'_` )/' _ ` _ `\\ /'__`\\| |  _ |  _ `\\ /'__`\\ /'___)| , <  ");
Console.WriteLine("| |`\\ |( (_| || ( ) ( ) |(  ___/| (_( )| | | |(  ___/( (___ | |\\`\\");
Console.WriteLine("(_) (_)`\\__,_)(_) (_) (_)`\\____)(____/'(_) (_)`\\____)`\\____)(_) (_)");
Console.WriteLine("    ");
Console.WriteLine("                         Twitter: @Digneety                            ");
Console.WriteLine("    ");
Console.ForegroundColor = ConsoleColor.Magenta;
Console.WriteLine("To stop checking names press ESC.");
Console.ResetColor();

do
{
    while (!Console.KeyAvailable)
    {
        await NameTask.CheckNamesAsync();
        break;
    }
} while (Console.ReadKey(true).Key != ConsoleKey.Escape);