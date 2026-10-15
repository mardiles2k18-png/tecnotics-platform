"use server";

import { redirect } from "next/navigation";
import {
  clearFailedLogins,
  clientKey,
  createSession,
  destroySession,
  hashPassword,
  isLockedOut,
  recordFailedLogin,
  requireAdmin,
  verifyPassword
} from "@/lib/admin-auth";
import { getDb } from "@/lib/db";
import { registerSale, saveProduct, voidSale } from "@/lib/store";

function go(path: string, kind: "ok" | "error", message: string): never {
  redirect(`${path}?${kind}=${encodeURIComponent(message)}`);
}

function text(form: FormData, name: string) {
  return String(form.get(name) ?? "").trim();
}

function whole(value: string) {
  const digits = value.replace(/[.\s$]/g, "");
  return /^\d+$/.test(digits) ? Number.parseInt(digits, 10) : null;
}

export async function loginAction(form: FormData) {
  const key = await clientKey();
  if (isLockedOut(key)) go("/admin/login", "error", "Demasiados intentos fallidos. Espera 15 minutos.");

  const email = text(form, "email").toLowerCase();
  const password = String(form.get("password") ?? "");
  const db = getDb();
  const { rows } = db
    ? await db.query<{ password_hash: string }>("SELECT password_hash FROM admin_users WHERE email = $1", [email])
    : { rows: [] };

  if (rows.length === 0 || !verifyPassword(password, rows[0].password_hash)) {
    recordFailedLogin(key);
    go("/admin/login", "error", "Correo o contraseña incorrectos");
  }

  clearFailedLogins(key);
  await createSession(email);
  redirect("/admin");
}

export async function logoutAction() {
  await destroySession();
  redirect("/admin/login");
}

export async function saveProductAction(form: FormData) {
  await requireAdmin();

  const slug = text(form, "slug") || undefined;
  const formPath = `/admin/productos/${slug ?? "nuevo"}`;
  const name = text(form, "name");
  const category = text(form, "category");
  const price = whole(text(form, "price"));
  const stock = whole(text(form, "stock"));

  if (name.length < 2) go(formPath, "error", "Escribe el nombre del producto");
  if (!category) go(formPath, "error", "Elige o escribe una categoría");
  if (price === null) go(formPath, "error", "El precio debe ser un número entero en pesos");
  if (stock === null) go(formPath, "error", "El stock debe ser un número entero (0 o más)");

  const result = await saveProduct({
    slug,
    name,
    category,
    price,
    stock,
    description: text(form, "description") || null,
    image: text(form, "image") || null,
    detailPath: text(form, "detail_path") || null,
    video: text(form, "video") || null,
    active: form.get("active") === "on"
  });

  if (!result.ok) go(formPath, "error", result.error);
  go("/admin", "ok", "Producto guardado");
}

export async function registerSaleAction(form: FormData) {
  const session = await requireAdmin();

  const slug = text(form, "slug");
  const quantity = whole(text(form, "quantity"));
  const unitPriceText = text(form, "unit_price");
  const unitPrice = unitPriceText ? whole(unitPriceText) : null;

  if (!slug) go("/admin/ventas", "error", "Elige un producto");
  if (quantity === null || quantity < 1) go("/admin/ventas", "error", "La cantidad debe ser 1 o más");
  if (unitPriceText && unitPrice === null) go("/admin/ventas", "error", "El precio debe ser un número entero en pesos");

  const result = await registerSale({
    slug,
    quantity,
    unitPrice,
    note: text(form, "note") || null,
    createdBy: session.email
  });

  if (!result.ok) go("/admin/ventas", "error", result.error);
  go("/admin/ventas", "ok", "Venta registrada y stock descontado");
}

export async function voidSaleAction(form: FormData) {
  await requireAdmin();

  const id = whole(text(form, "id"));
  if (id === null) go("/admin/ventas", "error", "Venta no válida");

  const result = await voidSale(id);
  if (!result.ok) go("/admin/ventas", "error", result.error);
  go("/admin/ventas", "ok", "Venta anulada y stock restituido");
}

export async function changePasswordAction(form: FormData) {
  const session = await requireAdmin({ allowPasswordChange: true });
  const path = "/admin/cambiar-clave";

  const current = String(form.get("current") ?? "");
  const next = String(form.get("next") ?? "");
  const confirm = String(form.get("confirm") ?? "");

  if (next.length < 10) go(path, "error", "La nueva contraseña debe tener al menos 10 caracteres");
  if (next !== confirm) go(path, "error", "Las contraseñas nuevas no coinciden");
  if (next === current) go(path, "error", "La nueva contraseña debe ser distinta de la actual");

  const db = getDb();
  if (!db) go(path, "error", "Base de datos no disponible");
  const { rows } = await db.query<{ password_hash: string }>(
    "SELECT password_hash FROM admin_users WHERE email = $1",
    [session.email]
  );
  if (rows.length === 0 || !verifyPassword(current, rows[0].password_hash)) {
    go(path, "error", "La contraseña actual no es correcta");
  }

  await db.query(
    "UPDATE admin_users SET password_hash = $2, must_change_password = false WHERE email = $1",
    [session.email, hashPassword(next)]
  );
  go("/admin", "ok", "Contraseña actualizada");
}
