using System.Text.RegularExpressions;

namespace VSProjectRenamer.Core;

/// <summary>
/// Randomizes TCP/UDP port numbers found in common configuration files.
/// </summary>
public static class PortRandomizer
{
    // Matches patterns like:
    //   "port": 5000              (JSON)
    //   port=5000                 (env / ini)
    //   <HttpsPort>5001</HttpsPort>   (XML)
    //   --port 5000               (CLI args)
    //   EXPOSE 80                 (Dockerfile)
    //   :5000  (scheme://host:port)
    private static readonly Regex[] PortPatterns =
    [
        // JSON/YAML: "port": 5000  or  port: 5000
        new Regex(@"(?i)(""?port""?\s*[=:]\s*)(\d{2,5})\b", RegexOptions.Compiled),
        // XML element: <SomePort>5000</SomePort>  or  <Kestrel>...<Port>5000</Port>
        new Regex(@"(?i)(<[^>]*[Pp]ort[^>]*>)(\d{2,5})(</)", RegexOptions.Compiled),
        // Dockerfile: EXPOSE 80
        new Regex(@"(?im)^(EXPOSE\s+)(\d{2,5})\b", RegexOptions.Compiled),
        // URL / host:port — preceded by a colon that follows alphanumeric chars
        new Regex(@"(?<=\w:)(\d{2,5})(?=\b(?:[/""\s]|$))", RegexOptions.Compiled),
        // Environment variable style: PORT=5000 or ASPNETCORE_URLS=http://+:5000
        new Regex(@"(?i)(PORT\s*=\s*)(\d{2,5})\b", RegexOptions.Compiled),
    ];

    private static readonly Random Rng = Random.Shared;

    // Well-known ports that should never be assigned as replacement ports.
    private static readonly HashSet<int> ReservedPorts = [80, 443, 21, 22, 25, 53, 110, 143, 3306, 5432, 27017, 6379, 1433];

    /// <summary>
    /// Replaces port numbers found in <paramref name="content"/> with new random values in
    /// the range [49152, 65535] (dynamic / private ports per IANA).
    /// The same source port is always mapped to the same new port within a single call.
    /// </summary>
    /// <param name="content">File content that may contain port numbers.</param>
    /// <param name="portMap">
    /// Optional dictionary to accumulate or pre-seed old→new port mappings.
    /// Pass the same instance across multiple files to keep port numbers consistent.
    /// </param>
    /// <returns>Updated text.</returns>
    public static string RandomizePorts(string content, Dictionary<int, int>? portMap = null)
    {
        portMap ??= [];

        foreach (var pattern in PortPatterns)
        {
            content = pattern.Replace(content, m =>
            {
                // Find the group that holds the actual port digits.
                // Group 2 holds the port for most patterns; group 1 for the URL pattern.
                var portGroup = m.Groups.Count > 2 ? m.Groups[2] : m.Groups[1];
                if (!int.TryParse(portGroup.Value, out var oldPort))
                    return m.Value;

                if (!portMap.TryGetValue(oldPort, out var newPort))
                {
                    newPort = GeneratePort(portMap);
                    portMap[oldPort] = newPort;
                }

                return m.Value.Replace(portGroup.Value, newPort.ToString());
            });
        }

        return content;
    }

    // -----------------------------------------------------------------------

    private static int GeneratePort(Dictionary<int, int> portMap)
    {
        var used = new HashSet<int>(portMap.Values);
        int port;
        do
        {
            port = Rng.Next(49152, 65536);
        }
        while (used.Contains(port) || ReservedPorts.Contains(port));
        return port;
    }
}
