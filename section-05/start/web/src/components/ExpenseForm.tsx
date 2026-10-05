import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createExpense, type CreateExpenseInput } from "../api/expenses";
import { CURRENCIES, DEFAULT_CURRENCY, type Currency } from "../lib/currency";
import styles from "./ExpenseForm.module.css";

const today = () => new Date().toISOString().slice(0, 10);

export function ExpenseForm() {
  const queryClient = useQueryClient();
  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState<Currency>(DEFAULT_CURRENCY);
  const [description, setDescription] = useState("");
  const [date, setDate] = useState(today);
  const [clientError, setClientError] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: (input: CreateExpenseInput) => createExpense(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["expenses"] });
      setAmount("");
      setDescription("");
      setDate(today());
      setClientError(null);
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const parsedAmount = Number(amount);
    if (!Number.isFinite(parsedAmount) || parsedAmount <= 0) {
      setClientError("Amount must be greater than zero.");
      return;
    }
    const trimmed = description.trim();
    if (trimmed.length === 0) {
      setClientError("Description is required.");
      return;
    }
    if (trimmed.length > 200) {
      setClientError("Description must be 200 characters or fewer.");
      return;
    }
    if (!date) {
      setClientError("Date is required.");
      return;
    }

    setClientError(null);
    mutation.mutate({ amount: parsedAmount, currency, description: trimmed, date });
  }

  const errorMessage = clientError ?? (mutation.error instanceof Error ? mutation.error.message : null);

  return (
    <form className={styles.form} onSubmit={handleSubmit} noValidate>
      <h2 className={styles.title}>Add expense</h2>
      <div className={styles.fields}>
        <label className={styles.field}>
          <span>Amount</span>
          <input
            type="number"
            inputMode="decimal"
            step="0.01"
            min="0.01"
            value={amount}
            onChange={(event) => setAmount(event.target.value)}
            required
          />
        </label>
        <label className={styles.field}>
          <span>Currency</span>
          <select value={currency} onChange={(event) => setCurrency(event.target.value as Currency)}>
            {CURRENCIES.map((code) => (
              <option key={code} value={code}>{code}</option>
            ))}
          </select>
        </label>
        <label className={styles.field}>
          <span>Description</span>
          <input
            type="text"
            maxLength={200}
            value={description}
            onChange={(event) => setDescription(event.target.value)}
            required
          />
        </label>
        <label className={styles.field}>
          <span>Date</span>
          <input
            type="date"
            value={date}
            onChange={(event) => setDate(event.target.value)}
            required
          />
        </label>
      </div>
      <div className={styles.actions}>
        <button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? "Saving..." : "Add expense"}
        </button>
        {errorMessage && <p className={styles.error} role="alert">{errorMessage}</p>}
      </div>
    </form>
  );
}
