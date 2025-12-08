using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Effects : IPatch
{
    public string Name => "Effects Patch (Experimental)";
    public object Description => "Disables all effects in the game.";

    private readonly string[] extensions = {
        ".aoc",
    };

    private readonly string[] _functions = {
        "ParticleEffects",
        "TrailsEffects",
        "DecalEvents",
        "ScreenShake",
        "Lights",
        // "WindEvents",
        // "SoundEvents",
    };

    private string RemoveFunctionBlock(string data, string functionName)
    {
        int index = 0;
        while (index < data.Length)
        {
            int funcIndex = data.IndexOf(functionName, index);
            if (funcIndex < 0) break;

            int openBraceIndex = data.IndexOf('{', funcIndex + functionName.Length);
            if (openBraceIndex < 0) break;
            int braceCount = 1;
            int i = openBraceIndex + 1;
            
            while (i < data.Length && braceCount > 0)
            {
                if (data[i] == '{') braceCount++;
                else if (data[i] == '}') braceCount--;
                i++;
            }

            if (braceCount == 0)
            {
                data = data.Remove(funcIndex, i - funcIndex);
                index = funcIndex;
            }
            else
            {
                break;
            }
        }
        return data;
    }

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

                    foreach (var func in _functions)
                    {
                        data = RemoveFunctionBlock(data, func);
                    }

                    var newBytes = System.Text.Encoding.Unicode.GetBytes(data);
                    record.Write(newBytes);
                }
            }
        }
    }

    public void Apply(DirectoryNode root)
    {
        // go to metadata/effects/spells
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
                            if (d3 is DirectoryNode spellDir && spellDir.Name == "spells")
                            {
                                RecursivePatcher(spellDir);
                            }
                        }
                    }
                }
            }
        }
    }
}