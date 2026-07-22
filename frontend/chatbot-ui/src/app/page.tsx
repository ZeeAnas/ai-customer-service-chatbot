"use client";

import { FormEvent, useState } from "react";
import { ChatApiError, sendChatMessage } from "@/services/chatService";

type Message = {
  id: number;
  role: "user" | "assistant";
  content: string;
};

export default function Home() {
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const trimmedMessage = input.trim();

    if (!trimmedMessage || isLoading) {
      return;
    }

    const userMessage: Message = {
      id: Date.now(),
      role: "user",
      content: trimmedMessage,
    };

    setMessages((currentMessages) => [
      ...currentMessages,
      userMessage,
    ]);

    setInput("");
    setError("");
    setIsLoading(true);

    try {
      const response = await sendChatMessage(trimmedMessage);

      const assistantMessage: Message = {
        id: Date.now() + 1,
        role: "assistant",
        content: response.reply,
      };

      setMessages((currentMessages) => [
        ...currentMessages,
        assistantMessage,
      ]);
    } catch (requestError) {
      console.error(requestError);

      if (requestError instanceof ChatApiError) {
        setError(requestError.message);
      } else {
        setError(
          "The backend could not be reached. Make sure it is running."
        );
      }
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <main className="min-h-screen bg-gray-100 p-4">
      <div className="mx-auto flex min-h-[calc(100vh-2rem)] max-w-3xl flex-col overflow-hidden rounded-2xl bg-white shadow-lg">
        <header className="border-b border-gray-200 p-6">
          <h1 className="text-2xl font-bold text-gray-900">
            Customer Service Chatbot
          </h1>

          <p className="mt-1 text-sm text-gray-500">
            Ask us a question
          </p>
        </header>

        <section className="flex-1 space-y-4 overflow-y-auto p-6">
          {messages.length === 0 && (
            <div className="flex h-full items-center justify-center">
              <p className="text-center text-gray-500">
                Send your first message to test the chatbot.
              </p>
            </div>
          )}

          {messages.map((message) => (
            <div
              key={message.id}
              className={`flex ${message.role === "user"
                  ? "justify-end"
                  : "justify-start"
                }`}
            >
              <div
                className={`max-w-[80%] rounded-2xl px-4 py-3 ${message.role === "user"
                    ? "bg-gray-900 text-white"
                    : "bg-gray-200 text-gray-900"
                  }`}
              >
                <p className="whitespace-pre-wrap">
                  {message.content}
                </p>
              </div>
            </div>
          ))}

          {isLoading && (
            <div className="flex justify-start">
              <div className="rounded-2xl bg-gray-200 px-4 py-3 text-gray-600">
                Thinking...
              </div>
            </div>
          )}
        </section>

        <div className="border-t border-gray-200 p-4">
          {error && (
            <p className="mb-3 text-sm text-red-600">
              {error}
            </p>
          )}

          <form
            onSubmit={handleSubmit}
            className="flex gap-3"
          >
            <input
              type="text"
              value={input}
              onChange={(event) => setInput(event.target.value)}
              placeholder="Type your message..."
              disabled={isLoading}
              maxLength={1000}
              className="flex-1 rounded-xl border border-gray-300 px-4 py-3 text-gray-900 outline-none focus:border-gray-600 disabled:bg-gray-100"
            />

            <button
              type="submit"
              disabled={isLoading || !input.trim()}
              className="rounded-xl bg-gray-900 px-5 py-3 font-medium text-white disabled:cursor-not-allowed disabled:opacity-50"
            >
              Send
            </button>
          </form>
        </div>
      </div>
    </main>
  );
}