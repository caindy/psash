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

namespace PSash
{
    internal class PSashHost : PSHost, IHostSupportsInteractiveSession, IDisposable
    {
        public override PSHostUserInterface UI
        {
            get
            {
                return new PSashHostUIAdapter(_visualizationFactory.DefaultVisualizer);
            }
        }

        private IVisualizationFactory _visualizationFactory;
        public PSashHost(IVisualizationFactory visualizationFactory)
        {
            _visualizationFactory = visualizationFactory;
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

        private Collection<PSObject> exec(PowerShell pipeline, string cmd = "", IEnumerable<object> input = null, bool sendToDefault = false)
        {
            pipeline.Runspace = CurrentRunspace;
            if(!String.IsNullOrEmpty(cmd))
                pipeline.AddScript(cmd);

            if (sendToDefault)
            {
                // Add the default outputter to the end of the pipe and then 
                // call the MergeMyResults method to merge the output and 
                // error streams from the pipeline. This will result in the 
                // output being written using the PSHost and PSHostUserInterface 
                // classes instead of returning objects to the host application.
                pipeline.AddCommand("out-default");
                pipeline.Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            }

            // If there is any input pass it in, otherwise just invoke the
            // the pipeline.
            if (input == null)
                return pipeline.Invoke();
            else
                return pipeline.Invoke(input);

        }

        public void Execute(string cmd)
        {
            var psashCmd = PSashCommands.From(cmd);
            if (psashCmd == null)
            {
                using (var pipeline = PowerShell.Create())
                {
                    Runspaces.Clear();
                    var result = exec(pipeline, cmd);
                    exec(pipeline, input: result, sendToDefault: true);
                }
            }
            ExecutePSashCommand(psashCmd);
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
