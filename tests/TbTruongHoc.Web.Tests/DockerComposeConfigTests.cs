using System;
using System.IO;
using Xunit;

namespace TbTruongHoc.Web.Tests;

/// <summary>
/// Partial, static coverage for the spec's I/O &amp; Edge-Case Matrix row 5
/// ("docker compose up on a clean checkout"). This test only guards the
/// *configuration* that makes the described resilience behavior possible -
/// it is a regression check against someone accidentally loosening the
/// pinned image tag, dropping the healthcheck gate, or losing the media/DB
/// volumes - not a live test of the actual crash/retry/recovery behavior
/// itself.
///
/// The live behavior (mariadb not ready yet -> piranha-app retries/waits
/// rather than permanently crash-looping; media persists across a real
/// container restart) is deliberately NOT exercised here. Simulating it
/// faithfully means stopping/restarting the shared docker-compose "mariadb"
/// container that this same test run's other integration tests
/// (HostnameResolutionTests, SiteSeedIdempotencyTests) depend on, waiting out
/// real healthcheck/retry timers (many tens of seconds), and asserting on
/// Docker's own "restart: unless-stopped" policy rather than any code this
/// story wrote - see the final task report for the full rationale for
/// leaving that part manual/ops-verified.
/// </summary>
public class DockerComposeConfigTests
{
    [Fact]
    public void MariaDb_Image_Is_Pinned_To_10_11_Not_Latest()
    {
        var compose = ReadDockerComposeYaml();

        Assert.Contains("image: mariadb:10.11", compose);
        Assert.DoesNotContain("mariadb:latest", compose);
    }

    [Fact]
    public void PiranhaApp_Waits_For_MariaDb_Healthcheck_Before_Starting()
    {
        var compose = ReadDockerComposeYaml();

        // depends_on with condition: service_healthy is what makes a *clean*
        // `docker compose up` wait for mariadb's healthcheck instead of racing
        // it - the first half of matrix row 5's requirement.
        Assert.Contains("condition: service_healthy", compose);

        var mariadbSection = ExtractServiceBlock(compose, "mariadb:");
        Assert.Contains("healthcheck:", mariadbSection);
        Assert.Contains("retries:", mariadbSection);
    }

    [Fact]
    public void PiranhaApp_Has_Restart_Policy_So_It_Does_Not_Permanently_Crash_Loop()
    {
        var compose = ReadDockerComposeYaml();
        var appSection = ExtractServiceBlock(compose, "piranha-app:");

        // If mariadb becomes unavailable *after* piranha-app has already
        // started (i.e. outside the depends_on gate), this restart policy is
        // what makes the container come back instead of staying dead - it
        // must not be "no" or missing.
        Assert.Contains("restart: unless-stopped", appSection);
    }

    [Fact]
    public void Media_And_Database_Data_Persist_Via_Mounted_Volumes()
    {
        var compose = ReadDockerComposeYaml();

        var mariadbSection = ExtractServiceBlock(compose, "mariadb:");
        Assert.Contains("mariadb-data:/var/lib/mysql", mariadbSection);

        var appSection = ExtractServiceBlock(compose, "piranha-app:");
        Assert.Contains("./media:/app/wwwroot/uploads", appSection);

        Assert.Contains("mariadb-data:", compose);
    }

    private static string ReadDockerComposeYaml()
    {
        var path = Path.Combine(FindRepoRoot(), "docker-compose.yml");
        Assert.True(File.Exists(path), $"docker-compose.yml not found at expected path: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Extracts the top-level service block (e.g. "mariadb:" or
    /// "piranha-app:") from the compose file's raw text, up to the next
    /// top-level ("services:"-indented) key or end of file.
    /// </summary>
    private static string ExtractServiceBlock(string composeYaml, string serviceHeader)
    {
        var lines = composeYaml.Replace("\r\n", "\n").Split('\n');
        var blockLines = new System.Collections.Generic.List<string>();
        var inBlock = false;

        foreach (var line in lines)
        {
            if (!inBlock)
            {
                if (line.TrimStart() == serviceHeader && line.StartsWith("  "))
                {
                    inBlock = true;
                    blockLines.Add(line);
                }
                continue;
            }

            // A new service (2-space indent, e.g. another "service:") or a
            // new top-level section (0-space indent, e.g. the file's
            // trailing "volumes:" key) both end the current block.
            var trimmedStart = line.TrimStart(' ');
            var indent = line.Length - trimmedStart.Length;
            var isSiblingKey = trimmedStart.Length > 0
                && indent <= 2
                && line.TrimEnd().EndsWith(":");

            if (isSiblingKey)
            {
                break;
            }

            blockLines.Add(line);
        }

        Assert.True(blockLines.Count > 1, $"Could not locate a '{serviceHeader}' service block in docker-compose.yml");
        return string.Join('\n', blockLines);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "TbTruongHoc.sln")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new InvalidOperationException(
                $"Could not locate repo root (TbTruongHoc.sln) starting from {AppContext.BaseDirectory}");
        }

        return dir.FullName;
    }
}
