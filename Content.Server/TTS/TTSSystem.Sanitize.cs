using System.Text.RegularExpressions;
using Content.Server.Chat.Systems;

namespace Content.Server.TTS;

public sealed partial class TTSSystem
{
    [GeneratedRegex(@"[^a-zA-Z0-9,\-+?!. ]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"(?<=[0-9])(\.|,)(?=[0-9])")]
    private static partial Regex DecimalSeparatorRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsRegex();

    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!_isEnabled)
            return;

        args.Message = args.Message.Replace("+", "");
    }

    private static string Sanitize(string text)
    {
        text = text.Trim();
        text = InvalidCharsRegex().Replace(text, "");
        text = DecimalSeparatorRegex().Replace(text, " point ");
        text = DigitsRegex().Replace(text, match => NumberToWords(match.Value));
        return text.Trim();
    }

    private static readonly string[] Ones =
    [
        "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine",
        "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen"
    ];

    private static readonly string[] Tens =
    [
        "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety"
    ];

    private static readonly (long Value, string Name)[] Scales =
    [
        (1_000_000_000_000, "trillion"),
        (1_000_000_000, "billion"),
        (1_000_000, "million"),
        (1_000, "thousand"),
        (100, "hundred")
    ];

    private static string NumberToWords(string digits)
    {
        if (!long.TryParse(digits, out var number))
            return digits;

        return number switch
        {
            0 => "zero",
            < 0 => "negative " + NumberToWords((-number).ToString()),
            >= 1_000_000_000_000_000 => digits,
            _ => ConvertNumber(number)
        };
    }

    private static string ConvertNumber(long number)
    {
        var parts = new List<string>();

        foreach (var (value, name) in Scales)
        {
            if (number < value)
                continue;

            parts.Add($"{NumberToWords((number / value).ToString())} {name}");
            number %= value;
        }

        switch (number)
        {
            case >= 20:
                var ten = Tens[number / 10];
                var one = Ones[number % 10];
                parts.Add(string.IsNullOrEmpty(one) ? ten : $"{ten} {one}");
                break;
            case > 0:
                parts.Add(Ones[number]);
                break;
        }

        return string.Join(" ", parts);
    }
}
