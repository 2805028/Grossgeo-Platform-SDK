// =====================================================================
// TestProduct.PluginDll — Тип дистрибуции PluginDll (одиночная DLL)
// Демонстрация: DistributionType.PluginDll, Perpetual, FeatureLimits
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

[assembly: CommandClass(typeof(TestProduct.PluginDll.TestPluginDllPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.PluginDll.TestPluginDllPlugin))]

namespace TestProduct.PluginDll
{
    /// <summary>
    /// Тестовый плагин с типом дистрибуции PluginDll.
    /// Демонстрирует: DistributionType.PluginDll, PlanTier.Pro,
    /// BillingModel.Perpetual, LicenseMode.Machine, Trial 14 дней.
    /// </summary>
    public class TestPluginDllPlugin : IExtensionApplication
    {
        // ProductKey (демо-плейсхолдер в формате GG-XXXX-XXXX-XXXX-XXXX).
        // Замените на ключ своего продукта из Developer Portal.
        private const string ProductKey = "GG-9D11-4B0C-0008-FE63";
        private const string PluginVersion = "1.0.0";

        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        private static ProductLicenseAccessor? _license;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  📄 TEST PRODUCT PLUGINDLL v1.0.0                             ║");
            WriteMessage("║  Тип дистрибуции: PluginDll (одиночная DLL)                    ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  DistributionType=PluginDll                                     ║");
            WriteMessage("║  Pro: Perpetual 4990₽, Trial 14 дней                           ║");
            WriteMessage("║  TrialBindingMode: AccountAndMachine                           ║");
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
                WriteMessage($"\n[SDK] Инициализация PluginDll продукта...");
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

                if (result.Features?.Count > 0)
                {
                    WriteMessage($"      - Features: {string.Join(", ", result.Features)}");
                }

                if (result.IsValid)
                {
                    WriteMessage($"\n[SDK] ✅ Лицензия активна!");
                    WriteMessage($"[SDK] Команды: GGDLLTEST, GGDLLINFO, GGDLLREPORT, GGDLLHELP");
                }
                else
                {
                    WriteMessage($"\n[SDK] ⚠️ Лицензия не найдена");
                    WriteMessage($"[SDK] Trial: 14 дней (TrialBindingMode=AccountAndMachine)");
                    WriteMessage($"[SDK] Доступна только команда: GGDLLINFO, GGDLLHELP");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ❌ Ошибка: {ex.Message}");
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Базовая команда — требует лицензию. Использует feature "core".
        /// </summary>
        [CommandMethod("GGDLLTEST")]
        public void DllTestCommand()
        {
            LicenseGuard.Protect(
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  ✅ GGDLLTEST — PluginDll команда       ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage($"PlanTier: {_license?.PlanTier}");
                    WriteMessage($"BillingModel: {_license?.BillingModel}");
                    WriteMessage($"DistributionType: PluginDll");
                    WriteMessage("Команда выполнена успешно!");
                },
                onBlocked: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  ❌ Требуется лицензия                 ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Запустите Trial или приобретите Perpetual лицензию.");
                }
            );
        }

        /// <summary>
        /// Информация о лицензии — работает всегда.
        /// </summary>
        [CommandMethod("GGDLLINFO")]
        public void DllInfoCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n═══ PluginDll Product Info ═══");
            sb.AppendLine($"IsInitialized:  {GrossGeoLicense.IsInitialized}");
            sb.AppendLine($"IsValid:        {_license?.IsValid}");

            sb.AppendLine("\n═══ License Model ═══");
            sb.AppendLine($"PlanTier:       {_license?.PlanTier}");
            sb.AppendLine($"BillingModel:   {_license?.BillingModel}");
            sb.AppendLine($"LicenseMode:    {_license?.LicenseMode}");
            sb.AppendLine($"DistributionType: PluginDll");

            sb.AppendLine("\n═══ Status ═══");
            sb.AppendLine($"ExpiresAt:      {_license?.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A (бессрочная)"}");
            sb.AppendLine($"IsOfflineMode:  {_license?.IsOfflineMode}");
            sb.AppendLine($"IsGracePeriod:  {_license?.IsInGracePeriod}");

            sb.AppendLine("\n═══ Features ═══");
            sb.AppendLine($"core:      {_license?.HasFeature("core")}");
            sb.AppendLine($"reporting: {_license?.HasFeature("reporting")}");

            var reportingLimit = _license?.GetFeatureLimit("reporting", "maxPerCall");
            sb.AppendLine($"\n═══ Limits ═══");
            sb.AppendLine($"reporting.maxPerCall: {(reportingLimit.HasValue ? reportingLimit.Value.ToString() : "N/A")}");

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Генерация отчёта — требует feature "reporting" и проверяет лимиты.
        /// </summary>
        [CommandMethod("GGDLLREPORT")]
        public void DllReportCommand()
        {
            _license?.RequireFeature("reporting",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📊 GGDLLREPORT — Отчёт                ║");
                    WriteMessage("║  Feature: reporting + Limits           ║");
                    WriteMessage("╚════════════════════════════════════════╝");

                    // Симуляция: пользователь запросил отчёт по 200 объектам
                    var objectCount = 200;
                    var limit = _license?.GetFeatureLimit("reporting", "maxPerCall");

                    WriteMessage($"Объектов в отчёте: {objectCount}");
                    WriteMessage($"Лимит maxPerCall:  {(limit.HasValue ? limit.Value.ToString() : "∞ (безлимит)")}");

                    if (_license?.CheckLimit("reporting", "maxPerCall", objectCount) == true)
                    {
                        WriteMessage($"✅ Лимит не превышен — отчёт сформирован!");
                    }
                    else
                    {
                        WriteMessage($"❌ Превышен лимит! Максимум: {limit}, запрошено: {objectCount}");
                    }
                },
                onMissing: () =>
                {
                    WriteMessage("\n🔒 Отчётность недоступна.");
                    WriteMessage("Приобретите Pro лицензию для доступа к отчётам.");
                }
            );
        }

        /// <summary>
        /// Справка по командам.
        /// </summary>
        [CommandMethod("GGDLLHELP")]
        public void DllHelpCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       TEST PRODUCT PLUGINDLL — Команды                       ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  GGDLLTEST     — Базовая команда (требует лицензию)           ║");
            sb.AppendLine("║  GGDLLINFO     — Информация о лицензии                    ║");
            sb.AppendLine("║  GGDLLREPORT   — Отчёт с Feature Limits                      ║");
            sb.AppendLine("║  GGDLLHELP     — Эта справка                                 ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  Тип дистрибуции: PluginDll (одиночная DLL)                   ║");
            sb.AppendLine("║  Устанавливается через NETLOAD или в                          ║");
            sb.AppendLine("║  GrossGeoPlatformApps.bundle автоматически                    ║");
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
