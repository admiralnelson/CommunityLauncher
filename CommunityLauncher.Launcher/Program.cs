﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using HarmonyLib;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;
using Trinet.Core.IO.Ntfs;

namespace CommunityLauncher.Launcher
{
    internal class Program
    {
        private static WindowsFramework _windowsFramework;

        private static GraphicsForm _graphicsForm;

        private static List<string> _args;

        private static string _addionalArgs;

        private static LauncherUIDomain _standaloneUIDomain;

        private static bool _gameStarted;
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        private static string StarterExecutable = "Bannerlord.exe";

        private static bool debugged;
        //for displaying console
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        static Program()
        {
            
            var fileappender = new FileAppender();
            fileappender.Layout = new PatternLayout("%date [%thread] %-5level %logger %ndc - %message%newline");
            fileappender.AppendToFile = false;
            fileappender.File = "CommunityLauncher.log";
            BasicConfigurator.Configure(new Hierarchy(),fileappender);

            AppDomain.CurrentDomain.AssemblyResolve += Program.OnAssemblyResolve;
            if (!Debugger.IsAttached)
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.UnhandledException +=
                    new UnhandledExceptionEventHandler(OnUnhandledException);
                Application.ThreadException += 
                    new ThreadExceptionEventHandler(OnUnhandledThreadException);
            }
            //admiralnelson: windows7 SSL bug workaround
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        }


        static void ApplyToModuleDLLs(System.IO.DirectoryInfo root, Action<string> functor)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.dll");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                log.Error(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    functor(fi.FullName);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    ApplyToModuleDLLs(dirInfo,functor);
                }
            }
        }

        private static void Main(string[] args)
        {
            if(Debugger.IsAttached)
            {
                Console.WriteLine("Is being debugged!");
                debugged = true;
            }
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");
            ApplyToModuleDLLs(new DirectoryInfo(BasePath.Name + "Modules/Native"),x=>Assembly.LoadFrom(x));
            var resourceDepot = new ResourceDepot(BasePath.Name);

            resourceDepot.AddLocation("GUI/Launcher/");
            resourceDepot.AddLocation("Modules/Native/LauncherGUI/");
            resourceDepot.AddLocation("Modules/CommunityLauncher/GUI/");
            resourceDepot.CollectResources();
            resourceDepot.StartWatchingChangesInDepot();
            Program._args = args.ToList<string>();
            if(_args.Contains("/console"))
            {
                AllocConsole();
                Console.WriteLine("args: ");
                foreach (var item in _args)
                {
                    Console.WriteLine(item);
                }
            }
            var name = "M&B II: Bannerlord Community Launcher";
            Program._graphicsForm = new GraphicsForm(1154, 701, resourceDepot, true, true, true, name);
            Program._windowsFramework = new WindowsFramework {ThreadConfig = WindowsFrameworkThreadConfig.SingleThread};
            Program._standaloneUIDomain = new LauncherUIDomain(Program._graphicsForm, resourceDepot);
            Program._windowsFramework.Initialize(new FrameworkDomain[]
            {
                Program._standaloneUIDomain
            });
            Program._windowsFramework.RegisterMessageCommunicator(Program._graphicsForm);
            Program._windowsFramework.Start();
            if (Program._gameStarted)
            {                
                ManagedStarter.Program.Main(Program._args.ToArray());
            }
        }
        
        private static void OnUnhandledThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ErrorHandler(e.Exception, true);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ErrorHandler((Exception)e.ExceptionObject, true);
        }

        private static void ErrorHandler(Exception e, bool unhandled)
        {
            string exceptionStr = e.ToString();
            log.Fatal(e.StackTrace);

            log.Fatal(e.Data);
            log.Fatal(e.HResult);
            log.Fatal(exceptionStr);

            ErrorWindow.Display(e.Message, e.Source, e.StackTrace);

        }

        #region dostuffwithoutlaunch

        #endregion

        public static void StartGame()
        {

            ApplyToModuleDLLs(new DirectoryInfo(BasePath.Name + "Modules/"), assembly =>
            {
                var fileinfo = new FileInfo(assembly);
                fileinfo.DeleteAlternateDataStream("Zone.Identifier");

            });
            Program._addionalArgs = Program._standaloneUIDomain.AdditionalArgs;
            Program._args.Add(Program._addionalArgs);
            Program._gameStarted = true;
            Program._windowsFramework.UnRegisterMessageCommunicator(Program._graphicsForm);
            Program._graphicsForm.Destroy();
            Program._windowsFramework.Stop();
            Program._windowsFramework = null;
            Program._graphicsForm = null;
            User32.SetForegroundWindow(Kernel32.GetConsoleWindow());
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            if (args.Name.Contains("ManagedStarter"))
            {
                return Assembly.LoadFrom(Program.StarterExecutable);
            }
            var canfind = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().FullName== args.Name);
            if (canfind != default)
            {
                return canfind;
            }
            try
            {
                if (args.Name.Contains("System.Threading.Tasks.Extensions"))
                {

                    return Assembly.LoadFrom("System.Threading.Tasks.Extensions.dll");
                }
                    
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;
        }
    }
}