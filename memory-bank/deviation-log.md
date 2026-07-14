# Deviation Log — Các điểm lệch so với thiết kế ban đầu

## LỆCH 1: Trạng thái mặc định Phiếu SP [ĐÃ FIX & ĐỒNG BỘ]

| | Thiết kế (Chat 1) | Trước fix | Sau fix |
|---|---|---|---|
| Mặc định | "hoan_thanh" | "cho_xuat" | ✅ "chua_hoan_thanh" cho PSP mới/import PDF |
| User action | Chủ động đánh dấu "chưa hoàn thành" | Chủ động chuyển trạng thái | ✅ Chuyển sang "hoan_thanh" khi hoàn tất |

- **File đã sửa**: `Controllers/PhieuSPController.cs`, `Models/Entities/Entities.cs`
- **File phụ**: `Controllers/HomeController.cs`, `Controllers/DeNghiXuatController.cs`, `Views/Home/Index.cshtml`
- **Quy ước hiện tại**:
  - `chua_hoan_thanh`: PSP mới tạo/import PDF, còn cần xử lý/đề nghị xuất.
  - `hoan_thanh`: PSP đã hoàn tất.
  - `cho_xuat`: trạng thái cũ, không dùng lại.

## LỆCH 2: Đợt đề nghị xuất giấy — gộp nhiều PSP [ĐÃ XỬ LÝ MỘT PHẦN]

| | Thiết kế (Chat 1) | Implement (Chat 2) | Sau fix (Chat 3) | Trạng thái |
|---|---|---|---|---|
| Gộp PSP | Gộp nhiều PSP vào 1 đợt | Chỉ chọn được 1 PSP/đợt | ✅ Chọn nhiều PSP (checkbox) | ✅ Đã xử lý |
| Gộp đợt trước | Tích hợp dòng chưa lấy từ đợt trước | Chưa có UI gộp đợt trước | ✅ Có UI tải dòng còn thiếu từ đợt trước và chuyển sang đợt mới | ✅ Đã xử lý |

- **File thay đổi**:
  - `Controllers/DeNghiXuatController.cs` — thêm action `GetVatTuByMultiplePhieu` (POST JSON)
  - `Views/DeNghiXuat/Create.cshtml` — thiết kế lại: bảng PSP checkbox + bảng VT từ nhiều PSP + checkbox all
- **Cách hoạt động**: User check nhiều PSP → gọi API `/DeNghiXuat/GetVatTuByMultiplePhieu` với `[FromBody] List<int>` → trả về tất cả dòng VT của các PSP đã chọn → user tiếp tục check dòng VT cần xuất → submit tạo đợt (cơ chế `DotDeNghiCreateVM.IdPhieuSpVatTuList` cũ)
- **Gộp đợt trước**: User bấm "Tải dữ liệu" ở khối "Đợt trước còn dư" → gọi `/DeNghiXuat/GetPendingFromPreviousDots` qua Service → chọn dòng còn thiếu → submit `IdPendingCtList`. Service tạo dòng mới với số lượng còn lại và bỏ qua dòng vật tư mới trùng cùng `id_phieu_sp_vat_tu`.

## LỖI ĐÃ FIX

### Lỗi 1: HomeController — inject sai interface
- **Fix**: Xóa `IVatTuService`, chỉ inject `ITonKhoService`, `IPhieuSPService`, `IKhuonBeService`
- **File**: `Controllers/HomeController.cs`

### Lỗi 2: DeNghiXuatService — dòng code thừa + bug getChiTiet
- **Fix**: Sửa `_repo.GetChiTietAsync(0)` → `_repo.GetChiTietAsync(idCt)`
- **File**: `Services/AllServices.cs` — DeNghiXuatService.UpdateThucNhanAsync

## VẤN ĐỀ KIẾN TRÚC ĐÃ GIẢI QUYẾT

### 1. ✅ Trùng lặp Controller XK TC / Nhập kho TC
- **Đã xóa** `PhieuXKTCController.cs` — chỉ giữ `NhapKhoTaiChinhController`

### 2. ✅ Controller inject Repository trực tiếp
- **Đã chuyển toàn bộ** Repository vào Service layer:
  - `DeNghiXuatService`: thêm `IVatTuRepository` + `ITonKhoRepository`
  - `PhieuXKTCService`: thêm `IVatTuRepository` + `ITonKhoRepository`
  - `TraLaiKhoService`: thêm `IVatTuRepository` + `ITonKhoRepository`
- Các Controller tương ứng đã bỏ hoàn toàn inject Repository

### 3. ✅ Báo cáo nhập/xuất
- **Đã fix**: `TonDauKy`, `TongNhap`, `TongXuat`, `TongTraLai` tính từ `kho_lich_su_ton`

### 4. ✅ Cấu hình tiêu hao mực
- **Đã thêm** trường `DinhMucTheoTenSp` vào Entity, ViewModel, Repository, Service
- Thêm option `ca_hai` cho phép chọn cả 2 loại định mức

## VẤN ĐỀ KIẾN TRÚC CÒN TỒN TẠI

### 1. ✅ Thiếu transaction — ĐÃ FIX
- Đã thêm transaction cho toàn bộ **9/9 Service method cần thiết**:
  - `PhieuSPService.CreateAsync`, `UpdateVatTuCtAsync`
  - `DeNghiXuatService.TaoDotAsync`, `UpdateThucNhanAsync`
  - `PhieuXKTCService.CreateAsync`
  - `TraLaiKhoService.CreateAsync`
  - `TonKhoService.XuatKhoNhanhAsync`
  - `KhuonBeService.GhiNhanSuDungAsync`
  - `DinhMucService.SaveTieuHaoAsync`
- **File thay đổi**: `Services/AllServices.cs` — thêm `IUnitOfWork` vào `TonKhoService`, `KhuonBeService`; bọc 3 method còn thiếu bằng `_uow.Begin/Commit/Rollback`
- Build: ✅ 0 lỗi, 0 warning
- Trước đây nếu MySQL crash giữa chừng, dữ liệu sẽ bị inconsistent → đã giải quyết

### 2. ✅ BaoCaoNhapXuat — ĐÃ FIX
- Đã sửa `BaoCaoService.GetNhapXuatAsync` dùng `GetBaoCaoNhapXuatAsync` (query SQL GROUP BY) thay vì load toàn bộ `GetLichSuAsync(0, ...)` rồi tính LINQ
- Query SQL chỉ trả về `(id_vat_tu, loai_gd, SUM)` — nhanh hơn, ít dữ liệu hơn
- **File thay đổi**: `Services/AllServices.cs` — BaoCaoService
- Build: ✅ 0 lỗi, 0 warning

### 3. 🟡 Import PDF PSP — ĐÃ CÓ CODE, CẦN TEST XÁC NHẬN
- Đã thấy code `IPdfParserService`/`PdfParserService.ParsePspAsync` để parse PSP PDF.
- `Program.cs` đã đăng ký DI cho parser.
- `PhieuSPController` đã có action `Upload` GET/POST, gọi parser để tạo PSP.
- `Views/PhieuSP/Upload.cshtml` và nút `Import PDF` trong `Views/PhieuSP/Index.cshtml` đã tồn tại; màn hình upload hiện hỗ trợ chọn nhiều file PDF trong một lần import.
- Đã bổ sung bước chuẩn hóa và chặn tràn độ dài cho các trường header PSP theo schema DB thực tế (`so_phieu` 50, `so_lsx` 50, `ten_san_pham` 255, `ma_san_pham` 50, `khach_hang` 200, `nguoi_tao` 100) để giảm lỗi do text PDF bị dính nhãn hoặc kéo dư nội dung.
- Đã đổi cách trích xuất `khach_hang` sang cắt theo nhãn kế tiếp (`4.Số lượng...`) và thêm bước làm sạch riêng cho tên khách hàng để hạn chế lấy kèm thông tin không cần thiết.
- Đã tăng độ bền parse `ngay_lenh` / `ngay_giao_hang` bằng cách cắt theo nhãn kế tiếp và lấy ngày đầu tiên đúng format `dd/MM/yyyy`; đồng thời thêm fallback `nguoi_tao` theo máy local khi chạy localhost, và hiển thị `NguoiTao` trên màn hình chi tiết PSP.
- Đã siết thêm `so_luong_sp` theo cơ chế lấy số trên cùng dòng của nhãn `Số lượng`, tránh bắt nhầm số ở dòng/nhãn phía dưới; đồng thời ưu tiên lấy `khach_hang` trên đúng dòng gần nhãn trước khi fallback sang cơ chế cắt theo vùng.
- Đã bổ sung parse trực tiếp ngày sau nhãn `7.Ngày nhận LSX:` và `8.Ngày giao hàng:` để xử lý mẫu PDF không có xuống dòng chuẩn; danh sách PSP trên web cũng đã đổi format hiển thị ngày sang `dd/MM/yyyy`.
- Đã thay cơ chế bắt `so_luong_sp` sang regex bám trực tiếp sau nhãn `Số lượng...`; đồng thời `khach_hang` giờ bị chặn cứng trước các nhãn `Số phiếu sản phẩm con`, `I. THÔNG TIN SẢN PHẨM CON`, `1. Tên sản phẩm` để không nuốt sang vùng sản phẩm con.
- Đã bổ sung kiểm tra trùng `so_phieu` trước khi lưu PSP import từ PDF: nếu phiếu đã tồn tại trong `kho_phieu_sp` thì hệ thống **bỏ qua phiếu đó**, không insert lại, và hiển thị thông báo tổng hợp thân thiện thay cho lỗi `MySqlException: Duplicate entry ... for key 'kho_phieu_sp.so_phieu'`.
- Trạng thái hiện tại: chưa đánh dấu hoàn tất vì cần test thực tế với PDF mẫu để xác nhận mapping header, dòng vật tư, số lượng, ngày, người tạo theo IP/máy và cảnh báo vật tư chưa khớp danh mục.
- Mapping PDF PSP đã chốt gồm 2 phần: header lưu vào `kho_phieu_sp`; phần **VẬT TƯ SẢN XUẤT** lưu trực tiếp vào `kho_phieu_sp_vat_tu`. Dòng vật tư parse từ PDF cần lưu `id_vat_tu` nếu match được danh mục, `chung_loai_text`, `kich_thuoc`, `so_luong_yeu_cau`, `so_luong_to_roi`, `don_vi_tinh`. Các trường `STT`, tỷ lệ xả/in/gia công, số lượng cần đạt, căn chỉnh không lưu DB.
- Quyết định nghiệp vụ mới đã chốt ngày 23/06/2026: cần bổ sung checkbox **"Sửa phiếu sản phẩm"** trên màn hình import PDF PSP. Khi không tick, phiếu trùng `so_phieu` vẫn bị bỏ qua như hiện tại. Khi tick, `so_phieu` phải tồn tại; nếu không tồn tại thì báo lỗi, không import. Nếu tồn tại thì import PDF sửa để update/merge dữ liệu cũ và ghi lịch sử.
- Import PDF sửa không tự tính lại vật tư theo số lượng sản phẩm; số lượng vật tư lấy từ PDF sửa do phòng ban khác đã tính. Nếu số đã nhận vượt số lượng yêu cầu mới thì giữ số đã nhận, trạng thái `du`; nếu cần xử lý dư thực tế thì dùng module Trả Lại Kho.
- Import PDF sửa phải merge dòng vật tư: cùng vật tư thì update số lượng/yêu cầu và giữ `so_luong_da_nhan`; vật tư mới thì insert; vật tư cũ không còn trong PDF sửa thì xóa nếu chưa phát sinh nhận/xuất, còn nếu đã phát sinh thì giữ lại để bảo toàn lịch sử và đánh dấu/ghi chú không cần nhận tiếp nếu phù hợp.
- File PDF PSP sau upload lưu trên server web app hoặc ổ mạng server truy cập được. Không lưu phân tán trên máy client, không hard-code đường dẫn; nên cho cấu hình đường dẫn lưu từ web app hoặc cấu hình hệ thống, ưu tiên lưu ngoài `wwwroot` lâu dài.
- Đã triển khai bước đầu storage PDF PSP ngày 23/06/2026: thêm `IPdfStorageService`/`PdfStorageService`, cấu hình `Storage:PspPdfPath`, fallback `App_Data/PspPdf` ngoài `wwwroot`; `PhieuSPController.Upload` dùng service lưu file và kiểm tra trùng phiếu trước khi lưu file. Build: ✅ 0 lỗi, 0 warning.
- Đã hoàn thiện bước merge import PDF PSP chế độ sửa phiếu ngày 23/06/2026. `PhieuSPController.Upload` nhận flag `suaPhieuSanPham`; nếu tick sửa mà không tìm thấy `so_phieu` thì báo lỗi; nếu tìm thấy thì lưu PDF mới và gọi `PhieuSPService.ImportSuaPhieuAsync`. Service update header, merge dòng vật tư, ghi lịch sử, xóa dòng cũ chưa phát sinh, giữ/đánh dấu `khong_can` dòng đã phát sinh. Build: ✅ 0 lỗi, 0 warning.
- Đã siết parser vật tư PDF PSP ngày 23/06/2026: sửa lỗi cắt thiếu token tên vật tư khi parse từ cuối dòng; xác thực kích thước/đơn vị; map đủ dữ liệu vào `PhieuSPVatTu` để lưu `kho_phieu_sp_vat_tu`; cải thiện match danh mục vật tư bằng chuẩn hóa tên. Build: ✅ 0 lỗi, 0 warning.
- Test PDF PSP thật ngày 24/06/2026 phát hiện mẫu PDF có bảng vật tư bị PdfPig trích thành một chuỗi liên tục, không còn xuống dòng, khiến parser line-by-line trả 0 dòng vật tư. Đã bổ sung fallback trong `Services/PdfParserService.cs` để parse dạng text liên tục. Test lại: `001017/2026/HNI-OFF` parse được `Giấy Duplex 230 K695 Hanchang` và match `id_vat_tu=68`; `001027/2026/HNI-OFF` parse được `Giấy Duplex 450 K900 Hankuk` và match `id_vat_tu=28`. Đã chạy import sửa trên DB thật cho `001017/2026/HNI-OFF`, merge 1 dòng vào `kho_phieu_sp_vat_tu`, ghi lịch sử. Storage fallback/configured path đều test OK.
- Audit DB thật ngày 24/06/2026: còn 2 PSP có header nhưng thiếu vật tư (`001027/2026/HNI-OFF`, `001018/2026/HNI-OFF`). Đã import sửa lại bằng PDF hiện có và audit lại còn 0 PSP thiếu vật tư. Trong quá trình import phát hiện lỗi `Data too long for column 'ly_do'`; đã xử lý trong `PhieuSPService.ImportSuaPhieuAsync` bằng cách chuẩn hóa/cắt ngắn nội dung lịch sử trước khi ghi DB.

### 4. ❌ Import PDF PhieuXKTC/Nhập kho tài chính — CHƯA HOÀN TẤT
- Chưa ghi nhận spec/parser tương ứng cho PDF Phiếu XKTC/Nhập kho tài chính.
- Nếu triển khai, phải dùng route/controller theo góc nhìn user: `NhapKhoTaiChinhController`, không khôi phục `PhieuXKTCController`.

### 5. ✅ UI flag `la_cuon_thay_to_roi` — ĐÃ HOÀN THIỆN
- Form tạo PSP (`Views/PhieuSP/Create.cshtml`) đã có checkbox `laCuonThayToRoi` cho từng dòng vật tư.
- Modal sửa dòng vật tư trong `Views/PhieuSP/Detail.cshtml` đã có checkbox `Cấp cuộn thay tờ rời`.
- `PhieuSPVatTuEditVM` đã có property `LaCuonThayToRoi`.
- `PhieuSPService.UpdateVatTuCtAsync` đã cập nhật lại `old.LaCuonThayToRoi` khi sửa dòng.
- Build sau thay đổi: ✅ 0 lỗi, 0 warning.

### 6. ✅ Đề nghị xuất — gộp dòng còn thiếu từ đợt trước — ĐÃ HOÀN THIỆN
- Controller không còn gọi Repository trực tiếp bằng `RequestServices`; đã chuyển sang `IDeNghiXuatService.GetPendingFromPreviousDotsAsync`.
- `DotDeNghiCreateVM` có thêm `IdPendingCtList` để submit riêng danh sách dòng chi tiết đợt cũ.
- Repository có `GetChiTietByIdAsync` và `GetPendingFromPreviousDotsAsync` trả về số lượng còn lại.
- Cập nhật ngày 24/06/2026: `GetPendingFromPreviousDotsAsync` không còn loại trừ đợt đã `hoan_thanh`; nếu user chốt đợt nhưng chưa nhập thực nhận, các dòng `chua_lay`/`thieu` vẫn xuất hiện ở khối chuyển tiếp sang đợt sau.
- Màn hình tạo đợt đã đổi nhãn **"Đợt trước còn dư"** thành **"Dòng chưa nhận/nhận thiếu từ đợt trước"** và lọc các PSP/dòng vật tư đang pending khỏi khối chọn mới để tránh tạo trùng.
- Cập nhật ngày 24/06/2026: sửa lỗi nhập thực nhận chỉ cập nhật chi tiết đợt mà chưa cập nhật dòng vật tư PSP. `DeNghiXuatService.UpdateThucNhanAsync` hiện đồng bộ tổng thực nhận về `kho_phieu_sp_vat_tu.so_luong_da_nhan` và `trang_thai_nhan`; tồn kho/lịch sử tồn cập nhật theo phần chênh lệch khi sửa số thực nhận để tránh trừ kho lặp.
- Repository vật tư PSP hiện đọc thêm tổng `so_luong_thuc_nhan` từ `kho_dot_de_nghi_ct` để phản ánh đúng cả dữ liệu cũ đã nhận nhưng chưa đồng bộ, giúp phiếu đã nhận đủ không còn hiện ở khối chọn đề nghị mới.
- Cập nhật bổ sung ngày 24/06/2026: phát hiện `UpdateVatTuCtAsync` trước đó chưa ghi cột `so_luong_da_nhan`, nên khi nhập thực nhận chỉ thấy `kho_dot_de_nghi_ct` thay đổi còn `kho_phieu_sp_vat_tu` chưa lưu số đã nhận. Đã sửa repository để update cả `so_luong_da_nhan`, sửa `UpdateTrangThaiNhanAsync` để ghi số lượng + trạng thái trong transaction, và thêm đồng bộ header PSP sang `hoan_thanh` khi mọi dòng vật tư cần nhận đã đủ.
- Danh sách Phiếu SP (`/PhieuSP`) hiện tính trạng thái hiển thị từ dòng vật tư/tổng thực nhận trong `kho_dot_de_nghi_ct`, nên phiếu đã đủ sẽ hiển thị "Hoàn thành" trên Index dù dữ liệu cũ chưa kịp cập nhật header; Detail vẫn giữ trạng thái dòng là nguồn kiểm tra chi tiết.
- Cập nhật ngày 25/06/2026: làm rõ lại nghiệp vụ bị hiểu nhầm. “Đề nghị xuất giấy” là phân xưởng đề nghị kho tổng/bên kho cấp giấy, nên với app kho phân xưởng thì khi nhập `thực nhận` phải là **nhập kho** (`nhap_de_nghi`, cộng tồn theo chênh lệch). Chỉ “Xuất kho nhanh” mới là xuất từ kho phân xưởng ra sản xuất (`xuat_khac`, trừ tồn). Đã rà soát Service hiện đúng dấu và chỉnh nhãn UI để tránh hiểu nhầm.
- Service tạo dòng chi tiết mới theo công thức: `SoLuongConLai = SoLuongDeNghi - SoLuongThucNhan`.
- Chống gộp trùng bằng `NOT EXISTS` dòng đợt sau cùng `id_phieu_sp_vat_tu` và Service bỏ qua dòng vật tư mới nếu đã chọn pending cùng vật tư.
- Build sau thay đổi: ✅ 0 lỗi, 0 warning.
- Test end-to-end ngày 25/06/2026: đã chạy harness .NET trên DB thật với dữ liệu test và cleanup sau test; các kiểm tra Đề nghị xuất giấy, sửa thực nhận theo delta, pending đợt kế tiếp, Xuất kho nhanh, Báo cáo Nhập/Xuất đều PASS.
- Audit dữ liệu cũ ngày 25/06/2026: phát hiện 3 dòng `kho_phieu_sp_vat_tu` chưa đồng bộ `so_luong_da_nhan/trang_thai_nhan` dù `kho_dot_de_nghi_ct` đã có thực nhận. Sau khi user xác nhận backup/cho phép cập nhật DB thật, đã chạy script đồng bộ 3 dòng và audit lại còn 0 dòng lệch.
- Cập nhật ngày 25/06/2026: màn tạo đợt đề nghị xuất giấy tự tải danh sách **Dòng chưa nhận/nhận thiếu từ đợt trước** khi mở trang, tránh quên bấm “Tải dữ liệu”. Dòng pending hiện trả đúng `don_vi_tinh` theo danh mục/PSP, không còn hard-code Kg.

### 7. ✅ Tồn kho âm — ĐÃ SIẾT CHẶN PHÁT SINH MỚI
- Rà soát code cho thấy `TonKhoService.XuatKhoNhanhAsync` đã chặn xuất vượt tồn, nhưng các luồng trừ tồn khác trước đó còn có thể làm âm:
  - `TraLaiKhoService.CreateAsync` trừ tồn khi trả lại kho.
  - `DinhMucService.SaveTieuHaoAsync` trừ tồn khi ghi tiêu hao thực tế.
  - `DeNghiXuatService.UpdateThucNhanAsync` có thể trừ tồn khi sửa giảm số thực nhận so với số cũ.
- Đã bổ sung kiểm tra tồn hiện tại trong Service trước khi trừ; nếu vượt tồn thì rollback transaction và trả thông báo thân thiện qua Controller.
- Lưu ý: thay đổi này chặn phát sinh tồn âm mới từ UI/nghiệp vụ chính; nếu DB đã có số âm cũ thì cần audit DB thật bằng truy vấn đọc trước khi xử lý dữ liệu.

### 8. ✅ Nhập kho tài chính — Giảm trùng cột mô tả/vật tư liên kết
- Giải thích nghiệp vụ cũ: `mo_ta_vat_tu` là text mô tả theo chứng từ, còn `id_vat_tu` là liên kết danh mục để hệ thống biết vật tư nào cần cộng tồn. Vì người dùng phải nhập/chọn cả hai nên gây cảm giác trùng lặp.
- Đã đổi UI tạo phiếu nhập kho tài chính thành 1 cột **Tên vật tư**: người dùng gõ/chọn theo mã hoặc tên vật tư từ danh mục; hệ thống lưu `id_vat_tu` hidden và vẫn lưu `mo_ta_vat_tu` là tên hiển thị để truy vết chứng từ.
- Đơn vị tính không còn mặc định Kg; tự điền theo `kho_vat_tu.don_vi_tinh` của vật tư đã chọn.
- Cột **Số PSP** đã nạp danh sách PSP qua Service để có thể liên kết `id_phieu_sp`.

### 9. ✅ Import PDF PSP và Đề nghị xuất — Không ẩn cảnh báo/không bỏ qua dòng thiếu `kho_vat_tu`
- Lỗi phát hiện ngày 25/06/2026: parser/import PDF PSP đã có warning khi vật tư không match danh mục `kho_vat_tu`, nhưng `_Layout.cshtml` chưa render `TempData["Warning"]` nên user không thấy cảnh báo.
- Đã sửa `_Layout.cshtml` hiển thị alert warning, nhờ đó các thông báo như “Vật tư ... chưa có trong danh mục” hiện rõ sau import.
- Lỗi liên quan module Đề nghị xuất giấy: khi user chọn đủ 2 dòng vật tư của 1 PSP nhưng một dòng chưa có `id_vat_tu`, `DeNghiXuatService.TaoDotAsync` trước đó `continue` âm thầm nên đợt tạo ra chỉ có 1 dòng.
- Đã sửa Service để không bỏ qua âm thầm: dòng mới/pending thiếu liên kết `id_vat_tu` sẽ gom lỗi, rollback transaction và yêu cầu user bổ sung/liên kết vật tư trong `kho_vat_tu` trước khi tạo đợt. Controller bắt lỗi và View hiển thị validation summary.
- Build kiểm tra qua output riêng `build_verify`: ✅ 0 lỗi; build mặc định bị khóa do app đang chạy.

### 10. ✅ Tự liên kết lại dòng PSP sau khi user bổ sung vật tư vào danh mục
- Vấn đề phát hiện sau phiên 19: import PDF PSP đã cảnh báo dòng chưa có trong `kho_vat_tu` và lưu `chung_loai_text`, nhưng sau khi user tạo vật tư mới trong danh mục, các dòng PSP cũ vẫn `id_vat_tu = NULL`, nên màn tạo đợt chưa dùng được ngay.
- Đã thêm `IPhieuSPService.AutoLienKetVatTuChuaCoDanhMucAsync`: rà tất cả dòng PSP chưa liên kết, match `chung_loai_text` với `kho_vat_tu.ten_vt` theo chuẩn hóa tên/khớp gần, rồi cập nhật `id_vat_tu` và `don_vi_tinh`.
- `DeNghiXuatController` gọi cơ chế này khi mở màn tạo đợt, tải vật tư nhiều PSP, tải pending và submit tạo đợt. Vì vậy sau khi thêm vật tư vào danh mục, user chỉ cần quay lại/tải lại màn tạo đề nghị; không cần import lại PDF.
- Build kiểm tra: ✅ 0 lỗi, 0 warning.

### 11. ✅ Cấp cuộn thay tờ rời — ĐÃ HOÀN THIỆN NGHIỆP VỤ VẬT TƯ THỰC CẤP
- Bổ sung cột `kho_phieu_sp_vat_tu.id_vat_tu_thay_the` để lưu giấy cuộn thay thế/vật tư thực cấp, trong khi vẫn giữ `id_vat_tu/chung_loai_text` là vật tư gốc theo PDF để truy vết.
- Khi tick `la_cuon_thay_to_roi`, Service bắt buộc phải có `IdVatTuThayThe`; khi bỏ tick thì clear vật tư thay thế.
- Dropdown giấy cuộn thay thế chỉ lấy danh mục `kho_vat_tu.loai_vat_tu='giay_cuon'` theo xác nhận của user.
- Tạo đợt đề nghị, tính tồn kỳ trước, nhập thực nhận, cập nhật tồn kho và lịch sử tồn dùng vật tư thực tế theo quy tắc `id_vat_tu_thay_the ?? id_vat_tu`.
- UI `PhieuSP/Create`, `PhieuSP/Detail`, `DeNghiXuat/Create`, `DeNghiXuat/Detail`, `DeNghiXuat/In` hiển thị/tương tác đủ hai cột “Giấy theo PDF” và “Giấy cuộn thay thế/thực cấp”.
- Script DB cần chạy: `App_Data/Sql/20260625_add_vat_tu_thay_the_psp.sql`.

### 12. ✅ UI chọn vật tư/PSP — ĐÃ ĐỔI SANG DATALIST
- Theo yêu cầu ngày 25/06/2026, đã rà soát các module Phiếu Sản Phẩm, Đợt Đề Nghị Xuất Giấy, Nhập kho tài chính, Trả lại kho, Giấy tiết kiệm.
- Các combo/select chọn vật tư hoặc phiếu sản phẩm đã đổi sang ô nhập có `datalist`, gồm:
  - `Views/PhieuSP/Create.cshtml`: vật tư PDF/gốc và giấy cuộn thay thế.
  - `Views/PhieuSP/Detail.cshtml`: giấy cuộn thay thế trong modal sửa dòng vật tư.
  - `Views/NhapKhoTaiChinh/Create.cshtml`: liên kết Số PSP; vật tư đã dùng datalist từ trước.
  - `Views/TraLaiKho/Create.cshtml`: vật tư trả lại và PSP liên kết.
  - `Views/GiayTietKiem/_GiayTietKiemForm.cshtml`: phiếu sản phẩm và giấy cuộn.
- Vật tư tìm theo `mã - tên vật tư`; PSP tìm theo `số phiếu - tên sản phẩm`. Khi chọn đúng item, JS gán hidden id để giữ nguyên binding server.
- Module Đề nghị xuất giấy đang chọn nhiều PSP/dòng vật tư bằng bảng checkbox, không phải combo/select, nên không đổi trong phạm vi này.
- Build kiểm tra `build_verify`: ✅ 0 lỗi, 0 warning.

### 13. ✅ Danh mục vật tư loại Khuôn bế chưa hiện trong module Khuôn Bế — ĐÃ XỬ LÝ
- Vấn đề user phản hồi ngày 25/06/2026: đã nhập nhiều khuôn bế ở `/DanhMuc/VatTu` nhưng không thấy link sang module `/KhuonBe`.
- Nguyên nhân: danh mục vật tư lưu trong `kho_vat_tu` theo `loai_vat_tu='khuon_be'`, còn module Khuôn Bế quản lý nghiệp vụ/tuổi thọ/lịch sử sử dụng bằng bảng riêng `kho_khuon_be`; trước đó chưa có sync/link giữa 2 bảng.
- Đã thêm `IKhuonBeService.DongBoTuDanhMucVatTuAsync()` để tự tạo bản ghi `kho_khuon_be` từ vật tư active loại `khuon_be` khi mở `/KhuonBe`; tránh trùng theo mã khuôn/tên khuôn.
- `KhuonBeController.Index` tự gọi đồng bộ và báo số bản ghi mới.
- `/DanhMuc/VatTu` hiển thị icon sang module Khuôn Bế cho từng dòng vật tư loại `khuon_be`.
- Cập nhật sau phản hồi lỗi duplicate: nếu DB đã có `ma_khuon` hoặc có request đồng bộ song song gây `MySqlException Duplicate entry ... kho_khuon_be.ma_khuon`, Service sẽ bắt lỗi duplicate MySQL 1062 và bỏ qua bản ghi đó thay vì làm văng trang `/KhuonBe`.
- Chưa ALTER schema để thêm `id_vat_tu` vào `kho_khuon_be`; đây là giải pháp ít ảnh hưởng DB. Nếu cần truy vết 1-1 tuyệt đối giữa vật tư và khuôn bế, cần thiết kế nâng cấp schema sau.
- Build kiểm tra `build_verify`: ✅ 0 lỗi; có 1 warning do file PDB đang bị process `.NET Host (14528)` khóa, không liên quan lỗi code.

### 14. ✅ Khuôn Bế — Làm rõ nghiệp vụ phiên bản và liên kết danh mục
- Phản hồi ngày 25/06/2026: không được hiểu `phien_ban` là tự tăng theo `Tên khuôn` mỗi khi tạo mới, đặc biệt khi dữ liệu được đồng bộ từ danh mục vật tư.
- Code hiện tại trong `KhuonBeService.CreateAsync` và `DongBoTuDanhMucVatTuAsync` đang gán `PhienBan = 1`, phù hợp quy tắc mới: khuôn/vật tư mới mặc định `v1`.
- Quy tắc nghiệp vụ được chốt: chỉ tạo version mới khi thay thế khuôn hỏng/cùng khuôn thật sự; không tự tăng version chỉ vì trùng/tương tự tên.
- Tạo từ `/DanhMuc/VatTu`: khi mở `/KhuonBe`, hệ thống tự đồng bộ vật tư active `loai_vat_tu='khuon_be'` sang `kho_khuon_be` mặc định v1.
- Tạo từ `/KhuonBe`: Service tự đảm bảo có bản ghi tương ứng trong `kho_vat_tu` loại `khuon_be` nếu danh mục chưa có mã/tên đó.
- Đã triển khai tiếp liên kết chặt bằng `kho_khuon_be.id_vat_tu` và script `App_Data/Sql/20260625_add_id_vat_tu_khuon_be.sql`.
- UI form Khuôn Bế cho chọn “Vật tư liên kết”; danh sách/chi tiết hiển thị vật tư liên kết.
- Chức năng **Tạo version thay thế** chỉ hiển thị khi khuôn đã `hong`; Service kiểm tra trạng thái hỏng, lấy max version theo `id_vat_tu`, tạo bản mới `dang_dung` với mã `MãCũ-V{n}`.
- Build kiểm tra sau triển khai: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

## CÂU HỎI ĐÃ GIẢI QUYẾT

1. **Cấu hình tiêu hao mực**: ✅ Đã confirm — hỗ trợ cả 2: theo tên SP (`theo_ten_sp`) và theo 1000 tờ (`theo_to_in`), thêm option `ca_hai`
2. **Giấy tiết kiệm — `KgTietKiem`**: ✅ Đã fix Service tự động tính `KgTietKiem = KgYeuCau - KgThucXa` khi Create/Update. User không cần nhập tay.

## CÂU HỎI CHƯA GIẢI QUYẾT

(Không còn câu hỏi nào chưa giải quyết.)

## VIỆC CÒN DANG DỞ
### Phiên 24 — Sửa logic TonKyTruoc trong phiếu in/chi tiết đề nghị xuất
- **Vấn đề**: TonKyTruoc trong In.cshtml và Detail.cshtml đọc trực tiếp từ DB, không phản ánh đúng khi có giao dịch nhập/xuất mới hoặc dòng pending.
- **Fix View**: Tính TonKyTruoc = SoLuongYeuCau - SoLuongDeNghi tại view, fallback DB nếu = 0.
- **Fix Repository**: GetTonKyTruocAsync — đổi `ngay_giao_dich<@truocNgay` thành `ngay_giao_dich<=@truocNgay`.
- **Fix Service TaoDotAsync**:
  - Dòng mới: DateTime.Today thành DateTime.Now để bao gồm giao dịch cùng ngày.
  - Dòng pending: thay TonKyTruoc=0 cứng bằng GetTonKyTruocAsync.
- **File**: Views/DeNghiXuat/In.cshtml, Views/DeNghiXuat/Detail.cshtml, Repositories/NghiepVuRepositories.cs, Services/AllServices.cs.
- Build kiểm tra: ✅ dotnet build thành công 0 lỗi, 0 warning.

### Phiên 25 — Màn hình cấu hình đường dẫn lưu PDF PSP
- **Mục tiêu**: Cho phép đổi `Storage:PspPdfPath` trực tiếp từ UI thay vì sửa tay `appsettings.json`.
- **Triển khai**: Thêm `/Home/Settings`, service `ConfigSettingsService` đọc/ghi `appsettings.json`, tạo thử thư mục đích khi lưu để báo lỗi sớm nếu đường dẫn không hợp lệ/thiếu quyền.
- **File**: `Services/ConfigSettingsService.cs`, `Models/ViewModels/ViewModels.cs`, `Controllers/HomeController.cs`, `Views/Home/Settings.cshtml`, `Views/Shared/_Layout.cshtml`, `Program.cs`.
- Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 26 — Báo cáo tuổi thọ / hiệu suất khuôn bế theo NCC
- **Mục tiêu**: Xây trang báo cáo so sánh tuổi thọ/hiệu suất khuôn bế theo nhà cung cấp.
- **Route mới**: `/BaoCao/KhuonBeTheoNcc`; export Excel `/BaoCao/ExportKhuonBeTheoNcc`.
- **Phân lớp**:
  - Repository `IKhuonBeRepository`: thêm query tổng hợp theo NCC và query danh sách khuôn cần chú ý.
  - Service `IBaoCaoService`: build `BaoCaoKhuonBeNccVM` và export Excel.
  - Controller `BaoCaoController`: chỉ gọi service và trả View/File.
  - ViewModel riêng trong `Models/ViewModels/ViewModels.cs`.
- **Chỉ tiêu**: tổng khuôn/NCC, số đang dùng/hỏng, tổng tờ đã in, tuổi thọ trung bình, định mức trung bình, tỷ lệ sử dụng so với định mức, số khuôn cần chú ý.
- **Khuôn cần chú ý**: khuôn đã hỏng, khuôn sắp chạm/vượt 80% định mức, hoặc khuôn chưa ghi nhận sử dụng.
- **File**: `Repositories/NghiepVuRepositories.cs`, `Services/AllServices.cs`, `Controllers/BaoCaoController.cs`, `Models/ViewModels/ViewModels.cs`, `Views/BaoCao/Index.cshtml`, `Views/BaoCao/KhuonBeTheoNcc.cshtml`.
- Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 27 — Tự gán mã khuôn bế theo mã vật tư kế tiếp
- **Mục tiêu**: Khi thêm khuôn bế mới, mã khuôn dùng chung quy luật mã vật tư `VTxxxxx`; form tự gán mã kế tiếp theo số lớn nhất hiện có trong `kho_vat_tu`, ví dụ đang lớn nhất `VT00528` thì thêm mới gợi ý `VT00529`.
- **Triển khai**:
  - `KhuonBeController.Form(id=0)` gọi `IDanhMucService.GenerateNextMaVatTuAsync()` để điền sẵn `KhuonBeVM.MaKhuon`.
  - `KhuonBeController.Save` nếu tạo mới mà mã khuôn trống thì tự sinh lại trước khi validate để tránh lỗi submit trực tiếp.
  - `KhuonBeService.CreateAsync` có lớp phòng vệ tự sinh mã nếu service được gọi với `MaKhuon` trống.
- **File**: `Controllers/KhuonBeController.cs`, `Services/AllServices.cs`.
- Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 28 — Đổi chọn vật tư Khuôn Bế/Định Mức sang datalist
- **Mục tiêu**: Khi danh mục vật tư nhiều, các combo/select vật tư trong form thêm Khuôn Bế và form thêm Định Mức khó tìm kiếm; đổi sang ô nhập có `datalist` để gõ mã/tên vật tư.
- **Triển khai**:
  - `Views/KhuonBe/_KhuonBeForm.cshtml`: trường `Vật tư liên kết` đổi từ select sang input+datalist, submit hidden `IdVatTu` như cũ.
  - `Views/DinhMuc/_DinhMucForm.cshtml`: trường `Vật tư` đổi từ select sang input+datalist, submit hidden `IdVatTu` như cũ.
  - `wwwroot/js/site.js`: thêm helper `syncHiddenFromDatalist` dùng chung cho modal AJAX.
- **Làm rõ nghiệp vụ mã khuôn**: không còn xung đột với mã khuôn auto-fill. Khi chọn vật tư liên kết có sẵn, UI tự đồng bộ `MaKhuon = MaVt` của vật tư được chọn và tự điền tên khuôn nếu đang trống. Nếu để trống vật tư liên kết, hệ thống giữ mã khuôn auto-fill kế tiếp và khi lưu sẽ tự tạo/liên kết vật tư mới theo mã/tên khuôn.
- **Fix sau test UI**: bổ sung event delegation và fallback parse trực tiếp mã `VTxxxxx` từ chuỗi datalist để đảm bảo khi chọn vật tư trong modal Khuôn Bế thì `MaKhuon` đổi ngay, không còn giữ mã auto-fill cũ. Sau phản hồi vẫn chưa chạy do script trong partial AJAX không được thực thi/cache JS, đã sửa thêm `openModal` để execute script trong partial, thêm inline local sync `__syncKhuonBeVatTuLocal` ngay trong `_KhuonBeForm.cshtml`, và bật `asp-append-version` cho `site.js`.
- **File**: `Views/KhuonBe/_KhuonBeForm.cshtml`, `Views/DinhMuc/_DinhMucForm.cshtml`, `wwwroot/js/site.js`.
- Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.

### Phiên 29 — Bổ sung NCC và thêm nhanh vật tư ở Nhập kho tài chính
- **Mục tiêu**: Trên `/NhapKhoTaiChinh/Create`, bổ sung cột **Nhà cung cấp** cạnh **Số PSP** bằng input+datalist; thêm nút **Thêm vật tư** để bổ sung vật tư ngay khi nhập kho mà không phải quay về module Danh mục.
- **Triển khai**:
  - Thêm cột nullable `id_nha_cung_cap` vào `kho_phieu_xk_tc_ct` và script `App_Data/Sql/20260629_add_nha_cung_cap_phieu_xk_tc_ct.sql`.
  - Entity `PhieuXKTCCt` thêm `IdNhaCungCap`, `TenNcc`; repository insert/select thêm join `kho_nha_cung_cap`.
  - `NhapKhoTaiChinhController.Create` nạp danh sách NCC, nhận list `idNhaCungCap`; thêm API `GetVatTuOptions()` để reload datalist sau khi thêm nhanh vật tư.
  - `Views/NhapKhoTaiChinh/Create.cshtml`: thêm cột NCC input+datalist, nút **Thêm vật tư**, reload danh sách vật tư sau khi modal thêm vật tư lưu thành công.
  - `Views/NhapKhoTaiChinh/Detail.cshtml`: hiển thị cột Nhà cung cấp.
  - `wwwroot/js/site.js`: `submitForm` hỗ trợ callback `window.quickVatTuSavedCallback` để modal thêm vật tư không reload trang nhập kho.
- **Lưu ý DB**: cần chạy script SQL mới trên DB thật trước khi lưu phiếu có NCC, nếu chưa chạy sẽ lỗi thiếu cột `id_nha_cung_cap`.
- Build kiểm tra: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, 0 warning.



- [x] Gộp nhiều PSP vào 1 đợt đề nghị xuất giấy
- [x] Gộp dòng chưa lấy/thiếu từ đợt đề nghị trước sang đợt tiếp theo
- [x] Chạy script `kho_schema.sql` trên MySQL
- [x] Insert dữ liệu danh mục thật
- [x] Upload PDF + parse tự động PSP: đã test với file mới, hoạt động ổn định; PhieuXKTC/Nhập kho tài chính không sử dụng đến, lưu ưu tiên cuối.
- [x] Export Excel bằng ClosedXML
- [x] UI set flag `la_cuon_thay_to_roi` khi tạo/sửa dòng vật tư trong PSP
- [x] Trang so sánh tuổi thọ khuôn bế theo NCC
- [x] Thêm transaction cho các method Service tạo nhiều bản ghi

## CÔNG VIỆC TIẾP THEO ĐỀ XUẤT

1. Rà soát dữ liệu khuôn bế thực tế: bổ sung NCC/định mức tuổi thọ cho các khuôn chưa có để báo cáo so sánh chính xác hơn.
2. Import PDF Nhập kho tài chính: hiện không sử dụng đến, lưu làm ưu tiên cuối cùng nếu nghiệp vụ phát sinh.

### Phiên 30 — Import Excel PSP
- **Mục tiêu**: Import Phiếu Sản Phẩm từ Excel `.xlsx` theo template Sheet1, header hàng 10, dữ liệu từ hàng 11; 1 file có thể chứa nhiều PSP, mỗi dòng là 1 vật tư giấy.
- **Đã triển khai**:
  - `Services/ExcelParserService.cs`: parse Excel bằng ClosedXML, mapping C/D/E/F/H/K/Q/R/T, chỉ lấy vật tư `Giấy`, match danh mục vật tư để lấy `id_vat_tu`/ĐVT.
  - `Controllers/PhieuSPController.cs`: thêm preview Excel, upload nhận `.xlsx`, loop từng PSP để import mới hoặc import sửa theo checkbox hiện có.
  - `Views/PhieuSP/Upload.cshtml`: chọn `.pdf,.xlsx`, nút **Xem trước**, datagrid preview.
  - `Views/PhieuSP/_ExcelPreview.cshtml`: thêm partial preview.
  - `Models/ViewModels/ViewModels.cs`: thêm `ExcelPreviewVM/ExcelPreviewRow`.
  - `Program.cs`: DI `IExcelParserService`.
- **Fix kèm theo**: `Views/DinhMuc/_TieuHaoForm.cshtml` sửa lỗi build `DateTime?.ToString("yyyy-MM")`.
- **Build kiểm tra**: ✅ `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi; còn 9 warning nullable cũ không liên quan Excel.
