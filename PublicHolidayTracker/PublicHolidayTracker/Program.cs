using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PublicHolidayTracker
{
    class Program
    {
        // Tüm yılların tatil verilerini tutacak global liste
        static List<Holiday> allHolidays = new List<Holiday>();

        static async Task Main(string[] args)
        {
            // Uygulama başlarken verileri çekiyoruz
            Console.WriteLine("Veriler sunucudan indiriliyor, lütfen bekleyiniz...");
            await LoadHolidays();
            Console.WriteLine("Veriler başarıyla yüklendi.\n");

            bool exit = false;
            while (!exit)
            {
                // Menü Seçenekleri
                Console.WriteLine("===== PublicHolidayTracker =====");
                Console.WriteLine("1. Tatil listesini göster (yıl seçmeli)");
                Console.WriteLine("2. Tarihe göre tatil ara (gg-aa formatı)");
                Console.WriteLine("3. İsme göre tatil ara");
                Console.WriteLine("4. Tüm tatilleri 3 yıl boyunca göster (2023–2025)");
                Console.WriteLine("5. Çıkış");
                Console.Write("Seçiminiz: ");

                string selection = Console.ReadLine();

                switch (selection)
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
                        ShowAllHolidays();
                        break;
                    case "5":
                        exit = true;
                        Console.WriteLine("Programdan çıkılıyor...");
                        break;
                    default:
                        Console.WriteLine("Geçersiz seçim. Lütfen tekrar deneyin.");
                        break;
                }
                Console.WriteLine(); // Boşluk bırak
            }
        }

        // API'den verileri çeken metod
        static async Task LoadHolidays()
        {
            // C# HttpClient kullanarak verileri alıyoruz
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string[] years = { "2023", "2024", "2025" };
                    foreach (string year in years)
                    {
                        string url = $"https://date.nager.at/api/v3/PublicHolidays/{year}/TR";
                        
                        // JSON verisini string olarak indir
                        string jsonResponse = await client.GetStringAsync(url);
                        
                        // JSON verisini Holiday listesine dönüştür
                        List<Holiday>? holidays = JsonSerializer.Deserialize<List<Holiday>>(jsonResponse);

                        if (holidays != null)
                        {
                            allHolidays.AddRange(holidays);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Bir hata oluştu: {ex.Message}");
                }
            }
        }

        // 1. Tatil listesini göster (yıl seçmeli)
        static void ShowHolidaysByYear()
        {
            Console.Write("Görmek istediğiniz yılı girin (2023, 2024, 2025): ");
            string year = Console.ReadLine();

            bool found = false;
            foreach (var h in allHolidays)
            {
                // Tarih verisi "yyyy-MM-dd" formatında olduğu için yıl ile başlıyor mu kontrol ediyoruz
                if (h.date.StartsWith(year))
                {
                    PrintHoliday(h);
                    found = true;
                }
            }

            if (!found)
            {
                Console.WriteLine("Bu yıla ait veri bulunamadı.");
            }
        }

        // 2. Tarihe göre tatil ara (gg-aa formatı)
        static void SearchByDate()
        {
            Console.Write("Tarih giriniz (gg-aa, Örn: 23-04): ");
            string inputDate = Console.ReadLine();

            // Kullanıcıdan gg-aa (örn: 23-04) alıyoruz.
            // API'deki format yyyy-MM-dd (örn: 2023-04-23).
            // Bu yüzden inputDate'i tersten kontrol etmemiz gerekebilir veya DateTime parse yapabiliriz.
            // Basitlik adına string işlemi yapalım.
            // Eğer input 23-04 ise, API verisinin sonu "-04-23" ile bitmeli. (Ay ve Gün yer değiştirmiş formatta)
            
            // Kullanıcı gg-aa giriyor: 29-10
            // Bizim aradığımız API formatında: MM-dd -> 10-29
            // O yüzden önce kullanıcının girdiği string'i parçalayalım.

            string searchPart = "";
            if (inputDate.Length == 5 && inputDate.Contains("-"))
            {
                string[] parts = inputDate.Split('-'); // [0]=gg, [1]=aa
                string day = parts[0];
                string month = parts[1];
                searchPart = $"-{month}-{day}"; // "-10-29"
            }
            else
            {
                Console.WriteLine("Hatalı format. Lütfen gg-aa şeklinde giriniz.");
                return;
            }

            bool found = false;
            foreach (var h in allHolidays)
            {
                // h.date "2023-10-29" -> EndsWith("-10-29") ?
                if (h.date.EndsWith(searchPart))
                {
                    PrintHoliday(h);
                    found = true;
                }
            }

            if (!found)
            {
                Console.WriteLine("Bu tarihte tatil bulunamadı.");
            }
        }

        // 3. İsme göre tatil ara
        static void SearchByName()
        {
            Console.Write("Tatil isminden bir parça giriniz: ");
            string search = Console.ReadLine().ToLower();

            bool found = false;
            foreach (var h in allHolidays)
            {
                // Hem Türkçe (localName) hem İngilizce (name) isminde arama yapıyoruz
                if ((h.localName != null && h.localName.ToLower().Contains(search)) || 
                    (h.name != null && h.name.ToLower().Contains(search)))
                {
                    PrintHoliday(h);
                    found = true;
                }
            }

            if (!found)
            {
                Console.WriteLine("Eşleşen tatil bulunamadı.");
            }
        }

        // 4. Tüm tatilleri 3 yıl boyunca göster (2023–2025)
        static void ShowAllHolidays()
        {
            if (allHolidays.Count == 0)
            {
                Console.WriteLine("Liste boş.");
                return;
            }

            foreach (var h in allHolidays)
            {
                PrintHoliday(h);
            }
        }

        // Ekrana yazdırma yardımcı metodu
        static void PrintHoliday(Holiday h)
        {
            // true/false değerini Türkçe 'Evet'/'Hayır' olarak göstermek daha şık olur
            string globalText = h.global ? "Evet" : "Hayır";
            
            Console.WriteLine($"Tarih: {h.date} | Tatil: {h.localName} ({h.name}) | Global: {globalText}");
        }
    }
}
