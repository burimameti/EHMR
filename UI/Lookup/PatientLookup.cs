namespace EHMR.UI.Lookup;

public static class MacedoniaCityLookup
{
    public record LookupItem(string Key, string Display);

    public static readonly IReadOnlyList<LookupItem> All =
    [
        new("All", "Сите градови"),

        new("Skopje", "Скопје"),
        new("Bitola", "Битола"),
        new("Kumanovo", "Куманово"),
        new("Prilep", "Прилеп"),
        new("Tetovo", "Тетово"),
        new("Veles", "Велес"),
        new("Ohrid", "Охрид"),
        new("Stip", "Штип"),
        new("Gostivar", "Гостивар"),
        new("Strumica", "Струмица"),
        new("Kavadarci", "Кавадарци"),
        new("Kocani", "Кочани"),
        new("Kicevo", "Кичево"),
        new("Struga", "Струга"),
        new("Radovis", "Радовиш"),
        new("Gevgelija", "Гевгелија"),
        new("Debar", "Дебар"),
        new("KrivaPalanka", "Крива Паланка"),
        new("Berovo", "Берово"),
        new("Delcevo", "Делчево"),
        new("Resen", "Ресен"),
        new("Probistip", "Пробиштип"),
        new("Valandovo", "Валандово"),
        new("Negotino", "Неготино"),
        new("SvetiNikole", "Свети Николе"),
        new("Vinica", "Виница"),
        new("Krusevo", "Крушево"),
        new("Bogdanci", "Богданци"),
        new("Pehcevo", "Пехчево"),
        new("Novaci", "Новаци")
    ];

    public class PatientFilterContext
    {
        public string SearchText { get; set; } = "";
        public string Status { get; set; } = "All";
        public string Gender { get; set; } = "All";
        public string BloodType { get; set; } = "All";
        public string City { get; set; } = "All";
        public string AgeGroup { get; set; } = "All";
    }
}