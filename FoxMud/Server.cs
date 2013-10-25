using System;
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
        Single = 500,
        Double = Single * 2,
        Triple = Single * 3,
        Quadruple = Single * 4,
        Quintuple = Single * 5,
    }

    class Server : IDisposable
    {
        public static Server Current { get; private set; }
        
        // config values
        // not yet sure of the value in using a Configuration object to contain these
        public static string DataDir { get { return "Data"; } }
        public static bool AutoApprovedEnabled { get { return true; } }
        public static string StartRoom { get { return "the square corner"; } }
        public static int Port = 9999;
        public static int CorpseDecayTime { get { return 10*60*1000; } } // 10 minutes
        public static int MobWalkInterval { get { return 5*60*1000; } } // 5 minutes
        public static int IncapacitatedHitPoints { get { return -3; } }
        public static int RegenTime { get { return 30*1000; } } // 30 seconds

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
            RepopHandler = new RepopHandler(TickTime);
            CombatHandler = new CombatHandler(CombatTickRate);
            
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
        public RepopHandler RepopHandler { get; private set; }
        public IEnumerable<Area> Areas { get; private set; }
        public Random Random { get; private set; }

        public void Start()
        {
            if (running)
                throw new InvalidOperationException("Server already running");

            running = true;
            ConnectionListener.Start();
            RepopHandler.Start();
            CombatHandler.Start();

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
    }
}
