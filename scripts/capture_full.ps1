$cs = @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class FullScreenCapture {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetThreadDesktop(IntPtr hDesktop);

    [DllImport("user32.dll")]
    public static extern bool CloseDesktop(IntPtr hDesktop);

    public static void Capture(string outPath) {
        IntPtr hDesk = OpenDesktop("Default", 0, false, 0x01FF);
        System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(() => {
            if (hDesk != IntPtr.Zero) {
                SetThreadDesktop(hDesk);
            }
            try {
                int width = Screen.PrimaryScreen.Bounds.Width;
                int height = Screen.PrimaryScreen.Bounds.Height;
                using (Bitmap bmp = new Bitmap(width, height)) {
                    using (Graphics g = Graphics.FromImage(bmp)) {
                        g.CopyFromScreen(0, 0, 0, 0, new Size(width, height));
                    }
                    bmp.Save(outPath, ImageFormat.Png);
                    Console.WriteLine("Saved screenshot to: " + outPath);
                }
            } catch (Exception ex) {
                Console.WriteLine("Capture error: " + ex.ToString());
            } finally {
                if (hDesk != IntPtr.Zero) {
                    CloseDesktop(hDesk);
                }
            }
        }));

        t.SetApartmentState(System.Threading.ApartmentState.STA);
        t.Start();
        t.Join();
    }
}
'@

$out = $args[0]
if (-not $out) {
    $out = "C:\Users\jinu\.gemini\antigravity\brain\fb3e784e-fc0f-432c-ba29-eab007bd245e\screen_now.png"
}

Add-Type -TypeDefinition $cs -ReferencedAssemblies System.Drawing, System.Windows.Forms
[FullScreenCapture]::Capture($out)
