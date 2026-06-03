// =====================================================================
// TestProduct.Freemium — Бесплатный базовый + PRO фичи по подписке
// Демонстрация: Freemium, Feature Limits, PlanTier, BillingModel
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

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
        // API Key из DbSeeder.FreemiumPluginId
        private const string ProductKey = "GG-FB8E-E1E2-0918-C769";
        private const string PluginVersion = "1.1.0";

        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        private static ProductLicenseAccessor? _license;

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

                _license = GrossGeoLicense.ForProduct(ProductKey);

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
                WriteMessage($"[SDK] Инфо: GGFMINFO, GGFMUPGRADE");
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
                var lic = _license ?? GrossGeoLicense.ForProduct(ProductKey);
                var result = await lic.RefreshAsync();
                
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
            sb.AppendLine($"PlanTier:       {_license?.PlanTier}");
            sb.AppendLine($"BillingModel:   {_license?.BillingModel}");
            sb.AppendLine($"LicenseMode:    {_license?.LicenseMode}");

            sb.AppendLine("\n═══ Free Tier Features ═══");
            sb.AppendLine($"basic-tools:    {(_license?.HasFeature("basic-tools") == true ? "✅" : "❌")}");
            sb.AppendLine($"simple-export:  {(_license?.HasFeature("simple-export") == true ? "✅" : "❌")}");

            sb.AppendLine("\n═══ PRO Features ═══");
            sb.AppendLine($"advanced-tools:    {(_license?.HasFeature("advanced-tools") == true ? "✅ (PRO)" : "🔒 Требуется PRO")}");
            sb.AppendLine($"batch-processing:  {(_license?.HasFeature("batch-processing") == true ? "✅ (PRO)" : "🔒 Требуется PRO")}");
            sb.AppendLine($"cloud-sync:        {(_license?.HasFeature("cloud-sync") == true ? "✅ (PRO)" : "🔒 Требуется PRO")}");
            sb.AppendLine($"priority-support:  {(_license?.HasFeature("priority-support") == true ? "✅ (PRO)" : "🔒 Требуется PRO")}");

            // Feature Limits
            sb.AppendLine("\n═══ Feature Limits ═══");
            var batchLimit = _license?.GetFeatureLimit("batch-processing", "maxPerCall");
            var exportSizeLimit = _license?.GetFeatureLimit("simple-export", "maxSize");
            sb.AppendLine($"batch-processing.maxPerCall: {(batchLimit.HasValue ? batchLimit.Value.ToString() : "безлимитно")}");
            sb.AppendLine($"simple-export.maxSize:       {(exportSizeLimit.HasValue ? $"{exportSizeLimit.Value} bytes" : "безлимитно")}");

            var isPro = _license?.HasFeature("advanced-tools") == true;
            sb.AppendLine($"\n💡 Статус: {(isPro ? "PRO пользователь" : "Free Tier")}");

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Базовые инструменты (Free).
        /// </summary>
        [CommandMethod("GGFMBASIC")]
        public void BasicToolsCommand()
        {
            // Проверяем default feature (всегда доступна для FREEMIUM)
            _license?.RequireFeature("basic-tools",
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
            _license?.RequireFeature("simple-export",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📤 GGFMEXPORT - Простой экспорт       ║");
                    WriteMessage("║  Free Tier                             ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage("Экспорт в базовом формате выполнен!");

                    // Upsell
                    if (_license?.HasFeature("advanced-tools") != true)
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
            if (_license?.HasFeature("advanced-tools") == true)
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
            _license?.RequireFeature("advanced-tools",
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
            _license?.RequireFeature("batch-processing",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📦 GGFMBATCH — Пакетная обработка     ║");
                    WriteMessage("║  PRO Feature + Limits                  ║");
                    WriteMessage("╚════════════════════════════════════════╝");

                    // Проверяем лимит через SDK
                    var objectCount = 50; // Симуляция: выбрано 50 объектов
                    var limit = _license?.GetFeatureLimit("batch-processing", "maxPerCall");

                    WriteMessage($"Выбрано объектов: {objectCount}");
                    WriteMessage($"Лимит maxPerCall: {(limit.HasValue ? limit.Value.ToString() : "безлимитно")}");

                    // Проверка через RequireLimit
                    _license?.RequireLimit(
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
                var value = _license?.GetFeatureLimit(feature, limit);
                sb.AppendLine($"  {feature}.{limit}: {(value.HasValue ? value.Value.ToString() : "null (безлимитно)")}");
            }

            // Проверка CheckLimit
            sb.AppendLine("\n═══ CheckLimit Examples ═══");
            var check1 = _license?.CheckLimit("batch-processing", "maxPerCall", 5);
            var check2 = _license?.CheckLimit("batch-processing", "maxPerCall", 500);
            sb.AppendLine($"  CheckLimit(batch, maxPerCall, 5):   {(check1 == true ? "✅ OK" : "❌ Exceeded")}");
            sb.AppendLine($"  CheckLimit(batch, maxPerCall, 500): {(check2 == true ? "✅ OK" : "❌ Exceeded")}");

            // FeatureLimits словарь
            var allLimits = _license?.FeatureLimits;
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
            _license?.RequireFeature("cloud-sync",
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

