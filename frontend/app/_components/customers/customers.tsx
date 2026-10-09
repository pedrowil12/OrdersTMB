import { useState, type FormEvent } from "react";
import { api } from "../../_lib/api";
import { formatPhone } from "../../_lib/formatters";
import type { Customer } from "../../_lib/types";
import { Empty } from "../ui/feedback";

export function CustomersView({
  customers,
  onCreate,
}: {
  customers: Customer[];
  onCreate: () => void;
}) {
  return (
    <section className="panel">
      <div className="panel-heading">
        <div>
          <h2>Clientes</h2>
          <p>{customers.length} cadastrado(s)</p>
        </div>
        <button className="primary-button compact" onClick={onCreate}>Novo cliente</button>
      </div>
      {customers.length === 0 ? (
        <Empty text="Nenhum cliente cadastrado." />
      ) : (
        <div className="card-list">
          {customers.map((customer) => (
            <div key={customer.id} className="list-card">
              <div className="avatar">{customer.name.slice(0, 2).toUpperCase()}</div>
              <span>
                <strong>{customer.name}</strong>
                <small>{customer.email} · {formatPhone(customer.phone)}</small>
              </span>
              <span className="active-label">Ativo</span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

export function CustomerForm({
  token,
  onCreated,
  onError,
}: {
  token: string;
  onCreated: () => Promise<void>;
  onError: (error: unknown) => void;
}) {
  const [form, setForm] = useState({ name: "", phone: "", email: "", password: "" });
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    try {
      await api.createCustomer(token, form);
      await onCreated();
    } catch (requestError) {
      onError(requestError);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="modal-form" onSubmit={submit}>
      <label>
        Nome
        <input
          value={form.name}
          onChange={(event) => setForm({ ...form, name: event.target.value })}
          required
          minLength={2}
        />
      </label>
      <label>
        Telefone
        <input
          type="tel"
          inputMode="numeric"
          maxLength={15}
          placeholder="(12) 99999-9999"
          value={form.phone}
          onChange={(event) => setForm({ ...form, phone: formatPhone(event.target.value) })}
          required
        />
      </label>
      <label>
        E-mail
        <input
          type="email"
          value={form.email}
          onChange={(event) => setForm({ ...form, email: event.target.value })}
          required
        />
      </label>
      <label>
        Senha inicial
        <input
          type="password"
          value={form.password}
          onChange={(event) => setForm({ ...form, password: event.target.value })}
          required
          minLength={6}
        />
      </label>
      <button className="primary-button" disabled={submitting}>
        {submitting ? "Salvando..." : "Criar cliente"}
      </button>
    </form>
  );
}
