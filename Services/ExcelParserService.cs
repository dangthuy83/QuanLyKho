using KhoQuanLy.Models.Entities;
using KhoQuanLy.Repositories;
using ClosedXML.Excel;
using System.Text.RegularExpressions;

namespace KhoQuanLy.Services;

public interface IExcelParserService
{
    /// <summary>
    /// Parse file Excel PSP, trả về danh sách (Phiếu, Vật tư, Cảnh báo).
    /// 1 file Excel có thể chứa nhiều PSP, mỗi dòng = 1 vật tư + header lặp lại.
    /// </summary>
    Task<List<(PhieuSP Phieu, List<PhieuSPVatTu> VatTus, List<string> Warnings)>> ParsePspAsync(Stream excelStream);
}

public class ExcelParserService : IExcelParserService
{
    private const int HeaderRow = 10;
    private const int DataStartRow = 11;
    private const string SheetName = "Sheet1";

    // Column indexes (1-based)
    private const int ColSoPhieu = 3;     // C
    private const int ColTenSanPham = 4;  // D
    private const int ColNgayLenh = 5;    // E
    private const int ColKhachHang = 6;   // F
    private const int ColSoLuongSp = 8;   // H
    private const int ColTenVatTu = 11;   // K
    private const int ColKichThuoc = 17;  // Q
    private const int ColSoLuongToRoi = 18; // R
    private const int ColSoLuongYeuCau = 20; // T

    private const int SoPhieuMaxLength = 50;
    private const int TenSanPhamMaxLength = 200;
    private const int KhachHangMaxLength = 200;
    private const int NguoiTaoMaxLength = 100;
    private const int TenVatTuMaxLength = 200;
    private const int KichThuocMaxLength = 100;

    private readonly IVatTuRepository _vtRepo;

    public ExcelParserService(IVatTuRepository vtRepo)
    {
        _vtRepo = vtRepo;
    }

    public async Task<List<(PhieuSP Phieu, List<PhieuSPVatTu> VatTus, List<string> Warnings)>> ParsePspAsync(Stream excelStream)
    {
        var result = new List<(PhieuSP Phieu, List<PhieuSPVatTu> VatTus, List<string> Warnings)>();
        var allVatTus = (await _vtRepo.GetAllAsync()).ToList();
        var nguoiTao = GetCurrentMachineName();

        using var workbook = new XLWorkbook(excelStream);
        var sheet = workbook.Worksheet(SheetName);
        if (sheet == null)
            throw new InvalidOperationException($"Không tìm thấy sheet '{SheetName}' trong file Excel.");

        // Validate header row
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? DataStartRow - 1;
        if (lastRow < DataStartRow)
            throw new InvalidOperationException("File Excel không có dòng dữ liệu nào (hàng dữ liệu bắt đầu từ 11).");

        // Dictionary nhóm dòng theo SoPhieu
        // Key: SoPhieu, Value: list các dòng vật tư
        var pspGroups = new Dictionary<string, List<(int RowIndex, string TenVatTu, string? KichThuoc, decimal? SoLuongToRoi, decimal SoLuongYeuCau)>>();
        var headerCache = new Dictionary<string, (string TenSanPham, string? NgayLenhRaw, string? KhachHang, decimal? SoLuongSp)>();

        for (int row = DataStartRow; row <= lastRow; row++)
        {
            var soPhieu = GetStringCell(sheet, row, ColSoPhieu);
            if (string.IsNullOrWhiteSpace(soPhieu))
                continue; // Bỏ qua dòng trống

            // Chuẩn hóa số phiếu
            soPhieu = soPhieu.Trim();
            if (soPhieu.Length > SoPhieuMaxLength)
                soPhieu = soPhieu[..SoPhieuMaxLength];

            // Header fields
            var tenSanPham = NormalizeTextField(GetStringCell(sheet, row, ColTenSanPham), TenSanPhamMaxLength);
            var ngayLenhRaw = GetStringCell(sheet, row, ColNgayLenh);
            var khachHang = NormalizeTextField(GetStringCell(sheet, row, ColKhachHang), KhachHangMaxLength);
            var soLuongSp = GetDecimalCell(sheet, row, ColSoLuongSp);

            if (!headerCache.ContainsKey(soPhieu))
            {
                headerCache[soPhieu] = (tenSanPham ?? "", ngayLenhRaw, khachHang, soLuongSp);
            }

            // Vật tư fields
            var tenVatTu = GetStringCell(sheet, row, ColTenVatTu);
            if (string.IsNullOrWhiteSpace(tenVatTu))
                continue; // Bỏ qua dòng không có tên vật tư

            tenVatTu = CleanMaterialName(tenVatTu.Trim());

            // Chỉ lấy vật tư loại Giấy (giống PDF parser)
            if (!Regex.IsMatch(tenVatTu, @"^Giấy", RegexOptions.IgnoreCase))
                continue;

            if (tenVatTu.Length > TenVatTuMaxLength)
                tenVatTu = tenVatTu[..TenVatTuMaxLength];

            var kichThuoc = GetStringCell(sheet, row, ColKichThuoc);
            if (!string.IsNullOrWhiteSpace(kichThuoc) && kichThuoc.Length > KichThuocMaxLength)
                kichThuoc = kichThuoc[..KichThuocMaxLength];

            var slToRoi = GetDecimalCell(sheet, row, ColSoLuongToRoi);
            var slYeuCau = GetDecimalCell(sheet, row, ColSoLuongYeuCau);

            if (slYeuCau == null || slYeuCau.Value <= 0)
                continue; // Bỏ qua dòng không có SL yêu cầu

            if (!pspGroups.ContainsKey(soPhieu))
                pspGroups[soPhieu] = new List<(int, string, string?, decimal?, decimal)>();

            pspGroups[soPhieu].Add((row, tenVatTu, kichThuoc, slToRoi, slYeuCau.Value));
        }

        // Build result
        foreach (var kvp in pspGroups)
        {
            var soPhieu = kvp.Key;
            var header = headerCache[soPhieu];
            var warnings = new List<string>();

            // Parse ngày
            DateTime? ngayLenh = ParseExcelDate(header.NgayLenhRaw);

            var phieu = new PhieuSP
            {
                SoPhieu = soPhieu,
                SoLsx = null, // Không có trong Excel
                TenSanPham = header.TenSanPham,
                MaSanPham = null, // Không có trong Excel
                KhachHang = header.KhachHang,
                SoLuongSp = header.SoLuongSp,
                NgayLenh = ngayLenh,
                NgayGiaoHang = null, // Không có trong Excel
                TrangThai = "chua_hoan_thanh",
                NguoiTao = nguoiTao
            };

            if (string.IsNullOrEmpty(phieu.TenSanPham))
                warnings.Add($"Phiếu {soPhieu}: thiếu tên sản phẩm (cột D).");

            var vatTus = new List<PhieuSPVatTu>();
            foreach (var vtRow in kvp.Value)
            {
                var vt = new PhieuSPVatTu
                {
                    ChungLoaiText = vtRow.TenVatTu,
                    KichThuoc = vtRow.KichThuoc,
                    SoLuongYeuCau = vtRow.SoLuongYeuCau,
                    SoLuongToRoi = (vtRow.SoLuongToRoi.HasValue && vtRow.SoLuongToRoi.Value > 0) ? vtRow.SoLuongToRoi.Value : null,
                    DonViTinh = "Kg", // Mặc định
                    SoLuongDaNhan = 0,
                    TrangThaiNhan = "chua_nhan"
                };

                // Match vật tư với danh mục (giống PdfParserService)
                var matched = allVatTus.FirstOrDefault(v => IsSameMaterialName(v.TenVt, vt.ChungLoaiText))
                    ?? allVatTus.FirstOrDefault(v => IsNearMaterialName(v.TenVt, vt.ChungLoaiText));
                if (matched != null)
                {
                    vt.IdVatTu = matched.Id;
                    vt.DonViTinh = matched.DonViTinh;
                }
                else if (!string.IsNullOrEmpty(vt.ChungLoaiText))
                {
                    warnings.Add($"Phiếu {soPhieu}: Vật tư \"{vt.ChungLoaiText}\" (dòng {vtRow.RowIndex}) chưa có trong danh mục.");
                }

                vatTus.Add(vt);
            }

            result.Add((phieu, vatTus, warnings));
        }

        return result;
    }

    // ── Helper methods ────────────────────────────────────────

    private static string? GetStringCell(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return null;
        var val = cell.GetString();
        return string.IsNullOrWhiteSpace(val) ? null : val.Trim();
    }

    private static decimal? GetDecimalCell(IXLWorksheet sheet, int row, int col)
    {
        var cell = sheet.Cell(row, col);
        if (cell.IsEmpty()) return null;

        try
        {
            if (cell.DataType == XLDataType.Number)
            {
                return (decimal)cell.GetDouble();
            }

            // Try parse string
            var text = cell.GetString().Trim();
            return ParseDecimalFromExcel(text);
        }
        catch
        {
            return null;
        }
    }

    private static decimal? ParseDecimalFromExcel(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        // Remove dots used as thousand separators, then parse
        // e.g. "4.280" -> "4280", but "69.5" should keep the dot
        // Detect: if the last dot is followed by exactly 3 digits, it's likely thousand separator
        // Better approach: remove all dots that are thousand separators
        var cleaned = value.Replace(",", ".");
        // If there's a dot followed by 3 digits and then possibly more, it might be thousand separator
        // Simple approach: remove all dots, then parse
        // Exception: numbers like "69.5" - but this is kich thuoc, not sl
        cleaned = cleaned.Replace(".", "");
        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return null;
    }

    private static DateTime? ParseExcelDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Try OADate (Excel serial number)
        if (double.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var oaDate))
        {
            try { return DateTime.FromOADate(oaDate); }
            catch { }
        }

        // Try date string formats
        string[] formats = { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "dd/MM/yy" };
        if (DateTime.TryParseExact(value.Trim(), formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var dt))
            return dt;

        // Try general parse
        if (DateTime.TryParse(value, out var dt2))
            return dt2;

        return null;
    }

    private static string? NormalizeTextField(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = Regex.Replace(value, @"\s+", " ").Trim();
        if (normalized.Length > maxLength)
            normalized = normalized[..maxLength].Trim();
        return normalized;
    }

    private static string CleanMaterialName(string value)
    {
        var cleaned = Regex.Replace(value ?? "", @"\s+", " ").Trim();

        // Khử dính lặp OCR: "GiấyGiấy" -> "Giấy"
        cleaned = Regex.Replace(cleaned, @"^(Giấy){2,}", "Giấy", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(Màng){2,}", "Màng", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(Mực){2,}", "Mực", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"^(Khuôn){2,}", "Khuôn", RegexOptions.IgnoreCase);

        // Khử lặp cách khoảng
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

    private static string GetCurrentMachineName()
    {
        try { return Environment.MachineName; }
        catch { return "unknown"; }
    }
}