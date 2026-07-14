# Kiến trúc chi tiết — KhoQuanLy

## 1. Cấu trúc thư mục

```
KhoQuanLy/
├── Controllers/           # 10 controllers (đã xóa PhieuXKTCController)
│   ├── HomeController.cs  — Dashboard
│   ├── DanhMucController.cs — Quản lý danh mục (4 CRUD)
│   ├── PhieuSPController.cs — Phiếu sản phẩm
│   ├── DeNghiXuatController.cs — Đợt đề nghị xuất giấy
│   ├── NhapKhoTaiChinhController.cs — Nhập kho TC (giao diện người dùng)
│   ├── TraLaiKhoController.cs — Trả lại kho
│   ├── GiayTietKiemController.cs — Giấy tiết kiệm
│   ├── KhuonBeController.cs — Khuôn bế
│   ├── DinhMucController.cs — Định mức & tiêu hao
│   ├── TonKhoController.cs — Tồn kho
│   └── BaoCaoController.cs — Báo cáo
├── Models/
│   ├── Entities/Entities.cs     # 16 entity class
│   └── ViewModels/ViewModels.cs # 25 ViewModel/row class
├── Repositories/
│   ├── DbConnectionFactory.cs   # Interface + MySqlConnectionFactory
│   ├── DanhMucRepositories.cs   # INhomVatTu, IVatTu, INhaCungCap, IBoPhan
│   └── NghiepVuRepositories.cs  # 8 repository nghiệp vụ
├── Services/
│   └── AllServices.cs           # 10 interface + 10 service class
├── Views/                       # ~57 file .cshtml
├── wwwroot/
│   ├── css/site.css             # Custom CSS
│   └── js/site.js               # JS helpers
├── Program.cs                   # Entry point + DI registration
├── appsettings.json             # Connection string
└── KhoQuanLy.csproj             # Project file
```

## 2. Controllers

| Controller | DI dependencies | Actions chính |
|---|---|---|
| HomeController | ITonKhoService, IPhieuSPService, IKhuonBeService | Index() — Dashboard |
| DanhMucController | IDanhMucService | NhomVatTu, VatTu, NhaCungCap, BoPhan — mỗi CRUD 3 action |
| PhieuSPController | IPhieuSPService, IDanhMucService, IPdfParserService, IExcelParserService, IPdfStorageService | Index, Detail, Create, Upload PDF/Excel, PreviewExcel, UpdateTrangThai, UpdateVatTuCt, XacNhanNhan |
| DeNghiXuatController | IDeNghiXuatService, IPhieuSPService | Index, Detail, Create, UpdateThucNhan, HoanThanh, In |
| NhapKhoTaiChinhController | IPhieuXKTCService, IDanhMucService | Index, Detail, Create, BoQua |
| TraLaiKhoController | ITraLaiKhoService, IDanhMucService | Index, Detail, Create |
| GiayTietKiemController | IGiayTietKiemService, IDanhMucService | Index, Form, Delete |
| KhuonBeController | IKhuonBeService, IDanhMucService | Index, Detail, Form, GhiNhanSuDung, DanhDauHong |
| DinhMucController | IDinhMucService, IDanhMucService | Index, DinhMucForm/Save/Delete, TieuHao, TieuHaoForm/Save/Delete |
| TonKhoController | ITonKhoService | Index, LichSu, XuatKhoNhanhForm |
| BaoCaoController | IBaoCaoService | Index, NhapXuat, TieuHao, KhuonBeTheoNcc, ExportNhapXuat, ExportTieuHao, ExportKhuonBeTheoNcc |

**Lưu ý**: ✅ Controller chỉ inject Service — đã fix hoàn toàn, không còn inject Repository trực tiếp.

## 3. Services (AllServices.cs)

| Interface | Service class | Phương thức chính |
|---|---|---|
| IDanhMucService | DanhMucService | CRUD cho NhómVT, VậtTư, NCC, BộPhận + các GetSelectList |
| IPhieuSPService | PhieuSPService | CRUD PSP, quản lý vật tư chi tiết, lịch sử sửa, xác nhận nhận |
| IDeNghiXuatService | DeNghiXuatService | Tạo đợt (gộp nhiều dòng VT), cập nhật thực nhận (kèm update tồn kho), hoàn thành đợt |
| IPhieuXKTCService | PhieuXKTCService | CRUD phiếu XK TC + cập nhật tồn kho + lịch sử tồn |
| ITraLaiKhoService | TraLaiKhoService | CRUD phiếu trả lại + cập nhật tồn kho |
| IGiayTietKiemService | GiayTietKiemService | CRUD giấy tiết kiệm |
| IKhuonBeService | KhuonBeService | CRUD khuôn bế, ghi nhận sử dụng, đánh dấu hỏng; khuôn/vật tư mới mặc định phiên bản 1, chỉ tạo version mới khi thay thế khuôn hỏng có chủ đích |
| IDinhMucService | DinhMucService | CRUD định mức + tiêu hao thực tế + tự động trừ tồn kho |
| ITonKhoService | TonKhoService | Xem tồn (filter), lịch sử tồn, xuất kho nhanh |
| IBaoCaoService | BaoCaoService | Báo cáo nhập/xuất (đã fix tính TonDauKy, TongNhap, TongXuat, TongTraLai), báo cáo tiêu hao, báo cáo tuổi thọ/hiệu suất khuôn bế theo NCC + export Excel |
| IExcelParserService | ExcelParserService | Parse Excel PSP `.xlsx` theo template Sheet1, header hàng 10, dữ liệu hàng 11+, trả nhiều PSP/file |

**Lưu ý**: ✅ Các Service đã nhận đủ Repository cần thiết qua DI — Controller không cần inject Repository.

## 4. Entities (16 class)

Tất cả đều trong file `Models/Entities/Entities.cs`, namespace `KhoQuanLy.Models.Entities`:

| Entity class | Bảng DB tương ứng | Ghi chú |
|---|---|---|
| NhomVatTu | kho_nhom_vat_tu | |
| VatTu | kho_vat_tu | Có trường TonKhoHienTai, TonKhoToiThieu |
| NhaCungCap | kho_nha_cung_cap | |
| BoPhan | kho_bo_phan | |
| PhieuSP | kho_phieu_sp | Trạng thái: chua_hoan_thanh (mặc định PSP mới/import PDF), hoan_thanh |
| PhieuSPVatTu | kho_phieu_sp_vat_tu | Trạng thái nhận: chua_nhan, du, thieu, khong_can |
| PhieuSPLichSuSua | kho_phieu_sp_lich_su_sua | Lịch sử thay đổi SL |
| DotDeNghi | kho_dot_de_nghi | Trạng thái: cho_lay, hoan_thanh |
| DotDeNghiCt | kho_dot_de_nghi_ct | Trạng thái: chua_lay, du, thieu |
| PhieuXKTC | kho_phieu_xk_tc | |
| PhieuXKTCCt | kho_phieu_xk_tc_ct | Trạng thái: du, thieu, da_bo_qua |
| PhieuTraLai | kho_phieu_tra_lai | |
| PhieuTraLaiCt | kho_phieu_tra_lai_ct | |
| GiayTietKiem | kho_giay_tiet_kiem | |
| KhuonBe | kho_khuon_be | |
| KhuonBeLichSu | kho_khuon_be_lich_su | |
| DinhMucTieuHao | kho_dinh_muc_tieu_hao | Thêm trường DinhMucTheoTenSp |
| TieuHaoThucTe | kho_tieu_hao_thuc_te | |
| LichSuTon | kho_lich_su_ton | loai_gd: nhap_tc, xuat_psp, xuat_tieu_hao, xuat_khac, tra_lai, dieu_chinh |

## 5. ViewModels (25 class/row model)

Tất cả đều trong file `Models/ViewModels/ViewModels.cs`, namespace `KhoQuanLy.Models.ViewModels`:

| ViewModel | Mục đích |
|---|---|
| DashboardVM | 5 stat counters (đã đổi PhieuSpChoXuat → PhieuSpHoanThanh) + giao dịch gần đây |
| NhomVatTuVM | Form nhóm VT |
| VatTuVM | Form VT (có NhomList SelectList) |
| NhaCungCapVM | Form NCC |
| BoPhanVM | Form bộ phận |
| PhieuSPListVM | Danh sách PSP kèm filter |
| PhieuSPDetailVM | Chi tiết PSP (header + danh sách VT + lịch sử sửa) |
| PhieuSPVatTuEditVM | Edit SL vật tư trong PSP |
| DotDeNghiCreateVM | Tạo đợt đề nghị (chọn danh sách Id VatTuCt) |
| DotDeNghiDetailVM | Chi tiết đợt (header + danh sách CT) |
| PhieuXKTCVM | Form phiếu XK TC (có BoPhanList) |
| PhieuXKTCDetailVM | Chi tiết phiếu XK TC |
| PhieuTraLaiVM | Form phiếu trả lại |
| PhieuTraLaiCtVM | Dòng chi tiết trả lại |
| GiayTietKiemVM | Form giấy tiết kiệm |
| KhuonBeVM | Form khuôn bế |
| KhuonBeLichSuVM | Ghi nhận sử dụng khuôn |
| DinhMucVM | Form định mức (thêm DinhMucTheoTenSp, LoaiDinhMucList) |
| TieuHaoVM | Form tiêu hao thực tế |
| TonKhoVM | Danh sách tồn kho + cảnh báo thấp + filter |
| LichSuTonVM | Lịch sử 1 vật tư |
| BaoCaoNhapXuatVM | Báo cáo nhập/xuất |
| BaoCaoNhapXuatRow | 1 dòng dữ liệu báo cáo nhập/xuất |
| BaoCaoTieuHaoVM | Báo cáo tiêu hao (12 tháng + tổng) |
| BaoCaoTieuHaoRow | 1 dòng dữ liệu báo cáo tiêu hao |
| BaoCaoKhuonBeNccVM | Báo cáo so sánh tuổi thọ/hiệu suất khuôn bế theo NCC |
| BaoCaoKhuonBeNccRow | 1 dòng tổng hợp khuôn bế theo NCC |
| KhuonBeCanhBaoRow | 1 dòng khuôn hiệu suất thấp/cần chú ý |
| ExcelPreviewVM | Preview dữ liệu import Excel PSP trước khi submit |
| ExcelPreviewRow | 1 dòng vật tư trong preview Excel PSP |

## 6. Data Access

- **Connection factory**: `IDbConnectionFactory` → `MySqlConnectionFactory`
- **Repository pattern**: Mỗi entity có interface + implementation
- **SQL**: Raw SQL với Dapper, alias thủ công
- **Transaction**: Đã có `UnitOfWork`/transaction cho các Service method tạo/sửa nhiều bảng; các query đơn giản vẫn dùng connection riêng trong Repository
- **Connection**: Repository thường dùng `using var c = _db.CreateConnection();`; các nghiệp vụ cần transaction dùng connection/transaction từ `IUnitOfWork`

## 7. DI Registration (Program.cs)

```csharp
// 12 repositories
builder.Services.AddScoped<INhomVatTuRepository, NhomVatTuRepository>(),
// ...
// 10 services
builder.Services.AddScoped<IDanhMucService, DanhMucService>(),
// ...
```

## 8. Giao diện

- **Layout**: _Layout.cshtml — sidebar navy (240px) + topbar 56px + main content
- **Bootstrap 5**: Modal, form, table, badge, button, card
- **Custom CSS**: site.css — sidebar, topbar, stat-card, .tbl, badge, toast, progress bar
- **JS**: site.js — toast(), openModal(), submitForm(), confirmAction(), postJson()
- **Tương thích mobile**: sidebar ẩn trên mobile, nút hamburger toggle

## 9. Lưu ý kiến trúc

- ✅ Controller chỉ gọi Service (đã fix hoàn toàn)
- ✅ Entity và ViewModel tách riêng
- ✅ Full DI
- ✅ Đã xóa controller trùng (PhieuXKTCController)
- ✅ Đã fix báo cáo nhập/xuất tính đúng các cột
- ✅ Đã thêm trường DinhMucTheoTenSp cho cấu hình tiêu hao mực
- ✅ Đã có UnitOfWork / transaction cho các Service method tạo/sửa nhiều bảng
- ✅ Đã siết chống tồn âm ở Service layer cho các nghiệp vụ trừ tồn chính: xuất kho nhanh, trả lại kho, tiêu hao thực tế, và sửa giảm thực nhận đề nghị xuất giấy.
- ⚠️ Chưa có AutoMapper — mapping thủ công trong Service