<!-- ============================================= -->
<!-- src/components/Sidebar.vue -->
<!-- ============================================= -->
<template>
<div class="w-full md:w-72 h-full overflow-auto bg-gray-100 flex flex-col">

    <!--<div class="p-4 border-b font-bold text-xl">-->
    <div class="p-4 font-bold text-xl">
        
        <div class="flex items-center justify-between">

            <div>
                <div class="text-sm text-gray-500">
                    Welcome
                </div>

                <div class="font-semibold text-lg truncate max-w-[180px]">
                    {{ userName }}
                </div>
            </div>

            <button
                @click="logout"
                class="text-sm px-3 py-2 hover:bg-white">
                Logout
            </button>

        </div>

    </div>

    <div class="p-3">

        <button
             @click="newChat"
            :disabled="chat.loading"
            class="w-full bg-black text-white rounded-xl p-3 mb-3 disabled:opacity-50">
            + New Chat
        </button>

        <!-- SEARCH -->
        <input
            v-model="searchText"
            placeholder="Search chats..."
            class="w-full mb-1 rounded-xl px-3 py-2 outline-none bg-white" />

        <div
            v-if="chat.loading"
            class="text-sm text-gray-500 p-2">
            Loading...
        </div>

        <div
            v-for="item in filteredSessions"
            :key="item.id"
            @click="openSession(item.id)"
            class="p-2 mb-1 rounded-xl cursor-pointer hover:bg-white border"
           :class="Number(chat.selectedSessionId) === Number(item.id)
                ? 'bg-blue-100 border-blue-500 shadow-md'
                : 'hover:bg-gray-100 border-gray-200'">

            <div class="font-medium text-sm truncate">
                {{ item.title }}
            </div>

            <div class="text-xs text-gray-500 mt-1">
                {{ item.updatedOn }}
            </div>
        </div>
        <div
            v-if="filteredSessions.length === 0 && !chat.loading"
            class="text-sm text-gray-400 p-2">
            No chats found.
        </div>
    </div>

</div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useChatStore } from "@/stores/chat";
import { useAuthStore } from "@/stores/auth";
const emit = defineEmits(["closeMobile"]);

const chat = useChatStore();
const auth = useAuthStore();

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

const searchText = ref("");
// ==========================================
// Filter Sessions
// ==========================================
const filteredSessions = computed(() => {

    const txt =
        searchText.value
            .toLowerCase()
            .trim();

    if (!txt)
        return chat.sessions;

    return chat.sessions.filter((x: any) =>
        (x.title || "")
            .toLowerCase()
            .includes(txt)
    );
});

// ==========================================
// SAME as ChatWindow New Chat
// ==========================================
function newChatold() {

    if (chat.loading)
        return;

    chat.messages = [];
    chat.selectedSessionId = null;
}

// ==========================================
// Open Session
// ==========================================
async function openSessionold(id: number) {

    if (chat.loading)
        return;

    await chat.loadMessages(id);
}

async function openSession(id: number) {

    if (chat.loading)
        return;

    await chat.loadMessages(id);

    // auto hide mobile menu
    emit("closeMobile");
}

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
function logout() {

    localStorage.removeItem("token");
    localStorage.removeItem("username");

    location.reload();
}
</script>