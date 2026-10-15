-- Esquema de la tienda. Idempotente: se puede aplicar varias veces.
-- Aplicar: docker exec -i tecnotics-postgres psql -U tecnotics -d tecnotics < db/schema.sql

ALTER TABLE products ADD COLUMN IF NOT EXISTS kind text NOT NULL DEFAULT 'digital';
ALTER TABLE products ADD COLUMN IF NOT EXISTS stock integer;
ALTER TABLE products ADD COLUMN IF NOT EXISTS active boolean NOT NULL DEFAULT true;
ALTER TABLE products ADD COLUMN IF NOT EXISTS image text;
ALTER TABLE products ADD COLUMN IF NOT EXISTS detail_path text;
ALTER TABLE products ALTER COLUMN source_price DROP NOT NULL;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'products_kind_stock_check') THEN
    ALTER TABLE products ADD CONSTRAINT products_kind_stock_check CHECK (
      (kind = 'digital' AND stock IS NULL)
      OR (kind = 'fisico' AND stock IS NOT NULL AND stock >= 0)
    );
  END IF;
END $$;

CREATE TABLE IF NOT EXISTS sales (
  id bigserial PRIMARY KEY,
  product_slug text NOT NULL REFERENCES products(slug),
  quantity integer NOT NULL CHECK (quantity > 0),
  unit_price integer NOT NULL CHECK (unit_price >= 0),
  total integer GENERATED ALWAYS AS (quantity * unit_price) STORED,
  note text,
  created_by text,
  created_at timestamptz NOT NULL DEFAULT now(),
  voided_at timestamptz
);

CREATE INDEX IF NOT EXISTS sales_created_at_idx ON sales (created_at DESC);

CREATE TABLE IF NOT EXISTS admin_users (
  email text PRIMARY KEY,
  password_hash text NOT NULL,
  must_change_password boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now()
);
