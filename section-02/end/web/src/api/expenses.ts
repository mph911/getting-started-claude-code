export interface Expense {
  id: string;
  amount: number;
  description: string;
  date: string;
  createdAt: string;
}

export interface CreateExpenseInput {
  amount: number;
  description: string;
  date: string;
}

const ENDPOINT = "/api/expenses";

async function readError(response: Response): Promise<string> {
  const text = await response.text();
  return text || `${response.status} ${response.statusText}`;
}

export async function listExpenses(signal?: AbortSignal): Promise<Expense[]> {
  const response = await fetch(ENDPOINT, { signal });
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
