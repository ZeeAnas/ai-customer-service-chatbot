import type {
  ApiError,
  ChatRequest,
} from "@/types/chat";

const apiUrl = process.env.NEXT_PUBLIC_API_URL;

export type ChatHistoryMessage = {
  id: string;
  role: "user" | "assistant";
  content: string;
  createdAt: string;
};

export type CreateLeadRequest = {
  sessionId: string;
  name: string;
  email: string | null;
  phone: string | null;
  message: string;
  consentToContact: boolean;
};

export type LeadResponse = {
  id: number;
  conversationId: string;
  name: string;
  email: string | null;
  phone: string | null;
  message: string;
  status: number;
  createdAtUtc: string;
};

type ApiValidationError = ApiError & {
  title?: string;
  errors?: Record<string, string[]>;
};

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

function ensureApiUrl(): string {
  if (!apiUrl) {
    throw new ChatApiError(
      "The backend URL is not configured.",
      0
    );
  }

  return apiUrl;
}

async function getApiError(
  response: Response,
  fallbackMessage: string
): Promise<ChatApiError> {
  let apiError: ApiValidationError = {};

  try {
    apiError =
      (await response.json()) as ApiValidationError;
  } catch {
    // Use the fallback message when the response
    // does not contain valid JSON.
  }

  const validationMessage = apiError.errors
    ? Object.values(apiError.errors).flat()[0]
    : undefined;

  return new ChatApiError(
    validationMessage ??
      apiError.error ??
      apiError.title ??
      fallbackMessage,
    response.status,
    apiError.traceId
  );
}

export async function getChatHistory(
  sessionId: string,
  signal?: AbortSignal
): Promise<ChatHistoryMessage[]> {
  const baseUrl = ensureApiUrl();

  const response = await fetch(
    `${baseUrl}/api/chat/history/${encodeURIComponent(
      sessionId
    )}`,
    {
      method: "GET",
      headers: {
        Accept: "application/json",
      },
      signal,
    }
  );

  if (!response.ok) {
    throw await getApiError(
      response,
      "The conversation history could not be loaded."
    );
  }

  return (await response.json()) as ChatHistoryMessage[];
}

export async function sendChatMessage(
  sessionId: string,
  messages: ChatRequest["messages"],
  onChunk: (chunk: string) => void,
  signal?: AbortSignal
): Promise<void> {
  const baseUrl = ensureApiUrl();

  const requestBody: ChatRequest = {
    sessionId,
    messages,
  };

  const response = await fetch(
    `${baseUrl}/api/chat`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(requestBody),
      signal,
    }
  );

  if (!response.ok) {
    throw await getApiError(
      response,
      "The chat request failed."
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

export async function submitLead(
  request: CreateLeadRequest,
  signal?: AbortSignal
): Promise<LeadResponse> {
  const baseUrl = ensureApiUrl();

  const response = await fetch(
    `${baseUrl}/api/leads`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      body: JSON.stringify(request),
      signal,
    }
  );

  if (!response.ok) {
    throw await getApiError(
      response,
      "The contact request could not be submitted."
    );
  }

  return (await response.json()) as LeadResponse;
}