# Database Schema — KhoQuanLy

## Tổng quan

- **DB Engine**: MySQL
- **Database**: `payroll_db`
- **Tables**: 19 tables, prefix `kho_`
- **Charset**: utf8mb4 (khuyến nghị)
- **Script tạo bảng**: `kho_schema.sql` (từ phiên thiết kế)

## Danh sách bảng

### 1. Danh mục (4 bảng)

#### `kho_nhom_vat_tu`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| ten_nhom | VARCHAR(100) NOT NULL | |
| mo_ta | TEXT nullable | |
| created_at | DATETIME DEFAULT NOW() | |

#### `kho_vat_tu`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| ma_vt | VARCHAR(50) NOT NULL | |
| ten_vt | VARCHAR(200) NOT NULL | |
| id_nhom | INT FK → kho_nhom_vat_tu.id | |
| don_vi_tinh | VARCHAR(20) | Kg, Tờ, Lít, etc. |
| loai_vat_tu | ENUM('giay_cuon','giay_to_roi','muc','khuon_be','khac') | |
| la_vat_tu_dich_danh | TINYINT(1) | Giấy, màng → cảnh báo thiếu theo PSP |
| can_theo_doi_tieu_hao | TINYINT(1) | Mực, chất phủ → báo cáo tiêu hao |
| ton_kho_hien_tai | DECIMAL(12,3) DEFAULT 0 | Tồn hiện tại |
| ton_kho_toi_thieu | DECIMAL(12,3) DEFAULT 0 | Cảnh báo tối thiểu |
| is_active | TINYINT(1) DEFAULT 1 | |
| created_at | DATETIME DEFAULT NOW() | |

#### `kho_nha_cung_cap`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| ten_ncc | VARCHAR(200) |
| dien_thoai | VARCHAR(20) nullable |
| dia_chi | TEXT nullable |
| ghi_chu | TEXT nullable |
| is_active | TINYINT(1) DEFAULT 1 |
| created_at | DATETIME DEFAULT NOW() |

#### `kho_bo_phan`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| ten_bo_phan | VARCHAR(100) |
| ghi_chu | TEXT nullable |
| is_active | TINYINT(1) DEFAULT 1 |
| created_at | DATETIME DEFAULT NOW() |

### 2. Phiếu Sản Phẩm (3 bảng)

#### `kho_phieu_sp`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| so_phieu | VARCHAR(50) | VD: 000136/2026/HNI-OFF-Mau |
| so_lsx | VARCHAR(50) nullable | Số lệnh sản xuất |
| ten_san_pham | VARCHAR(200) | |
| ma_san_pham | VARCHAR(50) nullable | |
| khach_hang | VARCHAR(200) nullable | |
| so_luong_sp | DECIMAL(12,3) nullable | Số lượng sản phẩm |
| ngay_lenh | DATE nullable | |
| ngay_giao_hang | DATE nullable | |
| trang_thai | VARCHAR(20) DEFAULT 'chua_hoan_thanh' | chua_hoan_thanh, hoan_thanh |
| file_pdf_path | VARCHAR(500) nullable | |
| nguoi_tao | VARCHAR(100) nullable | |
| created_at | DATETIME DEFAULT NOW() | |

**⚠️ Lưu ý**: Trạng thái hiện tại đã được chốt lại theo code thực tế: PSP mới/import PDF dùng `chua_hoan_thanh`, sau đó chuyển sang `hoan_thanh` khi hoàn tất. `cho_xuat` là trạng thái cũ, không dùng lại.

#### `kho_phieu_sp_vat_tu`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| id_phieu_sp | INT FK → kho_phieu_sp.id | |
| id_vat_tu | INT FK → kho_vat_tu.id nullable | |
| **id_vat_tu_thay_the** | INT FK → kho_vat_tu.id nullable | Giấy cuộn thay thế/vật tư thực cấp khi `la_cuon_thay_to_roi=1` |
| chung_loai_text | VARCHAR(200) nullable | Text tự do nếu không chọn VT |
| kich_thuoc | VARCHAR(100) nullable | VD: 96x69.5 |
| so_luong_yeu_cau | DECIMAL(12,3) | SL xuất kho (Kg) |
| so_luong_to_roi | DECIMAL(12,3) nullable | Số tờ xả ra |
| don_vi_tinh | VARCHAR(20) | |
| la_cuon_thay_to_roi | TINYINT(1) DEFAULT 0 | Ghi tờ rời nhưng cấp cuộn |
| so_luong_da_nhan | DECIMAL(12,3) DEFAULT 0 | |
| trang_thai_nhan | VARCHAR(20) DEFAULT 'chua_nhan' | chua_nhan, du, thieu, khong_can |
| ghi_chu_khong_can | TEXT nullable | |
| ghi_chu | TEXT nullable | |

#### `kho_phieu_sp_lich_su_sua`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| id_phieu_sp | INT FK → kho_phieu_sp.id |
| id_vat_tu | INT nullable |
| so_luong_cu | DECIMAL(12,3) nullable |
| so_luong_moi | DECIMAL(12,3) nullable |
| ly_do | TEXT nullable |
| nguoi_sua | VARCHAR(100) nullable |
| created_at | DATETIME DEFAULT NOW() |

### 3. Đợt Đề Nghị Xuất Giấy (2 bảng)

#### `kho_dot_de_nghi`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| ma_dot | VARCHAR(50) | Format: DDN-YYYYMMDD-XXX |
| ngay_tao | DATE |
| nguoi_tao | VARCHAR(100) nullable |
| trang_thai | VARCHAR(20) DEFAULT 'cho_lay' |
| ghi_chu | TEXT nullable |
| created_at | DATETIME DEFAULT NOW() |

#### `kho_dot_de_nghi_ct`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| id_dot | INT FK → kho_dot_de_nghi.id | |
| id_phieu_sp_vat_tu | INT FK → kho_phieu_sp_vat_tu.id | |
| so_luong_de_nghi | DECIMAL(12,3) nullable | |
| ton_ky_truoc | DECIMAL(12,3) DEFAULT 0 | |
| so_luong_thuc_nhan | DECIMAL(12,3) nullable | User nhập tay |
| trang_thai | VARCHAR(20) DEFAULT 'chua_lay' | chua_lay, du, thieu |
| ghi_chu | TEXT nullable | |

### 4. Phiếu Xuất Kho Tài Chính (2 bảng)

#### `kho_phieu_xk_tc`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| so_phieu | VARCHAR(50) |
| ngay_xuat | DATE |
| bo_phan_nhan | VARCHAR(200) nullable |
| ly_do_xuat | VARCHAR(500) nullable |
| file_pdf_path | VARCHAR(500) nullable |
| nguoi_upload | VARCHAR(100) nullable |
| created_at | DATETIME DEFAULT NOW() |

#### `kho_phieu_xk_tc_ct`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| id_phieu_xk_tc | INT FK | |
| id_vat_tu | INT FK nullable | |
| id_phieu_sp | INT FK nullable | Liên kết PSP nếu VT đích danh |
| id_nha_cung_cap | INT FK nullable | Nhà cung cấp của dòng nhập kho |
| mo_ta_vat_tu | TEXT nullable | |
| so_luong_chung_tu | DECIMAL(12,3) | SL trên chứng từ |
| so_luong_thuc_te | DECIMAL(12,3) | SL thực tế |
| don_vi_tinh | VARCHAR(20) nullable | |
| trang_thai | VARCHAR(20) DEFAULT 'du' | du, thieu, da_bo_qua |
| ly_do_bo_qua | TEXT nullable | |

**Cập nhật UI/nghiệp vụ 25/06/2026**: Trên màn hình tạo Nhập kho tài chính, `mo_ta_vat_tu` và `id_vat_tu` không còn hiển thị thành 2 cột riêng gây trùng lặp. UI dùng 1 cột **Tên vật tư**: khi chọn vật tư trong danh mục thì lưu `id_vat_tu`, tự điền `don_vi_tinh`, đồng thời lưu text hiển thị vào `mo_ta_vat_tu` để truy vết chứng từ. `id_phieu_sp` được chọn từ danh sách PSP để liên kết với module Phiếu sản phẩm khi cần.

**Cập nhật 29/06/2026**: Bổ sung `id_nha_cung_cap` cho từng dòng chi tiết nhập kho tài chính. UI `/NhapKhoTaiChinh/Create` chọn NCC bằng input + datalist cạnh cột Số PSP; Detail hiển thị lại tên NCC.

### 5. Trả Lại Kho (2 bảng)

#### `kho_phieu_tra_lai`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| so_phieu | VARCHAR(50) |
| ngay_tra | DATE |
| ly_do | VARCHAR(50) DEFAULT 'loi' |
| ghi_chu | TEXT nullable |
| nguoi_tao | VARCHAR(100) nullable |
| created_at | DATETIME DEFAULT NOW() |

#### `kho_phieu_tra_lai_ct`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| id_phieu_tra_lai | INT FK |
| id_vat_tu | INT FK |
| id_phieu_sp | INT FK nullable |
| so_luong | DECIMAL(12,3) |
| ghi_chu | TEXT nullable |

### 6. Giấy Tiết Kiệm (1 bảng)

#### `kho_giay_tiet_kiem`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| id_phieu_sp | INT FK | |
| id_vat_tu_cuon | INT FK | |
| kg_yeu_cau | DECIMAL(12,3) | |
| kg_thuc_xa | DECIMAL(12,3) | |
| kg_tiet_kiem | DECIMAL(12,3) | Computed? (kg_yeu_cau - kg_thuc_xa) |
| ten_thanh_pham_bu | VARCHAR(200) nullable | |
| so_bat | DECIMAL(12,3) nullable | |
| so_to_quy_doi | DECIMAL(12,3) nullable | |
| ghi_chu | TEXT nullable | |
| nguoi_tao | VARCHAR(100) nullable | |
| created_at | DATETIME DEFAULT NOW() | |

### 7. Khuôn Bế (2 bảng)

#### `kho_khuon_be`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| **id_vat_tu** | INT FK → kho_vat_tu.id nullable | Liên kết 1-1/nhóm version với danh mục vật tư loại `khuon_be`; các version thay thế cùng khuôn dùng chung `id_vat_tu` |
| ten_khuon | VARCHAR(200) | Tên khuôn nghiệp vụ; không tự tăng phiên bản chỉ vì trùng/cùng tên |
| ma_khuon | VARCHAR(50) | |
| phien_ban | INT DEFAULT 1 | Mặc định v1 cho khuôn/vật tư mới; chỉ tăng khi tạo khuôn thay thế cho khuôn hỏng/cùng khuôn thật sự theo thao tác có chủ đích |
| id_nha_cung_cap | INT FK nullable | |
| id_phieu_sp_dau | INT FK nullable | PSP đầu tiên dùng khuôn |
| ngay_bat_dau | DATE nullable | |
| trang_thai | VARCHAR(20) DEFAULT 'dang_dung' | dang_dung, hong |
| so_to_da_in | DECIMAL(12,3) DEFAULT 0 | |
| dinh_muc_tuoi_tho | DECIMAL(12,3) nullable | Số tờ tối đa |
| ngay_hong | DATE nullable | |
| ghi_chu | TEXT nullable | |
| created_at | DATETIME DEFAULT NOW() | |

#### `kho_khuon_be_lich_su`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| id_khuon_be | INT FK |
| id_phieu_sp | INT FK |
| so_to_in | DECIMAL(12,3) |
| ngay_su_dung | DATE nullable |
| ghi_chu | TEXT nullable |
| created_at | DATETIME DEFAULT NOW() |

### 8. Định Mức & Tiêu Hao (2 bảng)

**Cập nhật nghiệp vụ Khuôn Bế 25/06/2026**: Dữ liệu đồng bộ từ `kho_vat_tu` loại `khuon_be` sang `kho_khuon_be` luôn mặc định `phien_ban = 1`. Hệ thống không tự tăng version chỉ dựa trên `ten_khuon`, vì nhiều vật tư/khuôn mới có thể có tên tương tự nhưng không phải bản thay thế của cùng một khuôn hỏng. Đã bổ sung FK `id_vat_tu` trong `kho_khuon_be` để link chặt về danh mục vật tư. Chức năng tạo version thay thế chỉ dùng cho khuôn đã `hong`; bản thay thế giữ cùng `id_vat_tu`, tăng `phien_ban = max + 1` theo vật tư liên kết.

#### `kho_dinh_muc_tieu_hao`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| id_vat_tu | INT FK | |
| loai_dinh_muc | VARCHAR(20) DEFAULT 'theo_to_in' | theo_to_in, theo_ten_sp, ca_hai |
| ten_san_pham | VARCHAR(200) nullable | |
| dinh_muc_tren_1000_to | DECIMAL(12,3) nullable | |
| **dinh_muc_theo_ten_sp** | DECIMAL(12,3) nullable | ✅ Thêm mới |
| don_vi_tinh | VARCHAR(20) nullable | |
| ghi_chu | TEXT nullable | |
| is_active | TINYINT(1) DEFAULT 1 | |
| created_at | DATETIME DEFAULT NOW() | |

**⚠️ Cần ALTER TABLE**: Đã chạy thành công trên DB thật ✅
```sql
ALTER TABLE kho_dinh_muc_tieu_hao 
ADD COLUMN dinh_muc_theo_ten_sp DECIMAL(12,3) NULL 
AFTER dinh_muc_tren_1000_to;
```

#### `kho_tieu_hao_thuc_te`
| Column | Type |
|---|---|
| id | INT PK AUTO_INCREMENT |
| id_vat_tu | INT FK |
| id_phieu_xk_tc | INT FK nullable |
| thang_in_thuc_te | DATE |
| so_to_in | DECIMAL(12,3) nullable |
| so_luong_tieu_hao | DECIMAL(12,3) |
| ghi_chu | TEXT nullable |
| created_at | DATETIME DEFAULT NOW() |

### 9. Tồn Kho (1 bảng)

#### `kho_lich_su_ton`
| Column | Type | Notes |
|---|---|---|
| id | INT PK AUTO_INCREMENT | |
| id_vat_tu | INT FK | |
| ngay_giao_dich | DATETIME | |
| loai_gd | VARCHAR(20) | nhap_tc, xuat_psp, xuat_tieu_hao, xuat_khac, tra_lai, dieu_chinh |
| so_luong | DECIMAL(12,3) | |
| ton_truoc | DECIMAL(12,3) nullable | |
| ton_sau | DECIMAL(12,3) nullable | |
| id_phieu_xk_tc | INT FK nullable | |
| id_phieu_sp | INT FK nullable | |
| id_phieu_tra_lai | INT FK nullable | |
| ghi_chu | TEXT nullable | |
| created_at | DATETIME DEFAULT NOW() | |

## Mối quan hệ chính

```
kho_nhom_vat_tu 1──N kho_vat_tu
kho_nha_cung_cap 1──N kho_khuon_be
kho_phieu_sp 1──N kho_phieu_sp_vat_tu
kho_phieu_sp 1──N kho_phieu_sp_lich_su_sua
kho_phieu_sp 1──N kho_giay_tiet_kiem
kho_phieu_sp 1──N kho_khuon_be_lich_su
kho_dot_de_nghi 1──N kho_dot_de_nghi_ct
kho_phieu_sp_vat_tu 1──N kho_dot_de_nghi_ct
kho_phieu_xk_tc 1──N kho_phieu_xk_tc_ct
kho_phieu_tra_lai 1──N kho_phieu_tra_lai_ct
kho_khuon_be 1──N kho_khuon_be_lich_su
kho_vat_tu 1──N kho_lich_su_ton
```

## Ghi chú về loại giao dịch (loai_gd)

| Giá trị | Mô tả | Tác động tồn kho |
|---|---|---|
| nhap_tc | Nhập từ phiếu XK TC | + |
| xuat_psp | Xuất cho PSP | - |
| xuat_tieu_hao | Xuất tiêu hao thực tế | - |
| xuat_khac | Xuất khác (xuất nhanh) | - |
| tra_lai | Trả lại kho | - (từ phân xưởng) |
| dieu_chinh | Điều chỉnh thủ công | +/- |

## Công thức tính tồn

**Tồn giấy cuộn** = Σ nhập TC thực tế - Σ xuất PSP thực nhận - Σ trả lại

**Tồn kỳ trước (đề nghị xuất)** = Σ nhập TC (thực tế) - Σ xuất PSP (thực nhận) - Σ trả lại (cùng loại giấy)

## Các thay đổi database so với thiết kế ban đầu

| Thay đổi | Chi tiết | Ngày |
|---|---|---|
| ✅ Thêm cột `dinh_muc_theo_ten_sp` | Bảng `kho_dinh_muc_tieu_hao` — hỗ trợ định mức theo tên sản phẩm | 19/06/2026 |
| ✅ Đổi mặc định `trang_thai` PSP | Bảng/code `kho_phieu_sp` — từ trạng thái cũ `cho_xuat` sang quy ước hiện tại: `chua_hoan_thanh` cho PSP mới/import PDF, `hoan_thanh` khi hoàn tất | 19/06/2026, đồng bộ lại 23/06/2026 |
| ✅ Thêm `id_vat_tu_thay_the` | Bảng `kho_phieu_sp_vat_tu` — lưu giấy cuộn thay thế/vật tư thực cấp cho nghiệp vụ “Cấp cuộn thay tờ rời”; tồn kho/báo cáo dùng `id_vat_tu_thay_the ?? id_vat_tu` | 25/06/2026 |
| ✅ Thêm `id_vat_tu` cho Khuôn Bế | Bảng `kho_khuon_be` — liên kết khuôn nghiệp vụ với danh mục vật tư loại `khuon_be`, hỗ trợ tạo version thay thế theo cùng vật tư | 25/06/2026 |
| ✅ Thêm `id_nha_cung_cap` cho chi tiết Nhập kho tài chính | Bảng `kho_phieu_xk_tc_ct` — lưu NCC theo từng dòng nhập kho, chọn bằng datalist trên `/NhapKhoTaiChinh/Create` | 29/06/2026 |

**⚠️ Cần ALTER TABLE trên DB thật trước khi chạy code mới**:
```sql
ALTER TABLE kho_phieu_sp_vat_tu
ADD COLUMN id_vat_tu_thay_the INT NULL AFTER id_vat_tu;

ALTER TABLE kho_phieu_sp_vat_tu
ADD CONSTRAINT fk_kho_phieu_sp_vat_tu_thay_the
FOREIGN KEY (id_vat_tu_thay_the) REFERENCES kho_vat_tu(id);

CREATE INDEX idx_kho_phieu_sp_vat_tu_thay_the
ON kho_phieu_sp_vat_tu(id_vat_tu_thay_the);
```

Script đã được lưu tại `App_Data/Sql/20260625_add_vat_tu_thay_the_psp.sql`.

**⚠️ Cần ALTER TABLE Khuôn Bế trên DB thật trước khi chạy code mới**:
```sql
ALTER TABLE kho_khuon_be
ADD COLUMN id_vat_tu INT NULL AFTER id;

UPDATE kho_khuon_be k
LEFT JOIN kho_vat_tu v_ma
    ON v_ma.loai_vat_tu = 'khuon_be'
   AND LOWER(TRIM(v_ma.ma_vt)) = LOWER(TRIM(k.ma_khuon))
LEFT JOIN kho_vat_tu v_ten
    ON v_ten.loai_vat_tu = 'khuon_be'
   AND LOWER(TRIM(v_ten.ten_vt)) = LOWER(TRIM(k.ten_khuon))
SET k.id_vat_tu = COALESCE(v_ma.id, v_ten.id)
WHERE k.id_vat_tu IS NULL;

CREATE INDEX idx_kho_khuon_be_id_vat_tu
ON kho_khuon_be(id_vat_tu);

ALTER TABLE kho_khuon_be
ADD CONSTRAINT fk_kho_khuon_be_vat_tu
FOREIGN KEY (id_vat_tu) REFERENCES kho_vat_tu(id);
```

Script đã được lưu tại `App_Data/Sql/20260625_add_id_vat_tu_khuon_be.sql`.

**⚠️ Cần ALTER TABLE Nhập kho tài chính trên DB thật trước khi dùng cột NCC mới**:
```sql
ALTER TABLE kho_phieu_xk_tc_ct
ADD COLUMN id_nha_cung_cap INT NULL AFTER id_phieu_sp;

ALTER TABLE kho_phieu_xk_tc_ct
ADD CONSTRAINT fk_kho_phieu_xk_tc_ct_ncc
FOREIGN KEY (id_nha_cung_cap) REFERENCES kho_nha_cung_cap(id);

CREATE INDEX idx_kho_phieu_xk_tc_ct_ncc
ON kho_phieu_xk_tc_ct(id_nha_cung_cap);
```

Script đã được lưu tại `App_Data/Sql/20260629_add_nha_cung_cap_phieu_xk_tc_ct.sql`.