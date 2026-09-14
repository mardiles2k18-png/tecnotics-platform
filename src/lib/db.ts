import { Pool } from "pg";

let pool: Pool | null | undefined;

export function getDb(): Pool | null {
  if (pool !== undefined) return pool;

  const connectionString = process.env.DATABASE_URL;
  pool = connectionString ? new Pool({ connectionString }) : null;
  return pool;
}
