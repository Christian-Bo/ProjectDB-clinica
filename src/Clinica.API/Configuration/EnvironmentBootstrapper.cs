using System.Text;

namespace Clinica.API.Configuration;

public static class EnvironmentBootstrapper
{
    public static void LoadFromDotEnv(string contentRootPath, bool overrideExisting = false)
    {
        var candidates = BuildCandidates(contentRootPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(File.Exists)
            .ToArray();

        foreach (var file in candidates)
        {
            LoadFile(file, overrideExisting);
        }
    }

    private static IEnumerable<string> BuildCandidates(string contentRootPath)
    {
        var current = Directory.GetCurrentDirectory();

        yield return Path.Combine(contentRootPath, ".env");
        yield return Path.Combine(contentRootPath, ".env.local");
        yield return Path.Combine(current, ".env");
        yield return Path.Combine(current, ".env.local");
        yield return Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", ".env"));
        yield return Path.GetFullPath(Path.Combine(contentRootPath, "..", "..", ".env.local"));
    }

    private static void LoadFile(string filePath, bool overrideExisting)
    {
        try
        {
            foreach (var rawLine in File.ReadAllLines(filePath, Encoding.UTF8))
            {
                var line = rawLine.Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                {
                    continue;
                }

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                value = StripWrappingQuotes(value);

                var currentValue = Environment.GetEnvironmentVariable(key);
                if (!overrideExisting && !string.IsNullOrWhiteSpace(currentValue))
                {
                    continue;
                }

                Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch (IOException ex)
        {
            Console.WriteLine($">> ADVERTENCIA: no se pudo leer '{filePath}': {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($">> ADVERTENCIA: acceso denegado a '{filePath}': {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($">> ADVERTENCIA: error cargando '{filePath}': {ex.Message}");
        }
    }

    private static string StripWrappingQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                return value[1..^1];
            }
        }

        return value;
    }
}