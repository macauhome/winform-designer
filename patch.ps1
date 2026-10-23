$content = Get-Content 'D:\Ken\Ken\MyPrograms\winformdesginer\src\WinFormsDesigner.App\Designer\DesignerSurface.cs' -Raw
$old = "/// <summary>Converts a rectangle in form coordinates into surface coordinates.</summary>
        public Rectangle ToSurfaceRectangle(Rectangle formRectangle)
        {
            if (_session is null)
            {
                return formRectangle;
            }

            var screen = _session.RootForm.PointToScreen(formRectangle.Location);
            var location = PointToClient(screen);
            return new Rectangle(location, formRectangle.Size);
        }

        protected override void OnMouseLeave(EventArgs e)"
$new = "/// <summary>Converts a rectangle in form coordinates into surface coordinates.</summary>
        public Rectangle ToSurfaceRectangle(Rectangle formRectangle)
        {
            if (_session is null)
            {
                return formRectangle;
            }

            var screen = _session.RootForm.PointToScreen(formRectangle.Location);
            var location = PointToClient(screen);
            return new Rectangle(location, formRectangle.Size);
        }

        /// <summary>Converts a rectangle in a control's parent coordinates into surface coordinates.</summary>
        public Rectangle ToSurfaceRectangle(Control control, Rectangle boundsInParent)
        {
            if (_session is null)
            {
                return boundsInParent;
            }

            var screen = control.Parent?.PointToScreen(boundsInParent.Location) ?? control.PointToScreen(boundsInParent.Location);
            var location = PointToClient(screen);
            return new Rectangle(location, boundsInParent.Size);
        }

        protected override void OnMouseLeave(EventArgs e)"
$content = $content.Replace($old, $new)
Set-Content 'D:\Ken\Ken\MyPrograms\winformdesginer\src\WinFormsDesigner.App\Designer\DesignerSurface.cs' -Value $content
Write-Host 'Done'