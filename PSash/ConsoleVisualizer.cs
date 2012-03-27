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
    class ConsoleVisualizer : IDefaultVisualizer
    {
        RichTextBox box;
        private IVisualizationContainer _container;
        public ConsoleVisualizer(IVisualizationContainer container)
        {
            var flowDoc = new FlowDocument
            {
                PageWidth = container.Width,
                Focusable = false
            };
            box = new RichTextBox(flowDoc)
            {
                Background = container.Background,
                Foreground = container.Foreground,
                BorderThickness = new Thickness(0d),
                FontFamily = container.ConsoleFont,
                IsReadOnly = true,
                Focusable = false
            };
            _container = container;

            _container.Dispatcher.InvokeAsync(() =>
            {
                _container.AddNewVisualization(box);
            });
        }

        public void Write(string s)
        {
            _container.Dispatcher.InvokeAsync(() =>
            {
                box.AppendText(s);
            });
        }

        public void Visualize(Collection<PSObject> output)
        {
            throw new NotImplementedException();
        }
    }
}
