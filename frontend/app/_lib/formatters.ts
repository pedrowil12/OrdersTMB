export const currency = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
});

export const dateTime = new Intl.DateTimeFormat("pt-BR", {
  dateStyle: "short",
  timeStyle: "short",
});

export function formatPhone(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 11);
  if (!digits) return "";
  if (digits.length < 3) return `(${digits}`;

  const areaCode = digits.slice(0, 2);
  const number = digits.slice(2);
  const firstPartLength = number.length > 8 ? 5 : 4;
  const firstPart = number.slice(0, firstPartLength);
  const secondPart = number.slice(firstPartLength);

  return `(${areaCode}) ${firstPart}${secondPart ? `-${secondPart}` : ""}`;
}

export function formatCurrencyInput(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 11);
  return digits ? currency.format(Number(digits) / 100) : "";
}

export function parseCurrencyInput(value: string) {
  const digits = value.replace(/\D/g, "");
  return digits ? Number(digits) / 100 : 0;
}
