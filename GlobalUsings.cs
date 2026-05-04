// Fichier de résolution des ambiguïtés de namespaces.
// Nécessaire car le projet active à la fois UseWPF et UseWindowsForms,
// ce qui cause des conflits sur de nombreux types communs.

global using Application    = System.Windows.Application;
global using Color          = System.Windows.Media.Color;
global using ColorConverter = System.Windows.Media.ColorConverter;
global using Point          = System.Windows.Point;
global using Rect           = System.Windows.Rect;
global using Brushes        = System.Windows.Media.Brushes;
global using Cursors        = System.Windows.Input.Cursors;
global using KeyEventArgs   = System.Windows.Input.KeyEventArgs;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;
global using MessageBox     = System.Windows.MessageBox;
global using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
global using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
