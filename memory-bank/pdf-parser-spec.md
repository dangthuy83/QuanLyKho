# PDF/Excel Parser Specification — Phiếu Sản Phẩm (PSP)

Tài liệu này định nghĩa cấu trúc phân tích dữ liệu tự động (parse) từ file PDF và Excel Phiếu Sản Phẩm (PSP) tải lên hệ thống KhoQuanLy. Phần PDF dựa trên file mẫu thực tế `PSP001017_2026_HNI-OFF.pdf`; phần Excel dựa trên template đã chốt Sheet1, header hàng 10, dữ liệu từ hàng 11.

## Trạng thái triển khai cập nhật 23/06/2026

- 🟡 Đã có code triển khai parser PSP trong `Services/PdfParserService.cs` với interface `IPdfParserService`.
- 🟡 `PhieuSPController` đã có action upload PDF và `Views/PhieuSP/Upload.cshtml` đã tồn tại; hiện hỗ trợ import nhiều file PDF trong một lần gửi form.
- 🟡 Parser header đã được siết lại để chuẩn hóa dữ liệu text trước khi lưu DB: cắt phần bị dính nhãn kế tiếp, gom khoảng trắng, ưu tiên match pattern mã phiếu, và giới hạn độ dài theo schema thực tế để tránh lỗi `Data too long`.
- 🟡 Trường `Khách hàng` đã được tách theo nhãn kế tiếp thay vì chỉ regex bắt đến hết dòng, nhằm hạn chế hút dư nội dung khi text PDF bị dính nhiều cột/thông tin.
- 🟡 Trường `Khách hàng` hiện bị chặn cứng trước các nhãn `Số phiếu sản phẩm con`, `I. THÔNG TIN SẢN PHẨM CON`, `1. Tên sản phẩm`; trường `Số lượng cái` ưu tiên bắt số bám trực tiếp ngay sau nhãn để tránh dính số ở phần bên dưới.
- 🟡 Trường `Ngày nhận LSX` và `Ngày giao hàng` đã được parse theo cơ chế ưu tiên bắt trực tiếp ngày nằm ngay sau nhãn (`7.Ngày nhận LSX:23/06/2026`, `8.Ngày giao hàng:...`), sau đó mới fallback sang tách giữa các nhãn header; `NguoiTao` có fallback theo tên máy local khi test từ localhost.
- 🟡 Luồng import PSP đã bổ sung bước kiểm tra trùng `so_phieu` trước khi lưu DB. Nếu phiếu đã tồn tại, hệ thống bỏ qua phiếu trùng và đưa vào thông báo tổng hợp cuối cùng; áp dụng chung cho cả import 1 file và nhiều file.
- 🟡 Cần test end-to-end bằng PDF mẫu thực tế trước khi đánh dấu hoàn tất: kiểm tra header, dòng vật tư, xử lý số có dấu phân tách hàng nghìn, mapping vật tư, người tạo theo `may_tinh.json`, cảnh báo vật tư chưa khớp danh mục.
- ✅ Đã chốt nghiệp vụ bổ sung: màn hình import PDF PSP cần có checkbox **"Sửa phiếu sản phẩm"**. Khi không tick, giữ hành vi hiện tại: nếu `so_phieu` đã tồn tại thì bỏ qua và cảnh báo. Khi tick, chỉ cho cập nhật nếu `so_phieu` đã tồn tại; nếu không tìm thấy phiếu cũ thì báo lỗi và không import.
- ✅ Đã chốt: hệ thống **không tự tính lại số lượng vật tư theo số lượng sản phẩm**. Khi có phiếu sửa, phòng ban khác đã phát hành PDF mới và số lượng vật tư trên PDF sửa là dữ liệu chuẩn để cập nhật.
- ✅ Đã chốt: import PDF ở chế độ sửa phiếu phải update/merge dữ liệu theo PDF mới, ghi lịch sử thay đổi, nhưng không làm mất lịch sử nhận/xuất đã phát sinh.
- ✅ Đã chốt: file PDF sau upload lưu trên **server web app** hoặc ổ mạng mà server truy cập được; không lưu rải rác trên máy local người dùng. Đường dẫn lưu file PDF không được hard-code, cần cấu hình được trong web app hoặc tối thiểu qua cấu hình hệ thống.
- ✅ Cập nhật 24/06/2026: với PDF thật, PdfPig có thể trích phần **VẬT TƯ SẢN XUẤT** thành một chuỗi liên tục không có xuống dòng (`VẬT TƯ SẢN XUẤT...LoạiCăn chỉnhGiấyGiấy Duplex...`). Parser phải có fallback nhận diện dòng vật tư bằng regex trên text liên tục, không chỉ dựa vào split dòng. Đã triển khai trong `Services/PdfParserService.cs` và test parse được vật tư + match danh mục.
- ✅ Cập nhật 30/06/2026: đã bổ sung import Excel PSP bằng `Services/ExcelParserService.cs` (`IExcelParserService.ParsePspAsync(Stream)`) dùng ClosedXML đọc `.xlsx`, hỗ trợ 1 file chứa nhiều PSP, mỗi dòng = 1 vật tư và header PSP lặp lại. `PhieuSPController.Upload` nhận cả `.pdf,.xlsx`; nếu Excel thì parse danh sách PSP rồi loop import mới/sửa theo checkbox **Sửa phiếu sản phẩm** giống luồng PDF. Có API `/PhieuSP/PreviewExcel` để xem trước datagrid trên `Views/PhieuSP/Upload.cshtml`; thêm ViewModel `ExcelPreviewVM/ExcelPreviewRow` và partial `_ExcelPreview.cshtml`.
- ❌ Tài liệu này chỉ áp dụng cho PSP, chưa phải spec cho Phiếu XKTC/Nhập kho tài chính.

---

## 1. Phân tích cấu trúc Metadata (Header)

Dữ liệu Header sẽ được map trực tiếp vào bảng `kho_phieu_sp`.

| Trường dữ liệu trong PDF | Giá trị thực tế trích xuất | Thuộc tính Entity `PhieuSP` | Loại dữ liệu | Ghi chú & Logic Parse |Trường trên bảng 'kho_phieu_sp'|
|---|---|---|---|---|
| **Mã lệnh sản xuất** | `000375/2026/LSX_KDNB` | `SoLsx` | `VARCHAR(50)` | Tìm sau chuỗi `LSX: ` và trước dấu `(` | so_lsx|
| **Số phiếu sản phẩm** | `001017/2026/HNI-OFF` | `SoPhieu` | `VARCHAR(50)` | Tìm sau chuỗi `Số phiếu sản phẩm con:` | so_phieu|
| **Tên sản phẩm** | `Vỏ màu/Rio mini:140-8014-01 KT 343*333*306mm - 522000828` | `TenSanPham` | `VARCHAR(255)` | Tìm sau chuỗi `1. Tên sản phẩm:` (lấy của sản phẩm con) |ten_san_pham|
| **Mã sản phẩm** | `ACO.SYV.OTR.002` | `MaSanPham` | `VARCHAR(50)` | Tìm sau chuỗi `2. Mã sản phẩm:` |ma_san_pham|
| **Khách hàng** | `Công ty Cổ phần SYV` | `Khách hàng` | `VARCHAR(200)` | Tìm sau chuỗi `3. Khách hàng:` |khach_hang|
| **Số lượng cái** | `1710` | `SoLuongSp` | `DECIMAL(12,3)`| Tìm sau chuỗi `4.Số lượng (Cái):` hoặc `3.Số lượng:` (con), tự động quy đổi `1.710` hoặc `1,710` về số nguyên `1710` | so_luong_sp|
| **Ngày lập lệnh/nhận** | `18/06/2026` | `NgayLenh` | `DATE` | Tìm sau chuỗi `7.Ngày nhận LSX:` |ngay_lenh|
| **Ngày giao hàng** | `01/07/2026` | `NgayGiaoHang` | `DATE` | Tìm sau chuỗi `8.Ngày giao hàng:` |ngay_giao_hang|
| **Người tạo** | `HungPV9` | `NguoiTao` | `VARCHAR(100)` | không lấy theo file PDF PSP, đọc tên máy từ file may_tinh.json, trong file có IP máy tính và tên máy tính|  |nguoi_tao|
| **Trạng thái mặc định** | `chua_hoan_thanh` | `TrangThai` | `VARCHAR(20)` | Gán cứng giá trị khi import |trang_thai|

---

## 2. Phân tích chi tiết vật tư (VẬT TƯ SẢN XUẤT)

Dữ liệu dòng vật tư nằm ở bảng dưới mục **"VẬT TƯ SẢN XUẤT"** hoặc dưới dòng **"Nguyên liệu đích danh / SL xuất kho / Tiêu hao sản xuất"**.

Dữ liệu sẽ được map trực tiếp vào bảng `kho_phieu_sp_vat_tu`.

### Cấu trúc chuỗi text thô thu được từ PDF:
```text
GiấyGiấy Duplex 230 K695 Hanchang 0,40  1,00  6,60 Kg 498  1 69.5x72.5 4.280  3.646  600 
```

Một số file PDF thật có thể bị trích thành chuỗi liên tục:
```text
VẬT TƯ SẢN XUẤTKích thước (cm)Số lượng giấy in(Tờ)...LoạiCăn chỉnhGiấyGiấy Duplex 230 K695 Hanchang 0,40 1,00 6,60 Kg 498 1 69.5x72.5 4.280 3.646 600 (Ký, họ tên)...
```
Trong trường hợp này parser phải bỏ phần header/footer dính trước/sau, lấy đúng tên vật tư bắt đầu từ `GiấyGiấy...`, khử lặp thành `Giấy...`, nhưng **không xóa chữ `Giấy` khỏi tên vật tư**.

### Ánh xạ trường dữ liệu:

| Số Thứ Tự trong dòng | Cụm text / Giá trị | Thuộc tính Entity `PhieuSPVatTu` | Loại dữ liệu | Ghi chú & Logic Parse |Trường trên bảng 'kho_phieu_sp_vat_tu'|
|---|---|---|---|---|
| **Loại vật tư** | `Giấy` (từ `Giấy...`) | N/A | `ENUM` | trích xuất để biết rằng đây là giấy |
| **Tên vật tư** | `Giấy Duplex 230 K695 Hanchang` | `ChungLoaiText` | `VARCHAR(200)` | Tên chủng loại thô. Hệ thống sẽ cố gắng tìm trong `kho_vat_tu` vật tư có `ten_vt` khớp với tên này để gán `IdVatTu`, nếu không tìm thấy sẽ để `IdVatTu = null` và lưu vào `ChungLoaiText` và hiện thông báo nhắc nhở yêu cầu user bổ sung vật tư này vào bảng 'kho_vat'tu' |chung_loai_text|
| **Tỷ lệ xả** | `0,40` (0.4%) | N/A | `DECIMAL` | Bỏ qua|
| **Tỷ lệ in** | `1,00` (1%) | N/A | `DECIMAL` | Bỏ qua |
| **Tỷ lệ gia công** | `6,60` (6.6%) | N/A | `DECIMAL` | Bỏ qua|
| **Đơn vị tính** | `Kg` | `DonViTinh` | `VARCHAR(20)` | tìm trong `kho_vat_tu` vật tư có `ten_vt` khớp với tên này để gán đơn vị tính, nếu không tìm thấy lấy theo trên PSP) |don_vi_tinh|
| **Số lượng yêu cầu** | `498` | `SoLuongYeuCau` | `DECIMAL(12,3)`| Trọng lượng xuất kho thực tế (định mức kg) |so_luong_yeu_cau|
| **STT dòng** | `1` | N/A | `INT` | bỏ qua |
| **Kích thước** | `69.5x72.5` | `KichThuoc` | `VARCHAR(100)` | Kích thước khổ xả |kich_thuoc|
| **Số lượng tờ in** | `4280` | `SoLuongToRoi` | `DECIMAL(12,3)`| Số lượng tờ cấp phát (giấy in). Tách dấu `.`, xử lý từ `4.280` -> `4280` |so_luong_to_roi|
| **Số lượng cần đạt**| `3646` | N/A | `DECIMAL` | bỏ qua |
| **Cao căn chỉnh** | `600` | N/A | `DECIMAL` | bỏ qua|

---

## 3. Thư viện kỹ thuật & Giải pháp Parse dự kiến

- Thư viện .NET đề xuất: **`PdfPig`** (Mã nguồn mở, hiệu năng cao, đọc text chính xác theo tọa độ hoặc cụm ký tự dòng, không cần cài tool ngoài).
- Phương pháp Parse: Sử dụng các biểu thức chính quy (Regex) và phân tích dòng (Line-by-Line / Keyword Search).

---

## 4. Kế hoạch thống nhất trước khi code

Phản hồi của tôi như sau:
- Các trường dữ liệu ánh xạ trên đã đúng và đủ ( tôi có điểu chỉnh lại 1 số thông tin và chỉ rõ lưu vào các trường nào trong các bảng liên quan).
- nhất trí quy tắc làm tròn số lượng (VD: `1.710` -> `1710` cái, `4.280` -> `4280` tờ) vì dấu chấm này thể hiện chia tách hàng nghìn (trừ mục kích thước). Tuy nhiên, khi in ra phiếu đề nghị xuất giấy có dấu phân tách hàng nghìn và hàng thập phân ( dùng dấu "," phân tách hàng nghìn, "." là thập phân).
- không cần lưu cả `STT/Tỷ lệ xả/in/GC (%)` và `Số lượng cần đạt/Căn chỉnh` vào DB 

---

## 5. Nghiệp vụ import PDF ở chế độ sửa Phiếu Sản Phẩm

### 5.1. Checkbox "Sửa phiếu sản phẩm"

Màn hình upload/import PDF PSP cần bổ sung checkbox:

```text
[ ] Sửa phiếu sản phẩm
```

Quy tắc:

- **Không tick checkbox**:
  - Nếu `so_phieu` chưa tồn tại: import mới.
  - Nếu `so_phieu` đã tồn tại: bỏ qua phiếu đó và cảnh báo thân thiện như hiện tại.
- **Có tick checkbox**:
  - Nếu `so_phieu` không tồn tại: báo lỗi, không import.
  - Nếu `so_phieu` đã tồn tại: import PDF sửa và cập nhật/merge dữ liệu vào phiếu cũ.

### 5.2. Nguyên tắc nghiệp vụ đã chốt

- Hệ thống kho **không tự tính lại số lượng vật tư theo số lượng sản phẩm**.
- Khi nhận PDF sửa, số lượng sản phẩm và số lượng vật tư trên PDF đã được phòng ban khác tính lại; hệ thống chỉ cập nhật theo dữ liệu PDF mới.
- Import sửa phải ghi lịch sử thay đổi.
- Không được làm mất lịch sử nhận/xuất đã phát sinh trước đó.
- Nếu số lượng đã nhận lớn hơn số lượng yêu cầu mới sau khi sửa, không xử lý phức tạp: giữ `so_luong_da_nhan`, cập nhật `so_luong_yeu_cau`, trạng thái dòng là `du`. Nếu thực tế dư vật tư thì xử lý bằng module **Trả Lại Kho**.

### 5.3. Quy tắc merge chi tiết vật tư khi import phiếu sửa

Khi import PDF sửa, hệ thống so sánh danh sách dòng vật tư cũ trong `kho_phieu_sp_vat_tu` với danh sách dòng vật tư mới parse từ PDF.

Tiêu chí xác định cùng dòng vật tư đề xuất:

1. Ưu tiên cùng `id_vat_tu` nếu parser match được danh mục.
2. Nếu chưa match được danh mục, dùng `chung_loai_text` đã chuẩn hóa.
3. Kết hợp thêm `kich_thuoc` và `don_vi_tinh` để tránh nhầm dòng.

Quy tắc xử lý:

- **Cùng vật tư, chỉ đổi số lượng/kích thước/số tờ**:
  - Update dòng cũ theo dữ liệu PDF mới.
  - Giữ nguyên `so_luong_da_nhan`.
  - Tính lại `trang_thai_nhan` đơn giản:
    - `so_luong_da_nhan == 0` → `chua_nhan`
    - `0 < so_luong_da_nhan < so_luong_yeu_cau` → `thieu`
    - `so_luong_da_nhan >= so_luong_yeu_cau` → `du`
- **Dòng vật tư mới xuất hiện trong PDF sửa**:
  - Insert dòng mới vào `kho_phieu_sp_vat_tu`.
  - `so_luong_da_nhan = 0`, `trang_thai_nhan = chua_nhan`.
- **Dòng vật tư cũ không còn trong PDF sửa**:
  - Nếu PSP/dòng đó chưa phát sinh nhận/xuất: xóa dòng cũ khỏi `kho_phieu_sp_vat_tu`.
  - Nếu đã phát sinh nhận/xuất: không xóa; giữ để bảo toàn lịch sử và đánh dấu không cần nhận tiếp phần còn lại bằng `trang_thai_nhan = khong_can` hoặc ghi chú phù hợp.
- **Đổi vật tư A sang vật tư B**:
  - Nếu dòng A chưa phát sinh nhận/xuất: có thể xóa/replace theo PDF mới.
  - Nếu dòng A đã phát sinh nhận/xuất: không update đè A thành B vì sẽ làm sai lịch sử; giữ dòng A, đánh dấu không cần nhận tiếp nếu phù hợp, và insert dòng B mới.

### 5.4. Ghi lịch sử khi import phiếu sửa

Trước mắt dùng bảng hiện có `kho_phieu_sp_lich_su_sua` để ghi lịch sử dạng text trong `ly_do`.

Cần ghi được tối thiểu:

- File PDF sửa được import.
- Người/máy import.
- Thay đổi header quan trọng nếu có.
- Dòng vật tư sửa số lượng.
- Dòng vật tư thêm mới.
- Dòng vật tư bị bỏ/không cần nhận tiếp.
- Trường hợp vật tư cũ được thay bằng vật tư mới.

Nếu sau này cần audit sâu hơn, có thể thiết kế bảng log riêng như `kho_phieu_sp_import_log` và `kho_phieu_sp_import_log_ct`.

### 5.5. Lưu trữ file PDF PSP

Quy tắc đã chốt:

- PDF sau upload lưu trên **server web app** hoặc ổ mạng mà server có quyền truy cập.
- Không lưu phân tán trên máy local của người mở web app.
- Không hard-code đường dẫn lưu PDF trong code.
- Nên cho phép thay đổi vị trí lưu file PDF trên web app; tối thiểu phải cấu hình được qua cấu hình hệ thống.
- Về lâu dài không nên lưu trong `wwwroot/uploads`; nên lưu ngoài `wwwroot` và serve file qua controller để kiểm soát truy cập.
- Khi import PDF sửa thành công: giữ file PDF cũ để truy vết, lưu file PDF mới và cập nhật phiếu trỏ đến file mới nhất.
- Nếu upload qua browser thì server không thể xóa file gốc trên máy user. Nếu sau này import từ thư mục server/LAN thì có thể move/xóa file nguồn sau import thành công; ưu tiên move sang `processed/archive` để an toàn hơn.

---

## 6. Nghiệp vụ import Excel Phiếu Sản Phẩm

### 6.1. Template Excel đã chốt

| Mục | Giá trị |
|---|---|
| Sheet | `Sheet1` |
| Hàng header | 10 |
| Hàng dữ liệu | 11+ |
| 1 file = nhiều PSP | Có; mỗi dòng = 1 vật tư, header PSP lặp lại đầy đủ |
| Lọc vật tư | Chỉ lấy vật tư bắt đầu bằng `Giấy`, giống logic PDF parser |

### 6.2. Mapping cột Excel

| Cột Excel | Entity | Ghi chú |
|---|---|---|
| C | `PhieuSP.SoPhieu` | Bắt buộc để nhóm PSP |
| D | `PhieuSP.TenSanPham` | Header PSP |
| E | `PhieuSP.NgayLenh` | Ngày nhận LSX |
| F | `PhieuSP.KhachHang` | Header PSP |
| H | `PhieuSP.SoLuongSp` | Số lượng sản phẩm |
| K | `PhieuSPVatTu.ChungLoaiText` | Tên vật tư, chỉ lấy `Giấy...` |
| Q | `PhieuSPVatTu.KichThuoc` | Kích thước |
| R | `PhieuSPVatTu.SoLuongToRoi` | Số lượng tờ rời |
| T | `PhieuSPVatTu.SoLuongYeuCau` | Số lượng yêu cầu |
| ĐVT | `PhieuSPVatTu.DonViTinh` | Mặc định `Kg`, nếu match được danh mục thì lấy theo `kho_vat_tu.don_vi_tinh` |

### 6.3. Luồng xử lý Excel hiện tại

1. User chọn file `.xlsx` trên màn `/PhieuSP/Upload`.
2. Bấm **Xem trước** gọi AJAX `/PhieuSP/PreviewExcel`.
3. `ExcelParserService` đọc `Sheet1`, nhóm dữ liệu theo `SoPhieu`, match vật tư với danh mục `kho_vat_tu` bằng chuẩn hóa tên.
4. UI hiển thị datagrid preview: số phiếu, tên SP, khách hàng, SL SP, tên VT, kích thước, SL tờ rời, SL yêu cầu, ĐVT, cảnh báo.
5. User bấm **Import**:
   - Không tick sửa: PSP đã tồn tại bị bỏ qua; PSP mới được tạo bằng `PhieuSPService.CreateAsync`.
   - Có tick sửa: PSP phải tồn tại; hệ thống gọi `PhieuSPService.ImportSuaPhieuAsync` để update/merge giống luồng PDF, ghi lịch sử và bảo toàn phát sinh nhận/xuất.

### 6.4. File triển khai Excel PSP

| File | Trạng thái | Ghi chú |
|---|---|---|
| `Services/ExcelParserService.cs` | Đã thêm | Parser Excel PSP bằng ClosedXML |
| `Controllers/PhieuSPController.cs` | Đã sửa | Inject `IExcelParserService`, preview Excel, upload `.xlsx`, loop nhiều PSP |
| `Views/PhieuSP/Upload.cshtml` | Đã sửa | `accept=".pdf,.xlsx"`, nút xem trước, datagrid preview |
| `Views/PhieuSP/_ExcelPreview.cshtml` | Đã thêm | Partial preview server-side để tái sử dụng sau này |
| `Models/ViewModels/ViewModels.cs` | Đã sửa | Thêm `ExcelPreviewVM`, `ExcelPreviewRow` |
| `Program.cs` | Đã sửa | DI `IExcelParserService, ExcelParserService` |

Build kiểm tra 30/06/2026: `dotnet build -p:UseSharedCompilation=false -p:DebugType=None -p:UseAppHost=false -o build_verify` thành công 0 lỗi, còn 9 warning nullable cũ không liên quan import Excel.
