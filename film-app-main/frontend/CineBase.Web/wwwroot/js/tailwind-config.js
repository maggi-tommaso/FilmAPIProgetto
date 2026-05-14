tailwind.config = {
  darkMode: "class",
  theme: {
    extend: {
      fontFamily: {
        sans: ["'Inter'", "system-ui", "-apple-system", "sans-serif"],
      },
      colors: {
        brand: {
          red: "var(--brand-red)",
          "red-dark": "var(--brand-red-dark)",
          "red-light": "var(--brand-red-light)",
          gold: "var(--brand-gold)",
          "gold-dark": "var(--brand-gold-dark)",
          "gold-light": "var(--brand-gold-light)",
          cyan: "var(--brand-cyan)",
          "cyan-light": "var(--brand-cyan-light)",
          emerald: "var(--brand-emerald)",
          purple: "var(--brand-purple)",
          surface: "var(--brand-surface)",
          "surface-dim": "var(--brand-surface-dim)",
          "surface-container": "var(--brand-surface-container)",
          "surface-container-high": "var(--brand-surface-container-high)",
          "surface-container-highest": "var(--brand-surface-container-highest)",
          "surface-container-low": "var(--brand-surface-container-low)",
          "surface-container-lowest": "var(--brand-surface-container-lowest)",
          "on-surface": "var(--brand-on-surface)",
          "on-surface-variant": "var(--brand-on-surface-variant)",
          outline: "var(--brand-outline)",
          "outline-variant": "var(--brand-outline-variant)",
          error: "var(--brand-error)",
          "error-container": "var(--brand-error-container)",
          "sidebar-text": "var(--brand-sidebar-text)",
        },
      },
    },
  },
};
