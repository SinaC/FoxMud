using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FoxMud;

namespace FoxMud.Game.State
{
    class SimpleWordLexer
    {
        StringReader reader;
        TextToken next;

        public SimpleWordLexer(string text)
        {
            reader = new StringReader(text);
        }

        private bool isWhiteSpace(int c)
        {
            return c == ' ' 
                || c == '\t' 
                || c == '\f';
        }

        private bool isSep(int c)
        {
            return c == '.'
                || c == ','
                || c == ':'
                || c == ';'
                || c == '!'
                || c == '?';
        }

        private void SkipWhitespace()
        {
            int c = reader.Peek();

            while (isWhiteSpace(c))
            {
                reader.Read();
                c = reader.Peek();
            }
        }

        private TextToken ParseNewLine()
        {
            int c = reader.Read();
            
            if (c == '\r')
            {                                
                c = reader.Peek();
                if (c == '\n')
                {
                    reader.Read();                    
                }

                return new TextToken() { Type = TextTokenType.Newline };
            }

            if (c == '\n')
            {
                return new TextToken() { Type = TextTokenType.Newline };
            }

            throw new InvalidOperationException("Unrecognized characters for newline");
        }

        private TextToken ParseWord()
        {
            StringBuilder wordBuilder = new StringBuilder();

            int c = reader.Peek();
            while (!isWhiteSpace(c) && !isSep(c) && c != '\r' && c != '\n' && c != -1)
            {
                wordBuilder.Append((char)c);

                reader.Read();
                c = reader.Peek();
            }

            return new TextToken()
            {
                Type = TextTokenType.Word,
                Text = wordBuilder.ToString()
            };
        }

        public TextToken GetNext()
        {
            if (next != null)
            {
                try
                {
                    return next;
                }
                finally
                {
                    next = null;
                }
            }

            SkipWhitespace();

            int c = reader.Peek();

            if (c == -1)
                return new TextToken() { Type = TextTokenType.Eof };

            switch (c)
            {
                case '.':
                case ',':
                case ':':
                case ';':
                case '!':
                case '?':
                    reader.Read();
                    return new TextToken() { Type = TextTokenType.Sep, Text = ((char)c).ToString() };

                case '\r':
                case '\n':
                    return ParseNewLine();

                default:
                    return ParseWord();
            }
        }

        public TextToken Peek()
        {
            if (next != null)
                return next;

            next = GetNext();
            return next;
        }
    }


    class TextEditor : SessionStateBase
    {
        private List<string> lines;

        public TextEditor()
        {
            lines = new List<string>();
        }

        public TextEditor(string initialText)
        {
            lines = new List<string>(
                        initialText.Split(new string[] { "\r\n" }, StringSplitOptions.None));
        }

        /// <summary>
        /// Description of what the user should be entering
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The resulting string
        /// </summary>
        public string Result { get { return string.Join("\r\n", lines); } }

        /// <summary>
        /// Set to true when the editor exits with save condition true
        /// </summary>
        public bool Success { get; private set; }

        public Action<string> OnSuccess { get; set; }

        private void PrintHelp()
        {
            Session.WriteLine("Enter text line by line to append it to the text buffer.");
            Session.WriteLine("There are also a number of single letter commands you can enter.");
            Session.WriteLine("h            - Displays this help message.");
            Session.WriteLine("x            - Exit the text editor without saving the changes.");
            Session.WriteLine("s            - Save and exit the text editor.");
            Session.WriteLine("c            - Clears all text in the buffer.");
            Session.WriteLine("l            - Lists the current text.");
            Session.WriteLine("f            - Formats your text.");
            Session.WriteLine("r l <text>   - Replaces line number l with text <text>.");
            Session.WriteLine("d l          - Deletes line number l.");
            Session.WriteLine("");
            Session.WriteLine("When auto formating is invoked (f), single line breaks are");
            Session.WriteLine(" ignored while double line breaks are treated as end of paragraph"); 
            Session.WriteLine("markers which cause there to be an empty line in the final output.");            
        }

        private void PrintCurrentText()
        {
            Session.WriteLine("Current text:");

            if (lines.Count == 0)
            {
                Session.WriteLine("No text has been entered.");
            }

            for (int i = 0; i < lines.Count; i++)
            {
                Session.WriteLine("{0,3}: {1}", i + 1, lines[i]);
            }
        }

        public override void OnStateEnter()
        {
            Session.WriteLine("\f");
            Session.WriteLine(Description);
            Session.WriteLine("====================================================================");
            Session.WriteLine("Entering text editor.");
            Session.WriteLine("Type h for help, x to discard changes and exit the editor,");
            Session.WriteLine("s to save changes and exit the editor");
            Session.WriteLine("====================================================================");
            PrintCurrentText();
            Session.Write("\r\n> ");
            base.OnStateEnter();
        }

        private void FormatText()
        {
            const int MaxLineLength = 75;            
            List<string> lines = new List<string>();
            SimpleWordLexer lexer = new SimpleWordLexer(Result);
            StringBuilder currentLine = new StringBuilder();
           
            for (TextToken token = lexer.GetNext(); 
                 token.Type != TextTokenType.Eof; 
                 token = lexer.GetNext())
            {
                if (token.Type == TextTokenType.Newline)
                {
                    TextToken next = lexer.Peek();

                    if (next.Type == TextTokenType.Newline)
                    {
                        lines.Add(currentLine.ToString());
                        lines.Add(string.Empty);
                        currentLine.Clear();
                        token = lexer.GetNext();
                    }
                }

                if (token.Type == TextTokenType.Word)
                {
                    var next = lexer.Peek();
                    int addedRequiredSpace = 0;

                    if (currentLine.Length > 0)
                        addedRequiredSpace = 1;

                    if (next.Type == TextTokenType.Sep)
                        addedRequiredSpace = 2;

                    if (currentLine.Length + token.Text.Length + addedRequiredSpace >= MaxLineLength)
                    {
                        lines.Add(currentLine.ToString());
                        currentLine.Clear();
                    }

                    if (currentLine.Length > 0)
                        currentLine.Append(' ');

                    string textToAppend = token.Text;

                    while (textToAppend.Length > MaxLineLength)
                    {
                        lines.Add(textToAppend.Substring(0, MaxLineLength));
                        textToAppend = textToAppend.Substring(MaxLineLength);
                        currentLine.Clear();
                    }

                    currentLine.Append(textToAppend);
                }

                if (token.Type == TextTokenType.Sep)
                {
                    currentLine.Append(token.Text);
                }
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString());

            this.lines = lines;
        }

        private void DoReplace(string commandArguments)
        {
            string lineNumberStr;
            string remaining = commandArguments.ReadCommandLinePart(out lineNumberStr);

            int lineNumber;
            if (!int.TryParse(lineNumberStr, out lineNumber))
            {
                Session.WriteLine("First argument to replace was not a line number");
                return;
            }

            if (lineNumber < 1 || lineNumber > lines.Count)
            {
                Session.WriteLine("Invalid line number");
                return;
            }

            lines[lineNumber - 1] = remaining.TrimStart();
        }

        private void DoDelete(string commandArguments)
        {
            string lineNumberStr;
            commandArguments.ReadCommandLinePart(out lineNumberStr);

            int lineNumber;
            if (!int.TryParse(lineNumberStr, out lineNumber))
            {
                Session.WriteLine("First argument to replace was not a line number");
                return;
            }

            if (lineNumber < 1 || lineNumber > lines.Count)
            {
                Session.WriteLine("Invalid line number");
                return;
            }

            lines.RemoveAt(lineNumber - 1);
        }

        public override void OnInput(string input)
        {
            string command;
            string remaining = input.ReadCommandLinePart(out command);

            if (command.Length == 1)
            {
                switch (command.ToLower())
                {
                    case "h":
                        PrintHelp();
                        break;

                    case "x":
                        Success = false;
                        Session.PopState();
                        break;

                    case "s":
                        Success = true;
                        if (OnSuccess != null)
                            OnSuccess(Result);
                        Session.PopState();
                        break;

                    case "l":
                        PrintCurrentText();
                        break;

                    case "f":
                        FormatText();
                        break;

                    case "r":
                        DoReplace(remaining);
                        break;

                    case "d":
                        DoDelete(remaining);
                        break;

                    case "c":
                        lines.Clear();
                        break;
                }
            }
            else
            {
                lines.Add(input);
            }

            Session.Write("\r\n> ");

            base.OnInput(input);
        }


    }
}
