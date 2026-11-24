import { apiClientWithAuth } from "~/services/api/client";
import type { AgentKind } from "~/types/agentExperience";
import type { AssistantAction, AssistantChatResponse, AssistantContext, AssistantConversation, AssistantOverview, AssistantStatus, AssistantThread } from "~/types/assistant";

export const assistantService = {
  status: () => apiClientWithAuth<AssistantStatus>("/api/assistant/status"),
  overview: (params?: { page?: number; limit?: number }) => apiClientWithAuth<AssistantOverview>("/api/assistant/overview", { params }),
  actions: (params?: { page?: number; limit?: number }) => apiClientWithAuth<AssistantAction[]>("/api/assistant/actions", { params }),
  approve: (id: number) => apiClientWithAuth<AssistantAction>(`/api/assistant/actions/${id}/approve`, { method: "POST" }),
  reject: (id: number) => apiClientWithAuth<AssistantAction>(`/api/assistant/actions/${id}/reject`, { method: "POST" }),
  review: (id: number) => apiClientWithAuth<AssistantAction>(`/api/assistant/actions/${id}/review`, { method: "POST" }),
  chat: (message: string) => apiClientWithAuth<AssistantChatResponse>("/api/assistant/chat", { method: "POST", body: { message } }),
  threads: (params?: { page?: number; limit?: number }) => apiClientWithAuth<AssistantThread[]>("/api/assistant/threads", { params }),
  createThread: (body: { originAlertId?: number | null; parentThreadId?: number | null; subject?: string | null; specialistId?: AgentKind | null; sourceView?: string | null; subjectEntityType?: string | null; subjectEntityId?: number | null }) => apiClientWithAuth<AssistantConversation>("/api/assistant/threads", { method: "POST", body }),
  thread: (id: number) => apiClientWithAuth<AssistantConversation>(`/api/assistant/threads/${id}`),
  alertContext: (id: number) => apiClientWithAuth<AssistantContext>(`/api/assistant/alerts/${id}/context`),
  sendMessage: (id: number, message: string) => apiClientWithAuth<AssistantConversation>(`/api/assistant/threads/${id}/messages`, { method: "POST", body: { message } }),
  sendMessageWithImage: (id: number, message: string, file: File) => {
    const body = new FormData();
    body.append("message", message);
    body.append("file", file, file.name);
    return apiClientWithAuth<AssistantConversation>(`/api/assistant/threads/${id}/messages/attachment`, { method: "POST", body });
  }
};
