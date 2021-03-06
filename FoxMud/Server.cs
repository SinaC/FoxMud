﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoxMud.Game;
using FoxMud.Game.Command;
using FoxMud.Game.World;

namespace FoxMud
{
    enum LogType
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// artificial delay for commands
    /// </summary>
    public enum TickDelay
    {
        Instant = 1,
        Single = 650,
        Double = Single * 2,
        Triple = Single * 3,
        Quadruple = Single * 4,
        Quintuple = Single * 5,
        Sextuple = Single * 6,
        Septuple = Single * 7,
        Octuple = Single * 8,
        Nonuple = Single * 9,
        Decuple = Single * 10,
        Reroll = Single * 20,
    }

    class Server : IDisposable
    {
        public static Server Current { get; private set; }
        
        // config values
        // not yet sure of the value in using a Configuration object to contain these
        public static string DataDir { get { return "Data"; } }
        public static bool AutoApprovedEnabled { get { return false; } }
        public static string WelcomeRoom { get { return "welcome room"; } }
        public static string StartRoom { get { return "the academy"; } }
        public static int Port = 9999;
        public static int CorpseDecayTime { get { return 10*60*1000; } } // 10 minutes
        public static int MobWalkInterval { get { return 5*60*1000; } } // 5 minutes
        public static int IncapacitatedHitPoints { get { return -3; } }
        public static int DeadHitPoints { get { return -10; } }
        public static int RegenTime { get { return 30*1000; } } // 30 seconds
        public static int AgeTime { get { return 60*1000; } } // 1 minute

        private const int TickRate = 20;
        private const long TickTime = 1000/TickRate;
        private const int CombatTickRate = TickRate*50; // 1 second, no clue if this will feel smooth or not
        private const string LogFilePath = @"Log\gamelog.log";

        private AutoResetEvent wait;
        private bool running;

        public Server()
        {
            wait = new AutoResetEvent(false);
            Current = this;
            Random = new Random();

            // Create services
            ConnectionListener = new ConnectionListener(Port);
            ConnectionMonitor = new ConnectionMonitor();
            SessionMonitor = new SessionMonitor();
            Database = new Database(DataDir);
            CommandLookup = new DynamicCommandLookup();
            Areas = Database.GetAll<Area>();
            CombatSkills = Database.GetAll<CombatSkill>();
            RepopHandler = new RepopHandler(TickTime);
            CombatHandler = new CombatHandler(CombatTickRate);
            RegenHandler = new RegenHandler(RegenTime);
            AgeHandler = new AgeHandler(AgeTime);
            OpenTrades = new Dictionary<Session, string>();
            
            // Setup services
            ConnectionListener.ConnectionHandler = new StartupConnectionHandler(ConnectionMonitor, SessionMonitor);

            Console.WriteLine("listening on port {0}...", Port);
        }

        public ConnectionListener ConnectionListener { get; private set; }
        public ConnectionMonitor ConnectionMonitor { get; private set; }
        public SessionMonitor SessionMonitor { get; private set; }
        public Database Database { get; private set; }
        public CommandLookup CommandLookup { get; private set; }
        public CombatHandler CombatHandler { get; private set; }
        public RegenHandler RegenHandler { get; private set; }
        public RepopHandler RepopHandler { get; private set; }
        public AgeHandler AgeHandler { get; private set; }
        public IEnumerable<Area> Areas { get; private set; }
        public IEnumerable<CombatSkill> CombatSkills { get; private set; }
        public Dictionary<Session, string> OpenTrades { get; private set; }
        public Random Random { get; private set; }

        public void Start()
        {
            if (running)
                throw new InvalidOperationException("Server already running");

            running = true;
            ConnectionListener.Start();
            RepopHandler.Start();
            CombatHandler.Start();
            RegenHandler.Start();
            AgeHandler.Start();

            DoLoop();
        }

        private void WaitIfNeeded(long elapsedTime)
        {
            long timeToWait = TickTime - elapsedTime;
            if (timeToWait > 0)
            {
                wait.WaitOne(TimeSpan.FromMilliseconds(timeToWait));
                
                // for debugging, ****WARNING this can write a LOT to console
                //Console.WriteLine("tick: waited {0} elapsed: {1}", timeToWait, elapsedTime);
            }
        }

        private void DoLoop()
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            while (running)
            {
                ConnectionMonitor.Update();

                stopWatch.Stop();
                WaitIfNeeded(stopWatch.ElapsedMilliseconds);
                stopWatch.Reset();
            }
        }

        public void Log(LogType logType, string message)
        {
            try
            {
                var log = string.Format("{0}: {1}", logType, message);
                File.AppendAllText(LogFilePath, log);
            }
            catch
            {
                
            }
        }

        public void Stop()
        {
            running = false;
            ConnectionListener.Stop();
        }

        public void Dispose()
        {
            Stop();
            ConnectionListener.Dispose();
            wait.Dispose();
        }

        public void Log(string message, bool newLine = true)
        {
            Console.Write("{0,-10}: {1}{2}", DateTime.Now.ToString("mm:ss.fff"), message, newLine ? "\n" : string.Empty);
        }
    }
}
