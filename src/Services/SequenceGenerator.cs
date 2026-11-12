using System;
using System.Collections.Generic;
using SafeKeyBinder.Models;

namespace SafeKeyBinder.Services
{
    public class SequenceGenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> Items { get; set; }

        public SequenceGenerationResult()
        {
            Success = false;
            ErrorMessage = string.Empty;
            Items = new List<string>();
        }
    }

    public static class SequenceGenerator
    {
        public const int MaxGeneratedItems = 5000;

        public static SequenceGenerationResult Generate(AutoSequenceConfig config)
        {
            SequenceGenerationResult result = new SequenceGenerationResult();
            if (config == null)
            {
                result.ErrorMessage = "Konfigurasi urutan kosong.";
                return result;
            }

            switch (config.PatternMode)
            {
                case SequencePatternMode.LetterRange:
                    return GenerateLetters(config);
                case SequencePatternMode.NumberRange:
                    return GenerateNumbers(config);
                case SequencePatternMode.CustomList:
                    return GenerateCustomList(config);
                default:
                    result.ErrorMessage = "Mode pola urutan tidak dikenali.";
                    return result;
            }
        }

        public static string FormatCommand(string template, string item)
        {
            string value = item ?? string.Empty;
            if (string.IsNullOrEmpty(template))
            {
                return value;
            }

            if (template.IndexOf("{item}", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return ReplaceOrdinalIgnoreCase(template, "{item}", value);
            }

            return template + value;
        }

        private static SequenceGenerationResult GenerateLetters(AutoSequenceConfig config)
        {
            SequenceGenerationResult res = new SequenceGenerationResult();
            string start = (config.LetterStart ?? string.Empty).Trim();
            string end = (config.LetterEnd ?? string.Empty).Trim();
            string prefix = config.PrefixText ?? string.Empty;
            string suffix = config.SuffixText ?? string.Empty;

            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
            {
                res.ErrorMessage = "Huruf Awal dan Huruf Akhir wajib diisi (contoh: a sampai z).";
                return res;
            }

            if (start.Length != end.Length)
            {
                res.ErrorMessage = "Panjang huruf Awal dan Akhir harus sama (misal 'a' ke 'z', atau 'aa' ke 'zz').";
                return res;
            }

            if (start.Length > 3)
            {
                res.ErrorMessage = "Maksimal 3 huruf kombinasi (contoh: aaa sampai zzz).";
                return res;
            }

            bool useUpper = char.IsUpper(start[0]);
            string startLower = start.ToLowerInvariant();
            string endLower = end.ToLowerInvariant();

            if (!IsAsciiLettersOnly(startLower) || !IsAsciiLettersOnly(endLower))
            {
                res.ErrorMessage = "Rentang huruf hanya boleh berisi huruf A-Z.";
                return res;
            }

            long startVal = Base26ToLong(startLower);
            long endVal = Base26ToLong(endLower);
            if (startVal > endVal)
            {
                res.ErrorMessage = "Huruf Awal tidak boleh lebih besar dari Huruf Akhir.";
                return res;
            }

            long count = (endVal - startVal) + 1;
            if (count > MaxGeneratedItems)
            {
                res.ErrorMessage = string.Format("Jumlah item ({0}) melebihi batas aman {1} item.", count, MaxGeneratedItems);
                return res;
            }

            int width = start.Length;
            for (long val = startVal; val <= endVal; val++)
            {
                string letters = LongToBase26(val, width);
                if (useUpper)
                {
                    letters = letters.ToUpperInvariant();
                }
                res.Items.Add(prefix + letters + suffix);
            }

            res.Success = true;
            return res;
        }

        private static SequenceGenerationResult GenerateNumbers(AutoSequenceConfig config)
        {
            SequenceGenerationResult res = new SequenceGenerationResult();
            string prefix = config.PrefixText ?? string.Empty;
            string suffix = config.SuffixText ?? string.Empty;

            if (config.NumberStart > config.NumberEnd)
            {
                res.ErrorMessage = "Angka Awal tidak boleh lebih besar dari Angka Akhir.";
                return res;
            }

            long count = ((long)config.NumberEnd - (long)config.NumberStart) + 1;
            if (count > MaxGeneratedItems)
            {
                res.ErrorMessage = string.Format("Jumlah item ({0}) melebihi batas aman {1} item.", count, MaxGeneratedItems);
                return res;
            }

            int pad = config.NumberPadDigits;
            if (pad < 0) pad = 0;
            if (pad > 8) pad = 8;

            for (int n = config.NumberStart; n <= config.NumberEnd; n++)
            {
                string numStr = pad > 0 ? n.ToString().PadLeft(pad, '0') : n.ToString();
                res.Items.Add(prefix + numStr + suffix);
            }

            res.Success = true;
            return res;
        }

        private static SequenceGenerationResult GenerateCustomList(AutoSequenceConfig config)
        {
            SequenceGenerationResult res = new SequenceGenerationResult();
            string raw = config.CustomItemsText ?? string.Empty;
            string[] lines = raw.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    res.Items.Add(trimmed);
                    if (res.Items.Count >= MaxGeneratedItems)
                    {
                        break;
                    }
                }
            }

            if (res.Items.Count == 0)
            {
                res.ErrorMessage = "Daftar manual masih kosong. Isi minimal 1 baris.";
                return res;
            }

            res.Success = true;
            return res;
        }

        private static bool IsAsciiLettersOnly(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c < 'a' || c > 'z')
                {
                    return false;
                }
            }
            return true;
        }

        private static long Base26ToLong(string letters)
        {
            long acc = 0;
            for (int i = 0; i < letters.Length; i++)
            {
                acc = (acc * 26) + (letters[i] - 'a');
            }
            return acc;
        }

        private static string LongToBase26(long value, int width)
        {
            char[] buf = new char[width];
            long temp = value;
            for (int i = width - 1; i >= 0; i--)
            {
                buf[i] = (char)('a' + (temp % 26));
                temp /= 26;
            }
            return new string(buf);
        }

        private static string ReplaceOrdinalIgnoreCase(string source, string find, string replacement)
        {
            string result = source;
            int idx = result.IndexOf(find, StringComparison.OrdinalIgnoreCase);
            while (idx >= 0)
            {
                result = result.Substring(0, idx) + replacement + result.Substring(idx + find.Length);
                idx = result.IndexOf(find, idx + replacement.Length, StringComparison.OrdinalIgnoreCase);
            }
            return result;
        }
    }
}
