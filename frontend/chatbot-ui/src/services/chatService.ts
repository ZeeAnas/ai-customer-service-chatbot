import type {
  ApiError,
  ChatRequest,
} from "@/types/chat";

const apiUrl = process.env.NEXT_PUBLIC_API_URL;

export class ChatApiError extends Error {
  status: number;
  traceId?: string;

  constructor(
    message: string,
    status: number,
    traceId?: string
  ) {
    super(message);

    this.name = "ChatApiError";
    this.status = status;
    this.traceId = traceId;
  }
}

export async function sendChatMessage(
  messages: ChatRequest["messages"],
  onChunk: (chunk: string) => void,
  signal?: AbortSignal
): Promise<void> {
  if (!apiUrl) {
    throw new ChatApiError(
      "The backend URL is not configured.",
      0
    );
  }

  const requestBody: ChatRequest = {
    messages,
  };

  const response = await fetch(`${apiUrl}/api/chat`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(requestBody),
    signal,
  });

  if (!response.ok) {
    let apiError: ApiError = {};

    try {
      apiError = await response.json();
    } catch {
      
    }

    throw new ChatApiError(
      apiError.error ?? "The chat request failed.",
      response.status,
      apiError.traceId
    );
  }

  if (!response.body) {
    throw new ChatApiError(
      "The backend returned an empty response.",
      response.status
    );
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();

  try {
    while (true) {
      const { value, done } = await reader.read();

      if (done) {
        break;
      }

      const chunk = decoder.decode(value, {
        stream: true,
      });

      if (chunk) {
        onChunk(chunk);
      }
    }

    const finalChunk = decoder.decode();

    if (finalChunk) {
      onChunk(finalChunk);
    }
  } finally {
    reader.releaseLock();
  }
}