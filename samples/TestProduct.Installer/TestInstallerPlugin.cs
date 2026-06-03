// =====================================================================
// TestProduct.Installer — Тип дистрибуции Installer (MSI/EXE)
// Демонстрация: DistributionType.Installer, TrialBindingMode.AccountAndMachine
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

[assembly: CommandClass(typeof(TestProduct.Installer.TestInstallerPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Installer.TestInstallerPlugin))]

namespace TestProduct.Installer
{
    /// <summary>
    /// Тестовый плагин с типом дистрибуции Installer.
    /// Демонстрирует: DistributionType.Installer, TrialBindingMode.AccountAndMachine.
    /// </summary>
    public class TestInstallerPlugin : IExtensionApplication
    {
        // API Key для TestProduct.Installer
        private const string ProductKey = "GG-INST-TEST-0007";
        private const string PluginVersion = "1.0.0";

        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        private static ProductLicenseAccessor? _license;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  📦 TEST PRODUCT INSTALLER v1.0.0                             ║");
            WriteMessage("║  Тип дистрибуции: Installer (MSI/EXE)                         ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  DistributionType=Installer                                    ║");
            WriteMessage("║  Pro: 1490₽/мес, Trial 14 дней                                ║");
            WriteMessage("║  TrialBindingMode: AccountAndMachine                          ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");

            _ = InitializeSdkAsync();
        }

        public void Terminate()
        {
            GrossGeoLicense.Shutdown(ProductKey);
            WriteMessage("\n[TestProduct.Installer] Плагин выгружен");
        }

        #endregion

        #region SDK Initialization

        private async Task InitializeSdkAsync()
        {
            try
            {
                WriteMessage($"\n[SDK] Инициализация...");
                WriteMessage($"[SDK] ProductKey: {MaskKey(ProductKey)}");

                var result = await GrossGeoLicense.Initialize(new LicenseOptions
                {
                    ProductKey = ProductKey,
                    PluginVersion = PluginVersion,
                    GracePeriodDays = 7,
                    CheckForUpdatesOnInit = true
                });

                _license = GrossGeoLicense.ForProduct(ProductKey);

                WriteMessage($"\n[SDK] Результат:");
                WriteMessage($"      - Статус: {result.Status}");
                WriteMessage($"      - IsValid: {result.IsValid}");
                WriteMessage($"      - PlanTier: {result.PlanTier}");
                WriteMessage($"      - BillingModel: {result.BillingModel}");
                WriteMessage($"      - LicenseMode: {result.LicenseMode}");

                if (result.IsValid)
                {
                    WriteMessage($"\n[SDK] ✅ Лицензия активна!");
                    WriteMessage($"[SDK] Команды: GGINSTTEST, GGINSTINFO");
                }
                else
                {
                    WriteMessage($"\n[SDK] ⚠️ Лицензия не найдена");
                    WriteMessage($"[SDK] Trial: 14 дней (TrialBindingMode=AccountAndMachine)");
                    WriteMessage($"[SDK] Доступна только команда: GGINSTINFO");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ❌ Ошибка: {ex.Message}");
            }
        }

        #endregion

        #region AutoCAD Commands

        /// <summary>
        /// Базовая команда — требует лицензию.
        /// </summary>
        [CommandMethod("GGINSTTEST")]
        public void InstallerTest()
        {
            if (!(_license?.IsValid ?? false))
            {
                WriteMessage("\n[GGINSTTEST] ⚠️ Требуется лицензия");
                return;
            }

            WriteMessage("\n[GGINSTTEST] Installer plugin executed");
            WriteMessage("[GGINSTTEST] DistributionType=Installer, TrialBindingMode=AccountAndMachine");
        }

        /// <summary>
        /// Информация о SDK — работает всегда.
        /// </summary>
        [CommandMethod("GGINSTINFO")]
        public void InstallerInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n═══ TestProduct.Installer — SDK Info ═══");
            sb.AppendLine($"  IsValid: {_license?.IsValid}");
            sb.AppendLine($"  PlanTier: {_license?.PlanTier}");
            sb.AppendLine($"  BillingModel: {_license?.BillingModel}");
            sb.AppendLine($"  LicenseMode: {_license?.LicenseMode}");
            sb.AppendLine($"  DistributionType: Installer");
            sb.AppendLine("═══════════════════════════════════════");

            WriteMessage(sb.ToString());
        }

        #endregion

        #region Helpers

        private static void WriteMessage(string message)
        {
            try
            {
                Ed?.WriteMessage(message + "\n");
            }
            catch
            {
                // Suppress if no active document
            }
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 8) return "****";
            return key[..4] + "..." + key[^4..];
        }

        #endregion
    }
}

