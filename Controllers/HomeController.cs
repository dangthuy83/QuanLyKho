using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class HomeController : Controller
{
    private readonly ITonKhoService _tonSvc;
    private readonly IPhieuSPService _pspSvc;
    private readonly IKhuonBeService _kbSvc;
    private readonly IConfigSettingsService _cfgSvc;

    public HomeController(ITonKhoService tonSvc, IPhieuSPService pspSvc,
        IKhuonBeService kbSvc, IConfigSettingsService cfgSvc)
    {
        _tonSvc = tonSvc; _pspSvc = pspSvc; _kbSvc = kbSvc;
        _cfgSvc = cfgSvc;
    }

    public async Task<IActionResult> Index()
    {
        var tonKho = await _tonSvc.GetTonKhoAsync();
        var recent = await _tonSvc.GetRecentAsync();
        var phieuSP = await _pspSvc.GetAllAsync("hoan_thanh");
        var khuonBe = await _kbSvc.GetAllAsync("dang_dung");
        var vm = new DashboardVM
        {
            TongVatTu = tonKho.DanhSach.Count,
            VatTuTonThap = tonKho.CanhBaoTonThap.Count,
            PhieuSpHoanThanh = phieuSP.Count(),
            KhuonBeDangDung = khuonBe.Count(),
            GiaoDichGanDay = recent.ToList()
        };
        return View(vm);
    }

    public IActionResult Settings()
    {
        var vm = new SettingsVM
        {
            PspPdfPath = _cfgSvc.GetPspPdfPath(),
            ActualPath = _cfgSvc.GetActualPspPdfPath()
        };
        return View(vm);
    }

    [HttpPost]
    public IActionResult Settings(SettingsVM vm)
    {
        if (!ModelState.IsValid)
        {
            vm.ActualPath = _cfgSvc.GetActualPspPdfPath();
            return View(vm);
        }

        try
        {
            _cfgSvc.SavePspPdfPath(vm.PspPdfPath?.Trim() ?? "");
            TempData["Success"] = "Đã lưu cấu hình đường dẫn lưu file PDF.";
            return RedirectToAction("Settings");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Lỗi khi lưu cấu hình: {ex.Message}");
            vm.ActualPath = _cfgSvc.GetActualPspPdfPath();
            return View(vm);
        }
    }
}
