using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class GiayTietKiemController : Controller
{
    private readonly IGiayTietKiemService _svc;
    private readonly IDanhMucService _dmSvc;
    public GiayTietKiemController(IGiayTietKiemService svc, IDanhMucService dmSvc) { _svc=svc; _dmSvc=dmSvc; }

    public async Task<IActionResult> Index() => View(await _svc.GetAllAsync());

    [HttpGet] public async Task<IActionResult> Form(int id = 0)
    {
        var vm = new GiayTietKiemVM
        {
            PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync(),
            VatTuCuonList = await _dmSvc.GetVatTuSelectListAsync("giay_cuon")
        };
        if (id > 0)
        {
            var e = await _svc.GetByIdAsync(id); if (e == null) return NotFound();
            vm.Id=e.Id; vm.IdPhieuSp=e.IdPhieuSp; vm.IdVatTuCuon=e.IdVatTuCuon;
            vm.KgYeuCau=e.KgYeuCau; vm.KgThucXa=e.KgThucXa;
            vm.TenThanhPhamBu=e.TenThanhPhamBu; vm.SoBat=(int?)e.SoBat;
            vm.SoToQuyDoi=e.SoToQuyDoi; vm.GhiChu=e.GhiChu;
        }
        return PartialView("_GiayTietKiemForm", vm);
    }

    [HttpPost] public async Task<IActionResult> Save(GiayTietKiemVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync();
            vm.VatTuCuonList = await _dmSvc.GetVatTuSelectListAsync("giay_cuon");
            return PartialView("_GiayTietKiemForm", vm);
        }
        if (vm.Id == 0) await _svc.CreateAsync(vm); else await _svc.UpdateAsync(vm);
        return Json(new { ok = true });
    }

    [HttpPost] public async Task<IActionResult> Delete(int id)
    { await _svc.DeleteAsync(id); return Json(new { ok = true }); }
}
