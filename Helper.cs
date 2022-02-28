using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;

namespace Koko.XmlToGeneratedCode;

internal static class Helper {

    internal static int GetIntergerValue(string margin) {
        if (margin != null) {
            try {
                return int.Parse(margin);
            } catch (FormatException e) {
                Console.WriteLine(e.Message);
            }
        }
        return 0;
    }

    internal static int GetColumnsVal(XmlReader reader) {
        var columnsVal = GetIntergerValue(reader.GetAttribute("Columns"));
        return (columnsVal == 0) ? 2 : columnsVal;
    }

    internal static string GetBackgroundVal(XmlReader reader) {
        var background = reader.GetAttribute("BackGround-Color");
        if (background is null) {
            return "null";
        } else if (background.StartsWith("#")) {
            var rx = new Regex(@"^#(?<alpha>[0-9a-f]{2})?(?<red>[0-9a-f]{2})(?<green>[0-9a-f]{2})(?<blue>[0-9a-f]{2})$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var groups = rx.Matches(background)[0].Groups;
            var alpha = byte.Parse(groups[1].Value != "" ? groups[1].Value : "ff", NumberStyles.HexNumber);
            var red = byte.Parse(groups[2].Value, NumberStyles.HexNumber);
            var green = byte.Parse(groups[3].Value, NumberStyles.HexNumber);
            var blue = byte.Parse(groups[4].Value, NumberStyles.HexNumber);
            return $"new Color({red}, {green}, {blue}, {alpha})";
        } else {
            return "Color." + background;
        }
    }

    internal static bool GetBooleanAttribute(XmlReader reader, string attribute, bool defaultValue) {
        var value = reader.GetAttribute(attribute);
        if (value is null)
            return defaultValue;

        value = value.ToLower();
        if (value == "yes" || value == "true")
            return true;
        if (value == "no" || value == "false" || value == "0")
            return false;

        throw new ArgumentOutOfRangeException($"Don't know what to do with value '{value}' for boolean attribute");
    }


}
