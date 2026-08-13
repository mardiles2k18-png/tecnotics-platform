"use client";

import { useEffect, useState } from "react";

export function VisitCounter() {
  const [count, setCount] = useState<number | null>(null);

  useEffect(() => {
    fetch("/api/visits")
      .then((res) => res.json())
      .then((data: { count: number | null }) => setCount(data.count))
      .catch(() => setCount(null));
  }, []);

  return (
    <p className="text-xs text-white/50">
      {count !== null ? `Visitas: ${count.toLocaleString("es-CL")}` : "Visitas: --"}
    </p>
  );
}
