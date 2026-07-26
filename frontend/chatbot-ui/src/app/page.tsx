"use client";

import {
  FormEvent,
  useEffect,
  useRef,
  useState,
} from "react";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import {
  ChatApiError,
  sendChatMessage,
} from "@/services/chatService";
import { X } from "lucide-react";

type Message = {
  id: number;
  role: "user" | "assistant";
  content: string;
};

const suggestedQuestions = [
  "What are your prices?",
  "How do I book an appointment?",
  "Where are you located?",
  "Do you offer beard trimming?",
];

const CHAT_MESSAGES_KEY = "chatMessages";
const LAST_ACTIVITY_KEY = "chatLastActivity";
const CHAT_EXPIRATION_MINUTES = 30;

const FALLBACK_RESPONSE_PREFIX =
  "I couldn't find a reliable answer to your question.";

export default function Home() {
  const [input, setInput] = useState("");

  const [messages, setMessages] = useState<Message[]>([]);
  const [hasLoadedMessages, setHasLoadedMessages] =
    useState(false);

  const [showHandoffForm, setShowHandoffForm] =
    useState(false);

  const [handoffName, setHandoffName] = useState("");
  const [handoffEmail, setHandoffEmail] = useState("");
  const [handoffPhone, setHandoffPhone] = useState("");
  const [handoffMessage, setHandoffMessage] =
    useState("");
  const [handoffSuccess, setHandoffSuccess] =
    useState("");

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState("");

  const abortControllerRef =
    useRef<AbortController | null>(null);

  const messagesEndRef =
    useRef<HTMLDivElement | null>(null);

  const handoffFormRef =
    useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const savedMessages =
      sessionStorage.getItem(CHAT_MESSAGES_KEY);

    const savedLastActivity =
      sessionStorage.getItem(LAST_ACTIVITY_KEY);

    const expirationMilliseconds =
      CHAT_EXPIRATION_MINUTES * 60 * 1000;

    const lastActivity = savedLastActivity
      ? Number(savedLastActivity)
      : 0;

    const conversationHasExpired =
      Date.now() - lastActivity > expirationMilliseconds;

    if (conversationHasExpired) {
      sessionStorage.removeItem(CHAT_MESSAGES_KEY);
      sessionStorage.removeItem(LAST_ACTIVITY_KEY);
      setHasLoadedMessages(true);
      return;
    }

    if (savedMessages) {
      try {
        const parsedMessages =
          JSON.parse(savedMessages) as Message[];

        setMessages(parsedMessages);
      } catch {
        sessionStorage.removeItem(CHAT_MESSAGES_KEY);
        sessionStorage.removeItem(LAST_ACTIVITY_KEY);
      }
    }

    setHasLoadedMessages(true);
  }, []);

  useEffect(() => {
    if (!hasLoadedMessages) {
      return;
    }

    sessionStorage.setItem(
      CHAT_MESSAGES_KEY,
      JSON.stringify(messages)
    );

    if (messages.length > 0) {
      sessionStorage.setItem(
        LAST_ACTIVITY_KEY,
        Date.now().toString()
      );
    } else {
      sessionStorage.removeItem(LAST_ACTIVITY_KEY);
    }
  }, [messages, hasLoadedMessages]);

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({
      behavior: "smooth",
    });
  }, [messages]);

  useEffect(() => {
    const latestAssistantMessage = [...messages]
      .reverse()
      .find(
        (message) => message.role === "assistant"
      );

    const isFallbackResponse =
      latestAssistantMessage?.content
        .trim()
        .startsWith(FALLBACK_RESPONSE_PREFIX) ?? false;

    if (!isFallbackResponse) {
      return;
    }

    setShowHandoffForm(true);

    window.setTimeout(() => {
      handoffFormRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
    }, 100);
  }, [messages]);

  function handleStopGenerating() {
    abortControllerRef.current?.abort();

    setMessages((currentMessages) => {
      const lastMessage =
        currentMessages[currentMessages.length - 1];

      if (
        lastMessage?.role === "assistant" &&
        !lastMessage.content.trim()
      ) {
        return currentMessages.slice(0, -1);
      }

      return currentMessages;
    });
  }

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>
  ) {
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

    const assistantMessageId = Date.now() + 1;

    const assistantMessage: Message = {
      id: assistantMessageId,
      role: "assistant",
      content: "",
    };

    const updatedMessages = [
      ...messages,
      userMessage,
    ];

    setMessages([
      ...updatedMessages,
      assistantMessage,
    ]);

    setInput("");
    setError("");
    setHandoffSuccess("");
    setIsLoading(true);

    const abortController = new AbortController();

    abortControllerRef.current = abortController;

    try {
      await sendChatMessage(
        updatedMessages
          .filter((message) =>
            message.content.trim()
          )
          .map(({ role, content }) => ({
            role,
            content,
          })),

        (chunk) => {
          setMessages((currentMessages) =>
            currentMessages.map((message) =>
              message.id === assistantMessageId
                ? {
                    ...message,
                    content:
                      message.content + chunk,
                  }
                : message
            )
          );
        },

        abortController.signal
      );
    } catch (requestError) {
      console.error(requestError);

      if (
        requestError instanceof DOMException &&
        requestError.name === "AbortError"
      ) {
        return;
      }

      setMessages((currentMessages) =>
        currentMessages.filter(
          (message) =>
            message.id !== assistantMessageId
        )
      );

      if (requestError instanceof ChatApiError) {
        setError(requestError.message);
      } else {
        setError(
          "The backend could not be reached. Make sure it is running."
        );
      }
    } finally {
      abortControllerRef.current = null;
      setIsLoading(false);
    }
  }

  async function handleHandoffSubmit() {
    const trimmedName = handoffName.trim();
    const trimmedEmail = handoffEmail.trim();
    const trimmedPhone = handoffPhone.trim();
    const trimmedMessage = handoffMessage.trim();

    if (
      !trimmedName ||
      !trimmedEmail ||
      !trimmedMessage
    ) {
      alert(
        "Please enter your name, email and message."
      );
      return;
    }

    try {
      const response = await fetch(
        "http://localhost:5130/api/handoff",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            name: trimmedName,
            email: trimmedEmail,
            phone: trimmedPhone || null,
            message: trimmedMessage,
          }),
        }
      );

      if (!response.ok) {
        throw new Error(
          "The handoff request could not be submitted."
        );
      }

      setHandoffSuccess(
        "Your request has been submitted. Montana Barber will contact you soon."
      );

      setHandoffName("");
      setHandoffEmail("");
      setHandoffPhone("");
      setHandoffMessage("");
      setShowHandoffForm(false);
    } catch (requestError) {
      console.error(requestError);

      alert(
        "Something went wrong while sending your request. Please try again."
      );
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
            <div className="flex h-full flex-col items-center justify-center">
              <p className="mb-4 text-center text-gray-600">
                How can we help you?
              </p>

              <div className="grid w-full max-w-md gap-2 sm:grid-cols-2">
                {suggestedQuestions.map(
                  (question) => (
                    <button
                      key={question}
                      type="button"
                      onClick={() =>
                        setInput(question)
                      }
                      className="rounded-xl border border-gray-300 bg-white px-4 py-3 text-left text-sm text-gray-800 transition hover:border-gray-500 hover:bg-gray-50"
                    >
                      {question}
                    </button>
                  )
                )}
              </div>
            </div>
          )}

          {messages.map((message) => (
            <div
              key={message.id}
              className={`flex ${
                message.role === "user"
                  ? "justify-end"
                  : "justify-start"
              }`}
            >
              <div
                className={`max-w-[80%] rounded-2xl px-4 py-3 ${
                  message.role === "user"
                    ? "bg-gray-900 text-white"
                    : "bg-gray-200 text-gray-900"
                }`}
              >
                {message.role === "assistant" ? (
                  message.content ? (
                    <ReactMarkdown
                      remarkPlugins={[remarkGfm]}
                      components={{
                        p: ({ children }) => (
                          <p className="mb-2 last:mb-0">
                            {children}
                          </p>
                        ),

                        ul: ({ children }) => (
                          <ul className="mb-2 list-disc space-y-1 pl-5">
                            {children}
                          </ul>
                        ),

                        ol: ({ children }) => (
                          <ol className="mb-2 list-decimal space-y-1 pl-5">
                            {children}
                          </ol>
                        ),

                        h1: ({ children }) => (
                          <h1 className="mb-2 text-xl font-bold">
                            {children}
                          </h1>
                        ),

                        h2: ({ children }) => (
                          <h2 className="mb-2 text-lg font-bold">
                            {children}
                          </h2>
                        ),

                        h3: ({ children }) => (
                          <h3 className="mb-2 font-semibold">
                            {children}
                          </h3>
                        ),

                        strong: ({ children }) => (
                          <strong className="font-semibold">
                            {children}
                          </strong>
                        ),

                        a: ({
                          children,
                          href,
                        }) => (
                          <a
                            href={href}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="font-medium underline"
                          >
                            {children}
                          </a>
                        ),

                        code: ({ children }) => (
                          <code className="rounded bg-gray-300 px-1 py-0.5 text-sm">
                            {children}
                          </code>
                        ),
                      }}
                    >
                      {message.content}
                    </ReactMarkdown>
                  ) : isLoading ? (
                    <div
                      className="flex items-center gap-1 py-1"
                      aria-label="Assistant is typing"
                    >
                      <span className="h-2 w-2 animate-bounce rounded-full bg-gray-500 [animation-delay:-0.3s]" />

                      <span className="h-2 w-2 animate-bounce rounded-full bg-gray-500 [animation-delay:-0.15s]" />

                      <span className="h-2 w-2 animate-bounce rounded-full bg-gray-500" />
                    </div>
                  ) : null
                ) : (
                  <p className="whitespace-pre-wrap">
                    {message.content}
                  </p>
                )}
              </div>
            </div>
          ))}

          {handoffSuccess && (
            <div className="rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-800">
              {handoffSuccess}
            </div>
          )}

          <div className="mx-auto w-full max-w-md">
            <button
              type="button"
              onClick={() =>
                setShowHandoffForm(true)
              }
              className="w-full rounded-xl border border-gray-900 bg-gray-900 px-4 py-3 text-sm font-medium text-white transition hover:bg-gray-700"
            >
              Talk to a person
            </button>

            {showHandoffForm && (
              <div
                ref={handoffFormRef}
                className="relative mt-3 rounded-2xl border border-gray-300 bg-gray-50 p-5 shadow-sm"
              >
                <div className="mb-4 pr-10">
                  <h2 className="font-semibold text-gray-900">
                    Contact Montana Barber
                  </h2>

                  <p className="mt-1 text-sm text-gray-600">
                    Leave your details and we will get
                    back to you.
                  </p>

                  <button
                    type="button"
                    onClick={() =>
                      setShowHandoffForm(false)
                    }
                    aria-label="Close contact form"
                    className="absolute right-2 top-2 flex h-9 w-9 items-center justify-center rounded-full text-red-500 transition hover:bg-red-100 hover:text-red-700"
                  >
                    <X
                      size={21}
                      strokeWidth={2.5}
                    />
                  </button>
                </div>

                <div className="space-y-3">
                  <input
                    type="text"
                    placeholder="Your name"
                    value={handoffName}
                    onChange={(event) =>
                      setHandoffName(
                        event.target.value
                      )
                    }
                    className="w-full rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200"
                  />

                  <input
                    type="email"
                    placeholder="Your email"
                    value={handoffEmail}
                    onChange={(event) =>
                      setHandoffEmail(
                        event.target.value
                      )
                    }
                    className="w-full rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200"
                  />

                  <input
                    type="tel"
                    placeholder="Phone number (optional)"
                    value={handoffPhone}
                    onChange={(event) =>
                      setHandoffPhone(
                        event.target.value
                      )
                    }
                    className="w-full rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200"
                  />

                  <textarea
                    placeholder="How can we help you?"
                    rows={4}
                    value={handoffMessage}
                    onChange={(event) =>
                      setHandoffMessage(
                        event.target.value
                      )
                    }
                    className="w-full resize-none rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200"
                  />

                  <button
                    type="button"
                    onClick={handleHandoffSubmit}
                    className="w-full rounded-xl bg-gray-900 px-4 py-3 font-medium text-white transition hover:bg-gray-700"
                  >
                    Send request
                  </button>
                </div>
              </div>
            )}
          </div>

          <div ref={messagesEndRef} />
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
              onChange={(event) =>
                setInput(event.target.value)
              }
              placeholder="Type your message..."
              disabled={isLoading}
              maxLength={1000}
              className="flex-1 rounded-xl border border-gray-300 bg-white px-4 py-3 text-gray-900 placeholder:text-gray-500 outline-none focus:border-gray-600 disabled:bg-gray-100"
            />

            {isLoading ? (
              <button
                type="button"
                onClick={handleStopGenerating}
                className="rounded-xl bg-red-600 px-5 py-3 font-medium text-white"
              >
                Stop
              </button>
            ) : (
              <button
                type="submit"
                disabled={!input.trim()}
                className="rounded-xl bg-gray-900 px-5 py-3 font-medium text-white disabled:cursor-not-allowed disabled:opacity-50"
              >
                Send
              </button>
            )}
          </form>
        </div>
      </div>
    </main>
  );
}