using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Controls.Primitives;
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
        }

        protected override void OnInitialized(EventArgs e)
        {
#if DEBUG
            AllowsTransparency = false;
            Topmost = false;
            ResizeMode = ResizeMode.CanResize;
            WindowStyle = WindowStyle.ThreeDBorderWindow;
            WindowState = WindowState.Normal;
#endif
            MaxOpacity = Opacity;
            EnterInsertMode();
            _keyBindings = new ViBindings();
            Editor.PreviewKeyDown += CaptureSendCommand;
            //EventManager.RegisterClassHandler(typeof(UIElement), UIElement.PreviewKeyUpEvent,
            //    new RoutedEventHandler(RedirectAllInputToEditor), handledEventsToo: false);
            Editor.PreviewLostKeyboardFocus += (_, __) => ExitInsertMode();
            SetupPSash();
            _psash.Runspace.AvailabilityChanged += (_, ev) =>
            {
                if(ev.RunspaceAvailability == RunspaceAvailability.Available)
                   Dispatcher.InvokeAsync(() => SetPrompt());
            };
            base.OnInitialized(e);
        }

        private void SetPrompt()
        {
            Prompt.Content = _psash.Runspace.SessionStateProxy.Path.CurrentLocation;
        }

        #region key bindings
        void ExitInsertMode()
        {
            PreviewKeyUp -= CaptureEscapeInsertMode;
            PreviewKeyUp += CaptureEnterInsertMode;
            Editor.IsReadOnly = Editor.IsReadOnlyCaretVisible = true;
        }

        void EnterInsertMode()
        {
            Editor.IsReadOnly = Editor.IsReadOnlyCaretVisible = false;
            PreviewKeyUp += CaptureEscapeInsertMode;
            PreviewKeyUp -= CaptureEnterInsertMode;
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
            TextRange endOfPreviousLine = null;
            TextPointer frontEndOfPreviousLine = prevEnd;
            do
            {
                frontEndOfPreviousLine = frontEndOfPreviousLine.GetPositionAtOffset(-1);
                if (frontEndOfPreviousLine == null) //reached beginning of document
                    break;
                endOfPreviousLine = new TextRange(frontEndOfPreviousLine, prevEnd);
            } while (String.IsNullOrWhiteSpace(endOfPreviousLine.Text));
            if (endOfPreviousLine != null && endOfPreviousLine.Text[0] == '|')
                lineStart = GetInputLine(frontEndOfPreviousLine).Start;
            return lineStart;
        }

        private void CreateCurrentInput(TextRange range)
        {
            var text = range.Text;
            range.Text = String.Empty;//TODO: would rather wrap the range in situ, but how?
            CurrentInput = new Run(text, range.Start);
        }
        #endregion

        #region powershell host
        private PSashHost _psash;
        private void SetupPSash()
        {
            _psash = new PSashHost(new VisualizationFactory(new VisualizationContainer(OutputContainer)));
            _psash.Exit += (_, i) => Environment.Exit(i);
        }

        private void SendCommand()
        {
            var input = GetCurrentInput();
            if (String.IsNullOrWhiteSpace(input))
                return;
            var task = _psash.Execute(input);
            task.ContinueWith(_ => Dispatcher.InvokeAsync(() => SetPrompt()));
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
            if (msg == App.NativeMethods.WM_SHOWME)
            {
                if (Visibility.Visible != Visibility) ToggleVisibility(fadeDurationMillis: 200);
                handled = true;
            }
            return IntPtr.Zero;
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            if(Visibility.Visible == Visibility) ToggleVisibility(fadeDurationMillis: 200);
        }

        private void ToggleVisibility(int fadeDurationMillis = 400)
        {
            Duration fadeDuration = TimeSpan.FromMilliseconds(fadeDurationMillis);
            if (Visibility.Visible == Visibility) FadeOut(fadeDuration);
            else 
            {
                FadeIn(fadeDuration);
                Activate();
            };
        }

        private void FadeOut(Duration fadeDuration)
        {
            Blur();
            var fade = new DoubleAnimation(0, fadeDuration);
            fade.Completed += (s, _) => Visibility = Visibility.Hidden;
            BeginAnimation(UIElement.OpacityProperty, fade);
        }

        private void FadeIn(Duration fadeDuration)
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
