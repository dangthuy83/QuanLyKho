using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class KhuonBeController : Controller
{
    private readonly IKhuonBeService _svc;
    private readonly IDanhMucService _dmSvc;
    public KhuonBeController(IKhuonBeService svc, IDanhMucService dmSvc) { _svc=svc; _dmSvc=dmSvc; }

    public async Task<IActionResult> Index(string? trangThai)
    {
        var synced = await _svc.DongBoTuDanhMucVatTuAsync();
        if (synced > 0) TempData["Success"] = $"Đã tự đồng bộ {synced} khuôn bế từ danh mục vật tư.";
        ViewBag.TrangThai = trangThai;
        return View(await _svc.GetAllAsync(trangThai));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var kb = await _svc.GetByIdAsync(id); if (kb == null) return NotFound();
        var lichSu = await _svc.GetLichSuAsync(id);
        ViewBag.LichSu = lichSu;
        return View(kb);
    }

    [HttpGet] public async Task<IActionResult> Form(int id = 0)
    {
        var vm = new KhuonBeVM
        {
            TrangThai = "dang_dung",
            MaKhuon = id == 0 ? await _dmSvc.GenerateNextMaVatTuAsync() : "",
            VatTuKhuonBeList = await _dmSvc.GetVatTuSelectListAsync("khuon_be"),
            NccList = await _dmSvc.GetNccSelectListAsync(),
            PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync()
        };
        if (id > 0)
        {
            var e = await _svc.GetByIdAsync(id); if (e == null) return NotFound();
            vm.Id=e.Id; vm.IdVatTu=e.IdVatTu; vm.TenKhuon=e.TenKhuon; vm.MaKhuon=e.MaKhuon;
            vm.IdNhaCungCap=e.IdNhaCungCap; vm.IdPhieuSpDau=e.IdPhieuSpDau;
            vm.NgayBatDau=e.NgayBatDau; vm.TrangThai=e.TrangThai;
            vm.DinhMucTuoiTho=e.DinhMucTuoiTho; vm.GhiChu=e.GhiChu;
        }
        return PartialView("_KhuonBeForm", vm);
    }

    [HttpPost] public async Task<IActionResult> Save(KhuonBeVM vm)
    {
        if (vm.Id == 0 && string.IsNullOrWhiteSpace(vm.MaKhuon))
        {
            vm.MaKhuon = await _dmSvc.GenerateNextMaVatTuAsync();
            ModelState.Remove(nameof(vm.MaKhuon));
        }

        vm.NccList = await _dmSvc.GetNccSelectListAsync();
        vm.PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync();
        vm.VatTuKhuonBeList = await _dmSvc.GetVatTuSelectListAsync("khuon_be");
        if (!ModelState.IsValid) return PartialView("_KhuonBeForm", vm);
        if (vm.Id == 0) await _svc.CreateAsync(vm); else await _svc.UpdateAsync(vm);
        return Json(new { ok = true });
    }

    [HttpPost] public async Task<IActionResult> TaoVersionThayThe(int id)
    {
        try
        {
            var newId = await _svc.TaoVersionThayTheAsync(id);
            return Json(new { ok = true, id = newId });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok = false, msg = ex.Message });
        }
    }

    [HttpPost] public async Task<IActionResult> GhiNhanSuDung(KhuonBeLichSuVM vm)
    {
        await _svc.GhiNhanSuDungAsync(vm);
        return Json(new { ok = true });
    }

    [HttpPost] public async Task<IActionResult> DanhDauHong(int id, DateTime ngayHong)
    {
        await _svc.DanhDauHongAsync(id, ngayHong);
        return Json(new { ok = true });
    }
}
