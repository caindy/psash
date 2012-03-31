using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PSash
{
    /// <summary>
    /// Implements the Adapter pattern to allow the PowerShell runtime to 
    /// communicate with the PSash UI
    /// </summary>
    internal class PSashHostUIAdapter : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        /// <summary>
        /// Parse a string containing a hotkey character.
        /// Take a string of the form
        ///    Yes to &amp;all
        /// and returns a two-dimensional array split out as
        ///    "A", "Yes to all".
        /// </summary>
        /// <param name="input">The string to process</param>
        /// <returns>
        /// A two dimensional array containing the parsed components.
        /// </returns>
        /// <remarks>http://msdn.microsoft.com/en-us/library/windows/desktop/ee706546(v=vs.85).aspx</remarks>
        private static string[] GetHotkeyAndLabel(string input)
        {
            string[] result = new string[] { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0)
                {
                    result[0] = fragments[1][0].ToString().
                    ToUpper(CultureInfo.CurrentCulture);
                }

                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else
            {
                result[1] = input;
            }

            return result;
        }

        PSHostRawUserInterface _psashHostRawUI;
        public override PSHostRawUserInterface RawUI
        {
            get 
            {
                return _psashHostRawUI ?? (_psashHostRawUI = new PSashHostRawUIAdapter()); 
            }
        }

        #region prompting/reading

        /// <summary>
        /// Prompts the user for input. 
        /// <param name="caption">The caption or title of the prompt.</param>
        /// <param name="message">The text of the prompt.</param>
        /// <param name="descriptions">A collection of FieldDescription objects  
        /// that describe each field of the prompt.</param>
        /// <returns>A dictionary object that contains the results of the user 
        /// prompts.</returns>
        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            this.Write(
                       ConsoleColor.Blue,
                       ConsoleColor.Black,
                       caption + "\n" + message + " ");
            Dictionary<string, PSObject> results =
                       new Dictionary<string, PSObject>();
            foreach (FieldDescription fd in descriptions)
            {
                string[] label = GetHotkeyAndLabel(fd.Label);
                this.WriteLine(label[1]);
                string userData = Console.ReadLine();
                if (userData == null)
                {
                    return null;
                }

                results[fd.Name] = PSObject.AsPSObject(userData);
            }

            return results;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            throw new NotImplementedException();
        }

        public Collection<int> PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, IEnumerable<int> defaultChoices)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            throw new NotImplementedException();
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            throw new NotImplementedException();
        }

        public override string ReadLine()
        {
            throw new NotImplementedException();
        }

        public override SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException();
        }
        
        #endregion

        #region writing

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            Write(value);
        }

        public PSashHostUIAdapter(IConsoleWriterProvider writerProvider)
        {
            _writerProvider = writerProvider;
        }

        private IConsoleWriter _writer;
        private IConsoleWriterProvider _writerProvider;
        private StringBuilder sb;
        public void BeginExecutePipeline()
        {
            _writer = _writerProvider.ConsoleWriter;
            sb = new StringBuilder();
        }

        public void EndExecutePipeline()
        {
            _writer.Write(sb.ToString());
        }


        public override void Write(string value)
        {
            sb.Append(value);
        }

        public override void WriteDebugLine(string message)
        {
            WriteLine(message);
        }

        public override void WriteErrorLine(string value)
        {
            WriteLine(value);
        }

        public override void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }

        public override void WriteLine()
        {
            Write(Environment.NewLine);
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            WriteLine(value);
        }
       
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            throw new NotImplementedException();
        }

        public override void WriteVerboseLine(string message)
        {
            throw new NotImplementedException();
        }

        public override void WriteWarningLine(string message)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
