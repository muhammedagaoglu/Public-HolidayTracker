namespace PublicHolidayTracker
{
    // API'den gelen veriyi karşılayacak sınıf.
    // Özellik isimleri (date, localName vb.) JSON verisindeki alanlarla birebir aynı olmalıdır.
    public class Holiday
    {
        public string date { get; set; }
        public string localName { get; set; }
        public string name { get; set; }
        public string countryCode { get; set; }
        
        // 'fixed' C# dilinde rezerve edilmiş (ayrılmış) bir kelime olduğu için
        // değişken isminin başına '@' koyarak onu kullanabiliriz.
        public bool @fixed { get; set; } 
        
        public bool global { get; set; }
    }
}
