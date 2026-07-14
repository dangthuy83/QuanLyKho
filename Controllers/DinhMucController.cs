using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class DinhMucController : Controller
{
    private readonly IDinhMucService _svc;
    private readonly IDanhMucService _dmSvc;
    public DinhMucController(IDinhMucService svc, IDanhMucService dmSvc) { _svc=svc; _dmSvc=dmSvc; }

    public async Task<IActionResult> Index() => View(await _svc.GetAllDinhMucAsync());

    [HttpGet] public async Task<IActionResult> DinhMucForm(int id = 0)
    {
        var vm = new DinhMucVM { IsActive=true, VatTuList=await _dmSvc.GetVatTuSelectListAsync() };
        if (id > 0)
        {
            var e = await _svc.GetDinhMucByIdAsync(id); if (e == null) return NotFound();
            vm.Id=e.Id; vm.IdVatTu=e.IdVatTu; vm.LoaiDinhMuc=e.LoaiDinhMuc;
            vm.TenSanPham=e.TenSanPham; vm.DinhMucTren1000To=e.DinhMucTren1000To;
            vm.DonViTinh=e.DonViTinh; vm.GhiChu=e.GhiChu; vm.IsActive=e.IsActive;
        }
        return PartialView("_DinhMucForm", vm);
    }

    [HttpPost] public async Task<IActionResult> DinhMucSave(DinhMucVM vm)
    {
        vm.VatTuList = await _dmSvc.GetVatTuSelectListAsync();
        if (!ModelState.IsValid) return PartialView("_DinhMucForm", vm);
        await _svc.SaveDinhMucAsync(vm); return Json(new { ok=true });
    }

    [HttpPost] public async Task<IActionResult> DinhMucDelete(int id)
    { await _svc.DeleteDinhMucAsync(id); return Json(new { ok=true }); }

    public async Task<IActionResult> TieuHao(int? nam)
    {
        nam ??= DateTime.Today.Year;
        ViewBag.Nam = nam;
        return View(await _svc.GetTieuHaoAsync(nam));
    }

    [HttpGet] public async Task<IActionResult> TieuHaoForm()
    {
        var vm = new TieuHaoVM
        {
            ThangInThucTe = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
            VatTuList = await _dmSvc.GetVatTuSelectListAsync(),
            PhieuXkList = await _dmSvc.GetPhieuXkSelectListAsync()
        };
        return PartialView("_TieuHaoForm", vm);
    }

    [HttpPost] public async Task<IActionResult> TieuHaoSave(TieuHaoVM vm)
    {
        vm.VatTuList = await _dmSvc.GetVatTuSelectListAsync();
        vm.PhieuXkList = await _dmSvc.GetPhieuXkSelectListAsync();
        if (!ModelState.IsValid) return PartialView("_TieuHaoForm", vm);
        try
        {
            await _svc.SaveTieuHaoAsync(vm); return Json(new { ok=true });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { ok=false, msg=ex.Message });
        }
    }

    [HttpPost] public async Task<IActionResult> TieuHaoDelete(int id)
    { await _svc.DeleteTieuHaoAsync(id); return Json(new { ok=true }); }
}
