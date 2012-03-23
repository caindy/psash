using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PSash
{
    public interface IPSashKeyBindings
    {
        Tuple<ModifierKeys, Key> EnterInsertModeAtCurrentCursorPosition { get; }
        Tuple<ModifierKeys, Key> ExitInsertMode { get; }
        Tuple<ModifierKeys, Key> SendCommand { get; }
    }
    class ViBindings : IPSashKeyBindings
    {

        public Tuple<ModifierKeys, Key> EnterInsertModeAtCurrentCursorPosition
        {
            get { return Tuple.Create(ModifierKeys.None, Key.I); }
        }

        public Tuple<ModifierKeys, Key> ExitInsertMode
        {
            get { return Tuple.Create(ModifierKeys.None, Key.Escape); }
        }

        public Tuple<ModifierKeys, Key> SendCommand
        {
            get { return Tuple.Create(ModifierKeys.Shift, Key.Enter); }
        }
    }
}
