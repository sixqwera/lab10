using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PascalCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ПОЛНЫЙ КОМПИЛЯТОР ===");

            string testFile = @"D:\secondary fyles\учеба\COMP\Компилятор\test.pas";
            if (!File.Exists(testFile))
            {
                Console.WriteLine($"Ошибка: Файл {testFile} не найден!");
                return;
            }

            StreamReader stream = new StreamReader(testFile);

            InputOutput.Init(stream);


            SyntaxAnalyser.Parse();

            InputOutput.End();

            stream.Close();
        }
    }
}