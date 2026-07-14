using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class TraLaiKhoController : Controller
{
    private readonly ITraLaiKhoService _svc;
    private readonly IDanhMucService _dmSvc;

    public TraLaiKhoController(ITraLaiKhoService svc, IDanhMucService dmSvc)
    { _svc=svc; _dmSvc=dmSvc; }

    public async Task<IActionResult> Index() => View(await _svc.GetAllAsync());

    public async Task<IActionResult> Detail(int id)
    {
        var result = await _svc.GetDetailAsync(id);
        if (result == null) return NotFound();
        return View(result.Value);
    }

    [HttpGet] public async Task<IActionResult> Create()
    {
        ViewBag.VatTuList = await _dmSvc.GetVatTuSelectListAsync();
        ViewBag.PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync();
        return View(new PhieuTraLaiVM { NgayTra = DateTime.Today });
    }

    [HttpPost] public async Task<IActionResult> Create(PhieuTraLaiVM vm)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.VatTuList = await _dmSvc.GetVatTuSelectListAsync();
            ViewBag.PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync();
            return View(vm);
        }
        try
        {
            var id = await _svc.CreateAsync(vm);
            TempData["Success"] = "Lưu phiếu trả lại kho thành công!";
            return RedirectToAction("Detail", new { id });
        }
        catch (InvalidOperationException ex)
        {
            ViewBag.VatTuList = await _dmSvc.GetVatTuSelectListAsync();
            ViewBag.PhieuSpList = await _dmSvc.GetPhieuSpSelectListAsync();
            ModelState.AddModelError("", ex.Message);
            return View(vm);
        }
    }
}