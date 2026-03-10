// =====================================================================
// TestProduct.Analytics — Режим ExternalOnly (v2)
// Только листинг в каталоге + аналитика, лицензии — внешние
// =====================================================================

using System;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using GrossGeo.SDK;

[assembly: CommandClass(typeof(TestProduct.Analytics.TestAnalyticsPlugin))]
[assembly: ExtensionApplication(typeof(TestProduct.Analytics.TestAnalyticsPlugin))]

namespace TestProduct.Analytics
{
    /// <summary>
    /// Тестовый плагин в режиме ExternalOnly (v2).
    /// Лицензирование — на стороне разработчика, GrossGeo — только каталог и аналитика.
    /// </summary>
    public class TestAnalyticsPlugin : IExtensionApplication
    {
        // API Key из DbSeeder.AnalyticsPluginId
        private const string ProductKey = "GG-7C1F-02E8-E8DA-4BA1";
        private const string PluginVersion = "1.0.0";

        private static int _commandCounter;
        private static Editor? Ed => Application.DocumentManager?.MdiActiveDocument?.Editor;

        #region IExtensionApplication

        public void Initialize()
        {
            WriteMessage("\n╔══════════════════════════════════════════════════════════════╗");
            WriteMessage("║  📊 TEST PRODUCT ANALYTICS v1.0.0                            ║");
            WriteMessage("║  Режим ExternalOnly (v2) — внешнее лицензирование            ║");
            WriteMessage("╠══════════════════════════════════════════════════════════════╣");
            WriteMessage("║  v2: ProductLicensingMode=ExternalOnly                        ║");
            WriteMessage("║  Каталог: GrossGeo. Лицензии: внешние (сайт разработчика)     ║");
            WriteMessage("║  SDK: аналитика использования                                 ║");
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
                WriteMessage($"\n[SDK] Инициализация ExternalOnly продукта...");
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

                // Для ExternalOnly продукта IsValid может быть false — лицензии внешние
                WriteMessage($"\n[SDK] ℹ️ ExternalOnly — лицензии управляются разработчиком");
                WriteMessage($"[SDK] Все команды доступны, SDK отправляет аналитику.");
                WriteMessage($"[SDK] Команды: TEST_ANALYTICS_INFO, TEST_TRACK_FEATURE");
                WriteMessage($"[SDK]          TEST_ANALYTICS_UPDATE, TEST_SESSION_STATS");
            }
            catch (System.Exception ex)
            {
                WriteMessage($"\n[SDK] ⚠️ Ошибка: {ex.Message}");
                WriteMessage($"[SDK] Плагин продолжает работать автономно.");
            }
        }

        #endregion

        #region AutoCAD Commands

        /// <summary>
        /// Информация о SDK и ExternalOnly режиме.
        /// </summary>
        [CommandMethod("TEST_ANALYTICS_INFO")]
        public void ShowAnalyticsInfo()
        {
            _commandCounter++;

            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║        TEST PRODUCT ANALYTICS — ExternalOnly (v2)            ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Product Key:      {MaskKey(ProductKey),-39} ║");
            sb.AppendLine($"║  Plugin Version:   {PluginVersion,-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Initialized:      {GrossGeoLicense.IsInitialized,-39} ║");
            sb.AppendLine($"║  Valid:            {GrossGeoLicense.IsValid,-39} ║");
            sb.AppendLine($"║  PlanTier:         {GrossGeoLicense.PlanTier,-39} ║");
            sb.AppendLine($"║  BillingModel:     {GrossGeoLicense.BillingModel,-39} ║");
            sb.AppendLine($"║  Offline Mode:     {GrossGeoLicense.IsOfflineMode,-39} ║");
            sb.AppendLine($"║  Commands Used:    {_commandCounter,-39} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  ExternalOnly Mode (v2):                                     ║");
            sb.AppendLine("║    • Каталог — через GrossGeo                                ║");
            sb.AppendLine("║    • Покупка — редирект на сайт разработчика                 ║");
            sb.AppendLine("║    • Лицензии — внешние, не через GrossGeo                   ║");
            sb.AppendLine("║    • SDK — только аналитика и обновления                     ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Симуляция работы с отслеживанием (аналитика).
        /// </summary>
        [CommandMethod("TEST_TRACK_FEATURE")]
        public void TrackFeatureCommand()
        {
            _commandCounter++;
            WriteMessage("\n[TEST_TRACK_FEATURE] Выполнение команды...");

            // ExternalOnly — лицензия внешняя, но аналитика работает
            WriteMessage("[TEST_TRACK_FEATURE] ✅ Команда выполнена");
            WriteMessage("[TEST_TRACK_FEATURE] 📊 Аналитика: событие отправлено в GrossGeo");
            WriteMessage("[TEST_TRACK_FEATURE] ℹ️ Лицензия проверяется на стороне разработчика");
        }

        /// <summary>
        /// Проверка обновлений.
        /// </summary>
        [CommandMethod("TEST_ANALYTICS_UPDATE")]
        public async void CheckForUpdates()
        {
            _commandCounter++;
            WriteMessage("\n[TEST_ANALYTICS_UPDATE] Проверка обновлений...");

            try
            {
                var updateInfo = await GrossGeoLicense.CheckForUpdatesAsync();

                WriteMessage($"  Текущая версия:   {PluginVersion}");
                WriteMessage($"  Обновление:       {(updateInfo.HasUpdate ? "✅ Доступно" : "❌ Нет")}");

                if (updateInfo.HasUpdate)
                {
                    WriteMessage($"  Новая версия:     {updateInfo.AvailableVersion}");
                    WriteMessage($"  Дата релиза:      {updateInfo.PublishedAt?.ToString("dd.MM.yyyy")}");

                    if (!string.IsNullOrEmpty(updateInfo.Changelog))
                    {
                        WriteMessage($"  Изменения:        {updateInfo.Changelog}");
                    }

                    WriteMessage("\n  ℹ️ ExternalOnly: обновления скачиваются с сайта разработчика");
                }
            }
            catch (System.Exception ex)
            {
                WriteMessage($"  ❌ Ошибка: {ex.Message}");
            }
        }

        /// <summary>
        /// Статистика сессии.
        /// </summary>
        [CommandMethod("TEST_SESSION_STATS")]
        public void ShowSessionStats()
        {
            _commandCounter++;

            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║           TEST PRODUCT ANALYTICS — Session Stats             ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine($"║  Команд выполнено:     {_commandCounter,-35} ║");
            sb.AppendLine($"║  SDK Initialized:      {GrossGeoLicense.IsInitialized,-35} ║");
            sb.AppendLine($"║  PlanTier:             {GrossGeoLicense.PlanTier,-35} ║");
            sb.AppendLine($"║  BillingModel:         {GrossGeoLicense.BillingModel,-35} ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  SDK общается с User Panel через Named Pipe IPC             ║");
            sb.AppendLine("║  Если User Panel не запущен — используется локальный кэш    ║");
            sb.AppendLine("╚══════════════════════════════════════════════════════════════╝");

            WriteMessage(sb.ToString());
        }

        /// <summary>
        /// Справка по командам.
        /// </summary>
        [CommandMethod("TEST_ANALYTICS_HELP")]
        public void ShowHelp()
        {
            _commandCounter++;

            var sb = new StringBuilder();
            sb.AppendLine("\n╔══════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║       TEST PRODUCT ANALYTICS — Команды                       ║");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════╣");
            sb.AppendLine("║  TEST_ANALYTICS_INFO     — Информация (ExternalOnly v2)      ║");
            sb.AppendLine("║  TEST_TRACK_FEATURE      — Симуляция команды (аналитика)     ║");
            sb.AppendLine("║  TEST_ANALYTICS_UPDATE   — Проверка обновлений               ║");
            sb.AppendLine("║  TEST_SESSION_STATS      — Статистика сессии                 ║");
            sb.AppendLine("║  TEST_ANALYTICS_HELP     — Эта справка                       ║");
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
            if (string.IsNullOrEmpty(key) || key.Length < 10)
                return "****";
            return $"{key[..7]}...{key[^4..]}";
        }

        #endregion
    }
}

