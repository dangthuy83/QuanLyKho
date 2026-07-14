-- Bổ sung cột giấy cuộn thay thế / vật tư thực cấp cho dòng vật tư Phiếu sản phẩm.
-- Chạy trên database payroll_db trước khi sử dụng chức năng "Cấp cuộn thay tờ rời".

ALTER TABLE kho_phieu_sp_vat_tu
ADD COLUMN id_vat_tu_thay_the INT NULL AFTER id_vat_tu;

ALTER TABLE kho_phieu_sp_vat_tu
ADD CONSTRAINT fk_kho_phieu_sp_vat_tu_thay_the
FOREIGN KEY (id_vat_tu_thay_the) REFERENCES kho_vat_tu(id);

CREATE INDEX idx_kho_phieu_sp_vat_tu_thay_the
ON kho_phieu_sp_vat_tu(id_vat_tu_thay_the);