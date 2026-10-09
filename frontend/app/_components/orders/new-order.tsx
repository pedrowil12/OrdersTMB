import { useState } from "react";
import { api } from "../../_lib/api";
import { currency } from "../../_lib/formatters";
import type { Product } from "../../_lib/types";
import { ConfirmDialog } from "../ui/dialogs";
import { Empty } from "../ui/feedback";

type CartItem = {
  productId: string;
  quantity: number;
};

export function NewOrder({
  products,
  token,
  onCreated,
  onError,
}: {
  products: Product[];
  token: string;
  onCreated: () => Promise<void>;
  onError: (error: unknown) => void;
}) {
  const [productId, setProductId] = useState(products[0]?.id ?? "");
  const [quantity, setQuantity] = useState(1);
  const [cart, setCart] = useState<CartItem[]>([]);
  const [confirming, setConfirming] = useState(false);
  const [submitting, setSubmitting] = useState(false);

  function addItem() {
    if (!productId || quantity < 1) return;
    const product = products.find((item) => item.id === productId);
    if (!product) return;

    setCart((current) => {
      const existing = current.find((item) => item.productId === productId);
      if (existing) {
        return current.map((item) =>
          item.productId === productId
            ? { ...item, quantity: item.quantity + quantity }
            : item,
        );
      }
      return [...current, { productId, quantity }];
    });
    setQuantity(1);
  }

  const total = cart.reduce((sum, item) => {
    const product = products.find((candidate) => candidate.id === item.productId);
    return sum + (product?.price ?? 0) * item.quantity;
  }, 0);

  async function submit() {
    setSubmitting(true);
    try {
      await api.createOrder(token, cart);
      setConfirming(false);
      await onCreated();
    } catch (requestError) {
      setConfirming(false);
      onError(requestError);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="order-form">
      <div className="inline-form">
        <label>
          Produto
          <select value={productId} onChange={(event) => setProductId(event.target.value)}>
            <option value="">Selecione</option>
            {products.map((product) => (
              <option key={product.id} value={product.id}>
                {product.name} · {currency.format(product.price)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Quantidade
          <input
            type="number"
            min="1"
            value={quantity}
            onChange={(event) => setQuantity(Number(event.target.value))}
          />
        </label>
        <button type="button" className="secondary-button" onClick={addItem}>Adicionar</button>
      </div>

      {cart.length === 0 ? (
        <Empty text="Adicione produtos para começar." />
      ) : (
        <div className="cart-list">
          {cart.map((item) => {
            const product = products.find((candidate) => candidate.id === item.productId)!;
            return (
              <div key={item.productId}>
                <span>
                  <strong>{product.name}</strong>
                  <small>{item.quantity} × {currency.format(product.price)}</small>
                </span>
                <button
                  onClick={() => setCart((current) =>
                    current.filter((candidate) => candidate.productId !== item.productId))}
                >
                  Remover
                </button>
              </div>
            );
          })}
          <div className="cart-total">
            <span>Total</span>
            <strong>{currency.format(total)}</strong>
          </div>
          <button className="primary-button" onClick={() => setConfirming(true)}>Gerar pedido</button>
        </div>
      )}

      {confirming && (
        <ConfirmDialog
          title="Confirmar pedido?"
          message={`O pedido possui ${cart.length} produto(s) e total de ${currency.format(total)}.`}
          confirmLabel={submitting ? "Gerando..." : "Confirmar pedido"}
          disabled={submitting}
          onCancel={() => setConfirming(false)}
          onConfirm={() => { void submit(); }}
        />
      )}
    </div>
  );
}
