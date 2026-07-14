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