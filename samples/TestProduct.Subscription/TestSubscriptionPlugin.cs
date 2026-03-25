// =====================================================================
// TestProduct.Subscription — Trial + Subscription с Features
// Демонстрация: Trial, PlanTier.Pro, BillingModel.Subscription
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
        // API Key из DbSeeder.TrialSubscriptionPluginId
        private const string ProductKey = "GG-6E14-65BB-EF41-92C2";
        private const string PluginVersion = "1.1.0";

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
            GrossGeoLicense.Shutdown();
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
            sb.AppendLine($"IsValid:        {GrossGeoLicense.IsValid}");

            sb.AppendLine("\n═══ License Model ═══");
            sb.AppendLine($"PlanTier:       {GrossGeoLicense.PlanTier}");
            sb.AppendLine($"BillingModel:   {GrossGeoLicense.BillingModel}");
            sb.AppendLine($"LicenseMode:    {GrossGeoLicense.LicenseMode}");

            sb.AppendLine("\n═══ Subscription Status ═══");
            sb.AppendLine($"ExpiresAt:      {GrossGeoLicense.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A"}");
            sb.AppendLine($"DaysRemaining:  {GrossGeoLicense.DaysRemaining?.ToString() ?? "N/A"}");
            sb.AppendLine($"GracePeriod:    {GrossGeoLicense.IsInGracePeriod}");
            sb.AppendLine($"OfflineMode:    {GrossGeoLicense.IsOfflineMode}");

            sb.AppendLine("\n═══ Features ═══");
            sb.AppendLine($"basic:           {GrossGeoLicense.HasFeature("basic")}");
            sb.AppendLine($"export:          {GrossGeoLicense.HasFeature("export")}");
            sb.AppendLine($"advanced-export: {GrossGeoLicense.HasFeature("advanced-export")}");
            sb.AppendLine($"batch:           {GrossGeoLicense.HasFeature("batch")}");

            var features = GrossGeoLicense.Features;
            sb.AppendLine($"\nВсе Features: [{string.Join(", ", features)}]");

            WriteMessage(sb.ToString());
        }

        #endregion

        #region Commands - Licensed (требуют лицензию)

        /// <summary>
        /// Базовая команда - требует лицензию.
        /// </summary>
        [CommandMethod("GGSUBSTEST")]
        public void TestCommand()
        {
            // Мягкая защита с fallback
            LicenseGuard.Protect(
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  ✅ GGSUBSTEST - Лицензия активна      ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage($"Уровень плана: {GrossGeoLicense.PlanTier}");
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

        /// <summary>
        /// Проверка всех features.
        /// </summary>
        [CommandMethod("GGSUBSFEATURES")]
        public void FeaturesCommand()
        {
            WriteMessage("\n═══ Features Demo ═══");
            
            // Проверка basic
            FeatureGuard.Require("basic",
                action: () => WriteMessage("✅ [basic] Базовые функции доступны"),
                onMissing: () => WriteMessage("❌ [basic] Недоступно")
            );

            // Проверка export
            FeatureGuard.Require("export",
                action: () => WriteMessage("✅ [export] Экспорт доступен"),
                onMissing: () => WriteMessage("❌ [export] Недоступно")
            );

            // Проверка advanced-export (только Pro)
            FeatureGuard.Require("advanced-export",
                action: () => WriteMessage("✅ [advanced-export] Расширенный экспорт доступен (PRO)"),
                onMissing: () => WriteMessage("❌ [advanced-export] Требуется PRO подписка")
            );

            // Проверка batch (только Pro)
            FeatureGuard.Require("batch",
                action: () => WriteMessage("✅ [batch] Пакетная обработка доступна (PRO)"),
                onMissing: () => WriteMessage("❌ [batch] Требуется PRO подписка")
            );
        }

        /// <summary>
        /// Экспорт - требует feature "export".
        /// </summary>
        [CommandMethod("GGSUBSEXPORT")]
        public void ExportCommand()
        {
            FeatureGuard.Require("export",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📤 GGSUBSEXPORT - Экспорт             ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Экспорт выполнен успешно!");
                    
                    // Проверяем advanced-export
                    if (FeatureGuard.Has("advanced-export"))
                    {
                        WriteMessage("🌟 PRO: Расширенные опции экспорта доступны");
                    }
                    else
                    {
                        WriteMessage("💡 Совет: Обновитесь до PRO для расширенного экспорта");
                    }
                },
                onMissing: () =>
                {
                    WriteMessage("\n❌ Feature 'export' недоступен");
                    WriteMessage("Оформите подписку для доступа к экспорту");
                }
            );
        }

        /// <summary>
        /// Пакетная обработка - требует feature "batch" (PRO).
        /// </summary>
        [CommandMethod("GGSUBSBATCH")]
        public void BatchCommand()
        {
            // Жёсткая защита - выбросит исключение
            try
            {
                FeatureGuard.OrThrow("batch", () =>
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

