import { useQuery } from "@tanstack/react-query";
import { listMonthlyTotals, type MonthlyTotal } from "../api/expenses";
import { formatCurrency } from "../lib/currency";
import styles from "./MonthlyTotals.module.css";

const monthFormatter = new Intl.DateTimeFormat(undefined, { year: "numeric", month: "long" });

function formatMonth(value: string): string {
  const [year, month] = value.split("-").map(Number);
  if (!year || !month) {
    return value;
  }
  return monthFormatter.format(new Date(year, month - 1, 1));
}

interface MonthGroup {
  month: string;
  entries: MonthlyTotal[];
}

function groupByMonth(totals: MonthlyTotal[]): MonthGroup[] {
  const groupsByMonth = new Map<string, MonthGroup>();
  for (const total of totals) {
    let group = groupsByMonth.get(total.month);
    if (!group) {
      group = { month: total.month, entries: [] };
      groupsByMonth.set(total.month, group);
    }
    group.entries.push(total);
  }
  // Don't rely on the API's sort order: most-recent month first (YYYY-MM sorts
  // lexicographically = chronologically), currencies alphabetical within a month.
  const groups = [...groupsByMonth.values()].sort((a, b) => b.month.localeCompare(a.month));
  for (const group of groups) {
    group.entries.sort((a, b) => a.currency.localeCompare(b.currency));
  }
  return groups;
}

export function MonthlyTotals() {
  const query = useQuery({
    queryKey: ["expenses", "totals"],
    queryFn: ({ signal }) => listMonthlyTotals(signal),
  });

  let content;
  if (query.isPending) {
    content = <p className={styles.status}>Loading totals...</p>;
  } else if (query.isError) {
    const message = query.error instanceof Error ? query.error.message : "Unknown error";
    content = (
      <p className={`${styles.status} ${styles.error}`} role="alert">
        Could not load totals: {message}
      </p>
    );
  } else if (query.data.length === 0) {
    content = <p className={styles.status}>No totals yet.</p>;
  } else {
    content = (
      <ul className={styles.list}>
        {groupByMonth(query.data).map((group) => (
          <li key={group.month} className={styles.item}>
            <span className={styles.month}>{formatMonth(group.month)}</span>
            <span className={styles.amounts}>
              {group.entries.map((entry) => (
                <span key={entry.currency} className={styles.amount}>
                  {formatCurrency(entry.total, entry.currency)}
                </span>
              ))}
            </span>
          </li>
        ))}
      </ul>
    );
  }

  return (
    <section className={styles.panel} aria-label="Monthly totals">
      <h2 className={styles.title}>Monthly totals</h2>
      {content}
    </section>
  );
}
