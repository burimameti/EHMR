using System;
using System.Collections.Generic;

namespace EHMR.Domain.Entities.Rbac;

public class IconDefinition
{
    public string Glyph { get; set; } = string.Empty;

    public IconFontType Font
    {
        get; set;
    }

    public IconDefinition()
    {
    }

    public IconDefinition(string glyph, IconFontType font)
    {
        Glyph=glyph;
        Font=font;
    }
}

public enum IconFontType
{
    FontAwesomeSolid,
    Fluent
}

public static class FontResolver
{
    public static string GetFontFamily(IconFontType type)
    {
        return type switch
        {
            IconFontType.FontAwesomeSolid => "FASolid",
            IconFontType.Fluent => "FontIcons",
            _ => "FASolid"
        };
    }
}