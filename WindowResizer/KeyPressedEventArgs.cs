using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WindowResizer
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
