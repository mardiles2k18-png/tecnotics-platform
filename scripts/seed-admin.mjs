// Crea (o reinicia) el administrador del panel con una contraseña temporal.
// Uso: node --env-file=.env scripts/seed-admin.mjs correo@dominio.cl
import { randomBytes, scryptSync } from "node:crypto";
import pg from "pg";

const email = process.argv[2]?.trim().toLowerCase();
if (!email || !email.includes("@")) {
  console.error("Uso: node --env-file=.env scripts/seed-admin.mjs correo@dominio.cl");
  process.exit(1);
}
if (!process.env.DATABASE_URL) {
  console.error("Falta DATABASE_URL");
  process.exit(1);
}

const temporary = randomBytes(12).toString("base64url");
const salt = randomBytes(16);
const hash = `scrypt$${salt.toString("hex")}$${scryptSync(temporary, salt, 64).toString("hex")}`;

const client = new pg.Client({ connectionString: process.env.DATABASE_URL });
await client.connect();
await client.query(
  `INSERT INTO admin_users (email, password_hash, must_change_password)
   VALUES ($1, $2, true)
   ON CONFLICT (email) DO UPDATE SET password_hash = EXCLUDED.password_hash, must_change_password = true`,
  [email, hash]
);
await client.end();

console.log(`Administrador: ${email}`);
console.log(`Contraseña temporal: ${temporary}`);
console.log("Se pedirá cambiarla en el primer inicio de sesión.");
