"use client";

import { FormEvent, KeyboardEvent, useEffect, useRef, useState } from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { api, getErrorMessage } from "../../_lib/api";
import type { ChatMessage } from "../../_lib/types";

const WELCOME_MESSAGE: ChatMessage = {
  role: "assistant",
  content: "Olá! Posso ajudar com informações sobre pedidos e produtos.",
};

type ChatWidgetProps = {
  token: string;
  onError: (error: unknown) => void;
};

export function ChatWidget({ token, onError }: ChatWidgetProps) {
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>([WELCOME_MESSAGE]);
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const abortController = useRef<AbortController | null>(null);
  const inputRef = useRef<HTMLTextAreaElement | null>(null);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    return () => abortController.current?.abort();
  }, []);

  useEffect(() => {
    if (open) inputRef.current?.focus();
  }, [open]);

  useEffect(() => {
    if (open) messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages, open]);

  async function sendMessage(event?: FormEvent) {
    event?.preventDefault();

    const content = input.trim();
    if (!content || sending) return;

    const userMessage: ChatMessage = { role: "user", content };
    const conversation = [...messages, userMessage];
    const history = conversation.slice(1);

    setMessages([...conversation, { role: "assistant", content: "" }]);
    setInput("");
    setError(null);
    setSending(true);

    const controller = new AbortController();
    abortController.current = controller;

    try {
      await api.streamOrderAnalytics(
        token,
        history,
        (chunk) => {
          setMessages((current) => {
            const lastMessage = current[current.length - 1];
            if (lastMessage?.role !== "assistant") return current;

            return [
              ...current.slice(0, -1),
              { ...lastMessage, content: lastMessage.content + chunk },
            ];
          });
        },
        controller.signal,
      );
    } catch (requestError) {
      if (controller.signal.aborted) return;

      setMessages((current) => {
        const lastMessage = current[current.length - 1];
        return lastMessage?.role === "assistant" && !lastMessage.content
          ? current.slice(0, -1)
          : current;
      });
      setError(getErrorMessage(requestError));
      onError(requestError);
    } finally {
      if (abortController.current === controller) {
        abortController.current = null;
        setSending(false);
      }
    }
  }

  function handleInputKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void sendMessage();
    }
  }

  return (
    <div className="chat-widget">
      {open && (
        <section
          id="orders-chat"
          className="chat-panel"
          role="dialog"
          aria-label="Assistente de pedidos"
        >
          <header className="chat-header">
            <div className="chat-avatar" aria-hidden="true">IA</div>
            <div>
              <strong>Assistente OrdersTMB</strong>
              <span>Pedidos e produtos</span>
            </div>
            <button
              type="button"
              className="chat-close"
              aria-label="Fechar assistente"
              onClick={() => setOpen(false)}
            >
              ×
            </button>
          </header>

          <div className="chat-messages" role="log" aria-live="polite">
            {messages.map((message, index) => (
              <div
                key={`${message.role}-${index}`}
                className={`chat-message ${message.role}`}
              >
                {message.content ? (
                  <ReactMarkdown remarkPlugins={[remarkGfm]}>
                    {message.content}
                  </ReactMarkdown>
                ) : (
                  <span className="chat-typing" aria-label="Assistente escrevendo">
                    <i />
                    <i />
                    <i />
                  </span>
                )}
              </div>
            ))}
            <div ref={messagesEndRef} />
          </div>

          {error && <p className="chat-error" role="alert">{error}</p>}

          <form className="chat-form" onSubmit={sendMessage}>
            <label className="sr-only" htmlFor="chat-message">Digite sua mensagem</label>
            <textarea
              id="chat-message"
              ref={inputRef}
              value={input}
              rows={1}
              maxLength={1000}
              placeholder="Pergunte sobre seus pedidos..."
              disabled={sending}
              onChange={(event) => setInput(event.target.value)}
              onKeyDown={handleInputKeyDown}
            />
            <button
              type="submit"
              className="chat-send"
              disabled={sending || !input.trim()}
              aria-label="Enviar mensagem"
            >
              <svg viewBox="0 0 24 24" aria-hidden="true">
                <path d="m4 4 17 8-17 8 3-8-3-8Zm3.8 7h7.7L6.8 6.9 7.8 11Zm-1 6.1 8.7-4.1H7.8l-1 4.1Z" />
              </svg>
            </button>
          </form>
          <small className="chat-hint">Enter envia · Shift + Enter quebra a linha</small>
        </section>
      )}

      <button
        type="button"
        className={open ? "chat-trigger active" : "chat-trigger"}
        aria-label={open ? "Fechar assistente" : "Abrir assistente"}
        aria-expanded={open}
        aria-controls="orders-chat"
        onClick={() => setOpen((current) => !current)}
      >
        {open ? (
          <span aria-hidden="true">×</span>
        ) : (
          <svg viewBox="0 0 24 24" aria-hidden="true">
            <path d="M5 4h14a2 2 0 0 1 2 2v9a2 2 0 0 1-2 2H9l-5 4v-4a2 2 0 0 1-2-2V7a3 3 0 0 1 3-3Zm1 5h12V7H6v2Zm0 4h8v-2H6v2Z" />
          </svg>
        )}
      </button>
    </div>
  );
}
