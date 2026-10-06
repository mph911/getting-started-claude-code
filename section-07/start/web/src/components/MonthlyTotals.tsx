import { useQuery } from "@tanstack/react-query";
import { listMonthlyTotals, type MonthlyTotal } from "../api/expenses";
import { formatCurrency } from "../lib/currency";
import styles from "./MonthlyTotals.module.css";

const monthFormatter = new Intl.DateTimeFormat(undefined, { month: "long", year: "numeric" });

function formatMonth(value: string): string {
  const [year, month] = value.split("-").map(Number);
  if (!year || !month) {
    return value;
  }
  return monthFormatter.format(new Date(year, month - 1, 1));
}

// The API returns one row per month and currency, newest month first; keep that order.
function groupByMonth(totals: MonthlyTotal[]): [string, MonthlyTotal[]][] {
  const groups = new Map<string, MonthlyTotal[]>();
  for (const total of totals) {
    const group = groups.get(total.month);
    if (group) {
      group.push(total);
    } else {
      groups.set(total.month, [total]);
    }
  }
  return [...groups];
}

export function MonthlyTotals() {
  const query = useQuery({
    queryKey: ["expenses", "monthly-totals"],
    queryFn: ({ signal }) => listMonthlyTotals(signal),
  });

  let content;
  if (query.isPending) {
    content = <p className={styles.status}>Loading totals...</p>;
  } else if (query.isError) {
    const message = query.error instanceof Error ? query.error.message : "Unknown error";
    content = (
      <p className={`${styles.status} ${styles.error}`} role="alert">
        Could not load monthly totals: {message}
      </p>
    );
  } else if (query.data.length === 0) {
    content = <p className={styles.status}>No totals yet.</p>;
  } else {
    content = (
      <ul className={styles.list}>
        {groupByMonth(query.data).map(([month, totals]) => (
          <li key={month} className={styles.row}>
            <span className={styles.month}>{formatMonth(month)}</span>
            <span className={styles.amounts}>
              {/* One amount per currency: amounts in different currencies are never summed. */}
              {totals.map((total) => (
                <span key={total.currency} className={styles.amount}>
                  {formatCurrency(total.total, total.currency)}
                </span>
              ))}
            </span>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <div className={styles.panel}>
      <h2 className={styles.title}>Monthly totals</h2>
      {content}
    </div>
  );
}
