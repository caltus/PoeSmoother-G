using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Particles : IPatch
{
    public string Name => "Particles Patch";
    public object Description => "Disables all particle effects in the game.";

    private readonly string[] extensions = {
        ".pet",
        ".trl",
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
                    var newBytes = System.Text.Encoding.Unicode.GetBytes("0");
                    if (!newBytes.AsSpan().StartsWith(System.Text.Encoding.Unicode.GetPreamble()))
                    {
                        newBytes = System.Text.Encoding.Unicode.GetPreamble().Concat(newBytes).ToArray();
                    }
                    record.Write(newBytes);
                }
            }
        }
    }

    public void Apply(DirectoryNode root)
    {
        // go to metadata/particles/
        foreach (var d in root.Children)
        {
            if (d is DirectoryNode dir && dir.Name == "metadata")
            {
                foreach (var d2 in dir.Children)
                {
                    if (d2 is DirectoryNode subDir && subDir.Name == "particles")
                    {
                        RecursivePatcher(subDir);
                    }
                }
            }
        }
    }
}