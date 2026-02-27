using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace V_Task.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private static LocalizationService? _instance;
    public static LocalizationService Instance => _instance ??= new LocalizationService();

    private string _currentLanguage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public static readonly Dictionary<string, string> AvailableLanguages = new()
    {
        { "uk", "Українська" },
        { "ru", "Русский" },
        { "en", "English" },
        { "de", "Deutsch" },
        { "tr", "Türkçe" }
    };

    private LocalizationService()
    {
        // Load language from database
        _currentLanguage = DatabaseService.Instance.GetLanguage();
        
        // Validate that loaded language exists, otherwise use default
        if (!AvailableLanguages.ContainsKey(_currentLanguage))
        {
            _currentLanguage = "en";
        }
    }

    // Ukrainian (default)
    private readonly Dictionary<string, string> _ukStrings = new()
    {
        // Window
        { "AppTitle", "V-Task - Монітор Ресурсів" },
        
        // Tabs
        { "TabDashboard", "📊 Dashboard" },
        { "TabMemory", "💾 Memory" },
        { "TabGPU", "🎮 GPU" },
        
        // Dashboard - CPU
        { "CPU", "🧠 CPU" },
        { "DetectingProcessor", "Виявлення процесора…" },
        { "CoresPhysLog", "Ядра (фіз/лог):" },
        
        // Dashboard - Memory
        { "Memory", "💾 Memory" },
        { "Calculating", "Розрахунок…" },
        { "Used", "Використано" },
        { "Available", "Доступно" },
        { "Total", "Всього" },
        
        // Dashboard - Disk
        { "Disk", "💿 Disk" },
        
        // Dashboard - Network
        { "Network", "🌐 Network" },
        { "Download", "↓ Завантаження" },
        { "Upload", "↑ Вивантаження" },
        { "Received", "Отримано" },
        { "Sent", "Відправлено" },
        { "WiFiNotConnected", "📶 Wi-Fi: Не підключено" },
        { "EthNotConnected", "🔌 Ethernet: Не підключено" },
        { "NoConnection", "Немає підключення" },
        
        // Dashboard - System
        { "System", "💻 Система" },
        { "Uptime", "Uptime:" },
        { "Processes", "⚡ Процеси" },
        { "ProcessCount", "процесів" },
        { "ThreadCount", "потоків" },
        { "Battery", "🔋 Батарея" },
        { "UpdateTime", "🕐 Час оновлення" },
        { "Second", "секунда" },
        { "Refresh", "Оновити" },
        
        // Memory Panel
        { "MemoryDetails", "💾 Детальна інформація про пам'ять" },
        { "RAM", "Оперативна пам'ять (RAM)" },
        { "SwapFile", "Файл підкачки (Swap / Page File)" },
        { "Usage", "Використання" },
        { "AdditionalInfo", "📊 Додаткова інформація" },
        { "Frequency", "Частота:" },
        { "Type", "Тип:" },
        { "Slots", "Слоти:" },
        { "Caching", "🔄 Кешування" },
        { "Cached", "Кешовано:" },
        { "Allocated", "Виділено:" },
        { "Paged", "Сторінкова:" },
        
        // GPU Panel
        { "GPUDetails", "🎮 Детальна інформація про GPU" },
        { "GPUMemory", "Пам'ять GPU" },
        { "Status", "Статус" },
        { "TechData", "🔧 Технічні дані" },
        { "Driver", "Драйвер:" },
        { "Interface", "Інтерфейс:" },
        { "NA", "Н/Д" },
        
        // Settings
        { "Settings", "⚙️ Налаштування" },
        { "General", "Загальні" },
        { "Language", "Мова" },
        { "SelectLanguage", "Виберіть мову:" },
        { "About", "Про додаток" },
        { "AboutApp", "Про V-Task" },
        { "Version", "Версія:" },
        { "Author", "Автор:" },
        { "AuthorName", "Oleh Kurylo" },
        { "Description", "Опис:" },
        { "AppDescription", "V-Task - це сучасний монітор системних ресурсів для Windows, створений з використанням Avalonia UI." },
        { "Close", "Закрити" },
        { "Apply", "Застосувати" },
        
        // User Agreement
        { "UserAgreement", "🛡️ Угода користувача" },
        { "UserAgreementTitle", "Угода користувача" },
        { "UserAgreementText", @"Ласкаво просимо до V-Task!

Цей додаток призначений виключно для моніторингу системних ресурсів вашого комп'ютера.

📋 Збирання даних:
• V-Task зберігає ТІЛЬКИ назву вибраної мови інтерфейсу
• Жодні персональні дані не збираються
• Жодна інформація не передається третім особам

🔒 Конфіденційність:
• Всі дані обробляються локально на вашому пристрої
• Додаток не має мережевих функцій для передачі даних
• Налаштування зберігаються у локальній базі даних

⚠️ Відмова від відповідальності:
• Додаток надається ""як є"" без будь-яких гарантій
• Автор не несе відповідальності за будь-які збитки
• Використовуйте на власний ризик

Використовуючи цей додаток, ви погоджуєтесь з умовами цієї угоди." },
        { "Accept", "Прийняти" },
        { "Decline", "Відхилити" }
    };

    // Russian
    private readonly Dictionary<string, string> _ruStrings = new()
    {
        // Window
        { "AppTitle", "V-Task - Монитор Ресурсов" },
        
        // Tabs
        { "TabDashboard", "📊 Dashboard" },
        { "TabMemory", "💾 Memory" },
        { "TabGPU", "🎮 GPU" },
        
        // Dashboard - CPU
        { "CPU", "🧠 CPU" },
        { "DetectingProcessor", "Определение процессора…" },
        { "CoresPhysLog", "Ядра (физ/лог):" },
        
        // Dashboard - Memory
        { "Memory", "💾 Memory" },
        { "Calculating", "Расчёт…" },
        { "Used", "Использовано" },
        { "Available", "Доступно" },
        { "Total", "Всего" },
        
        // Dashboard - Disk
        { "Disk", "💿 Disk" },
        
        // Dashboard - Network
        { "Network", "🌐 Network" },
        { "Download", "↓ Загрузка" },
        { "Upload", "↑ Выгрузка" },
        { "Received", "Получено" },
        { "Sent", "Отправлено" },
        { "WiFiNotConnected", "📶 Wi-Fi: Не подключено" },
        { "EthNotConnected", "🔌 Ethernet: Не подключено" },
        { "NoConnection", "Нет подключения" },
        
        // Dashboard - System
        { "System", "💻 Система" },
        { "Uptime", "Uptime:" },
        { "Processes", "⚡ Процессы" },
        { "ProcessCount", "процессов" },
        { "ThreadCount", "потоков" },
        { "Battery", "🔋 Батарея" },
        { "UpdateTime", "🕐 Время обновления" },
        { "Second", "секунда" },
        { "Refresh", "Обновить" },
        
        // Memory Panel
        { "MemoryDetails", "💾 Подробная информация о памяти" },
        { "RAM", "Оперативная память (RAM)" },
        { "SwapFile", "Файл подкачки (Swap / Page File)" },
        { "Usage", "Использование" },
        { "AdditionalInfo", "📊 Дополнительная информация" },
        { "Frequency", "Частота:" },
        { "Type", "Тип:" },
        { "Slots", "Слоты:" },
        { "Caching", "🔄 Кеширование" },
        { "Cached", "Кешировано:" },
        { "Allocated", "Выделено:" },
        { "Paged", "Страничная:" },
        
        // GPU Panel
        { "GPUDetails", "🎮 Подробная информация о GPU" },
        { "GPUMemory", "Память GPU" },
        { "Status", "Статус" },
        { "TechData", "🔧 Технические данные" },
        { "Driver", "Драйвер:" },
        { "Interface", "Интерфейс:" },
        { "NA", "Н/Д" },
        
        // Settings
        { "Settings", "⚙️ Настройки" },
        { "General", "Общие" },
        { "Language", "Язык" },
        { "SelectLanguage", "Выберите язык:" },
        { "About", "О приложении" },
        { "AboutApp", "О V-Task" },
        { "Version", "Версия:" },
        { "Author", "Автор:" },
        { "AuthorName", "Oleh Kurylo" },
        { "Description", "Описание:" },
        { "AppDescription", "V-Task - это современный монитор системных ресурсов для Windows, созданный с использованием Avalonia UI." },
        { "Close", "Закрыть" },
        { "Apply", "Применить" },
        
        // User Agreement
        { "UserAgreement", "🛡️ Пользовательское соглашение" },
        { "UserAgreementTitle", "Пользовательское соглашение" },
        { "UserAgreementText", @"Добро пожаловать в V-Task!

Это приложение предназначено исключительно для мониторинга системных ресурсов вашего компьютера.

📋 Сбор данных:
• V-Task хранит ТОЛЬКО название выбранного языка интерфейса
• Никакие персональные данные не собираются
• Никакая информация не передается третьим лицам

🔒 Конфиденциальность:
• Все данные обрабатываются локально на вашем устройстве
• Приложение не имеет сетевых функций для передачи данных
• Настройки хранятся в локальной базе данных

⚠️ Отказ от ответственности:
• Приложение предоставляется ""как есть"" без каких-либо гарантий
• Автор не несет ответственности за любые убытки
• Используйте на свой страх и риск

Используя это приложение, вы соглашаетесь с условиями данного соглашения." },
        { "Accept", "Принять" },
        { "Decline", "Отклонить" }
    };

    // English
    private readonly Dictionary<string, string> _enStrings = new()
    {
        // Window
        { "AppTitle", "V-Task - Resource Monitor" },
        
        // Tabs
        { "TabDashboard", "📊 Dashboard" },
        { "TabMemory", "💾 Memory" },
        { "TabGPU", "🎮 GPU" },
        
        // Dashboard - CPU
        { "CPU", "🧠 CPU" },
        { "DetectingProcessor", "Detecting processor…" },
        { "CoresPhysLog", "Cores (phys/log):" },
        
        // Dashboard - Memory
        { "Memory", "💾 Memory" },
        { "Calculating", "Calculating…" },
        { "Used", "Used" },
        { "Available", "Available" },
        { "Total", "Total" },
        
        // Dashboard - Disk
        { "Disk", "💿 Disk" },
        
        // Dashboard - Network
        { "Network", "🌐 Network" },
        { "Download", "↓ Download" },
        { "Upload", "↑ Upload" },
        { "Received", "Received" },
        { "Sent", "Sent" },
        { "WiFiNotConnected", "📶 Wi-Fi: Not connected" },
        { "EthNotConnected", "🔌 Ethernet: Not connected" },
        { "NoConnection", "No connection" },
        
        // Dashboard - System
        { "System", "💻 System" },
        { "Uptime", "Uptime:" },
        { "Processes", "⚡ Processes" },
        { "ProcessCount", "processes" },
        { "ThreadCount", "threads" },
        { "Battery", "🔋 Battery" },
        { "UpdateTime", "🕐 Update time" },
        { "Second", "second" },
        { "Refresh", "Refresh" },
        
        // Memory Panel
        { "MemoryDetails", "💾 Memory details" },
        { "RAM", "RAM (Random Access Memory)" },
        { "SwapFile", "Swap / Page File" },
        { "Usage", "Usage" },
        { "AdditionalInfo", "📊 Additional information" },
        { "Frequency", "Frequency:" },
        { "Type", "Type:" },
        { "Slots", "Slots:" },
        { "Caching", "🔄 Caching" },
        { "Cached", "Cached:" },
        { "Allocated", "Allocated:" },
        { "Paged", "Paged:" },
        
        // GPU Panel
        { "GPUDetails", "🎮 GPU details" },
        { "GPUMemory", "GPU Memory" },
        { "Status", "Status" },
        { "TechData", "🔧 Technical data" },
        { "Driver", "Driver:" },
        { "Interface", "Interface:" },
        { "NA", "N/A" },
        
        // Settings
        { "Settings", "⚙️ Settings" },
        { "General", "General" },
        { "Language", "Language" },
        { "SelectLanguage", "Select language:" },
        { "About", "About" },
        { "AboutApp", "About V-Task" },
        { "Version", "Version:" },
        { "Author", "Author:" },
        { "AuthorName", "Oleh Kurylo" },
        { "Description", "Description:" },
        { "AppDescription", "V-Task is a modern system resource monitor for Windows, built with Avalonia UI." },
        { "Close", "Close" },
        { "Apply", "Apply" },
        
        // User Agreement
        { "UserAgreement", "🛡️ User Agreement" },
        { "UserAgreementTitle", "User Agreement" },
        { "UserAgreementText", @"Welcome to V-Task!

This application is designed exclusively for monitoring your computer's system resources.

📋 Data Collection:
• V-Task stores ONLY the name of the selected interface language
• No personal data is collected
• No information is shared with third parties

🔒 Privacy:
• All data is processed locally on your device
• The application has no network functions for data transmission
• Settings are stored in a local database

⚠️ Disclaimer:
• The application is provided ""as is"" without any warranties
• The author is not responsible for any damages
• Use at your own risk

By using this application, you agree to the terms of this agreement." },
        { "Accept", "Accept" },
        { "Decline", "Decline" }
    };

    // German
    private readonly Dictionary<string, string> _deStrings = new()
    {
        // Window
        { "AppTitle", "V-Task - Ressourcenmonitor" },
        
        // Tabs
        { "TabDashboard", "📊 Dashboard" },
        { "TabMemory", "💾 Speicher" },
        { "TabGPU", "🎮 GPU" },
        
        // Dashboard - CPU
        { "CPU", "🧠 CPU" },
        { "DetectingProcessor", "Prozessor wird erkannt…" },
        { "CoresPhysLog", "Kerne (phys/log):" },
        
        // Dashboard - Memory
        { "Memory", "💾 Speicher" },
        { "Calculating", "Berechnung…" },
        { "Used", "Verwendet" },
        { "Available", "Verfügbar" },
        { "Total", "Gesamt" },
        
        // Dashboard - Disk
        { "Disk", "💿 Festplatte" },
        
        // Dashboard - Network
        { "Network", "🌐 Netzwerk" },
        { "Download", "↓ Download" },
        { "Upload", "↑ Upload" },
        { "Received", "Empfangen" },
        { "Sent", "Gesendet" },
        { "WiFiNotConnected", "📶 Wi-Fi: Nicht verbunden" },
        { "EthNotConnected", "🔌 Ethernet: Nicht verbunden" },
        { "NoConnection", "Keine Verbindung" },
        
        // Dashboard - System
        { "System", "💻 System" },
        { "Uptime", "Betriebszeit:" },
        { "Processes", "⚡ Prozesse" },
        { "ProcessCount", "Prozesse" },
        { "ThreadCount", "Threads" },
        { "Battery", "🔋 Akku" },
        { "UpdateTime", "🕐 Aktualisierungszeit" },
        { "Second", "Sekunde" },
        { "Refresh", "Aktualisieren" },
        
        // Memory Panel
        { "MemoryDetails", "💾 Speicherdetails" },
        { "RAM", "Arbeitsspeicher (RAM)" },
        { "SwapFile", "Auslagerungsdatei" },
        { "Usage", "Nutzung" },
        { "AdditionalInfo", "📊 Zusätzliche Informationen" },
        { "Frequency", "Frequenz:" },
        { "Type", "Typ:" },
        { "Slots", "Steckplätze:" },
        { "Caching", "🔄 Zwischenspeicherung" },
        { "Cached", "Zwischengespeichert:" },
        { "Allocated", "Zugewiesen:" },
        { "Paged", "Ausgelagert:" },
        
        // GPU Panel
        { "GPUDetails", "🎮 GPU-Details" },
        { "GPUMemory", "GPU-Speicher" },
        { "Status", "Status" },
        { "TechData", "🔧 Technische Daten" },
        { "Driver", "Treiber:" },
        { "Interface", "Schnittstelle:" },
        { "NA", "K.A." },
        
        // Settings
        { "Settings", "⚙️ Einstellungen" },
        { "General", "Allgemein" },
        { "Language", "Sprache" },
        { "SelectLanguage", "Sprache auswählen:" },
        { "About", "Über" },
        { "AboutApp", "Über V-Task" },
        { "Version", "Version:" },
        { "Author", "Autor:" },
        { "AuthorName", "Oleh Kurylo" },
        { "Description", "Beschreibung:" },
        { "AppDescription", "V-Task ist ein moderner Systemressourcenmonitor für Windows, erstellt mit Avalonia UI." },
        { "Close", "Schließen" },
        { "Apply", "Anwenden" },
        
        // User Agreement
        { "UserAgreement", "🛡️ Benutzervereinbarung" },
        { "UserAgreementTitle", "Benutzervereinbarung" },
        { "UserAgreementText", @"Willkommen bei V-Task!

Diese Anwendung dient ausschließlich zur Überwachung der Systemressourcen Ihres Computers.

📋 Datenerfassung:
• V-Task speichert NUR den Namen der ausgewählten Oberflächensprache
• Es werden keine persönlichen Daten erfasst
• Es werden keine Informationen an Dritte weitergegeben

🔒 Datenschutz:
• Alle Daten werden lokal auf Ihrem Gerät verarbeitet
• Die Anwendung hat keine Netzwerkfunktionen zur Datenübertragung
• Einstellungen werden in einer lokalen Datenbank gespeichert

⚠️ Haftungsausschluss:
• Die Anwendung wird ""wie besehen"" ohne jegliche Garantien bereitgestellt
• Der Autor ist nicht für etwaige Schäden verantwortlich
• Nutzung auf eigene Gefahr

Durch die Nutzung dieser Anwendung stimmen Sie den Bedingungen dieser Vereinbarung zu." },
        { "Accept", "Akzeptieren" },
        { "Decline", "Ablehnen" }
    };

    // Turkish
    private readonly Dictionary<string, string> _trStrings = new()
    {
        // Window
        { "AppTitle", "V-Task - Kaynak İzleyici" },
        
        // Tabs
        { "TabDashboard", "📊 Panel" },
        { "TabMemory", "💾 Bellek" },
        { "TabGPU", "🎮 GPU" },
        
        // Dashboard - CPU
        { "CPU", "🧠 CPU" },
        { "DetectingProcessor", "İşlemci algılanıyor…" },
        { "CoresPhysLog", "Çekirdekler (fiz/man):" },
        
        // Dashboard - Memory
        { "Memory", "💾 Bellek" },
        { "Calculating", "Hesaplanıyor…" },
        { "Used", "Kullanılan" },
        { "Available", "Kullanılabilir" },
        { "Total", "Toplam" },
        
        // Dashboard - Disk
        { "Disk", "💿 Disk" },
        
        // Dashboard - Network
        { "Network", "🌐 Ağ" },
        { "Download", "↓ İndirme" },
        { "Upload", "↑ Yükleme" },
        { "Received", "Alınan" },
        { "Sent", "Gönderilen" },
        { "WiFiNotConnected", "📶 Wi-Fi: Bağlı değil" },
        { "EthNotConnected", "🔌 Ethernet: Bağlı değil" },
        { "NoConnection", "Bağlantı yok" },
        
        // Dashboard - System
        { "System", "💻 Sistem" },
        { "Uptime", "Çalışma süresi:" },
        { "Processes", "⚡ İşlemler" },
        { "ProcessCount", "işlem" },
        { "ThreadCount", "iş parçacığı" },
        { "Battery", "🔋 Pil" },
        { "UpdateTime", "🕐 Güncelleme süresi" },
        { "Second", "saniye" },
        { "Refresh", "Yenile" },
        
        // Memory Panel
        { "MemoryDetails", "💾 Bellek ayrıntıları" },
        { "RAM", "RAM (Rastgele Erişimli Bellek)" },
        { "SwapFile", "Sayfa Dosyası" },
        { "Usage", "Kullanım" },
        { "AdditionalInfo", "📊 Ek bilgi" },
        { "Frequency", "Frekans:" },
        { "Type", "Tür:" },
        { "Slots", "Yuvalar:" },
        { "Caching", "🔄 Önbellekleme" },
        { "Cached", "Önbelleklenen:" },
        { "Allocated", "Ayrılan:" },
        { "Paged", "Sayfalanan:" },
        
        // GPU Panel
        { "GPUDetails", "🎮 GPU ayrıntıları" },
        { "GPUMemory", "GPU Belleği" },
        { "Status", "Durum" },
        { "TechData", "🔧 Teknik veriler" },
        { "Driver", "Sürücü:" },
        { "Interface", "Arayüz:" },
        { "NA", "Yok" },
        
        // Settings
        { "Settings", "⚙️ Ayarlar" },
        { "General", "Genel" },
        { "Language", "Dil" },
        { "SelectLanguage", "Dil seçin:" },
        { "About", "Hakkında" },
        { "AboutApp", "V-Task Hakkında" },
        { "Version", "Sürüm:" },
        { "Author", "Yazar:" },
        { "AuthorName", "Oleh Kurylo" },
        { "Description", "Açıklama:" },
        { "AppDescription", "V-Task, Avalonia UI ile oluşturulmuş Windows için modern bir sistem kaynak izleyicisidir." },
        { "Close", "Kapat" },
        { "Apply", "Uygula" },
        
        // User Agreement
        { "UserAgreement", "🛡️ Kullanıcı Sözleşmesi" },
        { "UserAgreementTitle", "Kullanıcı Sözleşmesi" },
        { "UserAgreementText", @"V-Task'a hoş geldiniz!

Bu uygulama yalnızca bilgisayarınızın sistem kaynaklarını izlemek için tasarlanmıştır.

📋 Veri Toplama:
• V-Task YALNIZCA seçilen arayüz dilinin adını saklar
• Hiçbir kişisel veri toplanmaz
• Hiçbir bilgi üçüncü taraflarla paylaşılmaz

🔒 Gizlilik:
• Tüm veriler cihazınızda yerel olarak işlenir
• Uygulamanın veri iletimi için ağ işlevleri yoktur
• Ayarlar yerel bir veritabanında saklanır

⚠️ Sorumluluk Reddi:
• Uygulama herhangi bir garanti olmaksızın ""olduğu gibi"" sağlanır
• Yazar herhangi bir zarardan sorumlu değildir
• Kendi sorumluluğunuzda kullanın

Bu uygulamayı kullanarak, bu sözleşmenin şartlarını kabul etmiş olursunuz." },
        { "Accept", "Kabul Et" },
        { "Decline", "Reddet" }
    };

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value && AvailableLanguages.ContainsKey(value))
            {
                _currentLanguage = value;
                
                // Save to database
                DatabaseService.Instance.SaveLanguage(value);
                
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler? LanguageChanged;

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        var dict = _currentLanguage switch
        {
            "ru" => _ruStrings,
            "en" => _enStrings,
            "de" => _deStrings,
            "tr" => _trStrings,
            _ => _ukStrings
        };

        return dict.TryGetValue(key, out var value) ? value : key;
    }
}
