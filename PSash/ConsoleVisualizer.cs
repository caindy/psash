using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace PSash
{
    class ConsoleVisualizer : IConsoleWriter
    {
        RichTextBox _box;
        private IVisualizationContainer _container;
        public ConsoleVisualizer(IVisualizationContainer container)
        {
            _container = container;
        }

        private void Initialize()
        {
            var flowDoc = new FlowDocument
            {
                PageWidth = _container.Width
            };
            _box = new RichTextBox(flowDoc)
            {
                Background = _container.Background,
                Foreground = _container.Foreground,
                BorderThickness = new Thickness(0d),
                FontFamily = _container.ConsoleFont,
                IsReadOnly = true
            };
            _container.AddNewVisualization(_box);
        }

        public void Write(string s)
        {
            _container.Dispatcher.InvokeAsync(() =>
            {
                if (_box == null)
                    Initialize();
                _box.AppendText(s);
            });
        }

        public void Visualize(Collection<PSObject> output)
        {
            throw new NotImplementedException();
        }
    }
}
