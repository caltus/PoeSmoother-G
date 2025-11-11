using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using LibBundle3;
using LibBundledGGPK3;
using Microsoft.Win32;
using PoeSmoother.Patches;
using PoeSmoother.Models;

namespace PoeSmoother;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PatchViewModel> _patches;
    private string _ggpkPath = string.Empty;

    public MainWindow()
    {

        _patches = new ObservableCollection<PatchViewModel>();
        InitializeComponent();
        InitializePatches();
        PatchesItemsControl.ItemsSource = _patches;
        UpdateStatus();

        // Apply dark title bar
        Loaded += (s, e) => ApplyDarkTitleBar();
    }

    private void ApplyDarkTitleBar()
    {
        if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
        {
            IntPtr hwnd = hwndSource.Handle;

            // Use DWMWA_USE_IMMERSIVE_DARK_MODE (20) for Windows 11 / Windows 10 build 19041+
            int attribute = 20;
            int useImmersiveDarkMode = 1;
            DwmSetWindowAttribute(hwnd, attribute, ref useImmersiveDarkMode, sizeof(int));
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private void InitializePatches()
    {
        var patchInstances = new IPatch[]
        {
            new Camera(),
            new Fog(),
            new EnvironmentParticles(),
            new Minimap(),
            new Shadow(),
            new Light(),
            new Corpse(),
            new Delirium(),
            new Particles(),
        };

        foreach (var patch in patchInstances)
        {
            _patches.Add(new PatchViewModel(patch));
        }
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "GGPK Files (*.ggpk;*.bin)|*.ggpk;*.bin|All Files (*.*)|*.*",
            Title = "Select GGPK or Index File",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            _ggpkPath = openFileDialog.FileName;
            GgpkPathTextBox.Text = _ggpkPath;
            UpdateStatus();
        }
    }

    private void SelectAllButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var patch in _patches)
        {
            patch.IsSelected = true;
        }
    }

    private void SelectNoneButton_Click(object sender, RoutedEventArgs e)
    {
        foreach (var patch in _patches)
        {
            patch.IsSelected = false;
        }
    }

    private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomValueText != null)
        {
            ZoomValueText.Text = e.NewValue.ToString("F1").Replace(',', '.');
        }

        if (_patches != null)
        {
            foreach (var patch in _patches)
            {
                if (patch.Patch is Camera cameraPatch)
                {
                    cameraPatch.ZoomLevel = e.NewValue;
                }
            }
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedPatches = _patches.Where(p => p.IsSelected).ToList();

        if (selectedPatches.Count == 0)
        {
            MessageBox.Show("Please select at least one patch to apply.", "No Patches Selected",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ApplyPatches(selectedPatches);
    }

    private async Task ApplyPatches(List<PatchViewModel> patchesToApply)
    {
        if (string.IsNullOrEmpty(_ggpkPath) || !File.Exists(_ggpkPath))
        {
            MessageBox.Show("Please select a valid GGPK file first.", "Invalid File",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Disable buttons during operation
        ApplyButton.IsEnabled = false;
        ZoomSlider.IsEnabled = false;
        ProgressBar.Visibility = Visibility.Visible;
        ProgressBar.IsIndeterminate = false;
        ProgressBar.Minimum = 0;
        ProgressBar.Maximum = patchesToApply.Count;
        ProgressBar.Value = 0;

        try
        {
            await Task.Run(() =>
            {
                if (_ggpkPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                {
                    var index = new LibBundle3.Index(_ggpkPath);
                    PatchIndex(index, patchesToApply);
                    index.Dispose();
                }
                else if (_ggpkPath.EndsWith(".ggpk", StringComparison.OrdinalIgnoreCase))
                {
                    BundledGGPK ggpk = new(_ggpkPath);
                    var index = ggpk.Index;
                    PatchIndex(index, patchesToApply);
                    ggpk.Dispose();
                }
                else
                {
                    throw new InvalidDataException("The selected file is neither a GGPK nor an index BIN file.");
                }
            });

            StatusTextBlock.Text = $"Successfully applied {patchesToApply.Count} patch(es)!";
            MessageBox.Show($"Successfully applied {patchesToApply.Count} patch(es)!", "Success",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "Error occurred while applying patches.";
            MessageBox.Show($"Error applying patches:\n\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Re-enable buttons
            ApplyButton.IsEnabled = true;
            ProgressBar.Visibility = Visibility.Collapsed;
            ProgressBar.Value = 0;
            ZoomSlider.IsEnabled = true;
        }
    }

    private void PatchIndex(LibBundle3.Index index, List<PatchViewModel> patches)
    {
        var fileTree = index.BuildTree();

        for (int i = 0; i < patches.Count; i++)
        {
            var patch = patches[i];

            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = $"Applying {patch.Name} ({i + 1}/{patches.Count})...";
                ProgressBar.Value = i;
            });

            patch.Patch.Apply(fileTree);
            index.Save();

            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = i + 1;
            });
        }
    }

    private void UpdateStatus()
    {
        if (string.IsNullOrEmpty(_ggpkPath))
        {
            StatusTextBlock.Text = "Please select a GGPK file to begin.";
        }
        else
        {
            StatusTextBlock.Text = $"Ready - {Path.GetFileName(_ggpkPath)}";
        }
    }
}