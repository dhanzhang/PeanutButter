﻿using System;
using System.Linq;

namespace NugetPackageVersionIncrementer
{
    public static class Program
    {
        static int Main(string[] args)
        {
            if (args.Contains("-v") || args.Contains("--version"))
            {
                Console.WriteLine("Version: 1.1");
                return 0;
            }
            try
            {
                var coordinator = ResolveNuspecCoordinator();
                coordinator.LogAction = Console.WriteLine;
                coordinator.IncrementVersionsUnder(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ruh-Roh! Something went wrong, Shaggy!");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Stack trace follows:");
                Console.WriteLine(ex.StackTrace);
                return -1;
            }
        }

        private static INuspecVersionCoordinator ResolveNuspecCoordinator()
        {
            var bootstrapper = new WindsorBootstrapper();
            var container = bootstrapper.Bootstrap();
            var coordinator = container.Resolve<INuspecVersionCoordinator>();
            return coordinator;
        }
    }
}
