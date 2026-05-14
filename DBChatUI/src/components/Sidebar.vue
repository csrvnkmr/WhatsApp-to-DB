<!-- ============================================= -->
<!-- src/components/Sidebar.vue -->
<!-- ============================================= -->
<template>
<div class="w-full md:w-72 h-full overflow-auto bg-panel flex flex-col">

    <!--<div class="p-4 border-b font-bold text-xl">-->
<div class="p-4">

    <div class="flex items-center justify-between">

        <!-- Left: Welcome + Username inline -->
        <div class="flex items-center gap-2 min-w-0">

            <span class="text-sm text-gray-500">
                Welcome
            </span>

            <span class="font-semibold text-base truncate max-w-[140px]">
                {{ userName }}
            </span>

        </div>

        <!-- Right: Logout (smaller + subtle) -->
        <button
            @click="logout"
            class="text-xs px-2 py-1 rounded-md text-gray-500 hover:text-black hover:bg-hover transition">
            Logout
        </button>

    </div>

</div>

    <div class="p-3">

        <button
             @click="newChat"
            :disabled="chat.loading"
            class="w-full bg-user rounded-xl p-3 mb-3 disabled:opacity-50">
            + New Chat
        </button>

        <!-- SEARCH -->
        <input
            v-model="searchText"
            placeholder="Search chats..."
            class="w-full mb-1 rounded-xl px-3 py-2 outline-none bg-panel" />
        <div
            @click="loadBookmarks"
            class="cursor-pointer px-3 py-2 rounded-lg bg-panel hover:bg-hover transition">

            📌 Bookmarks

        </div>
        <div
            v-if="chat.loading"
            class="text-sm text-gray-500 p-2">
            Loading...
        </div>

        <div
            v-for="item in filteredSessions"
            :key="item.id"
            @click="openSession(item.id)"
            class="p-2 mb-1 rounded-xl cursor-pointer border transition"
            :class="Number(chat.selectedSessionId) === Number(item.id)
                ? 'bg-selected border-blue-400 shadow-sm'
                : 'bg-panel hover:bg-hover border-gray-200'">

            <div class="font-medium text-sm truncate">
                {{ item.title }}
            </div>

            <div class="text-xs mt-1 opacity-60">
                {{ item.updatedOn }}
            </div>

        </div>
        <div
            v-if="filteredSessions.length === 0 && !chat.loading"
            class="text-sm text-gray-400 p-2">
            No chats found.
        </div>

        <div v-if="searchText">

    <div
        v-for="s in groupedResults"
        :key="s.SessionId"
        class="mb-3">

        <!-- SESSION HEADER -->
        <div class="font-semibold text-sm px-2 py-1 text-gray-700">
            {{ s.title }}
        </div>

        <!-- QUESTIONS -->
        <div
            v-for="m in s.messages"
            :key="m.MessageId"
            @click="openSearchResult(s.sessionId, m.MessageId)"
            class="text-sm px-3 py-1 cursor-pointer rounded bg-panel hover:bg-hover transition">

            {{ m.MessageText }}

        </div>

    </div>

</div>
    </div>

</div>
</template>

<script setup lang="ts">
import { computed, onMounted } from "vue";
import { useChatStore } from "@/stores/chat";
import { useAuthStore } from "@/stores/auth";
import { useSidebarSearch } from "@/composables/useSidebarSearch";
import { logoutSession, getBookmarks } from "@/services/api";

const emit = defineEmits(["closeMobile"]);

const chat = useChatStore();
const auth = useAuthStore();

const { searchText, groupedResults, openSearchResult } = useSidebarSearch();

onMounted(async () => {
    if (chat.loading)
        return;
    await chat.loadSessions();
});

// ==========================================
// Username from localStorage or auth store
// ==========================================
const userName = computed(() =>
    localStorage.getItem("username")
    || auth.userName
    || "User"
);

const filteredSessions = computed(() => {
    return chat.sessions;
});

async function openSession(id: number) {
    console.log("Calling openSession", id);
    if (chat.loading)
        return;
    chat.viewMode = 'chat';

    await chat.loadMessages(id);

    // auto hide mobile menu
    emit("closeMobile");
}

// ==========================================
// SAME as ChatWindow New Chat
// ==========================================
function newChat() {
    if (chat.loading)
        return;

    chat.messages = [];
    chat.selectedSessionId = null;

    // close mobile menu
    emit("closeMobile");
}

// ==========================================
// Logout
// ==========================================
async function logout() {
    try {
        await logoutSession();
    } catch (err) {
        console.error("Logout request failed:", err);
    }

    localStorage.removeItem("token");
    localStorage.removeItem("username");

    location.reload();
}

// ==========================================
// Load Bookmarks
// ==========================================
async function loadBookmarks() {
    if (chat.loading) return;

    try {
        const data = await getBookmarks();

        // 🔥 KEY: map bookmarks → messages
        chat.messages = [];

        data.forEach((b: any) => {
            // USER QUESTION
            chat.messages.push({
                id: b.Id,
                role: "User",
                messageText: b.BookmarkText,
                createdOn: b.CreatedOn,
                isBookmarkView: true
            });

            // AI ANSWER
            chat.messages.push({
                id: b.Id,
                role: "Assistant",
                messageText: b.MessageText,
                createdOn: b.CreatedOn,
                canShowSql: b.CanShowSql,
                canShowData: b.CanShowData,
                canShowChart: b.CanShowChart,
                originalMessageId: b.Id,
                isBookmarkView: true,
                isBookmarked: true
            });
        });

        console.log("Bookmarks loaded ", chat.messages.length);
        chat.viewMode = 'bookmarks';
        chat.selectedSessionId = null;
    } catch (err) {
        console.error("Failed to load bookmarks:", err);
    }
}
</script>
