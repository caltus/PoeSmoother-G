using System;
using System.IO;
using System.Reflection;
using LibBundle3.Nodes;
using LibGGPK3.Records;

namespace PoeSmoother.Patches;

public class Fog : IPatch
{
    public string Name => "Fog Patch";
    public object Description => "Disables the default fog effect in the game.";

    public void Apply(DirectoryNode root)
    {
        // go to shaders/renderer/fog.ffx
        foreach (var d in root.Children)
        {
            if (d is DirectoryNode dir && dir.Name == "shaders")
            {
                foreach (var dir1 in dir.Children)
                {
                    if (dir1 is DirectoryNode subDir && subDir.Name == "renderer")
                    {
                        foreach (var dir2 in subDir.Children)
                        {
                            if (dir2 is FileNode file && file.Name == "fog.ffx")
                            {
                                var record = file.Record;
                                var bytes = record.Read();
                                string data = System.Text.Encoding.Unicode.GetString(bytes.ToArray());
                                List<string> lines = data.Split("\r\n").ToList();
                                for (int i = 0; i < lines.Count; i++)
                                {
                                    if (lines[i].Contains("oFogValue"))
                                    {
                                        if (lines[i].Contains("//"))
                                            continue;
                                        string trimmedLine = lines[i].TrimStart();
                                        if (trimmedLine.StartsWith("oFogValue"))
                                        {
                                            lines[i] = "//" + lines[i];
                                        }
                                    }
                                }
                                string newData = string.Join("\r\n", lines);
                                var newBytes = System.Text.Encoding.Unicode.GetBytes(newData);
                                record.Write(newBytes);
                            }
                        }
                    }
                }
            }
        }
    }
}