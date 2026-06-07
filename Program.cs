using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BackupTool;

public class Program
{
    [STAThread]
    static void Main()
    {
        var app = new Application();
        app.Run(new MainWindow());
    }
}

public partial class MainWindow : Window
{
    private TextBox addressInput = null!;
    private TextBox logOutput = null!;
    
    public MainWindow()
    {
        Title = "Backup Tool";
        Width = 800;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        
        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });
        
        grid.Children.Add(new TextBlock { Text = "Address List (Line 1=Name, Line 2=Path):", FontWeight = FontWeights.Bold });
        
        addressInput = new TextBox { AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 5, 0, 5) };
        Grid.SetRow(addressInput, 1);
        grid.Children.Add(addressInput);
        
        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
        
        var loadBtn = new Button { Content = "Load File", Width = 80, Margin = new Thickness(0, 0, 5, 0) };
        loadBtn.Click += (s, e) => LoadFile();
        buttonPanel.Children.Add(loadBtn);
        
        var saveBtn = new Button { Content = "Save File", Width = 80, Margin = new Thickness(0, 0, 5, 0) };
        saveBtn.Click += (s, e) => SaveFile();
        buttonPanel.Children.Add(saveBtn);
        
        var sampleBtn = new Button { Content = "Sample", Width = 80, Margin = new Thickness(0, 0, 20, 0) };
        sampleBtn.Click += (s, e) => SetSample();
        buttonPanel.Children.Add(sampleBtn);
        
        var startBtn = new Button { Content = "Start Backup", Width = 120, Background = System.Windows.Media.Brushes.LightGreen };
        startBtn.Click += (s, e) => StartBackup();
        buttonPanel.Children.Add(startBtn);
        
        Grid.SetRow(buttonPanel, 2);
        grid.Children.Add(buttonPanel);
        
        grid.Children.Add(new TextBlock { Text = "Log:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 5) });
        
        logOutput = new TextBox { IsReadOnly = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 20, 0, 0) };
        Grid.SetRow(logOutput, 3);
        grid.Children.Add(logOutput);
        
        Content = grid;
        
        if (File.Exists("address.txt"))
            addressInput.Text = File.ReadAllText("address.txt");
        else
            SetSample();
    }
    
    private void StartBackup()
    {
        var lines = addressInput.Text.Split('\n');
        logOutput.Clear();
        
        int success = 0, fail = 0;
        
        for (int i = 1; i < lines.Length; i += 2)
        {
            var path = lines[i].Trim();
            if (string.IsNullOrEmpty(path)) continue;
            
            var name = i > 0 ? lines[i-1].Trim() : "backup";
            
            Log($"[{i/2 + 1}] {name}");
            Log($"    Path: {path}");
            
            if (path.StartsWith("HKEY_") || path.StartsWith("HK"))
            {
                Directory.CreateDirectory("Registry");
                var psi = new ProcessStartInfo("reg", $"export \"{path}\" \"Registry\\{name}.reg\" /y")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var p = Process.Start(psi);
                p?.WaitForExit();
                
                if (p?.ExitCode == 0 && File.Exists($"Registry\\{name}.reg"))
                {
                    Log($"    ✅ Saved: Registry\\{name}.reg");
                    success++;
                }
                else
                {
                    Log($"    ❌ Failed to export");
                    fail++;
                }
            }
            else if (Directory.Exists(path) || File.Exists(path))
            {
                var target = path.Replace(":", "").Replace("\\", "/").TrimStart('/');
                try
                {
                    CopyDirectory(path, target);
                    Log($"    ✅ Copied to: {target}");
                    success++;
                }
                catch (Exception ex)
                {
                    Log($"    ❌ Failed: {ex.Message}");
                    fail++;
                }
            }
            else
            {
                Log($"    ❌ Path not found");
                fail++;
            }
        }
        
        Log($"\n========== SUMMARY ==========");
        Log($"✅ Success: {success}  ❌ Failed: {fail}");
    }
    
    private void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src))
            File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(src))
            CopyDirectory(dir, Path.Combine(dst, Path.GetFileName(dir)));
    }
    
    private void LoadFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Text|*.txt" };
        if (dlg.ShowDialog() == true)
            addressInput.Text = File.ReadAllText(dlg.FileName);
    }
    
    private void SaveFile()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog { Filter = "Text|*.txt", FileName = "address.txt" };
        if (dlg.ShowDialog() == true)
            File.WriteAllText(dlg.FileName, addressInput.Text);
    }
    
    private void SetSample()
    {
        addressInput.Text = $"Windows Startup{Environment.NewLine}HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Run{Environment.NewLine}{Environment.NewLine}My Documents{Environment.NewLine}C:\\Users\\{Environment.UserName}\\Documents";
    }
    
    private void Log(string msg)
    {
        logOutput.AppendText(msg + Environment.NewLine);
    }
}
