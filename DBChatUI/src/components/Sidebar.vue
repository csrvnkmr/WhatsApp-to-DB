<!-- ============================================= -->
<!-- src/components/Sidebar.vue -->
<!-- ============================================= -->
<template>
<div class="w-full md:w-72 h-full bg-panel flex flex-col overflow-hidden">

    <!-- WELCOME HEADER (Fixed) -->
    <div class="p-4 shrink-0 border-b border-soft/50">
        <div class="flex items-center justify-between">
            <!-- Left: Welcome + Username inline -->
            <div class="flex items-center gap-2 min-w-0">
                <span class="text-sm text-gray-500">Welcome</span>
                <span class="font-semibold text-base truncate max-w-[140px]">{{ userName }}</span>
            </div>
            <!-- Right: Logout -->
            <button
                @click="logout"
                class="text-xs px-2 py-1 rounded-md text-gray-500 hover:text-black hover:bg-hover transition">
                Logout
            </button>
        </div>
    </div>

    <!-- CONTROLS SECTION (Fixed) -->
    <div class="p-3 pb-2 shrink-0 space-y-2.5">
        <button
             @click="newChat"
            :disabled="chat.loading"
            class="w-full bg-user rounded-xl p-3 disabled:opacity-50 text-white font-semibold transition hover:opacity-90">
            + New Chat
        </button>

        <!-- SEARCH -->
        <input
            v-model="searchText"
            placeholder="Search chats..."
            class="w-full rounded-xl px-3 py-2 outline-none bg-panel border border-soft" />

        <div
            @click="loadBookmarks"
            class="cursor-pointer px-3 py-2 rounded-lg bg-panel hover:bg-hover transition border border-soft flex items-center">
            📌 Bookmarks
        </div>
    </div>

    <!-- SCROLLABLE CHAT SESSIONS -->
    <div class="flex-1 overflow-y-auto px-3 pb-3 min-h-0 space-y-1">
        <div
            v-if="chat.loading"
            class="text-sm text-gray-500 p-2">
            Loading...
        </div>

        <!-- REGULAR CHATS (When not searching) -->
        <div v-if="!searchText" class="space-y-1">
            <div
                v-for="item in filteredSessions"
                :key="item.id"
                @click="openSession(item.id)"
                class="p-2.5 rounded-xl cursor-pointer border transition"
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
        </div>

        <!-- SEARCH RESULTS (When searching) -->
        <div v-else class="space-y-3">
            <div
                v-for="s in groupedResults"
                :key="s.sessionId"
                class="mb-3">

                <!-- SESSION HEADER -->
                <div class="font-semibold text-xs px-2 py-1 text-gray-500 uppercase tracking-wider">
                    {{ s.title }}
                </div>

                <!-- QUESTIONS -->
                <div class="space-y-0.5 mt-1">
                    <div
                        v-for="m in s.messages"
                        :key="m.MessageId"
                        @click="openSearchResult(s.sessionId, m.MessageId)"
                        class="text-sm px-3 py-2 cursor-pointer rounded-lg bg-panel hover:bg-hover border border-soft/30 transition truncate">
                        {{ m.MessageText }}
                    </div>
                </div>

            </div>
            <div
                v-if="groupedResults.length === 0 && !chat.loading"
                class="text-sm text-gray-400 p-2">
                No matching messages found.
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

    chat.viewMode = 'chat';
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

    auth.logout();

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
