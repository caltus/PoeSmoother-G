using System;
using System.IO;
using System.Reflection;
using LibBundle3.Nodes;
using LibGGPK3.Records;

namespace PoeSmoother.Patches;

public class Delirium : IPatch
{
    public string Name => "Delirium Patch";
    public object Description => "Disables the delirium effects in the game.";
    private readonly string[] extensions = {
        ".ao",
        ".aoc",
    };

    private void RecursivePatcher(DirectoryNode dir)
    {
        foreach (var d in dir.Children)
        {
            if (d is DirectoryNode childDir)
            {
                RecursivePatcher(childDir);
            }
            else if (d is FileNode file)
            {

                if (Array.Exists(extensions, ext => file.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {

                    var record = file.Record;
                    var bytes = record.Read();
                    string data = System.Text.Encoding.Unicode.GetString(bytes.ToArray());

                    if (string.IsNullOrEmpty(data)) continue;

                    if (data.Contains("Metadata/FmtParent") && !data.Contains("AnimatedRender"))
                    {
                        data = "version 2\nextends \"Metadata/FmtParent\"";
                    }
                    else if (data.Contains("Metadata/FmtParent") && data.Contains("AnimatedRender"))
                    {
                        data = "version 2\nextends \"Metadata/FmtParent\"\n\nAnimatedRender\n{\n\tcannot_be_disabled = true\n}";
                    }
                    else if (data.Contains("default_animation = \"loop\""))
                    {
                        string[] separator = [Environment.NewLine];
                        string[] lines = data.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        IEnumerable<string> filteredLines = lines.Where(line => !line.Contains("default_animation = \"loop\""));
                        data = string.Join(Environment.NewLine, filteredLines);
                    }
                    else if (data.Contains("BoneGroups"))
                    {
                        data = @"version 2
extends ""Metadata/Parent""

ClientAnimationController
{
	skeleton = ""Art/Models/Effects/enviro_effects/weather_attachments/generic_rig/weather_rig.ast""
}

BoneGroups
{
	bone_group = ""box false aux_box1 aux_box2 aux_box3 ""
}";
                    }

                    var newBytes = System.Text.Encoding.Unicode.GetBytes(data);
                    record.Write(newBytes);
                }
            }
        }
    }

    public void Apply(DirectoryNode root)
    {
        // go to metadata/effects/environment/league_affliction
        foreach (var d in root.Children)
        {
            if (d is DirectoryNode dir && dir.Name == "metadata")
            {
                foreach (var d2 in dir.Children)
                {
                    if (d2 is DirectoryNode subDir && subDir.Name == "effects")
                    {
                        foreach (var d3 in subDir.Children)
                        {
                            if (d3 is DirectoryNode subDir2 && subDir2.Name == "environment")
                            {
                                foreach (var d4 in subDir2.Children)
                                {
                                    if (d4 is DirectoryNode subDir3 && subDir3.Name == "league_affliction")
                                    {
                                        RecursivePatcher(subDir3);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}