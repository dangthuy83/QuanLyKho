using Dapper;
using KhoQuanLy.Models.Entities;
using KhoQuanLy.Models.ViewModels;
using System.Data;

namespace KhoQuanLy.Repositories;

// ── Phiếu SP ─────────────────────────────────────────────────
public interface IPhieuSPRepository
{
    Task<IEnumerable<PhieuSP>> GetAllAsync(string? trangThai = null, string? search = null);
    Task<PhieuSP?> GetByIdAsync(int id);
    Task<PhieuSP?> GetBySoPhieuAsync(string soPhieu);
    Task<int> CreateAsync(PhieuSP e, IDbTransaction? txn = null);
    Task UpdateAsync(PhieuSP e, IDbTransaction? txn = null);
    Task UpdateTrangThaiAsync(int id, string trangThai, IDbTransaction? txn = null);
    Task<IEnumerable<PhieuSPVatTu>> GetVatTuByPhieuAsync(int idPhieu, IDbTransaction? txn = null);
    Task<PhieuSPVatTu?> GetVatTuCtByIdAsync(int id);
    Task<int> CreateVatTuCtAsync(PhieuSPVatTu e, IDbTransaction? txn = null);
    Task UpdateVatTuCtAsync(PhieuSPVatTu e, IDbTransaction? txn = null);
    Task DeleteVatTuCtAsync(int id, IDbTransaction? txn = null);
    Task<bool> HasVatTuCtPhatSinhAsync(int idVatTuCt, IDbTransaction? txn = null);
    Task<IEnumerable<PhieuSPLichSuSua>> GetLichSuSuaAsync(int idPhieu);
    Task CreateLichSuSuaAsync(PhieuSPLichSuSua e, IDbTransaction? txn = null);
    Task UpdateSoLuongDaNhanAsync(int idVatTuCt, decimal soLuong);
    Task UpdateTrangThaiNhanAsync(int idVatTuCt, decimal soLuong, string trangThai, string? ghiChu = null, IDbTransaction? txn = null);
}

public class PhieuSPRepository : IPhieuSPRepository
{
    private readonly IDbConnectionFactory _db;
    public PhieuSPRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<PhieuSP>> GetAllAsync(string? trangThai = null, string? search = null)
    {
        using var c = _db.CreateConnection();
        var sql = @"SELECT id Id,so_phieu SoPhieu,so_lsx SoLsx,ten_san_pham TenSanPham,
            ma_san_pham MaSanPham,khach_hang KhachHang,so_luong_sp SoLuongSp,
            ngay_lenh NgayLenh,ngay_giao_hang NgayGiaoHang,
            CASE
                WHEN trang_thai <> 'hoan_thanh'
                 AND EXISTS (
                    SELECT 1 FROM kho_phieu_sp_vat_tu pv
                    WHERE pv.id_phieu_sp=kho_phieu_sp.id AND pv.trang_thai_nhan <> 'khong_can'
                 )
                 AND NOT EXISTS (
                    SELECT 1
                    FROM kho_phieu_sp_vat_tu pv
                    LEFT JOIN (
                        SELECT id_phieu_sp_vat_tu, SUM(COALESCE(so_luong_thuc_nhan,0)) tong_thuc_nhan
                        FROM kho_dot_de_nghi_ct GROUP BY id_phieu_sp_vat_tu
                    ) dn ON dn.id_phieu_sp_vat_tu=pv.id
                    WHERE pv.id_phieu_sp=kho_phieu_sp.id
                      AND pv.trang_thai_nhan <> 'khong_can'
                      AND GREATEST(COALESCE(pv.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0)) < pv.so_luong_yeu_cau
                 ) THEN 'hoan_thanh'
                ELSE trang_thai
            END TrangThai,
            file_pdf_path FilePdfPath,nguoi_tao NguoiTao,created_at CreatedAt
            FROM kho_phieu_sp WHERE 1=1";
        if (!string.IsNullOrEmpty(trangThai))
        {
            if (trangThai == "hoan_thanh")
                sql += @" AND (trang_thai=@trangThai OR (
                    EXISTS (SELECT 1 FROM kho_phieu_sp_vat_tu pv WHERE pv.id_phieu_sp=kho_phieu_sp.id AND pv.trang_thai_nhan <> 'khong_can')
                    AND NOT EXISTS (
                        SELECT 1 FROM kho_phieu_sp_vat_tu pv
                        LEFT JOIN (
                            SELECT id_phieu_sp_vat_tu, SUM(COALESCE(so_luong_thuc_nhan,0)) tong_thuc_nhan
                            FROM kho_dot_de_nghi_ct GROUP BY id_phieu_sp_vat_tu
                        ) dn ON dn.id_phieu_sp_vat_tu=pv.id
                        WHERE pv.id_phieu_sp=kho_phieu_sp.id
                          AND pv.trang_thai_nhan <> 'khong_can'
                          AND GREATEST(COALESCE(pv.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0)) < pv.so_luong_yeu_cau
                    )))";
            else if (trangThai == "chua_hoan_thanh")
                sql += @" AND trang_thai=@trangThai AND NOT (
                    EXISTS (SELECT 1 FROM kho_phieu_sp_vat_tu pv WHERE pv.id_phieu_sp=kho_phieu_sp.id AND pv.trang_thai_nhan <> 'khong_can')
                    AND NOT EXISTS (
                        SELECT 1 FROM kho_phieu_sp_vat_tu pv
                        LEFT JOIN (
                            SELECT id_phieu_sp_vat_tu, SUM(COALESCE(so_luong_thuc_nhan,0)) tong_thuc_nhan
                            FROM kho_dot_de_nghi_ct GROUP BY id_phieu_sp_vat_tu
                        ) dn ON dn.id_phieu_sp_vat_tu=pv.id
                        WHERE pv.id_phieu_sp=kho_phieu_sp.id
                          AND pv.trang_thai_nhan <> 'khong_can'
                          AND GREATEST(COALESCE(pv.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0)) < pv.so_luong_yeu_cau
                    ))";
            else sql += " AND trang_thai=@trangThai";
        }
        if (!string.IsNullOrEmpty(search)) sql += " AND (so_phieu LIKE @search OR ten_san_pham LIKE @search OR khach_hang LIKE @search)";
        sql += " ORDER BY created_at DESC";
        return await c.QueryAsync<PhieuSP>(sql, new { trangThai, search = $"%{search}%" });
    }

    public async Task<PhieuSP?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<PhieuSP>(@"SELECT id Id,so_phieu SoPhieu,so_lsx SoLsx,
            ten_san_pham TenSanPham,ma_san_pham MaSanPham,khach_hang KhachHang,so_luong_sp SoLuongSp,
            ngay_lenh NgayLenh,ngay_giao_hang NgayGiaoHang,trang_thai TrangThai,
            file_pdf_path FilePdfPath,nguoi_tao NguoiTao,created_at CreatedAt
            FROM kho_phieu_sp WHERE id=@id", new { id });
    }

    public async Task<PhieuSP?> GetBySoPhieuAsync(string soPhieu)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<PhieuSP>(@"SELECT id Id,so_phieu SoPhieu,so_lsx SoLsx,
            ten_san_pham TenSanPham,ma_san_pham MaSanPham,khach_hang KhachHang,so_luong_sp SoLuongSp,
            ngay_lenh NgayLenh,ngay_giao_hang NgayGiaoHang,trang_thai TrangThai,
            file_pdf_path FilePdfPath,nguoi_tao NguoiTao,created_at CreatedAt
            FROM kho_phieu_sp WHERE so_phieu=@soPhieu LIMIT 1", new { soPhieu });
    }

    public async Task<int> CreateAsync(PhieuSP e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_sp
            (so_phieu,so_lsx,ten_san_pham,ma_san_pham,khach_hang,so_luong_sp,ngay_lenh,ngay_giao_hang,trang_thai,file_pdf_path,nguoi_tao)
            VALUES(@SoPhieu,@SoLsx,@TenSanPham,@MaSanPham,@KhachHang,@SoLuongSp,@NgayLenh,@NgayGiaoHang,@TrangThai,@FilePdfPath,@NguoiTao);
            SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_sp
            (so_phieu,so_lsx,ten_san_pham,ma_san_pham,khach_hang,so_luong_sp,ngay_lenh,ngay_giao_hang,trang_thai,file_pdf_path,nguoi_tao)
            VALUES(@SoPhieu,@SoLsx,@TenSanPham,@MaSanPham,@KhachHang,@SoLuongSp,@NgayLenh,@NgayGiaoHang,@TrangThai,@FilePdfPath,@NguoiTao);
            SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateAsync(PhieuSP e, IDbTransaction? txn = null)
    {
        var sql = @"UPDATE kho_phieu_sp SET so_phieu=@SoPhieu,so_lsx=@SoLsx,ten_san_pham=@TenSanPham,
            ma_san_pham=@MaSanPham,khach_hang=@KhachHang,so_luong_sp=@SoLuongSp,ngay_lenh=@NgayLenh,
            ngay_giao_hang=@NgayGiaoHang,trang_thai=@TrangThai,file_pdf_path=@FilePdfPath,nguoi_tao=@NguoiTao WHERE id=@Id";
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync(sql, e, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(sql, e);
    }

    public async Task UpdateTrangThaiAsync(int id, string trangThai, IDbTransaction? txn = null)
    {
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync("UPDATE kho_phieu_sp SET trang_thai=@trangThai WHERE id=@id", new { id, trangThai }, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_phieu_sp SET trang_thai=@trangThai WHERE id=@id", new { id, trangThai });
    }

    public async Task<IEnumerable<PhieuSPVatTu>> GetVatTuByPhieuAsync(int idPhieu, IDbTransaction? txn = null)
    {
        var sql = @"SELECT p.id Id,p.id_phieu_sp IdPhieuSp,p.id_vat_tu IdVatTu,p.id_vat_tu_thay_the IdVatTuThayThe,
            COALESCE(v.ten_vt,'') TenVt,COALESCE(v.ma_vt,'') MaVt,
            COALESCE(vt2.ten_vt,'') TenVtThayThe,COALESCE(vt2.ma_vt,'') MaVtThayThe,COALESCE(vt2.don_vi_tinh,'') DonViTinhThayThe,
            p.chung_loai_text ChungLoaiText,p.kich_thuoc KichThuoc,
            p.so_luong_yeu_cau SoLuongYeuCau,p.so_luong_to_roi SoLuongToRoi,
            p.don_vi_tinh DonViTinh,p.la_cuon_thay_to_roi LaCuonThayToRoi,
            GREATEST(COALESCE(p.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0)) SoLuongDaNhan,
            CASE
                WHEN p.trang_thai_nhan='khong_can' THEN 'khong_can'
                WHEN GREATEST(COALESCE(p.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0))<=0 THEN 'chua_nhan'
                WHEN GREATEST(COALESCE(p.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0))<p.so_luong_yeu_cau THEN 'thieu'
                ELSE 'du'
            END TrangThaiNhan,
            p.ghi_chu_khong_can GhiChuKhongCan,p.ghi_chu GhiChu
            FROM kho_phieu_sp_vat_tu p
            LEFT JOIN kho_vat_tu v ON v.id=p.id_vat_tu
            LEFT JOIN kho_vat_tu vt2 ON vt2.id=p.id_vat_tu_thay_the
            LEFT JOIN (
                SELECT id_phieu_sp_vat_tu, SUM(COALESCE(so_luong_thuc_nhan,0)) tong_thuc_nhan
                FROM kho_dot_de_nghi_ct GROUP BY id_phieu_sp_vat_tu
            ) dn ON dn.id_phieu_sp_vat_tu=p.id
            WHERE p.id_phieu_sp=@idPhieu";
        if (txn != null)
            return await txn.Connection!.QueryAsync<PhieuSPVatTu>(sql, new { idPhieu }, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.QueryAsync<PhieuSPVatTu>(sql, new { idPhieu });
    }

    public async Task<PhieuSPVatTu?> GetVatTuCtByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<PhieuSPVatTu>(@"SELECT p.id Id,p.id_phieu_sp IdPhieuSp,p.id_vat_tu IdVatTu,p.id_vat_tu_thay_the IdVatTuThayThe,
            COALESCE(v.ten_vt,'') TenVt,COALESCE(v.ma_vt,'') MaVt,
            COALESCE(vt2.ten_vt,'') TenVtThayThe,COALESCE(vt2.ma_vt,'') MaVtThayThe,COALESCE(vt2.don_vi_tinh,'') DonViTinhThayThe,
            p.chung_loai_text ChungLoaiText,p.kich_thuoc KichThuoc,
            p.so_luong_yeu_cau SoLuongYeuCau,p.so_luong_to_roi SoLuongToRoi,
            p.don_vi_tinh DonViTinh,p.la_cuon_thay_to_roi LaCuonThayToRoi,
            GREATEST(COALESCE(p.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0)) SoLuongDaNhan,
            CASE
                WHEN p.trang_thai_nhan='khong_can' THEN 'khong_can'
                WHEN GREATEST(COALESCE(p.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0))<=0 THEN 'chua_nhan'
                WHEN GREATEST(COALESCE(p.so_luong_da_nhan,0),COALESCE(dn.tong_thuc_nhan,0))<p.so_luong_yeu_cau THEN 'thieu'
                ELSE 'du'
            END TrangThaiNhan,
            p.ghi_chu_khong_can GhiChuKhongCan,p.ghi_chu GhiChu
            FROM kho_phieu_sp_vat_tu p LEFT JOIN kho_vat_tu v ON v.id=p.id_vat_tu
            LEFT JOIN kho_vat_tu vt2 ON vt2.id=p.id_vat_tu_thay_the
            LEFT JOIN (
                SELECT id_phieu_sp_vat_tu, SUM(COALESCE(so_luong_thuc_nhan,0)) tong_thuc_nhan
                FROM kho_dot_de_nghi_ct GROUP BY id_phieu_sp_vat_tu
            ) dn ON dn.id_phieu_sp_vat_tu=p.id
            WHERE p.id=@id", new { id });
    }

    public async Task<int> CreateVatTuCtAsync(PhieuSPVatTu e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_sp_vat_tu
            (id_phieu_sp,id_vat_tu,id_vat_tu_thay_the,chung_loai_text,kich_thuoc,so_luong_yeu_cau,so_luong_to_roi,don_vi_tinh,la_cuon_thay_to_roi,ghi_chu)
            VALUES(@IdPhieuSp,@IdVatTu,@IdVatTuThayThe,@ChungLoaiText,@KichThuoc,@SoLuongYeuCau,@SoLuongToRoi,@DonViTinh,@LaCuonThayToRoi,@GhiChu);
            SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_sp_vat_tu
            (id_phieu_sp,id_vat_tu,id_vat_tu_thay_the,chung_loai_text,kich_thuoc,so_luong_yeu_cau,so_luong_to_roi,don_vi_tinh,la_cuon_thay_to_roi,ghi_chu)
            VALUES(@IdPhieuSp,@IdVatTu,@IdVatTuThayThe,@ChungLoaiText,@KichThuoc,@SoLuongYeuCau,@SoLuongToRoi,@DonViTinh,@LaCuonThayToRoi,@GhiChu);
            SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateVatTuCtAsync(PhieuSPVatTu e, IDbTransaction? txn = null)
    {
        var sql = @"UPDATE kho_phieu_sp_vat_tu SET id_vat_tu=@IdVatTu,id_vat_tu_thay_the=@IdVatTuThayThe,chung_loai_text=@ChungLoaiText,
            kich_thuoc=@KichThuoc,so_luong_yeu_cau=@SoLuongYeuCau,so_luong_to_roi=@SoLuongToRoi,
            don_vi_tinh=@DonViTinh,la_cuon_thay_to_roi=@LaCuonThayToRoi,
            so_luong_da_nhan=@SoLuongDaNhan,trang_thai_nhan=@TrangThaiNhan,
            ghi_chu_khong_can=@GhiChuKhongCan,ghi_chu=@GhiChu WHERE id=@Id";
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync(sql, e, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(sql, e);
    }

    public async Task DeleteVatTuCtAsync(int id, IDbTransaction? txn = null)
    {
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync("DELETE FROM kho_phieu_sp_vat_tu WHERE id=@id", new { id }, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("DELETE FROM kho_phieu_sp_vat_tu WHERE id=@id", new { id });
    }

    public async Task<bool> HasVatTuCtPhatSinhAsync(int idVatTuCt, IDbTransaction? txn = null)
    {
        var sql = @"SELECT COUNT(*) FROM kho_dot_de_nghi_ct WHERE id_phieu_sp_vat_tu=@idVatTuCt";
        var dotCount = txn != null
            ? await txn.Connection!.ExecuteScalarAsync<int>(sql, new { idVatTuCt }, transaction: txn)
            : await QueryScalarAsync<int>(sql, new { idVatTuCt });
        if (dotCount > 0) return true;

        var vt = txn != null
            ? await txn.Connection!.QueryFirstOrDefaultAsync<PhieuSPVatTu>(@"SELECT so_luong_da_nhan SoLuongDaNhan,trang_thai_nhan TrangThaiNhan FROM kho_phieu_sp_vat_tu WHERE id=@idVatTuCt", new { idVatTuCt }, transaction: txn)
            : await QueryFirstOrDefaultAsync<PhieuSPVatTu>(@"SELECT so_luong_da_nhan SoLuongDaNhan,trang_thai_nhan TrangThaiNhan FROM kho_phieu_sp_vat_tu WHERE id=@idVatTuCt", new { idVatTuCt });
        return vt != null && (vt.SoLuongDaNhan > 0 || vt.TrangThaiNhan != "chua_nhan");
    }

    private async Task<T> QueryScalarAsync<T>(string sql, object param)
    {
        using var c = _db.CreateConnection();
        return (await c.ExecuteScalarAsync<T>(sql, param))!;
    }

    private async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object param)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<IEnumerable<PhieuSPLichSuSua>> GetLichSuSuaAsync(int idPhieu)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<PhieuSPLichSuSua>(@"SELECT l.id Id,l.id_phieu_sp IdPhieuSp,l.id_vat_tu IdVatTu,
            COALESCE(v.ten_vt,'') TenVt,l.so_luong_cu SoLuongCu,l.so_luong_moi SoLuongMoi,
            l.ly_do LyDo,l.nguoi_sua NguoiSua,l.created_at CreatedAt
            FROM kho_phieu_sp_lich_su_sua l LEFT JOIN kho_vat_tu v ON v.id=l.id_vat_tu
            WHERE l.id_phieu_sp=@idPhieu ORDER BY l.created_at DESC", new { idPhieu });
    }

    public async Task CreateLichSuSuaAsync(PhieuSPLichSuSua e, IDbTransaction? txn = null)
    {
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync(@"INSERT INTO kho_phieu_sp_lich_su_sua(id_phieu_sp,id_vat_tu,so_luong_cu,so_luong_moi,ly_do,nguoi_sua)
                VALUES(@IdPhieuSp,@IdVatTu,@SoLuongCu,@SoLuongMoi,@LyDo,@NguoiSua)", e, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"INSERT INTO kho_phieu_sp_lich_su_sua(id_phieu_sp,id_vat_tu,so_luong_cu,so_luong_moi,ly_do,nguoi_sua)
            VALUES(@IdPhieuSp,@IdVatTu,@SoLuongCu,@SoLuongMoi,@LyDo,@NguoiSua)", e);
    }

    public async Task UpdateSoLuongDaNhanAsync(int idVatTuCt, decimal soLuong)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_phieu_sp_vat_tu SET so_luong_da_nhan=@soLuong WHERE id=@idVatTuCt", new { idVatTuCt, soLuong });
    }

    public async Task UpdateTrangThaiNhanAsync(int idVatTuCt, decimal soLuong, string trangThai, string? ghiChu = null, IDbTransaction? txn = null)
    {
        var sql = @"UPDATE kho_phieu_sp_vat_tu
            SET so_luong_da_nhan=@soLuong,trang_thai_nhan=@trangThai,ghi_chu_khong_can=@ghiChu
            WHERE id=@idVatTuCt";
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync(sql, new { idVatTuCt, soLuong, trangThai, ghiChu }, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(sql, new { idVatTuCt, soLuong, trangThai, ghiChu });
    }
}

// ── Đợt Đề Nghị ──────────────────────────────────────────────
public interface IDeNghiXuatRepository
{
    Task<IEnumerable<DotDeNghi>> GetAllAsync();
    Task<DotDeNghi?> GetByIdAsync(int id);
    Task<int> CreateAsync(DotDeNghi e, IDbTransaction? txn = null);
    Task UpdateTrangThaiAsync(int id, string trangThai);
    Task<IEnumerable<DotDeNghiCt>> GetChiTietAsync(int idDot);
    Task<DotDeNghiCt?> GetChiTietByIdAsync(int idCt);
    Task<int> CreateChiTietAsync(DotDeNghiCt e, IDbTransaction? txn = null);
    Task UpdateThucNhanAsync(int id, decimal soLuong, string trangThai, IDbTransaction? txn = null);
    Task<decimal> GetTongThucNhanByVatTuCtAsync(int idPhieuSpVatTu, IDbTransaction? txn = null);
    Task<IEnumerable<DotDeNghiCt>> GetPendingFromPreviousDotsAsync();
    Task<string> GenMaDotAsync();
}

public class DeNghiXuatRepository : IDeNghiXuatRepository
{
    private readonly IDbConnectionFactory _db;
    public DeNghiXuatRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<DotDeNghi>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<DotDeNghi>(@"SELECT id Id,ma_dot MaDot,ngay_tao NgayTao,nguoi_tao NguoiTao,
            trang_thai TrangThai,ghi_chu GhiChu,created_at CreatedAt
            FROM kho_dot_de_nghi ORDER BY created_at DESC");
    }

    public async Task<DotDeNghi?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<DotDeNghi>(@"SELECT id Id,ma_dot MaDot,ngay_tao NgayTao,
            nguoi_tao NguoiTao,trang_thai TrangThai,ghi_chu GhiChu,created_at CreatedAt
            FROM kho_dot_de_nghi WHERE id=@id", new { id });
    }

    public async Task<int> CreateAsync(DotDeNghi e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_dot_de_nghi(ma_dot,ngay_tao,nguoi_tao,trang_thai,ghi_chu)
            VALUES(@MaDot,@NgayTao,@NguoiTao,@TrangThai,@GhiChu);SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_dot_de_nghi(ma_dot,ngay_tao,nguoi_tao,trang_thai,ghi_chu)
            VALUES(@MaDot,@NgayTao,@NguoiTao,@TrangThai,@GhiChu);SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateTrangThaiAsync(int id, string trangThai)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_dot_de_nghi SET trang_thai=@trangThai WHERE id=@id", new { id, trangThai });
    }

    public async Task<IEnumerable<DotDeNghiCt>> GetChiTietAsync(int idDot)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<DotDeNghiCt>(@"SELECT d.id Id,d.id_dot IdDot,d.id_phieu_sp_vat_tu IdPhieuSpVatTu,
            d.so_luong_de_nghi SoLuongDeNghi,d.ton_ky_truoc TonKyTruoc,
            d.so_luong_thuc_nhan SoLuongThucNhan,d.trang_thai TrangThai,d.ghi_chu GhiChu,
            p.so_phieu SoPhieu,ps.ten_san_pham TenSanPham,
            pv.id_vat_tu IdVatTu,pv.id_vat_tu_thay_the IdVatTuThayThe,
            pv.chung_loai_text ChungLoaiText,pv.kich_thuoc KichThuoc,pv.so_luong_yeu_cau SoLuongYeuCau,
            COALESCE(v.ten_vt,'') TenVt,COALESCE(v2.ten_vt,'') TenVtThayThe,COALESCE(v2.ma_vt,'') MaVtThayThe,
            COALESCE(v.don_vi_tinh,pv.don_vi_tinh,'') DonViTinh,COALESCE(v2.don_vi_tinh,'') DonViTinhThayThe
            FROM kho_dot_de_nghi_ct d
            JOIN kho_phieu_sp_vat_tu pv ON pv.id=d.id_phieu_sp_vat_tu
            JOIN kho_phieu_sp ps ON ps.id=pv.id_phieu_sp
            LEFT JOIN kho_phieu_sp p ON p.id=pv.id_phieu_sp
            LEFT JOIN kho_vat_tu v ON v.id=pv.id_vat_tu
            LEFT JOIN kho_vat_tu v2 ON v2.id=pv.id_vat_tu_thay_the
            WHERE d.id_dot=@idDot", new { idDot });
    }

    public async Task<DotDeNghiCt?> GetChiTietByIdAsync(int idCt)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<DotDeNghiCt>(@"SELECT d.id Id,d.id_dot IdDot,d.id_phieu_sp_vat_tu IdPhieuSpVatTu,
            d.so_luong_de_nghi SoLuongDeNghi,d.ton_ky_truoc TonKyTruoc,
            d.so_luong_thuc_nhan SoLuongThucNhan,d.trang_thai TrangThai,d.ghi_chu GhiChu,
            ps.so_phieu SoPhieu,ps.ten_san_pham TenSanPham,
            pv.id_vat_tu IdVatTu,pv.id_vat_tu_thay_the IdVatTuThayThe,
            pv.chung_loai_text ChungLoaiText,pv.kich_thuoc KichThuoc,pv.so_luong_yeu_cau SoLuongYeuCau,
            COALESCE(v.ten_vt,'') TenVt,COALESCE(v2.ten_vt,'') TenVtThayThe,COALESCE(v2.ma_vt,'') MaVtThayThe,
            COALESCE(v.don_vi_tinh,pv.don_vi_tinh,'') DonViTinh,COALESCE(v2.don_vi_tinh,'') DonViTinhThayThe, dot.ma_dot MaDot
            FROM kho_dot_de_nghi_ct d
            JOIN kho_dot_de_nghi dot ON dot.id=d.id_dot
            JOIN kho_phieu_sp_vat_tu pv ON pv.id=d.id_phieu_sp_vat_tu
            JOIN kho_phieu_sp ps ON ps.id=pv.id_phieu_sp
            LEFT JOIN kho_vat_tu v ON v.id=pv.id_vat_tu
            LEFT JOIN kho_vat_tu v2 ON v2.id=pv.id_vat_tu_thay_the
            WHERE d.id=@idCt", new { idCt });
    }

    public async Task<int> CreateChiTietAsync(DotDeNghiCt e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_dot_de_nghi_ct
            (id_dot,id_phieu_sp_vat_tu,so_luong_de_nghi,ton_ky_truoc,trang_thai,ghi_chu)
            VALUES(@IdDot,@IdPhieuSpVatTu,@SoLuongDeNghi,@TonKyTruoc,@TrangThai,@GhiChu);SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_dot_de_nghi_ct
            (id_dot,id_phieu_sp_vat_tu,so_luong_de_nghi,ton_ky_truoc,trang_thai,ghi_chu)
            VALUES(@IdDot,@IdPhieuSpVatTu,@SoLuongDeNghi,@TonKyTruoc,@TrangThai,@GhiChu);SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateThucNhanAsync(int id, decimal soLuong, string trangThai, IDbTransaction? txn = null)
    {
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync("UPDATE kho_dot_de_nghi_ct SET so_luong_thuc_nhan=@soLuong,trang_thai=@trangThai WHERE id=@id",
                new { id, soLuong, trangThai }, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_dot_de_nghi_ct SET so_luong_thuc_nhan=@soLuong,trang_thai=@trangThai WHERE id=@id",
            new { id, soLuong, trangThai });
    }

    public async Task<decimal> GetTongThucNhanByVatTuCtAsync(int idPhieuSpVatTu, IDbTransaction? txn = null)
    {
        const string sql = @"SELECT COALESCE(SUM(so_luong_thuc_nhan),0)
            FROM kho_dot_de_nghi_ct
            WHERE id_phieu_sp_vat_tu=@idPhieuSpVatTu";
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<decimal>(sql, new { idPhieuSpVatTu }, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<decimal>(sql, new { idPhieuSpVatTu });
    }

    public async Task<IEnumerable<DotDeNghiCt>> GetPendingFromPreviousDotsAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<DotDeNghiCt>(@"SELECT d.id Id,d.id_dot IdDot,d.id_phieu_sp_vat_tu IdPhieuSpVatTu,
            (COALESCE(d.so_luong_de_nghi,0) - COALESCE(d.so_luong_thuc_nhan,0)) SoLuongDeNghi,d.ton_ky_truoc TonKyTruoc,
            d.so_luong_thuc_nhan SoLuongThucNhan,d.trang_thai TrangThai,d.ghi_chu GhiChu,
            p.so_phieu SoPhieu,ps.ten_san_pham TenSanPham,
            pv.id_vat_tu IdVatTu,pv.id_vat_tu_thay_the IdVatTuThayThe,
            pv.chung_loai_text ChungLoaiText,pv.kich_thuoc KichThuoc,pv.so_luong_yeu_cau SoLuongYeuCau,
            COALESCE(v.ten_vt,'') TenVt,COALESCE(v2.ten_vt,'') TenVtThayThe,COALESCE(v2.ma_vt,'') MaVtThayThe,
            COALESCE(v.don_vi_tinh,pv.don_vi_tinh,'') DonViTinh,COALESCE(v2.don_vi_tinh,'') DonViTinhThayThe, dot.ma_dot MaDot
            FROM kho_dot_de_nghi_ct d
            JOIN kho_dot_de_nghi dot ON dot.id=d.id_dot
            JOIN kho_phieu_sp_vat_tu pv ON pv.id=d.id_phieu_sp_vat_tu
            JOIN kho_phieu_sp ps ON ps.id=pv.id_phieu_sp
            LEFT JOIN kho_phieu_sp p ON p.id=pv.id_phieu_sp
            LEFT JOIN kho_vat_tu v ON v.id=pv.id_vat_tu
            LEFT JOIN kho_vat_tu v2 ON v2.id=pv.id_vat_tu_thay_the
            WHERE d.trang_thai IN ('chua_lay','thieu')
              AND (d.so_luong_thuc_nhan IS NULL OR d.so_luong_thuc_nhan < d.so_luong_de_nghi)
              AND NOT EXISTS (
                  SELECT 1 FROM kho_dot_de_nghi_ct d2
                  WHERE d2.id_phieu_sp_vat_tu=d.id_phieu_sp_vat_tu
                    AND d2.id>d.id
              )
            ORDER BY dot.ma_dot, d.id");
    }

    public async Task<string> GenMaDotAsync()
    {
        using var c = _db.CreateConnection();
        var today = DateTime.Today.ToString("yyyyMMdd");
        var count = await c.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM kho_dot_de_nghi WHERE DATE(ngay_tao)=CURDATE()");
        return $"OFFSET SAUIN-{today}-{(count + 1):000}";
    }
}

// ── Phiếu XK TC ──────────────────────────────────────────────
public interface IPhieuXKTCRepository
{
    Task<IEnumerable<PhieuXKTC>> GetAllAsync();
    Task<PhieuXKTC?> GetByIdAsync(int id);
    Task<int> CreateAsync(PhieuXKTC e, IDbTransaction? txn = null);
    Task<IEnumerable<PhieuXKTCCt>> GetChiTietAsync(int idPhieu);
    Task<int> CreateChiTietAsync(PhieuXKTCCt e, IDbTransaction? txn = null);
    Task UpdateChiTietTrangThaiAsync(int id, string trangThai, string? lyDo);
    Task DeleteAsync(int id);
}

public class PhieuXKTCRepository : IPhieuXKTCRepository
{
    private readonly IDbConnectionFactory _db;
    public PhieuXKTCRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<PhieuXKTC>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<PhieuXKTC>(@"SELECT id Id,so_phieu SoPhieu,ngay_xuat NgayXuat,
            bo_phan_nhan BoPhanNhan,ly_do_xuat LyDoXuat,file_pdf_path FilePdfPath,
            nguoi_upload NguoiUpload,created_at CreatedAt
            FROM kho_phieu_xk_tc ORDER BY ngay_xuat DESC,created_at DESC");
    }

    public async Task<PhieuXKTC?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<PhieuXKTC>(@"SELECT id Id,so_phieu SoPhieu,ngay_xuat NgayXuat,
            bo_phan_nhan BoPhanNhan,ly_do_xuat LyDoXuat,file_pdf_path FilePdfPath,
            nguoi_upload NguoiUpload,created_at CreatedAt
            FROM kho_phieu_xk_tc WHERE id=@id", new { id });
    }

    public async Task<int> CreateAsync(PhieuXKTC e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_xk_tc
            (so_phieu,ngay_xuat,bo_phan_nhan,ly_do_xuat,file_pdf_path,nguoi_upload)
            VALUES(@SoPhieu,@NgayXuat,@BoPhanNhan,@LyDoXuat,@FilePdfPath,@NguoiUpload);SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_xk_tc
            (so_phieu,ngay_xuat,bo_phan_nhan,ly_do_xuat,file_pdf_path,nguoi_upload)
            VALUES(@SoPhieu,@NgayXuat,@BoPhanNhan,@LyDoXuat,@FilePdfPath,@NguoiUpload);SELECT LAST_INSERT_ID();", e);
    }

    public async Task<IEnumerable<PhieuXKTCCt>> GetChiTietAsync(int idPhieu)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<PhieuXKTCCt>(@"SELECT ct.id Id,ct.id_phieu_xk_tc IdPhieuXkTc,
            ct.id_vat_tu IdVatTu,ct.id_phieu_sp IdPhieuSp,ct.id_nha_cung_cap IdNhaCungCap,ct.mo_ta_vat_tu MoTaVatTu,
            ct.so_luong_chung_tu SoLuongChungTu,ct.so_luong_thuc_te SoLuongThucTe,
            ct.don_vi_tinh DonViTinh,ct.trang_thai TrangThai,ct.ly_do_bo_qua LyDoBoQua,
            COALESCE(v.ten_vt,'') TenVt,COALESCE(p.so_phieu,'') SoPhieuSp,COALESCE(n.ten_ncc,'') TenNcc
            FROM kho_phieu_xk_tc_ct ct
            LEFT JOIN kho_vat_tu v ON v.id=ct.id_vat_tu
            LEFT JOIN kho_phieu_sp p ON p.id=ct.id_phieu_sp
            LEFT JOIN kho_nha_cung_cap n ON n.id=ct.id_nha_cung_cap
            WHERE ct.id_phieu_xk_tc=@idPhieu", new { idPhieu });
    }

    public async Task<int> CreateChiTietAsync(PhieuXKTCCt e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_xk_tc_ct
            (id_phieu_xk_tc,id_vat_tu,id_phieu_sp,id_nha_cung_cap,mo_ta_vat_tu,so_luong_chung_tu,so_luong_thuc_te,don_vi_tinh,trang_thai)
            VALUES(@IdPhieuXkTc,@IdVatTu,@IdPhieuSp,@IdNhaCungCap,@MoTaVatTu,@SoLuongChungTu,@SoLuongThucTe,@DonViTinh,@TrangThai);
            SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_xk_tc_ct
            (id_phieu_xk_tc,id_vat_tu,id_phieu_sp,id_nha_cung_cap,mo_ta_vat_tu,so_luong_chung_tu,so_luong_thuc_te,don_vi_tinh,trang_thai)
            VALUES(@IdPhieuXkTc,@IdVatTu,@IdPhieuSp,@IdNhaCungCap,@MoTaVatTu,@SoLuongChungTu,@SoLuongThucTe,@DonViTinh,@TrangThai);
            SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateChiTietTrangThaiAsync(int id, string trangThai, string? lyDo)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_phieu_xk_tc_ct SET trang_thai=@trangThai,ly_do_bo_qua=@lyDo WHERE id=@id",
            new { id, trangThai, lyDo });
    }

    public async Task DeleteAsync(int id)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("DELETE FROM kho_phieu_xk_tc_ct WHERE id_phieu_xk_tc=@id", new { id });
        await c.ExecuteAsync("DELETE FROM kho_phieu_xk_tc WHERE id=@id", new { id });
    }
}

// ── Trả Lại Kho ──────────────────────────────────────────────
public interface ITraLaiKhoRepository
{
    Task<IEnumerable<PhieuTraLai>> GetAllAsync();
    Task<PhieuTraLai?> GetByIdAsync(int id);
    Task<int> CreateAsync(PhieuTraLai e, IDbTransaction? txn = null);
    Task<IEnumerable<PhieuTraLaiCt>> GetChiTietAsync(int idPhieu);
    Task<int> CreateChiTietAsync(PhieuTraLaiCt e, IDbTransaction? txn = null);
}

public class TraLaiKhoRepository : ITraLaiKhoRepository
{
    private readonly IDbConnectionFactory _db;
    public TraLaiKhoRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<PhieuTraLai>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<PhieuTraLai>(@"SELECT id Id,so_phieu SoPhieu,ngay_tra NgayTra,
            ly_do LyDo,ghi_chu GhiChu,nguoi_tao NguoiTao,created_at CreatedAt
            FROM kho_phieu_tra_lai ORDER BY ngay_tra DESC");
    }

    public async Task<PhieuTraLai?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<PhieuTraLai>(@"SELECT id Id,so_phieu SoPhieu,ngay_tra NgayTra,
            ly_do LyDo,ghi_chu GhiChu,nguoi_tao NguoiTao,created_at CreatedAt
            FROM kho_phieu_tra_lai WHERE id=@id", new { id });
    }

    public async Task<int> CreateAsync(PhieuTraLai e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_tra_lai(so_phieu,ngay_tra,ly_do,ghi_chu,nguoi_tao)
            VALUES(@SoPhieu,@NgayTra,@LyDo,@GhiChu,@NguoiTao);SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_tra_lai(so_phieu,ngay_tra,ly_do,ghi_chu,nguoi_tao)
            VALUES(@SoPhieu,@NgayTra,@LyDo,@GhiChu,@NguoiTao);SELECT LAST_INSERT_ID();", e);
    }

    public async Task<IEnumerable<PhieuTraLaiCt>> GetChiTietAsync(int idPhieu)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<PhieuTraLaiCt>(@"SELECT ct.id Id,ct.id_phieu_tra_lai IdPhieuTraLai,
            ct.id_vat_tu IdVatTu,ct.id_phieu_sp IdPhieuSp,ct.so_luong SoLuong,ct.ghi_chu GhiChu,
            v.ten_vt TenVt,v.don_vi_tinh DonViTinh,COALESCE(p.so_phieu,'') SoPhieuSp
            FROM kho_phieu_tra_lai_ct ct
            JOIN kho_vat_tu v ON v.id=ct.id_vat_tu
            LEFT JOIN kho_phieu_sp p ON p.id=ct.id_phieu_sp
            WHERE ct.id_phieu_tra_lai=@idPhieu", new { idPhieu });
    }

    public async Task<int> CreateChiTietAsync(PhieuTraLaiCt e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_tra_lai_ct(id_phieu_tra_lai,id_vat_tu,id_phieu_sp,so_luong,ghi_chu)
            VALUES(@IdPhieuTraLai,@IdVatTu,@IdPhieuSp,@SoLuong,@GhiChu);SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_phieu_tra_lai_ct(id_phieu_tra_lai,id_vat_tu,id_phieu_sp,so_luong,ghi_chu)
            VALUES(@IdPhieuTraLai,@IdVatTu,@IdPhieuSp,@SoLuong,@GhiChu);SELECT LAST_INSERT_ID();", e);
    }
}

// ── Giấy Tiết Kiệm ───────────────────────────────────────────
public interface IGiayTietKiemRepository
{
    Task<IEnumerable<GiayTietKiem>> GetAllAsync();
    Task<GiayTietKiem?> GetByIdAsync(int id);
    Task<int> CreateAsync(GiayTietKiem e);
    Task UpdateAsync(GiayTietKiem e);
    Task DeleteAsync(int id);
}

public class GiayTietKiemRepository : IGiayTietKiemRepository
{
    private readonly IDbConnectionFactory _db;
    public GiayTietKiemRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<GiayTietKiem>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<GiayTietKiem>(@"SELECT g.id Id,g.id_phieu_sp IdPhieuSp,g.id_vat_tu_cuon IdVatTuCuon,
            g.kg_yeu_cau KgYeuCau,g.kg_thuc_xa KgThucXa,g.kg_tiet_kiem KgTietKiem,
            g.ten_thanh_pham_bu TenThanhPhamBu,g.so_bat SoBat,g.so_to_quy_doi SoToQuyDoi,
            g.ghi_chu GhiChu,g.nguoi_tao NguoiTao,g.created_at CreatedAt,
            p.so_phieu SoPhieu,p.ten_san_pham TenSanPham,v.ten_vt TenVt
            FROM kho_giay_tiet_kiem g
            JOIN kho_phieu_sp p ON p.id=g.id_phieu_sp
            JOIN kho_vat_tu v ON v.id=g.id_vat_tu_cuon
            ORDER BY g.created_at DESC");
    }

    public async Task<GiayTietKiem?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<GiayTietKiem>(@"SELECT g.id Id,g.id_phieu_sp IdPhieuSp,g.id_vat_tu_cuon IdVatTuCuon,
            g.kg_yeu_cau KgYeuCau,g.kg_thuc_xa KgThucXa,g.kg_tiet_kiem KgTietKiem,
            g.ten_thanh_pham_bu TenThanhPhamBu,g.so_bat SoBat,g.so_to_quy_doi SoToQuyDoi,
            g.ghi_chu GhiChu,g.nguoi_tao NguoiTao,g.created_at CreatedAt,
            p.so_phieu SoPhieu,p.ten_san_pham TenSanPham,v.ten_vt TenVt
            FROM kho_giay_tiet_kiem g
            JOIN kho_phieu_sp p ON p.id=g.id_phieu_sp
            JOIN kho_vat_tu v ON v.id=g.id_vat_tu_cuon
            WHERE g.id=@id", new { id });
    }

    public async Task<int> CreateAsync(GiayTietKiem e)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_giay_tiet_kiem
            (id_phieu_sp,id_vat_tu_cuon,kg_yeu_cau,kg_thuc_xa,ten_thanh_pham_bu,so_bat,so_to_quy_doi,ghi_chu,nguoi_tao)
            VALUES(@IdPhieuSp,@IdVatTuCuon,@KgYeuCau,@KgThucXa,@TenThanhPhamBu,@SoBat,@SoToQuyDoi,@GhiChu,@NguoiTao);
            SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateAsync(GiayTietKiem e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"UPDATE kho_giay_tiet_kiem SET id_phieu_sp=@IdPhieuSp,id_vat_tu_cuon=@IdVatTuCuon,
            kg_yeu_cau=@KgYeuCau,kg_thuc_xa=@KgThucXa,ten_thanh_pham_bu=@TenThanhPhamBu,
            so_bat=@SoBat,so_to_quy_doi=@SoToQuyDoi,ghi_chu=@GhiChu WHERE id=@Id", e);
    }

    public async Task DeleteAsync(int id)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("DELETE FROM kho_giay_tiet_kiem WHERE id=@id", new { id });
    }
}

// ── Khuôn Bế ─────────────────────────────────────────────────
public interface IKhuonBeRepository
{
    Task<IEnumerable<KhuonBe>> GetAllAsync(string? trangThai = null);
    Task<KhuonBe?> GetByIdAsync(int id);
    Task<IEnumerable<BaoCaoKhuonBeNccRow>> GetBaoCaoTheoNccAsync();
    Task<IEnumerable<KhuonBeCanhBaoRow>> GetKhuonCanChuYAsync(decimal nguongTyLe = 80);
    Task<int> GetMaxPhienBanByVatTuAsync(int? idVatTu, string tenKhuon);
    Task<int> CreateAsync(KhuonBe e);
    Task UpdateAsync(KhuonBe e);
    Task AddSoToInAsync(int id, decimal soTo);
    Task<IEnumerable<KhuonBeLichSu>> GetLichSuAsync(int idKhuon);
    Task CreateLichSuAsync(KhuonBeLichSu e);
}

public class KhuonBeRepository : IKhuonBeRepository
{
    private readonly IDbConnectionFactory _db;
    public KhuonBeRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<KhuonBe>> GetAllAsync(string? trangThai = null)
    {
        using var c = _db.CreateConnection();
        var sql = @"SELECT k.id Id,k.id_vat_tu IdVatTu,k.ten_khuon TenKhuon,k.ma_khuon MaKhuon,k.phien_ban PhienBan,
            k.id_nha_cung_cap IdNhaCungCap,k.id_phieu_sp_dau IdPhieuSpDau,k.ngay_bat_dau NgayBatDau,
            k.trang_thai TrangThai,k.so_to_da_in SoToDaIn,k.dinh_muc_tuoi_tho DinhMucTuoiTho,
            k.ngay_hong NgayHong,k.ghi_chu GhiChu,k.created_at CreatedAt,
            COALESCE(v.ten_vt,'') TenVatTu,
            COALESCE(n.ten_ncc,'') TenNcc,COALESCE(p.so_phieu,'') SoPhieuDau,
            CASE WHEN k.dinh_muc_tuoi_tho>0 THEN ROUND(k.so_to_da_in/k.dinh_muc_tuoi_tho*100,1) ELSE 0 END PhanTramTuoiTho
            FROM kho_khuon_be k
            LEFT JOIN kho_vat_tu v ON v.id=k.id_vat_tu
            LEFT JOIN kho_nha_cung_cap n ON n.id=k.id_nha_cung_cap
            LEFT JOIN kho_phieu_sp p ON p.id=k.id_phieu_sp_dau
            WHERE 1=1";
        if (!string.IsNullOrEmpty(trangThai)) sql += " AND k.trang_thai=@trangThai";
        sql += " ORDER BY k.ten_khuon,k.phien_ban DESC";
        return await c.QueryAsync<KhuonBe>(sql, new { trangThai });
    }

    public async Task<KhuonBe?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<KhuonBe>(@"SELECT k.id Id,k.id_vat_tu IdVatTu,k.ten_khuon TenKhuon,k.ma_khuon MaKhuon,k.phien_ban PhienBan,
            k.id_nha_cung_cap IdNhaCungCap,k.id_phieu_sp_dau IdPhieuSpDau,k.ngay_bat_dau NgayBatDau,
            k.trang_thai TrangThai,k.so_to_da_in SoToDaIn,k.dinh_muc_tuoi_tho DinhMucTuoiTho,
            k.ngay_hong NgayHong,k.ghi_chu GhiChu,k.created_at CreatedAt,
            COALESCE(v.ten_vt,'') TenVatTu,
            COALESCE(n.ten_ncc,'') TenNcc,COALESCE(p.so_phieu,'') SoPhieuDau,
            CASE WHEN k.dinh_muc_tuoi_tho>0 THEN ROUND(k.so_to_da_in/k.dinh_muc_tuoi_tho*100,1) ELSE 0 END PhanTramTuoiTho
            FROM kho_khuon_be k
            LEFT JOIN kho_vat_tu v ON v.id=k.id_vat_tu
            LEFT JOIN kho_nha_cung_cap n ON n.id=k.id_nha_cung_cap
            LEFT JOIN kho_phieu_sp p ON p.id=k.id_phieu_sp_dau
            WHERE k.id=@id", new { id });
    }

    public async Task<IEnumerable<BaoCaoKhuonBeNccRow>> GetBaoCaoTheoNccAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<BaoCaoKhuonBeNccRow>(@"SELECT
            k.id_nha_cung_cap IdNhaCungCap,
            COALESCE(n.ten_ncc,'Chưa chọn NCC') TenNcc,
            COUNT(*) TongSoKhuon,
            SUM(CASE WHEN k.trang_thai='dang_dung' THEN 1 ELSE 0 END) SoKhuonDangDung,
            SUM(CASE WHEN k.trang_thai='hong' THEN 1 ELSE 0 END) SoKhuonHong,
            COALESCE(SUM(k.so_to_da_in),0) TongSoToDaIn,
            COALESCE(AVG(k.so_to_da_in),0) TuoiThoTrungBinh,
            COALESCE(AVG(CASE WHEN k.dinh_muc_tuoi_tho > 0 THEN k.dinh_muc_tuoi_tho END),0) DinhMucTrungBinh,
            CASE WHEN COALESCE(SUM(CASE WHEN k.dinh_muc_tuoi_tho > 0 THEN k.dinh_muc_tuoi_tho ELSE 0 END),0) > 0
                THEN ROUND(SUM(k.so_to_da_in) / SUM(CASE WHEN k.dinh_muc_tuoi_tho > 0 THEN k.dinh_muc_tuoi_tho ELSE 0 END) * 100, 1)
                ELSE 0 END TyLeSuDungDinhMuc,
            SUM(CASE WHEN k.dinh_muc_tuoi_tho > 0 THEN 1 ELSE 0 END) SoKhuonCoDinhMuc,
            SUM(CASE
                WHEN k.trang_thai='hong' THEN 1
                WHEN k.dinh_muc_tuoi_tho > 0 AND k.so_to_da_in / k.dinh_muc_tuoi_tho >= 0.8 THEN 1
                WHEN COALESCE(k.so_to_da_in,0) = 0 THEN 1
                ELSE 0 END) SoKhuonCanChuY
            FROM kho_khuon_be k
            LEFT JOIN kho_nha_cung_cap n ON n.id=k.id_nha_cung_cap
            GROUP BY k.id_nha_cung_cap, COALESCE(n.ten_ncc,'Chưa chọn NCC')
            ORDER BY TyLeSuDungDinhMuc DESC, TongSoToDaIn DESC, TenNcc");
    }

    public async Task<IEnumerable<KhuonBeCanhBaoRow>> GetKhuonCanChuYAsync(decimal nguongTyLe = 80)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<KhuonBeCanhBaoRow>(@"SELECT
            k.id Id,
            k.ten_khuon TenKhuon,
            k.ma_khuon MaKhuon,
            k.phien_ban PhienBan,
            COALESCE(n.ten_ncc,'Chưa chọn NCC') TenNcc,
            k.trang_thai TrangThai,
            k.so_to_da_in SoToDaIn,
            k.dinh_muc_tuoi_tho DinhMucTuoiTho,
            CASE WHEN k.dinh_muc_tuoi_tho > 0 THEN ROUND(k.so_to_da_in / k.dinh_muc_tuoi_tho * 100, 1) ELSE 0 END PhanTramTuoiTho,
            CASE
                WHEN k.trang_thai='hong' AND k.dinh_muc_tuoi_tho > 0 AND k.so_to_da_in < k.dinh_muc_tuoi_tho THEN 'Hỏng trước định mức'
                WHEN k.trang_thai='hong' THEN 'Đã hỏng'
                WHEN k.dinh_muc_tuoi_tho > 0 AND k.so_to_da_in / k.dinh_muc_tuoi_tho * 100 >= @nguongTyLe THEN 'Sắp chạm/vượt định mức'
                WHEN COALESCE(k.so_to_da_in,0) = 0 THEN 'Chưa ghi nhận sử dụng'
                ELSE 'Cần rà soát' END LyDoCanhBao
            FROM kho_khuon_be k
            LEFT JOIN kho_nha_cung_cap n ON n.id=k.id_nha_cung_cap
            WHERE k.trang_thai='hong'
               OR (k.dinh_muc_tuoi_tho > 0 AND k.so_to_da_in / k.dinh_muc_tuoi_tho * 100 >= @nguongTyLe)
               OR COALESCE(k.so_to_da_in,0) = 0
            ORDER BY
                CASE WHEN k.trang_thai='hong' AND k.dinh_muc_tuoi_tho > 0 AND k.so_to_da_in < k.dinh_muc_tuoi_tho THEN 0
                     WHEN k.trang_thai='hong' THEN 1
                     WHEN k.dinh_muc_tuoi_tho > 0 THEN 2
                     ELSE 3 END,
                PhanTramTuoiTho DESC,
                k.ten_khuon", new { nguongTyLe });
    }

    public async Task<int> GetMaxPhienBanByVatTuAsync(int? idVatTu, string tenKhuon)
    {
        using var c = _db.CreateConnection();
        if (idVatTu.HasValue)
            return await c.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(phien_ban),0) FROM kho_khuon_be WHERE id_vat_tu=@idVatTu", new { idVatTu });
        return await c.ExecuteScalarAsync<int>("SELECT COALESCE(MAX(phien_ban),0) FROM kho_khuon_be WHERE LOWER(TRIM(ten_khuon))=LOWER(TRIM(@tenKhuon))", new { tenKhuon });
    }

    public async Task<int> CreateAsync(KhuonBe e)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_khuon_be
            (id_vat_tu,ten_khuon,ma_khuon,phien_ban,id_nha_cung_cap,id_phieu_sp_dau,ngay_bat_dau,trang_thai,dinh_muc_tuoi_tho,ghi_chu)
            VALUES(@IdVatTu,@TenKhuon,@MaKhuon,@PhienBan,@IdNhaCungCap,@IdPhieuSpDau,@NgayBatDau,@TrangThai,@DinhMucTuoiTho,@GhiChu);
            SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateAsync(KhuonBe e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"UPDATE kho_khuon_be SET id_vat_tu=@IdVatTu,ten_khuon=@TenKhuon,ma_khuon=@MaKhuon,
            id_nha_cung_cap=@IdNhaCungCap,id_phieu_sp_dau=@IdPhieuSpDau,ngay_bat_dau=@NgayBatDau,
            trang_thai=@TrangThai,dinh_muc_tuoi_tho=@DinhMucTuoiTho,ngay_hong=@NgayHong,ghi_chu=@GhiChu
            WHERE id=@Id", e);
    }

    public async Task AddSoToInAsync(int id, decimal soTo)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_khuon_be SET so_to_da_in=so_to_da_in+@soTo WHERE id=@id", new { id, soTo });
    }

    public async Task<IEnumerable<KhuonBeLichSu>> GetLichSuAsync(int idKhuon)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<KhuonBeLichSu>(@"SELECT l.id Id,l.id_khuon_be IdKhuonBe,l.id_phieu_sp IdPhieuSp,
            l.so_to_in SoToIn,l.ngay_su_dung NgaySuDung,l.ghi_chu GhiChu,l.created_at CreatedAt,
            p.so_phieu SoPhieu,p.ten_san_pham TenSanPham
            FROM kho_khuon_be_lich_su l
            JOIN kho_phieu_sp p ON p.id=l.id_phieu_sp
            WHERE l.id_khuon_be=@idKhuon ORDER BY l.ngay_su_dung DESC", new { idKhuon });
    }

    public async Task CreateLichSuAsync(KhuonBeLichSu e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"INSERT INTO kho_khuon_be_lich_su(id_khuon_be,id_phieu_sp,so_to_in,ngay_su_dung,ghi_chu)
            VALUES(@IdKhuonBe,@IdPhieuSp,@SoToIn,@NgaySuDung,@GhiChu)", e);
    }
}

// ── Định Mức ─────────────────────────────────────────────────
public interface IDinhMucRepository
{
    Task<IEnumerable<DinhMucTieuHao>> GetAllDinhMucAsync();
    Task<DinhMucTieuHao?> GetDinhMucByIdAsync(int id);
    Task<int> CreateDinhMucAsync(DinhMucTieuHao e);
    Task UpdateDinhMucAsync(DinhMucTieuHao e);
    Task DeleteDinhMucAsync(int id);
    Task<IEnumerable<TieuHaoThucTe>> GetTieuHaoAsync(int? nam = null);
    Task<int> CreateTieuHaoAsync(TieuHaoThucTe e, IDbTransaction? txn = null);
    Task DeleteTieuHaoAsync(int id);
}

public class DinhMucRepository : IDinhMucRepository
{
    private readonly IDbConnectionFactory _db;
    public DinhMucRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<DinhMucTieuHao>> GetAllDinhMucAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<DinhMucTieuHao>(@"SELECT d.id Id,d.id_vat_tu IdVatTu,d.loai_dinh_muc LoaiDinhMuc,
            d.ten_san_pham TenSanPham,d.dinh_muc_tren_1000_to DinhMucTren1000To,
            d.dinh_muc_theo_ten_sp DinhMucTheoTenSp,
            d.don_vi_tinh DonViTinh,d.ghi_chu GhiChu,d.is_active IsActive,d.created_at CreatedAt,v.ten_vt TenVt
            FROM kho_dinh_muc_tieu_hao d JOIN kho_vat_tu v ON v.id=d.id_vat_tu ORDER BY v.ten_vt");
    }

    public async Task<DinhMucTieuHao?> GetDinhMucByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<DinhMucTieuHao>(@"SELECT d.id Id,d.id_vat_tu IdVatTu,d.loai_dinh_muc LoaiDinhMuc,
            d.ten_san_pham TenSanPham,d.dinh_muc_tren_1000_to DinhMucTren1000To,
            d.dinh_muc_theo_ten_sp DinhMucTheoTenSp,
            d.don_vi_tinh DonViTinh,d.ghi_chu GhiChu,d.is_active IsActive,d.created_at CreatedAt,v.ten_vt TenVt
            FROM kho_dinh_muc_tieu_hao d JOIN kho_vat_tu v ON v.id=d.id_vat_tu WHERE d.id=@id", new { id });
    }

    public async Task<int> CreateDinhMucAsync(DinhMucTieuHao e)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_dinh_muc_tieu_hao
            (id_vat_tu,loai_dinh_muc,ten_san_pham,dinh_muc_tren_1000_to,dinh_muc_theo_ten_sp,don_vi_tinh,ghi_chu,is_active)
            VALUES(@IdVatTu,@LoaiDinhMuc,@TenSanPham,@DinhMucTren1000To,@DinhMucTheoTenSp,@DonViTinh,@GhiChu,@IsActive);
            SELECT LAST_INSERT_ID();", e);
    }

    public async Task UpdateDinhMucAsync(DinhMucTieuHao e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"UPDATE kho_dinh_muc_tieu_hao SET id_vat_tu=@IdVatTu,loai_dinh_muc=@LoaiDinhMuc,
            ten_san_pham=@TenSanPham,dinh_muc_tren_1000_to=@DinhMucTren1000To,
            dinh_muc_theo_ten_sp=@DinhMucTheoTenSp,
            don_vi_tinh=@DonViTinh,ghi_chu=@GhiChu,is_active=@IsActive WHERE id=@Id", e);
    }

    public async Task DeleteDinhMucAsync(int id)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("DELETE FROM kho_dinh_muc_tieu_hao WHERE id=@id", new { id });
    }

    public async Task<IEnumerable<TieuHaoThucTe>> GetTieuHaoAsync(int? nam = null)
    {
        using var c = _db.CreateConnection();
        var sql = @"SELECT t.id Id,t.id_vat_tu IdVatTu,t.id_phieu_xk_tc IdPhieuXkTc,
            t.thang_in_thuc_te ThangInThucTe,t.so_to_in SoToIn,t.so_luong_tieu_hao SoLuongTieuHao,
            t.ghi_chu GhiChu,t.created_at CreatedAt,v.ten_vt TenVt,
            COALESCE(x.so_phieu,'') SoPhieuXk
            FROM kho_tieu_hao_thuc_te t
            JOIN kho_vat_tu v ON v.id=t.id_vat_tu
            LEFT JOIN kho_phieu_xk_tc x ON x.id=t.id_phieu_xk_tc
            WHERE 1=1";
        if (nam.HasValue) sql += " AND YEAR(t.thang_in_thuc_te)=@nam";
        sql += " ORDER BY t.thang_in_thuc_te DESC";
        return await c.QueryAsync<TieuHaoThucTe>(sql, new { nam });
    }

    public async Task<int> CreateTieuHaoAsync(TieuHaoThucTe e, IDbTransaction? txn = null)
    {
        if (txn != null)
            return await txn.Connection!.ExecuteScalarAsync<int>(@"INSERT INTO kho_tieu_hao_thuc_te
            (id_vat_tu,id_phieu_xk_tc,thang_in_thuc_te,so_to_in,so_luong_tieu_hao,ghi_chu)
            VALUES(@IdVatTu,@IdPhieuXkTc,@ThangInThucTe,@SoToIn,@SoLuongTieuHao,@GhiChu);
            SELECT LAST_INSERT_ID();", e, transaction: txn);
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_tieu_hao_thuc_te
            (id_vat_tu,id_phieu_xk_tc,thang_in_thuc_te,so_to_in,so_luong_tieu_hao,ghi_chu)
            VALUES(@IdVatTu,@IdPhieuXkTc,@ThangInThucTe,@SoToIn,@SoLuongTieuHao,@GhiChu);
            SELECT LAST_INSERT_ID();", e);
    }

    public async Task DeleteTieuHaoAsync(int id)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("DELETE FROM kho_tieu_hao_thuc_te WHERE id=@id", new { id });
    }
}

// ── Tồn Kho ──────────────────────────────────────────────────
public interface ITonKhoRepository
{
    Task<IEnumerable<LichSuTon>> GetLichSuAsync(int idVatTu, DateTime? tuNgay = null, DateTime? denNgay = null);
    Task<IEnumerable<LichSuTon>> GetRecentAsync(int top = 20);
    Task CreateLichSuAsync(LichSuTon e, IDbTransaction? txn = null);
    Task<decimal> GetTonKyTruocAsync(int idVatTu, DateTime truocNgay);
    Task<Dictionary<string, decimal>> GetBaoCaoNhapXuatAsync(DateTime tuNgay, DateTime denNgay);
}

public class TonKhoRepository : ITonKhoRepository
{
    private readonly IDbConnectionFactory _db;
    public TonKhoRepository(IDbConnectionFactory db) => _db = db;

    public async Task<IEnumerable<LichSuTon>> GetLichSuAsync(int idVatTu, DateTime? tuNgay = null, DateTime? denNgay = null)
    {
        using var c = _db.CreateConnection();
        var sql = @"SELECT l.id Id,l.id_vat_tu IdVatTu,l.ngay_giao_dich NgayGiaoDich,
            l.loai_gd LoaiGd,l.so_luong SoLuong,l.ton_truoc TonTruoc,l.ton_sau TonSau,
            l.ghi_chu GhiChu,l.created_at CreatedAt,v.ten_vt TenVt,v.don_vi_tinh DonViTinh
            FROM kho_lich_su_ton l JOIN kho_vat_tu v ON v.id=l.id_vat_tu
            WHERE 1=1";
        if (idVatTu > 0) sql += " AND l.id_vat_tu=@idVatTu";
        if (tuNgay.HasValue) sql += " AND l.ngay_giao_dich>=@tuNgay";
        if (denNgay.HasValue) sql += " AND l.ngay_giao_dich<=@denNgay";
        sql += " ORDER BY l.ngay_giao_dich DESC";
        return await c.QueryAsync<LichSuTon>(sql, new { idVatTu, tuNgay, denNgay });
    }

    public async Task<IEnumerable<LichSuTon>> GetRecentAsync(int top = 20)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<LichSuTon>($@"SELECT l.id Id,l.id_vat_tu IdVatTu,l.ngay_giao_dich NgayGiaoDich,
            l.loai_gd LoaiGd,l.so_luong SoLuong,l.ton_truoc TonTruoc,l.ton_sau TonSau,
            l.ghi_chu GhiChu,l.created_at CreatedAt,v.ten_vt TenVt,v.don_vi_tinh DonViTinh
            FROM kho_lich_su_ton l JOIN kho_vat_tu v ON v.id=l.id_vat_tu
            ORDER BY l.created_at DESC LIMIT {top}");
    }

    public async Task CreateLichSuAsync(LichSuTon e, IDbTransaction? txn = null)
    {
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync(@"INSERT INTO kho_lich_su_ton
            (id_vat_tu,ngay_giao_dich,loai_gd,so_luong,ton_truoc,ton_sau,id_phieu_xk_tc,id_phieu_sp,id_phieu_tra_lai,ghi_chu)
            VALUES(@IdVatTu,@NgayGiaoDich,@LoaiGd,@SoLuong,@TonTruoc,@TonSau,@IdPhieuXkTc,@IdPhieuSp,@IdPhieuTraLai,@GhiChu)",
            new { e.IdVatTu, e.NgayGiaoDich, e.LoaiGd, e.SoLuong, e.TonTruoc, e.TonSau,
                  IdPhieuXkTc = (int?)null, IdPhieuSp = (int?)null, IdPhieuTraLai = (int?)null, e.GhiChu }, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"INSERT INTO kho_lich_su_ton
            (id_vat_tu,ngay_giao_dich,loai_gd,so_luong,ton_truoc,ton_sau,id_phieu_xk_tc,id_phieu_sp,id_phieu_tra_lai,ghi_chu)
            VALUES(@IdVatTu,@NgayGiaoDich,@LoaiGd,@SoLuong,@TonTruoc,@TonSau,@IdPhieuXkTc,@IdPhieuSp,@IdPhieuTraLai,@GhiChu)",
            new { e.IdVatTu, e.NgayGiaoDich, e.LoaiGd, e.SoLuong, e.TonTruoc, e.TonSau,
                  IdPhieuXkTc = (int?)null, IdPhieuSp = (int?)null, IdPhieuTraLai = (int?)null, e.GhiChu });
    }

    public async Task<decimal> GetTonKyTruocAsync(int idVatTu, DateTime truocNgay)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<decimal>(@"SELECT COALESCE(SUM(so_luong),0)
            FROM kho_lich_su_ton
            WHERE id_vat_tu=@idVatTu AND ngay_giao_dich<=@truocNgay", new { idVatTu, truocNgay });
    }

    public async Task<Dictionary<string, decimal>> GetBaoCaoNhapXuatAsync(DateTime tuNgay, DateTime denNgay)
    {
        using var c = _db.CreateConnection();
        var result = await c.QueryAsync<(int id, string loai, decimal sl)>(@"
            SELECT id_vat_tu, loai_gd, SUM(so_luong) FROM kho_lich_su_ton
            WHERE ngay_giao_dich BETWEEN @tuNgay AND @denNgay GROUP BY id_vat_tu, loai_gd",
            new { tuNgay, denNgay });
        return result.ToDictionary(x => $"{x.id}_{x.loai}", x => x.sl);
    }
}
