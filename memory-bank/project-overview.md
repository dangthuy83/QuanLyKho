?# Tổng quan dự án KhoQuanLy

## Giới thiệu

**KhoQuanLy** là ứng dụng web ASP.NET Core MVC quản lý kho vật tư cho **phân xưởng in ấn** (Offset). Ứng dụng chạy trên mạng LAN nội bộ, dành cho **1 người dùng** (không có đăng nhập/phân quyền).

Mục tiêu chính:
- Quản lý phiếu sản phẩm (PSP) và theo dõi vật tư đích danh
- Quản lý đề nghị xuất giấy (gộp nhiều PSP)
- Nhập kho tài chính (góc nhìn phân xưởng = nhập kho từ bên ngoài)
- Trả lại kho, giấy tiết kiệm, khuôn bế
- Định mức tiêu hao, tồn kho, báo cáo

## Stack công nghệ

| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core MVC (.NET 8) |
| Ngôn ngữ | C# |
| Database | MySQL (payroll_db) |
| ORM | Dapper 2.1.35 + Dapper.Contrib 2.0.78 |
| MySQL Driver | MySqlConnector 2.3.7 |
| UI | Bootstrap 5, Bootstrap Icons |
| Export Excel | ClosedXML 0.102.3 (✅ đang dùng) |
| Port mặc định | 5000 (HTTP) |

## Database

- **Tên DB**: `payroll_db` (dùng chung với hệ thống khác)
- **19 bảng** tiền tố `kho_` trên MySQL
- Chạy script `kho_schema.sql` để tạo bảng (đã có từ phiên thiết kế)

## Kiến trúc

Dự án áp dụng kiến trúc **3 lớp**:
1. **Controller** — Xử lý request/response, không chứa nghiệp vụ ✅ (đã fix)
2. **Service** — Business logic, gọi Repository ✅ (đã fix: Service nhận đủ Repository qua DI)
3. **Repository** — Truy cập dữ liệu qua Dapper + SQL thuần

## Quy tắc đặt tên

- Controller/Route: theo góc nhìn **người dùng cuối** (phân xưởng)
  - Ví dụ: `NhapKhoTaiChinhController` thay vì `PhieuXKTCController`
- Entity/Service/DB: giữ tên chứng từ gốc để truy vết giấy tờ thật
  - Ví dụ: Entity `PhieuXKTC` (phiếu xuất kho tài chính), Service `PhieuXKTCService`

## 10 Module nghiệp vụ

| # | Module | Controller | Route prefix |
|---|---|---|---|
| 1 | Danh mục (VT, nhóm, NCC, BP) | DanhMucController | /DanhMuc |
| 2 | Phiếu Sản Phẩm (PSP) | PhieuSPController | /PhieuSP |
| 3 | Đợt Đề Nghị Xuất Giấy | DeNghiXuatController | /DeNghiXuat |
| 4 | Nhập Kho Tài Chính | NhapKhoTaiChinhController | /NhapKhoTaiChinh |
| 5 | Trả Lại Kho | TraLaiKhoController | /TraLaiKho |
| 6 | Giấy Tiết Kiệm | GiayTietKiemController | /GiayTietKiem |
| 7 | Khuôn Bế | KhuonBeController | /KhuonBe |
| 8 | Định Mức & Tiêu Hao | DinhMucController | /DinhMuc |
| 9 | Tồn Kho & Cảnh báo | TonKhoController | /TonKho |
| 10 | Báo Cáo & Export | BaoCaoController | /BaoCao |

Ngoài ra còn có:
- HomeController — Dashboard tổng quan tại /

## Trạng thái mã nguồn hiện tại

- ✅ Build thành công (0 lỗi, 0 warning)
- ✅ Đã fix và đồng bộ trạng thái PSP: PSP mới/import PDF mặc định `chua_hoan_thanh`, sau đó chuyển sang `hoan_thanh` khi hoàn tất; `cho_xuat` là trạng thái cũ, không dùng lại
- ✅ Đã xóa controller trùng `PhieuXKTCController`, chỉ giữ `NhapKhoTaiChinhController`
- ✅ Đã chuyển toàn bộ Repository injection từ Controller vào Service layer
- ✅ Đã fix báo cáo nhập/xuất tính đúng TonDauKy, TongNhap, TongXuat, TongTraLai
- ✅ Đã thêm trường DinhMucTheoTenSp cho cấu hình tiêu hao mực (hỗ trợ cả 2 loại: theo tên SP và theo 1000 tờ)
- ✅ Đã xử lý gộp nhiều PSP trong 1 đợt đề nghị xuất giấy (UI chọn nhiều PSP + checkbox dòng vật tư)
- 🟡 Đã có code import/parse PDF cho PSP (`PdfParserService`, `PhieuSPController.Upload`, `Views/PhieuSP/Upload.cshtml`); đã bổ sung chuẩn hóa/giới hạn an toàn cho các trường header (`so_phieu`, `so_lsx`, `ten_san_pham`, `ma_san_pham`, `khach_hang`, `nguoi_tao`) để tránh lỗi `Data too long`, đã siết riêng logic lấy `khach_hang` và chặn sang vùng `Số phiếu sản phẩm con / Thông tin sản phẩm con`, siết `so_luong_sp` bằng regex bám trực tiếp sau nhãn, parse trực tiếp `7.Ngày nhận LSX` / `8.Ngày giao hàng` theo mẫu liền nhãn-ngày, thêm fallback `nguoi_tao` theo máy local khi test localhost, màn hình upload hỗ trợ import nhiều file PDF một lần, và luồng import hiện chủ động kiểm tra trùng `so_phieu` để **bỏ qua phiếu đã tồn tại** rồi hiển thị thông báo tổng hợp thân thiện thay vì văng lỗi MySQL; vẫn cần test thực tế bằng PDF mẫu và xác nhận mapping dữ liệu/vật tư
- ✅ Đã chốt mapping PDF PSP gồm **2 phần bắt buộc**: Header phiếu map vào `kho_phieu_sp`; phần **VẬT TƯ SẢN XUẤT** map trực tiếp vào `kho_phieu_sp_vat_tu`. Các trường vật tư cần lưu gồm `id_vat_tu` nếu match được danh mục, `chung_loai_text`, `kich_thuoc`, `so_luong_yeu_cau`, `so_luong_to_roi`, `don_vi_tinh`; không lưu STT, tỷ lệ xả/in/gia công, số lượng cần đạt, căn chỉnh.
- ✅ Đã chốt thiết kế nghiệp vụ tiếp theo cho import PDF PSP: bổ sung checkbox **"Sửa phiếu sản phẩm"**. Khi tick checkbox, `so_phieu` phải tồn tại; hệ thống import PDF sửa để cập nhật/merge dữ liệu phiếu cũ, ghi lịch sử, không làm mất lịch sử nhận/xuất. Hệ thống không tự tính lại số lượng vật tư theo số lượng sản phẩm; dữ liệu vật tư lấy theo PDF sửa. Nếu số đã nhận vượt số lượng yêu cầu mới thì giữ số đã nhận, trạng thái `du`, phần dư nếu cần xử lý qua module Trả Lại Kho.
- ✅ Đã chốt lưu trữ PDF PSP: file PDF sau upload lưu trên server web app hoặc ổ mạng server truy cập được; không lưu phân tán trên máy local người dùng; không hard-code đường dẫn; về lâu dài nên cho phép cấu hình đường dẫn lưu trên web app và lưu ngoài `wwwroot`, serve file qua controller.
- ✅ Đã triển khai bước đầu storage PDF PSP không hard-code: thêm `IPdfStorageService`/`PdfStorageService`, cấu hình `Storage:PspPdfPath` trong `appsettings.json`; nếu chưa cấu hình thì fallback vào `App_Data/PspPdf` ngoài `wwwroot`. `PhieuSPController.Upload` đã dùng service này và kiểm tra trùng `so_phieu` trước khi lưu file để tránh sinh file mồ côi. Build sau thay đổi: ✅ 0 lỗi, 0 warning.
- ✅ Đã triển khai import PDF PSP chế độ **Sửa phiếu sản phẩm**: checkbox trên `Views/PhieuSP/Upload.cshtml`, `PhieuSPController.Upload` nhận flag `suaPhieuSanPham`; nếu tick sửa mà không tìm thấy `so_phieu` thì báo lỗi, nếu tồn tại thì lưu PDF mới và gọi `PhieuSPService.ImportSuaPhieuAsync` để update header, merge dòng vật tư, ghi lịch sử và bảo toàn dòng vật tư đã phát sinh nhận/xuất. Build sau thay đổi: ✅ 0 lỗi, 0 warning.
- ✅ Đã siết parser vật tư PDF PSP để lưu đúng `kho_phieu_sp_vat_tu`: sửa lỗi cắt thiếu token tên vật tư khi parse ngược từ cuối dòng; xác thực token kích thước/đơn vị tính; map rõ `chung_loai_text`, `kich_thuoc`, `so_luong_yeu_cau`, `so_luong_to_roi`, `don_vi_tinh`; cải thiện match danh mục vật tư bằng chuẩn hóa tên và match gần. Build sau thay đổi: ✅ 0 lỗi, 0 warning.
- ✅ Đã test PDF PSP thật ngày 24/06/2026 và phát hiện PdfPig có mẫu không giữ xuống dòng bảng **VẬT TƯ SẢN XUẤT** làm parser cũ trả 0 dòng. Đã bổ sung fallback parse bảng vật tư dạng text liên tục trong `Services/PdfParserService.cs`; test lại parse được vật tư, giữ chữ "Giấy" trong tên, match đúng danh mục (`001017/2026/HNI-OFF` → `Giấy Duplex 230 K695 Hanchang`, `001027/2026/HNI-OFF` → `Giấy Duplex 450 K900 Hankuk`). Import sửa trên DB thật đã merge được 1 dòng vào `kho_phieu_sp_vat_tu` và ghi lịch sử. Storage đã test OK cả fallback `App_Data/PspPdf` và đường dẫn cấu hình.
- ✅ Đã audit DB thật ngày 24/06/2026 để tìm PSP có header nhưng thiếu vật tư. Ban đầu còn 2 phiếu thiếu dòng `kho_phieu_sp_vat_tu`: `001027/2026/HNI-OFF` và `001018/2026/HNI-OFF`. Đã import sửa lại bằng PDF hiện có, bổ sung vật tư thành công; audit lại còn 0 PSP thiếu vật tư. Trong quá trình import phát hiện `kho_phieu_sp_lich_su_sua.ly_do` trên DB thật ngắn hơn tài liệu, đã giới hạn nội dung lịch sử import sửa trong `PhieuSPService` để tránh lỗi `Data too long`.
- ❌ Chưa ghi nhận triển khai parser PDF cho Phiếu XKTC/Nhập kho tài chính; nếu cần import PDF cho chứng từ này phải thiết kế spec riêng và tích hợp qua `NhapKhoTaiChinhController`
- ✅ Đã hoàn thiện UI nhập/sửa flag `la_cuon_thay_to_roi` cho dòng vật tư PSP: tạo mới đã có checkbox, modal sửa dòng vật tư đã bind và lưu được flag
- ✅ Đã triển khai gộp dòng chưa lấy/thiếu từ đợt đề nghị trước sang đợt tiếp theo, theo đúng Service layer và có cơ chế tránh gộp trùng dòng đã được chuyển tiếp
- ✅ Đã chỉnh nghiệp vụ tạo đợt đề nghị xuất giấy sau khi user chốt nhầm đợt chưa nhập thực nhận: các dòng `chua_lay`/`thieu` vẫn được đưa sang khối **Dòng chưa nhận/nhận thiếu từ đợt trước** kể cả đợt cũ đã `hoan_thanh`; danh sách PSP/vật tư chọn mới được lọc để tránh chọn trùng dòng đang pending; đổi nhãn UI từ "Đợt trước còn dư" sang tên sát nghĩa hơn.
- ✅ Đã sửa đồng bộ thực nhận đề nghị xuất giấy về dòng vật tư PSP: khi nhập/sửa `so_luong_thuc_nhan`, Service cập nhật tổng `so_luong_da_nhan` và `trang_thai_nhan` của `kho_phieu_sp_vat_tu`; tồn kho/lịch sử tồn chỉ ghi theo phần chênh lệch để tránh trừ lặp. Repository đọc vật tư PSP dùng tổng thực nhận từ `kho_dot_de_nghi_ct` để xử lý cả dữ liệu cũ đã phát sinh nhưng PSP chưa được đồng bộ, nhờ đó phiếu/dòng đã nhận đủ không còn hiện ở danh sách tạo đề nghị mới.
- ✅ Đã siết lại đồng bộ thực nhận và trạng thái PSP ngày 24/06/2026: `UpdateVatTuCtAsync` của repository đã ghi cả `so_luong_da_nhan` vào `kho_phieu_sp_vat_tu`; `XacNhanNhanAsync` và `DeNghiXuatService.UpdateThucNhanAsync` đồng bộ trạng thái header `kho_phieu_sp.trang_thai='hoan_thanh'` khi tất cả dòng vật tư cần nhận đã đủ. Danh sách Phiếu SP (`GetAllAsync`) cũng tính trạng thái hiển thị từ dữ liệu chi tiết/tổng thực nhận để dữ liệu cũ đã đủ không còn hiện "Chưa HT" trên Index. Build sau fix: ✅ 0 lỗi, 0 warning.
- ✅ Đã làm rõ nghiệp vụ tồn kho ngày 25/06/2026: **Đề nghị xuất giấy** là phân xưởng đề nghị kho tổng/bên kho cấp giấy cho phân xưởng, nên khi nhập `thực nhận` thì với app quản lý kho phân xưởng đây là **nhập kho** (`nhap_de_nghi`, cộng tồn theo chênh lệch). **Xuất kho nhanh** mới là xuất từ kho phân xưởng ra sản xuất (`xuat_khac`, trừ tồn). Đã rà soát Service và cập nhật nhãn UI để tránh hiểu nhầm “đề nghị xuất” là trừ kho phân xưởng.
- ✅ Đã test end-to-end ngày 25/06/2026 bằng harness .NET gọi trực tiếp Service/Repository trên DB thật với dữ liệu test và cleanup sau test: tạo PSP chưa hoàn thành, tạo đợt đề nghị kho cấp giấy, nhập/sửa thực nhận, kiểm tra đồng bộ `kho_dot_de_nghi_ct` → `kho_phieu_sp_vat_tu`, tồn kho cộng theo chênh lệch, lịch sử `nhap_de_nghi`; tạo đợt kế tiếp từ pending không trùng; Xuất kho nhanh trừ tồn và ghi `xuat_khac`; báo cáo Nhập/Xuất tính `nhap_de_nghi` vào Tổng nhập, `xuat_khac` vào Tổng xuất. Kết quả: 19/19 kiểm tra PASS, cleanup OK.
- ✅ Audit dữ liệu cũ ngày 25/06/2026 phát hiện 3 dòng `kho_phieu_sp_vat_tu` đã có tổng thực nhận trong `kho_dot_de_nghi_ct` nhưng `so_luong_da_nhan/trang_thai_nhan` còn chưa đồng bộ. Sau khi user xác nhận đã backup/cho phép cập nhật DB thật, đã chạy script đồng bộ 3 dòng; audit lại còn 0 dòng lệch.
- ✅ Đã siết chống tồn âm ngày 25/06/2026 ở Service layer: `DeNghiXuatService.UpdateThucNhanAsync` không cho giảm thực nhận nếu phần giảm lớn hơn tồn hiện tại; `TraLaiKhoService.CreateAsync` không cho trả lại vượt tồn; `DinhMucService.SaveTieuHaoAsync` không cho ghi tiêu hao vượt tồn. Controller bắt `InvalidOperationException` để trả thông báo thân thiện. `TonKhoService.XuatKhoNhanhAsync` trước đó đã chặn xuất vượt tồn.
- ✅ Đã cải thiện UI chọn vật tư ngày 25/06/2026: màn Nhập kho tài chính ghép cột “Mô tả vật tư” và “Vật tư liên kết” thành 1 cột **Tên vật tư**, dùng ô nhập có `datalist` tìm theo mã/tên vật tư; khi chọn đúng danh mục tự set `id_vat_tu` và `don_vi_tinh` theo `kho_vat_tu`, không còn mặc định Kg. Cột **Số PSP** đã nạp danh sách PSP từ Service thay vì select rỗng. Màn tạo Đề nghị xuất giấy tự tải khối **Dòng chưa nhận/nhận thiếu từ đợt trước** khi mở trang, tránh quên bấm “Tải dữ liệu”; dữ liệu pending cũng lấy đúng ĐVT từ vật tư/PSP thay vì hard-code Kg.
- ✅ Đã sửa lỗi cảnh báo và tạo đợt ngày 25/06/2026: `_Layout.cshtml` hiển thị `TempData["Warning"]` để cảnh báo import PDF PSP về vật tư chưa có trong `kho_vat_tu` không bị ẩn. `DeNghiXuatService.TaoDotAsync` không còn bỏ qua âm thầm dòng PSP chưa liên kết `id_vat_tu`; nếu user chọn đủ 2 dòng nhưng có dòng chưa có trong danh mục thì rollback và báo rõ dòng cần bổ sung/liên kết trước khi tạo đợt, tránh tạo đợt chỉ có 1 dòng. `DeNghiXuatController.Create` bắt lỗi nghiệp vụ và hiển thị lại form. Build kiểm tra bằng output riêng `build_verify`: ✅ 0 lỗi; build mặc định bị khóa do app đang chạy PID 1544.
- ✅ Đã bổ sung tự liên kết lại vật tư ngày 25/06/2026: sau khi user tạo vật tư còn thiếu trong danh mục, hệ thống tự rà các dòng `kho_phieu_sp_vat_tu` đang `id_vat_tu IS NULL` theo `chung_loai_text` và match với `kho_vat_tu.ten_vt` trước khi mở/tải/tạo đợt đề nghị xuất giấy. Nếu match được sẽ cập nhật `id_vat_tu` và ĐVT, giúp dòng vừa bổ sung danh mục xuất hiện/tạo đợt được mà không cần import lại PDF. Build kiểm tra `build_verify`: ✅ 0 lỗi, 0 warning.
- ✅ Đã triển khai nghiệp vụ **Cấp cuộn thay tờ rời** ngày 25/06/2026: giữ vật tư gốc theo PDF ở `id_vat_tu/chung_loai_text`, thêm `id_vat_tu_thay_the` để chọn giấy cuộn thay thế/vật tư thực cấp; khi tick `la_cuon_thay_to_roi` bắt buộc chọn giấy cuộn thay thế từ danh mục `loai_vat_tu='giay_cuon'`. Đề nghị xuất, nhập thực nhận, tồn kho và báo cáo dùng vật tư thực tế theo quy tắc `id_vat_tu_thay_the ?? id_vat_tu`; UI PSP/Đề nghị/In phiếu hiển thị cả “Giấy theo PDF” và “Giấy cuộn thay thế/thực cấp”. Cần chạy script `App_Data/Sql/20260625_add_vat_tu_thay_the_psp.sql` trên DB thật trước khi dùng.
- ✅ Cải thiện module 1 Danh mục vật tư ngày 25/06/2026: đưa nút **Xuất kho nhanh** ra trực tiếp từng dòng danh sách `/DanhMuc/VatTu` thay vì chỉ nằm trong popup sửa vật tư; form thêm mới vật tư tự điền mã kế tiếp theo mẫu `VT00000` bằng cách lấy số lớn nhất hiện có `VTxxxxx` rồi tăng 1. Build kiểm tra `build_verify`: ✅ 0 lỗi, 0 warning.
- ✅ Cải thiện UI chọn vật tư/PSP ngày 25/06/2026: trong các module Phiếu Sản Phẩm, Nhập kho tài chính, Trả lại kho, Giấy tiết kiệm đã đổi các combo/select chọn **vật tư** và **phiếu sản phẩm** sang ô nhập có `datalist`; vật tư tìm theo chuỗi `mã - tên vật tư`, PSP tìm theo `số phiếu - tên sản phẩm`, UI vẫn lưu hidden id để không đổi binding/Service. Module Đề nghị xuất giấy hiện dùng bảng checkbox chọn nhiều PSP/dòng vật tư nên không đổi. Build kiểm tra `build_verify`: ✅ 0 lỗi, 0 warning.
- ✅ Đã xử lý liên kết Danh mục vật tư → Module Khuôn Bế ngày 25/06/2026: nguyên nhân là `kho_vat_tu` có nhiều vật tư `loai_vat_tu='khuon_be'` nhưng module Khuôn Bế dùng bảng nghiệp vụ riêng `kho_khuon_be`. Đã thêm cơ chế tự đồng bộ vật tư khuôn bế đang active sang `kho_khuon_be` khi mở `/KhuonBe` (tránh trùng theo mã/tên), và thêm nút icon kéo sang module Khuôn Bế trên từng dòng vật tư loại khuôn bế tại `/DanhMuc/VatTu`. Build kiểm tra `build_verify`: ✅ 0 lỗi, 0 warning.
- ✅ Đã làm rõ nghiệp vụ phiên bản Khuôn Bế ngày 25/06/2026: khuôn/vật tư mới mặc định `v1`, dù tạo từ danh mục vật tư hay tạo trực tiếp trong module Khuôn Bế; không tự tăng version theo `Tên khuôn`. Chỉ tạo version mới khi thay thế khuôn hỏng/cùng khuôn thật sự. Tạo từ `/KhuonBe` hiện có tự tạo bản ghi danh mục vật tư loại `khuon_be` nếu chưa có, nhưng liên kết vẫn là liên kết mềm theo mã/tên vì schema chưa có `kho_khuon_be.id_vat_tu`.
- ✅ Đã triển khai liên kết chặt Khuôn Bế ↔ Danh mục vật tư ngày 25/06/2026: thêm `id_vat_tu` vào Entity/ViewModel/Repository/Service/UI của `kho_khuon_be`, form Khuôn Bế cho chọn vật tư khuôn bế liên kết, danh sách/chi tiết hiển thị “Vật tư liên kết”. Thêm chức năng **Tạo version thay thế** chỉ xuất hiện khi khuôn đã `hong`; version mới dùng cùng `id_vat_tu`, tăng `phien_ban = max + 1`, mã khuôn dạng `MãCũ-V{n}`, trạng thái `dang_dung`. Script DB cần chạy: `App_Data/Sql/20260625_add_id_vat_tu_khuon_be.sql`. Build kiểm tra `build_verify`: ✅ 0 lỗi, 0 warning.
- ✅ Đã triển khai import Excel PSP ngày 30/06/2026: thêm `IExcelParserService/ExcelParserService` dùng ClosedXML đọc template `Sheet1`, header hàng 10, dữ liệu từ hàng 11; mapping cột C/D/E/F/H/K/Q/R/T vào `PhieuSP` và `PhieuSPVatTu`; chỉ lấy vật tư bắt đầu bằng `Giấy`, match danh mục để gán `id_vat_tu`/ĐVT. `PhieuSPController.Upload` nhận `.xlsx`, loop nhiều PSP trong 1 file, hỗ trợ import mới và import sửa theo checkbox hiện có; thêm API preview `/PhieuSP/PreviewExcel`, ViewModel preview, partial `_ExcelPreview.cshtml`, UI datagrid preview. Build kiểm tra: ✅ 0 lỗi, còn warning nullable cũ.

## Giai đoạn hiện tại trong roadmap

Hiện dự án đã qua giai đoạn sửa nền kiến trúc và đang ở giai đoạn **hoàn thiện nghiệp vụ cốt lõi + bổ sung tự động hóa nhập liệu/UI/báo cáo nâng cao**.

Ưu tiên hiện tại:
1. Kiểm thử import PDF PSP end-to-end bằng PDF mẫu thật: import mới, import sửa, header vào `kho_phieu_sp`, vật tư sản xuất vào `kho_phieu_sp_vat_tu`, file PDF lưu đúng storage, lịch sử sửa ghi đúng.
2. Hoàn thiện cấu hình lưu PDF trên web app: hiện đã có cấu hình `Storage:PspPdfPath` trong appsettings và service lưu file; bước sau có thể làm màn hình cấu hình nếu cần đổi trực tiếp trên UI.
3. Rà soát/siết thêm thuật toán match dòng vật tư nếu PDF thực tế có nhiều dòng cùng vật tư/kích thước.
4. Xác định/thiết kế parser PDF cho Nhập kho tài chính nếu nghiệp vụ cần.
5. Làm báo cáo so sánh tuổi thọ khuôn bế theo nhà cung cấp.

## Các file đã thay đổi trong các phiên fix

### Phiên 1-3 (Kiến trúc + Transaction + Báo cáo)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Controllers/PhieuSPController.cs` | Sửa | Đổi mặc định PSP sang `chua_hoan_thanh` cho PSP mới |
| `Controllers/HomeController.cs` | Sửa | Đổi filter `"cho_xuat"` → `"hoan_thanh"` + property `PhieuSpHoanThanh` |
| `Controllers/DeNghiXuatController.cs` | Sửa | Bỏ inject Repository, sửa filter trạng thái PSP |
| `Controllers/NhapKhoTaiChinhController.cs` | Sửa | Bỏ inject Repository, gọi Service thuần |
| `Controllers/TraLaiKhoController.cs` | Sửa | Bỏ inject Repository, gọi Service thuần |
| `Controllers/PhieuXKTCController.cs` | ❌ Xóa | Controller trùng lặp đã xóa |
| `Services/AllServices.cs` | Sửa | Thêm Repository vào 3 Service, fix báo cáo nhập/xuất, fix UpdateThucNhan, thêm DinhMucTheoTenSp, thêm transaction cho 3 method, dùng GetBaoCaoNhapXuatAsync |
| `Repositories/NghiepVuRepositories.cs` | Sửa | Thêm trường dinh_muc_theo_ten_sp, sửa GetLichSu cho idVatTu=0 |
| `Models/Entities/Entities.cs` | Sửa | Thêm DinhMucTheoTenSp vào DinhMucTieuHao; đồng bộ default PSP `chua_hoan_thanh` |
| `Models/ViewModels/ViewModels.cs` | Sửa | Thêm DinhMucTheoTenSp + LoaiDinhMucList, đổi PhieuSpChoXuat → PhieuSpHoanThanh |
| `Views/Home/Index.cshtml` | Sửa | Đổi PhieuSpChoXuat → PhieuSpHoanThanh |

### Phiên 4 (Export Excel)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/AllServices.cs` | Sửa | Thêm `using ClosedXML.Excel` + 2 method `ExportNhapXuatExcelAsync`, `ExportTieuHaoExcelAsync` vào `IBaoCaoService` + `BaoCaoService` |
| `Controllers/BaoCaoController.cs` | Sửa | Thêm 2 action `ExportNhapXuat`, `ExportTieuHao` trả file Excel |
| `Views/BaoCao/NhapXuat.cshtml` | Sửa | Thêm nút "Export Excel" kế bên nút In |
| `Views/BaoCao/TieuHao.cshtml` | Sửa | Thêm nút "Export Excel" kế bên nút In |

### Phiên 5 (Import PDF PSP — cần test xác nhận)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/PdfParserService.cs` | Thêm/Sửa | Có `IPdfParserService` và `PdfParserService.ParsePspAsync` để parse header + dòng vật tư PSP từ PDF |
| `Program.cs` | Sửa | Đăng ký DI `IPdfParserService, PdfParserService` |
| `Controllers/PhieuSPController.cs` | Sửa | Có action `Upload` GET/POST, gọi parser và tạo PSP từ PDF |
| `Views/PhieuSP/Upload.cshtml` | Thêm/Sửa | Màn hình upload PDF PSP |
| `Views/PhieuSP/Index.cshtml` | Sửa | Có nút `Import PDF` |

### Phiên 8 (Xử lý import trùng phiếu PSP)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Repositories/NghiepVuRepositories.cs` | Sửa | Thêm `GetBySoPhieuAsync` để kiểm tra PSP đã tồn tại theo `so_phieu` |
| `Services/AllServices.cs` | Sửa | Expose `GetBySoPhieuAsync` qua `IPhieuSPService`/`PhieuSPService` |
| `Controllers/PhieuSPController.cs` | Sửa | Trước khi lưu PSP import từ PDF, kiểm tra trùng `so_phieu`; nếu trùng thì bỏ qua và gom vào thông báo cuối cùng cho cả import 1 file và nhiều file |

Build sau phiên 8: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 9 (Storage PDF PSP không hard-code)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/PdfStorageService.cs` | Thêm | Thêm `IPdfStorageService`, `PdfStorageService`, `StoredPdfFile`; lưu PDF PSP theo `Storage:PspPdfPath`, fallback `App_Data/PspPdf` ngoài `wwwroot` |
| `appsettings.json` | Sửa | Thêm section `Storage:PspPdfPath` để cấu hình đường dẫn lưu PDF PSP |
| `Program.cs` | Sửa | Đăng ký DI `IPdfStorageService, PdfStorageService` |
| `Controllers/PhieuSPController.cs` | Sửa | Bỏ hard-code `wwwroot/uploads`; dùng `IPdfStorageService`; kiểm tra trùng `so_phieu` trước khi lưu file để tránh file mồ côi |

Build sau phiên 9: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 10 (Checkbox sửa phiếu khi import PDF PSP)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Views/PhieuSP/Upload.cshtml` | Sửa | Thêm checkbox `Sửa phiếu sản phẩm đã tồn tại` và mô tả nghiệp vụ |
| `Controllers/PhieuSPController.cs` | Sửa | Action `Upload` nhận thêm `bool suaPhieuSanPham`; nếu tick sửa thì kiểm tra `so_phieu` tồn tại, báo lỗi nếu không tồn tại; nếu tồn tại thì tạm thông báo đã nhận diện phiếu sửa và chưa thay đổi dữ liệu cũ |

Build sau phiên 10: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 11 (Merge import PDF PSP chế độ sửa phiếu)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Repositories/NghiepVuRepositories.cs` | Sửa | Cho `UpdateAsync`, `UpdateVatTuCtAsync`, `DeleteVatTuCtAsync` nhận transaction; thêm `HasVatTuCtPhatSinhAsync` để kiểm tra dòng vật tư đã phát sinh đợt đề nghị/đã nhận hay chưa |
| `Services/AllServices.cs` | Sửa | Thêm `IPhieuSPService.ImportSuaPhieuAsync`; update header PSP; merge dòng vật tư theo `id_vat_tu` hoặc `chung_loai_text/kich_thuoc/don_vi_tinh`; thêm dòng mới; xóa dòng cũ chưa phát sinh; giữ và đánh dấu `khong_can` dòng đã phát sinh; ghi lịch sử sửa |
| `Controllers/PhieuSPController.cs` | Sửa | Khi tick `suaPhieuSanPham`, lưu PDF mới và gọi `ImportSuaPhieuAsync`; thông báo kết quả merge |

Build sau phiên 11: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 12 (Siết parser vật tư PDF PSP)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/PdfParserService.cs` | Sửa | Sửa parser dòng vật tư để không cắt thiếu token tên vật tư; xác thực kích thước/đơn vị; map đủ field cho `kho_phieu_sp_vat_tu`; cải thiện match vật tư danh mục bằng chuẩn hóa tên và match gần |

Build sau phiên 12: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 13 (Test end-to-end PDF PSP thật + fallback parser bảng dính dòng)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/PdfParserService.cs` | Sửa | Bổ sung fallback `ParseVatTuFromContinuousText` để parse phần **VẬT TƯ SẢN XUẤT** khi PdfPig trích text bảng thành một chuỗi liên tục, không có xuống dòng. Không thay đổi `CleanMaterialName` theo hướng xóa chữ "Giấy"; vẫn chỉ khử lặp `GiấyGiấy`/`Giấy Giấy` thành `Giấy`. |

Kết quả test 24/06/2026:
- PDF `PSP001017_2026_HNI-OFF.pdf`: header parse đúng; vật tư parse được 1 dòng `Giấy Duplex 230 K695 Hanchang`, `69.5x72.5`, `498 Kg`, `4280` tờ, match `id_vat_tu=68`.
- PDF `001027_2026_HNI-OFF.pdf`: header parse đúng; vật tư parse được 1 dòng `Giấy Duplex 450 K900 Hankuk`, `90x57.5`, `3638 Kg`, `15575` tờ, match `id_vat_tu=28`.
- Test import sửa trên DB thật cho phiếu `001017/2026/HNI-OFF`: merge thêm 1 dòng vào `kho_phieu_sp_vat_tu`, trạng thái `chua_nhan`, ghi 2 dòng lịch sử sửa.
- Test storage: fallback lưu vào `App_Data/PspPdf` OK; cấu hình đường dẫn tùy chỉnh cũng lưu file OK.

Build sau phiên 13: cần chạy lại sau khi hoàn tất cập nhật tài liệu.

### Phiên 14 (Audit DB PSP thiếu vật tư + giới hạn lịch sử sửa)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/AllServices.cs` | Sửa | Thêm `BuildLichSuLyDo` và giới hạn độ dài `LyDo` khi `ImportSuaPhieuAsync` ghi `kho_phieu_sp_lich_su_sua`, tránh lỗi `Data too long` trên DB thật. |

Kết quả audit/import 24/06/2026:
- Trước audit: còn 2 PSP thiếu vật tư (`001027/2026/HNI-OFF`, `001018/2026/HNI-OFF`).
- Đã import sửa `001027/2026/HNI-OFF`: thêm `Giấy Duplex 450 K900 Hankuk 90x57.5 SL 3638 Kg`.
- Đã import sửa `001018/2026/HNI-OFF`: thêm `Giấy Duplex 230 K695 Hanchang 69.5x72.5 SL 1777 Kg`.
- Sau audit: còn 0 PSP thiếu vật tư.

Build sau phiên 14: cần chạy lại sau khi hoàn tất cập nhật tài liệu.

### Phiên 6 (UI flag cuộn thay tờ rời)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Models/ViewModels/ViewModels.cs` | Sửa | Thêm `LaCuonThayToRoi` vào `PhieuSPVatTuEditVM` |
| `Services/AllServices.cs` | Sửa | `PhieuSPService.UpdateVatTuCtAsync` cập nhật `LaCuonThayToRoi` khi sửa dòng vật tư |
| `Views/PhieuSP/Detail.cshtml` | Sửa | Modal sửa dòng vật tư có checkbox `Cấp cuộn thay tờ rời`, bind giá trị hiện tại và submit về server |

Build sau phiên 6: ✅ `dotnet build` thành công 0 lỗi, 0 warning.

### Phiên 7 (Gộp dòng còn thiếu từ đợt đề nghị trước)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Models/ViewModels/ViewModels.cs` | Sửa | Thêm `IdPendingCtList` vào `DotDeNghiCreateVM` để phân biệt dòng vật tư mới và dòng còn thiếu từ đợt cũ |
| `Repositories/NghiepVuRepositories.cs` | Sửa | Thêm `GetChiTietByIdAsync`, sửa `GetPendingFromPreviousDotsAsync` trả số lượng còn lại và lọc tránh dòng đã được chuyển tiếp |
| `Services/AllServices.cs` | Sửa | `DeNghiXuatService.TaoDotAsync` nhận danh sách pending, tạo dòng mới với số lượng còn lại; Controller không gọi Repository trực tiếp |
| `Controllers/DeNghiXuatController.cs` | Sửa | Bỏ gọi Repository qua `RequestServices`, dùng Service `GetPendingFromPreviousDotsAsync`; `Create` nhận cả dòng mới và dòng pending |
| `Views/DeNghiXuat/Create.cshtml` | Sửa | Tách `selected` và `selectedPending`, submit `IdPendingCtList`, hiển thị số lượng còn lại từ đợt cũ |
| `Controllers/DeNghiXuatController.cs` | Sửa | Lọc PSP/dòng vật tư đang chưa nhận/nhận thiếu từ đợt trước khỏi khối chọn mới để tránh tạo trùng |
| `Repositories/NghiepVuRepositories.cs` | Sửa | `GetPendingFromPreviousDotsAsync` lấy cả dòng chưa nhận/nhận thiếu thuộc đợt đã chốt hoàn thành |
| `Services/AllServices.cs` | Sửa | `DeNghiXuatService.UpdateThucNhanAsync` đồng bộ tổng thực nhận về `PhieuSPVatTu.SoLuongDaNhan/TrangThaiNhan` và cập nhật tồn kho theo delta |
| `Repositories/NghiepVuRepositories.cs` | Sửa | `GetVatTuByPhieuAsync`/`GetVatTuCtByIdAsync` đọc tổng thực nhận từ `kho_dot_de_nghi_ct` để phản ánh đúng dữ liệu cũ chưa đồng bộ |

### Phiên 15 (Đồng bộ thực nhận và trạng thái PSP đủ)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Repositories/NghiepVuRepositories.cs` | Sửa | `UpdateVatTuCtAsync` cập nhật cả `so_luong_da_nhan`; `UpdateTrangThaiNhanAsync` nhận thêm số lượng và transaction; `GetAllAsync` tính trạng thái hiển thị PSP theo dòng vật tư/tổng thực nhận để Index không còn lệch Detail. |
| `Services/AllServices.cs` | Sửa | `DeNghiXuatService.UpdateThucNhanAsync` và `PhieuSPService.XacNhanNhanAsync` đồng bộ header PSP sang `hoan_thanh` khi toàn bộ dòng cần nhận đã đủ; giữ transaction trong Service. |

Build sau phiên 15: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 16 (Làm rõ đề nghị xuất giấy là nhập kho phân xưởng)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/AllServices.cs` | Rà soát | Xác nhận `DeNghiXuatService.UpdateThucNhanAsync` đang cộng tồn bằng `chenhlech`, ghi `LoaiGd="nhap_de_nghi"`; `TonKhoService.XuatKhoNhanhAsync` mới trừ tồn và ghi `LoaiGd="xuat_khac"`. |
| `Views/DeNghiXuat/Create.cshtml` | Sửa nhãn | Đổi nhãn sang “đề nghị kho cấp giấy”, “vật tư cần nhận từ kho”, “SL cần nhận”. |
| `Views/DeNghiXuat/Detail.cshtml` | Sửa nhãn | Đổi cột “Đề nghị” thành “Đề nghị kho cấp”, cột thực nhận thành “Thực nhận/nhập kho PX”. |
| `Views/DeNghiXuat/In.cshtml` | Sửa nhãn | Đổi cột in phiếu sang “Đề nghị kho cấp”. |
| `Views/Home/Index.cshtml` | Sửa nhãn | Thêm hiển thị rõ `xuat_khac` = “Xuất kho nhanh”, `xuat_tieu_hao` = “Xuất tiêu hao”. |
| `Views/TonKho/LichSu.cshtml` | Sửa nhãn | Thêm nhãn lịch sử tồn cho `xuat_khac` và `xuat_tieu_hao`, giữ `nhap_de_nghi` là nhập. |

Build sau phiên 16: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 17 (Test E2E đề nghị xuất giấy/xuất kho nhanh/báo cáo + đồng bộ dữ liệu cũ)
| Hạng mục | Kết quả |
|---|---|
| Test E2E Đề nghị xuất giấy | PASS: thực nhận cập nhật `kho_dot_de_nghi_ct`, đồng bộ `kho_phieu_sp_vat_tu.so_luong_da_nhan/trang_thai_nhan`, cộng tồn theo chênh lệch, ghi `nhap_de_nghi` dương |
| Test sửa thực nhận | PASS: sửa 40 → 70 chỉ cộng delta +30, không cộng lặp |
| Test đợt kế tiếp pending | PASS: số còn cần nhận = `so_luong_de_nghi - so_luong_thuc_nhan`, tạo đợt mới từ pending không chọn trùng dòng PSP mới |
| Test Xuất kho nhanh | PASS: trừ tồn, ghi `xuat_khac` âm |
| Test báo cáo Nhập/Xuất | PASS: `nhap_de_nghi` vào Tổng nhập, `xuat_khac` vào Tổng xuất, tồn đầu/cuối hợp lý |
| Cleanup dữ liệu test | PASS: dữ liệu test marker `E2E_TEST_*` đã xóa sau test |
| Audit dữ liệu cũ | Phát hiện 3 dòng lệch: PSP `001017/2026/HNI-OFF`, `001027/2026/HNI-OFF`, `001018/2026/HNI-OFF` |
| Đồng bộ DB thật | Đã chạy sau xác nhận backup/cho phép: cập nhật 3 dòng `so_luong_da_nhan = SUM(so_luong_thuc_nhan)`, tính lại `trang_thai_nhan`; audit lại 0 dòng lệch |

Build sau phiên 17: cần chạy lại sau khi cập nhật tài liệu.

### Phiên 18 (Chống tồn âm + cải thiện chọn vật tư + tự tải pending)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/AllServices.cs` | Sửa | Chặn tồn âm ở các nghiệp vụ trừ tồn: giảm thực nhận đề nghị, trả lại kho, tiêu hao thực tế. |
| `Controllers/DeNghiXuatController.cs` | Sửa | `UpdateThucNhan` bắt lỗi nghiệp vụ và trả JSON `{ ok=false, msg }`; pending trả đúng `DonViTinh`. |
| `Controllers/TraLaiKhoController.cs` | Sửa | Bắt lỗi trả lại vượt tồn và hiển thị lại form kèm thông báo. |
| `Controllers/DinhMucController.cs` | Sửa | Bắt lỗi tiêu hao vượt tồn và trả JSON cho modal. |
| `Controllers/NhapKhoTaiChinhController.cs` | Sửa | Nạp `VatTuList` và `PhieuSpList` cho màn tạo nhập kho tài chính. |
| `Views/NhapKhoTaiChinh/Create.cshtml` | Sửa | Ghép 2 cột mô tả/liên kết thành cột **Tên vật tư** dùng `datalist`; tự set `idVatTu` hidden và ĐVT theo danh mục; PSP select có dữ liệu. |
| `Views/NhapKhoTaiChinh/Detail.cshtml` | Sửa | Hiển thị 1 cột **Tên vật tư**. |
| `Views/DeNghiXuat/Create.cshtml` | Sửa | Tự gọi `loadPending()` khi mở trang để hiện PSP/dòng chưa nhận, không cần bấm tải dữ liệu. |
| `Repositories/NghiepVuRepositories.cs`, `Models/Entities/Entities.cs` | Sửa | Bổ sung joined field `DonViTinh` cho `DotDeNghiCt` và query pending/chi tiết. |

Build sau phiên 18: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None` thành công 0 lỗi, 0 warning.

### Phiên 19 (Hiển thị cảnh báo import PSP và chống tạo đợt thiếu dòng)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Views/Shared/_Layout.cshtml` | Sửa | Bổ sung hiển thị `TempData["Warning"]` bằng alert vàng để các cảnh báo import PDF PSP, đặc biệt vật tư chưa có trong `kho_vat_tu`, hiện ra cho user. |
| `Services/AllServices.cs` | Sửa | `DeNghiXuatService.TaoDotAsync` kiểm tra dòng vật tư/pending có `id_vat_tu`; nếu thiếu liên kết danh mục thì gom lỗi và rollback thay vì `continue` âm thầm. Đồng thời báo lỗi khi không tạo được dòng hợp lệ nào. |
| `Controllers/DeNghiXuatController.cs` | Sửa | Tách helper nạp danh sách PSP, bắt `InvalidOperationException` khi tạo đợt và hiển thị lỗi trên form thay vì tạo đợt thiếu dòng. |
| `Views/DeNghiXuat/Create.cshtml` | Sửa | Hiển thị validation summary trên màn tạo đợt để user thấy lỗi vật tư chưa liên kết `kho_vat_tu`. |

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 1 warning do app đang chạy khóa PDB. Build mặc định không chạy được vì `KhoQuanLy.exe/KhoQuanLy.dll` đang bị process `KhoQuanLy (1544)` khóa.

### Phiên 20 (Tự liên kết lại vật tư PSP sau khi bổ sung danh mục)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/AllServices.cs` | Sửa | Thêm `IPhieuSPService.AutoLienKetVatTuChuaCoDanhMucAsync`; service rà các dòng PSP chưa có `id_vat_tu`, match tên đã parse (`chung_loai_text`) với `kho_vat_tu.ten_vt` theo cùng logic chuẩn hóa gần với parser, cập nhật `id_vat_tu` và ĐVT trong transaction. |
| `Controllers/DeNghiXuatController.cs` | Sửa | Gọi tự liên kết trước khi mở màn tạo đợt, trước API tải vật tư nhiều PSP, trước API pending và trước submit tạo đợt; nếu có dòng được liên kết khi mở trang thì báo số dòng đã liên kết. |

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 27 (Tự gán mã khuôn bế theo mã vật tư kế tiếp)

#### Mục tiêu
Module Khuôn Bế đang dùng `ma_khuon` theo mã vật tư. Khi thêm mới khuôn bế, form cần tự điền mã kế tiếp giống chức năng thêm vật tư mới, ví dụ mã vật tư lớn nhất hiện tại là `VT00528` thì khuôn mới gợi ý `VT00529`.

#### Triển khai
| File | Loại | Mô tả |
|---|---|---|
| `Controllers/KhuonBeController.cs` | Sửa | `Form(id=0)` gọi `IDanhMucService.GenerateNextMaVatTuAsync()` để gán sẵn `MaKhuon`; `Save` tự sinh mã nếu tạo mới và mã trống trước khi validate. |
| `Services/AllServices.cs` | Sửa | `KhuonBeService.CreateAsync` bổ sung lớp phòng vệ tự sinh mã `VTxxxxx` theo `IVatTuRepository.GetMaxMaVtNumberAsync()` nếu `MaKhuon` trống. |

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 28 (Đổi chọn vật tư Khuôn Bế/Định Mức sang datalist)

#### Mục tiêu
Khi danh mục vật tư nhiều, combo/select ở form thêm Khuôn Bế (`Vật tư liên kết`) và form thêm Định Mức (`Vật tư`) khó tìm kiếm. Chuyển sang input + `datalist` để người dùng gõ mã hoặc tên vật tư.

#### Làm rõ nghiệp vụ mã khuôn
Không còn xung đột với mã khuôn auto-fill:
- Nếu chọn vật tư liên kết có sẵn: UI tự đồng bộ `Mã khuôn = Mã vật tư` (`MaVt`) của vật tư được chọn; nếu tên khuôn đang trống thì tự điền theo tên vật tư.
- Nếu để trống vật tư liên kết: giữ mã khuôn auto-fill kế tiếp (`VTxxxxx`), khi lưu hệ thống tự tạo/liên kết vật tư mới theo mã/tên khuôn.

#### File thay đổi
| File | Loại | Mô tả |
|---|---|---|
| `Views/KhuonBe/_KhuonBeForm.cshtml` | Sửa UI | Đổi `IdVatTu` từ select sang input+datalist; cập nhật ghi chú nghiệp vụ; submit hidden id như cũ. |
| `Views/DinhMuc/_DinhMucForm.cshtml` | Sửa UI | Đổi `IdVatTu` từ select sang input+datalist; submit hidden id như cũ. |
| `wwwroot/js/site.js` | Sửa | Thêm helper đồng bộ datalist → hidden id, riêng Khuôn Bế đồng bộ thêm mã/tên khuôn khi chọn vật tư. |

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 29 (Bổ sung NCC và thêm nhanh vật tư ở Nhập kho tài chính)

#### Mục tiêu
Trên màn hình `/NhapKhoTaiChinh/Create`:
- Bổ sung cột **Nhà cung cấp** cạnh **Số PSP**, dùng input + `datalist` để chọn nhanh.
- Bổ sung nút **Thêm vật tư** để khi nhập kho gặp vật tư chưa có thì thêm ngay, không cần rời sang module Danh mục vật tư.

#### Triển khai
| File | Loại | Mô tả |
|---|---|---|
| `App_Data/Sql/20260629_add_nha_cung_cap_phieu_xk_tc_ct.sql` | Thêm | ALTER TABLE thêm `id_nha_cung_cap` vào `kho_phieu_xk_tc_ct`, FK và index. |
| `Models/Entities/Entities.cs` | Sửa | `PhieuXKTCCt` thêm `IdNhaCungCap`, `TenNcc`. |
| `Repositories/NghiepVuRepositories.cs` | Sửa | Insert/select chi tiết nhập kho tài chính có `id_nha_cung_cap`, join `kho_nha_cung_cap`. |
| `Controllers/NhapKhoTaiChinhController.cs` | Sửa | Nạp `NccList`, nhận list `idNhaCungCap`, thêm API `GetVatTuOptions()` reload vật tư sau khi thêm nhanh. |
| `Views/NhapKhoTaiChinh/Create.cshtml` | Sửa UI | Thêm cột NCC input+datalist; thêm nút **Thêm vật tư** mở modal `/DanhMuc/VatTuForm`, sau lưu reload datalist vật tư. |
| `Views/NhapKhoTaiChinh/Detail.cshtml` | Sửa UI | Hiển thị cột Nhà cung cấp. |
| `wwwroot/js/site.js` | Sửa | `submitForm` hỗ trợ callback `window.quickVatTuSavedCallback` để modal thêm vật tư không reload trang nhập kho. |

#### Lưu ý vận hành
Cần chạy script `App_Data/Sql/20260629_add_nha_cung_cap_phieu_xk_tc_ct.sql` trên DB thật trước khi sử dụng cột Nhà cung cấp mới, nếu không insert chi tiết nhập kho sẽ lỗi thiếu cột `id_nha_cung_cap`.

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 30 (Import Excel PSP)

#### Mục tiêu
Triển khai import Excel cho module Phiếu Sản Phẩm theo template đã chốt: `Sheet1`, header hàng 10, dữ liệu từ hàng 11, 1 file có nhiều PSP, mỗi dòng là 1 vật tư và header PSP lặp lại đầy đủ.

#### Triển khai
| File | Loại | Mô tả |
|---|---|---|
| `Services/ExcelParserService.cs` | Thêm | `IExcelParserService.ParsePspAsync(Stream)` dùng ClosedXML đọc Excel, nhóm nhiều PSP theo số phiếu, chỉ lấy vật tư `Giấy`, match danh mục vật tư theo tên chuẩn hóa. |
| `Controllers/PhieuSPController.cs` | Sửa | Inject parser Excel; thêm `PreviewExcel`; `Upload` nhận `.pdf,.xlsx`, xử lý Excel bằng cách loop từng PSP và gọi `CreateAsync`/`ImportSuaPhieuAsync`. |
| `Views/PhieuSP/Upload.cshtml` | Sửa | `accept=".pdf,.xlsx"`, thêm nút **Xem trước**, datagrid preview Excel trước khi Import. |
| `Views/PhieuSP/_ExcelPreview.cshtml` | Thêm | Partial preview dữ liệu Excel để tái sử dụng server-side nếu cần. |
| `Models/ViewModels/ViewModels.cs` | Sửa | Thêm `ExcelPreviewVM` và `ExcelPreviewRow`. |
| `Program.cs` | Sửa | Đăng ký DI `IExcelParserService, ExcelParserService`. |
| `Services/AllServices.cs` | Sửa nhẹ | Nội dung lịch sử import sửa đổi từ “PDF” sang “file” để dùng chung PDF/Excel. |
| `Views/DinhMuc/_TieuHaoForm.cshtml` | Fix build | Sửa `DateTime?` format `ToString("yyyy-MM")` để build view thành công. |

#### Kết quả kiểm tra
- Lệnh build: `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify`
- Kết quả: ✅ Build thành công 0 lỗi.
- Còn 9 warning nullable cũ ở các view/controller/service khác, không phát sinh từ import Excel.

### Phiên 21 (Cải thiện Danh mục vật tư)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Views/DanhMuc/VatTu.cshtml` | Sửa | Thêm nút **Xuất kho nhanh** trực tiếp trên từng dòng danh sách vật tư, mở lại partial `/TonKho/XuatKhoNhanhForm?id=...`. |
| `Repositories/DanhMucRepositories.cs` | Sửa | Thêm `IVatTuRepository.GetMaxMaVtNumberAsync()` để lấy số lớn nhất từ các mã đúng mẫu `VT[0-9]+`. |
| `Services/AllServices.cs` | Sửa | Thêm `IDanhMucService.GenerateNextMaVatTuAsync()` sinh mã kế tiếp dạng `VT00000`; khi tạo mới nếu mã trống thì vẫn tự sinh ở Service. |
| `Controllers/DanhMucController.cs` | Sửa | Khi mở form thêm vật tư (`id=0`), tự gán `MaVt` bằng mã kế tiếp, ví dụ đang lớn nhất `VT00528` thì form điền `VT00529`. |

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 22 (Đổi combo/select vật tư và PSP sang datalist)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Views/PhieuSP/Create.cshtml` | Sửa UI | Đổi select `idVatTu` và `idVatTuThayThe` sang input + datalist, tìm theo mã/tên vật tư; giữ hidden id để submit về controller hiện tại. |
| `Views/PhieuSP/Detail.cshtml` | Sửa UI | Modal sửa dòng vật tư đổi chọn giấy cuộn thay thế sang input + datalist, vẫn validate bắt buộc chọn khi tick cấp cuộn thay tờ rời. |
| `Views/NhapKhoTaiChinh/Create.cshtml` | Sửa UI | Cột Số PSP đổi từ select sang input + datalist tìm theo số PSP; vật tư trước đó đã là datalist và được giữ nguyên. |
| `Views/TraLaiKho/Create.cshtml` | Sửa UI | Dòng trả lại đổi chọn vật tư và liên kết PSP sang input + datalist, áp dụng cả dòng đầu và dòng thêm động. |
| `Views/GiayTietKiem/_GiayTietKiemForm.cshtml` | Sửa UI | Form modal đổi chọn Phiếu sản phẩm và Giấy cuộn sang input + datalist, hỗ trợ hiển thị lại giá trị khi sửa. |

Ghi chú: các select còn lại trong 5 module là trạng thái/lý do/bộ phận hoặc bảng checkbox chọn nhiều dòng, không thuộc phạm vi đổi vật tư/PSP sang datalist.

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 23 (Liên kết Danh mục vật tư loại Khuôn bế sang module Khuôn Bế)
| File | Loại thay đổi | Mô tả |
|---|---|---|
| `Services/AllServices.cs` | Sửa Service | Thêm `IKhuonBeService.DongBoTuDanhMucVatTuAsync()`: lấy vật tư active có `loai_vat_tu='khuon_be'`, tạo bản ghi `kho_khuon_be` nếu chưa có theo mã/tên; mặc định trạng thái `dang_dung`, phiên bản 1, ghi chú nguồn đồng bộ. Sau phản hồi lỗi duplicate `ma_khuon`, đã bắt MySQL duplicate 1062 và bỏ qua bản ghi đã tồn tại để không văng trang. |
| `Controllers/KhuonBeController.cs` | Sửa Controller | Khi mở `/KhuonBe`, tự gọi đồng bộ và hiển thị thông báo số khuôn bế được thêm từ danh mục vật tư. |
| `Views/DanhMuc/VatTu.cshtml` | Sửa UI | Với dòng vật tư loại Khuôn bế, thêm icon kéo sang module `/KhuonBe`; khi mở module sẽ tự đồng bộ các khuôn bế còn thiếu. |

Ghi chú nghiệp vụ: hiện chưa thay schema để thêm khóa ngoại trực tiếp `kho_khuon_be.id_vat_tu`; cách triển khai này ưu tiên ít ảnh hưởng DB, giúp các khuôn bế đã nhập trong danh mục xuất hiện ở module Khuôn Bế mà không cần nhập lại. Nếu về sau cần liên kết 1-1 chặt, có thể thiết kế ALTER TABLE bổ sung `id_vat_tu` vào `kho_khuon_be`.

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi; có 1 warning do file PDB đang bị process `.NET Host (14528)` khóa, không liên quan lỗi code.

Quy tắc nghiệp vụ phiên 7:
- Dòng đợt cũ được coi là còn thiếu khi `trang_thai IN ('chua_lay','thieu')` và `so_luong_thuc_nhan < so_luong_de_nghi`, kể cả khi đợt cũ đã bấm hoàn thành/chốt đợt.
- Số lượng đưa sang đợt mới = `so_luong_de_nghi - so_luong_thuc_nhan`.
- Không gộp trùng dòng đã có bản ghi đợt sau cùng `id_phieu_sp_vat_tu`.
- Khi chọn cả dòng pending và dòng vật tư gốc cùng `id_phieu_sp_vat_tu`, Service ưu tiên dòng pending và bỏ qua dòng mới trùng.
- Khi nhập/sửa số thực nhận ở chi tiết đợt, tổng thực nhận của tất cả đợt được đồng bộ về dòng vật tư PSP. Nếu sửa số thực nhận, tồn kho chỉ thay đổi theo chênh lệch so với số cũ.
- Theo góc nhìn kho phân xưởng, thực nhận từ Đề nghị xuất giấy là **nhập kho phân xưởng**: ghi lịch sử `nhap_de_nghi` và cộng tồn. Xuất kho nhanh (`xuat_khac`) mới là thao tác trừ tồn khi phân xưởng cấp vật tư ra sản xuất/tiêu dùng.

Build sau phiên 7: ✅ `dotnet build` thành công 0 lỗi, 0 warning.

### Phiên 25 (Màn hình cấu hình đường dẫn lưu PDF PSP từ UI)

Đã thêm màn hình cấu hình tại `/Home/Settings`, cho phép sửa `Storage:PspPdfPath` trực tiếp trên web app. Nếu để trống, hệ thống tiếp tục dùng fallback `App_Data/PspPdf`. Khi lưu cấu hình, service thử tạo thư mục đích để phát hiện sớm đường dẫn sai hoặc thiếu quyền ghi.

| File | Loại | Mô tả |
|---|---|---|
| `Services/ConfigSettingsService.cs` | Thêm | Service đọc/ghi `appsettings.json`, lấy đường dẫn PDF PSP cấu hình/thực tế, validate tạo thư mục khi lưu. |
| `Models/ViewModels/ViewModels.cs` | Sửa | Thêm `SettingsVM` cho form cấu hình. |
| `Controllers/HomeController.cs` | Sửa | Inject `IConfigSettingsService`, thêm GET/POST `Settings`. |
| `Views/Home/Settings.cshtml` | Thêm | Form cấu hình đường dẫn lưu PDF PSP, hiển thị đường dẫn thực tế. |
| `Views/Shared/_Layout.cshtml` | Sửa | Thêm nhóm menu “Hệ thống” và link “Cấu hình”. |
| `Program.cs` | Sửa | Đăng ký DI `IConfigSettingsService, ConfigSettingsService`. |

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

## Technical Debt

1. ✅ **Transaction** — Đã thêm transaction cho 9/9 Service method cần thiết (tạo/sửa nhiều bảng):
   - `PhieuSPService.CreateAsync`, `UpdateVatTuCtAsync`
   - `DeNghiXuatService.TaoDotAsync`, `UpdateThucNhanAsync`
   - `PhieuXKTCService.CreateAsync`
   - `TraLaiKhoService.CreateAsync`
   - `TonKhoService.XuatKhoNhanhAsync`
   - `KhuonBeService.GhiNhanSuDungAsync`
   - `DinhMucService.SaveTieuHaoAsync`
2. **Mapping thủ công**: Chưa có AutoMapper → Entity ↔ ViewModel mapping thủ công trong Service
3. ✅ **Báo cáo nhập/xuất** — Đã sửa `BaoCaoService.GetNhapXuatAsync` dùng `GetBaoCaoNhapXuatAsync` (query SQL GROUP BY) thay vì load toàn bộ `GetLichSuAsync(0, ...)` rồi tính LINQ. Nhanh hơn, ít dữ liệu hơn.

## Việc còn dang dở

- [x] Gộp nhiều PSP vào 1 đợt đề nghị xuất giấy (đã thiết kế UI mới: chọn nhiều PSP + checkbox all)
- [x] Gộp dòng chưa lấy/thiếu từ đợt đề nghị trước sang đợt tiếp theo
- [x] Chạy script `kho_schema.sql` trên MySQL
- [x] Insert dữ liệu danh mục thật
- [x] Upload PDF + parse tự động (PSP): Đã test với file mới, hoạt động ổn định.
- [x] Import PDF PSP chế độ sửa phiếu: checkbox, update/merge phiếu đã tồn tại, ghi lịch sử thay đổi, bảo toàn lịch sử nhận/xuất
- [x] Cấu hình đường dẫn lưu PDF bước đầu qua `Storage:PspPdfPath`, không hard-code `wwwroot/uploads`, fallback ngoài `wwwroot` tại `App_Data/PspPdf`
- [x] Màn hình cấu hình đường dẫn lưu PDF trên web app nếu cần đổi trực tiếp từ UI
- [x] Export Excel bằng ClosedXML
- [x] UI set flag `la_cuon_thay_to_roi` khi tạo/sửa dòng vật tư trong PSP
- [x] Trang so sánh tuổi thọ khuôn bế theo NCC
- [x] Thêm transaction cho các method Service tạo nhiều bản ghi
- [x] ALTER TABLE để thêm cột `dinh_muc_theo_ten_sp` vào `kho_dinh_muc_tieu_hao`

## Công việc tiếp theo đề xuất

1. ✅ **Import PDF PSP**: Đã test với file mới, hoạt động ổn định.
2. 🟡 **Import PDF Nhập kho tài chính**: Không sử dụng đến, lưu làm ưu tiên cuối cùng.
3. **Dữ liệu báo cáo khuôn bế**: rà soát/bổ sung NCC và `dinh_muc_tuoi_tho` cho các khuôn còn thiếu để chỉ số so sánh theo NCC đáng tin cậy hơn.

### Phiên 26 (Báo cáo tuổi thọ / hiệu suất khuôn bế theo NCC)

#### Mục tiêu
Hoàn thiện hạng mục còn dang dở: trang báo cáo so sánh tuổi thọ/hiệu suất khuôn bế theo nhà cung cấp, dùng dữ liệu `kho_khuon_be`, `kho_khuon_be_lich_su`, `kho_nha_cung_cap`, `kho_phieu_sp` đã có trong module Khuôn Bế.

#### Triển khai
1. Thêm ViewModel riêng:
   - `BaoCaoKhuonBeNccVM`
   - `BaoCaoKhuonBeNccRow`
   - `KhuonBeCanhBaoRow`
2. Repository `IKhuonBeRepository`:
   - `GetBaoCaoTheoNccAsync()` tổng hợp theo NCC.
   - `GetKhuonCanChuYAsync()` lấy danh sách khuôn hỏng/sắp chạm định mức/chưa ghi nhận sử dụng.
3. Service `IBaoCaoService`:
   - `GetKhuonBeTheoNccAsync()` build VM.
   - `ExportKhuonBeTheoNccExcelAsync()` export Excel 2 phần: tổng hợp NCC và danh sách cần chú ý.
4. Controller/View:
   - Route `/BaoCao/KhuonBeTheoNcc`.
   - Route `/BaoCao/ExportKhuonBeTheoNcc`.
   - Card trên `/BaoCao` trỏ sang báo cáo mới.

#### Chỉ tiêu hiển thị
- Tổng số khuôn theo NCC.
- Số khuôn đang dùng / hỏng.
- Tổng số tờ đã in.
- Tuổi thọ trung bình.
- Định mức trung bình và tỷ lệ sử dụng so với định mức nếu có `dinh_muc_tuoi_tho`.
- Danh sách khuôn hiệu suất thấp/cần chú ý.

#### File thay đổi
| File | Loại | Mô tả |
|---|---|---|
| `Models/ViewModels/ViewModels.cs` | Thêm | ViewModel báo cáo khuôn bế theo NCC |
| `Repositories/NghiepVuRepositories.cs` | Sửa | Query tổng hợp NCC và khuôn cần chú ý trong `KhuonBeRepository` |
| `Services/AllServices.cs` | Sửa | Thêm service method và export Excel cho báo cáo |
| `Controllers/BaoCaoController.cs` | Sửa | Thêm action báo cáo/export |
| `Views/BaoCao/Index.cshtml` | Sửa UI | Card Tuổi thọ khuôn bế mở báo cáo mới |
| `Views/BaoCao/KhuonBeTheoNcc.cshtml` | Thêm UI | Trang báo cáo Bootstrap 5 theo style hiện có |

Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

## Liên kết

- Code: `e:\THUYDD\VSCode\KhoQuanLy`
- Database: MySQL `payroll_db` trên `localhost`
- Connection string: `Server=localhost;Database=payroll_db;User ID=OffsetSauin;Password=...`

### Phiên 24 (Sửa logic TonKyTruoc trong phiếu in/chi tiết đề nghị xuất)

#### Vấn đề
Trên view `In.cshtml` và `Detail.cshtml` của module Đề nghị xuất, `TonKyTruoc` đọc trực tiếp từ cột `ton_ky_truoc` trong bảng `kho_dot_de_nghi_ct`. Khi có giao dịch nhập/xuất mới hoặc đợt pending, giá trị này không phản ánh đúng tồn thực tế.

#### Giải pháp
1. **View**: Đổi cách hiển thị `TonKyTruoc` = `SoLuongYeuCau - SoLuongDeNghi` (tổng đã lấy từ các đợt trước). Nếu = 0 (lần đầu đề nghị) thì fallback về giá trị DB.
2. **Repository `GetTonKyTruocAsync`**: Đổi `ngay_giao_dich<@truocNgay` thanh `ngay_giao_dich<=@truocNgay` để không bỏ sót giao dịch cùng ngày.
3. **Service `TaoDotAsync`**:
   - Dòng mới: đổi `DateTime.Today` (00:00 hôm nay) thanh `DateTime.Now` (đến thời điểm hiện tại)
   - Dòng pending: thay `TonKyTruoc = 0` cứng bằng `GetTonKyTruocAsync` thực tế

#### File thay đổi
| File | Loại | Mô tả |
|---|---|---|
| `Views/DeNghiXuat/In.cshtml` | Sửa | `TonKyTruoc` tính bằng `SoLuongYeuCau - SoLuongDeNghi`, fallback DB nếu = 0 |
| `Views/DeNghiXuat/Detail.cshtml` | Sửa | Áp dụng cùng logic `TonKyTruoc` như `In.cshtml` |
| `Repositories/NghiepVuRepositories.cs` | Sửa | `GetTonKyTruocAsync`: đổi `<` thành `<=` để bao gồm giao dịch cùng ngày |
| `Services/AllServices.cs` | Sửa | Dòng mới: `DateTime.Today` thanh `DateTime.Now`; Dòng pending: bỏ `TonKyTruoc=0` cứng, dùng `GetTonKyTruocAsync` |

Build kiểm tra: ✅ `dotnet build` thành công 0 lỗi, 0 warning.

