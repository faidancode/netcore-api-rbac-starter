using System.Text;

namespace netcore_api_rbac_starter.Common;

public static class DotEnv
{
    public static void Load(string path = ".env")
    {
        if (!File.Exists(path)) return;

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();

            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                continue;

            if (line.StartsWith("export "))
                line = line.Substring(7).Trim();

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            // remove inline comment
            var commentIndex = value.IndexOf('#');
            if (commentIndex >= 0)
                value = value[..commentIndex].Trim();

            // remove quotes
            if (value.Length >= 2 &&
                ((value.StartsWith('"') && value.EndsWith('"')) ||
                 (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            // 🔥 do not override existing env
            if (Environment.GetEnvironmentVariable(key) == null)
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
