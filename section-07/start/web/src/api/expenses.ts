import type { Currency } from "../lib/currency";

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
  const response = await fetch(`${ENDPOINT}/recent`, { signal });
  if (!response.ok) {
    throw new Error(await readError(response));
  }
  return (await response.json()) as Expense[];
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
