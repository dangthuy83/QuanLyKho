using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using KhoQuanLy.Models.Entities;
using KhoQuanLy.Repositories;
using System.Text.RegularExpressions;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace KhoQuanLy.Services;

public interface IPdfParserService
{
    Task<(PhieuSP? Phieu, List<PhieuSPVatTu> VatTus, List<string> Warnings)> ParsePspAsync(Stream pdfStream, IPAddress? clientIp);
}

public class PdfParserService : IPdfParserService
{
    private const int SoPhieuMaxLength = 50;
    private const int SoLsxMaxLength = 50;
    private const int TenSanPhamMaxLength = 255;
    private const int MaSanPhamMaxLength = 50;
    private const int KhachHangMaxLength = 200;
    private const int NguoiTaoMaxLength = 100;
    private readonly IVatTuRepository _vtRepo;
    private readonly string _mayTinhJsonPath;

    public PdfParserService(IVatTuRepository vtRepo)
    {
        _vtRepo = vtRepo;
        _mayTinhJsonPath = Path.Combine(Directory.GetCurrentDirectory(), "may_tinh.json");
    }

    public async Task<(PhieuSP? Phieu, List<PhieuSPVatTu> VatTus, List<string> Warnings)> ParsePspAsync(
        Stream pdfStream, IPAddress? clientIp)
    {
        var warnings = new List<string>();
        using var pdf = PdfDocument.Open(pdfStream);
        var allText = string.Join("\n", pdf.GetPages().Select(p => p.Text));

        // ── 1. Parse header ──────────────────────────────────────
        var phieu = ParseHeader(allText, clientIp);
        if (phieu == null)
        {
            warnings.Add("Không thể trích xuất thông tin header từ PDF.");
            return (null, new List<PhieuSPVatTu>(), warnings);
        }

        // ── 2. Parse vật tư ──────────────────────────────────────
        var vatTus = ParseVatTuLines(allText);
        if (vatTus.Count == 0)
        {
            warnings.Add("Không tìm thấy dòng vật tư nào trong PDF.");
            return (phieu, vatTus, warnings);
        }

        // ── 3. Match vật tư với DB ────────────────────────────────
        var allVatTus = (await _vtRepo.GetAllAsync()).ToList();
        foreach (var vt in vatTus)
        {
            var matched = allVatTus.FirstOrDefault(v => IsSameMaterialName(v.TenVt, vt.ChungLoaiText))
                ?? allVatTus.FirstOrDefault(v => IsNearMaterialName(v.TenVt, vt.ChungLoaiText));
            if (matched != null)
            {
                vt.IdVatTu = matched.Id;
                vt.DonViTinh = matched.DonViTinh;
            }
            else if (!string.IsNullOrEmpty(vt.ChungLoaiText))
            {
                warnings.Add($"Vật tư \"{vt.ChungLoaiText}\" chưa có trong danh mục. Sẽ lưu dạng text tự do.");
            }
        }

        return (phieu, vatTus, warnings);
    }

    private PhieuSP? ParseHeader(string text, IPAddress? clientIp)
    {
        var soLsxRaw = ExtractBetweenLabels(text, "LSX:", "(", "Số phiếu sản phẩm con:", "1. Tên sản phẩm:");
        var soPhieuRaw = ExtractBetweenLabels(text, "Số phiếu sản phẩm con:", "1. Tên sản phẩm:", "2. Mã sản phẩm:");
        var tenSanPhamRaw = ExtractBetweenLabels(text, "1. Tên sản phẩm:", "2. Mã sản phẩm:", "3. Khách hàng:");
        var maSanPhamRaw = ExtractBetweenLabels(text, "2. Mã sản phẩm:", "3. Khách hàng:", "4.Số lượng", "4. Số lượng");
        var khachHangRaw = ExtractValueByLabel(text, "3. Khách hàng:",
                "Số phiếu sản phẩm con:",
                "I. THÔNG TIN SẢN PHẨM CON:",
                "1. Tên sản phẩm:",
                "4.Số lượng",
                "4. Số lượng",
                "7.Ngày nhận LSX:",
                "7. Ngày nhận LSX:")
            ?? ExtractBetweenLabels(text, "3. Khách hàng:", "4.Số lượng", "4. Số lượng", "7.Ngày nhận LSX:", "7. Ngày nhận LSX:");
        var soLuongSpRaw = ExtractNumberByLabel(text, "4.Số lượng (Cái):")
            ?? ExtractNumberByLabel(text, "4. Số lượng (Cái):")
            ?? ExtractNumberByLabel(text, "4.Số lượng:")
            ?? ExtractNumberByLabel(text, "4. Số lượng:")
            ?? ExtractNumberByLabel(text, "3.Số lượng:")
            ?? ExtractNumberByLabel(text, "3. Số lượng:");
        var soLuongSp = ParseDecimal(soLuongSpRaw);
        var ngayLenhRaw = ExtractDateAfterLabel(text, "7.Ngày nhận LSX:")
            ?? ExtractDateAfterLabel(text, "7. Ngày nhận LSX:")
            ?? ExtractBetweenLabels(text, "7.Ngày nhận LSX:", "8.Ngày giao hàng:", "8. Ngày giao hàng:")
            ?? ExtractBetweenLabels(text, "7. Ngày nhận LSX:", "8.Ngày giao hàng:", "8. Ngày giao hàng:");
        var ngayGiaoHangRaw = ExtractDateAfterLabel(text, "8.Ngày giao hàng:")
            ?? ExtractDateAfterLabel(text, "8. Ngày giao hàng:")
            ?? ExtractBetweenLabels(text, "8.Ngày giao hàng:", "9.", "Ngày duyệt", "Ghi chú")
            ?? ExtractBetweenLabels(text, "8. Ngày giao hàng:", "9.", "Ngày duyệt", "Ghi chú");
        var ngayLenh = ParseDateFromText(ngayLenhRaw);
        var ngayGiaoHang = ParseDateFromText(ngayGiaoHangRaw);
        var nguoiTaoRaw = GetNguoiTaoFromClientIp(clientIp);
        var soLsx = NormalizeCodeField(soLsxRaw, SoLsxMaxLength);
        var soPhieu = NormalizeSoPhieu(soPhieuRaw);
        var tenSanPham = NormalizeTextField(tenSanPhamRaw, TenSanPhamMaxLength,
            "2.", "3.", "4.", "5.", "6.", "7.", "8.", "Mã sản phẩm:", "Khách hàng:", "Số lượng", "Ngày nhận", "Ngày giao");
        var maSanPham = NormalizeCodeField(maSanPhamRaw, MaSanPhamMaxLength,
            "3.", "4.", "5.", "6.", "7.", "8.", "Khách hàng:", "Số lượng", "Ngày nhận", "Ngày giao");
        var khachHang = NormalizeCustomerField(khachHangRaw, KhachHangMaxLength,
            "4.", "5.", "6.", "7.", "8.", "Số lượng", "Ngày nhận", "Ngày giao");
        var nguoiTao = NormalizeTextField(nguoiTaoRaw, NguoiTaoMaxLength);

        if (string.IsNullOrEmpty(soPhieu)) return null;

        return new PhieuSP
        {
            SoPhieu = soPhieu.Trim(),
            SoLsx = soLsx,
            TenSanPham = tenSanPham ?? "",
            MaSanPham = maSanPham,
            KhachHang = khachHang,
            SoLuongSp = soLuongSp,
            NgayLenh = ngayLenh,
            NgayGiaoHang = ngayGiaoHang,
            TrangThai = "chua_hoan_thanh",
            NguoiTao = nguoiTao
        };
    }

    private List<PhieuSPVatTu> ParseVatTuLines(string text)
    {
        var result = new List<PhieuSPVatTu>();

        // Tìm section "VẬT TƯ SẢN XUẤT"
        var sectionStart = text.IndexOf("VẬT TƯ SẢN XUẤT");
        if (sectionStart < 0)
            sectionStart = text.IndexOf("Nguyên liệu đích danh");
        if (sectionStart < 0) return result;

        var section = text[sectionStart..];
        var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Skip header lines, find material data lines
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            // Skip section headers
            if (trimmed.StartsWith("VẬT TƯ") || trimmed.StartsWith("Nguyên liệu")
                || trimmed.StartsWith("STT") || trimmed.StartsWith("Loại")
                || trimmed.StartsWith("Tên") || trimmed.StartsWith("---"))
                continue;

            // Try to parse as a material data line
            var vt = ParseSingleVatTuLine(trimmed);
            if (vt != null) result.Add(vt);
        }

        // Một số PDF do PdfPig trích text không giữ xuống dòng trong bảng vật tư,
        // toàn bộ header + dòng dữ liệu + footer bị dính thành một chuỗi dài.
        // Khi đó parser line-by-line phía trên sẽ không nhận diện được dòng vật tư.
        if (result.Count == 0)
        {
            result.AddRange(ParseVatTuFromContinuousText(section));
        }

        return result;
    }

    private List<PhieuSPVatTu> ParseVatTuFromContinuousText(string section)
    {
        var result = new List<PhieuSPVatTu>();
        if (string.IsNullOrWhiteSpace(section)) return result;

        var normalized = Regex.Replace(section.Replace('\r', ' ').Replace('\n', ' '), @"\s+", " ").Trim();

        // Cấu trúc thực tế khi bảng bị dính dòng:
        // ... LoạiCăn chỉnhGiấyGiấy Duplex 230 K695 Hanchang 0,40 1,00 6,60 Kg 498 1 69.5x72.5 4.280 3.646 600 ...
        // Chỉ lấy các field cần lưu: tên vật tư, ĐVT, SL yêu cầu, kích thước, SL tờ rời.
        var pattern = @"(?<name>(?:Giấy|Màng|Mực|Khuôn){1,2}[\p{L}\p{N}\s\-_/\.]+?)\s+" +
                      @"\d+(?:[\.,]\d{1,2})\s+\d+(?:[\.,]\d{1,2})\s+\d+(?:[\.,]\d{1,2})\s+" +
                      @"(?<unit>Kg|Kgs|Tờ|To|Tấm|Tam|Cái|Cai|Cuộn|Cuon|Mét|Met|m|Lít|Lit)\s+" +
                      @"(?<slYc>\d+(?:[\.,]\d{3})*(?:[\.,]\d+)?)\s+" +
                      @"\d+\s+" +
                      @"(?<kichThuoc>\d+(?:[\.,]\d+)?\s*[xX]\s*\d+(?:[\.,]\d+)?)\s+" +
                      @"(?<slToRoi>\d+(?:[\.,]\d{3})*(?:[\.,]\d+)?)\s+" +
                      @"\d+(?:[\.,]\d{3})*(?:[\.,]\d+)?\s+\d+";

        foreach (Match match in Regex.Matches(normalized, pattern, RegexOptions.IgnoreCase))
        {
            var rawName = match.Groups["name"].Value.Trim();
            foreach (var marker in new[] { "Căn chỉnh", "Loại" })
            {
                var idx = rawName.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    rawName = rawName[(idx + marker.Length)..].Trim();
                }
            }

            var cleanName = CleanMaterialName(rawName);
            var kichThuoc = match.Groups["kichThuoc"].Value.Trim();
            var donViTinh = match.Groups["unit"].Value.Trim();
            var slYeuCau = ParseDecimal(match.Groups["slYc"].Value);
            var slToRoi = ParseDecimal(match.Groups["slToRoi"].Value);

            if (string.IsNullOrWhiteSpace(cleanName)) continue;
            if (!IsKichThuoc(kichThuoc)) continue;
            if (!IsLikelyUnit(donViTinh)) continue;
            if (slYeuCau == null || slYeuCau.Value <= 0) continue;

            result.Add(new PhieuSPVatTu
            {
                ChungLoaiText = cleanName,
                KichThuoc = kichThuoc,
                SoLuongYeuCau = slYeuCau.Value,
                SoLuongToRoi = (slToRoi.HasValue && slToRoi.Value > 0) ? slToRoi.Value : null,
                DonViTinh = donViTinh
            });
        }

        return result;
    }

    private PhieuSPVatTu? ParseSingleVatTuLine(string line)
    {
        // Line format:
        // GiấyGiấy Duplex 230 K695 Hanchang 0,40  1,00  6,60 Kg 498  1 69.5x72.5 4.280  3.646  600
        try
        {
            // Split by whitespace
            var tokens = Regex.Split(line.Trim(), @"\s+").ToList();
            if (tokens.Count < 8) return null;

            // Work backwards from the end to identify known fields theo spec:
            // ... [tên vật tư] [tỷ lệ xả] [tỷ lệ in] [tỷ lệ GC] [ĐVT] [SL yêu cầu] [STT] [Kích thước] [SL tờ] [SL cần đạt] [Căn chỉnh]
            var slToIn = ParseDecimal(tokens[^3]);
            var kichThuoc = tokens[^4];
            var slYeuCau = ParseDecimal(tokens[^6]);
            var donViTinh = tokens[^7];

            if (!IsKichThuoc(kichThuoc)) return null;
            if (!IsLikelyUnit(donViTinh)) return null;

            // 10 token cuối không thuộc tên vật tư: 3 tỷ lệ + ĐVT + SL yêu cầu + STT + Kích thước + SL tờ + SL cần đạt + Căn chỉnh.
            var tenVtTokens = tokens.Take(tokens.Count - 10).ToList();
            var tenVt = string.Join(" ", tenVtTokens);

            // Clean name: remove "Giấy" type prefix if present
            var cleanName = CleanMaterialName(tenVt);
            if (string.IsNullOrWhiteSpace(cleanName)) cleanName = tenVt;

            if (slYeuCau == null || slYeuCau.Value <= 0) return null;

            return new PhieuSPVatTu
            {
                ChungLoaiText = cleanName,
                KichThuoc = kichThuoc,
                SoLuongYeuCau = slYeuCau.Value,
                SoLuongToRoi = (slToIn.HasValue && slToIn.Value > 0) ? slToIn.Value : null,
                DonViTinh = donViTinh
            };
        }
        catch
        {
            return null;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static string? ExtractValue(string text, string pattern)
    {
        var m = Regex.Match(text, pattern, RegexOptions.Multiline);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? ExtractBetweenLabels(string text, string startLabel, params string[] endLabels)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(startLabel)) return null;

        var startIdx = text.IndexOf(startLabel, StringComparison.OrdinalIgnoreCase);
        if (startIdx < 0) return null;

        startIdx += startLabel.Length;
        var remaining = text[startIdx..];
        var endIdx = remaining.Length;

        foreach (var endLabel in endLabels.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var idx = remaining.IndexOf(endLabel, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0 && idx < endIdx)
            {
                endIdx = idx;
            }
        }

        var raw = remaining[..endIdx];
        raw = raw.Replace('\r', ' ').Replace('\n', ' ');
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }

    private static string? ExtractSingleLineLabelValue(string text, string label)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label)) return null;

        var normalizedText = text.Replace('\r', '\n');
        foreach (var line in normalizedText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            var value = line[(idx + label.Length)..].Trim();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }

    private static string? ExtractSingleLineNumberAfterLabel(string text, string label)
    {
        var lineValue = ExtractSingleLineLabelValue(text, label);
        if (string.IsNullOrWhiteSpace(lineValue)) return null;

        var match = Regex.Match(lineValue, @"\b\d{1,3}(?:[\.,]\d{3})*\b");
        return match.Success ? match.Value : null;
    }

    private static string? ExtractValueByLabel(string text, string label, params string[] stopLabels)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label)) return null;

        var labelPattern = Regex.Escape(label);
        var stopPattern = string.Join("|", stopLabels
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(Regex.Escape));

        if (string.IsNullOrWhiteSpace(stopPattern))
        {
            var simple = Regex.Match(text, labelPattern + @"\s*(.+)", RegexOptions.IgnoreCase);
            return simple.Success ? simple.Groups[1].Value.Trim() : null;
        }

        var pattern = labelPattern + @"\s*(.*?)\s*(?=" + stopPattern + @")";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Replace('\r', ' ').Replace('\n', ' ').Trim() : null;
    }

    private static string? ExtractNumberByLabel(string text, string label)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label)) return null;

        var pattern = Regex.Escape(label) + @"\s*(\d{1,3}(?:[\.,]\d{3})*)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? ExtractDateAfterLabel(string text, string label)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(label)) return null;

        var pattern = Regex.Escape(label) + @"\s*(\d{2}/\d{2}/\d{4})";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string? NormalizeSoPhieu(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim();

        // Nếu text PDF bị dính các nhãn khác cùng dòng, chỉ giữ phần đầu trước nhãn kế tiếp.
        normalized = Regex.Split(normalized,
            @"\s+(?:1\.|2\.|3\.|4\.|5\.|6\.|7\.|8\.|LSX:|Tên sản phẩm:|Mã sản phẩm:|Khách hàng:|Số lượng|Ngày nhận|Ngày giao)",
            RegexOptions.IgnoreCase)[0].Trim();

        // Ưu tiên bắt theo format số phiếu thực tế: 001017/2026/HNI-OFF hoặc tương tự.
        var strongMatch = Regex.Match(normalized, @"\b[0-9A-Za-z._-]+/[0-9A-Za-z._-]+(?:/[0-9A-Za-z._-]+)*\b");
        if (strongMatch.Success)
        {
            normalized = strongMatch.Value.Trim();
        }
        else
        {
            // Fallback: chỉ giữ token đầu tiên nếu parser vẫn kéo dư text.
            normalized = Regex.Split(normalized, @"\s+")[0].Trim();
        }

        if (string.IsNullOrWhiteSpace(normalized)) return null;

        if (normalized.Length > SoPhieuMaxLength)
        {
            return normalized[..SoPhieuMaxLength].Trim();
        }

        return normalized;
    }

    private static string? NormalizeCodeField(string? value, int maxLength, params string[] stopMarkers)
    {
        var normalized = NormalizeTextField(value, maxLength, stopMarkers);
        if (string.IsNullOrWhiteSpace(normalized)) return normalized;

        var firstToken = Regex.Split(normalized, @"\s+")[0].Trim();
        return string.IsNullOrWhiteSpace(firstToken) ? normalized : firstToken;
    }

    private static string? NormalizeTextField(string? value, int maxLength, params string[] stopMarkers)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = Regex.Replace(value, @"\s+", " ").Trim();

        foreach (var marker in stopMarkers.Where(m => !string.IsNullOrWhiteSpace(m)))
        {
            var idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                normalized = normalized[..idx].Trim();
            }
        }

        normalized = normalized.Trim(' ', '-', ':', ';', '|');
        if (string.IsNullOrWhiteSpace(normalized)) return null;

        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength].Trim();
        }

        return normalized;
    }

    private static string? NormalizeCustomerField(string? value, int maxLength, params string[] stopMarkers)
    {
        var normalized = NormalizeTextField(value, maxLength, stopMarkers);
        if (string.IsNullOrWhiteSpace(normalized)) return normalized;

        foreach (var hardStop in new[] { "4.Số lượng", "4. Số lượng", "7.Ngày nhận LSX", "7. Ngày nhận LSX" })
        {
            var idx = normalized.IndexOf(hardStop, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                normalized = normalized[..idx].Trim();
            }
        }

        // Nếu OCR/PDF dính thêm mô tả ở sau tên KH, ưu tiên giữ phần trước các dấu phân tách phổ biến.
        foreach (var separator in new[] { "Số phiếu sản phẩm con:", "I. THÔNG TIN SẢN PHẨM CON:", "1. Tên sản phẩm:", " - ", " | ", " ; ", " / MST", " MST", " Địa chỉ:", " Dia chi:" })
        {
            var idx = normalized.IndexOf(separator, StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
            {
                normalized = normalized[..idx].Trim();
                break;
            }
        }

        if (normalized.Length > maxLength)
        {
            normalized = normalized[..maxLength].Trim();
        }

        return normalized;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        // Remove dots used as thousand separators, then parse
        // e.g. "4.280" -> "4280", but "69.5" (kich thuoc) should not reach here
        var cleaned = value.Replace(".", "").Replace(",", ".");
        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        if (DateTime.TryParseExact(value.Trim(), "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }

    private static DateTime? ParseDateFromText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var match = Regex.Match(value, @"\b\d{2}/\d{2}/\d{4}\b");
        return match.Success ? ParseDate(match.Value) : ParseDate(value);
    }

    private static bool IsKichThuoc(string token)
    {
        return Regex.IsMatch(token, @"^\d+(?:[\.,]\d+)?\s*[xX]\s*\d+(?:[\.,]\d+)?$");
    }

    private static bool IsLikelyUnit(string token)
    {
        return Regex.IsMatch(token, @"^(Kg|Kgs|Tờ|To|Tấm|Tam|Cái|Cai|Cuộn|Cuon|Mét|Met|m|Lít|Lit)$", RegexOptions.IgnoreCase);
    }

    private static string CleanMaterialName(string value)
    {
        var cleaned = Regex.Replace(value ?? "", @"\s+", " ").Trim();
        
        // Khử dính lặp OCR: "GiấyGiấy" -> "Giấy", "MàngMàng" -> "Màng", "MựcMực" -> "Mực"
        cleaned = Regex.Replace(cleaned, @"^(Giấy){2,}", "Giấy", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(Màng){2,}", "Màng", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(Mực){2,}", "Mực", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(Khuôn){2,}", "Khuôn", RegexOptions.IgnoreCase);

        // Khử lặp cách khoảng: "Giấy Giấy" -> "Giấy"
        cleaned = Regex.Replace(cleaned, @"^Giấy\s+Giấy", "Giấy", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^Màng\s+Màng", "Màng", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^Mực\s+Mực", "Mực", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^Khuôn\s+Khuôn", "Khuôn", RegexOptions.IgnoreCase);

        return cleaned.Length > 200 ? cleaned[..200].Trim() : cleaned;
    }

    private static bool IsSameMaterialName(string? a, string? b)
    {
        return NormalizeMaterialName(a) == NormalizeMaterialName(b);
    }

    private static bool IsNearMaterialName(string? catalogName, string? parsedName)
    {
        var c = NormalizeMaterialName(catalogName);
        var p = NormalizeMaterialName(parsedName);
        if (string.IsNullOrWhiteSpace(c) || string.IsNullOrWhiteSpace(p)) return false;
        return c.Contains(p) || p.Contains(c);
    }

    private static string NormalizeMaterialName(string? value)
    {
        var cleaned = CleanMaterialName(value ?? "").ToLowerInvariant();
        cleaned = Regex.Replace(cleaned, @"[^\p{L}\p{N}]+", " ");
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private string? GetNguoiTaoFromClientIp(IPAddress? clientIp)
    {
        try
        {
            if (!File.Exists(_mayTinhJsonPath)) return null;
            var json = File.ReadAllText(_mayTinhJsonPath);
            var machines = JsonSerializer.Deserialize<List<MayTinhEntry>>(json);

            var candidateIps = new List<IPAddress>();
            if (clientIp != null)
            {
                candidateIps.Add(clientIp);
            }

            if (clientIp == null || IPAddress.IsLoopback(clientIp))
            {
                candidateIps.AddRange(Dns.GetHostAddresses(Dns.GetHostName())
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork));
            }

            var entry = machines?.FirstOrDefault(m =>
                IPAddress.TryParse(m.Ip, out var ip) && candidateIps.Any(c => c.Equals(ip)));

            if (entry != null) return entry.TenMay;

            return Environment.MachineName;
        }
        catch
        {
            return Environment.MachineName;
        }
    }

    private class MayTinhEntry
    {
        public string Ip { get; set; } = "";
        public string TenMay { get; set; } = "";
    }
}