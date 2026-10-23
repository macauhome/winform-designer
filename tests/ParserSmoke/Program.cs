using WinFormsDesigner.App;
using WinFormsDesigner.App.Designer;
using WinFormsDesigner.Core.Codegen;
using WinFormsDesigner.Core.Model;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var document = DesignerFileLoader.Load(Path.Combine(AppContext.BaseDirectory, "Form1.cs"))
            ?? throw new InvalidOperationException("The code-behind file did not load its paired designer file.");

        if (document.ClassName != "MainForm" || document.Namespace != "Sample.Forms")
        {
            throw new InvalidOperationException("The class or namespace was not loaded.");
        }

        if (!document.Root.Properties.TryGetValue("Font", out var font) || font is not System.Drawing.Font)
        {
            throw new InvalidOperationException("The form font was not parsed.");
        }

        var panel = document.Root.Children.Single();
        if (panel.ControlType != typeof(System.Windows.Forms.TableLayoutPanel) || panel.Children.Count != 2)
        {
            throw new InvalidOperationException("The Controls.Add and Controls.AddRange hierarchy was not loaded.");
        }

        var button = panel.Children.Single(node => node.Name == "button1");
        if (!button.Properties.TryGetValue("Text", out var buttonText) || buttonText is not string text || text != "Click me"
            || !button.Events.ContainsKey("Click"))
        {
            throw new InvalidOperationException("The button property or event handler was not loaded.");
        }

        var session = new DesignSession();
        session.LoadDocument(document);
        if (session.Document.ClassName != "MainForm"
            || session.RootForm.ClientSize != new System.Drawing.Size(320, 200)
            || session.RootForm.Controls.Count != 1)
        {
            throw new InvalidOperationException("The parsed document was not applied to the live form.");
        }

        var loadedButton = session.Document.FindNode("button1")
            ?? throw new InvalidOperationException("The loaded button is missing from the document.");
        session.SetProperty(loadedButton, "Text", "Edited");
        var generated = new CSharpFormGenerator().Generate(session.Document);
        if (!generated.Files[$"{session.Document.ClassName}.designer.cs"].Contains("Edited", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The loaded form did not produce editable designer code.");
        }

        var runtimeSession = new DesignSession();
        var node = runtimeSession.AddControl(typeof(System.Windows.Forms.Button), new System.Drawing.Point(40, 40));
        var runtimeSurface = new DesignerSurface { Dock = System.Windows.Forms.DockStyle.Fill, Session = runtimeSession };
        using var host = new System.Windows.Forms.Form { ClientSize = new System.Drawing.Size(1100, 750), ShowInTaskbar = false };
        host.Controls.Add(runtimeSurface);
        host.Show();
        System.Windows.Forms.Application.DoEvents();

        var liveButton = runtimeSession.GetLive(node) ?? throw new InvalidOperationException("The live control is missing.");
        var originalLocation = liveButton.Location;
        var start = runtimeSurface.PointToClient(liveButton.PointToScreen(new System.Drawing.Point(20, 10)));
        using (var initialImage = new System.Drawing.Bitmap(runtimeSurface.Width, runtimeSurface.Height))
        {
            runtimeSurface.DrawToBitmap(initialImage, runtimeSurface.ClientRectangle);
            var blankFormPixel = initialImage.GetPixel(start.X + 250, start.Y + 200);
            if (blankFormPixel.R < 80 && blankFormPixel.G < 80 && blankFormPixel.B < 80)
            {
                throw new InvalidOperationException("The transparent adornment layer is covering the form with black.");
            }
        }

        SendMouse(runtimeSurface, 0x0201, new System.Drawing.Point(20, 10), liveButton);
        SendMouse(runtimeSurface, 0x0200, new System.Drawing.Point(start.X + 23, start.Y + 17), runtimeSurface);
        SendMouse(runtimeSurface, 0x0202, new System.Drawing.Point(start.X + 23, start.Y + 17), runtimeSurface);

        liveButton = runtimeSession.GetLive(node) ?? throw new InvalidOperationException("The moved control is missing.");
        if (liveButton.Location != originalLocation + new System.Drawing.Size(23, 17))
        {
            throw new InvalidOperationException(
                $"Moving the control failed: expected {originalLocation + new System.Drawing.Size(23, 17)}, got {liveButton.Location}.");
        }

        if (runtimeSurface.Cursor.Handle != System.Windows.Forms.Cursors.Default.Handle
            || System.Windows.Forms.Cursor.Current?.Handle != System.Windows.Forms.Cursors.Default.Handle)
        {
            throw new InvalidOperationException(
                $"Restoring the cursor failed: surface={runtimeSurface.Cursor.Handle}, current={System.Windows.Forms.Cursor.Current?.Handle}.");
        }

        using (var surfaceImage = new System.Drawing.Bitmap(runtimeSurface.Width, runtimeSurface.Height))
        {
            runtimeSurface.DrawToBitmap(surfaceImage, runtimeSurface.ClientRectangle);
            var oldButton = runtimeSurface.PointToClient(
                liveButton.Parent!.PointToScreen(originalLocation));
            var darkPixels = 0;
            for (var y = oldButton.Y + 3; y < oldButton.Y + 16; y++)
            {
                for (var x = oldButton.X + 3; x < oldButton.X + liveButton.Width - 3; x++)
                {
                    var pixel = surfaceImage.GetPixel(x, y);
                    if (pixel.R < 128 && pixel.G < 128 && pixel.B < 128)
                    {
                        darkPixels++;
                    }
                }
            }

            if (darkPixels > 4)
            {
                throw new InvalidOperationException($"The old control area was not repainted ({darkPixels} dark pixels remain).");
            }
        }

        runtimeSession.Undo();

        node = runtimeSession.Document.FindNode("button1")
            ?? throw new InvalidOperationException("The moved control was not restored by undo.");
        liveButton = runtimeSession.GetLive(node) ?? throw new InvalidOperationException("The resized control is missing.");
        var overlay = runtimeSurface.Controls[0].Controls[0];
        var originalButtonSize = liveButton.Size;
        var buttonBottomRight = runtimeSurface.PointToClient(
            liveButton.Parent!.PointToScreen(new System.Drawing.Point(liveButton.Right - 4, liveButton.Bottom - 4)));
        SendMouse(runtimeSurface, 0x0201, ToTargetPoint(overlay, runtimeSurface, buttonBottomRight), overlay);
        SendMouse(runtimeSurface, 0x0200, buttonBottomRight + new System.Drawing.Size(28, 20), runtimeSurface);
        SendMouse(runtimeSurface, 0x0202, buttonBottomRight + new System.Drawing.Size(28, 20), runtimeSurface);
        if (liveButton.Size != originalButtonSize + new System.Drawing.Size(28, 20))
        {
            throw new InvalidOperationException(
                $"Resizing the control failed: expected {originalButtonSize + new System.Drawing.Size(28, 20)}, got {liveButton.Size}.");
        }

        runtimeSession.Undo();
        var root = runtimeSession.RootForm;
        var originalClientSize = root.ClientSize;
        var rootRight = runtimeSurface.PointToClient(
            root.PointToScreen(new System.Drawing.Point(root.Width - 4, root.Height - 4)));
        SendMouse(runtimeSurface, 0x0201, ToTargetPoint(overlay, runtimeSurface, rootRight), overlay);
        SendMouse(runtimeSurface, 0x0200, new System.Drawing.Point(rootRight.X + 32, rootRight.Y + 16), runtimeSurface);
        SendMouse(runtimeSurface, 0x0202, new System.Drawing.Point(rootRight.X + 32, rootRight.Y + 16), runtimeSurface);

        if (root.ClientSize != originalClientSize + new System.Drawing.Size(32, 16))
        {
            throw new InvalidOperationException("Resizing the form failed.");
        }

        runtimeSession.Undo();
        if (root.ClientSize != originalClientSize)
        {
            throw new InvalidOperationException("Resizing the form did not create one undoable edit.");
        }

        host.Close();
        session.RootForm.Dispose();
        Console.WriteLine("Parser, repaint, drag/resize, cursor restore, and undo smoke tests passed.");
    }

    private static void SendMouse(DesignerSurface surface, int message, System.Drawing.Point point, System.Windows.Forms.Control target)
    {
        var lParam = (point.X & 0xFFFF) | ((point.Y & 0xFFFF) << 16);
        if (!PostMessage(target.Handle, message, IntPtr.Zero, new IntPtr(lParam)))
        {
            throw new InvalidOperationException("The pointer message could not be queued.");
        }

        System.Windows.Forms.Application.DoEvents();
    }

    private static System.Drawing.Point ToTargetPoint(
        System.Windows.Forms.Control target,
        DesignerSurface surface,
        System.Drawing.Point surfacePoint)
        => target.PointToClient(surface.PointToScreen(surfacePoint));

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
}
