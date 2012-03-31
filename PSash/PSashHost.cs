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
using System.Windows.Controls;
using System.Diagnostics;
using System.Reactive.Linq;

namespace PSash
{
    internal class PSashHost : PSHost, IHostSupportsInteractiveSession, IDisposable
    {
        private PSashHostUIAdapter _psashHostUIAdapter;
        public override PSHostUserInterface UI
        {
            get
            {
                return (_psashHostUIAdapter ?? (_psashHostUIAdapter = new PSashHostUIAdapter(_visualizationFactory))) as PSHostUserInterface;
            }
        }

        private IConsoleWriterProvider _visualizationFactory;
        public PSashHost(IConsoleWriterProvider visualizationFactory)
        {
            _visualizationFactory = visualizationFactory;
            PushRunspace(RunspaceFactory.CreateRunspace(this));
        }

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
            var runspace = Runspaces.Pop();
            if(runspace.RunspaceAvailability != RunspaceAvailability.None) 
                runspace.CloseAsync();
        }

        public void PushRunspace(Runspace runspace)
        {
            Runspaces.Push(runspace);
            if (runspace.RunspaceAvailability != RunspaceAvailability.Available) runspace.OpenAsync();
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


        private PowerShell GetPipeline()
        {
            var pipeline = PowerShell.Create();
            pipeline.Runspace = CurrentRunspace;
            return pipeline;
        }

        private Mutex _outputMutex = new Mutex();
        public Task Execute(string cmd)
        {
            var psashCmd = PSashCommands.From(cmd);
            if (psashCmd != null)
                return Task.Run(() => ExecutePSashCommand(psashCmd));

            var pipeline = GetPipeline();
            if (!String.IsNullOrEmpty(cmd))
                pipeline.AddScript(cmd);
            var pipelineTask = Task.Run(() => pipeline.Invoke());
            pipelineTask.ContinueWith(t =>
            {
                pipeline.Dispose();
                pipeline = GetPipeline();
                var mutex = new Mutex(initiallyOwned: true);
                var outputTask = Task.Run(() =>
                {
                    try
                    {
                        _outputMutex.WaitOne();
                        _psashHostUIAdapter.BeginExecutePipeline();
                        pipeline.AddCommand("out-default");
                        pipeline.Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
                        pipeline.Invoke(t.Result);
                        pipeline.Dispose();
                        _psashHostUIAdapter.EndExecutePipeline();
                    }
                    finally
                    {
                        _outputMutex.ReleaseMutex();
                    }
                });
            });
            return pipelineTask;
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

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
    }
}
