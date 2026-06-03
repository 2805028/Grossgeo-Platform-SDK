// =====================================================================
// TestProduct.Free — Бесплатный продукт (FREE)
// Демонстрация: PlanTier.Free, BillingModel.Free, Default-фичи
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

[assembly: CommandClass(typeof(TestProduct.Free.TestFreePlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Free.TestFreePlugin))]

namespace TestProduct.Free
{
    /// <summary>
    /// Тестовый FREE плагин — работает без лицензии.
    /// Демонстрирует: PlanTier.Free, BillingModel.Free, Default-фичи (IsDefault).
    /// </summary>
    public class TestFreePlugin : IExtensionApplication
    {
        // API Key из DbSeeder.FreePluginId
        private const string ProductKey = "GG-974A-E898-5FBB-66FF";
        private const string PluginVersion = "1.0.0";

        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        private static ProductLicenseAccessor? _license;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  🆓 TEST PRODUCT FREE v1.0.0                                  ║");
            WriteMessage("║  Бесплатный продукт — работает без лицензии                   ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  PlanTier=Free, BillingModel=Free                             ║");
            WriteMessage("║  Default-фичи (IsDefault) доступны во всех планах             ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");

            _ = InitializeSdkAsync();
        }

        public void Terminate()
        {
            GrossGeoLicense.Shutdown(ProductKey);
        }

        #endregion

        #region SDK Initialization

        private async Task InitializeSdkAsync()
        {
            try
            {
                WriteMessage($"\n[SDK] Инициализация FREE продукта...");
                WriteMessage($"[SDK] ProductKey: {MaskKey(ProductKey)}");

                var result = await GrossGeoLicense.Initialize(new LicenseOptions
                {
                    ProductKey = ProductKey,
                    PluginVersion = PluginVersion,
                    GracePeriodDays = 7
                });

                _license = GrossGeoLicense.ForProduct(ProductKey);

                WriteMessage($"[SDK] Результат:");
                WriteMessage($"      - Статус: {result.Status}");
                WriteMessage($"      - IsValid: {result.IsValid}");
                WriteMessage($"      - PlanTier: {result.PlanTier}");
                WriteMessage($"      - BillingModel: {result.BillingModel}");
                WriteMessage($"      - LicenseMode: {result.LicenseMode}");

                if (result.Features?.Count > 0)
                {
                    WriteMessage($"      - Features: {string.Join(", ", result.Features)}");
                }

                WriteMessage($"\n[SDK] ✅ FREE продукт — все функции доступны!");
                WriteMessage($"[SDK] Команды: GGFREETEST, GGFREEINFO, GGFREEPUBLIC, GGFREEHELP");
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ⚠️ Ошибка: {ex.Message}");
                WriteMessage($"[SDK] FREE продукт работает даже без SDK");
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Тестовая команда FREE продукта — всегда доступна.
        /// </summary>
        [CommandMethod("GGFREETEST")]
        public void FreeTestCommand()
        {
            WriteMessage("\n╔════════════════════════════════════════╗");
            WriteMessage("║  🆓 GGFREETEST — Бесплатная команда    ║");
            WriteMessage("╚════════════════════════════════════════╝");
            WriteMessage("Команда работает без лицензии.");
            WriteMessage("FREE продукт = неограниченный доступ.");
        }

        /// <summary>
        /// Полная информация о лицензии.
        /// </summary>
        [CommandMethod("GGFREEINFO")]
        public void FreeInfoCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n═══ FREE Product Info ═══");
            sb.AppendLine($"IsInitialized:  {GrossGeoLicense.IsInitialized}");
            sb.AppendLine($"IsValid:        {_license?.IsValid}");

            // Свойства лицензии
            sb.AppendLine($"\n═══ License Model ═══");
            sb.AppendLine($"PlanTier:       {_license?.PlanTier}");
            sb.AppendLine($"BillingModel:   {_license?.BillingModel}");
            sb.AppendLine($"LicenseMode:    {_license?.LicenseMode}");

            sb.AppendLine($"\n═══ Status ═══");
            sb.AppendLine($"ExpiresAt:      {_license?.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A (бессрочная)"}");
            sb.AppendLine($"IsOfflineMode:  {_license?.IsOfflineMode}");
            sb.AppendLine($"IsGracePeriod:  {_license?.IsInGracePeriod}");

            var features = _license?.Features ?? Array.Empty<string>();
            sb.AppendLine($"\n═══ Features ═══");
            sb.AppendLine($"Count: {features.Count}");
            foreach (var f in features)
            {
                sb.AppendLine($"  • {f}");
            }

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Демонстрация фичей доступных всем (IsDefault=true) — работают без платной лицензии.
        /// </summary>
        [CommandMethod("GGFREEPUBLIC")]
        public void PublicFeaturesCommand()
        {
            WriteMessage("\n═══ Default Features Demo (v3) ═══");
            WriteMessage("Фичи с IsDefault=true доступны во всех планах:");

            var defaultFeatures = new[] { "view-objects", "basic-info" };
            foreach (var feature in defaultFeatures)
            {
                var hasIt = _license?.HasFeature(feature);
                var icon = hasIt == true ? "✅" : "❌";
                WriteMessage($"  {icon} {feature}: HasFeature={hasIt}");
            }

            WriteMessage("\nv3: Код без SDK-обёртки доступен всем по определению.");
            WriteMessage("  • IsDefault=true — доступны во всех планах (включая Free)");
            WriteMessage("  • PlanFeature — доступны по плану");
        }

        /// <summary>
        /// Справка по командам.
        /// </summary>
        [CommandMethod("GGFREEHELP")]
        public void HelpCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       TEST PRODUCT FREE — Команды                            ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  GGFREETEST     — Бесплатная команда                         ║");
            sb.AppendLine("║  GGFREEINFO     — Информация о лицензии                      ║");
            sb.AppendLine("║  GGFREEPUBLIC   — Демо Default-фичей (IsDefault)             ║");
            sb.AppendLine("║  GGFREEHELP     — Эта справка                                ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            WriteMessage(sb.ToString());
        }

        #endregion

        #region Helpers

        private static void WriteMessage(string message)
        {
            Ed?.WriteMessage($"\n{message}");
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 10)
                return "***";
            return $"{key[..7]}...{key[^4..]}";
        }

        #endregion
    }
}

