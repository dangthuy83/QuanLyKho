using Microsoft.AspNetCore.Mvc;
using KhoQuanLy.Services;

namespace KhoQuanLy.Controllers;

public class BaoCaoController : Controller
{
    private readonly IBaoCaoService _svc;
    public BaoCaoController(IBaoCaoService svc) => _svc = svc;

    public IActionResult Index() => View();

    public async Task<IActionResult> NhapXuat(DateTime? tuNgay, DateTime? denNgay)
    {
        tuNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        denNgay ??= DateTime.Today;
        return View(await _svc.GetNhapXuatAsync(tuNgay.Value, denNgay.Value));
    }

    public async Task<IActionResult> TieuHao(int? nam)
    {
        nam ??= DateTime.Today.Year;
        return View(await _svc.GetTieuHaoAsync(nam.Value));
    }

    public async Task<IActionResult> KhuonBeTheoNcc()
    {
        return View(await _svc.GetKhuonBeTheoNccAsync());
    }

    public async Task<IActionResult> ExportNhapXuat(DateTime? tuNgay, DateTime? denNgay)
    {
        tuNgay ??= new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        denNgay ??= DateTime.Today;
        var wb = await _svc.ExportNhapXuatExcelAsync(tuNgay.Value, denNgay.Value);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoNhapXuat_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> ExportTieuHao(int? nam)
    {
        nam ??= DateTime.Today.Year;
        var wb = await _svc.ExportTieuHaoExcelAsync(nam.Value);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoTieuHao_{nam}.xlsx");
    }

    public async Task<IActionResult> ExportKhuonBeTheoNcc()
    {
        var wb = await _svc.ExportKhuonBeTheoNccExcelAsync();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoKhuonBeTheoNcc_{DateTime.Today:yyyyMMdd}.xlsx");
    }
}
