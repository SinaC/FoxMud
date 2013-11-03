using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FoxMud.Game;
using FoxMud.Text;

namespace FoxMud
{
    class Session : OutputTextWriter
    {
        private readonly Stack<SessionStateBase> sessionStates;
        private Connection connection;        

        public Session(Connection connection)
        {
            this.sessionStates = new Stack<SessionStateBase>();
            this.connection = connection;
            this.connection.LineRecieved += OnInputReceived;
            this.connection.Closed += OnConnectionClosed;
        }

        public TextTransformer OutputTransformer { get; set; }
        public Player Player { get; set; }

        public void Echo(bool echo)
        {
            connection.Echo(echo);
        }

        private SessionStateBase CurrentState
        {
            get
            {
                if (sessionStates.Count == 0)
                    return null;

                return sessionStates.Peek();
            }
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            // For now we'll end the session.
            connection = null;
            End();
        }

        private void OnInputReceived(object sender, LineReceivedEventArgs e)
        {
            if (CurrentState != null)
                CurrentState.OnInput(e.Data);
        }

        /// <summary>
        /// Moves the controller of the session to the passed in session.
        /// </summary>
        /// <param name="session">The session that control should be assumed.</param>
        public void HandConnectionTo(Session session)
        {
            session.connection.WriteLine("This player has been logged in from else where");
            session.ChangeConnection(RelinquishConnectionOwnership());
            End();    
        }

        private Connection RelinquishConnectionOwnership()
        {
            if (connection != null)
            {
                connection.Closed -= OnConnectionClosed;
                connection.LineRecieved -= OnInputReceived;
                var result = connection;
                connection = null;
                return result;
            }

            return null;
        }

        public void ChangeConnection(Connection newConnection)
        {
            if (connection != null)
            {
                connection.Closed -= OnConnectionClosed;
                connection.LineRecieved -= OnInputReceived;
                connection.Close();
            }

            if (newConnection != null)
            {
                connection = newConnection;
                connection.Closed += OnConnectionClosed;
                connection.LineRecieved += OnInputReceived;
            }
        }

        public void PushState(SessionStateBase state)
        {
            if (state == null)
                throw new ArgumentNullException("state", "state is null.");

            if (CurrentState != null)
                CurrentState.OnStateLeave();

            state.Session = this;
            sessionStates.Push(state);
            state.OnStateInitialize();
            state.OnStateEnter();
            
        }

        public SessionStateBase PopState()
        {
            if (CurrentState == null)
                return null;

            var poppedState = sessionStates.Pop();
            poppedState.OnStateLeave();
            poppedState.OnStateShutdown();

            if (CurrentState != null)
                CurrentState.OnStateEnter();

            return poppedState;
        }

        public void Write(string value)
        {
            if (connection == null)
                return;

            if (OutputTransformer != null)
                value = OutputTransformer.Transform(value);

            connection.Write(value);
        }

        public void Write(object value)
        {
            Write(value.ToString());
        }

        public void Write(string format, params object[] args)
        {
            Write(string.Format(format, args));
        }

        public void WriteLine(string value)
        {
            if (connection == null)
                return;

            if (OutputTransformer != null)
                value = OutputTransformer.Transform(value);

            connection.WriteLine(value);
        }

        public void WriteLine(object value)
        {
            WriteLine(value.ToString());
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine(string.Format(format, args));
        }

        public event EventHandler SessionEnded;
        private void OnSessionEnded()
        {
            var ev = SessionEnded;

            if (ev != null)
                SessionEnded(this, EventArgs.Empty);
        }


        public void End()
        {
            foreach (var session in sessionStates)
            {                
                session.OnStateShutdown();
            }

            if (connection != null)
            {
                connection.Close();                
            }

            OnSessionEnded();
        }
    }
}
