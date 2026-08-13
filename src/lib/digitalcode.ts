import * as cheerio from "cheerio";

const SOURCE_BASE_URL = "https://digitalcodespa.cl";
const INSTALL_MARKUP_CLP = 12000;
const REQUEST_HEADERS = {
  "User-Agent": "TecnoticsPriceSync/1.0 (+https://tecnotics.cl)"
};

// DigitalCode miscategorizes some products under the wrong section on their own site.
const CATEGORY_OVERRIDES: Record<string, "windows" | "office"> = {
  "office-2024/windows-11-enterprise-ltsc-2024": "windows"
};

export type SyncedProduct = {
  slug: string;
  category: "windows" | "office";
  subcategory: string | null;
  name: string;
  description: string | null;
  sourcePrice: number;
  ourPrice: number;
};

function parsePriceClp(text: string): number {
  const digits = text.replace(/[^\d]/g, "");
  return digits ? Number.parseInt(digits, 10) : 0;
}

function parseCards(html: string, category: "windows" | "office", dataCatPrefix: string): SyncedProduct[] {
  const $ = cheerio.load(html);
  const products: SyncedProduct[] = [];

  $(".product-card").each((_, el) => {
    const card = $(el);
    const dataCat = card.attr("data-cat") ?? "";
    if (!dataCat.startsWith(dataCatPrefix)) return;

    const cartButton = card.find(".btn--cart");
    const inStock = cartButton.length > 0 && cartButton.attr("disabled") === undefined;
    if (!inStock) return;

    const nameLink = card.find(".product-card__name a").first();
    const href = nameLink.attr("href");
    const name = nameLink.text().trim();
    if (!href || !name) return;

    const priceEl = card.find(".product-card__price").first();
    const promoNew = priceEl.find(".precio-promo__new");
    const priceText = promoNew.length > 0 ? promoNew.text() : priceEl.text();
    const sourcePrice = parsePriceClp(priceText);
    if (!sourcePrice) return;

    products.push({
      slug: href.replace(/^\//, ""),
      category,
      subcategory: card.find(".product-card__cat").first().text().trim() || null,
      name,
      description: card.find(".product-card__desc").first().text().trim() || null,
      sourcePrice,
      ourPrice: sourcePrice + INSTALL_MARKUP_CLP
    });
  });

  return products;
}

async function fetchHtml(path: string): Promise<string> {
  const res = await fetch(`${SOURCE_BASE_URL}${path}`, { headers: REQUEST_HEADERS });
  if (!res.ok) throw new Error(`Fetch failed for ${path}: ${res.status}`);
  return res.text();
}

function extractTotalPages(html: string): number {
  const $ = cheerio.load(html);
  let max = 1;
  $('a[href*="page="]').each((_, el) => {
    const match = ($(el).attr("href") ?? "").match(/page=(\d+)/);
    if (match) max = Math.max(max, Number.parseInt(match[1], 10));
  });
  return Math.min(max, 20);
}

async function syncWindows(): Promise<SyncedProduct[]> {
  const html = await fetchHtml("/windows");
  return parseCards(html, "windows", "windows");
}

async function syncOffice(): Promise<SyncedProduct[]> {
  const bySlug = new Map<string, SyncedProduct>();

  const firstPageHtml = await fetchHtml("/office?page=1");
  for (const product of parseCards(firstPageHtml, "office", "office")) bySlug.set(product.slug, product);

  const totalPages = extractTotalPages(firstPageHtml);
  for (let page = 2; page <= totalPages; page += 1) {
    const html = await fetchHtml(`/office?page=${page}`);
    for (const product of parseCards(html, "office", "office")) bySlug.set(product.slug, product);
  }

  return Array.from(bySlug.values());
}

export async function syncCatalog(): Promise<SyncedProduct[]> {
  const [windows, office] = await Promise.all([syncWindows(), syncOffice()]);
  const bySlug = new Map<string, SyncedProduct>();
  for (const product of [...windows, ...office]) bySlug.set(product.slug, product);

  return Array.from(bySlug.values()).map((product) => {
    const override = CATEGORY_OVERRIDES[product.slug];
    return override ? { ...product, category: override } : product;
  });
}
