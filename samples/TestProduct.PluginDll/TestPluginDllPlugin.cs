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
using GrossGeo.Contracts.Licensing;
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
        // API Key для TestProduct.PluginDll
        private const string ProductKey = "GG-9739-D69F-BDF2-664C";
        private const string PluginVersion = "1.0.0";

        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        // LGC-1350: диспетчер главного потока AutoCAD, запомненный в Initialize.
        private static System.Windows.Threading.Dispatcher? _ui;

        #region IExtensionApplication

        public void Initialize()
        {
            // LGC-1350: главный поток запоминается ЗДЕСЬ — синхронно, до первого await и не в Task.Run.
            // Вывод из продолжений после await идёт через него (WriteMessage → RunOnMainThread).
            _ui = System.Windows.Threading.Dispatcher.CurrentDispatcher;

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
            GrossGeoLicense.Shutdown();
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

                WriteMessage($"\n[SDK] Результат:");
                WriteMessage($"      - Статус: {result.Status}");
                WriteMessage($"      - IsValid: {result.IsValid}");
                WriteMessage($"      - PlanTier: {result.PlanTier}");
                WriteMessage($"      - BillingModel: {result.BillingModel}");
                WriteMessage($"      - LicenseMode: {result.LicenseMode}");

                // LGC-689: у отказа обязана быть НАЗВАННАЯ причина. Запись завелась с
                // прогона, где образец показал «IsValid: False, PlanTier: Free» — и ни слова
                // о том, почему. Причина была в журнале SDK (PRODUCT_NOT_FOUND), а в выводе
                // самого образца её не было: человек у экрана видел отказ без повода.
                //
                // Значения тарифа сегодня уже не лгут — три перечисления получили
                // Unknown = 255, умолчания свойств починены LGC-1020, разбор с провода идёт
                // через заслон IsDefined (LGC-938). Осталось второе: НАЗВАТЬ причину там,
                // где показан вердикт. Образцовая форма — TestProduct.Licensed.
                if (!result.IsValid)
                {
                    WriteMessage($"      - ErrorCode: {result.ErrorCode}");
                    WriteMessage($"      - Message: {result.Message}");
                }

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
                    WriteMessage($"PlanTier: {GrossGeoLicense.PlanTier}");
                    WriteMessage($"BillingModel: {GrossGeoLicense.BillingModel}");
                    WriteMessage($"DistributionType: PluginDll");
                    WriteMessage("Команда выполнена успешно!");
                },
                // LGC-671: причина доезжает до участника. «Требуется лицензия» при неподнятой
                // панели — неправда, и она посылает человека покупать то, что у него есть.
                onBlocked: reason =>
                {
                    if (reason?.Status == LicenseCheckStatus.NetworkError)
                    {
                        WriteMessage($"\n⏳ Проверить лицензию не удалось: {reason.Message}");
                        WriteMessage("Право не отозвано — повторите после запуска User Panel.");
                        return;
                    }

                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  ❌ Требуется лицензия                 ║");
                    WriteMessage("╚════════════════════════════════════════╝");
                    WriteMessage(reason?.Message ?? "Запустите Trial или приобретите Perpetual лицензию.");
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
            sb.AppendLine($"IsValid:        {GrossGeoLicense.IsValid}");

            sb.AppendLine("\n═══ License Model ═══");
            sb.AppendLine($"PlanTier:       {GrossGeoLicense.PlanTier}");
            sb.AppendLine($"BillingModel:   {GrossGeoLicense.BillingModel}");
            sb.AppendLine($"LicenseMode:    {GrossGeoLicense.LicenseMode}");
            sb.AppendLine($"DistributionType: PluginDll");

            sb.AppendLine("\n═══ Status ═══");
            sb.AppendLine($"ExpiresAt:      {GrossGeoLicense.ExpiresAt?.ToString("dd.MM.yyyy") ?? "N/A (бессрочная)"}");
            sb.AppendLine($"IsOfflineMode:  {GrossGeoLicense.IsOfflineMode}");
            sb.AppendLine($"IsGracePeriod:  {GrossGeoLicense.IsInGracePeriod}");

            sb.AppendLine("\n═══ Features ═══");
            sb.AppendLine($"core:      {GrossGeoLicense.HasFeature("core")}");
            sb.AppendLine($"reporting: {GrossGeoLicense.HasFeature("reporting")}");

            var reportingLimit = GrossGeoLicense.GetFeatureLimit("reporting", "maxPerCall");
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
            FeatureGuard.Require("reporting",
                action: () =>
                {
                    WriteMessage("\n╔════════════════════════════════════════╗");
                    WriteMessage("║  📊 GGDLLREPORT — Отчёт                ║");
                    WriteMessage("║  Feature: reporting + Limits           ║");
                    WriteMessage("╚════════════════════════════════════════╝");

                    // Симуляция: пользователь запросил отчёт по 200 объектам
                    var objectCount = 200;
                    var limit = GrossGeoLicense.GetFeatureLimit("reporting", "maxPerCall");

                    WriteMessage($"Объектов в отчёте: {objectCount}");
                    WriteMessage($"Лимит maxPerCall:  {(limit.HasValue ? limit.Value.ToString() : "∞ (безлимит)")}");

                    if (GrossGeoLicense.CheckLimit("reporting", "maxPerCall", objectCount))
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
            // LGC-1350: сюда приходят и из продолжений после await — AutoCAD API только с главного потока.
            RunOnMainThread(() => Ed?.WriteMessage($"\n{message}"));
        }

        /// <summary>
        /// LGC-1350: выполнить действие на главном потоке AutoCAD через диспетчер, запомненный в
        /// <see cref="Initialize"/>. Сюда приходят и из продолжений после <c>await</c> (поток пула), и из
        /// событий SDK (<c>SessionExpired</c> — поток таймера), а API AutoCAD — только с главного потока.
        /// <c>Dispatcher.BeginInvoke</c> безопасен с любого потока; на главном потоке действие идёт сразу.
        /// </summary>
        private static void RunOnMainThread(Action action)
        {
            var ui = _ui;
            if (ui == null)
            {
                System.Diagnostics.Debug.WriteLine("[TestProduct.PluginDll] главный поток не запомнен в Initialize — вывод пропущен");
                return;
            }

            if (ui.CheckAccess())
            {
                RunSafely(action);
                return;
            }

            ui.BeginInvoke(new Action(() => RunSafely(action)));
        }

        private static void RunSafely(Action action)
        {
            try
            {
                action();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TestProduct.PluginDll] {ex}");
            }
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
