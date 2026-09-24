// =====================================================================
// TestProduct.Installer — Тип дистрибуции Installer (MSI/EXE)
// Демонстрация: DistributionType.Installer, TrialBindingMode.AccountAndMachine
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.Contracts.Licensing;
using GrossGeo.SDK;

[assembly: CommandClass(typeof(TestProduct.Installer.TestInstallerPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Installer.TestInstallerPlugin))]

namespace TestProduct.Installer
{
    /// <summary>
    /// Тестовый плагин с типом дистрибуции Installer.
    /// Демонстрирует: DistributionType.Installer, TrialBindingMode.AccountAndMachine.
    /// </summary>
    public class TestInstallerPlugin : IExtensionApplication
    {
        // API Key для TestProduct.Installer
        private const string ProductKey = "GG-ECD5-9B1D-0333-C517";
        private const string PluginVersion = "1.0.0";

        private static ProductLicenseAccessor License => GrossGeoLicense.ForProduct(ProductKey);
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
            WriteMessage("║  📦 TEST PRODUCT INSTALLER v1.0.0                             ║");
            WriteMessage("║  Тип дистрибуции: Installer (MSI/EXE)                         ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  DistributionType=Installer                                    ║");
            WriteMessage("║  Pro: 1490₽/мес, Trial 14 дней                                ║");
            WriteMessage("║  TrialBindingMode: AccountAndMachine                          ║");
            WriteMessage("╚══════════════════════════════════════════════════════════════╝");

            _ = InitializeSdkAsync();
        }

        public void Terminate()
        {
            GrossGeoLicense.Shutdown(ProductKey);
            WriteMessage("\n[TestProduct.Installer] Плагин выгружен");
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

                if (result.IsValid)
                {
                    WriteMessage($"\n[SDK] ✅ Лицензия активна!");
                    WriteMessage($"[SDK] Команды: GGINSTTEST, GGINSTINFO");
                }
                else
                {
                    WriteMessage($"\n[SDK] ⚠️ Лицензия не найдена");
                    WriteMessage($"[SDK] Trial: 14 дней (TrialBindingMode=AccountAndMachine)");
                    WriteMessage($"[SDK] Доступна только команда: GGINSTINFO");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ❌ Ошибка: {ex.Message}");
            }
        }

        #endregion

        #region AutoCAD Commands

        /// <summary>
        /// Базовая команда — требует лицензию.
        /// </summary>
        [CommandMethod("GGINSTTEST")]
        public void InstallerTest()
        {
            if (!License.IsValid)
            {
                // LGC-671: решать по одному признаку IsValid значит объявлять «прав нет» там, где
                // просто не поднялась панель. Причина лежит в LastResult — спрашиваем её.
                var reason = License.LastResult;
                WriteMessage(reason?.Status == LicenseCheckStatus.NetworkError
                    ? $"\n[GGINSTTEST] ⏳ Проверить лицензию не удалось: {reason.Message}"
                    : $"\n[GGINSTTEST] ⚠️ Требуется лицензия: {reason?.Message ?? "проверка не выполнялась"}");
                return;
            }

            WriteMessage("\n[GGINSTTEST] Installer plugin executed");
            WriteMessage("[GGINSTTEST] DistributionType=Installer, TrialBindingMode=AccountAndMachine");
        }

        /// <summary>
        /// Информация о SDK — работает всегда.
        /// </summary>
        [CommandMethod("GGINSTINFO")]
        public void InstallerInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\n═══ TestProduct.Installer — SDK Info ═══");
            sb.AppendLine($"  IsValid: {License.IsValid}");
            sb.AppendLine($"  PlanTier: {License.PlanTier}");
            sb.AppendLine($"  BillingModel: {License.BillingModel}");
            sb.AppendLine($"  LicenseMode: {License.LicenseMode}");
            sb.AppendLine($"  DistributionType: Installer");
            sb.AppendLine("═══════════════════════════════════════");

            WriteMessage(sb.ToString());
        }

        #endregion

        #region Helpers

        private static void WriteMessage(string message)
        {
            // LGC-1350: сюда приходят и из продолжений после await — AutoCAD API только с главного потока.
            RunOnMainThread(() => Ed?.WriteMessage(message + "\n"));
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
                System.Diagnostics.Debug.WriteLine("[TestProduct.Installer] главный поток не запомнен в Initialize — вывод пропущен");
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
                System.Diagnostics.Debug.WriteLine($"[TestProduct.Installer] {ex}");
            }
        }

        private static string MaskKey(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Length < 8) return "****";
            return key[..4] + "..." + key[^4..];
        }

        #endregion
    }
}

