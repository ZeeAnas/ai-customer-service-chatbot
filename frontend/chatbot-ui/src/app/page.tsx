
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
  getChatHistory,
  sendChatMessage,
  submitLead,
} from "@/services/chatService";
import { X } from "lucide-react";

type Message = {
  id: string;
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
const CHAT_SESSION_ID_KEY = "chatSessionId";

const CHAT_EXPIRATION_MINUTES = 30;

const FALLBACK_RESPONSE_PREFIX =
  "I couldn't find a reliable answer to your question.";

function getOrCreateSessionId(): string {
  const existingSessionId = sessionStorage.getItem(
    CHAT_SESSION_ID_KEY
  );

  if (existingSessionId) {
    return existingSessionId;
  }

  const newSessionId = crypto.randomUUID();

  sessionStorage.setItem(
    CHAT_SESSION_ID_KEY,
    newSessionId
  );

  return newSessionId;
}

function clearStoredConversation() {
  sessionStorage.removeItem(CHAT_MESSAGES_KEY);
  sessionStorage.removeItem(LAST_ACTIVITY_KEY);
  sessionStorage.removeItem(CHAT_SESSION_ID_KEY);
}

export default function Home() {
  const [input, setInput] = useState("");

  const [messages, setMessages] = useState<Message[]>(
    []
  );

  const [hasLoadedMessages, setHasLoadedMessages] =
    useState(false);

  const [showHandoffForm, setShowHandoffForm] =
    useState(false);

  const [handoffName, setHandoffName] = useState("");

  const [handoffEmail, setHandoffEmail] =
    useState("");

  const [handoffPhone, setHandoffPhone] =
    useState("");

  const [handoffMessage, setHandoffMessage] =
    useState("");

  const [handoffConsent, setHandoffConsent] =
    useState(false);

  const [handoffSuccess, setHandoffSuccess] =
    useState("");

  const [handoffError, setHandoffError] =
    useState("");

  const [isSubmittingHandoff, setIsSubmittingHandoff] =
    useState(false);

  const [isLoading, setIsLoading] = useState(false);

  const [error, setError] = useState("");

  const abortControllerRef =
    useRef<AbortController | null>(null);

  const messagesEndRef =
    useRef<HTMLDivElement | null>(null);

  const handoffFormRef =
    useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const abortController = new AbortController();

    async function loadConversationHistory() {
      const savedMessages = sessionStorage.getItem(
        CHAT_MESSAGES_KEY
      );

      const savedLastActivity = sessionStorage.getItem(
        LAST_ACTIVITY_KEY
      );

      const expirationMilliseconds =
        CHAT_EXPIRATION_MINUTES * 60 * 1000;

      const lastActivity = savedLastActivity
        ? Number(savedLastActivity)
        : 0;

      const conversationHasExpired =
        lastActivity > 0 &&
        Date.now() - lastActivity >
          expirationMilliseconds;

      if (conversationHasExpired) {
        clearStoredConversation();
      }

      const sessionId = getOrCreateSessionId();

      try {
        const history = await getChatHistory(
          sessionId,
          abortController.signal
        );

        const restoredMessages: Message[] =
          history.map((message) => ({
            id: message.id,
            role: message.role,
            content: message.content,
          }));

        setMessages(restoredMessages);
        setError("");
      } catch (requestError) {
        if (
          requestError instanceof DOMException &&
          requestError.name === "AbortError"
        ) {
          return;
        }

        console.error(
          "Could not load conversation history:",
          requestError
        );

        if (!conversationHasExpired && savedMessages) {
          try {
            const parsedMessages = JSON.parse(
              savedMessages
            ) as Message[];

            setMessages(parsedMessages);
          } catch {
            sessionStorage.removeItem(
              CHAT_MESSAGES_KEY
            );

            sessionStorage.removeItem(
              LAST_ACTIVITY_KEY
            );
          }
        }

        setError(
          "The saved conversation could not be loaded from the server."
        );
      } finally {
        setHasLoadedMessages(true);
      }
    }

    void loadConversationHistory();

    return () => {
      abortController.abort();
    };
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
    if (!hasLoadedMessages) {
      return;
    }

    messagesEndRef.current?.scrollIntoView({
      behavior: "smooth",
    });
  }, [messages, hasLoadedMessages]);

  useEffect(() => {
    const latestAssistantMessage = [...messages]
      .reverse()
      .find(
        (message) => message.role === "assistant"
      );

    const isFallbackResponse =
      latestAssistantMessage?.content
        .trim()
        .startsWith(FALLBACK_RESPONSE_PREFIX) ??
      false;

    if (!isFallbackResponse) {
      return;
    }

    setHandoffError("");
    setShowHandoffForm(true);

    const timeoutId = window.setTimeout(() => {
      handoffFormRef.current?.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
    }, 100);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [messages]);

  function handleStopGenerating() {
    abortControllerRef.current?.abort();

    setMessages((currentMessages) => {
      const lastMessage =
        currentMessages[
          currentMessages.length - 1
        ];

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

    if (
      !trimmedMessage ||
      isLoading ||
      !hasLoadedMessages
    ) {
      return;
    }

    const userMessage: Message = {
      id: crypto.randomUUID(),
      role: "user",
      content: trimmedMessage,
    };

    const assistantMessageId = crypto.randomUUID();

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
      const sessionId = getOrCreateSessionId();

      await sendChatMessage(
        sessionId,
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
      if (
        requestError instanceof DOMException &&
        requestError.name === "AbortError"
      ) {
        return;
      }

      console.error(requestError);

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

    setHandoffError("");
    setHandoffSuccess("");

    if (!trimmedName) {
      setHandoffError("Please enter your name.");
      return;
    }

    if (!trimmedEmail && !trimmedPhone) {
      setHandoffError(
        "Please enter either an email address or a phone number."
      );

      return;
    }

    if (!trimmedMessage) {
      setHandoffError(
        "Please describe how Montana Barber can help you."
      );

      return;
    }

    if (!handoffConsent) {
      setHandoffError(
        "You must agree to be contacted before submitting."
      );

      return;
    }

    if (isSubmittingHandoff) {
      return;
    }

    setIsSubmittingHandoff(true);

    try {
      const sessionId = getOrCreateSessionId();

      await submitLead({
        sessionId,
        name: trimmedName,
        email: trimmedEmail || null,
        phone: trimmedPhone || null,
        message: trimmedMessage,
        consentToContact: handoffConsent,
      });

      setHandoffSuccess(
        "Your request has been submitted. Montana Barber will contact you soon."
      );

      setHandoffName("");
      setHandoffEmail("");
      setHandoffPhone("");
      setHandoffMessage("");
      setHandoffConsent(false);
      setShowHandoffForm(false);
    } catch (requestError) {
      console.error(
        "Could not submit lead:",
        requestError
      );

      if (requestError instanceof ChatApiError) {
        if (requestError.status === 404) {
          setHandoffError(
            "Please send a chat message before requesting human assistance."
          );

          return;
        }

        setHandoffError(requestError.message);
        return;
      }

      setHandoffError(
        "Something went wrong while sending your request. Please try again."
      );
    } finally {
      setIsSubmittingHandoff(false);
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
          {!hasLoadedMessages && (
            <div className="flex h-full items-center justify-center">
              <div
                className="flex items-center gap-1"
                aria-label="Loading conversation"
              >
                <span className="h-2 w-2 animate-bounce rounded-full bg-gray-500 [animation-delay:-0.3s]" />

                <span className="h-2 w-2 animate-bounce rounded-full bg-gray-500 [animation-delay:-0.15s]" />

                <span className="h-2 w-2 animate-bounce rounded-full bg-gray-500" />
              </div>
            </div>
          )}

          {hasLoadedMessages &&
            messages.length === 0 && (
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
            <div
              role="status"
              aria-live="polite"
              className="rounded-xl border border-green-200 bg-green-50 px-4 py-3 text-sm text-green-800"
            >
              {handoffSuccess}
            </div>
          )}

          {hasLoadedMessages && (
            <div className="mx-auto w-full max-w-md">
              <button
                type="button"
                onClick={() => {
                  setHandoffError("");
                  setHandoffSuccess("");
                  setShowHandoffForm(true);
                }}
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
                      disabled={isSubmittingHandoff}
                      aria-label="Close contact form"
                      className="absolute right-2 top-2 flex h-9 w-9 items-center justify-center rounded-full text-red-500 transition hover:bg-red-100 hover:text-red-700 disabled:cursor-not-allowed disabled:opacity-50"
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
                      disabled={isSubmittingHandoff}
                      maxLength={100}
                      onChange={(event) =>
                        setHandoffName(
                          event.target.value
                        )
                      }
                      className="w-full rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200 disabled:cursor-not-allowed disabled:bg-gray-100"
                    />

                    <input
                      type="email"
                      placeholder="Email address (optional)"
                      value={handoffEmail}
                      disabled={isSubmittingHandoff}
                      maxLength={254}
                      onChange={(event) =>
                        setHandoffEmail(
                          event.target.value
                        )
                      }
                      className="w-full rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200 disabled:cursor-not-allowed disabled:bg-gray-100"
                    />

                    <input
                      type="tel"
                      placeholder="Phone number (optional)"
                      value={handoffPhone}
                      disabled={isSubmittingHandoff}
                      maxLength={30}
                      onChange={(event) =>
                        setHandoffPhone(
                          event.target.value
                        )
                      }
                      className="w-full rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200 disabled:cursor-not-allowed disabled:bg-gray-100"
                    />

                    <p className="text-xs text-gray-500">
                      Enter at least an email address or a
                      phone number.
                    </p>

                    <textarea
                      placeholder="How can we help you?"
                      rows={4}
                      value={handoffMessage}
                      disabled={isSubmittingHandoff}
                      maxLength={1000}
                      onChange={(event) =>
                        setHandoffMessage(
                          event.target.value
                        )
                      }
                      className="w-full resize-none rounded-xl border border-gray-300 bg-white px-3 py-2.5 text-gray-900 placeholder:text-gray-500 outline-none transition focus:border-gray-700 focus:ring-2 focus:ring-gray-200 disabled:cursor-not-allowed disabled:bg-gray-100"
                    />

                    <label className="flex items-start gap-3 text-sm text-gray-700">
                      <input
                        type="checkbox"
                        checked={handoffConsent}
                        disabled={isSubmittingHandoff}
                        onChange={(event) =>
                          setHandoffConsent(
                            event.target.checked
                          )
                        }
                        className="mt-1 h-4 w-4 rounded border-gray-300"
                      />

                      <span>
                        I agree that Montana Barber may
                        contact me using the information
                        provided.
                      </span>
                    </label>

                    {handoffError && (
                      <p
                        role="alert"
                        className="rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700"
                      >
                        {handoffError}
                      </p>
                    )}

                    <button
                      type="button"
                      onClick={handleHandoffSubmit}
                      disabled={isSubmittingHandoff}
                      className="w-full rounded-xl bg-gray-900 px-4 py-3 font-medium text-white transition hover:bg-gray-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      {isSubmittingHandoff
                        ? "Sending..."
                        : "Send request"}
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}

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
              placeholder={
                hasLoadedMessages
                  ? "Type your message..."
                  : "Loading conversation..."
              }
              disabled={
                isLoading || !hasLoadedMessages
              }
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
                disabled={
                  !input.trim() ||
                  !hasLoadedMessages
                }
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
