using System;
using System.Windows.Input;

namespace WindowResizerShared
{
    public class KeyPressedEventArgs : EventArgs
    {
        public Key KeyPressed { get; private set; }

        public KeyPressedEventArgs(Key key)
        {
            KeyPressed = key;
        }
    }
}
