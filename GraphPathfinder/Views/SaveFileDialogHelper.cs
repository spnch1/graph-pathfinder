using Microsoft.Win32;

namespace GraphPathfinder.Views
{
    public static class SaveFileDialogHelper
    {
        public static string? ShowSaveDialog(string defaultFileName = "PathResult.txt")
        {
            var dlg = new SaveFileDialog
            {
                FileName = defaultFileName,
                DefaultExt = ".txt",
                Filter = "Text documents (*.txt)|*.txt|All files (*.*)|*.*"
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }
    }
}
