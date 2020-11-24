using System.Windows.Forms;

namespace ManagedShell.Common.Helpers
{
    public class NativeWindowEx : NativeWindow
    {
        public delegate void MessageReceivedEventHandler(Message m);

        public event MessageReceivedEventHandler MessageReceived;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            MessageReceived?.Invoke(m);
        }

        public override void CreateHandle(CreateParams cp)
        {
            base.CreateHandle(cp);
        }
    }
}