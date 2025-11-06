using System;
using System.IO;
using System.Linq;
using LibBundle3.Nodes;
using LibBundledGGPK3;
using PoeSmoother.Patches;

namespace PoeSmoother;
//C:\\Program Files (x86)\\Steam\\steamapps\\common\\Path of Exile\\Bundles2\\_.index.bin
public static class Program
{
    public static void Main()
    {
        try
        {
            Console.WriteLine("=== Poe Smoother ===");
            Console.WriteLine();

            // Получение пути к GGPK
            string ggpkPath = GetGgpkPath();
            if (!File.Exists(ggpkPath))
            {
                Console.WriteLine("File not found.");
                WaitForExit();
                return;
            }

            // Инициализация системы патчей
            var patches = InitializePatches();
            if (patches.Length == 0)
            {
                Console.WriteLine("No patches found.");
                WaitForExit();
                return;
            }

            // Отображение меню
            ShowMainMenu(ggpkPath, patches);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine("An error occurred:");
            Console.Error.WriteLine(e);
            WaitForExit();
        }
    }

    private static string GetGgpkPath()
    {
        Console.Write("Enter the path to the GGPK file: ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    private static IPatch[] InitializePatches()
    {
        return new IPatch[]
        {
            new Camera(),
            new Fog(),
            new EnvironmentParticles(),
            new Minimap(),
            new Shadow(),
            new Light(),
            new Corpse(),
            new Delirium(),
        };
    }

    private static void ShowMainMenu(string ggpkPath, IPatch[] patches)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("=== Main Menu ===");
            Console.WriteLine($"Current GGPK: {Path.GetFileName(ggpkPath)}");
            Console.WriteLine();
            Console.WriteLine("Available actions:");
            Console.WriteLine("1 - Show patch information");
            Console.WriteLine("2 - Apply all patches");
            Console.WriteLine("3 - Apply selected patches");
            Console.WriteLine("4 - Change GGPK path");
            Console.WriteLine("0 - Exit");
            Console.WriteLine();
            Console.Write("Select an action: ");

            var input = Console.ReadLine();
            switch (input)
            {
                case "1":
                    ShowPatchesInfo(patches);
                    break;
                case "2":
                    ApplyAllPatches(ggpkPath, patches);
                    break;
                case "3":
                    ApplySelectedPatches(ggpkPath, patches);
                    break;
                case "4":
                    ggpkPath = GetGgpkPath();
                    if (!File.Exists(ggpkPath))
                    {
                        Console.WriteLine("File not found.");
                        WaitForExit();
                    }
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Invalid option. Please try again.");
                    WaitForExit();
                    break;
            }
        }
    }

    private static LibBundle3.Index GetGGPKIndex(string ggpkPath)
    {
        if (ggpkPath.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
        {
            LibBundle3.Index index = new(ggpkPath);
            return index;
        }
        else if (ggpkPath.EndsWith(".ggpk", StringComparison.OrdinalIgnoreCase))
        {
            BundledGGPK ggpk = new(ggpkPath);
            var index = ggpk.Index;
            return index;
        }
        throw new InvalidOperationException("Unsupported file type.");
    }

    private static void ShowPatchesInfo(IPatch[] patches)
    {
        Console.Clear();
        Console.WriteLine("=== Patch Information ===");
        Console.WriteLine();

        for (int i = 0; i < patches.Length; i++)
        {
            Console.WriteLine($"[{i + 1}] {patches[i].Name}");
            Console.WriteLine($"    Description: {patches[i].Description}");
            Console.WriteLine();
        }

        Console.WriteLine("Press any key to return to the menu...");
        Console.ReadKey();
    }

    private static void ApplyAllPatches(string ggpkPath, IPatch[] patches)
    {
        Console.Clear();
        Console.WriteLine("=== Apply All Patches ===");
        Console.WriteLine();

        try
        {
            using var index = GetGGPKIndex(ggpkPath);
            var fileTree = index.BuildTree();

            foreach (var patch in patches)
            {
                Console.WriteLine($"Applying patch: {patch.Name}");
                patch.Apply(fileTree);
            }

            index.Save();
            Console.WriteLine();
            Console.WriteLine("All patches applied successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying patches: {ex.Message}");
        }

        WaitForExit();
    }

    private static void ApplySelectedPatches(string ggpkPath, IPatch[] patches)
    {
        Console.Clear();
        Console.WriteLine("=== Apply Selected Patches ===");
        Console.WriteLine();

        // Show the list of patches
        for (int i = 0; i < patches.Length; i++)
        {
            Console.WriteLine($"{i + 1} - {patches[i].Name}");
        }

        Console.WriteLine();
        Console.Write("Enter patch numbers separated by spaces: ");
        var input = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("No patches selected.");
            WaitForExit();
            return;
        }

        try
        {
            var selectedIndices = input.Split(' ')
                .Select(s => int.TryParse(s.Trim(), out int temp) ? temp : -1)
                .Where(i => i >= 1 && i <= patches.Length)
                .Select(i => i - 1)
                .Distinct()
                .ToArray();

            if (selectedIndices.Length == 0)
            {
                Console.WriteLine("No patches selected.");
                WaitForExit();
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Applying selected patches...");

            using var indexInstance = GetGGPKIndex(ggpkPath);
            var fileTree = indexInstance.BuildTree();

            foreach (var patchIndex in selectedIndices)
            {
                Console.WriteLine($"Applying patch: {patches[patchIndex].Name}");
                patches[patchIndex].Apply(fileTree);
            }

            indexInstance.Save();
            Console.WriteLine();
            Console.WriteLine("Selected patches applied successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        WaitForExit();
    }

    private static void WaitForExit()
    {
        Console.WriteLine();
        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}