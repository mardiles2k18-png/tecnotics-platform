import { stockState } from "@/lib/store";

const styles = {
  disponible: "bg-emerald-100 text-emerald-800",
  ultimas: "bg-amber-100 text-amber-800",
  agotado: "bg-red-100 text-red-800"
} as const;

const labels = {
  disponible: "Disponible",
  ultimas: "Últimas unidades",
  agotado: "Agotado"
} as const;

export function StockBadge({ stock }: { stock: number | null }) {
  const state = stockState(stock);
  if (!state) return null;
  return (
    <span className={`inline-flex items-center rounded px-2.5 py-1 text-xs font-bold ${styles[state]}`}>
      {labels[state]}
    </span>
  );
}
