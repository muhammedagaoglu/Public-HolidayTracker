using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PublicHolidayTracker
{
    public class Holiday
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }
        
        [JsonPropertyName("localName")]
        public string LocalName { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("countryCode")]
        public string CountryCode { get; set; }
        
        [JsonPropertyName("fixed")]
        public bool Fixed { get; set; }
        
        [JsonPropertyName("global")]
        public bool Global { get; set; }

        // Yardımcı özellik: DateTime olarak tarih
        public DateTime DateTimeValue
        {
            get
            {
                // API tarih formatı: yyyy-MM-dd
                if (DateTime.TryParse(Date, out var dt))
                    return dt;
                return DateTime.MinValue;
            }
        }
    }

    class Program
    {
        // Yıllara göre tatilleri tutacağımız sözlük
        private static readonly Dictionary<int, List<Holiday>> _holidayCache = new Dictionary<int, List<Holiday>>();
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly int[] _years = new[] { 2023, 2024, 2025 };

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("Resmi tatiller API'den çekiliyor, lütfen bekleyiniz...\n");

            // Uygulama başlarken 3 yılın da verisini çek
            foreach (var year in _years)
            {
                await LoadHolidaysForYearAsync(year);
            }

            await ShowMenuAsync();
        }

        private static async Task LoadHolidaysForYearAsync(int year)
        {
            if (_holidayCache.ContainsKey(year))
                return;

            try
            {
                string url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/TR";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"{year} yılı için tatiller alınırken hata oluştu. StatusCode: {response.StatusCode}");
                    _holidayCache[year] = new List<Holiday>();
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var holidays = JsonSerializer.Deserialize<List<Holiday>>(json, options) ?? new List<Holiday>();
                _holidayCache[year] = holidays;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{year} yılı için veriler alınırken hata oluştu: {ex.Message}");
                _holidayCache[year] = new List<Holiday>();
            }
        }

        private static async Task ShowMenuAsync()
        {
            while (true)
            {
                Console.WriteLine("===== PublicHolidayTracker =====");
                Console.WriteLine("1. Tatil listesini göster (yıl seçmeli)");
                Console.WriteLine("2. Tarihe göre tatil ara (gg-aa formatı)");
                Console.WriteLine("3. İsme göre tatil ara");
                Console.WriteLine("4. Tüm tatilleri 3 yıl boyunca göster (2023–2025)");
                Console.WriteLine("5. Çıkış");
                Console.Write("Seçiminiz: ");

                string input = Console.ReadLine();
                Console.WriteLine();

                switch (input)
                {
                    case "1":
                        ShowHolidaysByYear();
                        break;
                    case "2":
                        SearchByDate();
                        break;
                    case "3":
                        SearchByName();
                        break;
                    case "4":
                        ShowAllYears();
                        break;
                    case "5":
                        Console.WriteLine("Programdan çıkılıyor...");
                        return;
                    default:
                        Console.WriteLine("Geçersiz seçim, lütfen 1-5 arasında bir değer girin.\n");
                        break;
                }

                Console.WriteLine("Devam etmek için bir tuşa basın...");
                Console.ReadKey();
                Console.Clear();
            }
        }

        private static void ShowHolidaysByYear()
        {
            Console.Write("Yıl giriniz (2023, 2024, 2025): ");
            var yearText = Console.ReadLine();

            if (!int.TryParse(yearText, out int year) || Array.IndexOf(_years, year) < 0)
            {
                Console.WriteLine("Geçersiz yıl girdiniz.\n");
                return;
            }

            if (!_holidayCache.TryGetValue(year, out var holidays) || holidays.Count == 0)
            {
                Console.WriteLine($"{year} yılı için tatil verisi bulunamadı.\n");
                return;
            }

            Console.WriteLine($"\n=== {year} Resmi Tatiller ===");
            foreach (var h in holidays)
            {
                Console.WriteLine($"{h.DateTimeValue:dd.MM.yyyy} - {h.LocalName} ({h.Name})");
            }
            Console.WriteLine();
        }

        private static void SearchByDate()
        {
            Console.Write("Tarih giriniz (gg-aa formatında, örn: 01-01): ");
            var dateInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(dateInput) || dateInput.Length != 5 || dateInput[2] != '-')
            {
                Console.WriteLine("Format hatalı. Örnek giriş: 01-01\n");
                return;
            }

            var parts = dateInput.Split('-');
            if (parts.Length != 2 ||
                !int.TryParse(parts[0], out int day) ||
                !int.TryParse(parts[1], out int month))
            {
                Console.WriteLine("Format hatalı. Örnek giriş: 01-01\n");
                return;
            }

            bool foundAny = false;
            Console.WriteLine("\n=== Tarihe Göre Tatil Arama Sonuçları ===");
            foreach (var year in _years)
            {
                if (!_holidayCache.TryGetValue(year, out var holidays)) continue;

                foreach (var h in holidays)
                {
                    var dt = h.DateTimeValue;
                    if (dt.Day == day && dt.Month == month)
                    {
                        foundAny = true;
                        Console.WriteLine($"{dt:dd.MM.yyyy} - {h.LocalName} ({h.Name})");
                    }
                }
            }

            if (!foundAny)
            {
                Console.WriteLine("Bu tarih için 2023-2025 yılları arasında resmi tatil bulunamadı.");
            }

            Console.WriteLine();
        }

        private static void SearchByName()
        {
            Console.Write("Tatil adı giriniz (Türkçe veya İngilizce, kısmi yazım da olur): ");
            var nameInput = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(nameInput))
            {
                Console.WriteLine("Bir isim girmeniz gerekiyor.\n");
                return;
            }

            nameInput = nameInput.ToLower();
            bool foundAny = false;

            Console.WriteLine("\n=== İsme Göre Tatil Arama Sonuçları ===");
            foreach (var year in _years)
            {
                if (!_holidayCache.TryGetValue(year, out var holidays)) continue;

                foreach (var h in holidays)
                {
                    string localName = h.LocalName?.ToLower() ?? string.Empty;
                    string name = h.Name?.ToLower() ?? string.Empty;

                    if (localName.Contains(nameInput) || name.Contains(nameInput))
                    {
                        foundAny = true;
                        Console.WriteLine($"{h.DateTimeValue:dd.MM.yyyy} - {h.LocalName} ({h.Name}) - Yıl: {year}");
                    }
                }
            }

            if (!foundAny)
            {
                Console.WriteLine("Bu isimle eşleşen tatil bulunamadı.");
            }

            Console.WriteLine();
        }

        private static void ShowAllYears()
        {
            Console.WriteLine("=== 2023-2025 Arası Tüm Resmi Tatiller ===");

            foreach (var year in _years)
            {
                Console.WriteLine($"\n--- {year} ---");
                if (!_holidayCache.TryGetValue(year, out var holidays) || holidays.Count == 0)
                {
                    Console.WriteLine("Veri bulunamadı.");
                    continue;
                }

                foreach (var h in holidays)
                {
                    Console.WriteLine($"{h.DateTimeValue:dd.MM.yyyy} - {h.LocalName} ({h.Name})");
                }
            }

            Console.WriteLine();
        }
    }
}
