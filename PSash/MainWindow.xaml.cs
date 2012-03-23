using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PSash
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (_, __) =>
            {
                MaxOpacity = Opacity;
                SetupPSash();
                EnterInsertMode();
                _keyBindings = new ViBindings();
                Editor.PreviewKeyDown += CaptureSendCommand;
            };
        }

        #region key bindings

        void PreventLosingKeyboardFocus(object sender, RoutedEventArgs r)
        {
            r.Handled = true;
        }

        void ExitInsertMode()
        {
            Editor.PreviewLostKeyboardFocus -= PreventLosingKeyboardFocus;
            Editor.PreviewKeyUp -= CaptureEscapeInsertMode;
            Editor.PreviewKeyUp += CaptureEnterInsertMode;
            Editor.IsReadOnly = Editor.IsReadOnlyCaretVisible = true;
        }

        void EnterInsertMode()
        {
            Editor.IsReadOnly = Editor.IsReadOnlyCaretVisible = false;
            Editor.PreviewLostKeyboardFocus += PreventLosingKeyboardFocus;
            Editor.PreviewKeyUp += CaptureEscapeInsertMode;
            Editor.PreviewKeyUp -= CaptureEnterInsertMode;
            Editor.Focus();
        }

        private bool WasKeyCombinationPressed(KeyEventArgs e, Tuple<ModifierKeys, Key> keyCombo)
        {
            return (keyCombo.Item1 == (e.KeyboardDevice.Modifiers & keyCombo.Item1)
                && e.Key == keyCombo.Item2);
        }

        private IPSashKeyBindings _keyBindings;

        private void CaptureEnterInsertMode(object sender, KeyEventArgs e)
        {
            if (WasKeyCombinationPressed(e, _keyBindings.EnterInsertModeAtCurrentCursorPosition))
            {
                e.Handled = true;
                EnterInsertMode();
            }
        }

        private void CaptureEscapeInsertMode(object sender, KeyEventArgs e)
        {
            if (WasKeyCombinationPressed(e, _keyBindings.ExitInsertMode))
            {
                e.Handled = true;
                ExitInsertMode();
            }
        }

        private void CaptureSendCommand(object sender, KeyEventArgs e)
        {
            if (WasKeyCombinationPressed(e, _keyBindings.SendCommand))
            {
                e.Handled = true;
                SendCommand();
            }
        }
        #endregion

        #region input management
        private Run _currentInput;
        private Run CurrentInput
        {
            get
            {
                return _currentInput;
            }
            set
            {
                _currentInput = value;
                _currentInput.Background = SystemColors.HighlightBrush;
                _currentInput.Foreground = SystemColors.HighlightTextBrush;
            }
        }

        private string GetCurrentInput()
        {
            CreateCurrentInput(Editor.Selection.IsEmpty ? GetCurrentLine() : Editor.Selection);
            return CurrentInput.Text;
        }

        private TextRange GetCurrentLine()
        {
            TextRange inputRange;
            var pointer = Editor.CaretPosition;
            inputRange = GetInputLine(pointer);
            return inputRange;
        }

        private TextRange GetInputLine(TextPointer pointer)
        {
            var nextStart = pointer.GetLineStartPosition(1);
            var lineEnd = (nextStart ?? pointer.DocumentEnd).GetInsertionPosition(LogicalDirection.Backward);
            var lineStart = pointer.GetLineStartPosition(0);
            var prevEnd = lineStart.GetInsertionPosition(LogicalDirection.Backward);
            if (GetLineNumber(prevEnd) != 0) //are we already at the beginning of the content?
                lineStart = SearchBackwardsForLineContinuation(lineStart, prevEnd);
            return new TextRange(lineStart, lineEnd);
        }

        private int GetLineNumber(TextPointer pointer)
        {
            int someBigNumber = int.MaxValue;
            int lineMoved;
            pointer.GetLineStartPosition(-someBigNumber, out lineMoved);
            return -lineMoved;
        }

        private TextPointer SearchBackwardsForLineContinuation(TextPointer lineStart, TextPointer prevEnd)
        {
            TextRange endOfPreviousLine;
            TextPointer frontEndOfPreviousLine = prevEnd;
            do
            {
                frontEndOfPreviousLine = frontEndOfPreviousLine.GetPositionAtOffset(-1);
                endOfPreviousLine = new TextRange(frontEndOfPreviousLine, prevEnd);
            } while (String.IsNullOrWhiteSpace(endOfPreviousLine.Text));
            if (endOfPreviousLine.Text[0] == '|')
                lineStart = GetInputLine(frontEndOfPreviousLine).Start;
            return lineStart;
        }

        private void CreateCurrentInput(TextRange range)
        {
            var text = range.Text;
            range.Text = String.Empty;//.Start.DeleteTextInRun(text.Length); //TODO: would rather wrap the range in situ, but how?
            CurrentInput = new Run(text, range.Start);
        }
        #endregion

        #region powershell host
        private PSashHost _psash;
        private void SetupPSash()
        {
            _psash = new PSashHost();
            _psash.Exit += (_, i) => Environment.Exit(i);
        }

        protected override void OnClosed(EventArgs e)
        {
            _psash.Dispose();
            base.OnClosed(e);
        }

        private void SendCommand()
        {
            Output.AppendText(_psash.Execute(GetCurrentInput()));
        }
        #endregion

        #region show/hide
        /// <remarks>http://stackoverflow.com/a/1926796</remarks>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
            HotKeyWinApi.RegisterKeysFor(this);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == HotKeyWinApi.WM_HOTKEY)
            {
                ToggleVisibility();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ToggleVisibility()
        {
            if (Visibility.Visible == Visibility) FadeOut();
            else FadeIn();
        }

        Duration fadeDuration = TimeSpan.FromMilliseconds(400);
        private void FadeOut()
        {
            Blur();
            var fade = new DoubleAnimation(0, fadeDuration);
            fade.Completed += (s, _) => Visibility = Visibility.Hidden;
            BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void FadeIn()
        {
            Visibility = Visibility.Visible;
            var anim = new DoubleAnimation(0, MaxOpacity, fadeDuration);
            anim.Completed += (s, _) => Unblur();
            BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void Blur()
        {
            var blur = new BlurEffect();
            var current = Background;
            blur.Radius = 5;
            Effect = blur;
        }

        private void Unblur()
        {
            Effect = null;
        }

        public double MaxOpacity { get; set; }
        #endregion
    }
}
