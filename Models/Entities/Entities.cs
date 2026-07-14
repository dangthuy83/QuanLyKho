namespace KhoQuanLy.Models.Entities;

public class NhomVatTu
{
    public int Id { get; set; }
    public string TenNhom { get; set; } = "";
    public string? MoTa { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VatTu
{
    public int Id { get; set; }
    public string MaVt { get; set; } = "";
    public string TenVt { get; set; } = "";
    public int? IdNhom { get; set; }
    public string TenNhom { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public string LoaiVatTu { get; set; } = "khac";
    public bool LaVatTuDichDanh { get; set; }
    public bool CanTheoDoiTieuHao { get; set; }
    public decimal TonKhoHienTai { get; set; }
    public decimal TonKhoToiThieu { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class NhaCungCap
{
    public int Id { get; set; }
    public string TenNcc { get; set; } = "";
    public string? DienThoai { get; set; }
    public string? DiaChiNcc { get; set; }
    public string? GhiChu { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class BoPhan
{
    public int Id { get; set; }
    public string TenBoPhan { get; set; } = "";
    public string? GhiChu { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public class PhieuSP
{
    public int Id { get; set; }
    public string SoPhieu { get; set; } = "";
    public string? SoLsx { get; set; }
    public string TenSanPham { get; set; } = "";
    public string? MaSanPham { get; set; }
    public string? KhachHang { get; set; }
    public decimal? SoLuongSp { get; set; }
    public DateTime? NgayLenh { get; set; }
    public DateTime? NgayGiaoHang { get; set; }
    public string TrangThai { get; set; } = "chua_hoan_thanh";
    public string? FilePdfPath { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PhieuSPVatTu
{
    public int Id { get; set; }
    public int IdPhieuSp { get; set; }
    public int? IdVatTu { get; set; }
    public int? IdVatTuThayThe { get; set; }
    public string TenVt { get; set; } = "";
    public string MaVt { get; set; } = "";
    public string TenVtThayThe { get; set; } = "";
    public string MaVtThayThe { get; set; } = "";
    public string DonViTinhThayThe { get; set; } = "";
    public string? ChungLoaiText { get; set; }
    public string? KichThuoc { get; set; }
    public decimal SoLuongYeuCau { get; set; }
    public decimal? SoLuongToRoi { get; set; }
    public string? DonViTinh { get; set; }
    public bool LaCuonThayToRoi { get; set; }
    public decimal SoLuongDaNhan { get; set; }
    public string TrangThaiNhan { get; set; } = "chua_nhan";
    public string? GhiChuKhongCan { get; set; }
    public string? GhiChu { get; set; }
}

public class PhieuSPLichSuSua
{
    public int Id { get; set; }
    public int IdPhieuSp { get; set; }
    public int? IdVatTu { get; set; }
    public string TenVt { get; set; } = "";
    public decimal? SoLuongCu { get; set; }
    public decimal? SoLuongMoi { get; set; }
    public string? LyDo { get; set; }
    public string? NguoiSua { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DotDeNghi
{
    public int Id { get; set; }
    public string MaDot { get; set; } = "";
    public DateTime NgayTao { get; set; }
    public string? NguoiTao { get; set; }
    public string TrangThai { get; set; } = "cho_lay";
    public string? GhiChu { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DotDeNghiCt
{
    public int Id { get; set; }
    public int IdDot { get; set; }
    public int IdPhieuSpVatTu { get; set; }
    public decimal? SoLuongDeNghi { get; set; }
    public decimal TonKyTruoc { get; set; }
    public decimal? SoLuongThucNhan { get; set; }
    public string TrangThai { get; set; } = "chua_lay";
    public string? GhiChu { get; set; }
    // Joined fields
    public string SoPhieu { get; set; } = "";
    public string SoPhieuNgan 
    {
        get 
        {
            if (string.IsNullOrWhiteSpace(SoPhieu)) return "";
            
            // Cắt chuỗi theo dấu '/'
            var parts = SoPhieu.Split('/');
            
            // Kiểm tra nếu mảng sau khi cắt có phần tử thì lấy phần tử đầu tiên
            // Nếu không (trường hợp hiếm gặp) thì trả về chuỗi gốc
            return parts.Length > 0 ? parts[0] : SoPhieu; 
        }
    }
    public string TenSanPham { get; set; } = "";
    public string? ChungLoaiText { get; set; }
    public string? KichThuoc { get; set; }
    public decimal SoLuongYeuCau { get; set; }
    public string TenVt { get; set; } = "";
    public int? IdVatTu { get; set; }
    public int? IdVatTuThayThe { get; set; }
    public string TenVtThayThe { get; set; } = "";
    public string MaVtThayThe { get; set; } = "";
    public string DonViTinhThayThe { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    // Joined field from previous dots gộp
    public string MaDot { get; set; } = "";
}

public class PhieuXKTC
{
    public int Id { get; set; }
    public string SoPhieu { get; set; } = "";
    public DateTime NgayXuat { get; set; }
    public string? BoPhanNhan { get; set; }
    public string? LyDoXuat { get; set; }
    public string? FilePdfPath { get; set; }
    public string? NguoiUpload { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PhieuXKTCCt
{
    public int Id { get; set; }
    public int IdPhieuXkTc { get; set; }
    public int? IdVatTu { get; set; }
    public int? IdPhieuSp { get; set; }
    public int? IdNhaCungCap { get; set; }
    public string? MoTaVatTu { get; set; }
    public decimal SoLuongChungTu { get; set; }
    public decimal SoLuongThucTe { get; set; }
    public string? DonViTinh { get; set; }
    public string TrangThai { get; set; } = "du";
    public string? LyDoBoQua { get; set; }
    public string TenVt { get; set; } = "";
    public string SoPhieuSp { get; set; } = "";
    public string TenNcc { get; set; } = "";
}

public class PhieuTraLai
{
    public int Id { get; set; }
    public string SoPhieu { get; set; } = "";
    public DateTime NgayTra { get; set; }
    public string LyDo { get; set; } = "loi";
    public string? GhiChu { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PhieuTraLaiCt
{
    public int Id { get; set; }
    public int IdPhieuTraLai { get; set; }
    public int IdVatTu { get; set; }
    public int? IdPhieuSp { get; set; }
    public decimal SoLuong { get; set; }
    public string? GhiChu { get; set; }
    public string TenVt { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public string SoPhieuSp { get; set; } = "";
}

public class GiayTietKiem
{
    public int Id { get; set; }
    public int IdPhieuSp { get; set; }
    public int IdVatTuCuon { get; set; }
    public decimal KgYeuCau { get; set; }
    public decimal KgThucXa { get; set; }
    public decimal KgTietKiem { get; set; }
    public string? TenThanhPhamBu { get; set; }
    public decimal? SoBat { get; set; }
    public decimal? SoToQuyDoi { get; set; }
    public string? GhiChu { get; set; }
    public string? NguoiTao { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SoPhieu { get; set; } = "";
    public string TenSanPham { get; set; } = "";
    public string TenVt { get; set; } = "";
}

public class KhuonBe
{
    public int Id { get; set; }
    public int? IdVatTu { get; set; }
    public string TenKhuon { get; set; } = "";
    public string MaKhuon { get; set; } = "";
    public int PhienBan { get; set; } = 1;
    public int? IdNhaCungCap { get; set; }
    public int? IdPhieuSpDau { get; set; }
    public DateTime? NgayBatDau { get; set; }
    public string TrangThai { get; set; } = "dang_dung";
    public decimal SoToDaIn { get; set; }
    public decimal? DinhMucTuoiTho { get; set; }
    public DateTime? NgayHong { get; set; }
    public string? GhiChu { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TenVatTu { get; set; } = "";
    public string TenNcc { get; set; } = "";
    public string SoPhieuDau { get; set; } = "";
    public decimal PhanTramTuoiTho { get; set; }
}

public class KhuonBeLichSu
{
    public int Id { get; set; }
    public int IdKhuonBe { get; set; }
    public int IdPhieuSp { get; set; }
    public decimal SoToIn { get; set; }
    public DateTime? NgaySuDung { get; set; }
    public string? GhiChu { get; set; }
    public DateTime CreatedAt { get; set; }
    public string SoPhieu { get; set; } = "";
    public string TenSanPham { get; set; } = "";
}

public class DinhMucTieuHao
{
    public int Id { get; set; }
    public int IdVatTu { get; set; }
    public string LoaiDinhMuc { get; set; } = "theo_to_in";
    public string? TenSanPham { get; set; }
    public decimal? DinhMucTren1000To { get; set; }
    public decimal? DinhMucTheoTenSp { get; set; }
    public string? DonViTinh { get; set; }
    public string? GhiChu { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public string TenVt { get; set; } = "";
}

public class TieuHaoThucTe
{
    public int Id { get; set; }
    public int IdVatTu { get; set; }
    public int? IdPhieuXkTc { get; set; }
    public DateTime ThangInThucTe { get; set; }
    public decimal? SoToIn { get; set; }
    public decimal SoLuongTieuHao { get; set; }
    public string? GhiChu { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TenVt { get; set; } = "";
    public string SoPhieuXk { get; set; } = "";
}

public class LichSuTon
{
    public int Id { get; set; }
    public int IdVatTu { get; set; }
    public DateTime NgayGiaoDich { get; set; }
    public string LoaiGd { get; set; } = "";
    public decimal SoLuong { get; set; }
    public decimal? TonTruoc { get; set; }
    public decimal? TonSau { get; set; }
    public string? GhiChu { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TenVt { get; set; } = "";
    public string DonViTinh { get; set; } = "";
}
