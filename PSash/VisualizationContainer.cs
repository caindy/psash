using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace PSash
{
    public interface IVisualizationContainer
    {
        void AddNewVisualization(UIElement control);
        double Width { get; }
        Brush Background { get; }
        Brush Foreground { get; }
        FontFamily ConsoleFont { get; }
        Dispatcher Dispatcher { get; }
    }

    internal class VisualizationContainer : IVisualizationContainer
    {
        public VisualizationContainer(StackPanel container)
        {
            _container = container;
        }

        private StackPanel _container;

        public void AddNewVisualization(UIElement control)
        {
            _container.Children.Add(control);
            if (control is TextBoxBase)
                (control as TextBoxBase).TextChanged += (_, __) =>
            {
                (_container.Parent as ScrollViewer).ScrollToEnd();
            };
        }

        public double Width
        {
            get { return _container.ActualWidth; }
        }


        public Brush Background
        {
            get { return Brushes.Transparent; }
        }

        public Brush Foreground
        {
            get { return Brushes.White; }
        }

        public FontFamily ConsoleFont
        {
            get { return new FontFamily("Consolas"); }
        }

        public Dispatcher Dispatcher 
        { 
            get 
            { 
                return Application.Current.Dispatcher; 
            } 
        }
    }
}
