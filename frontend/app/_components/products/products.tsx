import { useState, type FormEvent } from "react";
import { api } from "../../_lib/api";
import { currency, formatCurrencyInput, parseCurrencyInput } from "../../_lib/formatters";
import type { Product } from "../../_lib/types";
import { Empty } from "../ui/feedback";

export function ProductsView({
  products,
  onCreate,
}: {
  products: Product[];
  onCreate: () => void;
}) {
  return (
    <section className="panel">
      <div className="panel-heading">
        <div>
          <h2>Produtos</h2>
          <p>{products.length} no catálogo</p>
        </div>
        <button className="primary-button compact" onClick={onCreate}>Novo produto</button>
      </div>
      {products.length === 0 ? (
        <Empty text="Nenhum produto cadastrado." />
      ) : (
        <div className="card-list">
          {products.map((product) => (
            <div key={product.id} className="list-card product-card">
              <div className="product-icon">□</div>
              <span>
                <strong>{product.name}</strong>
                <small>{currency.format(product.price)}</small>
              </span>
              <span className="active-label">Ativo</span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

export function ProductForm({
  token,
  onCreated,
  onError,
}: {
  token: string;
  onCreated: () => Promise<void>;
  onError: (error: unknown) => void;
}) {
  const [form, setForm] = useState({ name: "", price: "" });
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    try {
      await api.createProduct(token, form.name, parseCurrencyInput(form.price));
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
        Preço
        <input
          type="text"
          inputMode="numeric"
          placeholder="R$ 0,00"
          value={form.price}
          onChange={(event) => setForm({ ...form, price: formatCurrencyInput(event.target.value) })}
          required
        />
      </label>
      <button className="primary-button" disabled={submitting}>
        {submitting ? "Salvando..." : "Criar produto"}
      </button>
    </form>
  );
}
