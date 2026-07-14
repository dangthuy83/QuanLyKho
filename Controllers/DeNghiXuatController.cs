using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class DeNghiXuatController : Controller
{
    private readonly IDeNghiXuatService _svc;
    private readonly IPhieuSPService _pspSvc;

    public DeNghiXuatController(IDeNghiXuatService svc, IPhieuSPService pspSvc)
    { _svc=svc; _pspSvc=pspSvc; }

    public async Task<IActionResult> Index() => View(await _svc.GetAllAsync());

    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _svc.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> Create()
    {
        var linkedCount = await _pspSvc.AutoLienKetVatTuChuaCoDanhMucAsync();
        if (linkedCount > 0)
            TempData["Success"] = $"Đã tự liên kết {linkedCount} dòng vật tư PSP với danh mục kho_vat_tu.";
        ViewBag.PhieuList = await BuildPhieuCoVatTuMoiAsync();
        return View();
    }

    [HttpPost] public async Task<IActionResult> Create(DotDeNghiCreateVM vm)
    {
        if (!vm.IdPhieuSpVatTuList.Any() && !vm.IdPendingCtList.Any())
        {
            ModelState.AddModelError("", "Vui lòng chọn ít nhất một dòng vật tư mới hoặc dòng chưa nhận/nhận thiếu từ đợt trước.");
            ViewBag.PhieuList = await BuildPhieuCoVatTuMoiAsync();
            return View(vm);
        }
        try
        {
            await _pspSvc.AutoLienKetVatTuChuaCoDanhMucAsync();
            var id = await _svc.TaoDotAsync(vm.IdPhieuSpVatTuList, vm.IdPendingCtList, vm.GhiChu, vm.NguoiTao);
            return RedirectToAction("Detail", new { id });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.PhieuList = await BuildPhieuCoVatTuMoiAsync();
            return View(vm);
        }
    }

    private async Task<List<KhoQuanLy.Models.Entities.PhieuSP>> BuildPhieuCoVatTuMoiAsync()
    {
        var phieuList = await _pspSvc.GetAllAsync("chua_hoan_thanh");
        var pendingVatTuIds = (await _svc.GetPendingFromPreviousDotsAsync())
            .Select(x => x.IdPhieuSpVatTu)
            .ToHashSet();

        var phieuCoVatTuMoi = new List<KhoQuanLy.Models.Entities.PhieuSP>();
        foreach (var phieu in phieuList)
        {
            var detail = await _pspSvc.GetDetailAsync(phieu.Id);
            var vatTus = detail?.VatTus?.ToList() ?? new List<KhoQuanLy.Models.Entities.PhieuSPVatTu>();
            if (vatTus.Any(vt => !pendingVatTuIds.Contains(vt.Id) && CanDeNghiTiep(vt)))
                phieuCoVatTuMoi.Add(phieu);
        }

        return phieuCoVatTuMoi;
    }

    [HttpPost] public async Task<IActionResult> GetVatTuByMultiplePhieu([FromBody] List<int> ids)
    {
        if (ids == null || !ids.Any()) return Json(new List<object>());
        await _pspSvc.AutoLienKetVatTuChuaCoDanhMucAsync();
        var result = new List<object>();
        var pendingVatTuIds = (await _svc.GetPendingFromPreviousDotsAsync())
            .Select(x => x.IdPhieuSpVatTu)
            .ToHashSet();
        foreach (var id in ids)
        {
            var detail = await _pspSvc.GetDetailAsync(id);
            if (detail?.VatTus != null)
            {
                foreach (var vt in detail.VatTus)
                {
                    if (pendingVatTuIds.Contains(vt.Id)) continue;
                    if (!CanDeNghiTiep(vt)) continue;
                    result.Add(new
                    {
                        vt.Id,
                        vt.IdPhieuSp,
                        SoPhieu = detail.PhieuSP.SoPhieu,
                        TenSanPham = detail.PhieuSP.TenSanPham,
                        vt.ChungLoaiText,
                        GiayTheoPdf = string.IsNullOrWhiteSpace(vt.TenVt) ? vt.ChungLoaiText : vt.TenVt,
                        GiayThucCap = vt.LaCuonThayToRoi && !string.IsNullOrWhiteSpace(vt.TenVtThayThe) ? vt.TenVtThayThe : (string.IsNullOrWhiteSpace(vt.TenVt) ? vt.ChungLoaiText : vt.TenVt),
                        vt.KichThuoc,
                        vt.SoLuongYeuCau,
                        DonViTinh = vt.LaCuonThayToRoi && !string.IsNullOrWhiteSpace(vt.DonViTinhThayThe) ? vt.DonViTinhThayThe : vt.DonViTinh
                    });
                }
            }
        }
        return Json(result);
    }

    private static bool CanDeNghiTiep(KhoQuanLy.Models.Entities.PhieuSPVatTu vt)
        => vt.TrangThaiNhan != "du"
            && vt.TrangThaiNhan != "khong_can"
            && vt.SoLuongDaNhan < vt.SoLuongYeuCau;

    [HttpPost] public async Task<IActionResult> UpdateThucNhan(int idCt, decimal soLuong)
    {
        try
        {
            await _svc.UpdateThucNhanAsync(idCt, soLuong);
            return Json(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, msg = ex.Message });
        }
    }

    [HttpPost] public async Task<IActionResult> HoanThanh(int id)
    {
        await _svc.HoanThanhDotAsync(id);
        return Json(new { ok = true });
    }

    [HttpPost]
    public async Task<IActionResult> GetPendingFromPreviousDots()
    {
        await _pspSvc.AutoLienKetVatTuChuaCoDanhMucAsync();
        var pending = await _svc.GetPendingFromPreviousDotsAsync();
        var result = pending.Select(d => new
        {
            id = d.Id,
            idPhieuSpVatTu = d.IdPhieuSpVatTu,
            idCt = d.Id,
            maDot = d.MaDot ?? "",
            soPhieu = d.SoPhieu ?? "",
            tenSanPham = d.TenSanPham ?? "",
            chungLoaiText = d.ChungLoaiText ?? "",
            giayTheoPdf = string.IsNullOrWhiteSpace(d.TenVt) ? (d.ChungLoaiText ?? "") : d.TenVt,
            giayThucCap = !string.IsNullOrWhiteSpace(d.TenVtThayThe) ? d.TenVtThayThe : (string.IsNullOrWhiteSpace(d.TenVt) ? (d.ChungLoaiText ?? "") : d.TenVt),
            kichThuoc = d.KichThuoc ?? "",
            soLuongConLai = d.SoLuongDeNghi ?? 0,
            donViTinh = !string.IsNullOrWhiteSpace(d.DonViTinhThayThe) ? d.DonViTinhThayThe : (d.DonViTinh ?? "")
        });
        return Json(result);
    }

    public async Task<IActionResult> In(int id)
    {
        var vm = await _svc.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }
}