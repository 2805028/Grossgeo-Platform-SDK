// =====================================================================
// TestProduct.Concurrent — Multi-tier + Concurrent Sessions
// Демонстрация: Free/Pro/Pro+, LicenseMode.Concurrent, Heartbeat,
//               Feature Limits, Default Features (IsDefault)
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;
using GrossGeo.Contracts.Licensing;

[assembly: CommandClass(typeof(TestProduct.Concurrent.TestConcurrentPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Concurrent.TestConcurrentPlugin))]

namespace TestProduct.Concurrent
{
    /// <summary>
    /// Тестовый плагин с Multi-tier планами и Concurrent лицензированием.
    /// Демонстрирует: Free/Pro/Pro+, LicenseMode.Concurrent, heartbeat,
    /// Feature Limits, Default-фичи (IsDefault), AcquireSession/ReleaseSession.
    /// </summary>
    public class TestConcurrentPlugin : IExtensionApplication
    {
        // Демонстрационный ProductKey. Для своего продукта возьмите ключ в Developer Portal.
        private const string ProductKey = "GG-E597-C7A1-CCED-D6E2";
        private const string PluginVersion = "1.0.0";

        private static ProductLicenseAccessor License => GrossGeoLicense.ForProduct(ProductKey);
        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  🔁 TEST PRODUCT CONCURRENT v1.0.0                            ║");
            WriteMessage("║  Multi-tier + Concurrent Sessions                              ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  Free: basic-tools, view-objects (IsDefault)                   ║");
            WriteMessage("║  Pro (800₽/мес): + pro-tools, export-batch (LicenseMode=User) ║");
            WriteMessage("║  Pro+ (1500₽/мес): + enterprise-api, cloud (Concurrent)       ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");

            _ = InitializeSdkAsync();
        }

        public void Terminate()
        {
            // v2: Освобождаем concurrent-сессию при выгрузке
            if (License.HasActiveSession)
            {
                _ = License.ReleaseSessionAsync();
            }

            GrossGeoLicense.Shutdown(ProductKey);
        }

        #endregion

        #region SDK Initialization

        private async Task InitializeSdkAsync()
        {
            try
            {
                WriteMessage($"\n[SDK] Инициализация Multi-tier Concurrent продукта...");
                WriteMessage($"[SDK] ProductKey: {MaskKey(ProductKey)}");

                var result = await GrossGeoLicense.Initialize(new LicenseOptions
                {
                    ProductKey = ProductKey,
                    PluginVersion = PluginVersion,
                    GracePeriodDays = 7,
                    CheckForUpdatesOnInit = true
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

                // v2: Для Concurrent режима нужно получить сессию
                if (result.IsValid && result.LicenseMode == GrossGeo.Contracts.Licensing.LicenseMode.Concurrent)
                {
                    WriteMessage("\n[SDK] Concurrent режим — получаем сессию...");
                    var session = await License.AcquireSessionAsync(Environment.MachineName);

                    if (session.IsSuccess)
                    {
                        WriteMessage($"[SDK] ✅ Сессия получена!");
                        WriteMessage($"[SDK]    Token: {session.SessionToken?[..8]}...");
                        WriteMessage($"[SDK]    Истекает: {session.ExpiresAt:HH:mm:ss}");
                        WriteMessage("[SDK]    Heartbeat запущен (каждые 5 мин)");
                    }
                    else
                    {
                        WriteMessage($"[SDK] ⚠️ Не удалось получить сессию: {session.ErrorMessage}");
                        WriteMessage("[SDK] Возможно, все слоты заняты.");
                    }
                }

                // v2: Подписываемся на SessionExpired
                GrossGeoLicense.SessionExpired += OnSessionExpired;

                WriteMessage("\n[SDK] Команды: GGCONCINFO, GGCONCSESSION, GGCONCPUBLIC");
                WriteMessage("[SDK]          GGCONCPRO, GGCONCBATCH, GGCONCLIMITS, GGCONCHELP");
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ❌ Ошибка инициализации: {ex.Message}");
            }
        }

        private static void OnSessionExpired(object? sender, SessionExpiredEventArgs e)
        {
            WriteMessage($"\n[SDK] ⚠️ SESSION EXPIRED: {e.Message}");
            WriteMessage("[SDK] Сессия потеряна. Попробуйте GGCONCSESSION для получения новой.");
        }

        #endregion

        #region Commands — Info

        /// <summary>
        /// Полная информация о лицензии и сессии.
        /// </summary>
        [CommandMethod("GGCONCINFO")]
        public void InfoCommand()
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
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║          CONCURRENT TOOLKIT — Info                            ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");

            // Модель лицензирования
            sb.AppendLine("║  License Model:                                                ║");
            sb.AppendLine($"║    PlanTier:       {License.PlanTier,-39} ║");
            sb.AppendLine($"║    BillingModel:   {License.BillingModel,-39} ║");
            sb.AppendLine($"║    LicenseMode:    {License.LicenseMode,-39} ║");
            sb.AppendLine($"║    IsValid:        {License.IsValid,-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");

            // Concurrent session
            sb.AppendLine("║  Concurrent Session:                                          ║");
            sb.AppendLine($"║    HasSession:     {License.HasActiveSession,-39} ║");
            sb.AppendLine($"║    SessionToken:   {Truncate(License.SessionToken ?? "N/A", 39),-39} ║");
            sb.AppendLine($"║    ExpiresAt:      {License.SessionExpiresAt?.ToString("HH:mm:ss") ?? "N/A",-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");

            // Features
            sb.AppendLine("║  Features:                                                    ║");
            var featuresToCheck = new[] { "view-objects", "basic-tools", "pro-tools", "export-batch", "enterprise-api", "cloud-sync" };
            foreach (var f in featuresToCheck)
            {
                var has = License.HasFeature(f);
                var suffix = has ? "" : " [🔒]";
                var icon = has ? "✅" : "❌";
                sb.AppendLine($"║    {icon} {f,-20}{suffix,-32} ║");
            }

            // Limits
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  Feature Limits:                                              ║");
            var batchLimit = License.GetFeatureLimit("export-batch", "maxPerCall");
            sb.AppendLine($"║    export-batch.maxPerCall: {(batchLimit.HasValue ? batchLimit.Value.ToString() : "∞"),-31} ║");

            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");
            WriteMessage(sb.ToString());
        }

        #endregion

        #region Commands — Concurrent Session

        /// <summary>
        /// Управление concurrent-сессией (получить / освободить / heartbeat).
        /// </summary>
        [CommandMethod("GGCONCSESSION")]
        public async void SessionCommand()
        {
            WriteMessage("\n═══ Concurrent Session Management ═══");
            WriteMessage($"Текущий LicenseMode: {License.LicenseMode}");
            WriteMessage($"HasActiveSession:    {License.HasActiveSession}");

            if (License.HasActiveSession)
            {
                WriteMessage($"\n[Сессия активна]");
                WriteMessage($"  Token:     {Truncate(License.SessionToken ?? "", 20)}");
                WriteMessage($"  ExpiresAt: {License.SessionExpiresAt?.ToString("HH:mm:ss") ?? "N/A"}");

                WriteMessage("\n  Отправляем heartbeat...");
                var heartbeatOk = await License.SendSessionHeartbeatAsync();
                WriteMessage($"  Heartbeat: {(heartbeatOk ? "✅ OK" : "❌ Failed")}");

                WriteMessage("\n  Для освобождения сессии используйте GGCONCSESSIONRELEASE");
            }
            else
            {
                WriteMessage("\n[Нет активной сессии]");
                WriteMessage("  Попытка получить сессию...");

                var result = await License.AcquireSessionAsync(Environment.MachineName);

                if (result.IsSuccess)
                {
                    WriteMessage($"  ✅ Сессия получена!");
                    WriteMessage($"     SessionId: {result.SessionId}");
                    WriteMessage($"     Token:     {result.SessionToken?[..Math.Min(8, result.SessionToken?.Length ?? 0)]}...");
                    WriteMessage($"     ExpiresAt: {result.ExpiresAt:HH:mm:ss}");
                    WriteMessage("     Heartbeat автоматически запущен.");
                }
                else
                {
                    WriteMessage($"  ❌ Ошибка: {result.ErrorCode} — {result.ErrorMessage}");
                    WriteMessage("  Возможные причины:");
                    WriteMessage("  • Все concurrent слоты заняты");
                    WriteMessage("  • Лицензия не в Concurrent режиме");
                    WriteMessage("  • User Panel не запущен");
                }
            }
        }

        /// <summary>
        /// Освободить concurrent-сессию вручную.
        /// </summary>
        [CommandMethod("GGCONCSESSIONRELEASE")]
        public async void SessionReleaseCommand()
        {
            if (!License.HasActiveSession)
            {
                WriteMessage("\n[GGCONCSESSIONRELEASE] Нет активной сессии для освобождения.");
                return;
            }

            WriteMessage("\n[GGCONCSESSIONRELEASE] Освобождение сессии...");
            await License.ReleaseSessionAsync();
            WriteMessage("[GGCONCSESSIONRELEASE] ✅ Сессия освобождена. Слот доступен другим.");
        }

        #endregion

        #region Commands — Public Features

        /// <summary>
        /// Демо Default-фичей (IsDefault=true) — доступны во всех планах.
        /// </summary>
        [CommandMethod("GGCONCPUBLIC")]
        public void PublicFeaturesCommand()
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

            WriteMessage("\n═══ Default Features Demo (IsDefault) ═══");
            WriteMessage("Default-фичи доступны во всех планах (включая Free):\n");

            // view-objects — IsDefault=true
            License.RequireFeature("view-objects",
                action: () =>
                {
                    WriteMessage("  ✅ [view-objects] Просмотр объектов — DEFAULT фича");
                    WriteMessage("     Работает для всех планов: Free, Pro, Pro+");
                },
                onMissing: () =>
                {
                    WriteMessage("  ❌ [view-objects] Недоступна (неожиданно для PUBLIC фичи)");
                }
            );

            // basic-tools — IsDefault=true (только авторизованные)
            License.RequireFeature("basic-tools",
                action: () =>
                {
                    WriteMessage("  ✅ [basic-tools] Базовые инструменты — DEFAULT фича");
                    WriteMessage("     Работает для: Free, Pro, Pro+ (не Guest)");
                },
                onMissing: () =>
                {
                    WriteMessage("  ❌ [basic-tools] Недоступна — требуется авторизация (Free+)");
                }
            );

            // enterprise-api — только Pro+
            License.RequireFeature("enterprise-api",
                action: () =>
                {
                    WriteMessage("  ✅ [enterprise-api] Enterprise API — PRO+ фича");
                },
                onMissing: () =>
                {
                    WriteMessage("  🔒 [enterprise-api] Недоступна — требуется Pro+ план");
                }
            );
        }

        #endregion

        #region Commands — Pro Features

        /// <summary>
        /// Pro инструменты — требуют Plan ≥ Pro.
        /// </summary>
        [CommandMethod("GGCONCPRO")]
        public void ProToolsCommand()
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

            License.RequireFeature("pro-tools",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  🔧 GGCONCPRO — Pro инструменты        ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage($"PlanTier: {License.PlanTier}");
                    WriteMessage("Pro инструменты выполнены успешно!");
                },
                onMissing: () =>
                {
                    WriteMessage("\n🔒 Pro инструменты недоступны в Free плане.");
                    WriteMessage("Обновитесь до Pro (800₽/мес) или Pro+ (1500₽/мес).");
                }
            );
        }

        /// <summary>
        /// Пакетный экспорт — Pro/Pro+ с Feature Limits.
        /// </summary>
        [CommandMethod("GGCONCBATCH")]
        public void BatchExportCommand()
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

            License.RequireFeature("export-batch",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📦 GGCONCBATCH — Пакетный экспорт     ║");
                    WriteMessage("║  Feature Limits                        ║");
                    WriteMessage("╚════════════════════════════════════════╝");

                    // Симуляция: пользователь выбрал 75 объектов
                    var objectCount = 75;
                    var limit = License.GetFeatureLimit("export-batch", "maxPerCall");

                    WriteMessage($"Объектов выбрано: {objectCount}");
                    WriteMessage($"Лимит maxPerCall: {(limit.HasValue ? limit.Value.ToString() : "∞ (безлимит)")}");

                    if (License.CheckLimit("export-batch", "maxPerCall", objectCount))
                    {
                        WriteMessage($"✅ Лимит не превышен — экспорт {objectCount} объектов...");
                        WriteMessage("Экспорт завершён успешно!");
                    }
                    else
                    {
                        WriteMessage($"❌ Лимит превышен: {objectCount} > {limit}");
                        WriteMessage("   Обновитесь до Pro+ для увеличенных лимитов.");
                    }
                },
                onMissing: () =>
                {
                    WriteMessage("\n🔒 Пакетный экспорт недоступен в Free плане.");
                    WriteMessage("Обновитесь до Pro (800₽/мес).");
                }
            );
        }

        #endregion

        #region Commands — Limits & Help

        /// <summary>
        /// Демонстрация Feature Limits API.
        /// </summary>
        [CommandMethod("GGCONCLIMITS")]
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
            sb.AppendLine("\n═══ Feature Limits Overview ═══");
            sb.AppendLine($"PlanTier: {License.PlanTier}");

            sb.AppendLine("\n[GetFeatureLimit]");
            var limitPairs = new[]
            {
                ("export-batch", "maxPerCall"),
                ("export-batch", "maxPerSession"),
                ("cloud-sync", "maxSize"),
            };

            foreach (var (feature, limit) in limitPairs)
            {
                var val = License.GetFeatureLimit(feature, limit);
                sb.AppendLine($"  {feature}.{limit}: {(val.HasValue ? val.Value.ToString() : "∞")}");
            }

            sb.AppendLine("\n[CheckLimit Examples]");
            sb.AppendLine($"  CheckLimit(export-batch, maxPerCall, 10):   {(License.CheckLimit("export-batch", "maxPerCall", 10) ? "✅ OK" : "❌ Exceeded")}");
            sb.AppendLine($"  CheckLimit(export-batch, maxPerCall, 500):  {(License.CheckLimit("export-batch", "maxPerCall", 500) ? "✅ OK" : "❌ Exceeded")}");
            sb.AppendLine($"  CheckLimit(export-batch, maxPerCall, 5000): {(License.CheckLimit("export-batch", "maxPerCall", 5000) ? "✅ OK" : "❌ Exceeded")}");

            // Показываем все лимиты
            var allLimits = License.FeatureLimits;
            if (allLimits != null && allLimits.Count > 0)
            {
                sb.AppendLine("\n[All FeatureLimits]");
                foreach (var kv in allLimits)
                {
                    sb.AppendLine($"  {kv.Key} = {kv.Value}");
                }
            }
            else
            {
                sb.AppendLine("\n  (Нет лимитов — безлимитный доступ или лимиты не заданы)");
            }

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Справка по всем командам.
        /// </summary>
        [CommandMethod("GGCONCHELP")]
        public void HelpCommand()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       CONCURRENT TOOLKIT — Команды                          ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  Информация:                                                 ║");
            sb.AppendLine("║    GGCONCINFO          — Лицензия, сессия, features, limits   ║");
            sb.AppendLine("║    GGCONCHELP          — Эта справка                         ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  Concurrent Sessions:                                          ║");
            sb.AppendLine("║    GGCONCSESSION       — Получить / heartbeat сессию          ║");
            sb.AppendLine("║    GGCONCSESSIONRELEASE— Освободить сессию                   ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  Features:                                                    ║");
            sb.AppendLine("║    GGCONCPUBLIC        — Default-фичи (IsDefault)            ║");
            sb.AppendLine("║    GGCONCPRO           — Pro инструменты (Pro+)              ║");
            sb.AppendLine("║    GGCONCBATCH         — Пакетный экспорт + Limits           ║");
            sb.AppendLine("║    GGCONCLIMITS        — Обзор Feature Limits                ║");
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

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return text.Length <= maxLength ? text : text[..(maxLength - 3)] + "...";
        }

        #endregion
    }
}

