﻿using Marimo.LinqToDejizo;
using System;
using System.Linq;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var tested = new DejizoSource();

            tested.Requested += (sender, e) =>
            {
                Console.WriteLine($"{DateTime.Now:O} {e.Uri}");
            };
            tested.ReturnedResponse += (sender, e) =>
            {
                Console.WriteLine($"{DateTime.Now:O} {e.Response}");
            };

            var query =
                from item in tested.EJdict
                where item.HeaderText.Contains("dict")
                select item;

            query.ToArray();

            Console.ReadLine();
        }
    }
}