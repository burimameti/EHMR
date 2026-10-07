using System.Text;

namespace EHMR.Helpers;

public static class MacedonianTransliterator
{
    private static readonly (string Latin, string Cyrillic)[] Digraphs =
    [
        ("dzh", "\u045F"),
        ("dz", "\u0455"),
        ("gj", "\u0453"),
        ("kj", "\u045C"),
        ("lj", "\u0459"),
        ("nj", "\u045A"),
        ("zh", "\u0436"),
        ("ch", "\u0447"),
        ("sh", "\u0448")
    ];

    private static readonly IReadOnlyDictionary<char, char> Letters =
        new Dictionary<char, char>
        {
            ['a']='\u0430', ['b']='\u0431', ['c']='\u0446', ['d']='\u0434',
            ['e']='\u0435', ['f']='\u0444', ['g']='\u0433', ['h']='\u0445',
            ['i']='\u0438', ['j']='\u0458', ['k']='\u043A', ['l']='\u043B',
            ['m']='\u043C', ['n']='\u043D', ['o']='\u043E', ['p']='\u043F',
            ['r']='\u0440', ['s']='\u0441', ['t']='\u0442', ['u']='\u0443',
            ['v']='\u0432', ['z']='\u0437'
        };

    public static string ToCyrillic(string value)
    {
        if(string.IsNullOrEmpty(value))
            return value;

        var result=new StringBuilder(value.Length);
        for(var index=0; index<value.Length;)
        {
            var matched=false;
            foreach(var (latin, cyrillic) in Digraphs)
            {
                if(index+latin.Length>value.Length||
                   !value.AsSpan(index, latin.Length).Equals(
                       latin, StringComparison.OrdinalIgnoreCase))
                    continue;

                result.Append(char.IsUpper(value[index])
                    ? cyrillic.ToUpperInvariant()
                    : cyrillic);
                index+=latin.Length;
                matched=true;
                break;
            }

            if(matched)
                continue;

            var character=value[index++];
            var lower=char.ToLowerInvariant(character);
            if(!Letters.TryGetValue(lower, out var converted))
            {
                result.Append(character);
                continue;
            }

            result.Append(char.IsUpper(character)
                ? char.ToUpperInvariant(converted)
                : converted);
        }

        return result.ToString()
            .Replace("\u0433\u0458", "\u0453", StringComparison.OrdinalIgnoreCase)
            .Replace("\u043A\u0458", "\u045C", StringComparison.OrdinalIgnoreCase)
            .Replace("\u043B\u0458", "\u0459", StringComparison.OrdinalIgnoreCase)
            .Replace("\u043D\u0458", "\u045A", StringComparison.OrdinalIgnoreCase)
            .Replace("\u0437\u0445", "\u0436", StringComparison.OrdinalIgnoreCase)
            .Replace("\u0446\u0445", "\u0447", StringComparison.OrdinalIgnoreCase)
            .Replace("\u0441\u0445", "\u0448", StringComparison.OrdinalIgnoreCase)
            .Replace("\u0455\u0445", "\u045F", StringComparison.OrdinalIgnoreCase)
            .Replace("\u0434\u0437", "\u0455", StringComparison.OrdinalIgnoreCase);
    }
}

        public static string ToLatin(string value)
    {
        if(string.IsNullOrEmpty(value))
            return value;

        var map = new Dictionary<char, string>
        {
            ['а']="a",['б']="b",['в']="v",['г']="g",['д']="d",['ѓ']="gj",['е']="e",
            ['ж']="zh",['з']="z",['ѕ']="dz",['и']="i",['ј']="j",['к']="k",['л']="l",
            ['љ']="lj",['м']="m",['н']="n",['њ']="nj",['о']="o",['п']="p",['р']="r",
            ['с']="s",['т']="t",['ќ']="kj",['у']="u",['ф']="f",['х']="h",['ц']="c",
            ['ч']="ch",['џ']="dzh",['ш']="sh"
        };

        var result=new StringBuilder(value.Length);
        foreach(var character in value)
        {
            var lower=char.ToLowerInvariant(character);
            if(!map.TryGetValue(lower, out var converted))
            {
                result.Append(character);
                continue;
            }

            result.Append(char.IsUpper(character)
                ? converted.Length==1
                    ? converted.ToUpperInvariant()
                    : char.ToUpperInvariant(converted[0])+converted[1..]
                : converted);
        }

        return result.ToString();
    }

    public static string ToBilingual(string value)
    {
        if(string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var hasCyrillic=value.Any(c => c is >= '\u0400' and <= '\u04FF');
        var first=hasCyrillic ? value : ToLatin(value);
        var second=hasCyrillic ? ToLatin(value) : ToCyrillic(value);

        return $"{first}\n{second}";
    }
