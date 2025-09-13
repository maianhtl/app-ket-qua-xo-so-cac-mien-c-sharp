using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace KetQuaXoSo
{
    public class XoSoHelper
    {
        public class KetQuaXoSo
        {
            public string Ngay { get; set; } = "";
            public DateTime NgayQuay { get; set; }
            public string DacBiet { get; set; } = "";
            public string Giai1 { get; set; } = "";
            public string Giai2 { get; set; } = "";
            public List<string> Giai3 { get; set; } = new List<string>();
            public List<string> Giai4 { get; set; } = new List<string>();
            public string Giai5 { get; set; } = "";
            public List<string> Giai6 { get; set; } = new List<string>();
            public List<string> Giai7 { get; set; } = new List<string>();
            public string Giai8 { get; set; } = "";
            public bool CoGiai8 { get; set; } = false;
        }

        public static class XoSoParser
        {
            public static async Task<List<KetQuaXoSo>> ParseRss(string rssUrl)
            {
                var ketQuaList = new List<KetQuaXoSo>();

                using (var httpClient = new HttpClient())
                {
                    var settings = new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Parse
                    };

                    using (var stream = await httpClient.GetStreamAsync(rssUrl))
                    using (var reader = XmlReader.Create(stream, settings))
                    {
                        var feed = SyndicationFeed.Load(reader);

                        foreach (var item in feed.Items)
                        {
                            var tokens = item.Summary.Text
                                .Replace("\n", " ").Replace("\r", " ").Trim()
                                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .Where(t => t != "-").ToList();

                            var kq = new KetQuaXoSo();
                            kq.Ngay = item.Title.Text;

                            var matchDate = Regex.Match(item.Title.Text, @"(\d{2}[-/]\d{2}([-/]\d{4})?)");
                            if (matchDate.Success)
                            {
                                string rawDate = matchDate.Value;
                                if (Regex.IsMatch(rawDate, @"^\d{2}[-/]\d{2}$"))
                                    rawDate += "/" + DateTime.Now.Year;

                                DateTime ngay;
                                if (DateTime.TryParseExact(rawDate,
                                    new[] { "dd-MM-yyyy", "dd/MM/yyyy" },
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    System.Globalization.DateTimeStyles.None,
                                    out ngay))
                                {
                                    kq.NgayQuay = ngay.Date;
                                }
                            }

                            string currentKey = "";

                            for (int i = 0; i < tokens.Count; i++)
                            {
                                string token = tokens[i];

                                var match78 = Regex.Match(token, @"^(?<g7>\d{3,4}):(?<g8>\d{2})$");
                                if (currentKey == "7" && match78.Success)
                                {
                                    AddValue(kq, "7", match78.Groups["g7"].Value);
                                    AddValue(kq, "8", match78.Groups["g8"].Value);
                                    continue;
                                }

                                if (token == "7:" && i + 2 < tokens.Count && tokens[i + 1].EndsWith(":"))
                                {
                                    string giai7 = tokens[i + 1].TrimEnd(':');
                                    string giai8 = tokens[i + 2];
                                    if (giai7.Length > 3)
                                        giai7 = giai7.Substring(0, 3);

                                    AddValue(kq, "7", giai7);
                                    AddValue(kq, "8", giai8);
                                    i += 2;
                                    continue;
                                }

                                if (token.EndsWith(":"))
                                {
                                    currentKey = token.TrimEnd(':');
                                }
                                else if (token.Contains(":"))
                                {
                                    var parts = token.Split(new[] { ':' }, 2, StringSplitOptions.RemoveEmptyEntries);
                                    currentKey = parts[0];
                                    AddValue(kq, currentKey, parts[1]);
                                }
                                else
                                {
                                    AddValue(kq, currentKey, token);
                                }
                            }

                            kq.CoGiai8 = !string.IsNullOrEmpty(kq.Giai8);
                            ketQuaList.Add(kq);
                        }
                    }
                }

                return ketQuaList;
            }
        }

        private static void AddValue(KetQuaXoSo kq, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value)) return;

            switch (key)
            {
                case "ĐB": kq.DacBiet = value; break;
                case "1": kq.Giai1 = value; break;
                case "2": kq.Giai2 = value; break;
                case "3": kq.Giai3.Add(value); break;
                case "4": kq.Giai4.Add(value); break;
                case "5": kq.Giai5 = value; break;
                case "6": kq.Giai6.Add(value); break;
                case "7": kq.Giai7.Add(value); break;
                case "8": kq.Giai8 = value; break;
            }
        }

        public static class Program
        {
            public static string ToSlug(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return "";
                input = input.Replace("đ", "d").Replace("Đ", "D");
                string normalized = input.Normalize(System.Text.NormalizationForm.FormD);
                var chars = normalized
                    .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                                != System.Globalization.UnicodeCategory.NonSpacingMark)
                    .ToArray();

                return Regex.Replace(new string(chars), @"[^a-zA-Z0-9]+", "-")
                    .Trim('-')
                    .ToLower();
            }

            public static string ToShortCode(string input)
            {
                input = input.Replace("đ", "d").Replace("Đ", "D");
                if (string.IsNullOrWhiteSpace(input)) return "";

                var chars = input.Normalize(System.Text.NormalizationForm.FormD)
                                 .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                                             != System.Globalization.UnicodeCategory.NonSpacingMark)
                                 .ToArray();

                return new string(chars).ToLower()
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Aggregate("", (acc, part) => acc + part[0]);
            }

            public static string CreateCodeRSS(string prv)
            {
                string code = ToShortCode(prv);
                string tinh = ToSlug(prv);

                switch (tinh)
                {
                    case "binh-dinh": code = "bdi"; break;
                    case "da-nang": code = "dng"; break;
                    case "dak-nong": code = "dno"; break;
                    case "quang-ngai": code = "qng"; break;
                    case "quang-nam": code = "qnm"; break;
                    case "dak-lak": code = "dlk"; break;
                    case "binh-thuan": code = "bth"; break;
                }
                return code;
            }

            public static string CreateRSSLink(string prv, string code)
            {
                string tinh = ToSlug(prv);
                return string.Format("https://xskt.com.vn/rss-feed/{0}-xs{1}.rss", tinh, code);
            }

            public static string KiemTraVeSo(string ve, KetQuaXoSo kq)
            {
                if (string.IsNullOrWhiteSpace(ve) || ve.Length < 2 || ve.Length > 6)
                    return "Vé phải từ 2 đến 6 số!";

                Func<string, string, bool> Match = (veso, giai) =>
                    !string.IsNullOrEmpty(giai) && veso.EndsWith(giai);

                if (kq.CoGiai8 && Match(ve, kq.Giai8)) return "Trúng giải 8";
                if (kq.Giai7.Any(g => Match(ve, g))) return "Trúng giải 7";
                if (kq.Giai6.Any(g => Match(ve, g))) return "Trúng giải 6";
                if (!string.IsNullOrEmpty(kq.Giai5) && Match(ve, kq.Giai5)) return "Trúng giải 5";
                if (kq.Giai4.Any(g => Match(ve, g))) return "Trúng giải 4";
                if (kq.Giai3.Any(g => Match(ve, g))) return "Trúng giải 3";
                if (!string.IsNullOrEmpty(kq.Giai2) && Match(ve, kq.Giai2)) return "Trúng giải 2";
                if (!string.IsNullOrEmpty(kq.Giai1) && Match(ve, kq.Giai1)) return "Trúng giải 1";
                if (!string.IsNullOrEmpty(kq.DacBiet) && Match(ve, kq.DacBiet)) return "Trúng giải Đặc Biệt";

                if (!string.IsNullOrEmpty(kq.DacBiet) &&
                    ve.EndsWith(kq.DacBiet.Substring(Math.Max(0, kq.DacBiet.Length - 2))))
                {
                    return "Trúng giải an ủi";
                }

                return "Không trúng";
            }
        }
    }
}
