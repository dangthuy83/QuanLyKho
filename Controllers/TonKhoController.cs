using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class TonKhoController : Controller
{
    private readonly ITonKhoService _svc;
    public TonKhoController(ITonKhoService svc) => _svc = svc;

    public async Task<IActionResult> Index(string? loai, string? search)
        => View(await _svc.GetTonKhoAsync(loai, search));

    public async Task<IActionResult> LichSu(int id, DateTime? tuNgay, DateTime? denNgay)
        => View(await _svc.GetLichSuAsync(id, tuNgay, denNgay));

    [HttpGet] public async Task<IActionResult> XuatKhoNhanhForm(int id)
    {
        var vt = await _svc.GetVatTuByIdAsync(id);
        if (vt == null) return NotFound();
        return PartialView("_XuatKhoNhanhForm", vt);
    }

    [HttpPost] public async Task<IActionResult> XuatKhoNhanh(int idVatTu, decimal soLuong, string lyDo)
    {
        var (ok, msg) = await _svc.XuatKhoNhanhAsync(idVatTu, soLuong, lyDo);
        return Json(new { ok, msg });
    }
}
