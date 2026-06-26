export const CURRENCY_LOCALE = "de-DE";
export const CURRENCY_CODE = "EUR";

const formatter = new Intl.NumberFormat(CURRENCY_LOCALE, {
  style: "currency",
  currency: CURRENCY_CODE,
});

export function formatCurrency(amount: number): string {
  return formatter.format(amount);
}
