using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace FoxMud
{
    internal class Connection
    {
        private const int OutputBufferSize = 8192;
        private const int InputBufferSize = 4096;

        private readonly byte[] outputBuffer;
        private int outputBufferLength;

        private readonly byte[] inputBuffer;
        private int inputBufferLength;

        public Connection(Socket socket)
        {
            this.Socket = socket;
            this.outputBuffer = new byte[OutputBufferSize];
            this.outputBufferLength = 0;
            this.inputBuffer = new byte[InputBufferSize];
            this.inputBufferLength = 0;
        }

        public Socket Socket { get; private set; }

        public static explicit operator Socket(Connection connection)
        {
            return connection.Socket;
        }

        public event EventHandler<LineReceivedEventArgs> LineRecieved;

        private void OnLineRecieved(string line)
        {
            var ev = LineRecieved;

            if (ev != null)
                ev(this, new LineReceivedEventArgs(line));
        }

        public event EventHandler Closed;

        private void OnClosed()
        {
            var ev = Closed;
            if (Closed != null)
                Closed(this, EventArgs.Empty);
        }

        public void Close()
        {
            Socket.Close();
            OnClosed();
        }

        /// <summary>
        /// Flushes output down the underlying socket
        /// </summary>
        public void Flush()
        {
            if (outputBufferLength == 0)
                return;

            int bytesSent = Socket.Send(outputBuffer, 0, outputBufferLength, SocketFlags.None);

            if (bytesSent == 0)
            {
                Close();
                return;
            }

            outputBufferLength -= bytesSent;
            Buffer.BlockCopy(outputBuffer, bytesSent, outputBuffer, 0, outputBufferLength);
        }

        private void FilterBufferIntoInputBuffer(byte[] readBuffer, int bytesRead)
        {
            // Copy bytes into input buffer, filtering out TELNET commands
            for (int i = 0; i < bytesRead; i++)
            {
                if (readBuffer[i] == '\b')
                {
                    if (inputBufferLength > 0 &&
                        (inputBuffer[inputBufferLength - 1] != '\r' ||
                         inputBuffer[inputBufferLength - 1] != '\n'))
                        inputBufferLength--;
                    continue;
                }

                if (readBuffer[i] == 255)
                {
                    if (i > bytesRead - 2)
                        break;

                    if (readBuffer[i + 1] == 255)
                    {
                        inputBuffer[inputBufferLength] = 255;
                        inputBufferLength += 1;
                        i += 1;
                        continue;
                    }

                    if (readBuffer[i + 1] <= 250)
                    {
                        i += 1;
                        continue;
                    }

                    if (readBuffer[i + 1] > 250)
                    {
                        if (i > bytesRead - 3)
                            break;

                        i += 2;
                        continue;
                    }
                }
                else
                {
                    inputBuffer[inputBufferLength] = readBuffer[i];
                    inputBufferLength++;
                }
            }
        }

        /// <summary>
        /// Reads input from the underlying socket into the input buffer
        /// </summary>
        public void Fill()
        {
            byte[] readBuffer = new byte[InputBufferSize];

            // Read data into buffer
            int bytesRead = Socket.Receive(readBuffer, 0, InputBufferSize - inputBufferLength, SocketFlags.None);

            if (bytesRead == 0)
            {
                Close();
                return;
            }

            FilterBufferIntoInputBuffer(readBuffer, bytesRead);

            // Check for line ending            
            int indexOfNewLine = IndexOfNewLine(inputBuffer, inputBufferLength);
            int indexOfEndOfNewLine = indexOfNewLine + 2;

            // If no line ending, we don't need to do anything
            if (indexOfNewLine == -1)
            {
                // Unless the buffer is full, which means we'd have to overflow to
                // get a line ending, this is an error!
                if (inputBufferLength >= InputBufferSize)
                    throw new OverflowException("Input buffer would overflowed");

                return;
            }

            // this loop will parse multiple commands from advanced mud clients e.g.
            // in Zmud, look;look will send two new lines, so need to queue up both
            var skip = 0;
            while (inputBufferLength > 0)
            {
                // Get line contents
                string input = Encoding.ASCII.GetString(inputBuffer, 0, indexOfNewLine);
                // Remove line from buffer
                inputBufferLength -= indexOfEndOfNewLine;
                Buffer.BlockCopy(inputBuffer, indexOfEndOfNewLine, inputBuffer, 0, inputBufferLength);
                // Trigger line recieved event
                Console.WriteLine("COMMAND: {0}", input);
                OnLineRecieved(input);

                // reset indices to get next command
                indexOfNewLine = IndexOfNewLine(inputBuffer, inputBufferLength);
                indexOfEndOfNewLine = indexOfNewLine + 2;
            }
        }

        private int IndexOfNewLine(byte[] buffer, int length)
        {
            for (int i = 0; i < length - 1; i++)
            {
                if (buffer[i] == '\r' && buffer[i + 1] == '\n')
                {
                    return i;
                }
            }

            return -1;
        }

        public void Write(string value)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(value);

            int amountToAdd = Math.Min(OutputBufferSize - outputBufferLength, bytes.Length);

            if (amountToAdd == 0)
                return;

            Buffer.BlockCopy(bytes, 0, outputBuffer, outputBufferLength, amountToAdd);
            outputBufferLength += amountToAdd;
        }

        public void WriteLine(string value)
        {
            Write(value + "\r\n");
        }

        public void Echo(bool echo)
        {
            var echoBit = echo ? 0xFC : 0xFB;
            var bytes = new byte[]
                {
                    0xFF,
                    (byte)echoBit,
                    0x01
                };

            Socket.Send(bytes);
        }
    }
}