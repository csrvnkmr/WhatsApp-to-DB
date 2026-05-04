// ================================================
// src/stores/chat.ts
// ================================================
import { defineStore } from "pinia";
import { ref } from "vue";
import { getSessions, getMessages } from "@/services/api";

export const useChatStore = defineStore("chat", () => {
  const sessions = ref<any[]>([]);
  const messages = ref<any[]>([]);
  const selectedSessionId = ref<number | null>(null);
  const loading = ref(false);
  const viewMode = ref<'chat' | 'bookmarks'>('chat')
  const bookmarks = ref<any[]>([])

  async function loadSessions() {
    loading.value = true;

    try {
      sessions.value = await getSessions();
    }
    finally {
      loading.value = false;
    }
  }

  async function loadMessages(sessionId: number) {
    loading.value = true;

    try {
      selectedSessionId.value = sessionId;
      messages.value = await getMessages(sessionId);
    }
    finally {
      loading.value = false;
    }
  }

  return {
    sessions,
    messages,
    selectedSessionId,
    loading,
    loadSessions,
    loadMessages,
    bookmarks,
    viewMode
  };
});
