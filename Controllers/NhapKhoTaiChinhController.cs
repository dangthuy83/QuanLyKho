using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.Entities;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class NhapKhoTaiChinhController : Controller
{
    private readonly IPhieuXKTCService _svc;
    private readonly IDanhMucService _dmSvc;

    public NhapKhoTaiChinhController(IPhieuXKTCService svc, IDanhMucService dmSvc)
    { _svc=svc; _dmSvc=dmSvc; }

    public async Task<IActionResult> Index() => View(await _svc.GetAllAsync());

    public async Task<IActionResult> Detail(int id)
    {
        var vm = await _svc.GetDetailAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpGet] public async Task<IActionResult> Create()
    {
        var vm = new PhieuXKTCVM
        {
            NgayXuat = DateTime.Today,
            BoPhanList = await _dmSvc.GetBoPhanSelectListAsync()
        };
        ViewBag.VatTuList = (await _dmSvc.GetVatTuAsync()).Where(x => x.IsActive).ToList();
        ViewBag.PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync();
        ViewBag.NccList = await _dmSvc.GetNccSelectListAsync();
        return View(vm);
    }

    [HttpPost] public async Task<IActionResult> Create(PhieuXKTCVM vm,
        [FromForm] List<int?> idVatTu, [FromForm] List<int?> idPhieuSp, [FromForm] List<int?> idNhaCungCap,
        [FromForm] List<string?> moTa, [FromForm] List<decimal> slChungTu,
        [FromForm] List<decimal> slThucTe, [FromForm] List<string?> dvt)
    {
        vm.BoPhanList = await _dmSvc.GetBoPhanSelectListAsync();
        ViewBag.VatTuList = (await _dmSvc.GetVatTuAsync()).Where(x => x.IsActive).ToList();
        ViewBag.PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync();
        ViewBag.NccList = await _dmSvc.GetNccSelectListAsync();
        if (!ModelState.IsValid) return View(vm);
        var phieu = new PhieuXKTC
        {
            SoPhieu=vm.SoPhieu, NgayXuat=vm.NgayXuat,
            BoPhanNhan=vm.BoPhanNhan, LyDoXuat=vm.LyDoXuat
        };
        var chiTiet = new List<PhieuXKTCCt>();
        for (int i = 0; i < slChungTu.Count; i++)
        {
            if (slChungTu[i] <= 0) continue;
            chiTiet.Add(new PhieuXKTCCt
            {
                IdVatTu = idVatTu.ElementAtOrDefault(i),
                IdPhieuSp = idPhieuSp.ElementAtOrDefault(i),
                IdNhaCungCap = idNhaCungCap.ElementAtOrDefault(i),
                MoTaVatTu = moTa.ElementAtOrDefault(i),
                SoLuongChungTu = slChungTu[i],
                SoLuongThucTe = slThucTe.ElementAtOrDefault(i),
                DonViTinh = dvt.ElementAtOrDefault(i)
            });
        }
        var id = await _svc.CreateAsync(phieu, chiTiet);
        TempData["Success"] = "Lưu phiếu nhập kho tài chính thành công!";
        return RedirectToAction("Detail", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetVatTuOptions()
    {
        var items = (await _dmSvc.GetVatTuAsync())
            .Where(x => x.IsActive)
            .Select(x => new { id = x.Id, maVt = x.MaVt, tenVt = x.TenVt, donViTinh = x.DonViTinh })
            .ToList();
        return Json(items);
    }

    [HttpPost] public async Task<IActionResult> BoQua(int idCt, string lyDo)
    {
        await _svc.XacNhanBoQuaAsync(idCt, lyDo);
        return Json(new { ok = true });
    }
}