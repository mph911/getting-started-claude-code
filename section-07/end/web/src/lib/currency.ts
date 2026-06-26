export const CURRENCY_LOCALE = "pt-PT";

export const CURRENCIES = ["EUR", "USD", "GBP"] as const;
export type Currency = (typeof CURRENCIES)[number];
export const DEFAULT_CURRENCY: Currency = "EUR";

const formatters = new Map<string, Intl.NumberFormat>();

export function formatCurrency(amount: number, currency: string = DEFAULT_CURRENCY): string {
  let formatter = formatters.get(currency);
  if (!formatter) {
    formatter = new Intl.NumberFormat(CURRENCY_LOCALE, {
      style: "currency",
      currency,
    });
    formatters.set(currency, formatter);
  }
  return formatter.format(amount);
}
