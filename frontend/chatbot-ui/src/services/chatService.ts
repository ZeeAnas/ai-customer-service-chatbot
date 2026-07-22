import type {
    ApiError,
    ChatRequest,
    ChatResponse,
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
    message: string,
    signal?: AbortSignal
  ): Promise<ChatResponse> {
    if (!apiUrl) {
      throw new ChatApiError(
        "The backend URL is not configured.",
        0
      );
    }
  
    const requestBody: ChatRequest = {
      message,
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
        // The backend did not return a JSON error response.
      }
  
      throw new ChatApiError(
        apiError.error ?? "The chat request failed.",
        response.status,
        apiError.traceId
      );
    }
  
    return await response.json() as ChatResponse;
  }