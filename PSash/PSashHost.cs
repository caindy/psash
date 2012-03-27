using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Host;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using System.Threading;
using System.Management.Automation;
using System.Security;
using System.Reflection;
using System.Runtime.InteropServices;

namespace PSash
{
    internal class PSashHost : PSHost, IHostSupportsInteractiveSession, IHostUISupportsMultipleChoiceSelection, IDisposable
    {
        #region runspaces

        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed) Runspaces.Pop().Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public bool IsRunspacePushed
        {
            get 
            { 
                return CurrentRunspace.ConnectionInfo != null; 
            }
        }

        private Stack<Runspace> _runspaces;
        private Stack<Runspace> Runspaces 
        {
            get
            {
                return _runspaces ?? (_runspaces = new Stack<Runspace>());
            }
        }
        
        public void PopRunspace()
        {
            if (Runspaces.Count > 1)
                Runspaces.Pop().Close();
        }

        public void PushRunspace(Runspace runspace)
        {
            Runspaces.Push(runspace);
            if (runspace.RunspaceAvailability != RunspaceAvailability.Available) runspace.Open();
        }

        public Runspace Runspace
        {
            get { return CurrentRunspace; }
        }

        private Runspace CurrentRunspace
        {
            get 
            {
                if(Runspaces.Count < 1)
                    PushRunspace(RunspaceFactory.CreateRunspace(this));
                return Runspaces.Peek();
            }
        }
        
        #endregion

        public string Execute(string cmd)
        {
            var psashCmd = PSashCommands.From(cmd);
            if (psashCmd == null)
            {
                var pipeline = CurrentRunspace.CreatePipeline(cmd, true);
                var results = pipeline.Invoke();
                return results.Aggregate(String.Empty, (s, p) => s + p.ToString() + Environment.NewLine);
            }
            return ExecutePSashCommand(psashCmd);
        }

        private string ExecutePSashCommand(PSashCommands.PSashCommand psashCmd)
        {
            return String.Empty;
        }

        public override CultureInfo CurrentCulture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
        }

        public override CultureInfo CurrentUICulture
        {
            get { return Thread.CurrentThread.CurrentUICulture; }
        }

        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException();
        }

        public override Guid InstanceId
        {
            get
            {
                return AssemblyInfo.Guid;
            }
        }

        const string NAME = "PSash";
        public override string Name
        {
            get { return NAME; }
        }

        public override void NotifyBeginApplication()
        {
            throw new NotImplementedException();
        }

        public override void NotifyEndApplication()
        {
            throw new NotImplementedException();
        }

        public event EventHandler<int> Exit;
        public override void SetShouldExit(int exitCode)
        {
            if (Exit != null)
                Exit(this, exitCode);
        }

        PSHostUserInterface hostUI = new PSashHostUI();
        public override PSHostUserInterface UI
        {
            get 
            { 
                return hostUI; 
            }
        }

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public Collection<int> PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, IEnumerable<int> defaultChoices)
        {
            throw new NotImplementedException();
        }

        internal class PSashHostUI : PSHostUserInterface
        {

            public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
            {
                throw new NotImplementedException();
            }

            public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
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

            PSHostRawUserInterface _psashHostRawUI; 
            public override PSHostRawUserInterface RawUI
            {
                get { return _psashHostRawUI ?? (_psashHostRawUI = new PSashHostRawUI()); }
            }

            public override string ReadLine()
            {
                throw new NotImplementedException();
            }

            public override SecureString ReadLineAsSecureString()
            {
                throw new NotImplementedException();
            }

            public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
            {
                throw new NotImplementedException();
            }

            public override void Write(string value)
            {
                throw new NotImplementedException();
            }

            public override void WriteDebugLine(string message)
            {
                throw new NotImplementedException();
            }

            public override void WriteErrorLine(string value)
            {
                throw new NotImplementedException();
            }

            public override void WriteLine(string value)
            {
                throw new NotImplementedException();
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
        }

        internal class PSashHostRawUI : PSHostRawUserInterface
        {

            public override ConsoleColor BackgroundColor
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override Size BufferSize
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override Coordinates CursorPosition
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override int CursorSize
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override void FlushInputBuffer()
            {
                throw new NotImplementedException();
            }

            public override ConsoleColor ForegroundColor
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override BufferCell[,] GetBufferContents(Rectangle rectangle)
            {
                throw new NotImplementedException();
            }

            public override bool KeyAvailable
            {
                get { throw new NotImplementedException(); }
            }

            public override Size MaxPhysicalWindowSize
            {
                get { throw new NotImplementedException(); }
            }

            public override Size MaxWindowSize
            {
                get { throw new NotImplementedException(); }
            }

            public override KeyInfo ReadKey(ReadKeyOptions options)
            {
                throw new NotImplementedException();
            }

            public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
            {
                throw new NotImplementedException();
            }

            public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
            {
                throw new NotImplementedException();
            }

            public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
            {
                throw new NotImplementedException();
            }

            public override Coordinates WindowPosition
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override Size WindowSize
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public override string WindowTitle
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
