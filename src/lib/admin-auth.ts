import { createHmac, randomBytes, scryptSync, timingSafeEqual } from "node:crypto";
import { cookies, headers } from "next/headers";
import { redirect } from "next/navigation";
import { getDb } from "@/lib/db";

const COOKIE = "tn_admin";
const SESSION_SECONDS = 60 * 60 * 8;
const MAX_FAILED_LOGINS = 5;
const LOCKOUT_MS = 15 * 60 * 1000;

const failedLogins = new Map<string, { count: number; first: number }>();

function sessionSecret() {
  const secret = process.env.ADMIN_SESSION_SECRET;
  return secret && secret.length >= 32 ? secret : null;
}

function sign(payload: string, secret: string) {
  return createHmac("sha256", secret).update(payload).digest("hex");
}

export function hashPassword(password: string) {
  const salt = randomBytes(16);
  const key = scryptSync(password, salt, 64);
  return `scrypt$${salt.toString("hex")}$${key.toString("hex")}`;
}

export function verifyPassword(password: string, stored: string) {
  const [scheme, saltHex, keyHex] = stored.split("$");
  if (scheme !== "scrypt" || !saltHex || !keyHex) return false;
  const expected = Buffer.from(keyHex, "hex");
  const actual = scryptSync(password, Buffer.from(saltHex, "hex"), expected.length);
  return timingSafeEqual(actual, expected);
}

export async function clientKey() {
  const h = await headers();
  return h.get("cf-connecting-ip") ?? h.get("x-forwarded-for")?.split(",")[0]?.trim() ?? "local";
}

export function isLockedOut(key: string) {
  const entry = failedLogins.get(key);
  if (!entry) return false;
  if (Date.now() - entry.first > LOCKOUT_MS) {
    failedLogins.delete(key);
    return false;
  }
  return entry.count >= MAX_FAILED_LOGINS;
}

export function recordFailedLogin(key: string) {
  const entry = failedLogins.get(key);
  if (!entry || Date.now() - entry.first > LOCKOUT_MS) {
    failedLogins.set(key, { count: 1, first: Date.now() });
  } else {
    entry.count += 1;
  }
}

export function clearFailedLogins(key: string) {
  failedLogins.delete(key);
}

export async function createSession(email: string) {
  const secret = sessionSecret();
  if (!secret) throw new Error("ADMIN_SESSION_SECRET no esta configurado");
  const expires = Math.floor(Date.now() / 1000) + SESSION_SECONDS;
  const payload = `${email}|${expires}`;
  const token = `${Buffer.from(payload).toString("base64url")}.${sign(payload, secret)}`;
  (await cookies()).set(COOKIE, token, {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax",
    path: "/admin",
    maxAge: SESSION_SECONDS
  });
}

export async function destroySession() {
  (await cookies()).set(COOKIE, "", { path: "/admin", maxAge: 0 });
}

export async function getSession(): Promise<{ email: string } | null> {
  const secret = sessionSecret();
  if (!secret) return null;
  const raw = (await cookies()).get(COOKIE)?.value;
  if (!raw) return null;
  const [encoded, signature] = raw.split(".");
  if (!encoded || !signature) return null;
  const payload = Buffer.from(encoded, "base64url").toString();
  const expected = sign(payload, secret);
  if (signature.length !== expected.length) return null;
  if (!timingSafeEqual(Buffer.from(signature), Buffer.from(expected))) return null;
  const [email, expires] = payload.split("|");
  if (!email || Number(expires) < Date.now() / 1000) return null;
  return { email };
}

export async function requireAdmin(options: { allowPasswordChange?: boolean } = {}) {
  const session = await getSession();
  if (!session) redirect("/admin/login");

  const db = getDb();
  const { rows } = db
    ? await db.query<{ must_change_password: boolean }>(
        "SELECT must_change_password FROM admin_users WHERE email = $1",
        [session.email]
      )
    : { rows: [] };
  if (rows.length === 0) redirect("/admin/login");
  if (rows[0].must_change_password && !options.allowPasswordChange) redirect("/admin/cambiar-clave");

  return session;
}
