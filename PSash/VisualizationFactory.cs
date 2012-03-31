using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PSash
{

    public abstract class Visualizer
    {
        private IVisualizationContainer _container;
        public Visualizer(IVisualizationContainer container)
        {
            _container = container;
        }
        public abstract void Visualize(Collection<PSObject> output);
    }

    public interface IConsoleWriter
    {
        void Write(string value);
    }

    public interface IConsoleWriterProvider
    {
        IConsoleWriter ConsoleWriter { get; }
    }

    internal class VisualizationFactory : IConsoleWriterProvider
    {
        private VisualizationContainer _visualizationContainer;
        public VisualizationFactory(VisualizationContainer visualizationContainer)
        {
            _visualizationContainer = visualizationContainer;
        }

        public IConsoleWriter ConsoleWriter
        {
            get { return new ConsoleVisualizer(_visualizationContainer); }
        }
    }
}
