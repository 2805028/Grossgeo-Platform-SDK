// =====================================================================
// TestProduct.Free — Бесплатный продукт (FREE)
// Демонстрация: PlanTier.Free, BillingModel.Free, публичные фичи
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
    /// Демонстрирует v2: PlanTier.Free, BillingModel.Free, публичные фичи (IsPublic).
    /// </summary>
    public class TestFreePlugin : IExtensionApplication
    {
        // API Key из DbSeeder.FreePluginId
        private const string ProductKey = "GG-FREE-TEST-0001";
        private const string PluginVersion = "1.0.0";

        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  🆓 TEST PRODUCT FREE v1.0.0                                  ║");
            WriteMessage("║  Бесплатный продукт — работает без лицензии                   ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  v2: PlanTier=Free, BillingModel=Free                         ║");
            WriteMessage("║  Публичные фичи (IsPublic) доступны даже Guest                ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");

            _ = InitializeSdkAsync();
        }

        public void Terminate()
        {
            GrossGeoLicense.Shutdown();
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
        /// Полная информация о лицензии (v2 API).
        /// </summary>
        [CommandMethod("GGFREEINFO")]
        public void FreeInfoCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n═══ FREE Product Info (v2) ═══");
            sb.AppendLine($"IsInitialized:  {GrossGeoLicense.IsInitialized}");
            sb.AppendLine($"IsValid:        {GrossGeoLicense.IsValid}");

            // v2 свойства
            sb.AppendLine($"\n═══ v2 License Model ═══");
            sb.AppendLine($"PlanTier:       {GrossGeoLicense.PlanTier}");
            sb.AppendLine($"BillingModel:   {GrossGeoLicense.BillingModel}");
            sb.AppendLine($"LicenseMode:    {GrossGeoLicense.LicenseMode}");

            sb.AppendLine($"\n═══ Status ═══");
            sb.AppendLine($"ExpiresAt:      {GrossGeoLicense.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A (бессрочная)"}");
            sb.AppendLine($"IsOfflineMode:  {GrossGeoLicense.IsOfflineMode}");
            sb.AppendLine($"IsGracePeriod:  {GrossGeoLicense.IsInGracePeriod}");

            var features = GrossGeoLicense.Features;
            sb.AppendLine($"\n═══ Features ═══");
            sb.AppendLine($"Count: {features.Count}");
            foreach (var f in features)
            {
                var isPublic = GrossGeoLicense.IsPublicFeature(f);
                sb.AppendLine($"  • {f}{(isPublic ? " [PUBLIC]" : "")}");
            }

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Демонстрация публичных фичей (IsPublic=true) — работают без авторизации.
        /// </summary>
        [CommandMethod("GGFREEPUBLIC")]
        public void PublicFeaturesCommand()
        {
            WriteMessage("\n═══ Public Features Demo (v2) ═══");
            WriteMessage("Публичные фичи (IsPublic=true) доступны даже для Guest:");

            // Пример публичных фичей — работают без авторизации
            var publicFeatures = new[] { "view-objects", "basic-info" };
            foreach (var feature in publicFeatures)
            {
                var hasIt = GrossGeoLicense.HasFeature(feature);
                var isPublic = GrossGeoLicense.IsPublicFeature(feature);
                var icon = hasIt ? "✅" : "❌";
                WriteMessage($"  {icon} {feature}: HasFeature={hasIt}, IsPublic={isPublic}");
            }

            WriteMessage("\nДля Guest (неавторизованного) пользователя:");
            WriteMessage("  • IsPublic=true фичи возвращают HasFeature=true");
            WriteMessage("  • IsPublic=false фичи возвращают HasFeature=false");
            WriteMessage("  • FREE план получает все IsDefault=true фичи");
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
            sb.AppendLine("║  GGFREEINFO     — Информация о лицензии (v2 API)             ║");
            sb.AppendLine("║  GGFREEPUBLIC   — Демо публичных фичей (IsPublic)            ║");
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
