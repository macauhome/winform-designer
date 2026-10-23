$content = Get-Content 'D:\Ken\Ken\MyPrograms\winformdesginer\src\WinFormsDesigner.App\Designer\DesignerSurface.cs' -Raw
$content = $content.Replace('ToSurfaceRectangle(selected.Bounds)', 'ToSurfaceRectangle(selected, selected.Bounds)')
$content = $content.Replace('ToSurfaceRectangle(_hovered.Bounds)', 'ToSurfaceRectangle(_hovered, _hovered.Bounds)')
$content = $content.Replace('ToSurfaceRectangle(live.Bounds)', 'ToSurfaceRectangle(live, live.Bounds)')
Set-Content 'D:\Ken\Ken\MyPrograms\winformdesginer\src\WinFormsDesigner.App\Designer\DesignerSurface.cs' -Value $content
Write-Host 'Done'