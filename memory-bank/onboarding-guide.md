# Hướng dẫn Onboarding — Lập trình viên mới

## 🚀 Bước 1: Clone & Cài đặt

### Yêu cầu
- .NET SDK 8.0
- MySQL 8.0+ (có sẵn trên máy)
- Visual Studio Code / Visual Studio 2022

### Cài đặt

```bash
# 1. Kiểm tra .NET version
dotnet --version  # Phải >= 8.0

# 2. Khôi phục package
cd e:\THUYDD\VSCode\KhoQuanLy
dotnet restore

# 3. Kiểm tra build
dotnet build
```

Nếu có lỗi NuGet, chạy:
```bash
dotnet nuget locals all --clear
dotnet restore --force
```

### Database

```bash
# 1. Mở MySQL Workbench
# 2. Tạo database nếu chưa có:
CREATE DATABASE IF NOT EXISTS payroll_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

# 3. Chạy script kho_schema.sql
# (Script này tạo 19 bảng kho_* và dữ liệu mẫu danh mục)
```

**⚠️ Lưu ý**: File `kho_schema.sql` có thể ở Desktop hoặc cần tạo lại từ phiên thiết kế. Nếu chưa có, cần hỏi user.

### Cấu hình connection string

Sửa file `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=payroll_db;User ID=OffsetSauin;Password=123456a@A;CharSet=utf8mb4;AllowPublicKeyRetrieval=true;SslMode=None;"
  }
}
```

Thay đổi `User ID` và `Password` cho phù hợp với máy.

## 🏃 Bước 2: Chạy ứng dụng

```bash
dotnet run
```

Truy cập: `http://localhost:5000`

## 📁 Bước 3: Cấu trúc code

Đọc theo thứ tự:
1. **`memory-bank/project-overview.md`** — Tổng quan dự án
2. **`memory-bank/architecture.md`** — Kiến trúc chi tiết
3. **`Program.cs`** — Entry point + DI
4. **`Models/Entities/Entities.cs`** — Entity classes (16 class)
5. **`Models/ViewModels/ViewModels.cs`** — ViewModel/row classes (25 class)
6. **`Repositories/`** — Data access (Dapper + SQL)
7. **`Services/AllServices.cs`** — Business logic
8. **`Controllers/`** — Các controller
9. **`Views/`** — UI templates

## 🧭 Bước 4: Routes chính

| Route | Mô tả |
|---|---|
| `/` | Dashboard |
| `/DanhMuc/VatTu` | Danh sách vật tư |
| `/PhieuSP` | Phiếu sản phẩm |
| `/DeNghiXuat` | Đợt đề nghị xuất giấy |
| `/NhapKhoTaiChinh` | Nhập kho tài chính |
| `/TraLaiKho` | Trả lại kho |
| `/GiayTietKiem` | Giấy tiết kiệm |
| `/KhuonBe` | Khuôn bế |
| `/DinhMuc` | Định mức & tiêu hao |
| `/TonKho` | Tồn kho & cảnh báo |
| `/BaoCao` | Báo cáo |

## 🛠️ Bước 5: Luồng nghiệp vụ chính

### 1. Tạo phiếu sản phẩm → Xuất giấy
```
Tạo/import PSP ở trạng thái 'chua_hoan_thanh' → Thêm/kiểm tra vật tư (giấy)
→ Vào Đề nghị xuất giấy → Chọn PSP → Tạo đợt → In phiếu gửi thủ kho
→ Thủ kho điền "Thực nhận" → Đợt hoàn thành → Chuyển PSP sang 'hoan_thanh' khi hoàn tất
```

Nếu đợt trước còn dòng `chua_lay`/`thieu`, kể cả đợt cũ đã bấm hoàn thành/chốt đợt nhưng chưa nhập đủ thực nhận, vào màn hình tạo đề nghị xuất → bấm **Tải dữ liệu** ở khối **"Dòng chưa nhận/nhận thiếu từ đợt trước"** → chọn dòng còn thiếu → hệ thống tạo dòng mới với số lượng còn lại và tự tránh gộp trùng. Các PSP/dòng vật tư đang nằm ở khối này sẽ không còn hiện như dòng chọn mới để tránh đề nghị trùng.

Khi nhập hoặc sửa **Thực nhận** ở chi tiết đợt đề nghị, hệ thống đồng bộ tổng số đã nhận về dòng vật tư của Phiếu sản phẩm (`so_luong_da_nhan`, `trang_thai_nhan`). Nếu sửa số thực nhận đã lưu, tồn kho chỉ điều chỉnh theo phần chênh lệch so với số cũ, không trừ lặp toàn bộ số mới. Các dòng đã nhận đủ sẽ không còn xuất hiện ở màn hình tạo đợt đề nghị mới.

Cập nhật ngày 25/06/2026: khi mở màn hình tạo đợt đề nghị xuất giấy, khối **Dòng chưa nhận/nhận thiếu từ đợt trước** được tự tải, không cần bấm “Tải dữ liệu”. Nếu chọn các dòng pending này, Service tạo dòng mới theo số lượng còn thiếu và vẫn chống gộp trùng với dòng chọn mới.

Cập nhật bổ sung ngày 25/06/2026: khi tạo đợt đề nghị xuất giấy, dòng vật tư PSP bắt buộc phải liên kết được `id_vat_tu` trong `kho_vat_tu`. Nếu import PDF PSP parse ra dòng vật tư nhưng không match danh mục, hệ thống vẫn lưu dòng PSP dạng text để user kiểm tra, đồng thời hiển thị cảnh báo import. Khi tạo đợt, Service sẽ rollback và báo rõ dòng chưa có/liên kết vật tư thay vì tạo đợt thiếu dòng âm thầm.

Cập nhật bổ sung tiếp ngày 25/06/2026: nếu user đã thêm vật tư còn thiếu vào danh mục sau khi import PDF, màn tạo Đề nghị xuất giấy sẽ tự rà lại các dòng PSP chưa có `id_vat_tu` và liên kết theo tên `chung_loai_text` ↔ `kho_vat_tu.ten_vt`. Khi mở/tải lại màn tạo đợt, nếu match được thì không cần import lại PDF; dòng vật tư sẽ tự cập nhật liên kết và tạo đợt được.

Cập nhật ngày 25/06/2026: nghiệp vụ **Cấp cuộn thay tờ rời** đã có vật tư thực cấp riêng. Dòng PSP giữ nguyên giấy theo PDF ở `id_vat_tu/chung_loai_text`; nếu tick `la_cuon_thay_to_roi` thì phải chọn `id_vat_tu_thay_the` từ danh mục giấy cuộn (`loai_vat_tu='giay_cuon'`). Các nghiệp vụ Đề nghị xuất giấy, nhập thực nhận, tồn kho và báo cáo dùng vật tư thực tế theo quy tắc `id_vat_tu_thay_the ?? id_vat_tu`, còn UI vẫn hiển thị cả “Giấy theo PDF” và “Giấy cuộn thay thế/thực cấp”. Trước khi chạy code mới trên DB thật, cần chạy SQL trong `App_Data/Sql/20260625_add_vat_tu_thay_the_psp.sql`.

Lưu ý nghiệp vụ quan trọng: **Đề nghị xuất giấy không phải xuất khỏi kho phân xưởng**. Đây là đề nghị với kho tổng/bên kho để cấp giấy cho phân xưởng, vì vậy khi nhập **Thực nhận** thì app ghi nhận là **nhập kho phân xưởng** (`nhap_de_nghi`, cộng tồn). Chỉ chức năng **Xuất kho nhanh** trong module Tồn kho mới là xuất từ kho phân xưởng ra sản xuất/tiêu dùng (`xuat_khac`, trừ tồn).

Cập nhật ngày 24/06/2026: khi toàn bộ dòng vật tư cần nhận của PSP đã đủ, Service tự đồng bộ header `kho_phieu_sp.trang_thai` sang `hoan_thanh`. Danh sách `/PhieuSP` cũng tính trạng thái hiển thị dựa trên chi tiết vật tư/tổng thực nhận để tránh trường hợp Detail đã "Đủ" nhưng Index vẫn hiện "Chưa HT" do dữ liệu cũ chưa cập nhật header.

Cập nhật ngày 25/06/2026: đã test end-to-end luồng Đề nghị xuất giấy bằng harness gọi Service thật trên DB thật với dữ liệu test và cleanup sau test. Kết quả xác nhận: nhập/sửa Thực nhận cộng tồn theo chênh lệch, ghi `nhap_de_nghi`; dòng pending sang đợt kế tiếp tính đúng phần còn thiếu và tránh trùng; Xuất kho nhanh ghi `xuat_khac` âm; báo cáo Nhập/Xuất tính đúng Tổng nhập/Tổng xuất. Cùng ngày đã đồng bộ 3 dòng dữ liệu cũ chưa cập nhật `kho_phieu_sp_vat_tu.so_luong_da_nhan/trang_thai_nhan`; audit lại còn 0 dòng lệch.

Cập nhật ngày 25/06/2026: các Service trừ tồn đã được siết chống tồn âm. Xuất kho nhanh, trả lại kho, tiêu hao thực tế và sửa giảm thực nhận đề nghị đều kiểm tra tồn hiện tại trước khi trừ; nếu vượt tồn thì rollback và báo lỗi.

Ghi chú hiện tại: luồng import PDF PSP đã có code (`PdfParserService` + `PhieuSPController.Upload`) nhưng cần test end-to-end bằng PDF mẫu trước khi coi là hoàn tất ổn định.

Khi import PDF PSP, dữ liệu phải được lưu đủ 2 lớp: header phiếu vào `kho_phieu_sp` và phần **VẬT TƯ SẢN XUẤT** vào `kho_phieu_sp_vat_tu`. Dòng vật tư PDF cần map tối thiểu `id_vat_tu` nếu match danh mục, `chung_loai_text`, `kich_thuoc`, `so_luong_yeu_cau`, `so_luong_to_roi`, `don_vi_tinh`. Không lưu STT, tỷ lệ xả/in/gia công, số lượng cần đạt, căn chỉnh.

Ghi chú nghiệp vụ đã chốt ngày 23/06/2026: import PDF PSP cần bổ sung chế độ **Sửa phiếu sản phẩm** bằng checkbox trên màn hình upload. Khi tick checkbox, `so_phieu` phải tồn tại; nếu không tồn tại thì báo lỗi. Nếu tồn tại thì hệ thống update/merge dữ liệu theo PDF sửa, ghi lịch sử, không làm mất lịch sử nhận/xuất. Hệ thống không tự tính lại số lượng vật tư theo số lượng sản phẩm; số lượng vật tư lấy theo PDF sửa.

Quy tắc merge phiếu sửa: cùng vật tư thì update số lượng yêu cầu và giữ số đã nhận; vật tư mới thì insert dòng mới; vật tư cũ không còn trong PDF sửa thì xóa nếu chưa phát sinh nhận/xuất, còn nếu đã phát sinh thì giữ để bảo toàn lịch sử. Nếu số đã nhận vượt yêu cầu mới thì để trạng thái `du`, phần dư nếu cần xử lý qua module Trả Lại Kho.

File PDF PSP sau upload lưu trên server web app hoặc ổ mạng server truy cập được. Không hard-code đường dẫn lưu; lâu dài nên cho cấu hình trên web app và không lưu phụ thuộc `wwwroot/uploads`.

Cập nhật kỹ thuật ngày 23/06/2026: đã có `IPdfStorageService`/`PdfStorageService` để lưu PDF PSP. Đường dẫn lấy từ cấu hình `Storage:PspPdfPath`; nếu để trống thì fallback vào `App_Data/PspPdf` ngoài `wwwroot`. `PhieuSPController.Upload` không còn hard-code `wwwroot/uploads` và kiểm tra trùng `so_phieu` trước khi lưu file để tránh file mồ côi.

Cập nhật kỹ thuật ngày 23/06/2026: màn hình `Views/PhieuSP/Upload.cshtml` đã có checkbox **Sửa phiếu sản phẩm đã tồn tại**. Controller nhận flag `suaPhieuSanPham`; nếu tick sửa mà `so_phieu` không tồn tại thì báo lỗi; nếu tồn tại thì lưu PDF mới và gọi `PhieuSPService.ImportSuaPhieuAsync` để update header, merge dòng vật tư, ghi lịch sử, bảo toàn dòng đã phát sinh nhận/xuất.

Cập nhật kỹ thuật ngày 23/06/2026: parser vật tư trong `PdfParserService` đã được siết để map phần **VẬT TƯ SẢN XUẤT** vào `kho_phieu_sp_vat_tu` chắc hơn: không cắt thiếu token tên vật tư, xác thực kích thước/đơn vị, lấy đúng `so_luong_yeu_cau`, `so_luong_to_roi`, `don_vi_tinh`, và cải thiện match danh mục vật tư.

Cập nhật kỹ thuật ngày 24/06/2026: đã test PDF PSP thật và bổ sung fallback parser cho trường hợp PdfPig trích bảng vật tư thành một chuỗi liên tục không có xuống dòng. Parser hiện parse được vật tư từ các mẫu `001017/2026/HNI-OFF` và `001027/2026/HNI-OFF`, giữ chữ `Giấy` trong tên vật tư, match được danh mục, và import sửa đã merge được dòng vào `kho_phieu_sp_vat_tu` + ghi lịch sử. Storage PDF PSP đã test OK cả fallback `App_Data/PspPdf` và đường dẫn cấu hình.

Cập nhật dữ liệu ngày 24/06/2026: đã audit DB thật để tìm PSP có header nhưng thiếu vật tư. Đã bổ sung vật tư cho `001027/2026/HNI-OFF` và `001018/2026/HNI-OFF` bằng chế độ import sửa; audit lại còn 0 PSP thiếu vật tư. `PhieuSPService.ImportSuaPhieuAsync` cũng đã giới hạn độ dài nội dung `ly_do` khi ghi lịch sử để tránh lỗi `Data too long` trên DB thật.

### 2. Nhập kho tài chính (vật tư từ ngoài vào)
```
Vào Nhập kho tài chính → Tạo phiếu → Nhập số chứng từ vs số thực tế
→ Hệ thống tự cập nhật tồn kho → Cảnh báo thiếu → Bỏ qua + lý do
```

### Bổ sung module 1 — Danh mục vật tư

Cập nhật ngày 25/06/2026: danh sách `/DanhMuc/VatTu` có nút **Xuất kho nhanh** ngay trên từng dòng vật tư để mở popup xuất kho của module Tồn kho. Khi bấm **Thêm vật tư**, trường **Mã vật tư** tự được điền mã kế tiếp theo mẫu `VT00000`; ví dụ mã lớn nhất hiện có là `VT00528` thì form mới hiển thị `VT00529`. Logic sinh mã nằm trong Service/Repository, Controller chỉ gán ViewModel cho form.

Cập nhật ngày 25/06/2026: chi tiết nhập kho tài chính dùng 1 cột **Tên vật tư** thay cho 2 cột “Mô tả vật tư” và “Vật tư liên kết”. Người dùng gõ/chọn vật tư từ danh mục bằng mã/tên; hệ thống tự lưu liên kết `id_vat_tu`, tự điền ĐVT theo danh mục và vẫn lưu text tên vật tư vào `mo_ta_vat_tu` để truy vết. Cột **Số PSP** đã có danh sách PSP để liên kết nếu vật tư/chứng từ cần gắn phiếu sản phẩm.

Cập nhật bổ sung ngày 25/06/2026: các vị trí chọn **vật tư** và **phiếu sản phẩm** trong các module Phiếu Sản Phẩm, Nhập kho tài chính, Trả lại kho, Giấy tiết kiệm dùng ô nhập có `datalist` thay cho combo/select. Người dùng gõ mã/tên vật tư hoặc số PSP để lọc/chọn; form vẫn submit hidden id để giữ nguyên binding và Service hiện có. Các select không phải vật tư/PSP như trạng thái, lý do, bộ phận vẫn giữ dạng select.

### 3. Trả lại kho
```
Vào Trả lại kho → Chọn vật tư → Nhập số lượng → Lưu
→ Tồn kho phân xưởng giảm
```

### 4. Khuôn bế
```
Tạo khuôn → Ghi nhận PSP đầu tiên → Ghi nhận sử dụng từng PSP
→ Khi hỏng: đánh dấu hỏng → Tạo phiên bản mới
```

Quy tắc phiên bản hiện tại: khuôn/vật tư khuôn bế mới luôn mặc định `v1`, dù được tạo từ `/DanhMuc/VatTu` hay tạo trực tiếp từ `/KhuonBe`. Không tự tăng version chỉ theo `Tên khuôn`. Chỉ tạo version mới khi người dùng thật sự thay thế một khuôn hỏng/cùng khuôn thật sự.

Cập nhật ngày 25/06/2026: Danh mục vật tư và module Khuôn Bế là 2 bảng khác nhau (`kho_vat_tu` và `kho_khuon_be`). Nếu người dùng đã nhập nhiều vật tư loại `khuon_be` trong `/DanhMuc/VatTu`, khi mở `/KhuonBe` hệ thống sẽ tự đồng bộ các vật tư khuôn bế đang active chưa có trong `kho_khuon_be` sang module Khuôn Bế. Trên danh sách vật tư, các dòng loại Khuôn bế có thêm icon kéo sang module Khuôn Bế.

Nếu tạo mới trực tiếp trong `/KhuonBe`, Service hiện gọi `EnsureVatTuKhuonBeAsync` để tự tạo/liên kết bản ghi tương ứng trong `kho_vat_tu` loại `khuon_be` khi danh mục chưa có mã/tên đó. Từ ngày 25/06/2026, `kho_khuon_be` có `id_vat_tu` để liên kết chặt về danh mục; cần chạy script `App_Data/Sql/20260625_add_id_vat_tu_khuon_be.sql` trên DB thật. Khi một khuôn đã đánh dấu `hong`, màn chi tiết có nút **Tạo version thay thế**; bản mới giữ cùng `id_vat_tu`, tăng version theo max hiện có và mặc định `dang_dung`.

## ⚠️ Bước 6: Lưu ý khi code

### Nguyên tắc
- **Controller**: Chỉ xử lý request/response. KHÔNG viết nghiệp vụ trong Controller.
- **Service**: Toàn bộ business logic.
- **Repository**: Chỉ truy cập dữ liệu (Dapper + SQL). KHÔNG viết nghiệp vụ.
- **Entity vs ViewModel**: Tách biệt. Entity map với DB, ViewModel cho UI.

### Quy ước alias SQL
```sql
SELECT v.id Id, v.ten_vt TenVt, v.ma_vt MaVt
FROM kho_vat_tu v
```
Dùng alias `CộtDB TênThuộcTínhEntity` để Dapper map được.

### View
- Sử dụng `@using KhoQuanLy.Models.ViewModels`
- Form dùng AJAX với `submitForm()` trong `site.js`
- Partial View cho modal form: tên file `_*Form.cshtml`
- Modal mở bằng `openModal(title, url)`

### Validation
- Server: `ModelState.IsValid` + `[Required]` attribute trong ViewModel
- Client: Chưa có validation JS mạnh — cần bổ sung

### Toast / Thông báo
- `TempData["Success"]` và `TempData["Error"]` — hiển thị ở Layout
- JS: `toast(msg, type)` — type = 'ok' hoặc 'err'

## 🔄 Bước 7: Quy trình làm việc

1. Đọc `memory-bank/deviation-log.md` để biết các lệch/thiếu so với thiết kế
2. Kiểm tra task cần làm có trong "Việc còn dang dở" không
3. Code xong → chạy `dotnet build` → test trên trình duyệt
4. cập nhật các file `memory-bank/` liên quan mỗi khi qua 4-5 vòng lặp hoặc hoàn thành 1 nâng cấp nhỏ nào đấy.
5. Cập nhật `memory-bank/` nếu có thay đổi lớn về kiến trúc
6. Commit code (nếu có Git)

### Ưu tiên tiếp theo sau cập nhật memory-bank 23/06/2026

1. Test thêm nhánh import mới PDF PSP bằng file có `so_phieu` chưa tồn tại trong DB hoặc DB test sạch; các file hiện có trong workspace đang chủ yếu trùng số phiếu.
2. Rà soát thuật toán match dòng vật tư sau khi test thêm PDF thật, nhất là PDF có nhiều dòng vật tư.
3. Thiết kế spec/parser cho PDF Nhập kho tài chính nếu có file mẫu.
4. Làm báo cáo so sánh tuổi thọ khuôn bế theo NCC.

Ghi chú: UI nhập/sửa `la_cuon_thay_to_roi` cho dòng vật tư PSP đã hoàn thiện ngày 23/06/2026; tạo mới và modal sửa dòng đều có checkbox, build 0 lỗi/0 warning.
Ghi chú: Nghiệp vụ gộp dòng chưa lấy/thiếu từ đợt đề nghị trước sang đợt tiếp theo đã hoàn thiện ngày 23/06/2026, build 0 lỗi/0 warning.

## 🐛 Bước 8: Debug tips

- Lỗi connection: Kiểm tra MySQL service + connection string
- Lỗi Dapper mapping: Kiểm tra alias SQL có khớp tên property trong Entity không
- Nếu view không render được: Kiểm tra ViewBag, ViewData
- Nếu form không submit AJAX: Kiểm tra `form.action`, `getToken()`
- Lỗi 404: Kiểm tra route trong Controller

## 📞 Liên hệ

- Project code: `e:\THUYDD\VSCode\KhoQuanLy`
- Database: MySQL `payroll_db`
- User đã trao đổi: Nhân viên kho phân xưởng Offset