using System.Text;

namespace LangFlip
{
    internal static class TextMapper
    {
        private static readonly Dictionary<char, char> EnglishToArabic = new()
        {
            { 'q', 'ض' }, { 'Q', 'ض' },
            { 'w', 'ص' }, { 'W', 'ص' },
            { 'e', 'ث' }, { 'E', 'ث' },
            { 'r', 'ق' }, { 'R', 'ق' },
            { 't', 'ف' }, { 'T', 'ف' },
            { 'y', 'غ' }, { 'Y', 'غ' },
            { 'u', 'ع' }, { 'U', 'ع' },
            { 'i', 'ه' }, { 'I', 'ه' },
            { 'o', 'خ' }, { 'O', 'خ' },
            { 'p', 'ح' }, { 'P', 'ح' },
            { '[', 'ج' }, { '{', 'ج' },
            { ']', 'د' }, { '}', 'د' },
            
            { 'a', 'ش' }, { 'A', 'ش' },
            { 's', 'س' }, { 'S', 'س' },
            { 'd', 'ي' }, { 'D', 'ي' },
            { 'f', 'ب' }, { 'F', 'ب' },
            { 'g', 'ل' }, { 'G', 'ل' },
            { 'h', 'ا' }, { 'H', 'ا' },
            { 'j', 'ت' }, { 'J', 'ت' },
            { 'k', 'ن' }, { 'K', 'ن' },
            { 'l', 'م' }, { 'L', 'م' },
            { ';', 'ك' }, { ':', 'ك' },
            { '\'', 'ط' }, { '"', 'ط' },
            
            { 'z', 'ئ' }, { 'Z', 'ئ' },
            { 'x', 'ء' }, { 'X', 'ء' },
            { 'c', 'ؤ' }, { 'C', 'ؤ' },
            { 'v', 'ر' }, { 'V', 'ر' },
            { 'n', 'ى' }, { 'N', 'ى' },
            { 'm', 'ة' }, { 'M', 'ة' },
            { ',', 'و' }, { '<', 'و' },
            { '.', 'ز' }, { '>', 'ز' },
            { '/', 'ظ' }, { '?', 'ظ' },
            
            { '\\', 'ذ' }, { '|', 'ذ' },
        };

        private static readonly Dictionary<char, char> ArabicToEnglish = new()
        {
            { 'ض', 'q' }, { 'ص', 'w' }, { 'ث', 'e' }, { 'ق', 'r' },
            { 'ف', 't' }, { 'غ', 'y' }, { 'ع', 'u' }, { 'ه', 'i' },
            { 'خ', 'o' }, { 'ح', 'p' }, { 'ج', '[' }, { 'د', ']' },
            { 'ش', 'a' }, { 'س', 's' }, { 'ي', 'd' }, { 'ب', 'f' },
            { 'ل', 'g' }, { 'ا', 'h' }, { 'ت', 'j' }, { 'ن', 'k' },
            { 'م', 'l' }, { 'ك', ';' }, { 'ط', '\'' },
            { 'ئ', 'z' }, { 'ء', 'x' }, { 'ؤ', 'c' }, { 'ر', 'v' },
            { 'ى', 'n' }, { 'ة', 'm' }, { 'و', ',' },
            { 'ز', '.' }, { 'ظ', '/' }, { 'ذ', '\\' },
        };

        public static bool ContainsArabic(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            foreach (char c in text)
            {
                if (c >= 0x0600 && c <= 0x06FF)
                    return true;
            }
            return false;
        }

        public static string ConvertText(string text, bool toArabic)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            var sb = new StringBuilder(text.Length * 2); // May expand for 'b' → 'لا'

            if (toArabic)
            {
                foreach (char c in text)
                {
                    if (c == 'b' || c == 'B')
                    {
                        // b/B should become the lam-alif pair as typed on an Arabic keyboard
                        sb.Append('ل');
                        sb.Append('ا');
                    }
                    else if (EnglishToArabic.TryGetValue(c, out char arabicChar))
                    {
                        sb.Append(arabicChar);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            else
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    
                    if (c == 'ل' && i + 1 < text.Length && text[i + 1] == 'ا')
                    {
                        // lam-alif together maps back to a single b
                        sb.Append('b');
                        i++; // Skip the next character ('ا')
                    }
                    else if (ArabicToEnglish.TryGetValue(c, out char englishChar))
                    {
                        sb.Append(englishChar);
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            return sb.ToString();
        }
    }
}

