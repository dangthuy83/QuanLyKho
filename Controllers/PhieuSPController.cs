using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.Entities;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;
using System.Net;

namespace KhoQuanLy.Controllers;

public class PhieuSPController : Controller
{
    private readonly IPhieuSPService _svc;
    private readonly IDanhMucService _dmSvc;
    private readonly IPdfParserService _pdfParser;
    private readonly IExcelParserService _excelParser;
    private readonly IPdfStorageService _pdfStorage;
    public PhieuSPController(IPhieuSPService svc, IDanhMucService dmSvc,
        IPdfParserService pdfParser, IExcelParserService excelParser, IPdfStorageService pdfStorage)
    { _svc=svc; _dmSvc=dmSvc; _pdfParser=pdfParser; _excelParser=excelParser; _pdfStorage=pdfStorage; }

    public async Task<IActionResult> Index(string? trangThai, string? search)
    {
        var vm = new PhieuSPListVM
        {
            Items = (await _svc.GetAllAsync(trangThai, search)).ToList(),
            FilterTrangThai = trangThai, FilterSearch = search
        };
        return View(vm);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _svc.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> Create()
    {
        ViewBag.VatTuList = await _dmSvc.GetVatTuSelectListAsync();
        ViewBag.VatTuCuonList = await _dmSvc.GetVatTuSelectListAsync("giay_cuon");
        return View(new PhieuSP { TrangThai = "chua_hoan_thanh" });
    }

    [HttpPost] public async Task<IActionResult> Create(PhieuSP phieu,
        [FromForm] List<string?> chungLoai, [FromForm] List<string?> kichThuoc,
        [FromForm] List<decimal> soLuongYc, [FromForm] List<int?> idVatTu, [FromForm] List<int?> idVatTuThayThe,
        [FromForm] List<string?> dvt, [FromForm] List<string?> laCuonThayToRoi)
    {
        if (!ModelState.IsValid) { ViewBag.VatTuList = await _dmSvc.GetVatTuSelectListAsync(); ViewBag.VatTuCuonList = await _dmSvc.GetVatTuSelectListAsync("giay_cuon"); return View(phieu); }
        var vatTus = new List<PhieuSPVatTu>();
        var laCuonCheck = new HashSet<int>();
        if (laCuonThayToRoi != null)
        {
            int rowIdx = 0;
            foreach (var val in laCuonThayToRoi)
            {
                if (val == null) continue;
                if (val == "false") rowIdx++;
                else if (val == "true") laCuonCheck.Add(rowIdx - 1);
            }
        }

        for (int i = 0; i < soLuongYc.Count; i++)
        {
            if (soLuongYc[i] <= 0) continue;
            vatTus.Add(new PhieuSPVatTu
            {
                ChungLoaiText = chungLoai.ElementAtOrDefault(i),
                KichThuoc = kichThuoc.ElementAtOrDefault(i),
                SoLuongYeuCau = soLuongYc[i],
                IdVatTu = idVatTu.ElementAtOrDefault(i),
                IdVatTuThayThe = idVatTuThayThe.ElementAtOrDefault(i),
                DonViTinh = dvt.ElementAtOrDefault(i) ?? "Kg",
                LaCuonThayToRoi = laCuonCheck.Contains(i)
            });
        }
        try
        {
            var id = await _svc.CreateAsync(phieu, vatTus);
            TempData["Success"] = "Tạo phiếu sản phẩm thành công!";
            return RedirectToAction("Detail", new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.VatTuList = await _dmSvc.GetVatTuSelectListAsync();
            ViewBag.VatTuCuonList = await _dmSvc.GetVatTuSelectListAsync("giay_cuon");
            return View(phieu);
        }
    }

    [HttpPost] public async Task<IActionResult> UpdateTrangThai(int id, string trangThai)
    {
        await _svc.UpdateTrangThaiAsync(id, trangThai);
        return Json(new { ok = true });
    }

    [HttpPost] public async Task<IActionResult> UpdateVatTuCt(PhieuSPVatTuEditVM vm)
    {
        try
        {
            await _svc.UpdateVatTuCtAsync(vm);
            return Json(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, msg = ex.Message });
        }
    }

    [HttpPost] public async Task<IActionResult> XacNhanNhan(int id, decimal soLuong, string trangThai, string? ghiChu)
    {
        await _svc.XacNhanNhanAsync(id, soLuong, trangThai, ghiChu);
        return Json(new { ok = true });
    }

    // ── Upload (PDF + Excel) ──────────────────────────────────
    [HttpGet]
    public IActionResult Upload() => View();

    /// <summary>
    /// API preview dữ liệu từ file Excel, trả về JSON để render datagridview.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PreviewExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return Json(new { ok = false, msg = "Vui lòng chọn file." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return Json(new { ok = false, msg = "Chỉ hỗ trợ file .xlsx." });

        try
        {
            using var stream = file.OpenReadStream();
            var parsedList = await _excelParser.ParsePspAsync(stream);

            var previewRows = new List<ExcelPreviewRow>();
            var uniqueSoPhieu = new HashSet<string>();

            foreach (var (phieu, vatTus, warnings) in parsedList)
            {
                uniqueSoPhieu.Add(phieu.SoPhieu);
                var daTonTai = !string.IsNullOrWhiteSpace(phieu.SoPhieu)
                    ? await _svc.GetBySoPhieuAsync(phieu.SoPhieu) != null
                    : false;

                foreach (var vt in vatTus)
                {
                    var rowWarnings = new List<string>();
                    if (warnings.Any(w => w.Contains(vt.ChungLoaiText ?? "")))
                        rowWarnings.Add("VT chưa có danh mục");
                    if (daTonTai)
                        rowWarnings.Add("Phiếu đã tồn tại");

                    previewRows.Add(new ExcelPreviewRow
                    {
                        RowIndex = 0,
                        SoPhieu = phieu.SoPhieu,
                        TenSanPham = phieu.TenSanPham,
                        NgayLenh = phieu.NgayLenh?.ToString("dd/MM/yyyy"),
                        KhachHang = phieu.KhachHang,
                        SoLuongSp = phieu.SoLuongSp,
                        TenVatTu = vt.ChungLoaiText ?? "",
                        KichThuoc = vt.KichThuoc,
                        SoLuongToRoi = vt.SoLuongToRoi,
                        SoLuongYeuCau = vt.SoLuongYeuCau,
                        DonViTinh = vt.DonViTinh ?? "Kg",
                        CanhBao = rowWarnings.Count > 0 ? string.Join("; ", rowWarnings) : null,
                        DaTonTai = daTonTai,
                        DuocChon = !daTonTai
                    });
                }
            }

            var vm = new ExcelPreviewVM
            {
                TenFile = file.FileName,
                TongDong = previewRows.Count,
                SoPsp = uniqueSoPhieu.Count,
                Rows = previewRows
            };

            return Json(new { ok = true, data = vm });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, msg = $"Lỗi parse Excel: {ex.Message}" });
        }
    }

    [HttpPost]
    [RequestSizeLimit(10_000_000)] // 10MB
    public async Task<IActionResult> Upload(List<IFormFile> files, bool suaPhieuSanPham = false)
    {
        if (files == null || files.Count == 0 || files.All(f => f == null || f.Length == 0))
        {
            TempData["Error"] = "Vui lòng chọn ít nhất 1 file.";
            return RedirectToAction("Upload");
        }

        // Lấy IP client
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp != null && IPAddress.IsLoopback(remoteIp))
            remoteIp = null;

        var successMessages = new List<string>();
        var warningMessages = new List<string>();
        var errorMessages = new List<string>();

        foreach (var file in files.Where(f => f != null && f.Length > 0))
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext == ".pdf")
            {
                await ProcessPdfFile(file, remoteIp, suaPhieuSanPham, successMessages, warningMessages, errorMessages);
            }
            else if (ext == ".xlsx")
            {
                await ProcessExcelFile(file, suaPhieuSanPham, successMessages, warningMessages, errorMessages);
            }
            else
            {
                errorMessages.Add($"{file.FileName}: chỉ hỗ trợ file PDF (.pdf) hoặc Excel (.xlsx).");
            }
        }

        if (successMessages.Count == 0)
        {
            var finalMessages = warningMessages.Concat(errorMessages).ToList();
            TempData["Error"] = finalMessages.Count > 0
                ? string.Join(" || ", finalMessages)
                : "Không có file nào được import.";
            return RedirectToAction("Upload");
        }

        TempData["Success"] = $"Import thành công {successMessages.Count} phiếu/file.";
        if (warningMessages.Count > 0)
            TempData["Warning"] = string.Join(" || ", warningMessages);
        if (errorMessages.Count > 0)
            TempData["Error"] = string.Join(" || ", errorMessages);

        return RedirectToAction("Index");
    }

    // ── Private helpers ────────────────────────────────────────

    private async Task ProcessPdfFile(IFormFile file, IPAddress? remoteIp, bool suaPhieuSanPham,
        List<string> successMessages, List<string> warningMessages, List<string> errorMessages)
    {
        using var stream = file.OpenReadStream();
        var (phieu, vatTus, warnings) = await _pdfParser.ParsePspAsync(stream, remoteIp);

        if (phieu == null)
        {
            errorMessages.Add($"{file.FileName}: không thể parse file PDF. {string.Join("; ", warnings)}");
            return;
        }

        var existing = !string.IsNullOrWhiteSpace(phieu.SoPhieu)
            ? await _svc.GetBySoPhieuAsync(phieu.SoPhieu)
            : null;

        if (suaPhieuSanPham)
        {
            if (existing == null)
            {
                errorMessages.Add($"{file.FileName}: đang chọn chế độ sửa phiếu nhưng không tìm thấy phiếu {phieu.SoPhieu} trong hệ thống.");
                return;
            }

            var storedPdfSua = await _pdfStorage.SavePspPdfAsync(file);
            var msg = await _svc.ImportSuaPhieuAsync(phieu, vatTus, storedPdfSua.LogicalPath, phieu.NguoiTao, file.FileName);
            successMessages.Add($"{file.FileName}: {msg}");
            if (warnings.Count > 0)
                warningMessages.Add($"{file.FileName}: {string.Join(" | ", warnings)}");
            return;
        }

        if (existing != null)
        {
            warningMessages.Add($"{file.FileName}: bỏ qua vì phiếu {phieu.SoPhieu} đã tồn tại trong hệ thống.");
            return;
        }

        var storedPdf = await _pdfStorage.SavePspPdfAsync(file);
        phieu.FilePdfPath = storedPdf.LogicalPath;
        var id = await _svc.CreateAsync(phieu, vatTus);
        successMessages.Add($"{file.FileName} → {phieu.SoPhieu} ({vatTus.Count} dòng vật tư, ID {id})");
        if (warnings.Count > 0)
            warningMessages.Add($"{file.FileName}: {string.Join(" | ", warnings)}");
    }

    private async Task ProcessExcelFile(IFormFile file, bool suaPhieuSanPham,
        List<string> successMessages, List<string> warningMessages, List<string> errorMessages)
    {
        List<(PhieuSP Phieu, List<PhieuSPVatTu> VatTus, List<string> Warnings)> parsedList;
        try
        {
            using var stream = file.OpenReadStream();
            parsedList = await _excelParser.ParsePspAsync(stream);
        }
        catch (Exception ex)
        {
            errorMessages.Add($"{file.FileName}: lỗi parse Excel: {ex.Message}");
            return;
        }

        if (parsedList.Count == 0)
        {
            errorMessages.Add($"{file.FileName}: không tìm thấy dữ liệu PSP nào trong file Excel.");
            return;
        }

        var fileWarnings = new List<string>();
        foreach (var (phieu, vatTus, warnings) in parsedList)
        {
            if (string.IsNullOrWhiteSpace(phieu.SoPhieu))
            {
                errorMessages.Add($"{file.FileName}: có dòng thiếu số phiếu.");
                continue;
            }

            if (vatTus.Count == 0)
            {
                warningMessages.Add($"{file.FileName}: phiếu {phieu.SoPhieu} không có dòng vật tư hợp lệ.");
                continue;
            }

            var existing = await _svc.GetBySoPhieuAsync(phieu.SoPhieu);

            if (suaPhieuSanPham)
            {
                if (existing == null)
                {
                    errorMessages.Add($"{file.FileName}: đang chọn chế độ sửa phiếu nhưng không tìm thấy phiếu {phieu.SoPhieu} trong hệ thống.");
                    continue;
                }

                var msg = await _svc.ImportSuaPhieuAsync(phieu, vatTus, $"Excel:{file.FileName}", phieu.NguoiTao, file.FileName);
                successMessages.Add($"{file.FileName}: {msg}");
                fileWarnings.AddRange(warnings);
                continue;
            }

            if (existing != null)
            {
                warningMessages.Add($"{file.FileName}: bỏ qua phiếu {phieu.SoPhieu} vì đã tồn tại trong hệ thống.");
                continue;
            }

            phieu.FilePdfPath = $"Excel:{file.FileName}";
            var id = await _svc.CreateAsync(phieu, vatTus);
            successMessages.Add($"{file.FileName} → {phieu.SoPhieu} ({vatTus.Count} dòng vật tư, ID {id})");
            fileWarnings.AddRange(warnings);
        }

        if (fileWarnings.Count > 0)
            warningMessages.Add($"{file.FileName}: {string.Join(" | ", fileWarnings.Distinct())}");
    }
}