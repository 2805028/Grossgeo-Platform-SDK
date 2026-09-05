// =====================================================================
// TestProduct.Freemium — Бесплатный базовый + PRO фичи по подписке
// Демонстрация: Freemium, Feature Limits, Usage Tracking (v3)
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;
using GrossGeo.Contracts.Licensing;

[assembly: CommandClass(typeof(TestProduct.Freemium.TestFreemiumPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Freemium.TestFreemiumPlugin))]

namespace TestProduct.Freemium
{
    /// <summary>
    /// Тестовый FREEMIUM плагин.
    /// Демонстрирует: Feature Limits (GetFeatureLimit, CheckLimit, RequireLimit).
    /// </summary>
    public class TestFreemiumPlugin : IExtensionApplication
    {
        // Демонстрационный ProductKey. Для своего продукта возьмите ключ в Developer Portal.
        private const string ProductKey = "GG-D7E6-51E3-D5AF-1E11";
        private const string PluginVersion = "1.1.0";

        private static ProductLicenseAccessor License => GrossGeoLicense.ForProduct(ProductKey);
        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  ⭐ TEST PRODUCT FREEMIUM v1.1.0                               ║");
            WriteMessage("║  Базовые функции бесплатно, PRO по подписке                   ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  PlanTier=Free/Pro, BillingModel=Free/Subscription             ║");
            WriteMessage("║  Feature Limits: GetFeatureLimit, CheckLimit, RequireLimit     ║");
            WriteMessage("║  Free: basic-tools, simple-export                             ║");
            WriteMessage("║  PRO (490₽/мес): + advanced-tools, batch, cloud, support      ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");

            _ = InitializeSdkAsync();
        }

        public void Terminate()
        {
            GrossGeoLicense.Shutdown(ProductKey);
            WriteMessage("\n[TestProduct.Freemium] Плагин выгружен");
        }

        #endregion

        #region SDK Initialization

        private async Task InitializeSdkAsync()
        {
            try
            {
                WriteMessage($"\n[SDK] Инициализация FREEMIUM продукта...");
                WriteMessage($"[SDK] ProductKey: {MaskKey(ProductKey)}");

                var result = await GrossGeoLicense.Initialize(new LicenseOptions
                {
                    ProductKey = ProductKey,
                    PluginVersion = PluginVersion,
                    GracePeriodDays = 7
                });

                WriteMessage($"\n[SDK] Результат:");
                WriteMessage($"      - Статус: {result.Status}");
                WriteMessage($"      - PlanTier: {result.PlanTier}");
                WriteMessage($"      - BillingModel: {result.BillingModel}");
                WriteMessage($"      - LicenseMode: {result.LicenseMode}");

                if (result.Features?.Count > 0)
                {
                    WriteMessage($"      - Features: {string.Join(", ", result.Features)}");
                }

                // FREEMIUM всегда работает - разница только в доступных фичах
                WriteMessage($"\n[SDK] ✅ Плагин готов к работе!");
                WriteMessage($"[SDK] Бесплатные команды: GGFMBASIC, GGFMEXPORT");
                WriteMessage($"[SDK] PRO команды: GGFMADVANCED, GGFMBATCH, GGFMCLOUD");
                WriteMessage($"[SDK] Usage (v3): GGFMUSAGE, GGFMUSAGEREPORT");
                WriteMessage($"[SDK] Инфо: GGFMINFO, GGFMLIMITS, GGFMREFRESH, GGFMUPGRADE");
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ⚠️ Ошибка: {ex.Message}");
                WriteMessage($"[SDK] Бесплатные функции всё равно доступны!");
            }
        }

        #endregion

        #region Commands - Free Tier (всегда доступны)

        /// <summary>
        /// Принудительно обновить данные лицензии (после покупки/триала).
        /// </summary>
        [CommandMethod("GGFMREFRESH")]
        public async void RefreshCommand()
        {
            try
            {
                WriteMessage("\n[SDK] Обновление данных лицензии...");
                var result = await License.RefreshAsync();
                
                WriteMessage($"[SDK] Статус: {result.Status}");
                WriteMessage($"[SDK] План: {result.PlanTier}");
                if (result.Features?.Count > 0)
                {
                    WriteMessage($"[SDK] Features: {string.Join(", ", result.Features)}");
                }
                WriteMessage("[SDK] ✅ Данные обновлены!");
            }
            catch (System.Exception ex)
            {
                WriteMessage($"[SDK] ❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Информация о статусе, features и лимитах.
        /// </summary>
        [CommandMethod("GGFMINFO")]
        public void InfoCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n═══ Freemium Product Info ═══");
            sb.AppendLine($"IsInitialized:  {GrossGeoLicense.IsInitialized}");
            sb.AppendLine($"PlanTier:       {License.PlanTier}");
            sb.AppendLine($"BillingModel:   {License.BillingModel}");
            sb.AppendLine($"LicenseMode:    {License.LicenseMode}");

            sb.AppendLine("\n═══ Free Tier Features ═══");
            sb.AppendLine($"basic-tools:    {(License.HasFeature("basic-tools") ? "✅" : "❌")}");
            sb.AppendLine($"simple-export:  {(License.HasFeature("simple-export") ? "✅" : "❌")}");

            sb.AppendLine("\n═══ PRO Features ═══");
            sb.AppendLine($"advanced-tools:    {(License.HasFeature("advanced-tools") ? "✅ (PRO)" : "🔒 Требуется PRO")}");
            sb.AppendLine($"batch-processing:  {(License.HasFeature("batch-processing") ? "✅ (PRO)" : "🔒 Требуется PRO")}");
            sb.AppendLine($"cloud-sync:        {(License.HasFeature("cloud-sync") ? "✅ (PRO)" : "🔒 Требуется PRO")}");
            sb.AppendLine($"priority-support:  {(License.HasFeature("priority-support") ? "✅ (PRO)" : "🔒 Требуется PRO")}");

            // Feature Limits
            sb.AppendLine("\n═══ Feature Limits ═══");
            var batchLimit = License.GetFeatureLimit("batch-processing", "maxPerCall");
            var exportSizeLimit = License.GetFeatureLimit("simple-export", "maxSize");
            sb.AppendLine($"batch-processing.maxPerCall: {(batchLimit.HasValue ? batchLimit.Value.ToString() : "безлимитно")}");
            sb.AppendLine($"simple-export.maxSize:       {(exportSizeLimit.HasValue ? $"{exportSizeLimit.Value} bytes" : "безлимитно")}");

            var isPro = License.HasFeature("advanced-tools");
            sb.AppendLine($"\n💡 Статус: {(isPro ? "PRO пользователь" : "Free Tier")}");

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Базовые инструменты (Free).
        /// </summary>
        [CommandMethod("GGFMBASIC")]
        public void BasicToolsCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            // Проверяем default feature (всегда доступна для FREEMIUM)
            License.RequireFeature("basic-tools",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  🔧 GGFMBASIC - Базовые инструменты    ║");
                    WriteMessage("║  Free Tier                             ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Базовые инструменты работают!");
                    WriteMessage("Это бесплатная функция.");
                },
                onMissing: () =>
                {
                    // Для FREEMIUM это не должно происходить
                    WriteMessage("❌ Ошибка: basic-tools недоступны");
                }
            );
        }

        /// <summary>
        /// Простой экспорт (Free).
        /// </summary>
        [CommandMethod("GGFMEXPORT")]
        public void SimpleExportCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            License.RequireFeature("simple-export",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📤 GGFMEXPORT - Простой экспорт       ║");
                    WriteMessage("║  Free Tier                             ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Экспорт в базовом формате выполнен!");

                    // Upsell
                    if (!License.HasFeature("advanced-tools"))
                    {
                        WriteMessage("\n💡 Совет: Обновитесь до PRO для расширенных форматов!");
                        WriteMessage("   Используйте команду GGFMUPGRADE");
                    }
                },
                onMissing: () =>
                {
                    WriteMessage("❌ Ошибка: simple-export недоступен");
                }
            );
        }

        /// <summary>
        /// Информация об апгрейде.
        /// </summary>
        [CommandMethod("GGFMUPGRADE")]
        public void UpgradeCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            if (License.HasFeature("advanced-tools"))
            {
                WriteMessage("\n✅ Вы уже PRO пользователь!");
                WriteMessage("Все функции доступны.");
                return;
            }

            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  ⭐ ОБНОВИТЕ ДО PRO!                                          ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  Всего 490₽/месяц (или 4900₽/год с экономией 15%)            ║");
            WriteMessage("║                                                               ║");
            WriteMessage("║  PRO включает:                                                ║");
            WriteMessage("║  ✅ advanced-tools - Продвинутые инструменты                  ║");
            WriteMessage("║  ✅ batch-processing - Пакетная обработка                     ║");
            WriteMessage("║  ✅ cloud-sync - Облачная синхронизация                       ║");
            WriteMessage("║  ✅ priority-support - Приоритетная поддержка                 ║");
            WriteMessage("║                                                               ║");
            WriteMessage("║  7 дней бесплатного Trial!                                    ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");
            WriteMessage("\nОткройте User Panel → Каталог → Freemium Tool Suite → Подписка");
        }

        #endregion

        #region Commands - PRO Features

        /// <summary>
        /// Продвинутые инструменты (PRO).
        /// </summary>
        [CommandMethod("GGFMADVANCED")]
        public void AdvancedToolsCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            License.RequireFeature("advanced-tools",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  🌟 GGFMADVANCED - Продвинутые         ║");
                    WriteMessage("║  PRO Feature                           ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Продвинутые инструменты активированы!");
                    WriteMessage("Спасибо за подписку PRO!");
                },
                onMissing: () =>
                {
                    WriteMessage("\n🔒 Требуется PRO подписка");
                    WriteMessage("Команда GGFMUPGRADE покажет информацию об апгрейде");
                }
            );
        }

        /// <summary>
        /// Пакетная обработка (PRO) с Feature Limits.
        /// </summary>
        [CommandMethod("GGFMBATCH")]
        public void BatchCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            License.RequireFeature("batch-processing",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📦 GGFMBATCH — Пакетная обработка     ║");
                    WriteMessage("║  PRO Feature + Limits                  ║");
                    WriteMessage("╚════════════════════════════════════════╝");

                    // Проверяем лимит через SDK
                    var objectCount = 50; // Симуляция: выбрано 50 объектов
                    var limit = License.GetFeatureLimit("batch-processing", "maxPerCall");

                    WriteMessage($"Выбрано объектов: {objectCount}");
                    WriteMessage($"Лимит maxPerCall: {(limit.HasValue ? limit.Value.ToString() : "безлимитно")}");

                    // Проверка через RequireLimit
                    License.RequireLimit(
                        "batch-processing", "maxPerCall", objectCount,
                        action: () =>
                        {
                            WriteMessage($"✅ Лимит не превышен — обработано {objectCount} объектов!");
                        },
                        onLimitExceeded: (maxValue) =>
                        {
                            WriteMessage($"❌ Превышен лимит! Макс: {maxValue}, выбрано: {objectCount}");
                            WriteMessage("Обновитесь до PRO+ для увеличенных лимитов.");
                        }
                    );
                },
                onMissing: () =>
                {
                    WriteMessage("\n🔒 Пакетная обработка доступна только в PRO");
                    WriteMessage("Free Tier: обрабатывайте файлы по одному");
                }
            );
        }

        /// <summary>
        /// Демонстрация Feature Limits API.
        /// </summary>
        [CommandMethod("GGFMLIMITS")]
        public void LimitsCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("\n═══ Feature Limits Demo ═══");

            // Получение лимитов через GetFeatureLimit
            var limitCodes = new[]
            {
                ("batch-processing", "maxPerCall"),
                ("batch-processing", "maxPerSession"),
                ("simple-export", "maxSize"),
                ("cloud-sync", "maxSize"),
            };

            foreach (var (feature, limit) in limitCodes)
            {
                var value = License.GetFeatureLimit(feature, limit);
                sb.AppendLine($"  {feature}.{limit}: {(value.HasValue ? value.Value.ToString() : "null (безлимитно)")}");
            }

            // Проверка CheckLimit
            sb.AppendLine("\n═══ CheckLimit Examples ═══");
            var check1 = License.CheckLimit("batch-processing", "maxPerCall", 5);
            var check2 = License.CheckLimit("batch-processing", "maxPerCall", 500);
            sb.AppendLine($"  CheckLimit(batch, maxPerCall, 5):   {(check1 ? "✅ OK" : "❌ Exceeded")}");
            sb.AppendLine($"  CheckLimit(batch, maxPerCall, 500): {(check2 ? "✅ OK" : "❌ Exceeded")}");

            // FeatureLimits словарь
            var allLimits = License.FeatureLimits;
            if (allLimits != null && allLimits.Count > 0)
            {
                sb.AppendLine("\n═══ All FeatureLimits (raw) ═══");
                foreach (var kv in allLimits)
                {
                    sb.AppendLine($"  {kv.Key} = {kv.Value}");
                }
            }
            else
            {
                sb.AppendLine("\n  (Нет лимитов — безлимитный доступ или план без лимитов)");
            }

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Облачная синхронизация (PRO).
        /// </summary>
        [CommandMethod("GGFMCLOUD")]
        public void CloudSyncCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            License.RequireFeature("cloud-sync",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  ☁️ GGFMCLOUD - Облачная синхронизация ║");
                    WriteMessage("║  PRO Feature                           ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Синхронизация с облаком...");
                    WriteMessage("✅ Все настройки синхронизированы!");
                },
                onMissing: () =>
                {
                    WriteMessage("\n🔒 Облачная синхронизация доступна только в PRO");
                    WriteMessage("Free Tier: настройки хранятся локально");
                }
            );
        }

        #endregion

        #region Commands — Usage Tracking (v3)

        /// <summary>
        /// v3: Демонстрация Usage Tracking — IncrementUsageAsync / GetCurrentUsageAsync.
        /// Подсчёт операций для лимитов MaxPerDay/MaxPerMonth.
        /// </summary>
        [CommandMethod("GGFMUSAGE")]
        public async void UsageTrackingCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            try
            {
                WriteMessage("\n═══ Usage Tracking Demo (v3) ═══");

                // 1. Получаем текущий usage до операции
                var currentUsage = await GrossGeoLicense.GetCurrentUsageAsync("batch-processing", "maxPerDay");
                var dailyLimit = License.GetFeatureLimit("batch-processing", "maxPerDay");

                WriteMessage($"[Usage] Текущий usage (maxPerDay): {currentUsage}");
                WriteMessage($"[Usage] Дневной лимит:             {(dailyLimit.HasValue ? dailyLimit.Value.ToString() : "безлимитно")}");

                // 2. Проверяем лимит до операции
                if (dailyLimit.HasValue && currentUsage >= dailyLimit.Value)
                {
                    WriteMessage($"\n❌ Дневной лимит исчерпан ({currentUsage}/{dailyLimit.Value})");
                    WriteMessage("   Лимит сбросится в 00:00 UTC.");
                    WriteMessage("   Обновитесь до PRO для безлимитного доступа: GGFMUPGRADE");
                    return;
                }

                // 3. Выполняем операцию
                WriteMessage("\n[Usage] Выполнение операции пакетной обработки...");
                var objectsProcessed = 5;

                // 4. Инкрементируем usage после успешной операции
                var result = await License.IncrementUsageAsync("batch-processing", "maxPerDay", objectsProcessed);

                if (result.IsSuccess)
                {
                    WriteMessage($"✅ Обработано: {objectsProcessed} объектов");
                    WriteMessage($"   Текущий usage:  {result.CurrentUsage}");

                    if (result.Limit.HasValue)
                    {
                        WriteMessage($"   Лимит:          {result.Limit.Value}");
                        WriteMessage($"   Осталось:       {result.Remaining ?? (result.Limit.Value - result.CurrentUsage)}");
                    }
                    else
                    {
                        WriteMessage($"   Лимит:          безлимитно");
                    }
                }
                else
                {
                    WriteMessage($"⚠️ Ошибка учёта: {result.ErrorCode} — {result.ErrorMessage}");
                    WriteMessage("   Операция выполнена, но usage не обновлён.");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// v3: Отчёт по использованию — текущий usage для всех отслеживаемых лимитов.
        /// </summary>
        [CommandMethod("GGFMUSAGEREPORT")]
        public async void UsageReportCommand()
        {
            // LGC-720: дождитесь инициализации. Она идёт в фоне (иначе встанет загрузка
            // AutoCAD), и команду можно запустить раньше, чем появится вердикт. Без
            // ожидания отказ НЕОТЛИЧИМ от «лицензии нет».
            var ready = GrossGeoLicense.WaitUntilReady(TimeSpan.FromSeconds(10));
            if (ready.Status == LicenseCheckStatus.Unknown)
            {
                WriteMessage("\n" + ready.Message);   // «спросить не удалось», не «прав нет»
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("\n═══ Usage Report (v3) ═══");
                sb.AppendLine($"PlanTier: {License.PlanTier}");

                var limitsToTrack = new[]
                {
                    ("batch-processing", "maxPerDay",     "Пакетная/день"),
                    ("batch-processing", "maxPerSession", "Пакетная/сессия"),
                    ("simple-export",    "maxPerCall",    "Экспорт/вызов"),
                };

                sb.AppendLine("\n[Текущий usage]");
                foreach (var (feature, limit, label) in limitsToTrack)
                {
                    var usage = await GrossGeoLicense.GetCurrentUsageAsync(feature, limit);
                    var maxVal = License.GetFeatureLimit(feature, limit);

                    var maxStr = maxVal.HasValue ? maxVal.Value.ToString() : "∞";
                    var bar = maxVal.HasValue && maxVal.Value > 0
                        ? $" ({usage * 100 / maxVal.Value}%)"
                        : "";

                    sb.AppendLine($"  {label,-22} {usage,5} / {maxStr,5}{bar}");
                }

                sb.AppendLine("\nv3: Usage сбрасывается автоматически:");
                sb.AppendLine("  • MaxPerDay  — в 00:00 UTC");
                sb.AppendLine("  • MaxPerMonth — 1-го числа");
                sb.AppendLine("  • PRO план = без ограничений");

                WriteMessage(sb.ToString());
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n❌ Ошибка: {ex.Message}");
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

