using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FoxMud.Game.Command;

namespace FoxMud
{
    class Server : IDisposable
    {
        public static Server Current { get; private set; }
        
        // config values
        // not yet sure of the value in using a Configuration object to contain these
        public static string DataDir { get { return "Data"; } }
        public static bool AutoApprovedEnabled { get { return true; } }
        public static string StartRoom { get { return "void"; } }
        public static int Port = 9999;

        private const int TickRate = 20;
        private const long TickTime = 1000 / TickRate;

        private AutoResetEvent wait;
        private bool running;

        public Server()
        {
            wait = new AutoResetEvent(false);
            Current = this;

            // Create services
            ConnectionListener = new ConnectionListener(Port);
            ConnectionMonitor = new ConnectionMonitor();
            SessionMonitor = new SessionMonitor();
            Database = new Database(DataDir);
            CommandLookup = new DynamicCommandLookup();

            // Setup services
            ConnectionListener.ConnectionHandler = new StartupConnectionHandler(ConnectionMonitor, SessionMonitor);

            Console.WriteLine("listening on port {0}...", Port);
        }

        public ConnectionListener ConnectionListener { get; private set; }
        public ConnectionMonitor ConnectionMonitor { get; private set; }
        public SessionMonitor SessionMonitor { get; private set; }
        public Database Database { get; private set; }
        public CommandLookup CommandLookup { get; private set; }

        public void Start()
        {
            if (running)
                throw new InvalidOperationException("Server already running");

            running = true;
            ConnectionListener.Start();

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
