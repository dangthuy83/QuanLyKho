using Dapper;
using KhoQuanLy.Models.Entities;
using System.Data;

namespace KhoQuanLy.Repositories;

public interface INhomVatTuRepository
{
    Task<IEnumerable<NhomVatTu>> GetAllAsync();
    Task<NhomVatTu?> GetByIdAsync(int id);
    Task<int> CreateAsync(NhomVatTu e);
    Task UpdateAsync(NhomVatTu e);
    Task DeleteAsync(int id);
}
public class NhomVatTuRepository : INhomVatTuRepository
{
    private readonly IDbConnectionFactory _db;
    public NhomVatTuRepository(IDbConnectionFactory db) => _db = db;
    public async Task<IEnumerable<NhomVatTu>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<NhomVatTu>("SELECT id Id,ten_nhom TenNhom,mo_ta MoTa,created_at CreatedAt FROM kho_nhom_vat_tu ORDER BY ten_nhom");
    }
    public async Task<NhomVatTu?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<NhomVatTu>("SELECT id Id,ten_nhom TenNhom,mo_ta MoTa,created_at CreatedAt FROM kho_nhom_vat_tu WHERE id=@id", new{id});
    }
    public async Task<int> CreateAsync(NhomVatTu e)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>("INSERT INTO kho_nhom_vat_tu(ten_nhom,mo_ta) VALUES(@TenNhom,@MoTa);SELECT LAST_INSERT_ID();", e);
    }
    public async Task UpdateAsync(NhomVatTu e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_nhom_vat_tu SET ten_nhom=@TenNhom,mo_ta=@MoTa WHERE id=@Id", e);
    }
    public async Task DeleteAsync(int id)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("DELETE FROM kho_nhom_vat_tu WHERE id=@id", new{id});
    }
}

public interface IVatTuRepository
{
    Task<IEnumerable<VatTu>> GetAllAsync();
    Task<VatTu?> GetByIdAsync(int id);
    Task<IEnumerable<VatTu>> GetByLoaiAsync(string loai);
    Task<int> GetMaxMaVtNumberAsync();
    Task<int> CreateAsync(VatTu e);
    Task UpdateAsync(VatTu e);
    Task SetActiveAsync(int id, bool isActive);
    Task UpdateTonKhoAsync(int id, decimal delta, IDbTransaction? txn = null);
}
public class VatTuRepository : IVatTuRepository
{
    private readonly IDbConnectionFactory _db;
    public VatTuRepository(IDbConnectionFactory db) => _db = db;
    const string SEL = @"SELECT v.id Id,v.ma_vt MaVt,v.ten_vt TenVt,v.id_nhom IdNhom,
        COALESCE(n.ten_nhom,'') TenNhom,v.don_vi_tinh DonViTinh,v.loai_vat_tu LoaiVatTu,
        v.la_vat_tu_dich_danh LaVatTuDichDanh,v.can_theo_doi_tieu_hao CanTheoDoiTieuHao,
        v.ton_kho_hien_tai TonKhoHienTai,v.ton_kho_toi_thieu TonKhoToiThieu,
        v.is_active IsActive,v.created_at CreatedAt
        FROM kho_vat_tu v LEFT JOIN kho_nhom_vat_tu n ON n.id=v.id_nhom";
    public async Task<IEnumerable<VatTu>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<VatTu>(SEL + " ORDER BY v.ma_vt");
    }
    public async Task<VatTu?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<VatTu>(SEL + " WHERE v.id=@id", new{id});
    }
    public async Task<IEnumerable<VatTu>> GetByLoaiAsync(string loai)
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<VatTu>(SEL + " WHERE v.loai_vat_tu=@loai AND v.is_active=1 ORDER BY v.ten_vt", new{loai});
    }
    public async Task<int> GetMaxMaVtNumberAsync()
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"
            SELECT COALESCE(MAX(CAST(SUBSTRING(ma_vt, 3) AS UNSIGNED)), 0)
            FROM kho_vat_tu
            WHERE ma_vt REGEXP '^VT[0-9]+$'");
    }
    public async Task<int> CreateAsync(VatTu e)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>(@"INSERT INTO kho_vat_tu(ma_vt,ten_vt,id_nhom,don_vi_tinh,loai_vat_tu,la_vat_tu_dich_danh,can_theo_doi_tieu_hao,ton_kho_toi_thieu,is_active)
            VALUES(@MaVt,@TenVt,@IdNhom,@DonViTinh,@LoaiVatTu,@LaVatTuDichDanh,@CanTheoDoiTieuHao,@TonKhoToiThieu,@IsActive);SELECT LAST_INSERT_ID();", e);
    }
    public async Task UpdateAsync(VatTu e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync(@"UPDATE kho_vat_tu SET ma_vt=@MaVt,ten_vt=@TenVt,id_nhom=@IdNhom,don_vi_tinh=@DonViTinh,
            loai_vat_tu=@LoaiVatTu,la_vat_tu_dich_danh=@LaVatTuDichDanh,can_theo_doi_tieu_hao=@CanTheoDoiTieuHao,
            ton_kho_toi_thieu=@TonKhoToiThieu,is_active=@IsActive WHERE id=@Id", e);
    }
    public async Task SetActiveAsync(int id, bool isActive)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_vat_tu SET is_active=@isActive WHERE id=@id", new{id,isActive});
    }
    public async Task UpdateTonKhoAsync(int id, decimal delta, IDbTransaction? txn = null)
    {
        if (txn != null)
        {
            await txn.Connection!.ExecuteAsync("UPDATE kho_vat_tu SET ton_kho_hien_tai=ton_kho_hien_tai+@delta WHERE id=@id", new { id, delta }, transaction: txn);
            return;
        }
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_vat_tu SET ton_kho_hien_tai=ton_kho_hien_tai+@delta WHERE id=@id", new{id,delta});
    }
}

public interface INhaCungCapRepository
{
    Task<IEnumerable<NhaCungCap>> GetAllAsync();
    Task<NhaCungCap?> GetByIdAsync(int id);
    Task<int> CreateAsync(NhaCungCap e);
    Task UpdateAsync(NhaCungCap e);
    Task SetActiveAsync(int id, bool isActive);
}
public class NhaCungCapRepository : INhaCungCapRepository
{
    private readonly IDbConnectionFactory _db;
    public NhaCungCapRepository(IDbConnectionFactory db) => _db = db;
    public async Task<IEnumerable<NhaCungCap>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<NhaCungCap>("SELECT id Id,ten_ncc TenNcc,dien_thoai DienThoai,dia_chi DiaChiNcc,ghi_chu GhiChu,is_active IsActive,created_at CreatedAt FROM kho_nha_cung_cap ORDER BY ten_ncc");
    }
    public async Task<NhaCungCap?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<NhaCungCap>("SELECT id Id,ten_ncc TenNcc,dien_thoai DienThoai,dia_chi DiaChiNcc,ghi_chu GhiChu,is_active IsActive,created_at CreatedAt FROM kho_nha_cung_cap WHERE id=@id",new{id});
    }
    public async Task<int> CreateAsync(NhaCungCap e)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>("INSERT INTO kho_nha_cung_cap(ten_ncc,dien_thoai,dia_chi,ghi_chu,is_active) VALUES(@TenNcc,@DienThoai,@DiaChiNcc,@GhiChu,@IsActive);SELECT LAST_INSERT_ID();",e);
    }
    public async Task UpdateAsync(NhaCungCap e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_nha_cung_cap SET ten_ncc=@TenNcc,dien_thoai=@DienThoai,dia_chi=@DiaChiNcc,ghi_chu=@GhiChu,is_active=@IsActive WHERE id=@Id",e);
    }
    public async Task SetActiveAsync(int id, bool isActive)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_nha_cung_cap SET is_active=@isActive WHERE id=@id",new{id,isActive});
    }
}

public interface IBoPhanRepository
{
    Task<IEnumerable<BoPhan>> GetAllAsync();
    Task<BoPhan?> GetByIdAsync(int id);
    Task<int> CreateAsync(BoPhan e);
    Task UpdateAsync(BoPhan e);
    Task SetActiveAsync(int id, bool isActive);
}
public class BoPhanRepository : IBoPhanRepository
{
    private readonly IDbConnectionFactory _db;
    public BoPhanRepository(IDbConnectionFactory db) => _db = db;
    public async Task<IEnumerable<BoPhan>> GetAllAsync()
    {
        using var c = _db.CreateConnection();
        return await c.QueryAsync<BoPhan>("SELECT id Id,ten_bo_phan TenBoPhan,ghi_chu GhiChu,is_active IsActive,created_at CreatedAt FROM kho_bo_phan ORDER BY ten_bo_phan");
    }
    public async Task<BoPhan?> GetByIdAsync(int id)
    {
        using var c = _db.CreateConnection();
        return await c.QueryFirstOrDefaultAsync<BoPhan>("SELECT id Id,ten_bo_phan TenBoPhan,ghi_chu GhiChu,is_active IsActive,created_at CreatedAt FROM kho_bo_phan WHERE id=@id",new{id});
    }
    public async Task<int> CreateAsync(BoPhan e)
    {
        using var c = _db.CreateConnection();
        return await c.ExecuteScalarAsync<int>("INSERT INTO kho_bo_phan(ten_bo_phan,ghi_chu,is_active) VALUES(@TenBoPhan,@GhiChu,@IsActive);SELECT LAST_INSERT_ID();",e);
    }
    public async Task UpdateAsync(BoPhan e)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_bo_phan SET ten_bo_phan=@TenBoPhan,ghi_chu=@GhiChu,is_active=@IsActive WHERE id=@Id",e);
    }
    public async Task SetActiveAsync(int id, bool isActive)
    {
        using var c = _db.CreateConnection();
        await c.ExecuteAsync("UPDATE kho_bo_phan SET is_active=@isActive WHERE id=@id",new{id,isActive});
    }
}
