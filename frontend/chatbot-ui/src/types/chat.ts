export type ChatRequest = {
    message: string;
  };
  
  export type ChatResponse = {
    reply: string;
  };
  export type ApiError = {
    error?: string;
    traceId?: string;
  };