import type { Currency } from "../lib/currency";
import { localToday } from "../lib/date";

export interface Expense {
  id: string;
  amount: number;
  description: string;
  date: string;
  currency: string;
  createdAt: string;
}

export interface CreateExpenseInput {
  amount: number;
  description: string;
  date: string;
  currency: Currency;
}

export interface MonthlyTotal {
  /** `YYYY-MM` */
  month: string;
  currency: string;
  total: number;
  count: number;
}

export interface ExpenseFilters {
  from?: string;
  to?: string;
}

const ENDPOINT = "/api/expenses";

async function readError(response: Response): Promise<string> {
  const text = await response.text();
  return text || `${response.status} ${response.statusText}`;
}

export async function listExpenses(
  filters: ExpenseFilters = {},
  signal?: AbortSignal,
): Promise<Expense[]> {
  const params = new URLSearchParams();
  if (filters.from) {
    params.set("from", filters.from);
  }
  if (filters.to) {
    params.set("to", filters.to);
  }
  const query = params.toString();
  const url = query ? `${ENDPOINT}?${query}` : ENDPOINT;

  const response = await fetch(url, { signal });
  if (!response.ok) {
    throw new Error(await readError(response));
  }
  return (await response.json()) as Expense[];
}

export async function listRecentExpenses(signal?: AbortSignal): Promise<Expense[]> {
  const params = new URLSearchParams({ today: localToday() });
  const response = await fetch(`${ENDPOINT}/recent?${params}`, { signal });
  if (!response.ok) {
    throw new Error(await readError(response));
  }
  return (await response.json()) as Expense[];
}

export async function listMonthlyTotals(signal?: AbortSignal): Promise<MonthlyTotal[]> {
  const response = await fetch(`${ENDPOINT}/monthly-totals`, { signal });
  if (!response.ok) {
    throw new Error(await readError(response));
  }
  return (await response.json()) as MonthlyTotal[];
}

export async function createExpense(input: CreateExpenseInput): Promise<Expense> {
  const response = await fetch(ENDPOINT, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  if (!response.ok) {
    throw new Error(await readError(response));
  }
  return (await response.json()) as Expense;
}
