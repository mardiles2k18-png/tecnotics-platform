import { createClient, type RedisClientType } from "redis";
import { NextResponse } from "next/server";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

let client: RedisClientType | null = null;

async function getClient() {
  if (!process.env.REDIS_URL) return null;

  if (!client) {
    client = createClient({ url: process.env.REDIS_URL });
    client.on("error", () => {});
  }
  if (!client.isOpen) {
    await client.connect();
  }
  return client;
}

export async function GET() {
  const redis = await getClient();
  if (!redis) {
    return NextResponse.json({ count: null, configured: false });
  }

  const count = await redis.incr("site:visits");
  return NextResponse.json({ count, configured: true });
}
