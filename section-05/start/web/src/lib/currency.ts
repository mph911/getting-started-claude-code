export const CURRENCY_LOCALE = "de-DE";

// Mirrors Domain/Currencies.cs on the API.
export const CURRENCIES = ["EUR", "USD", "GBP"] as const;
export type Currency = (typeof CURRENCIES)[number];
export const DEFAULT_CURRENCY: Currency = "EUR";

const formatters = new Map<string, Intl.NumberFormat>();

function formatterFor(currency: string): Intl.NumberFormat {
  let formatter = formatters.get(currency);
  if (!formatter) {
    formatter = new Intl.NumberFormat(CURRENCY_LOCALE, { style: "currency", currency });
    formatters.set(currency, formatter);
  }
  return formatter;
}

export function formatCurrency(amount: number, currency: string): string {
  return formatterFor(currency).format(amount);
}
