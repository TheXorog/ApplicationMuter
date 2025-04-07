using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NativeMethods;

namespace ApplicationMuter;
public partial class Overlay : Form
{
    public Overlay()
    {
        InitializeComponent();
        this.Opacity = 0.5;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams createParams = base.CreateParams;
            createParams.ExStyle |= (int)(
                WindowStylesEx.WS_EX_TOPMOST |
                WindowStylesEx.WS_EX_LAYERED |
                WindowStylesEx.WS_EX_TRANSPARENT |
                WindowStylesEx.WS_EX_TOOLWINDOW);
            return createParams;
        }
    }

    public static class WindowStylesEx
    {
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x80;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_NOACTIVATE = 0x0010;
    }

    private void Overlay_Load(object sender, EventArgs e)
    {
        this.Opacity = 0;

        Screen[] screens = Screen.AllScreens;
        if (screens.Length > 0)
        {
            Screen firstScreen = screens[0];
            this.StartPosition = FormStartPosition.Manual;
            this.Location = new System.Drawing.Point(firstScreen.Bounds.Left + 20, firstScreen.Bounds.Top + 20);
        }
    }

    Stopwatch? currentShowTime = null;
    nint? parentWindow = null;
    string? parentWindowName = null;

    public void ShowNotification(string message, string parentWindowName, int parentProcessId, nint? parentWindowHandler)
    {
        var action = async () =>
        {
            this.parentWindow = parentWindowHandler;
            this.label1.Text = message;

            bool Reposition()
            {
                void DefaultPosition()
                {
                    Screen[] screens = Screen.AllScreens;
                    if (screens.Length > 0)
                    {
                        var firstScreen = screens[0];
                        int left = firstScreen.Bounds.Left;
                        int top = firstScreen.Bounds.Top;
                        int width = firstScreen.Bounds.Right - firstScreen.Bounds.Left;
                        int height = firstScreen.Bounds.Bottom - firstScreen.Bounds.Top;

                        var newLeft = left + (width / 2) - (this.Width / 2);
                        var newTop = top + (height / 2) - (this.Height / 2) + 50;

                        if (this.Location.X == newLeft && this.Location.Y == newTop)
                            return;

                        Console.WriteLine($"Default New X, Y: {newTop}, {newLeft}");
                        this.Location = new Point(newLeft, newTop);
                    }
                    return;
                }

                if (this.parentWindow is null || !NativeMethods.IsWindowVisible(this.parentWindow.Value) || NativeMethods.GetForegroundWindow() != this.parentWindow.Value)
                {
                    DefaultPosition();
                    return false;
                }

                var success = NativeMethods.GetWindowRect(this.parentWindow.Value, out var windowRectangle);

                if (!success)
                {
                    DefaultPosition();
                    return false;
                }

                int left = windowRectangle.Left;
                int top = windowRectangle.Top;
                int width = windowRectangle.Right - windowRectangle.Left;
                int height = windowRectangle.Bottom - windowRectangle.Top;

                if (left == -32000 || top == -32000)
                {
                    DefaultPosition();
                    return false;
                }

                var newLeft = left + (width / 2) - (this.Width / 2);
                var newTop = top + height - this.Height - 20;

                if (this.Location.X == newLeft && this.Location.Y == newTop)
                    return true;

                Console.WriteLine($"New X, Y: {newTop}, {newLeft}");
                this.Location = new Point(newLeft, newTop);
                return true;
            }

            if (parentWindowName != this.parentWindowName)
            {
                Console.WriteLine($"New Window: {parentWindowName}");
                Icon? icon = null;

                try
                {
                    var path = Program.GetProcessName(parentProcessId);
                    icon = path is not null ? Icon.ExtractAssociatedIcon(path) : null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                var newHeight = icon?.Height ?? this.Height;

                if (newHeight < 50)
                    newHeight = 50;

                if (newHeight > 80)
                    newHeight = 80;

                this.Height = newHeight;
                this.pictureBox1.Width = this.pictureBox1.Height;

                this.Width = TextRenderer.MeasureText(message, this.label1.Font).Width + this.pictureBox1.Width + 20;
                this.label1.Width = this.Width - this.pictureBox1.Width;

                this.Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 5, 5));

                var iconFailed = false;
                if (icon is not null)
                {
                    try
                    {
                        var bitmap = icon.ToBitmap();
                        this.pictureBox1.Image = bitmap;

                        int width = bitmap.Width;
                        int height = bitmap.Height;

                        var colorCounts = new Dictionary<Color, int>();

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                var pixelColor = bitmap.GetPixel(x, y);

                                if (pixelColor != Color.Transparent)
                                {
                                    if (colorCounts.ContainsKey(pixelColor))
                                    {
                                        colorCounts[pixelColor]++;
                                    }
                                    else
                                    {
                                        colorCounts[pixelColor] = 1;
                                    }
                                }
                            }
                        }

                        var mostUsedColor = colorCounts.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;
                        this.pictureBox1.BackColor = this.DarkenColor(mostUsedColor, 0.5);
                        this.label1.BackColor = this.DarkenColor(mostUsedColor, 0.7);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        iconFailed = true;
                    }
                }
                else
                {
                    iconFailed = true;
                }

                if (iconFailed)
                {
                    this.pictureBox1.Image = null;
                    this.pictureBox1.BackColor = Color.Black;
                    this.label1.BackColor = Color.Black;
                }

                this.parentWindowName = parentWindowName;
            }

            if (this.currentShowTime != null)
            {
                this.currentShowTime.Restart();
                return;
            }

            this.currentShowTime = Stopwatch.StartNew();

            Console.WriteLine("Showing Window");
            this.KeepOnTop();

            while (this.Opacity < 0.8)
            {
                Reposition();
                this.Opacity += 0.08;
                await Task.Delay(1);
            }

            while (this.currentShowTime.ElapsedMilliseconds < 3000 && Reposition())
            {
                await Task.Delay(1);
            }

            this.currentShowTime = null;
            this.parentWindowName = null;

            while (this.Opacity > 0.0 && this.currentShowTime == null && Reposition())
            {
                this.Opacity -= 0.12;
                await Task.Delay(1);
            }

            this.Opacity = 0;
            Console.WriteLine("Hidden Window");
        };

        this.Invoke(action);
    }

    Color DarkenColor(Color color, double factor)
    {
        // Calculate new RGB values by multiplying the current values by the factor
        int newR = (int)(color.R * factor);
        int newG = (int)(color.G * factor);
        int newB = (int)(color.B * factor);

        // Ensure the values stay within the valid range (0-255)
        newR = Math.Max(0, Math.Min(255, newR));
        newG = Math.Max(0, Math.Min(255, newG));
        newB = Math.Max(0, Math.Min(255, newB));

        // Return the new color
        return Color.FromArgb(newR, newG, newB);
    }

    public void KeepOnTop()
    {
        NativeMethods.SetWindowPos(this.Handle, WindowStylesEx.HWND_TOPMOST, 0, 0, 0, 0, WindowStylesEx.SWP_NOMOVE | WindowStylesEx.SWP_NOSIZE | WindowStylesEx.SWP_NOACTIVATE | WindowStylesEx.SWP_SHOWWINDOW);
    }

    [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
    private static extern IntPtr CreateRoundRectRgn
(
    int nLeftRect,     // x-coordinate of upper-left corner
    int nTopRect,      // y-coordinate of upper-left corner
    int nRightRect,    // x-coordinate of lower-right corner
    int nBottomRect,   // y-coordinate of lower-right corner
    int nWidthEllipse, // width of ellipse
    int nHeightEllipse // height of ellipse
);
}
