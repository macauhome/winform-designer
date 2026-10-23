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
        var panelNode = runtimeSession.AddControl(typeof(System.Windows.Forms.Panel), new System.Drawing.Point(100, 100));
        var insertionParent = runtimeSession.GetInsertionParent();
        if (!ReferenceEquals(insertionParent, panelNode))
        {
            throw new InvalidOperationException("New controls should be inserted into the selected container.");
        }

        var insertionHost = runtimeSession.GetLive(insertionParent)
            ?? throw new InvalidOperationException("The insertion container has no live instance.");
        using var defaultButton = new System.Windows.Forms.Button();
        var defaultButtonSize = defaultButton.Size;
        var insertionLocation = new System.Drawing.Point(
            Math.Max(0, (insertionHost.ClientSize.Width - defaultButtonSize.Width) / 2),
            Math.Max(0, (insertionHost.ClientSize.Height - defaultButtonSize.Height) / 2));
        var node = runtimeSession.AddControl(typeof(System.Windows.Forms.Button), insertionLocation);
        var runtimeSurface = new DesignerSurface { Dock = System.Windows.Forms.DockStyle.Fill, Session = runtimeSession };
        var pointerMoveCount = 0;
        runtimeSurface.PointerMoved += (_, _) => pointerMoveCount++;
        if (runtimeSurface.Controls.Count != 1 || runtimeSurface.Controls[0].Controls.Count != 0)
        {
            throw new InvalidOperationException("The design canvas is still hosting live form/control windows.");
        }

        var canvas = runtimeSurface.Controls[0];
        using var host = new System.Windows.Forms.Form { ClientSize = new System.Drawing.Size(1100, 750), ShowInTaskbar = false };
        host.Controls.Add(runtimeSurface);
        host.Show();
        System.Windows.Forms.Application.DoEvents();

        var liveButton = runtimeSession.GetLive(node) ?? throw new InvalidOperationException("The live control is missing.");
        var livePanel = runtimeSession.GetLive(panelNode) ?? throw new InvalidOperationException("The container control is missing.");
        if (!livePanel.ClientRectangle.Contains(liveButton.Bounds))
        {
            throw new InvalidOperationException("The centered new control was positioned outside the selected container.");
        }
        var originalLocation = liveButton.Location;
        var canvasOrigin = new System.Drawing.Size(24, 24);
        var buttonOrigin = new System.Drawing.Size(
            canvasOrigin.Width + livePanel.Left,
            canvasOrigin.Height + livePanel.Top);
        var start = originalLocation + buttonOrigin + new System.Drawing.Size(20, 10);
        using (var initialImage = new System.Drawing.Bitmap(runtimeSurface.Width, runtimeSurface.Height))
        {
            runtimeSurface.DrawToBitmap(initialImage, runtimeSurface.ClientRectangle);
            var blankFormPixel = initialImage.GetPixel(start.X + 250, start.Y + 200);
            if (blankFormPixel.R < 80 && blankFormPixel.G < 80 && blankFormPixel.B < 80)
            {
                throw new InvalidOperationException("The form background is rendering black.");
            }

            var visibleButtonPixels = 0;
            for (var y = buttonOrigin.Height + originalLocation.Y; y < buttonOrigin.Height + originalLocation.Y + liveButton.Height; y++)
            {
                for (var x = buttonOrigin.Width + originalLocation.X; x < buttonOrigin.Width + originalLocation.X + liveButton.Width; x++)
                {
                    if (initialImage.GetPixel(x, y).ToArgb() != blankFormPixel.ToArgb())
                    {
                        visibleButtonPixels++;
                    }
                }
            }

            if (visibleButtonPixels < 20)
            {
                throw new InvalidOperationException(
                    $"A newly added button did not render visibly ({visibleButtonPixels} pixels differed from the form background).");
            }
        }

        SendMouse(runtimeSurface, 0x0201, start, canvas);
        if (!canvas.Capture)
        {
            throw new InvalidOperationException(
                $"The canvas did not capture the mouse after button-down at {start}; canvas size={canvas.Size}, host active={host.ContainsFocus}.");
        }

        SendMouse(runtimeSurface, 0x0200, start + new System.Drawing.Size(23, 17), canvas);
        if (pointerMoveCount == 0)
        {
            throw new InvalidOperationException("The canvas did not raise PointerMoved for the queued mouse-move.");
        }
        if (liveButton.Location == originalLocation)
        {
            throw new InvalidOperationException(
                $"Pointer moved but the node bounds did not change (captured={canvas.Capture}, selected={runtimeSession.SelectedNode?.Name}).");
        }

        SendMouse(runtimeSurface, 0x0202, start + new System.Drawing.Size(23, 17), canvas);

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
            var oldButton = originalLocation + buttonOrigin;
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
        panelNode = runtimeSession.Document.FindNode("panel1")
            ?? throw new InvalidOperationException("The container was not restored by undo.");
        livePanel = runtimeSession.GetLive(panelNode) ?? throw new InvalidOperationException("The container was not restored by undo.");
        var originalButtonSize = liveButton.Size;
        buttonOrigin = new System.Drawing.Size(
            canvasOrigin.Width + livePanel.Left,
            canvasOrigin.Height + livePanel.Top);
        var buttonBottomRight = new System.Drawing.Point(liveButton.Right - 4, liveButton.Bottom - 4) + buttonOrigin;
        SendMouse(runtimeSurface, 0x0201, buttonBottomRight, canvas);
        SendMouse(runtimeSurface, 0x0200, buttonBottomRight + new System.Drawing.Size(28, 20), canvas);
        SendMouse(runtimeSurface, 0x0202, buttonBottomRight + new System.Drawing.Size(28, 20), canvas);
        if (liveButton.Size != originalButtonSize + new System.Drawing.Size(28, 20))
        {
            throw new InvalidOperationException(
                $"Resizing the control failed: expected {originalButtonSize + new System.Drawing.Size(28, 20)}, got {liveButton.Size}.");
        }

        runtimeSession.Undo();
        var root = runtimeSession.RootForm;
        var originalClientSize = root.ClientSize;
        var formMoveStart = new System.Drawing.Point(500, 300) + canvasOrigin;
        var formMoveDelta = new System.Drawing.Size(20, 15);
        SendMouse(runtimeSurface, 0x0201, formMoveStart, canvas);
        SendMouse(runtimeSurface, 0x0200, formMoveStart + formMoveDelta, canvas);
        SendMouse(runtimeSurface, 0x0202, formMoveStart + formMoveDelta, canvas);

        var movedFormOrigin = canvasOrigin + formMoveDelta;
        var rootRight = new System.Drawing.Point(root.Width - 4, root.Height - 4) + movedFormOrigin;
        SendMouse(runtimeSurface, 0x0201, rootRight, canvas);
        SendMouse(runtimeSurface, 0x0200, rootRight + new System.Drawing.Size(32, 16), canvas);
        SendMouse(runtimeSurface, 0x0202, rootRight + new System.Drawing.Size(32, 16), canvas);

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

    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);
}
