import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}"
  ],
  theme: {
    extend: {
      colors: {
        graphite: "#111318",
        circuit: "#69C7D8",
        signal: "#33F3A6",
        ember: "#F7931A",
        panel: "#F5F7FB"
      },
      boxShadow: {
        glow: "0 0 40px rgba(0, 215, 255, 0.22)"
      }
    }
  },
  plugins: []
};

export default config;
