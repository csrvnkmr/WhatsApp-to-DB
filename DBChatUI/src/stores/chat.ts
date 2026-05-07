// ================================================
// src/stores/chat.ts
// ================================================
import { defineStore } from "pinia";
import { ref } from "vue";
import { getSessions, getMessagesDatabases } from "@/services/api";

export const useChatStore = defineStore("chat", () => {
  const sessions = ref<any[]>([]);
  const messages = ref<any[]>([]);
  const selectedSessionId = ref<number | null>(null);
  const loading = ref(false);
  const viewMode = ref<'chat' | 'bookmarks'>('chat')
  const bookmarks = ref<any[]>([])
  const selectedDatabases = ref<string[]>([])

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

      console.log("Loading messages for session", sessionId);
      selectedSessionId.value = sessionId;
      //messages.value = await getMessages(sessionId);
      messages.value = await getMessagesDatabases(sessionId, selectedDatabases.value);
    }
    finally {
      loading.value = false;
    }
  }

  function setSelectedDatabases(items: string[]) {
    console.log("Setting DB filter", items)
    selectedDatabases.value = items
  }

  return {
    sessions,
    messages,
    selectedSessionId,
    loading,
    loadSessions,
    loadMessages,
    bookmarks,
    viewMode,
    selectedDatabases,
    setSelectedDatabases
  };
});
