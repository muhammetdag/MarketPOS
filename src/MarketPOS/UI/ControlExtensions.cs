using System.Runtime.InteropServices;

namespace MarketPOS.UI;

public static class ControlExtensions
{
    private const int EM_SETCUEBANNER = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

    public static void SetPlaceholder(this TextBox textBox, string placeholder)
    {
#if NETCOREAPP
        textBox.PlaceholderText = placeholder;
#else
        if (textBox.IsHandleCreated)
        {
            SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
        }
        else
        {
            textBox.HandleCreated += (s, e) => SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, placeholder);
        }
#endif
    }
}
