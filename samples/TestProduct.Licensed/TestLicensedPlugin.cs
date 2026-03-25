// =====================================================================
// TestProduct.Licensed — Perpetual лицензия + Maintenance
// Демонстрация: PlanTier.Pro, BillingModel.Perpetual, LicenseMode.Machine
// =====================================================================

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

[assembly: CommandClass(typeof(TestProduct.Licensed.TestLicensedPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Licensed.TestLicensedPlugin))]

namespace TestProduct.Licensed
{
    /// <summary>
    /// Тестовый плагин с Perpetual лицензией.
    /// Демонстрирует: PlanTier.Pro, BillingModel.Perpetual, LicenseMode.Machine, Maintenance.
    /// </summary>
    public class TestLicensedPlugin : IExtensionApplication
    {
        // API Key из DbSeeder.PaidPerpetualPluginId
        private const string ProductKey = "GG-4CE5-8D13-2CB8-032F";
        private const string PluginVersion = "1.1.0";

        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  💎 TEST PRODUCT LICENSED v1.1.0                              ║");
            WriteMessage("║  Perpetual лицензия (разовая покупка)                         ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  PlanTier=Pro, BillingModel=Perpetual, Mode=Machine            ║");
            WriteMessage("║  Maintenance: подписка на обновления (аддон)                  ║");
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
                WriteMessage($"\n[SDK] Инициализация Perpetual продукта...");
                WriteMessage($"[SDK] ProductKey: {MaskKey(ProductKey)}");

                var result = await GrossGeoLicense.Initialize(new LicenseOptions
                {
                    ProductKey = ProductKey,
                    PluginVersion = PluginVersion,
                    GracePeriodDays = 7,
                    CheckForUpdatesOnInit = true,
                    IpcTimeoutSeconds = 10
                });

                WriteMessage($"[SDK] Результат:");
                WriteMessage($"      - Статус: {result.Status}");
                WriteMessage($"      - IsValid: {result.IsValid}");
                WriteMessage($"      - PlanTier: {result.PlanTier}");
                WriteMessage($"      - BillingModel: {result.BillingModel}");
                WriteMessage($"      - LicenseMode: {result.LicenseMode}");

                if (result.IsValid)
                {
                    WriteMessage($"      - ExpiresAt: {result.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A (бессрочная)"}");
                    WriteMessage($"      - Grace Period: {result.IsInGracePeriod}");
                    WriteMessage($"      - Offline Mode: {result.IsOfflineMode}");

                    if (result.Features?.Count > 0)
                    {
                        WriteMessage($"      - Features: {string.Join(", ", result.Features)}");
                    }

                    WriteMessage("\n[SDK] ✅ Лицензия активна!");
                }
                else
                {
                    WriteMessage($"      - ErrorCode: {result.ErrorCode}");
                    WriteMessage($"      - Message: {result.Message}");
                    WriteMessage("\n[SDK] ⚠️ Лицензия недействительна.");
                }

                WriteMessage("[SDK] Команды: TEST_LICENSE_INFO, TEST_PROTECTED_CMD, TEST_FEATURE_CHECK");
                WriteMessage("[SDK]          TEST_CHECK_UPDATE, TEST_LICENSE_RECHECK, TEST_LICENSED_HELP");
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ❌ Ошибка инициализации: {ex.Message}");
            }
        }

        #endregion

        #region AutoCAD Commands

        /// <summary>
        /// Полная информация о лицензии.
        /// </summary>
        [CommandMethod("TEST_LICENSE_INFO")]
        public void ShowLicenseInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║          TEST PRODUCT LICENSED — License Info                ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Product Key:      {MaskKey(ProductKey),-39} ║");
            sb.AppendLine($"║  Plugin Version:   {PluginVersion,-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Initialized:      {GrossGeoLicense.IsInitialized,-39} ║");
            sb.AppendLine($"║  Valid:            {GrossGeoLicense.IsValid,-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");

            // Модель лицензирования
            sb.AppendLine("║  License Model:                                                ║");
            sb.AppendLine($"║    PlanTier:       {GrossGeoLicense.PlanTier,-39} ║");
            sb.AppendLine($"║    BillingModel:   {GrossGeoLicense.BillingModel,-39} ║");
            sb.AppendLine($"║    LicenseMode:    {GrossGeoLicense.LicenseMode,-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");

            // Perpetual + Maintenance
            sb.AppendLine("║  Perpetual Details:                                           ║");
            sb.AppendLine($"║    ExpiresAt:      {GrossGeoLicense.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A (бессрочная)",-39} ║");
            sb.AppendLine($"║    DaysRemaining:  {GrossGeoLicense.DaysRemaining?.ToString() ?? "∞",-39} ║");
            sb.AppendLine($"║    Grace Period:   {GrossGeoLicense.IsInGracePeriod,-39} ║");
            sb.AppendLine($"║    Offline Mode:   {GrossGeoLicense.IsOfflineMode,-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");

            var features = GrossGeoLicense.Features;
            if (features.Count > 0)
            {
                sb.AppendLine("║  Features:                                                   ║");
                foreach (var feature in features)
                {
                    sb.AppendLine($"║    • {feature,-54} ║");
                }
            }
            else
            {
                sb.AppendLine("║  Features:         (нет доступных)                           ║");
            }

            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Защищённая функция (требует лицензию).
        /// </summary>
        [CommandMethod("TEST_PROTECTED_CMD")]
        public void ProtectedCommand()
        {
            WriteMessage("\n[TEST_PROTECTED_CMD] Выполнение защищённой команды...");

            var executed = GrossGeoLicense.Protect(
                action: () =>
                {
                    WriteMessage("[TEST_PROTECTED_CMD] ✅ Лицензия валидна!");
                    WriteMessage($"[TEST_PROTECTED_CMD] PlanTier: {GrossGeoLicense.PlanTier}");
                    WriteMessage($"[TEST_PROTECTED_CMD] BillingModel: {GrossGeoLicense.BillingModel}");
                    WriteMessage("[TEST_PROTECTED_CMD] 🎉 Команда выполнена!");
                },
                onBlocked: () =>
                {
                    WriteMessage("[TEST_PROTECTED_CMD] ❌ Лицензия недействительна!");
                    WriteMessage("[TEST_PROTECTED_CMD] Установите User Panel и активируйте продукт.");
                }
            );

            WriteMessage($"[TEST_PROTECTED_CMD] Результат: {(executed ? "Выполнено" : "Заблокировано")}");
        }

        /// <summary>
        /// Проверка feature flags.
        /// </summary>
        [CommandMethod("TEST_FEATURE_CHECK")]
        public void CheckFeature()
        {
            WriteMessage("\n[TEST_FEATURE_CHECK] Проверка feature flags...");

            var featuresToCheck = new[] { "export_pdf", "batch_processing", "premium_tools", "enterprise_api" };

            foreach (var feature in featuresToCheck)
            {
                var hasFeature = GrossGeoLicense.HasFeature(feature);
                var icon = hasFeature ? "✅" : "❌";
                WriteMessage($"  {icon} '{feature}': {(hasFeature ? "Доступна" : "Недоступна")}");
            }

            WriteMessage("\n[TEST_FEATURE_CHECK] RequireFeature('premium_tools')...");
            GrossGeoLicense.RequireFeature(
                "premium_tools",
                action: () => WriteMessage("  ✅ Premium Tools активированы!"),
                onMissing: () => WriteMessage("  ⚠️ Premium Tools недоступны в вашей лицензии")
            );
        }

        /// <summary>
        /// Проверка обновлений (Maintenance влияет на доступность).
        /// </summary>
        [CommandMethod("TEST_CHECK_UPDATE")]
        public async void CheckUpdate()
        {
            WriteMessage("\n[TEST_CHECK_UPDATE] Проверка обновлений...");
            WriteMessage("[TEST_CHECK_UPDATE] Perpetual: обновления зависят от Maintenance!");

            try
            {
                var updateResult = await GrossGeoLicense.CheckForUpdatesAsync();

                WriteMessage($"  Текущая версия:  {PluginVersion}");
                WriteMessage($"  Обновление:      {(updateResult.HasUpdate ? "Доступно" : "Нет")}");

                if (updateResult.HasUpdate)
                {
                    WriteMessage($"  Новая версия:    {updateResult.AvailableVersion}");
                    WriteMessage($"  Дата релиза:     {updateResult.PublishedAt?.ToString("dd.MM.yyyy")}");
                    if (!string.IsNullOrEmpty(updateResult.Changelog))
                    {
                        WriteMessage($"  Изменения:       {Truncate(updateResult.Changelog, 50)}");
                    }

                    // Maintenance проверка
                    WriteMessage("\n  [Maintenance Info]");
                    WriteMessage("  При BillingModel=Perpetual обновления зависят от Maintenance:");
                    WriteMessage("  • Maintenance активен → обновления разрешены");
                    WriteMessage("  • Maintenance истёк → версия заблокирована (VersionLock)");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"  ❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Повторная проверка лицензии.
        /// </summary>
        [CommandMethod("TEST_LICENSE_RECHECK")]
        public async void RecheckLicense()
        {
            WriteMessage("\n[TEST_LICENSE_RECHECK] Повторная проверка лицензии...");

            try
            {
                var result = await GrossGeoLicense.CheckAsync();
                WriteMessage($"  Статус:       {result.Status}");
                WriteMessage($"  IsValid:      {result.IsValid}");
                WriteMessage($"  PlanTier:     {result.PlanTier}");
                WriteMessage($"  BillingModel: {result.BillingModel}");
                WriteMessage($"  LicenseMode:  {result.LicenseMode}");
            }
            catch (System.Exception ex)
            {
                WriteMessage($"  ❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Справка по командам.
        /// </summary>
        [CommandMethod("TEST_LICENSED_HELP")]
        public void ShowHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       TEST PRODUCT LICENSED — Команды                        ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  TEST_LICENSE_INFO     — Информация о лицензии              ║");
            sb.AppendLine("║  TEST_PROTECTED_CMD    — Защищённая команда (Protect)        ║");
            sb.AppendLine("║  TEST_FEATURE_CHECK    — Проверка feature flags              ║");
            sb.AppendLine("║  TEST_CHECK_UPDATE     — Проверка обновлений (Maintenance)   ║");
            sb.AppendLine("║  TEST_LICENSE_RECHECK  — Повторная проверка лицензии         ║");
            sb.AppendLine("║  TEST_LICENSED_HELP    — Эта справка                         ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            WriteMessage(sb.ToString());
        }

        #endregion

        #region Helpers

        private static void WriteMessage(string message)
        {
            Ed?.WriteMessage(message + "\n");
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 8)
                return "****";
            return key.Substring(0, 4) + "..." + key.Substring(key.Length - 4);
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }

        #endregion
    }
}

