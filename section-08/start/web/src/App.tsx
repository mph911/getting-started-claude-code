import { ExpenseForm } from "./components/ExpenseForm";
import { MonthlyTotals } from "./components/MonthlyTotals";
import { ExpenseTable } from "./components/ExpenseTable";
import styles from "./App.module.css";

export function App() {
  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <h1>Expense Tracker</h1>
        <p>Track what you spend, one entry at a time.</p>
      </header>
      <section className={styles.section}>
        <ExpenseForm />
      </section>
      <section className={styles.section}>
        <MonthlyTotals />
      </section>
      <section className={styles.section}>
        <ExpenseTable />
      </section>
    </main>
  );
}
