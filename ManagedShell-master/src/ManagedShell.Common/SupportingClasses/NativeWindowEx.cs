using System.Windows.Forms;

namespace ManagedShell.Common.SupportingClasses
{
    public class NativeWindowEx : NativeWindow
    {
        public delegate void MessageReceivedEventHandler(ref Message m, ref bool handled);

        public event MessageReceivedEventHandler MessageReceived;

        protected override void WndProc(ref Message m)
        {
            bool handled = false;
            MessageReceived?.Invoke(ref m, ref handled);

            if (!handled)
            {
                base.WndProc(ref m);
            }
        }

        public override void CreateHandle(CreateParams cp)
        {
            base.CreateHandle(cp);
        }
    }
}