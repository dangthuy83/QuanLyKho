using System.ComponentModel.DataAnnotations;
using KhoQuanLy.Models.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace KhoQuanLy.Models.ViewModels;

// ── Dashboard ────────────────────────────────────────────────
public class DashboardVM
{
    public int TongVatTu { get; set; }
    public int VatTuTonThap { get; set; }
    public int PhieuSpHoanThanh { get; set; }
    public int KhuonBeDangDung { get; set; }
    public int DotDeNghiChoLay { get; set; }
    public List<LichSuTon> GiaoDichGanDay { get; set; } = new();
}

// ── Nhóm VT ─────────────────────────────────────────────────
public class NhomVatTuVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên nhóm")]
    [Display(Name = "Tên nhóm")]
    public string TenNhom { get; set; } = "";
    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }
}

// ── Vật Tư ──────────────────────────────────────────────────
public class VatTuVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập mã vật tư")]
    [Display(Name = "Mã vật tư")]
    public string MaVt { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập tên vật tư")]
    [Display(Name = "Tên vật tư")]
    public string TenVt { get; set; } = "";
    [Display(Name = "Nhóm vật tư")]
    public int? IdNhom { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập đơn vị tính")]
    [Display(Name = "Đơn vị tính")]
    public string DonViTinh { get; set; } = "";
    [Display(Name = "Loại vật tư")]
    public string LoaiVatTu { get; set; } = "khac";
    [Display(Name = "Vật tư đích danh")]
    public bool LaVatTuDichDanh { get; set; }
    [Display(Name = "Theo dõi tiêu hao")]
    public bool CanTheoDoiTieuHao { get; set; }
    [Display(Name = "Tồn kho tối thiểu")]
    public decimal TonKhoToiThieu { get; set; }
    public bool IsActive { get; set; } = true;
    public IEnumerable<SelectListItem>? NhomList { get; set; }
}

// ── NCC ──────────────────────────────────────────────────────
public class NhaCungCapVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên NCC")]
    [Display(Name = "Tên nhà cung cấp")]
    public string TenNcc { get; set; } = "";
    [Display(Name = "Điện thoại")]
    public string? DienThoai { get; set; }
    [Display(Name = "Địa chỉ")]
    public string? DiaChiNcc { get; set; }
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Bộ phận ──────────────────────────────────────────────────
public class BoPhanVM
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Vui lòng nhập tên bộ phận")]
    [Display(Name = "Tên bộ phận")]
    public string TenBoPhan { get; set; } = "";
    [Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }
    public bool IsActive { get; set; } = true;
}

// ── Phiếu SP ─────────────────────────────────────────────────
public class PhieuSPListVM
{
    public List<PhieuSP> Items { get; set; } = new();
    public string? FilterTrangThai { get; set; }
    public string? FilterSearch { get; set; }
}

public class PhieuSPDetailVM
{
    public PhieuSP? PhieuSP { get; set; }
    public List<PhieuSPVatTu> VatTus { get; set; } = new();
    public List<PhieuSPLichSuSua> LichSuSua { get; set; } = new();
    public IEnumerable<SelectListItem> VatTuCuonList { get; set; } = new List<SelectListItem>();
}

public class PhieuSPVatTuEditVM
{
    [Required] public int Id { get; set; }
    [Required] public decimal SoLuongYeuCau { get; set; }
    public decimal? SoLuongToRoi { get; set; }
    public string? KichThuoc { get; set; }
    public string? ChungLoaiText { get; set; }
    public string? DonViTinh { get; set; }
    public int? IdVatTu { get; set; }
    public int? IdVatTuThayThe { get; set; }
    public bool LaCuonThayToRoi { get; set; }
    public string? LyDo { get; set; }
    public string? NguoiSua { get; set; }
    public string? GhiChu { get; set; }
}

// ── Đợt đề nghị ──────────────────────────────────────────────
public class DotDeNghiCreateVM
{
    public string? MaDot { get; set; }
    public string? GhiChu { get; set; }
    /// <summary>IdPhieuSpVatTu được chọn từ danh sách PSP mới</summary>
    public List<int> IdVatTuCtList { get; set; } = new();
    /// <summary>Id chi tiết còn thiếu từ đợt trước (đã có IdDotCt)</summary>
    public List<int> IdPendingCtList { get; set; } = new();
    // Các field cũ (tương thích)
    public List<int> IdPhieuSpVatTuList { get; set; } = new();
    public string? NguoiTao { get; set; }
}

public class DotDeNghiDetailVM
{
    public DotDeNghi? Dot { get; set; }
    public List<DotDeNghiCt> ChiTiets { get; set; } = new();
    // Các field cũ (tương thích)
    public DotDeNghi? DotDeNghi { get; set; }
    public List<DotDeNghiCt> ChiTiet { get; set; } = new();
}

// ── Nhập kho TC ──────────────────────────────────────────────
public class PhieuXKTCVM
{
    public int Id { get; set; }
    [Required] public string SoPhieu { get; set; } = "";
    [Required] public DateTime NgayXuat { get; set; } = DateTime.Today;
    [Display(Name = "Bộ phận nhận")] public string? BoPhanNhan { get; set; }
    public string? LyDoXuat { get; set; }
    public string? FilePdfPath { get; set; }
    public IEnumerable<SelectListItem> BoPhanList { get; set; } = new List<SelectListItem>();
}

public class PhieuXKTCDetailVM
{
    public PhieuXKTC? Phieu { get; set; }
    public List<PhieuXKTCCt> ChiTiets { get; set; } = new();
    // Các field cũ (tương thích)
    public PhieuXKTC? PhieuXKTC { get; set; }
    public List<PhieuXKTCCt> ChiTiet { get; set; } = new();
}

// ── Trả lại kho ──────────────────────────────────────────────
public class PhieuTraLaiVM
{
    public int Id { get; set; }
    [Required] public string SoPhieu { get; set; } = "";
    public DateTime NgayTra { get; set; } = DateTime.Today;
    public string LyDo { get; set; } = "loi";
    public string? GhiChu { get; set; }
    public List<PhieuTraLaiCtVM> ChiTiets { get; set; } = new();
    // Các field cũ (tương thích)
    public string? NguoiTao { get; set; }
    public List<PhieuTraLaiCtVM> ChiTiet { get; set; } = new();
}

public class PhieuTraLaiCtVM
{
    public int IdVatTu { get; set; }
    public int? IdPhieuSp { get; set; }
    public decimal SoLuong { get; set; }
    public string? GhiChu { get; set; }
}

// ── Giấy tiết kiệm ───────────────────────────────────────────
public class GiayTietKiemVM
{
    public int Id { get; set; }
    public int? IdPhieuSp { get; set; }
    public int? IdVatTu { get; set; }
    public decimal SoLuong { get; set; }
    public string? GhiChu { get; set; }
    // Các field cũ (tương thích)
    public IEnumerable<SelectListItem>? PhieuSpList { get; set; }
    public IEnumerable<SelectListItem>? VatTuCuonList { get; set; }
    public int? IdVatTuCuon { get; set; }
    public decimal? KgYeuCau { get; set; }
    public decimal? KgThucXa { get; set; }
    public string? TenThanhPhamBu { get; set; }
    public int? SoBat { get; set; }
    public decimal? SoToQuyDoi { get; set; }
}

// ── Khuôn bế ─────────────────────────────────────────────────
public class KhuonBeVM
{
    public int Id { get; set; }
    [Display(Name = "Mã khuôn")] public string MaKhuon { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập tên khuôn")]
    [Display(Name = "Tên khuôn")] public string TenKhuon { get; set; } = "";
    public int PhienBan { get; set; } = 1;
    [Display(Name = "Vật tư liên kết")] public int? IdVatTu { get; set; }
    [Display(Name = "Nhà cung cấp")] public int? IdNhaCungCap { get; set; }
    public string? MoTa { get; set; }
    [Display(Name = "Định mức tuổi thọ")] public decimal? DinhMucTuoiTho { get; set; }
    public string TrangThai { get; set; } = "dang_dung";
    public string? GhiChu { get; set; }
    public string? MoTaGhiChu { get; set; }
    // Các field cũ (tương thích)
    public IEnumerable<SelectListItem>? VatTuKhuonBeList { get; set; }
    public IEnumerable<SelectListItem>? NccList { get; set; }
    public IEnumerable<SelectListItem>? PhieuSpList { get; set; }
    public int? IdPhieuSpDau { get; set; }
    public DateTime? NgayBatDau { get; set; }
}

public class KhuonBeLichSuVM
{
    public int IdKhuonBe { get; set; }
    public int? IdPhieuSp { get; set; }
    public decimal SoToIn { get; set; }
    public string? GhiChu { get; set; }
    // Các field cũ (tương thích)
    public DateTime? NgaySuDung { get; set; }
}

// ── Định mức & Tiêu hao ──────────────────────────────────────
public class DinhMucVM
{
    public int Id { get; set; }
    [Display(Name = "Vật tư")] public int IdVatTu { get; set; }
    [Display(Name = "Nhóm sản phẩm")] public string? NhomSanPham { get; set; }
    [Display(Name = "Tên sản phẩm")] public string? TenSanPham { get; set; }
    [Display(Name = "Định mức (theo 1000 SP)")] public decimal? DinhMuc { get; set; }
    [Display(Name = "Định mức (theo tên SP)")] public decimal? DinhMucTheoTenSp { get; set; }
    public IEnumerable<SelectListItem> LoaiDinhMucList { get; set; } = new List<SelectListItem>();
    // Các field cũ (tương thích)
    public bool IsActive { get; set; } = true;
    public IEnumerable<SelectListItem>? VatTuList { get; set; }
    public string? LoaiDinhMuc { get; set; }
    public decimal? DinhMucTren1000To { get; set; }
    public string? DonViTinh { get; set; }
    public string? GhiChu { get; set; }
}

public class TieuHaoVM
{
    public int Id { get; set; }
    public int IdDinhMuc { get; set; }
    public int IdPhieuSp { get; set; }
    public decimal SoLuongTieuHao { get; set; }
    public DateTime NgayGhiNhan { get; set; } = DateTime.Today;
    public string? GhiChu { get; set; }
    // Các field cũ (tương thích)
    public int? IdVatTu { get; set; }
    public int? IdPhieuXkTc { get; set; }
    public DateTime? ThangInThucTe { get; set; }
    public decimal? SoToIn { get; set; }
    public IEnumerable<SelectListItem>? VatTuList { get; set; }
    public IEnumerable<SelectListItem>? PhieuXkList { get; set; }
}

// ── Tồn kho ──────────────────────────────────────────────────
public class TonKhoVM
{
    public List<VatTu> Items { get; set; } = new();
    public string? FilterLoai { get; set; }
    public string? FilterSearch { get; set; }
    public List<VatTu> CanhBaoThap { get; set; } = new();
    // Các field cũ (tương thích)
    public List<VatTu> DanhSach { get; set; } = new();
    public List<VatTu> CanhBaoTonThap { get; set; } = new();
}

public class LichSuTonVM
{
    public VatTu? VatTu { get; set; }
    public List<LichSuTon> Items { get; set; } = new();
    // Các field cũ (tương thích)
    public int IdVatTu { get; set; }
    public string TenVt { get; set; } = "";
    public List<LichSuTon> LichSu { get; set; } = new();
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
}

// ── Báo cáo ──────────────────────────────────────────────────
public class BaoCaoNhapXuatVM
{
    public DateTime TuNgay { get; set; } = new DateTime(DateTime.Today.Year, 1, 1);
    public DateTime DenNgay { get; set; } = DateTime.Today;
    public int? IdVatTu { get; set; }
    public List<BaoCaoNhapXuatRow> Items { get; set; } = new();
    // Các field cũ (tương thích)
    public List<BaoCaoNhapXuatRow> Rows { get; set; } = new();
}

public class BaoCaoNhapXuatRow
{
    public int IdVatTu { get; set; }
    public string TenVt { get; set; } = "";
    public string MaVt { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public decimal TonDauKy { get; set; }
    public decimal TongNhap { get; set; }
    public decimal TongXuat { get; set; }
    public decimal TongTraLai { get; set; }
    public decimal TonCuoiKy { get; set; }
}

public class BaoCaoTieuHaoVM
{
    public int? IdVatTu { get; set; }
    public int Nam { get; set; } = DateTime.Today.Year;
    public List<BaoCaoTieuHaoRow> Items { get; set; } = new();
    // Các field cũ (tương thích)
    public List<BaoCaoTieuHaoRow> Rows { get; set; } = new();
}

public class BaoCaoTieuHaoRow
{
    public int IdVatTu { get; set; }
    public string TenVt { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public decimal[] Thang { get; set; } = new decimal[12];
    public decimal Tong { get; set; }
    // Các field cũ (tương thích) - các tháng riêng lẻ
    public decimal T1 { get => Thang.Length > 0 ? Thang[0] : 0; set { if (Thang.Length > 0) Thang[0] = value; } }
    public decimal T2 { get => Thang.Length > 1 ? Thang[1] : 0; set { if (Thang.Length > 1) Thang[1] = value; } }
    public decimal T3 { get => Thang.Length > 2 ? Thang[2] : 0; set { if (Thang.Length > 2) Thang[2] = value; } }
    public decimal T4 { get => Thang.Length > 3 ? Thang[3] : 0; set { if (Thang.Length > 3) Thang[3] = value; } }
    public decimal T5 { get => Thang.Length > 4 ? Thang[4] : 0; set { if (Thang.Length > 4) Thang[4] = value; } }
    public decimal T6 { get => Thang.Length > 5 ? Thang[5] : 0; set { if (Thang.Length > 5) Thang[5] = value; } }
    public decimal T7 { get => Thang.Length > 6 ? Thang[6] : 0; set { if (Thang.Length > 6) Thang[6] = value; } }
    public decimal T8 { get => Thang.Length > 7 ? Thang[7] : 0; set { if (Thang.Length > 7) Thang[7] = value; } }
    public decimal T9 { get => Thang.Length > 8 ? Thang[8] : 0; set { if (Thang.Length > 8) Thang[8] = value; } }
    public decimal T10 { get => Thang.Length > 9 ? Thang[9] : 0; set { if (Thang.Length > 9) Thang[9] = value; } }
    public decimal T11 { get => Thang.Length > 10 ? Thang[10] : 0; set { if (Thang.Length > 10) Thang[10] = value; } }
    public decimal T12 { get => Thang.Length > 11 ? Thang[11] : 0; set { if (Thang.Length > 11) Thang[11] = value; } }
}

// ── Báo cáo Khuôn bế ─────────────────────────────────────────
public class BaoCaoKhuonBeNccVM
{
    public List<BaoCaoKhuonBeNccRow> TheoNcc { get; set; } = new();
    public List<KhuonBeCanhBaoRow> CanhBaos { get; set; } = new();
    // Các field cũ (tương thích)
    public List<BaoCaoKhuonBeNccRow> Rows { get; set; } = new();
    public List<KhuonBeCanhBaoRow> CanhBao { get; set; } = new();
    public int TongKhuon { get; set; }
    public int TongDangDung { get; set; }
    public int TongHong { get; set; }
    public decimal TongSoToDaIn { get; set; }
}

public class BaoCaoKhuonBeNccRow
{
    public int? IdNhaCungCap { get; set; }
    public string TenNcc { get; set; } = "";
    public int TongSoKhuon { get; set; }
    public int SoKhuonDangDung { get; set; }
    public int SoKhuonHong { get; set; }
    public decimal TongSoToDaIn { get; set; }
    public decimal TuoiThoTrungBinh { get; set; }
    public decimal DinhMucTrungBinh { get; set; }
    public decimal TyLeSuDungDinhMuc { get; set; }
    public int SoKhuonCoDinhMuc { get; set; }
    public int SoKhuonCanChuY { get; set; }
}

public class KhuonBeCanhBaoRow
{
    public int Id { get; set; }
    public string TenKhuon { get; set; } = "";
    public string MaKhuon { get; set; } = "";
    public int PhienBan { get; set; }
    public string TenNcc { get; set; } = "";
    public string TrangThai { get; set; } = "";
    public decimal SoToDaIn { get; set; }
    public decimal? DinhMucTuoiTho { get; set; }
    public decimal PhanTramTuoiTho { get; set; }
    public string LyDoCanhBao { get; set; } = "";
}

// ── Settings ──────────────────────────────────────────────────
public class SettingsVM
{
    [Display(Name = "Đường dẫn lưu file PDF PSP")]
    public string? PspPdfPath { get; set; }
    public string? PspPdfPathThucTe { get; set; }
    public string? ThongBao { get; set; }
    // Các field cũ (tương thích)
    public string? ActualPath { get; set; }
}

// ── Excel Preview (Import PSP) ──────────────────────────────
public class ExcelPreviewRow
{
    /// <summary>Số thứ tự dòng trong file Excel (để trace lỗi)</summary>
    public int RowIndex { get; set; }
    // Header
    public string SoPhieu { get; set; } = "";
    public string TenSanPham { get; set; } = "";
    public string? NgayLenh { get; set; }
    public string? KhachHang { get; set; }
    public decimal? SoLuongSp { get; set; }
    // Vật tư
    public string TenVatTu { get; set; } = "";
    public string? KichThuoc { get; set; }
    public decimal? SoLuongToRoi { get; set; }
    public decimal SoLuongYeuCau { get; set; }
    public string DonViTinh { get; set; } = "Kg";
    // Trạng thái
    public string? CanhBao { get; set; }
    public bool DaTonTai { get; set; }
    public bool DuocChon { get; set; } = true;
}

public class ExcelPreviewVM
{
    public string TenFile { get; set; } = "";
    public int TongDong { get; set; }
    public int SoPsp { get; set; }
    public List<ExcelPreviewRow> Rows { get; set; } = new();
}