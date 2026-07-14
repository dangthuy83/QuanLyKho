using KhoQuanLy.Models.Entities;
using KhoQuanLy.Models.ViewModels;
using KhoQuanLy.Repositories;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;
using MySqlConnector;

namespace KhoQuanLy.Services;

// ── Danh Mục ─────────────────────────────────────────────────
public interface IDanhMucService
{
    Task<IEnumerable<NhomVatTu>> GetNhomVatTuAsync();
    Task<NhomVatTu?> GetNhomByIdAsync(int id);
    Task SaveNhomAsync(NhomVatTuVM vm);
    Task DeleteNhomAsync(int id);
    Task<IEnumerable<VatTu>> GetVatTuAsync();
    Task<VatTu?> GetVatTuByIdAsync(int id);
    Task<IEnumerable<VatTu>> GetVatTuByLoaiAsync(string loai);
    Task<string> GenerateNextMaVatTuAsync();
    Task SaveVatTuAsync(VatTuVM vm);
    Task SetVatTuActiveAsync(int id, bool isActive);
    Task<IEnumerable<NhaCungCap>> GetNhaCungCapAsync();
    Task<NhaCungCap?> GetNccByIdAsync(int id);
    Task SaveNccAsync(NhaCungCapVM vm);
    Task SetNccActiveAsync(int id, bool isActive);
    Task<IEnumerable<BoPhan>> GetBoPhanAsync();
    Task<BoPhan?> GetBoPhanByIdAsync(int id);
    Task SaveBoPhanAsync(BoPhanVM vm);
    Task SetBoPhanActiveAsync(int id, bool isActive);
    Task<IEnumerable<SelectListItem>> GetNhomSelectListAsync();
    Task<IEnumerable<SelectListItem>> GetVatTuSelectListAsync(string? loai = null);
    Task<IEnumerable<SelectListItem>> GetNccSelectListAsync();
    Task<IEnumerable<SelectListItem>> GetBoPhanSelectListAsync();
    Task<IEnumerable<SelectListItem>> GetPhieuSpSelectListAsync();
    Task<IEnumerable<SelectListItem>> GetPhieuXkSelectListAsync();
}

public class DanhMucService : IDanhMucService
{
    private readonly INhomVatTuRepository _nhom;
    private readonly IVatTuRepository _vt;
    private readonly INhaCungCapRepository _ncc;
    private readonly IBoPhanRepository _bp;
    private readonly IPhieuSPRepository _psp;
    private readonly IPhieuXKTCRepository _xk;

    public DanhMucService(INhomVatTuRepository nhom, IVatTuRepository vt,
        INhaCungCapRepository ncc, IBoPhanRepository bp,
        IPhieuSPRepository psp, IPhieuXKTCRepository xk)
    {
        _nhom = nhom; _vt = vt; _ncc = ncc; _bp = bp; _psp = psp; _xk = xk;
    }

    public Task<IEnumerable<NhomVatTu>> GetNhomVatTuAsync() => _nhom.GetAllAsync();
    public Task<NhomVatTu?> GetNhomByIdAsync(int id) => _nhom.GetByIdAsync(id);
    public async Task SaveNhomAsync(NhomVatTuVM vm)
    {
        var e = new NhomVatTu { Id = vm.Id, TenNhom = vm.TenNhom, MoTa = vm.MoTa };
        if (vm.Id == 0) await _nhom.CreateAsync(e); else await _nhom.UpdateAsync(e);
    }
    public Task DeleteNhomAsync(int id) => _nhom.DeleteAsync(id);

    public Task<IEnumerable<VatTu>> GetVatTuAsync() => _vt.GetAllAsync();
    public Task<VatTu?> GetVatTuByIdAsync(int id) => _vt.GetByIdAsync(id);
    public Task<IEnumerable<VatTu>> GetVatTuByLoaiAsync(string loai) => _vt.GetByLoaiAsync(loai);
    public async Task<string> GenerateNextMaVatTuAsync()
    {
        var maxNumber = await _vt.GetMaxMaVtNumberAsync();
        return $"VT{maxNumber + 1:00000}";
    }
    public async Task SaveVatTuAsync(VatTuVM vm)
    {
        if (vm.Id == 0 && string.IsNullOrWhiteSpace(vm.MaVt))
            vm.MaVt = await GenerateNextMaVatTuAsync();

        var e = new VatTu { Id=vm.Id,MaVt=vm.MaVt,TenVt=vm.TenVt,IdNhom=vm.IdNhom,DonViTinh=vm.DonViTinh,
            LoaiVatTu=vm.LoaiVatTu,LaVatTuDichDanh=vm.LaVatTuDichDanh,CanTheoDoiTieuHao=vm.CanTheoDoiTieuHao,
            TonKhoToiThieu=vm.TonKhoToiThieu,IsActive=vm.IsActive };
        if (vm.Id == 0) await _vt.CreateAsync(e); else await _vt.UpdateAsync(e);
    }
    public Task SetVatTuActiveAsync(int id, bool isActive) => _vt.SetActiveAsync(id, isActive);

    public Task<IEnumerable<NhaCungCap>> GetNhaCungCapAsync() => _ncc.GetAllAsync();
    public Task<NhaCungCap?> GetNccByIdAsync(int id) => _ncc.GetByIdAsync(id);
    public async Task SaveNccAsync(NhaCungCapVM vm)
    {
        var e = new NhaCungCap { Id=vm.Id,TenNcc=vm.TenNcc,DienThoai=vm.DienThoai,DiaChiNcc=vm.DiaChiNcc,GhiChu=vm.GhiChu,IsActive=vm.IsActive };
        if (vm.Id == 0) await _ncc.CreateAsync(e); else await _ncc.UpdateAsync(e);
    }
    public Task SetNccActiveAsync(int id, bool isActive) => _ncc.SetActiveAsync(id, isActive);

    public Task<IEnumerable<BoPhan>> GetBoPhanAsync() => _bp.GetAllAsync();
    public Task<BoPhan?> GetBoPhanByIdAsync(int id) => _bp.GetByIdAsync(id);
    public async Task SaveBoPhanAsync(BoPhanVM vm)
    {
        var e = new BoPhan { Id=vm.Id,TenBoPhan=vm.TenBoPhan,GhiChu=vm.GhiChu,IsActive=vm.IsActive };
        if (vm.Id == 0) await _bp.CreateAsync(e); else await _bp.UpdateAsync(e);
    }
    public Task SetBoPhanActiveAsync(int id, bool isActive) => _bp.SetActiveAsync(id, isActive);

    public async Task<IEnumerable<SelectListItem>> GetNhomSelectListAsync()
        => (await _nhom.GetAllAsync()).Select(x => new SelectListItem(x.TenNhom, x.Id.ToString()));
    public async Task<IEnumerable<SelectListItem>> GetVatTuSelectListAsync(string? loai = null)
    {
        var list = loai == null ? await _vt.GetAllAsync() : await _vt.GetByLoaiAsync(loai);
        return list.Where(x => x.IsActive).Select(x => new SelectListItem($"{x.MaVt} - {x.TenVt}", x.Id.ToString()));
    }
    public async Task<IEnumerable<SelectListItem>> GetNccSelectListAsync()
        => (await _ncc.GetAllAsync()).Where(x => x.IsActive).Select(x => new SelectListItem(x.TenNcc, x.Id.ToString()));
    public async Task<IEnumerable<SelectListItem>> GetBoPhanSelectListAsync()
        => (await _bp.GetAllAsync()).Where(x => x.IsActive).Select(x => new SelectListItem(x.TenBoPhan, x.Id.ToString()));
    public async Task<IEnumerable<SelectListItem>> GetPhieuSpSelectListAsync()
        => (await _psp.GetAllAsync()).Select(x => new SelectListItem($"{x.SoPhieu} - {x.TenSanPham}", x.Id.ToString()));
    public async Task<IEnumerable<SelectListItem>> GetPhieuXkSelectListAsync()
        => (await _xk.GetAllAsync()).Select(x => new SelectListItem($"{x.SoPhieu} ({x.NgayXuat:dd/MM/yy})", x.Id.ToString()));
}

// ── Phiếu SP ─────────────────────────────────────────────────
public interface IPhieuSPService
{
    Task<IEnumerable<PhieuSP>> GetAllAsync(string? trangThai = null, string? search = null);
    Task<PhieuSPDetailVM?> GetDetailAsync(int id);
    Task<int> CreateAsync(PhieuSP phieu, List<PhieuSPVatTu> vatTus);
    Task<string> ImportSuaPhieuAsync(PhieuSP phieuMoi, List<PhieuSPVatTu> vatTusMoi, string filePdfPath, string? nguoiSua, string? tenFile);
    Task<PhieuSP?> GetBySoPhieuAsync(string soPhieu);
    Task<int> AutoLienKetVatTuChuaCoDanhMucAsync();
    Task UpdateTrangThaiAsync(int id, string trangThai);
    Task UpdateVatTuCtAsync(PhieuSPVatTuEditVM vm);
    Task XacNhanNhanAsync(int idVatTuCt, decimal soLuong, string trangThai, string? ghiChu);
}

public class PhieuSPService : IPhieuSPService
{
    private const int LichSuLyDoMaxLength = 200;
    private readonly IPhieuSPRepository _repo;
    private readonly IVatTuRepository _vtRepo;
    private readonly IUnitOfWork _uow;
    public PhieuSPService(IPhieuSPRepository repo, IVatTuRepository vtRepo, IUnitOfWork uow) { _repo = repo; _vtRepo = vtRepo; _uow = uow; }

    public Task<IEnumerable<PhieuSP>> GetAllAsync(string? trangThai = null, string? search = null)
        => _repo.GetAllAsync(trangThai, search);

    public Task<PhieuSP?> GetBySoPhieuAsync(string soPhieu)
        => _repo.GetBySoPhieuAsync(soPhieu);

    public async Task<int> AutoLienKetVatTuChuaCoDanhMucAsync()
    {
        var danhMucVatTu = (await _vtRepo.GetAllAsync()).Where(x => x.IsActive).ToList();
        if (danhMucVatTu.Count == 0) return 0;

        var updated = 0;
        var phieuList = (await _repo.GetAllAsync()).ToList();

        _uow.Begin();
        try
        {
            foreach (var phieu in phieuList)
            {
                var vatTus = (await _repo.GetVatTuByPhieuAsync(phieu.Id, _uow.Transaction)).ToList();
                foreach (var vtCt in vatTus.Where(x => x.IdVatTu == null && !string.IsNullOrWhiteSpace(x.ChungLoaiText)))
                {
                    var matched = danhMucVatTu.FirstOrDefault(v => IsSameMaterialName(v.TenVt, vtCt.ChungLoaiText))
                        ?? danhMucVatTu.FirstOrDefault(v => IsNearMaterialName(v.TenVt, vtCt.ChungLoaiText));
                    if (matched == null) continue;

                    vtCt.IdVatTu = matched.Id;
                    vtCt.DonViTinh = string.IsNullOrWhiteSpace(matched.DonViTinh) ? vtCt.DonViTinh : matched.DonViTinh;
                    await _repo.UpdateVatTuCtAsync(vtCt, _uow.Transaction);
                    updated++;
                }
            }

            _uow.Commit();
            return updated;
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    public async Task<PhieuSPDetailVM?> GetDetailAsync(int id)
    {
        var phieu = await _repo.GetByIdAsync(id);
        if (phieu == null) return null;
        return new PhieuSPDetailVM
        {
            PhieuSP = phieu,
            VatTus = (await _repo.GetVatTuByPhieuAsync(id)).ToList(),
            LichSuSua = (await _repo.GetLichSuSuaAsync(id)).ToList(),
            VatTuCuonList = (await _vtRepo.GetByLoaiAsync("giay_cuon"))
                .Where(x => x.IsActive)
                .Select(x => new SelectListItem($"{x.MaVt} - {x.TenVt}", x.Id.ToString()))
        };
    }

    public async Task<int> CreateAsync(PhieuSP phieu, List<PhieuSPVatTu> vatTus)
    {
        _uow.Begin();
        try
        {
            var id = await _repo.CreateAsync(phieu, _uow.Transaction);
            foreach (var vt in vatTus)
            {
                vt.IdPhieuSp = id;
                if (!vt.LaCuonThayToRoi) vt.IdVatTuThayThe = null;
                if (vt.LaCuonThayToRoi && vt.IdVatTuThayThe == null)
                    throw new InvalidOperationException($"Dòng vật tư {DescribeVatTu(vt)} đang tick cấp cuộn thay tờ rời nhưng chưa chọn giấy cuộn thay thế.");
                await _repo.CreateVatTuCtAsync(vt, _uow.Transaction);
            }
            _uow.Commit();
            return id;
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    public async Task<string> ImportSuaPhieuAsync(PhieuSP phieuMoi, List<PhieuSPVatTu> vatTusMoi, string filePdfPath, string? nguoiSua, string? tenFile)
    {
        var existing = await _repo.GetBySoPhieuAsync(phieuMoi.SoPhieu);
        if (existing == null)
            throw new InvalidOperationException($"Không tìm thấy phiếu {phieuMoi.SoPhieu} để cập nhật.");

        _uow.Begin();
        try
        {
            var oldVatTus = (await _repo.GetVatTuByPhieuAsync(existing.Id)).ToList();
            var matchedOldIds = new HashSet<int>();
            var changes = new List<string>();

            await _repo.CreateLichSuSuaAsync(new PhieuSPLichSuSua
            {
                IdPhieuSp = existing.Id,
                LyDo = BuildLichSuLyDo($"Import phiếu sửa từ file: {tenFile}. File cũ: {existing.FilePdfPath ?? "(không có)"}; file mới: {filePdfPath}"),
                NguoiSua = nguoiSua
            }, _uow.Transaction);

            existing.SoLsx = phieuMoi.SoLsx;
            existing.TenSanPham = phieuMoi.TenSanPham;
            existing.MaSanPham = phieuMoi.MaSanPham;
            existing.KhachHang = phieuMoi.KhachHang;
            existing.SoLuongSp = phieuMoi.SoLuongSp;
            existing.NgayLenh = phieuMoi.NgayLenh;
            existing.NgayGiaoHang = phieuMoi.NgayGiaoHang;
            existing.FilePdfPath = filePdfPath;
            existing.NguoiTao = phieuMoi.NguoiTao ?? existing.NguoiTao;
            await _repo.UpdateAsync(existing, _uow.Transaction);

            foreach (var newVt in vatTusMoi.Where(x => x.SoLuongYeuCau > 0))
            {
                var old = oldVatTus.FirstOrDefault(x => !matchedOldIds.Contains(x.Id) && IsSameVatTuLine(x, newVt));
                if (old == null)
                {
                    newVt.IdPhieuSp = existing.Id;
                    newVt.SoLuongDaNhan = 0;
                    newVt.TrangThaiNhan = "chua_nhan";
                    await _repo.CreateVatTuCtAsync(newVt, _uow.Transaction);
                    await _repo.CreateLichSuSuaAsync(new PhieuSPLichSuSua
                    {
                        IdPhieuSp = existing.Id,
                        IdVatTu = newVt.IdVatTu,
                        SoLuongCu = null,
                        SoLuongMoi = newVt.SoLuongYeuCau,
                        LyDo = BuildLichSuLyDo($"Import phiếu sửa: thêm dòng vật tư {DescribeVatTu(newVt)} từ file {tenFile}."),
                        NguoiSua = nguoiSua
                    }, _uow.Transaction);
                    changes.Add($"thêm {DescribeVatTu(newVt)}");
                    continue;
                }

                matchedOldIds.Add(old.Id);
                var oldQty = old.SoLuongYeuCau;
                var oldDesc = DescribeVatTu(old);

                old.IdVatTu = newVt.IdVatTu;
                old.IdVatTuThayThe = newVt.LaCuonThayToRoi ? newVt.IdVatTuThayThe : null;
                old.ChungLoaiText = newVt.ChungLoaiText;
                old.KichThuoc = newVt.KichThuoc;
                old.SoLuongYeuCau = newVt.SoLuongYeuCau;
                old.SoLuongToRoi = newVt.SoLuongToRoi;
                old.DonViTinh = newVt.DonViTinh;
                old.LaCuonThayToRoi = newVt.LaCuonThayToRoi;
                old.GhiChu = newVt.GhiChu;
                old.GhiChuKhongCan = null;
                old.TrangThaiNhan = CalcTrangThaiNhan(old.SoLuongDaNhan, old.SoLuongYeuCau);

                await _repo.UpdateVatTuCtAsync(old, _uow.Transaction);
                if (oldQty != newVt.SoLuongYeuCau || oldDesc != DescribeVatTu(newVt))
                {
                    await _repo.CreateLichSuSuaAsync(new PhieuSPLichSuSua
                    {
                        IdPhieuSp = existing.Id,
                        IdVatTu = old.IdVatTu,
                        SoLuongCu = oldQty,
                        SoLuongMoi = newVt.SoLuongYeuCau,
                        LyDo = BuildLichSuLyDo($"Import phiếu sửa từ file {tenFile}: cập nhật dòng {oldDesc} → {DescribeVatTu(newVt)}; đã nhận {old.SoLuongDaNhan} {old.DonViTinh}."),
                        NguoiSua = nguoiSua
                    }, _uow.Transaction);
                    changes.Add($"cập nhật {DescribeVatTu(newVt)}");
                }
            }

            foreach (var old in oldVatTus.Where(x => !matchedOldIds.Contains(x.Id)))
            {
                var hasPhatSinh = await _repo.HasVatTuCtPhatSinhAsync(old.Id, _uow.Transaction);
                if (!hasPhatSinh)
                {
                    await _repo.CreateLichSuSuaAsync(new PhieuSPLichSuSua
                    {
                        IdPhieuSp = existing.Id,
                        IdVatTu = old.IdVatTu,
                        SoLuongCu = old.SoLuongYeuCau,
                        SoLuongMoi = null,
                        LyDo = BuildLichSuLyDo($"Import phiếu sửa từ file {tenFile}: xóa dòng vật tư cũ không còn trong file mới: {DescribeVatTu(old)}."),
                        NguoiSua = nguoiSua
                    }, _uow.Transaction);
                    await _repo.DeleteVatTuCtAsync(old.Id, _uow.Transaction);
                    changes.Add($"xóa {DescribeVatTu(old)}");
                }
                else
                {
                    old.TrangThaiNhan = "khong_can";
                    old.GhiChuKhongCan = $"Không còn trong phiếu sửa import từ file {tenFile}. Giữ dòng để bảo toàn lịch sử đã phát sinh.";
                    await _repo.UpdateVatTuCtAsync(old, _uow.Transaction);
                    await _repo.CreateLichSuSuaAsync(new PhieuSPLichSuSua
                    {
                        IdPhieuSp = existing.Id,
                        IdVatTu = old.IdVatTu,
                        SoLuongCu = old.SoLuongYeuCau,
                        SoLuongMoi = old.SoLuongYeuCau,
                        LyDo = BuildLichSuLyDo($"Import phiếu sửa từ file {tenFile}: dòng {DescribeVatTu(old)} không còn trong file mới nhưng đã phát sinh nhận/xuất nên giữ lại và đánh dấu không cần nhận tiếp."),
                        NguoiSua = nguoiSua
                    }, _uow.Transaction);
                    changes.Add($"giữ/đánh dấu không cần {DescribeVatTu(old)}");
                }
            }

            await DongBoTrangThaiPhieuAsync(existing.Id);
            _uow.Commit();
            return changes.Count == 0
                ? $"Đã cập nhật phiếu {existing.SoPhieu}; không phát hiện thay đổi dòng vật tư."
                : $"Đã cập nhật phiếu {existing.SoPhieu}: {string.Join("; ", changes)}.";
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    private static bool IsSameVatTuLine(PhieuSPVatTu oldLine, PhieuSPVatTu newLine)
    {
        if (oldLine.IdVatTu.HasValue && newLine.IdVatTu.HasValue)
            return oldLine.IdVatTu.Value == newLine.IdVatTu.Value
                && Normalize(oldLine.KichThuoc) == Normalize(newLine.KichThuoc)
                && Normalize(oldLine.DonViTinh) == Normalize(newLine.DonViTinh);

        return Normalize(oldLine.ChungLoaiText) == Normalize(newLine.ChungLoaiText)
            && Normalize(oldLine.KichThuoc) == Normalize(newLine.KichThuoc)
            && Normalize(oldLine.DonViTinh) == Normalize(newLine.DonViTinh);
    }

    private static string Normalize(string? value)
        => string.Join(" ", (value ?? "").Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static bool IsSameMaterialName(string? catalogName, string? parsedName)
        => NormalizeMaterialName(catalogName) == NormalizeMaterialName(parsedName);

    private static bool IsNearMaterialName(string? catalogName, string? parsedName)
    {
        var c = NormalizeMaterialName(catalogName);
        var p = NormalizeMaterialName(parsedName);
        if (string.IsNullOrWhiteSpace(c) || string.IsNullOrWhiteSpace(p)) return false;
        return c.Contains(p) || p.Contains(c);
    }

    private static string NormalizeMaterialName(string? value)
    {
        var cleaned = value ?? "";
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(Giấy){2,}", "Giấy", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(Màng){2,}", "Màng", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^(Mực){2,}", "Mực", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^Giấy\s+Giấy", "Giấy", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^Màng\s+Màng", "Màng", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^Mực\s+Mực", "Mực", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        cleaned = cleaned.ToLowerInvariant();
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[^\p{L}\p{N}]+", " ");
        return System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private static string DescribeVatTu(PhieuSPVatTu vt)
        => $"{(string.IsNullOrWhiteSpace(vt.TenVt) ? vt.ChungLoaiText : vt.TenVt)} {vt.KichThuoc} SL {vt.SoLuongYeuCau} {vt.DonViTinh}".Trim();

    private static string BuildLichSuLyDo(string value)
    {
        var normalized = string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= LichSuLyDoMaxLength
            ? normalized
            : normalized[..LichSuLyDoMaxLength].Trim();
    }

    private static string CalcTrangThaiNhan(decimal daNhan, decimal yeuCau)
    {
        if (daNhan <= 0) return "chua_nhan";
        return daNhan < yeuCau ? "thieu" : "du";
    }

    public Task UpdateTrangThaiAsync(int id, string trangThai) => _repo.UpdateTrangThaiAsync(id, trangThai);

    public async Task UpdateVatTuCtAsync(PhieuSPVatTuEditVM vm)
    {
        _uow.Begin();
        try
        {
            var old = await _repo.GetVatTuCtByIdAsync(vm.Id);
            if (old == null)
            {
                _uow.Rollback();
                return;
            }
            await _repo.CreateLichSuSuaAsync(new PhieuSPLichSuSua
            {
                IdPhieuSp = old.IdPhieuSp, IdVatTu = old.IdVatTu,
                SoLuongCu = old.SoLuongYeuCau, SoLuongMoi = vm.SoLuongYeuCau,
                LyDo = vm.LyDo, NguoiSua = vm.NguoiSua
            }, _uow.Transaction);
            old.SoLuongYeuCau = vm.SoLuongYeuCau;
            old.LaCuonThayToRoi = vm.LaCuonThayToRoi;
            old.IdVatTuThayThe = vm.LaCuonThayToRoi ? vm.IdVatTuThayThe : null;
            if (old.LaCuonThayToRoi && old.IdVatTuThayThe == null)
                throw new InvalidOperationException("Vui lòng chọn giấy cuộn thay thế khi tick Cấp cuộn thay tờ rời.");
            old.GhiChu = vm.GhiChu;
            old.TrangThaiNhan = CalcTrangThaiNhan(old.SoLuongDaNhan, old.SoLuongYeuCau);
            await _repo.UpdateVatTuCtAsync(old, _uow.Transaction);
            await DongBoTrangThaiPhieuAsync(old.IdPhieuSp);
            _uow.Commit();
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    public async Task XacNhanNhanAsync(int idVatTuCt, decimal soLuong, string trangThai, string? ghiChu)
    {
        _uow.Begin();
        try
        {
            var vtCt = await _repo.GetVatTuCtByIdAsync(idVatTuCt);
            if (vtCt == null)
                throw new InvalidOperationException($"Không tìm thấy dòng vật tư PSP #{idVatTuCt}.");

            await _repo.UpdateTrangThaiNhanAsync(idVatTuCt, soLuong, trangThai, ghiChu, _uow.Transaction);
            await DongBoTrangThaiPhieuAsync(vtCt.IdPhieuSp);
            _uow.Commit();
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    public async Task DongBoTrangThaiPhieuAsync(int idPhieuSp)
    {
        var vatTus = (await _repo.GetVatTuByPhieuAsync(idPhieuSp, _uow.Transaction)).ToList();
        var coDongCanNhan = vatTus.Any(x => x.TrangThaiNhan != "khong_can");
        var daNhanDu = coDongCanNhan && vatTus
            .Where(x => x.TrangThaiNhan != "khong_can")
            .All(x => x.TrangThaiNhan == "du" || x.SoLuongDaNhan >= x.SoLuongYeuCau);

        if (daNhanDu)
            await _repo.UpdateTrangThaiAsync(idPhieuSp, "hoan_thanh", _uow.Transaction);
    }
}

// ── Đợt Đề Nghị ──────────────────────────────────────────────
public interface IDeNghiXuatService
{
    Task<IEnumerable<DotDeNghi>> GetAllAsync();
    Task<DotDeNghiDetailVM?> GetDetailAsync(int id);
    Task<IEnumerable<DotDeNghiCt>> GetPendingFromPreviousDotsAsync();
    Task<int> TaoDotAsync(List<int> idVatTuCtList, List<int> idPendingCtList, string? ghiChu, string? nguoiTao);
    Task UpdateThucNhanAsync(int idCt, decimal soLuong);
    Task HoanThanhDotAsync(int id);
}

public class DeNghiXuatService : IDeNghiXuatService
{
    private readonly IDeNghiXuatRepository _repo;
    private readonly IPhieuSPRepository _pspRepo;
    private readonly IVatTuRepository _vtRepo;
    private readonly ITonKhoRepository _tonKhoRepo;
    private readonly IUnitOfWork _uow;

    public DeNghiXuatService(IDeNghiXuatRepository repo, IPhieuSPRepository pspRepo,
        IVatTuRepository vtRepo, ITonKhoRepository tonKhoRepo, IUnitOfWork uow)
    {
        _repo = repo; _pspRepo = pspRepo;
        _vtRepo = vtRepo; _tonKhoRepo = tonKhoRepo;
        _uow = uow;
    }

    public Task<IEnumerable<DotDeNghi>> GetAllAsync() => _repo.GetAllAsync();

    public Task<IEnumerable<DotDeNghiCt>> GetPendingFromPreviousDotsAsync()
        => _repo.GetPendingFromPreviousDotsAsync();

    public async Task<DotDeNghiDetailVM?> GetDetailAsync(int id)
    {
        var dot = await _repo.GetByIdAsync(id);
        if (dot == null) return null;
        return new DotDeNghiDetailVM
        {
            DotDeNghi = dot,
            ChiTiet = (await _repo.GetChiTietAsync(id)).ToList()
        };
    }

    public async Task<int> TaoDotAsync(List<int> idVatTuCtList, List<int> idPendingCtList, string? ghiChu, string? nguoiTao)
    {
        _uow.Begin();
        try
        {
            var errors = new List<string>();
            var createdCount = 0;
            var maDot = await _repo.GenMaDotAsync();
            var idDot = await _repo.CreateAsync(new DotDeNghi
            {
                MaDot = maDot, NgayTao = DateTime.Today,
                NguoiTao = nguoiTao, TrangThai = "cho_lay", GhiChu = ghiChu
            }, _uow.Transaction);

            var pendingVtIds = new HashSet<int>();
            foreach (var idPendingCt in idPendingCtList.Distinct())
            {
                var oldCt = await _repo.GetChiTietByIdAsync(idPendingCt);
                if (oldCt == null) continue;
                var oldVtCt = await _pspRepo.GetVatTuCtByIdAsync(oldCt.IdPhieuSpVatTu);
                var oldVtThucTeId = GetVatTuThucTeId(oldVtCt);
                if (oldVtCt?.LaCuonThayToRoi == true && oldVtCt.IdVatTuThayThe == null)
                {
                    errors.Add($"Dòng pending PSP {oldCt.SoPhieu} - {oldCt.ChungLoaiText} {oldCt.KichThuoc} đã tick cấp cuộn thay tờ rời nhưng chưa chọn giấy cuộn thay thế.");
                    continue;
                }
                if (oldVtThucTeId == null)
                {
                    errors.Add($"Dòng pending PSP {oldCt.SoPhieu} - {oldCt.ChungLoaiText} {oldCt.KichThuoc} chưa liên kết vật tư thực cấp trong kho_vat_tu.");
                    continue;
                }
                var remaining = (oldCt.SoLuongDeNghi ?? 0) - (oldCt.SoLuongThucNhan ?? 0);
                if (remaining <= 0) continue;

                pendingVtIds.Add(oldCt.IdPhieuSpVatTu);
                var tonKyTruocPending = await _tonKhoRepo.GetTonKyTruocAsync(oldVtThucTeId.Value, DateTime.Now);
                await _repo.CreateChiTietAsync(new DotDeNghiCt
                {
                    IdDot = idDot,
                    IdPhieuSpVatTu = oldCt.IdPhieuSpVatTu,
                    SoLuongDeNghi = remaining,
                    TonKyTruoc = tonKyTruocPending,
                    TrangThai = "chua_lay",
                    /*GhiChu = $"Xuất kho tiếp {oldCt.MaDot} - dòng #{oldCt.Id}"*/
                    GhiChu = $"Xuất kho tiếp {oldCt.MaDot}"
                }, _uow.Transaction);
                createdCount++;
            }

            foreach (var idVtCt in idVatTuCtList.Distinct().Where(id => !pendingVtIds.Contains(id)))
            {
                var vtCt = await _pspRepo.GetVatTuCtByIdAsync(idVtCt);
                if (vtCt == null) continue;
                var vtThucTeId = GetVatTuThucTeId(vtCt);
                if (vtCt.LaCuonThayToRoi && vtCt.IdVatTuThayThe == null)
                {
                    errors.Add($"Dòng PSP #{vtCt.IdPhieuSp} - {DescribeVatTuForError(vtCt)} đã tick cấp cuộn thay tờ rời nhưng chưa chọn giấy cuộn thay thế.");
                    continue;
                }
                if (vtThucTeId == null)
                {
                    errors.Add($"Dòng PSP #{vtCt.IdPhieuSp} - {DescribeVatTuForError(vtCt)} chưa có vật tư thực cấp trong kho_vat_tu. Vui lòng bổ sung/liên kết giấy theo PDF hoặc chọn giấy cuộn thay thế.");
                    continue;
                }
                if (vtCt.TrangThaiNhan == "du" || vtCt.TrangThaiNhan == "khong_can" || vtCt.SoLuongDaNhan >= vtCt.SoLuongYeuCau) continue;
                var tonKyTruoc = await _tonKhoRepo.GetTonKyTruocAsync(vtThucTeId.Value, DateTime.Now);
                var soLuongCanNhan = vtCt.SoLuongYeuCau - vtCt.SoLuongDaNhan - tonKyTruoc;
                if (soLuongCanNhan <= 0) continue;
                await _repo.CreateChiTietAsync(new DotDeNghiCt
                {
                    IdDot = idDot, IdPhieuSpVatTu = idVtCt,
                    SoLuongDeNghi = soLuongCanNhan,
                    TonKyTruoc = tonKyTruoc, TrangThai = "chua_lay"
                }, _uow.Transaction);
                createdCount++;
            }
            if (errors.Count > 0)
                throw new InvalidOperationException(string.Join(" ", errors));
            if (createdCount == 0)
                throw new InvalidOperationException("Không có dòng vật tư hợp lệ để tạo đợt. Vui lòng kiểm tra vật tư đã liên kết kho_vat_tu, trạng thái nhận và tồn kỳ trước.");
            _uow.Commit();
            return idDot;
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    private static string DescribeVatTuForError(PhieuSPVatTu vt)
        => $"{(string.IsNullOrWhiteSpace(vt.TenVt) ? vt.ChungLoaiText : vt.TenVt)} {vt.KichThuoc} SL {vt.SoLuongYeuCau:N3} {vt.DonViTinh}".Trim();

    private static int? GetVatTuThucTeId(PhieuSPVatTu? vt)
        => vt == null ? null : (vt.LaCuonThayToRoi ? vt.IdVatTuThayThe : vt.IdVatTu);

    public async Task UpdateThucNhanAsync(int idCt, decimal soLuong)
    {
        _uow.Begin();
        try
        {
            var ct = await _repo.GetChiTietByIdAsync(idCt);
            if (ct == null)
                throw new InvalidOperationException($"Không tìm thấy dòng chi tiết đề nghị xuất #{idCt}.");
            var soLuongDeNghi = ct.SoLuongDeNghi ?? 0;
            var trangThai = soLuong >= soLuongDeNghi && soLuongDeNghi > 0 ? "du" : "thieu";
            var soLuongCu = ct.SoLuongThucNhan ?? 0;
            var chenhlech = soLuong - soLuongCu;
            await _repo.UpdateThucNhanAsync(idCt, soLuong, trangThai, _uow.Transaction);

            var vtCt = await _pspRepo.GetVatTuCtByIdAsync(ct.IdPhieuSpVatTu);
            if (vtCt != null)
            {
                var tongDaNhan = await _repo.GetTongThucNhanByVatTuCtAsync(ct.IdPhieuSpVatTu, _uow.Transaction);
                vtCt.SoLuongDaNhan = tongDaNhan;
                vtCt.TrangThaiNhan = CalcTrangThaiNhanTheoDeNghi(tongDaNhan, vtCt.SoLuongYeuCau);
                await _pspRepo.UpdateVatTuCtAsync(vtCt, _uow.Transaction);
                await DongBoTrangThaiPhieuAsync(vtCt.IdPhieuSp);
            }

            // Theo góc nhìn phân xưởng, đề nghị xuất giấy là kho tổng xuất cho mình,
            // nên khi nhập thực nhận thì phân xưởng đang NHẬP vật tư về kho nội bộ.
            // Chỉ ghi phần chênh lệch để tránh cộng/trừ lặp khi sửa số thực nhận.
            var vtThucTeId = GetVatTuThucTeId(vtCt);
            if (chenhlech != 0 && vtThucTeId != null)
            {
                var vt = await _vtRepo.GetByIdAsync(vtThucTeId.Value);
                if (vt != null)
                {
                    var tonTruoc = vt.TonKhoHienTai;
                    if (chenhlech < 0 && Math.Abs(chenhlech) > tonTruoc)
                        throw new InvalidOperationException($"Không thể giảm thực nhận {Math.Abs(chenhlech):N3} {vt.DonViTinh} vì tồn kho hiện tại chỉ còn {tonTruoc:N3} {vt.DonViTinh}. Vui lòng kiểm tra các giao dịch xuất/trả/tiêu hao đã phát sinh.");
                    await _vtRepo.UpdateTonKhoAsync(vtThucTeId.Value, chenhlech, _uow.Transaction);
                    await _tonKhoRepo.CreateLichSuAsync(new LichSuTon
                    {
                        IdVatTu = vtThucTeId.Value, NgayGiaoDich = DateTime.Now,
                        LoaiGd = "nhap_de_nghi", SoLuong = chenhlech,
                        TonTruoc = tonTruoc, TonSau = tonTruoc + chenhlech,
                        GhiChu = $"Nhập giấy từ đề nghị xuất - cập nhật thực nhận CT #{idCt}"
                    }, _uow.Transaction);
                }
            }
            _uow.Commit();
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    private static string CalcTrangThaiNhanTheoDeNghi(decimal daNhan, decimal yeuCau)
    {
        if (daNhan <= 0) return "chua_nhan";
        return daNhan < yeuCau ? "thieu" : "du";
    }

    private async Task DongBoTrangThaiPhieuAsync(int idPhieuSp)
    {
        var vatTus = (await _pspRepo.GetVatTuByPhieuAsync(idPhieuSp, _uow.Transaction)).ToList();
        var coDongCanNhan = vatTus.Any(x => x.TrangThaiNhan != "khong_can");
        var daNhanDu = coDongCanNhan && vatTus
            .Where(x => x.TrangThaiNhan != "khong_can")
            .All(x => x.TrangThaiNhan == "du" || x.SoLuongDaNhan >= x.SoLuongYeuCau);

        if (daNhanDu)
            await _pspRepo.UpdateTrangThaiAsync(idPhieuSp, "hoan_thanh", _uow.Transaction);
    }

    public Task HoanThanhDotAsync(int id) => _repo.UpdateTrangThaiAsync(id, "hoan_thanh");
}

// ── Phiếu XK TC ──────────────────────────────────────────────
public interface IPhieuXKTCService
{
    Task<IEnumerable<PhieuXKTC>> GetAllAsync();
    Task<PhieuXKTCDetailVM?> GetDetailAsync(int id);
    Task<int> CreateAsync(PhieuXKTC phieu, List<PhieuXKTCCt> chiTiet);
    Task XacNhanBoQuaAsync(int idCt, string lyDo);
    Task DeleteAsync(int id);
}

public class PhieuXKTCService : IPhieuXKTCService
{
    private readonly IPhieuXKTCRepository _repo;
    private readonly IVatTuRepository _vtRepo;
    private readonly ITonKhoRepository _tonKhoRepo;
    private readonly IUnitOfWork _uow;
    public PhieuXKTCService(IPhieuXKTCRepository repo, IVatTuRepository vtRepo, ITonKhoRepository tonKhoRepo, IUnitOfWork uow)
    {
        _repo = repo; _vtRepo = vtRepo; _tonKhoRepo = tonKhoRepo; _uow = uow;
    }

    public Task<IEnumerable<PhieuXKTC>> GetAllAsync() => _repo.GetAllAsync();

    public async Task<PhieuXKTCDetailVM?> GetDetailAsync(int id)
    {
        var phieu = await _repo.GetByIdAsync(id);
        if (phieu == null) return null;
        return new PhieuXKTCDetailVM
        {
            PhieuXKTC = phieu,
            ChiTiet = (await _repo.GetChiTietAsync(id)).ToList()
        };
    }

    public async Task<int> CreateAsync(PhieuXKTC phieu, List<PhieuXKTCCt> chiTiet)
    {
        _uow.Begin();
        try
        {
            var id = await _repo.CreateAsync(phieu, _uow.Transaction);
            foreach (var ct in chiTiet)
            {
                ct.IdPhieuXkTc = id;
                ct.TrangThai = ct.SoLuongThucTe >= ct.SoLuongChungTu ? "du" : "thieu";
                await _repo.CreateChiTietAsync(ct, _uow.Transaction);
                // Cập nhật tồn kho
                if (ct.IdVatTu.HasValue && ct.SoLuongThucTe > 0)
                {
                    var vt = await _vtRepo.GetByIdAsync(ct.IdVatTu.Value);
                    if (vt != null)
                    {
                        var tonTruoc = vt.TonKhoHienTai;
                        await _vtRepo.UpdateTonKhoAsync(ct.IdVatTu.Value, ct.SoLuongThucTe, _uow.Transaction);
                        await _tonKhoRepo.CreateLichSuAsync(new LichSuTon
                        {
                            IdVatTu = ct.IdVatTu.Value, NgayGiaoDich = phieu.NgayXuat,
                            LoaiGd = "nhap_tc", SoLuong = ct.SoLuongThucTe,
                            TonTruoc = tonTruoc, TonSau = tonTruoc + ct.SoLuongThucTe,
                            GhiChu = $"Nhập từ phiếu XK TC {phieu.SoPhieu}"
                        }, _uow.Transaction);
                    }
                }
            }
            _uow.Commit();
            return id;
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    public Task XacNhanBoQuaAsync(int idCt, string lyDo)
        => _repo.UpdateChiTietTrangThaiAsync(idCt, "da_bo_qua", lyDo);

    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}

// ── Trả Lại Kho ──────────────────────────────────────────────
public interface ITraLaiKhoService
{
    Task<IEnumerable<PhieuTraLai>> GetAllAsync();
    Task<(PhieuTraLai, List<PhieuTraLaiCt>)?> GetDetailAsync(int id);
    Task<int> CreateAsync(PhieuTraLaiVM vm);
}

public class TraLaiKhoService : ITraLaiKhoService
{
    private readonly ITraLaiKhoRepository _repo;
    private readonly IVatTuRepository _vtRepo;
    private readonly ITonKhoRepository _tonKhoRepo;
    private readonly IUnitOfWork _uow;
    public TraLaiKhoService(ITraLaiKhoRepository repo, IVatTuRepository vtRepo, ITonKhoRepository tonKhoRepo, IUnitOfWork uow)
    {
        _repo = repo; _vtRepo = vtRepo; _tonKhoRepo = tonKhoRepo; _uow = uow;
    }

    public Task<IEnumerable<PhieuTraLai>> GetAllAsync() => _repo.GetAllAsync();

    public async Task<(PhieuTraLai, List<PhieuTraLaiCt>)?> GetDetailAsync(int id)
    {
        var phieu = await _repo.GetByIdAsync(id);
        if (phieu == null) return null;
        var ct = (await _repo.GetChiTietAsync(id)).ToList();
        return (phieu, ct);
    }

    public async Task<int> CreateAsync(PhieuTraLaiVM vm)
    {
        _uow.Begin();
        try
        {
            var id = await _repo.CreateAsync(new PhieuTraLai
            {
                SoPhieu = vm.SoPhieu, NgayTra = vm.NgayTra,
                LyDo = vm.LyDo, GhiChu = vm.GhiChu, NguoiTao = vm.NguoiTao
            }, _uow.Transaction);
            foreach (var ct in vm.ChiTiet.Where(x => x.SoLuong > 0))
            {
                await _repo.CreateChiTietAsync(new PhieuTraLaiCt
                {
                    IdPhieuTraLai = id, IdVatTu = ct.IdVatTu,
                    IdPhieuSp = ct.IdPhieuSp, SoLuong = ct.SoLuong, GhiChu = ct.GhiChu
                }, _uow.Transaction);
                var vt = await _vtRepo.GetByIdAsync(ct.IdVatTu);
                if (vt != null)
                {
                    var tonTruoc = vt.TonKhoHienTai;
                    if (ct.SoLuong > tonTruoc)
                        throw new InvalidOperationException($"Không thể trả lại {ct.SoLuong:N3} {vt.DonViTinh} vì tồn kho hiện tại của vật tư {vt.TenVt} chỉ còn {tonTruoc:N3} {vt.DonViTinh}.");
                    await _vtRepo.UpdateTonKhoAsync(ct.IdVatTu, -ct.SoLuong, _uow.Transaction);
                    await _tonKhoRepo.CreateLichSuAsync(new LichSuTon
                        {
                            IdVatTu = ct.IdVatTu, NgayGiaoDich = vm.NgayTra,
                            LoaiGd = "tra_lai", SoLuong = -ct.SoLuong,
                            TonTruoc = tonTruoc, TonSau = tonTruoc - ct.SoLuong,
                            GhiChu = $"Trả lại từ phiếu {vm.SoPhieu} - {vm.LyDo}"
                        }, _uow.Transaction);
                }
            }
            _uow.Commit();
            return id;
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }
}

// ── Giấy Tiết Kiệm ───────────────────────────────────────────
public interface IGiayTietKiemService
{
    Task<IEnumerable<GiayTietKiem>> GetAllAsync();
    Task<GiayTietKiem?> GetByIdAsync(int id);
    Task<int> CreateAsync(GiayTietKiemVM vm);
    Task UpdateAsync(GiayTietKiemVM vm);
    Task DeleteAsync(int id);
}

public class GiayTietKiemService : IGiayTietKiemService
{
    private readonly IGiayTietKiemRepository _repo;
    public GiayTietKiemService(IGiayTietKiemRepository repo) => _repo = repo;
    public Task<IEnumerable<GiayTietKiem>> GetAllAsync() => _repo.GetAllAsync();
    public Task<GiayTietKiem?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
    public Task<int> CreateAsync(GiayTietKiemVM vm)
        => _repo.CreateAsync(new GiayTietKiem { IdPhieuSp=vm.IdPhieuSp ?? 0,IdVatTuCuon=vm.IdVatTuCuon ?? 0,
            KgYeuCau=vm.KgYeuCau ?? 0,KgThucXa=vm.KgThucXa ?? 0,
            KgTietKiem = (vm.KgYeuCau ?? 0) - (vm.KgThucXa ?? 0),
            TenThanhPhamBu=vm.TenThanhPhamBu,
            SoBat=vm.SoBat ?? 0,SoToQuyDoi=vm.SoToQuyDoi ?? 0,GhiChu=vm.GhiChu });
    public Task UpdateAsync(GiayTietKiemVM vm)
        => _repo.UpdateAsync(new GiayTietKiem { Id=vm.Id,IdPhieuSp=vm.IdPhieuSp ?? 0,IdVatTuCuon=vm.IdVatTuCuon ?? 0,
            KgYeuCau=vm.KgYeuCau ?? 0,KgThucXa=vm.KgThucXa ?? 0,
            KgTietKiem = (vm.KgYeuCau ?? 0) - (vm.KgThucXa ?? 0),
            TenThanhPhamBu=vm.TenThanhPhamBu,
            SoBat=vm.SoBat ?? 0,SoToQuyDoi=vm.SoToQuyDoi ?? 0,GhiChu=vm.GhiChu });
    public Task DeleteAsync(int id) => _repo.DeleteAsync(id);
}

// ── Khuôn Bế ─────────────────────────────────────────────────
public interface IKhuonBeService
{
    Task<IEnumerable<KhuonBe>> GetAllAsync(string? trangThai = null);
    Task<KhuonBe?> GetByIdAsync(int id);
    Task<int> DongBoTuDanhMucVatTuAsync();
    Task<int> CreateAsync(KhuonBeVM vm);
    Task UpdateAsync(KhuonBeVM vm);
    Task<int> TaoVersionThayTheAsync(int idKhuonHong);
    Task GhiNhanSuDungAsync(KhuonBeLichSuVM vm);
    Task<IEnumerable<KhuonBeLichSu>> GetLichSuAsync(int idKhuon);
    Task DanhDauHongAsync(int id, DateTime ngayHong);
}

public class KhuonBeService : IKhuonBeService
{
    private readonly IKhuonBeRepository _repo;
    private readonly IVatTuRepository _vtRepo;
    private readonly IUnitOfWork _uow;
    public KhuonBeService(IKhuonBeRepository repo, IVatTuRepository vtRepo, IUnitOfWork uow) { _repo = repo; _vtRepo = vtRepo; _uow = uow; }

    public Task<IEnumerable<KhuonBe>> GetAllAsync(string? trangThai = null) => _repo.GetAllAsync(trangThai);
    public Task<KhuonBe?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<int> DongBoTuDanhMucVatTuAsync()
    {
        var vatTuKhuonBe = (await _vtRepo.GetByLoaiAsync("khuon_be")).Where(x => x.IsActive).ToList();
        if (!vatTuKhuonBe.Any()) return 0;

        var existing = (await _repo.GetAllAsync()).ToList();
        var existingKeys = existing
            .SelectMany(x => new[] { NormalizeKey(x.MaKhuon), NormalizeKey(x.TenKhuon) })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var count = 0;
        foreach (var vt in vatTuKhuonBe)
        {
            var maKey = NormalizeKey(vt.MaVt);
            var tenKey = NormalizeKey(vt.TenVt);
            if (existingKeys.Contains(maKey) || existingKeys.Contains(tenKey)) continue;

            try
            {
                await _repo.CreateAsync(new KhuonBe
                {
                    TenKhuon = vt.TenVt,
                    MaKhuon = vt.MaVt,
                    IdVatTu = vt.Id,
                    PhienBan = 1,
                    TrangThai = "dang_dung",
                    GhiChu = "Tự đồng bộ từ danh mục vật tư loại Khuôn bế"
                });
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Dữ liệu đã tồn tại hoặc có request đồng bộ song song chèn cùng mã khuôn.
                // Bỏ qua để không làm văng màn hình Khuôn Bế.
                existingKeys.Add(maKey);
                existingKeys.Add(tenKey);
                continue;
            }
            existingKeys.Add(maKey);
            existingKeys.Add(tenKey);
            count++;
        }
        return count;
    }

    private static string NormalizeKey(string? value)
        => (value ?? "").Trim().ToLowerInvariant();

    public async Task<int> CreateAsync(KhuonBeVM vm)
    {
        if (string.IsNullOrWhiteSpace(vm.MaKhuon))
            vm.MaKhuon = await GenerateNextMaKhuonAsync();

        var idVatTu = vm.IdVatTu ?? await EnsureVatTuKhuonBeAsync(vm.MaKhuon, vm.TenKhuon);
        return await _repo.CreateAsync(new KhuonBe
        {
            IdVatTu=idVatTu,TenKhuon=vm.TenKhuon,MaKhuon=vm.MaKhuon,PhienBan=1,
            IdNhaCungCap=vm.IdNhaCungCap,IdPhieuSpDau=vm.IdPhieuSpDau,
            NgayBatDau=vm.NgayBatDau,TrangThai=vm.TrangThai,
            DinhMucTuoiTho=vm.DinhMucTuoiTho,GhiChu=vm.GhiChu
        });
    }

    public async Task UpdateAsync(KhuonBeVM vm)
    {
        var e = await _repo.GetByIdAsync(vm.Id);
        if (e == null) return;
        e.IdVatTu=vm.IdVatTu ?? await EnsureVatTuKhuonBeAsync(vm.MaKhuon, vm.TenKhuon);
        e.TenKhuon=vm.TenKhuon; e.MaKhuon=vm.MaKhuon; e.IdNhaCungCap=vm.IdNhaCungCap;
        e.IdPhieuSpDau=vm.IdPhieuSpDau; e.NgayBatDau=vm.NgayBatDau; e.TrangThai=vm.TrangThai;
        e.DinhMucTuoiTho=vm.DinhMucTuoiTho; e.GhiChu=vm.GhiChu;
        await _repo.UpdateAsync(e);
    }

    private async Task<int?> EnsureVatTuKhuonBeAsync(string maKhuon, string tenKhuon)
    {
        var maKey = NormalizeKey(maKhuon);
        var tenKey = NormalizeKey(tenKhuon);
        var existing = (await _vtRepo.GetByLoaiAsync("khuon_be"))
            .FirstOrDefault(x => NormalizeKey(x.MaVt) == maKey || NormalizeKey(x.TenVt) == tenKey);
        if (existing != null) return existing.Id;

        try
        {
            return await _vtRepo.CreateAsync(new VatTu
            {
                MaVt = maKhuon,
                TenVt = tenKhuon,
                DonViTinh = "Cái",
                LoaiVatTu = "khuon_be",
                IsActive = true
            });
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            // Danh mục vật tư đã có mã/tên này do người dùng hoặc request khác vừa tạo.
            return (await _vtRepo.GetByLoaiAsync("khuon_be"))
                .FirstOrDefault(x => NormalizeKey(x.MaVt) == maKey || NormalizeKey(x.TenVt) == tenKey)?.Id;
        }
    }

    private async Task<string> GenerateNextMaKhuonAsync()
    {
        var maxNumber = await _vtRepo.GetMaxMaVtNumberAsync();
        return $"VT{maxNumber + 1:00000}";
    }

    public async Task<int> TaoVersionThayTheAsync(int idKhuonHong)
    {
        var old = await _repo.GetByIdAsync(idKhuonHong);
        if (old == null) throw new InvalidOperationException("Không tìm thấy khuôn cần thay thế.");
        if (old.TrangThai != "hong") throw new InvalidOperationException("Chỉ tạo version thay thế cho khuôn đã đánh dấu hỏng.");

        var nextVersion = Math.Max(old.PhienBan, await _repo.GetMaxPhienBanByVatTuAsync(old.IdVatTu, old.TenKhuon)) + 1;
        var newMaKhuon = BuildReplacementMaKhuon(old.MaKhuon, nextVersion);
        return await _repo.CreateAsync(new KhuonBe
        {
            IdVatTu = old.IdVatTu,
            TenKhuon = old.TenKhuon,
            MaKhuon = newMaKhuon,
            PhienBan = nextVersion,
            IdNhaCungCap = old.IdNhaCungCap,
            IdPhieuSpDau = null,
            NgayBatDau = DateTime.Today,
            TrangThai = "dang_dung",
            DinhMucTuoiTho = old.DinhMucTuoiTho,
            GhiChu = $"Version thay thế cho khuôn hỏng {old.MaKhuon} (v{old.PhienBan})."
        });
    }

    private static string BuildReplacementMaKhuon(string maKhuonCu, int version)
    {
        var baseCode = System.Text.RegularExpressions.Regex.Replace(maKhuonCu ?? "", @"-V\d+$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(baseCode)) baseCode = "KHUON";
        return $"{baseCode}-V{version}";
    }

    public async Task GhiNhanSuDungAsync(KhuonBeLichSuVM vm)
    {
        _uow.Begin();
        try
        {
            await _repo.CreateLichSuAsync(new KhuonBeLichSu
            {
                IdKhuonBe=vm.IdKhuonBe,IdPhieuSp=vm.IdPhieuSp ?? 0,
                SoToIn=vm.SoToIn,NgaySuDung=vm.NgaySuDung,GhiChu=vm.GhiChu
            });
            await _repo.AddSoToInAsync(vm.IdKhuonBe, vm.SoToIn);
            _uow.Commit();
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }

    public Task<IEnumerable<KhuonBeLichSu>> GetLichSuAsync(int idKhuon) => _repo.GetLichSuAsync(idKhuon);
    public async Task DanhDauHongAsync(int id, DateTime ngayHong)
    {
        var e = await _repo.GetByIdAsync(id);
        if (e == null) return;
        e.TrangThai = "hong"; e.NgayHong = ngayHong;
        await _repo.UpdateAsync(e);
    }
}

// ── Định Mức ─────────────────────────────────────────────────
public interface IDinhMucService
{
    Task<IEnumerable<DinhMucTieuHao>> GetAllDinhMucAsync();
    Task<DinhMucTieuHao?> GetDinhMucByIdAsync(int id);
    Task SaveDinhMucAsync(DinhMucVM vm);
    Task DeleteDinhMucAsync(int id);
    Task<IEnumerable<TieuHaoThucTe>> GetTieuHaoAsync(int? nam = null);
    Task SaveTieuHaoAsync(TieuHaoVM vm);
    Task DeleteTieuHaoAsync(int id);
}

public class DinhMucService : IDinhMucService
{
    private readonly IDinhMucRepository _repo;
    private readonly IVatTuRepository _vtRepo;
    private readonly ITonKhoRepository _tonRepo;
    private readonly IUnitOfWork _uow;
    public DinhMucService(IDinhMucRepository repo, IVatTuRepository vtRepo, ITonKhoRepository tonRepo, IUnitOfWork uow)
    { _repo = repo; _vtRepo = vtRepo; _tonRepo = tonRepo; _uow = uow; }
    public Task<IEnumerable<DinhMucTieuHao>> GetAllDinhMucAsync() => _repo.GetAllDinhMucAsync();
    public Task<DinhMucTieuHao?> GetDinhMucByIdAsync(int id) => _repo.GetDinhMucByIdAsync(id);
    public async Task SaveDinhMucAsync(DinhMucVM vm)
    {
        var e = new DinhMucTieuHao { Id=vm.Id,IdVatTu=vm.IdVatTu,LoaiDinhMuc=vm.LoaiDinhMuc ?? "theo_to_in",
            TenSanPham=vm.TenSanPham,DinhMucTren1000To=vm.DinhMucTren1000To,
            DinhMucTheoTenSp=vm.DinhMucTheoTenSp,DonViTinh=vm.DonViTinh,
            GhiChu=vm.GhiChu,IsActive=vm.IsActive };
        if (vm.Id == 0) await _repo.CreateDinhMucAsync(e); else await _repo.UpdateDinhMucAsync(e);
    }
    public Task DeleteDinhMucAsync(int id) => _repo.DeleteDinhMucAsync(id);
    public Task<IEnumerable<TieuHaoThucTe>> GetTieuHaoAsync(int? nam = null) => _repo.GetTieuHaoAsync(nam);
    public async Task SaveTieuHaoAsync(TieuHaoVM vm)
    {
        _uow.Begin();
        try
        {
            await _repo.CreateTieuHaoAsync(new TieuHaoThucTe
            {
                IdVatTu = vm.IdVatTu ?? 0,
                IdPhieuXkTc = vm.IdPhieuXkTc,
                ThangInThucTe = vm.ThangInThucTe ?? DateTime.Today,
                SoToIn = vm.SoToIn ?? 0,
                SoLuongTieuHao = vm.SoLuongTieuHao,
                GhiChu = vm.GhiChu
            }, _uow.Transaction);
            var vt = await _vtRepo.GetByIdAsync(vm.IdVatTu ?? 0);
            if (vt != null)
            {
                var tonTruoc = vt.TonKhoHienTai;
                if (vm.SoLuongTieuHao > tonTruoc)
                    throw new InvalidOperationException($"Không thể ghi tiêu hao {vm.SoLuongTieuHao:N3} {vt.DonViTinh} vì tồn kho hiện tại của vật tư {vt.TenVt} chỉ còn {tonTruoc:N3} {vt.DonViTinh}.");
                await _vtRepo.UpdateTonKhoAsync(vm.IdVatTu ?? 0, -vm.SoLuongTieuHao, _uow.Transaction);
                await _tonRepo.CreateLichSuAsync(new LichSuTon
                {
                    IdVatTu = vm.IdVatTu ?? 0,
                    NgayGiaoDich = vm.ThangInThucTe ?? DateTime.Today,
                    LoaiGd = "xuat_tieu_hao",
                    SoLuong = -vm.SoLuongTieuHao,
                    TonTruoc = tonTruoc,
                    TonSau = tonTruoc - vm.SoLuongTieuHao,
                    GhiChu = $"Tiêu hao thực tế tháng {(vm.ThangInThucTe ?? DateTime.Today):MM/yyyy}"
                }, _uow.Transaction);
            }
            _uow.Commit();
        }
        catch
        {
            _uow.Rollback();
            throw;
        }
    }
    public Task DeleteTieuHaoAsync(int id) => _repo.DeleteTieuHaoAsync(id);
}

// ── Tồn Kho ──────────────────────────────────────────────────
public interface ITonKhoService
{
    Task<TonKhoVM> GetTonKhoAsync(string? filterLoai = null, string? search = null);
    Task<LichSuTonVM> GetLichSuAsync(int idVatTu, DateTime? tuNgay = null, DateTime? denNgay = null);
    Task<IEnumerable<LichSuTon>> GetRecentAsync();
    Task<(bool ok, string msg)> XuatKhoNhanhAsync(int idVatTu, decimal soLuong, string lyDo);
    Task<VatTu?> GetVatTuByIdAsync(int id);
}

public class TonKhoService : ITonKhoService
{
    private readonly IVatTuRepository _vtRepo;
    private readonly ITonKhoRepository _tonRepo;
    private readonly IUnitOfWork _uow;
    public TonKhoService(IVatTuRepository vtRepo, ITonKhoRepository tonRepo, IUnitOfWork uow)
    { _vtRepo = vtRepo; _tonRepo = tonRepo; _uow = uow; }

    public Task<VatTu?> GetVatTuByIdAsync(int id) => _vtRepo.GetByIdAsync(id);

    public async Task<(bool ok, string msg)> XuatKhoNhanhAsync(int idVatTu, decimal soLuong, string lyDo)
    {
        if (soLuong <= 0) return (false, "Số lượng xuất phải lớn hơn 0");
        var vt = await _vtRepo.GetByIdAsync(idVatTu);
        if (vt == null) return (false, "Không tìm thấy vật tư");
        if (soLuong > vt.TonKhoHienTai) return (false, $"Số lượng xuất vượt tồn kho hiện tại ({vt.TonKhoHienTai:N3} {vt.DonViTinh})");
        _uow.Begin();
        try
        {
            var tonTruoc = vt.TonKhoHienTai;
            await _vtRepo.UpdateTonKhoAsync(idVatTu, -soLuong, _uow.Transaction);
            await _tonRepo.CreateLichSuAsync(new LichSuTon
            {
                IdVatTu = idVatTu, NgayGiaoDich = DateTime.Now,
                LoaiGd = "xuat_khac", SoLuong = -soLuong,
                TonTruoc = tonTruoc, TonSau = tonTruoc - soLuong,
                GhiChu = lyDo
            }, _uow.Transaction);
            _uow.Commit();
            return (true, "Đã xuất kho!");
        }
        catch
        {
            _uow.Rollback();
            return (false, "Lỗi xử lý giao dịch, đã rollback.");
        }
    }

    public async Task<TonKhoVM> GetTonKhoAsync(string? filterLoai = null, string? search = null)
    {
        var all = (await _vtRepo.GetAllAsync()).Where(x => x.IsActive).ToList();
        if (!string.IsNullOrEmpty(filterLoai)) all = all.Where(x => x.LoaiVatTu == filterLoai).ToList();
        if (!string.IsNullOrEmpty(search))
            all = all.Where(x => x.TenVt.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || x.MaVt.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        return new TonKhoVM
        {
            DanhSach = all, CanhBaoTonThap = all.Where(x => x.TonKhoToiThieu > 0 && x.TonKhoHienTai < x.TonKhoToiThieu).ToList(),
            FilterLoai = filterLoai, FilterSearch = search
        };
    }

    public async Task<LichSuTonVM> GetLichSuAsync(int idVatTu, DateTime? tuNgay = null, DateTime? denNgay = null)
    {
        var vt = await _vtRepo.GetByIdAsync(idVatTu);
        return new LichSuTonVM
        {
            IdVatTu = idVatTu, TenVt = vt?.TenVt ?? "",
            LichSu = (await _tonRepo.GetLichSuAsync(idVatTu, tuNgay, denNgay)).ToList(),
            TuNgay = tuNgay, DenNgay = denNgay
        };
    }

    public async Task<IEnumerable<LichSuTon>> GetRecentAsync() => await _tonRepo.GetRecentAsync(30);
}

// ── Báo Cáo ──────────────────────────────────────────────────
public interface IBaoCaoService
{
    Task<BaoCaoNhapXuatVM> GetNhapXuatAsync(DateTime tuNgay, DateTime denNgay);
    Task<BaoCaoTieuHaoVM> GetTieuHaoAsync(int nam);
    Task<BaoCaoKhuonBeNccVM> GetKhuonBeTheoNccAsync();
    Task<XLWorkbook> ExportNhapXuatExcelAsync(DateTime tuNgay, DateTime denNgay);
    Task<XLWorkbook> ExportTieuHaoExcelAsync(int nam);
    Task<XLWorkbook> ExportKhuonBeTheoNccExcelAsync();
}

public class BaoCaoService : IBaoCaoService
{
    private readonly IVatTuRepository _vtRepo;
    private readonly ITonKhoRepository _tonRepo;
    private readonly IDinhMucRepository _dinhMucRepo;
    private readonly IKhuonBeRepository _khuonBeRepo;
    public BaoCaoService(IVatTuRepository vtRepo, ITonKhoRepository tonRepo, IDinhMucRepository dinhMucRepo, IKhuonBeRepository khuonBeRepo)
    { _vtRepo = vtRepo; _tonRepo = tonRepo; _dinhMucRepo = dinhMucRepo; _khuonBeRepo = khuonBeRepo; }

    public async Task<BaoCaoNhapXuatVM> GetNhapXuatAsync(DateTime tuNgay, DateTime denNgay)
    {
        var vatTus = (await _vtRepo.GetAllAsync()).Where(x => x.IsActive).ToList();
        var dict = await _tonRepo.GetBaoCaoNhapXuatAsync(tuNgay, denNgay);
        var rows = vatTus.Select(vt =>
        {
            var nhap = dict.GetValueOrDefault($"{vt.Id}_nhap_tc", 0)
                + dict.GetValueOrDefault($"{vt.Id}_nhap_de_nghi", 0);
            var xuatTh = dict.GetValueOrDefault($"{vt.Id}_xuat_tieu_hao", 0);
            var xuatK = dict.GetValueOrDefault($"{vt.Id}_xuat_khac", 0);
            var traLai = dict.GetValueOrDefault($"{vt.Id}_tra_lai", 0);
            var dieuChinh = dict.GetValueOrDefault($"{vt.Id}_dieu_chinh", 0);
            var phatSinh = nhap + xuatTh + xuatK + traLai + dieuChinh;
            return new BaoCaoNhapXuatRow
            {
                TenVt = vt.TenVt, DonViTinh = vt.DonViTinh,
                TonDauKy = vt.TonKhoHienTai - phatSinh,
                TongNhap = nhap,
                TongXuat = -(xuatTh + xuatK),
                TongTraLai = -(traLai),
                TonCuoiKy = vt.TonKhoHienTai
            };
        }).ToList();
        return new BaoCaoNhapXuatVM { TuNgay = tuNgay, DenNgay = denNgay, Rows = rows };
    }

    public async Task<BaoCaoTieuHaoVM> GetTieuHaoAsync(int nam)
    {
        var tieuHaos = (await _dinhMucRepo.GetTieuHaoAsync(nam)).ToList();
        var vatTus = (await _vtRepo.GetAllAsync()).Where(x => x.CanTheoDoiTieuHao).ToList();
        var rows = vatTus.Select(vt =>
        {
            var row = new BaoCaoTieuHaoRow { TenVt = vt.TenVt, DonViTinh = vt.DonViTinh };
            var thList = tieuHaos.Where(t => t.IdVatTu == vt.Id).ToList();
            row.T1 = thList.Where(t => t.ThangInThucTe.Month == 1).Sum(t => t.SoLuongTieuHao);
            row.T2 = thList.Where(t => t.ThangInThucTe.Month == 2).Sum(t => t.SoLuongTieuHao);
            row.T3 = thList.Where(t => t.ThangInThucTe.Month == 3).Sum(t => t.SoLuongTieuHao);
            row.T4 = thList.Where(t => t.ThangInThucTe.Month == 4).Sum(t => t.SoLuongTieuHao);
            row.T5 = thList.Where(t => t.ThangInThucTe.Month == 5).Sum(t => t.SoLuongTieuHao);
            row.T6 = thList.Where(t => t.ThangInThucTe.Month == 6).Sum(t => t.SoLuongTieuHao);
            row.T7 = thList.Where(t => t.ThangInThucTe.Month == 7).Sum(t => t.SoLuongTieuHao);
            row.T8 = thList.Where(t => t.ThangInThucTe.Month == 8).Sum(t => t.SoLuongTieuHao);
            row.T9 = thList.Where(t => t.ThangInThucTe.Month == 9).Sum(t => t.SoLuongTieuHao);
            row.T10 = thList.Where(t => t.ThangInThucTe.Month == 10).Sum(t => t.SoLuongTieuHao);
            row.T11 = thList.Where(t => t.ThangInThucTe.Month == 11).Sum(t => t.SoLuongTieuHao);
            row.T12 = thList.Where(t => t.ThangInThucTe.Month == 12).Sum(t => t.SoLuongTieuHao);
            return row;
        }).ToList();
        return new BaoCaoTieuHaoVM { Nam = nam, Rows = rows };
    }

    public async Task<BaoCaoKhuonBeNccVM> GetKhuonBeTheoNccAsync()
    {
        return new BaoCaoKhuonBeNccVM
        {
            Rows = (await _khuonBeRepo.GetBaoCaoTheoNccAsync()).ToList(),
            CanhBao = (await _khuonBeRepo.GetKhuonCanChuYAsync()).ToList()
        };
    }

    public async Task<XLWorkbook> ExportNhapXuatExcelAsync(DateTime tuNgay, DateTime denNgay)
    {
        var vm = await GetNhapXuatAsync(tuNgay, denNgay);
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Báo cáo nhập xuất");

        // Header
        ws.Cell("A1").Value = $"BÁO CÁO NHẬP XUẤT TỒN KHO (Từ {tuNgay:dd/MM/yyyy} đến {denNgay:dd/MM/yyyy})";
        ws.Range("A1:F1").Merge().Style.Font.Bold = true;
        ws.Cell("A2").Value = "Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        ws.Range("A2:F2").Merge();

        // Table header
        ws.Cell("A4").Value = "Vật tư";
        ws.Cell("B4").Value = "ĐVT";
        ws.Cell("C4").Value = "Tồn đầu kỳ";
        ws.Cell("D4").Value = "Nhập";
        ws.Cell("E4").Value = "Xuất";
        ws.Cell("F4").Value = "Trả lại";
        ws.Cell("G4").Value = "Tồn cuối kỳ";
        var hRow = ws.Range("A4:G4");
        hRow.Style.Font.Bold = true;
        hRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
        hRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        // Data
        var row = 5;
        foreach (var r in vm.Rows)
        {
            ws.Cell(row, 1).Value = r.TenVt;
            ws.Cell(row, 2).Value = r.DonViTinh;
            ws.Cell(row, 3).Value = (double)r.TonDauKy;
            ws.Cell(row, 4).Value = (double)r.TongNhap;
            ws.Cell(row, 5).Value = (double)r.TongXuat;
            ws.Cell(row, 6).Value = (double)r.TongTraLai;
            ws.Cell(row, 7).Value = (double)r.TonCuoiKy;
            ws.Range(row, 1, row, 7).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();
        return wb;
    }

    public async Task<XLWorkbook> ExportTieuHaoExcelAsync(int nam)
    {
        var vm = await GetTieuHaoAsync(nam);
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Tiêu hao thực tế");

        ws.Cell("A1").Value = $"BÁO CÁO TIÊU HAO THỰC TẾ NĂM {nam}";
        ws.Range("A1:N1").Merge().Style.Font.Bold = true;
        ws.Cell("A2").Value = "Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        ws.Range("A2:N2").Merge();

        ws.Cell("A4").Value = "Vật tư";
        ws.Cell("B4").Value = "ĐVT";
        var months = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12", "Tổng" };
        for (int i = 0; i < months.Length; i++)
            ws.Cell(4, 3 + i).Value = months[i];
        var hRow = ws.Range("A4:N4");
        hRow.Style.Font.Bold = true;
        hRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
        hRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var row = 5;
        foreach (var r in vm.Rows)
        {
            ws.Cell(row, 1).Value = r.TenVt;
            ws.Cell(row, 2).Value = r.DonViTinh;
            ws.Cell(row, 3).Value = (double)r.T1;
            ws.Cell(row, 4).Value = (double)r.T2;
            ws.Cell(row, 5).Value = (double)r.T3;
            ws.Cell(row, 6).Value = (double)r.T4;
            ws.Cell(row, 7).Value = (double)r.T5;
            ws.Cell(row, 8).Value = (double)r.T6;
            ws.Cell(row, 9).Value = (double)r.T7;
            ws.Cell(row, 10).Value = (double)r.T8;
            ws.Cell(row, 11).Value = (double)r.T9;
            ws.Cell(row, 12).Value = (double)r.T10;
            ws.Cell(row, 13).Value = (double)r.T11;
            ws.Cell(row, 14).Value = (double)r.T12;
            ws.Cell(row, 15).Value = (double)r.Tong;
            ws.Range(row, 1, row, 15).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }

        ws.Columns().AdjustToContents();
        return wb;
    }

    public async Task<XLWorkbook> ExportKhuonBeTheoNccExcelAsync()
    {
        var vm = await GetKhuonBeTheoNccAsync();
        var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Khuôn bế theo NCC");

        ws.Cell("A1").Value = "BÁO CÁO TUỔI THỌ / HIỆU SUẤT KHUÔN BẾ THEO NHÀ CUNG CẤP";
        ws.Range("A1:I1").Merge().Style.Font.Bold = true;
        ws.Cell("A2").Value = "Ngày xuất: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        ws.Range("A2:I2").Merge();

        var headers = new[] { "Nhà cung cấp", "Tổng khuôn", "Đang dùng", "Hỏng", "Tổng tờ đã in", "Tuổi thọ TB", "Định mức TB", "% định mức", "Khuôn cần chú ý" };
        for (int i = 0; i < headers.Length; i++) ws.Cell(4, i + 1).Value = headers[i];
        var hRow = ws.Range("A4:I4");
        hRow.Style.Font.Bold = true;
        hRow.Style.Fill.BackgroundColor = XLColor.LightBlue;
        hRow.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

        var row = 5;
        foreach (var r in vm.Rows)
        {
            ws.Cell(row, 1).Value = r.TenNcc;
            ws.Cell(row, 2).Value = r.TongSoKhuon;
            ws.Cell(row, 3).Value = r.SoKhuonDangDung;
            ws.Cell(row, 4).Value = r.SoKhuonHong;
            ws.Cell(row, 5).Value = (double)r.TongSoToDaIn;
            ws.Cell(row, 6).Value = (double)r.TuoiThoTrungBinh;
            ws.Cell(row, 7).Value = r.DinhMucTrungBinh > 0 ? (double)r.DinhMucTrungBinh : "";
            ws.Cell(row, 8).Value = r.TyLeSuDungDinhMuc > 0 ? (double)r.TyLeSuDungDinhMuc : "";
            ws.Cell(row, 9).Value = r.SoKhuonCanChuY;
            ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }

        row += 2;
        ws.Cell(row, 1).Value = "DANH SÁCH KHUÔN CẦN CHÚ Ý";
        ws.Range(row, 1, row, 8).Merge().Style.Font.Bold = true;
        row += 1;

        var detailHeaders = new[] { "Khuôn", "Mã", "Version", "NCC", "Trạng thái", "Tờ đã in", "Định mức", "% tuổi thọ", "Lý do" };
        for (int i = 0; i < detailHeaders.Length; i++) ws.Cell(row, i + 1).Value = detailHeaders[i];
        ws.Range(row, 1, row, 9).Style.Font.Bold = true;
        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.LightYellow;
        row++;

        foreach (var r in vm.CanhBao)
        {
            ws.Cell(row, 1).Value = r.TenKhuon;
            ws.Cell(row, 2).Value = r.MaKhuon;
            ws.Cell(row, 3).Value = r.PhienBan;
            ws.Cell(row, 4).Value = r.TenNcc;
            ws.Cell(row, 5).Value = r.TrangThai == "dang_dung" ? "Đang dùng" : r.TrangThai == "hong" ? "Hỏng" : r.TrangThai;
            ws.Cell(row, 6).Value = (double)r.SoToDaIn;
            ws.Cell(row, 7).Value = r.DinhMucTuoiTho.HasValue ? (double)r.DinhMucTuoiTho.Value : "";
            ws.Cell(row, 8).Value = (double)r.PhanTramTuoiTho;
            ws.Cell(row, 9).Value = r.LyDoCanhBao;
            ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            row++;
        }

        ws.Columns().AdjustToContents();
        return wb;
    }
}