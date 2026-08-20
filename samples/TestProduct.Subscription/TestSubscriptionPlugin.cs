// =====================================================================
// TestProduct.Subscription — Trial + Subscription с Features
// Демонстрация: Trial, PlanTier.Pro, BillingModel.Subscription,
//               Usage Tracking (v3) — MaxPerMonth
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

[assembly: CommandClass(typeof(TestProduct.Subscription.TestSubscriptionPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Subscription.TestSubscriptionPlugin))]

namespace TestProduct.Subscription
{
    /// <summary>
    /// Тестовый плагин с Trial и подпиской.
    /// Демонстрирует: Trial, PlanTier.Pro, BillingModel.Subscription, LicenseMode.User.
    /// </summary>
    public class TestSubscriptionPlugin : IExtensionApplication
    {
        // Демонстрационный ProductKey. Для своего продукта возьмите ключ в Developer Portal.
        private const string ProductKey = "GG-57AA-E102-E1B8-3140";
        private const string PluginVersion = "1.1.0";

        private static ProductLicenseAccessor License => GrossGeoLicense.ForProduct(ProductKey);
        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  🔄 TEST PRODUCT SUBSCRIPTION v1.1.0                          ║");
            WriteMessage("║  Trial 14 дней → Подписка Standard/Pro                        ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  PlanTier=Pro, BillingModel=Subscription, Mode=User             ║");
            WriteMessage("║  Trial: 14 дней                                               ║");
            WriteMessage("║  Standard: 990₽/мес (basic, export)                           ║");
            WriteMessage("║  Pro: 1990₽/мес (+ advanced-export, batch)                    ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");

            _ = InitializeSdkAsync();
        }

        public void Terminate()
        {
            GrossGeoLicense.Shutdown(ProductKey);
            WriteMessage("\n[TestProduct.Subscription] Плагин выгружен");
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

                WriteMessage($"\n[SDK] Результат:");
                WriteMessage($"      - Статус: {result.Status}");
                WriteMessage($"      - IsValid: {result.IsValid}");
                WriteMessage($"      - PlanTier: {result.PlanTier}");
                WriteMessage($"      - BillingModel: {result.BillingModel}");
                WriteMessage($"      - LicenseMode: {result.LicenseMode}");
                WriteMessage($"      - Истекает: {result.ExpiresAt?.ToString("dd.MM.yyyy HH:mm") ?? "N/A"}");
                WriteMessage($"      - Дней осталось: {result.DaysRemaining?.ToString() ?? "N/A"}");
                WriteMessage($"      - Grace Period: {result.IsInGracePeriod}");
                WriteMessage($"      - Offline: {result.IsOfflineMode}");

                if (result.Features?.Count > 0)
                {
                    WriteMessage($"      - Features: {string.Join(", ", result.Features)}");
                }

                if (result.IsValid)
                {
                    WriteMessage($"\n[SDK] ✅ Лицензия активна!");
                    WriteMessage($"[SDK] Команды: GGSUBSTEST, GGSUBSFEATURES, GGSUBSEXPORT, GGSUBSBATCH");
                    WriteMessage($"[SDK] Usage (v3): GGSUBSUSAGE");
                }
                else
                {
                    WriteMessage($"\n[SDK] ⚠️ Лицензия не найдена");
                    WriteMessage($"[SDK] Запустите Trial через User Panel или оформите подписку");
                    WriteMessage($"[SDK] Доступна только команда: GGSUBSINFO");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ❌ Ошибка: {ex.Message}");
            }
        }

        #endregion

        #region Commands - Basic (доступны всем)

        /// <summary>
        /// Информация о лицензии и features.
        /// </summary>
        [CommandMethod("GGSUBSINFO")]
        public void InfoCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n═══ Subscription Product Info ═══");
            sb.AppendLine($"IsInitialized:  {GrossGeoLicense.IsInitialized}");
            sb.AppendLine($"IsValid:        {License.IsValid}");

            sb.AppendLine("\n═══ License Model ═══");
            sb.AppendLine($"PlanTier:       {License.PlanTier}");
            sb.AppendLine($"BillingModel:   {License.BillingModel}");
            sb.AppendLine($"LicenseMode:    {License.LicenseMode}");

            sb.AppendLine("\n═══ Subscription Status ═══");
            sb.AppendLine($"ExpiresAt:      {License.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A"}");
            sb.AppendLine($"DaysRemaining:  {License.DaysRemaining?.ToString() ?? "N/A"}");
            sb.AppendLine($"GracePeriod:    {License.IsInGracePeriod}");
            sb.AppendLine($"OfflineMode:    {License.IsOfflineMode}");

            sb.AppendLine("\n═══ Features ═══");
            sb.AppendLine($"basic:           {License.HasFeature("basic")}");
            sb.AppendLine($"export:          {License.HasFeature("export")}");
            sb.AppendLine($"advanced-export: {License.HasFeature("advanced-export")}");
            sb.AppendLine($"batch:           {License.HasFeature("batch")}");

            var features = License.Features;
            sb.AppendLine($"\nВсе Features: [{string.Join(", ", features)}]");

            WriteMessage(sb.ToString());
        }

        #endregion

        #region Commands - Licensed (требуют лицензию)

        /// <summary>
        /// Базовая команда - требует лицензию.
        /// </summary>
        [CommandMethod("GGSUBSTEST")]
        public async void TestCommand()
        {
            try
            {
                await EnsureLicenseCurrentAsync();

                // Мягкая защита с fallback
                License.Protect(
                    action: () =>
                    {
                        WriteMessage("\n╔════════════════════════════════════════╗");
                        WriteMessage("║  ✅ GGSUBSTEST - Лицензия активна      ║");
                        WriteMessage("╚════════════════════════════════════════╝");
                        WriteMessage($"Уровень плана: {License.PlanTier}");
                        WriteMessage("Базовая команда выполнена успешно!");
                    },
                    onBlocked: () =>
                    {
                        WriteMessage("\n╔════════════════════════════════════════╗");
                        WriteMessage("║  ❌ Требуется лицензия                 ║");
                        WriteMessage("╚════════════════════════════════════════╝");
                        WriteMessage("Запустите Trial или оформите подписку.");
                    }
                );
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверка всех features.
        /// </summary>
        [CommandMethod("GGSUBSFEATURES")]
        public async void FeaturesCommand()
        {
            try
            {
                await EnsureLicenseCurrentAsync();

                WriteMessage("\n═══ Features Demo ═══");

                // Проверка basic
                License.RequireFeature("basic",
                    action: () => WriteMessage("✅ [basic] Базовые функции доступны"),
                    onMissing: () => WriteMessage("❌ [basic] Недоступно")
                );

                // Проверка export
                License.RequireFeature("export",
                    action: () => WriteMessage("✅ [export] Экспорт доступен"),
                    onMissing: () => WriteMessage("❌ [export] Недоступно")
                );

                // Проверка advanced-export (только Pro)
                License.RequireFeature("advanced-export",
                    action: () => WriteMessage("✅ [advanced-export] Расширенный экспорт доступен (PRO)"),
                    onMissing: () => WriteMessage("❌ [advanced-export] Требуется PRO подписка")
                );

                // Проверка batch (только Pro)
                License.RequireFeature("batch",
                    action: () => WriteMessage("✅ [batch] Пакетная обработка доступна (PRO)"),
                    onMissing: () => WriteMessage("❌ [batch] Требуется PRO подписка")
                );
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Экспорт - требует feature "export".
        /// v3: Учитывает месячный лимит экспортов через Usage Tracking.
        /// </summary>
        [CommandMethod("GGSUBSEXPORT")]
        public async void ExportCommand()
        {
            try
            {
                await EnsureLicenseCurrentAsync();

                if (!License.HasFeature("export"))
                {
                    WriteMessage("\n❌ Feature 'export' недоступен");
                    WriteMessage("Оформите подписку для доступа к экспорту");
                    return;
                }

                // v3: Проверяем месячный лимит перед экспортом
                var monthlyLimit = License.GetFeatureLimit("export", "monthlyExports");
                if (monthlyLimit.HasValue)
                {
                    var currentUsage = await License.GetCurrentUsageAsync("export", "monthlyExports");
                    if (currentUsage >= monthlyLimit.Value)
                    {
                        WriteMessage($"\n❌ Месячный лимит экспортов исчерпан ({currentUsage}/{monthlyLimit.Value})");
                        WriteMessage("Обновитесь до Pro+ для безлимитного экспорта");
                        return;
                    }
                }

                WriteMessage("\n╔════════════════════════════════════════╗");
                WriteMessage("║  📤 GGSUBSEXPORT - Экспорт             ║");
                WriteMessage("╚════════════════════════════════════════╝");
                WriteMessage("Экспорт выполнен успешно!");

                // v3: Инкрементируем usage после успешного экспорта
                var usageResult = await License.IncrementUsageAsync("export", "monthlyExports");
                if (usageResult.IsSuccess && usageResult.Limit.HasValue)
                {
                    WriteMessage($"📊 Usage: {usageResult.CurrentUsage}/{usageResult.Limit.Value} экспортов в этом месяце");
                }

                if (License.HasFeature("advanced-export"))
                {
                    WriteMessage("🌟 PRO: Расширенные опции экспорта доступны");
                }
                else
                {
                    WriteMessage("💡 Совет: Обновитесь до PRO для расширенного экспорта");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// v3: Отчёт по использованию (Usage Tracking).
        /// </summary>
        [CommandMethod("GGSUBSUSAGE")]
        public async void UsageReportCommand()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("\n═══ Usage Report (v3) ═══");
                sb.AppendLine($"PlanTier: {License.PlanTier}");
                sb.AppendLine($"BillingModel: {License.BillingModel}");

                var monthlyUsage = await License.GetCurrentUsageAsync("export", "monthlyExports");
                var monthlyLimit = License.GetFeatureLimit("export", "monthlyExports");

                sb.AppendLine("\n[Экспорт — месячный лимит]");
                var maxStr = monthlyLimit.HasValue ? monthlyLimit.Value.ToString() : "∞ (безлимит) ";
                sb.AppendLine($"  Использовано: {monthlyUsage} / {maxStr}");

                if (monthlyLimit.HasValue && monthlyLimit.Value > 0)
                {
                    var pct = monthlyUsage * 100 / monthlyLimit.Value;
                    var bar = pct >= 90 ? "⚠️ Почти исчерпан" : pct >= 50 ? "🟡 Более половины" : "🟢 В норме";
                    sb.AppendLine($"  Статус:      {bar} ({pct}%)");
                }

                sb.AppendLine("\nv3: MaxPerMonth сбрасывается 1-го числа каждого месяца.");
                sb.AppendLine("Обновление до Pro+ снимает лимиты.");

                WriteMessage(sb.ToString());
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Пакетная обработка - требует feature "batch" (PRO).
        /// </summary>
        [CommandMethod("GGSUBSBATCH")]
        public async void BatchCommand()
        {
            // Жёсткая защита - выбросит исключение
            var lic = License;
            try
            {
                await EnsureLicenseCurrentAsync();

                lic.RequireFeatureOrThrow("batch", () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📦 GGSUBSBATCH - Пакетная обработка   ║");
                    WriteMessage("║  🌟 PRO Feature                        ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Пакетная обработка запущена...");
                    WriteMessage("Обработано 10 файлов.");
                });
            }
            catch (FeatureNotAvailableException ex)
            {
                WriteMessage($"\n❌ FeatureNotAvailableException: {ex.FeatureCode}");
                WriteMessage("Эта функция доступна только в PRO подписке");
                WriteMessage("Обновите план: User Panel → Моя подписка → Изменить план");
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Если лицензия невалидна, запрашивает актуальные данные у User Panel.
        /// Нужно для сценария: Trial истёк → куплена подписка → SDK ещё не знает.
        /// </summary>
        private static async Task EnsureLicenseCurrentAsync()
        {
            if (!License.IsValid)
            {
                WriteMessage("[SDK] Лицензия не актуальна, обновление...");
                var refreshed = await License.RefreshAsync();
                WriteMessage($"[SDK] Обновлено: IsValid={refreshed.IsValid}, PlanTier={refreshed.PlanTier}");
            }
        }

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

