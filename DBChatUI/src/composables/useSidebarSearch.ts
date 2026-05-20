import { ref, watch, nextTick } from "vue";
import { useChatStore } from "@/stores/chat";
import { searchChats } from "@/services/api";

export function useSidebarSearch() {
  const chat = useChatStore();
  const searchText = ref("");
  const groupedResults = ref<any[]>([]);
  let debounceTimer: any = null;

  watch(searchText, (val) => {
    if (!val) {
      groupedResults.value = [];
      if (debounceTimer) clearTimeout(debounceTimer);
      return;
    }

    if (debounceTimer) clearTimeout(debounceTimer);

    debounceTimer = setTimeout(() => {
      runSearch();
    }, 300);
  });

  async function runSearch() {
    if (!searchText.value) {
      groupedResults.value = [];
      return;
    }

    try {
      const data = await searchChats(searchText.value);
      groupResults(data);
    } catch (err) {
      console.error("Error in search:", err);
      groupedResults.value = [];
    }
  }

  function groupResults(data: any[]) {
    const map: any = {};

    data.forEach((r) => {
      if (!map[r.SessionId]) {
        map[r.SessionId] = {
          sessionId: r.SessionId,
          title: r.SessionTitle,
          messages: [],
        };
      }

      // only add message if it matches text
      if (r.MessageText?.toLowerCase().includes(searchText.value.toLowerCase())) {
        map[r.SessionId].messages.push(r);
      }
    });

    console.log("Search results grouped map", map);
    groupedResults.value = Object.values(map);
  }

  async function openSearchResult(sessionId: number, messageId: number) {
    console.log("openSearchResult", sessionId, messageId);
    console.log("Loading session", sessionId);
    await chat.loadMessages(sessionId);

    chat.viewMode = "chat";
    chat.selectedSessionId = sessionId;
    
    // Clear search text to revert sidebar list back to normal
    searchText.value = "";

    nextTick(() => {
      const el = document.getElementById(`msg-${messageId}`);
      el?.scrollIntoView({
        behavior: "smooth",
        block: "center",
      });
    });
  }

  return {
    searchText,
    groupedResults,
    runSearch,
    openSearchResult,
  };
}
