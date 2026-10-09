import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "OrdersTMB | Gestão de pedidos",
  description: "Gestão simples de clientes, produtos e pedidos.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="pt-BR">
      <body>{children}</body>
    </html>
  );
}
