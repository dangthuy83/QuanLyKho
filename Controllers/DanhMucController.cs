using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class DanhMucController : Controller
{
    private readonly IDanhMucService _svc;
    public DanhMucController(IDanhMucService svc) => _svc = svc;

    // ── Nhóm VT ──────────────────────────────────────────────
    public async Task<IActionResult> NhomVatTu() => View(await _svc.GetNhomVatTuAsync());

    [HttpGet] public async Task<IActionResult> NhomVatTuForm(int id = 0)
    {
        var vm = id == 0 ? new NhomVatTuVM() : null;
        if (id > 0) { var e = await _svc.GetNhomByIdAsync(id); if (e == null) return NotFound();
            vm = new NhomVatTuVM { Id=e.Id, TenNhom=e.TenNhom, MoTa=e.MoTa }; }
        return PartialView("_NhomVatTuForm", vm!);
    }
    [HttpPost] public async Task<IActionResult> NhomVatTuSave(NhomVatTuVM vm)
    {
        if (!ModelState.IsValid) return PartialView("_NhomVatTuForm", vm);
        await _svc.SaveNhomAsync(vm); return Json(new { ok=true });
    }
    [HttpPost] public async Task<IActionResult> NhomVatTuDelete(int id)
    {
        try { await _svc.DeleteNhomAsync(id); return Json(new { ok=true }); }
        catch { return Json(new { ok=false, msg="Không thể xóa, nhóm đang được sử dụng." }); }
    }

    // ── Vật Tư ───────────────────────────────────────────────
    public async Task<IActionResult> VatTu() => View(await _svc.GetVatTuAsync());

    [HttpGet] public async Task<IActionResult> VatTuForm(int id = 0)
    {
        var vm = new VatTuVM { NhomList = await _svc.GetNhomSelectListAsync() };
        if (id == 0) vm.MaVt = await _svc.GenerateNextMaVatTuAsync();
        if (id > 0) { var e = await _svc.GetVatTuByIdAsync(id); if (e == null) return NotFound();
            vm.Id=e.Id; vm.MaVt=e.MaVt; vm.TenVt=e.TenVt; vm.IdNhom=e.IdNhom;
            vm.DonViTinh=e.DonViTinh; vm.LoaiVatTu=e.LoaiVatTu;
            vm.LaVatTuDichDanh=e.LaVatTuDichDanh; vm.CanTheoDoiTieuHao=e.CanTheoDoiTieuHao;
            vm.TonKhoToiThieu=e.TonKhoToiThieu; vm.IsActive=e.IsActive; }
        return PartialView("_VatTuForm", vm);
    }
    [HttpPost] public async Task<IActionResult> VatTuSave(VatTuVM vm)
    {
        vm.NhomList = await _svc.GetNhomSelectListAsync();
        if (!ModelState.IsValid) return PartialView("_VatTuForm", vm);
        await _svc.SaveVatTuAsync(vm); return Json(new { ok=true });
    }
    [HttpPost] public async Task<IActionResult> VatTuToggle(int id, bool isActive)
    { await _svc.SetVatTuActiveAsync(id, isActive); return Json(new { ok=true }); }

    // ── NCC ──────────────────────────────────────────────────
    public async Task<IActionResult> NhaCungCap() => View(await _svc.GetNhaCungCapAsync());

    [HttpGet] public async Task<IActionResult> NhaCungCapForm(int id = 0)
    {
        var vm = new NhaCungCapVM { IsActive=true };
        if (id > 0) { var e = await _svc.GetNccByIdAsync(id); if (e == null) return NotFound();
            vm = new NhaCungCapVM { Id=e.Id,TenNcc=e.TenNcc,DienThoai=e.DienThoai,DiaChiNcc=e.DiaChiNcc,GhiChu=e.GhiChu,IsActive=e.IsActive }; }
        return PartialView("_NhaCungCapForm", vm);
    }
    [HttpPost] public async Task<IActionResult> NhaCungCapSave(NhaCungCapVM vm)
    {
        if (!ModelState.IsValid) return PartialView("_NhaCungCapForm", vm);
        await _svc.SaveNccAsync(vm); return Json(new { ok=true });
    }
    [HttpPost] public async Task<IActionResult> NhaCungCapToggle(int id, bool isActive)
    { await _svc.SetNccActiveAsync(id, isActive); return Json(new { ok=true }); }

    // ── Bộ Phận ──────────────────────────────────────────────
    public async Task<IActionResult> BoPhan() => View(await _svc.GetBoPhanAsync());

    [HttpGet] public async Task<IActionResult> BoPhanForm(int id = 0)
    {
        var vm = new BoPhanVM { IsActive=true };
        if (id > 0) { var e = await _svc.GetBoPhanByIdAsync(id); if (e == null) return NotFound();
            vm = new BoPhanVM { Id=e.Id,TenBoPhan=e.TenBoPhan,GhiChu=e.GhiChu,IsActive=e.IsActive }; }
        return PartialView("_BoPhanForm", vm);
    }
    [HttpPost] public async Task<IActionResult> BoPhanSave(BoPhanVM vm)
    {
        if (!ModelState.IsValid) return PartialView("_BoPhanForm", vm);
        await _svc.SaveBoPhanAsync(vm); return Json(new { ok=true });
    }
    [HttpPost] public async Task<IActionResult> BoPhanToggle(int id, bool isActive)
    { await _svc.SetBoPhanActiveAsync(id, isActive); return Json(new { ok=true }); }
}
