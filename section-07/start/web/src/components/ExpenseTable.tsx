import { useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { listExpenses, listRecentExpenses, type Expense } from "../api/expenses";
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
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [recent, setRecent] = useState(false);
  const fromRef = useRef<HTMLInputElement>(null);

  const invalidRange = from !== "" && to !== "" && from > to;
  const hasFilter = recent || from !== "" || to !== "";

  const openPicker = (input: HTMLInputElement | null) => {
    if (!input) {
      return;
    }
    try {
      input.showPicker();
    } catch {
      input.focus();
    }
  };

  const setRange = (next: { from?: string; to?: string }) => {
    setRecent(false);
    if (next.from !== undefined) {
      setFrom(next.from);
    }
    if (next.to !== undefined) {
      setTo(next.to);
    }
  };

  const toggleRecent = () => {
    setFrom("");
    setTo("");
    setRecent((value) => !value);
  };

  const query = useQuery({
    queryKey: recent ? ["expenses", "recent"] : ["expenses", { from, to }],
    queryFn: ({ signal }) =>
      recent
        ? listRecentExpenses(signal)
        : listExpenses({ from: from || undefined, to: to || undefined }, signal),
    enabled: !invalidRange,
  });

  let content;
  if (invalidRange) {
    content = (
      <p className={`${styles.status} ${styles.error}`} role="alert">
        “From” must be on or before “To”.
      </p>
    );
  } else if (query.isPending) {
    content = <p className={styles.status}>Loading expenses...</p>;
  } else if (query.isError) {
    const message = query.error instanceof Error ? query.error.message : "Unknown error";
    content = (
      <p className={`${styles.status} ${styles.error}`} role="alert">
        Could not load expenses: {message}
      </p>
    );
  } else if (query.data.length === 0) {
    content = (
      <p className={styles.status}>
        {recent
          ? "No expenses in the last 7 days."
          : hasFilter
            ? "No expenses match the selected date range."
            : "No expenses yet. Add your first one above."}
      </p>
    );
  } else {
    const expenses = [...query.data].sort(byDateDescending);
    content = (
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

  return (
    <div className={styles.container}>
      <div className={styles.filter}>
        <span className={styles.label}>Date range</span>
        <div className={styles.controls}>
          <button
            type="button"
            className={`${styles.toggle} ${recent ? styles.toggleActive : ""}`}
            onClick={toggleRecent}
            aria-pressed={recent}
          >
            Last 7 days
          </button>
          <div className={styles.rangeField}>
            <input
              ref={fromRef}
              type="date"
              aria-label="From date"
              value={from}
              max={to || undefined}
              disabled={recent}
              onClick={(event) => openPicker(event.currentTarget)}
              onChange={(event) => setRange({ from: event.target.value })}
            />
            <span className={styles.separator} aria-hidden="true">–</span>
            <input
              type="date"
              aria-label="To date"
              value={to}
              min={from || undefined}
              disabled={recent}
              onClick={(event) => openPicker(event.currentTarget)}
              onChange={(event) => setRange({ to: event.target.value })}
            />
            <button
              type="button"
              className={styles.calendarButton}
              aria-label="Open date picker"
              disabled={recent}
              onClick={() => openPicker(fromRef.current)}
            >
              <svg
                viewBox="0 0 24 24"
                width="18"
                height="18"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
                aria-hidden="true"
              >
                <rect x="3" y="4" width="18" height="18" rx="2" />
                <path d="M16 2v4M8 2v4M3 10h18" />
              </svg>
            </button>
          </div>
          <button
            type="button"
            className={styles.clear}
            onClick={() => {
              setFrom("");
              setTo("");
              setRecent(false);
            }}
            disabled={!hasFilter}
          >
            Clear
          </button>
        </div>
      </div>
      {content}
    </div>
  );
}
