import { useQuery } from "@tanstack/react-query";
import { listExpenses, type Expense } from "../api/expenses";
import { formatCurrency } from "../lib/currency";
import styles from "./ExpenseTable.module.css";

const dateFormatter = new Intl.DateTimeFormat(undefined, { dateStyle: "medium" });

function formatDate(value: string): string {
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) {
    return value;
  }
  return dateFormatter.format(new Date(year, month - 1, day));
}

function byDateDescending(a: Expense, b: Expense): number {
  if (a.date === b.date) {
    return b.createdAt.localeCompare(a.createdAt);
  }
  return b.date.localeCompare(a.date);
}

export function ExpenseTable() {
  const query = useQuery({
    queryKey: ["expenses"],
    queryFn: ({ signal }) => listExpenses(signal),
  });

  if (query.isPending) {
    return <p className={styles.status}>Loading expenses...</p>;
  }

  if (query.isError) {
    const message = query.error instanceof Error ? query.error.message : "Unknown error";
    return <p className={`${styles.status} ${styles.error}`} role="alert">Could not load expenses: {message}</p>;
  }

  const expenses = [...query.data].sort(byDateDescending);

  if (expenses.length === 0) {
    return <p className={styles.status}>No expenses yet. Add your first one above.</p>;
  }

  return (
    <table className={styles.table}>
      <thead>
        <tr>
          <th scope="col">Date</th>
          <th scope="col">Description</th>
          <th scope="col" className={styles.amount}>Amount</th>
        </tr>
      </thead>
      <tbody>
        {expenses.map((expense) => (
          <tr key={expense.id}>
            <td>{formatDate(expense.date)}</td>
            <td>{expense.description}</td>
            <td className={styles.amount}>{formatCurrency(expense.amount, expense.currency)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
