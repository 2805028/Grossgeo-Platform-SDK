// =====================================================================
// StubInstaller — Заглушка-установщик для E2E тестирования
// Создаёт файл-маркер в целевой директории и завершается с кодом 0.
// =====================================================================

using System;
using System.IO;

try
{
    var targetDir = args.Length > 0
        ? args[0]
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "GrossGeo", "Tests", "TestProduct.Installer");

    Directory.CreateDirectory(targetDir);

    // Создаём маркер-файл
    var markerPath = Path.Combine(targetDir, "installed.marker");
    var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    File.WriteAllText(markerPath, $"installed={version}\ntimestamp={DateTime.UtcNow:O}\n");

    Console.WriteLine($"[StubInstaller] Installation completed successfully.");
    Console.WriteLine($"[StubInstaller] Marker: {markerPath}");
    Console.WriteLine($"[StubInstaller] Version: {version}");

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[StubInstaller] ERROR: {ex.Message}");
    return 1;
}

