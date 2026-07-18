using System.Diagnostics;

namespace Lobby.Tests.Integration;

/// <summary>
/// Замінник для інтеграційних тестів, яким потрібен Docker.
/// Якщо Docker-демон недоступний, тест позначається як пропущений (Skipped).
/// </summary>
public sealed class DockerFactAttribute : FactAttribute
{
    public DockerFactAttribute()
    {
        if (!DockerProbe.IsAvailable)
            Skip = "Docker не запущено - інтеграційний тест пропущено.";
    }
}

/// <summary>
/// Одноразова (на весь тестовий запуск) перевірка доступності Docker-демона
/// через `docker info` з таймаутом 5 секунд.
/// </summary>
internal static class DockerProbe
{
    public static bool IsAvailable { get; } = Probe();

    private static bool Probe()
    {
        try
        {
            var startInfo = new ProcessStartInfo("docker", "info --format {{.ServerVersion}}")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            using var process = Process.Start(startInfo);
            if (process is null)
                return false;
            if (!process.WaitForExit(5000))
            {
                process.Kill(entireProcessTree: true);
                return false;
            }
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
