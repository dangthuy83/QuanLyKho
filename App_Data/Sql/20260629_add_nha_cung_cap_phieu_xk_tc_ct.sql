-- Thêm nhà cung cấp cho từng dòng nhập kho tài chính.
-- Chạy trên database payroll_db trước khi sử dụng cột Nhà cung cấp ở /NhapKhoTaiChinh/Create.

ALTER TABLE kho_phieu_xk_tc_ct
ADD COLUMN id_nha_cung_cap INT NULL AFTER id_phieu_sp;

ALTER TABLE kho_phieu_xk_tc_ct
ADD CONSTRAINT fk_kho_phieu_xk_tc_ct_ncc
FOREIGN KEY (id_nha_cung_cap) REFERENCES kho_nha_cung_cap(id);

CREATE INDEX idx_kho_phieu_xk_tc_ct_ncc
ON kho_phieu_xk_tc_ct(id_nha_cung_cap);