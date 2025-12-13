using LibBundle3.Nodes;

namespace PoeSmoother.Patches;

public class Rain : IPatch
{
    public string Name => "Rain Patch";
    public object Description => "Disables the default rain effect in the game.";

    private readonly string[] extensions = {
        ".env",
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

                    string pattern = @"(""rain_intensity"":\s*)([^,\r\n}]+)(,?)";
                    string replacement = "${1}0.0${3}";
                    data = System.Text.RegularExpressions.Regex.Replace(data, pattern, replacement);

                    var newBytes = System.Text.Encoding.Unicode.GetBytes(data);
                    record.Write(newBytes);
                }
            }
        }
    }

    public void Apply(DirectoryNode root)
    {
        // go to metadata/environmentsettings/
        foreach (var d in root.Children)
        {
            if (d is DirectoryNode dir && dir.Name == "metadata")
            {
                foreach (var d2 in dir.Children)
                {
                    if (d2 is DirectoryNode subDir && subDir.Name == "environmentsettings")
                    {
                        RecursivePatcher(subDir);
                    }
                }
            }
        }
    }
}