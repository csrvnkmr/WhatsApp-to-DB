<!-- ============================================= -->
<!-- src/components/ChatWindow.vue
UPDATED:
1. Disable textbox + Send while loading
2. Placeholder changes while loading
3. Auto-scroll to latest message
-->
<template>
<!--<div class="flex-1 h-screen flex flex-col">-->

<div class="h-full flex flex-col bg-base">

    <!-- HEADER -->
    <div class="p-4 flex items-center justify-between border-b border-soft bg-base">

        <!-- LEFT: Title + DB Description -->
        <div class="flex flex-col">
            <div class="font-bold text-xl">
                Insight Chat
            </div>

            <div class="text-xs opacity-70">
                {{ activeDbDescription }}
            </div>
        </div>

        <!-- RIGHT SIDE -->
        <div class="flex items-center gap-3">

            <!-- Theme Toggle -->
            <div class="flex items-center gap-1 bg-panel rounded-xl p-1">

                <button
                    @click="theme.currentTheme = 'light'"
                    :class="themeBtnClass('light')">
                    ☀️
                </button>

                <button
                    @click="theme.currentTheme = 'dark'"
                    :class="themeBtnClass('dark')">
                    🌙
                </button>

                <button
                    @click="theme.currentTheme = 'corporate'"
                    :class="themeBtnClass('corporate')">
                    🏢
                </button>

            </div>

            <!-- DATABASE FILTER -->
            <div ref="dbFilterRef" class="relative">

                <button
                    @click="showDbFilter = !showDbFilter"
                    class="flex items-center gap-2 px-3 py-2 rounded-xl bg-panel border border-soft hover:bg-hover transition text-sm min-w-[160px] max-w-[220px]">

                      <span class="truncate">

                          {{ allSelected
                              ? 'All Databases'
                              : `${chat.selectedDatabases.length} Selected` }}

                      </span>

                      <span class="text-xs opacity-70">
                          ▼
                      </span>

                  </button>

                  <!-- DROPDOWN -->
                  <div
                      v-if="showDbFilter"
                      class="absolute right-0 mt-2 w-72 max-w-[90vw] bg-panel border border-soft rounded-2xl shadow-xl z-50 overflow-hidden">

                      <!-- ALL -->
                      <div
                          @click="toggleAllDatabases"
                          class="flex items-center justify-between px-4 py-3 cursor-pointer hover:bg-hover border-b border-soft transition">

                          <div class="font-medium text-sm">
                              All
                          </div>

                          <div
                              class="w-5 h-5 rounded border border-soft flex items-center justify-center text-xs"
                              :class="allSelected
                                  ? 'bg-user text-white'
                                  : 'bg-base'">

                              <span v-if="allSelected">
                                  ✓
                              </span>

                          </div>

                      </div>

                      <!-- DATABASES -->
                      <div
                          v-for="db in databases"
                          :key="db.name"
                          @click="toggleDatabase(db.name)"
                          class="flex items-center justify-between px-4 py-3 cursor-pointer hover:bg-hover transition">

                          <div class="flex flex-col min-w-0">

                              <div class="text-sm font-medium truncate">
                                  {{ db.name }}
                              </div>

                              <div class="text-xs opacity-70 truncate">
                                  {{ db.description }}
                              </div>

                          </div>

                          <div
                              class="w-5 h-5 rounded border border-soft flex items-center justify-center text-xs ml-3 shrink-0"
                              :class="chat.selectedDatabases.includes(db.name)
                                  ? 'bg-user text-white'
                                  : 'bg-base'">

                              <span v-if="chat.selectedDatabases.includes(db.name)">
                                  ✓
                              </span>

                          </div>
                      </div>

                    </div>

              </div>

            <div class="relative">

              <button
                  @click="showLlmMenu = !showLlmMenu"
                  class="px-3 py-2 rounded-xl bg-panel hover:bg-hover text-sm">

                  {{ activeModel }}
                  <span>▾</span>
              </button>

              <div
                  v-if="showLlmMenu"
                  class="absolute right-0 mt-2 w-72 bg-panel border border-soft rounded-xl shadow-lg z-50">

                  <div
                      v-for="llm in llms"
                      :key="llm.provider"
                      class="p-2 border-b border-soft">

                      <div class="font-semibold text-sm mb-2">
                          {{ llm.provider }}
                      </div>

                      <div
                          v-for="model in llm.models"
                          :key="model"
                          @click="selectLlm(llm.provider, model)"
                          class="px-2 py-1 rounded cursor-pointer hover:bg-hover text-sm">

                          {{ model }}

                      </div>

                  </div>

              </div>
          </div>

            <!-- DATABASE DROPDOWN -->
            <div class="relative">

                <!-- Selected DB -->
                <button
                    @click="toggleDbMenu"
                    class="px-3 py-2 rounded-xl bg-panel hover:bg-hover text-sm flex items-center gap-2">

                    <span>{{ activeDbName }}</span>
                    <span>▾</span>
                </button>

                <!-- Dropdown -->
                <div
                    v-if="showDbMenu"
                    class="absolute right-0 mt-2 w-56 bg-panel border border-soft rounded-xl shadow-lg z-50">

                    <div
                        v-for="db in databases"
                        :key="db.name"
                        @click="selectDb(db)"
                        class="px-3 py-2 cursor-pointer hover:bg-hover text-sm">

                        <div class="font-medium">
                            {{ db.name }}
                        </div>

                        <div class="text-xs opacity-70">
                            {{ db.description }}
                        </div>

                    </div>

                </div>
            </div>

        </div>
    </div>

    <!-- CHAT BODY -->
    <div
        ref="chatBody"
        class="flex-1 overflow-auto p-6 space-y-4">

        <div
            v-if="chat.messages.length === 0"
            class="text-gray-400">
            Start a new conversation or select chat history.
        </div>

        <div  :id="`msg-${msg.id}`"
            v-for="msg in chat.messages"
            :key="`${msg.role}-${msg.id}`">

            <!-- USER -->
           <div
              v-if="isUser(msg.role)"
              class="flex justify-end">

              <!-- COLUMN WRAPPER -->
              <div class="flex flex-col items-end max-w-[85%] md:max-w-[70%] lg:max-w-[60%]">

                  <!-- MESSAGE BUBBLE -->
                  <div class="bg-user px-4 py-3 rounded-2xl w-full">

                      <!-- BOOKMARK MODE -->
                      <div
                          v-if="msg.isBookmarkView"
                          @click="startFromBookmark(msg.messageText)"
                          class="cursor-pointer hover:opacity-90 transition">

                          <span class="hover:underline">
                              {{ msg.messageText }}
                          </span>

                          <span class="ml-2 text-xs opacity-70">
                              ↺
                          </span>

                      </div>

                      <!-- NORMAL CHAT -->
                      <div v-else>
                          {{ msg.messageText }}
                      </div>

                  </div>

                  <!-- TIMESTAMP (ALWAYS BELOW) -->
                  <div class="text-xs opacity-70 mt-1">
                      {{ msg.createdOn }}
                  </div>

              </div>

          </div>
            <!-- AI -->
            <div
                v-else
                class="flex justify-start">

                <div class="bg-panel px-4 py-3 rounded-2xl max-w-[90%] md:max-w-[75%] lg:max-w-[75%] whitespace-pre-wrap break-words">
                    {{ msg.messageText }}


                    <!-- Footer -->
                    <div class="mt-2 flex flex-wrap items-center text-xs text-gray-500 gap-y-1">

                        <!-- Timestamp -->
                        <span>{{ msg.createdOn }}</span>

                        <!-- SQL -->
                        <template v-if="msg.canShowSql">
                            <span class="mx-2 text-gray-300">·</span>

                            <a
                                href="#"
                                @click.prevent="showSql(msg)"
                                class="text-blue-600 hover:text-blue-700 hover:underline transition-colors">
                                SQL
                            </a>
                        </template>

                        <!-- DATA -->
                        <template v-if="msg.canShowData">
                            <span class="mx-2 text-gray-300">·</span>

                            <a
                                href="#"
                                @click.prevent="showData(msg)"
                                class="text-blue-600 hover:text-blue-700 hover:underline transition-colors">
                                Data
                            </a>
                            <span class="mx-2 text-gray-300">·</span>

                            <a
                                href="#"
                                @click.prevent="exportExcel(msg)"
                                class="text-blue-600 hover:text-blue-700 hover:underline transition-colors">
                                Excel
                            </a>
                        </template>

                        <!-- CHART -->
                        <template v-if="msg.canShowChart">
                            <span class="mx-2 text-gray-300">·</span>

                            <a
                                href="#"
                                @click.prevent="showChart(msg)"
                                class="text-blue-600 hover:text-blue-700 hover:underline transition-colors">
                                Chart
                            </a>
                        </template>

                        <!-- EMAIL -->
                        <span class="mx-2 text-gray-300">·</span>

                        <a
                            href="#"
                            @click.prevent="emailResult(msg)"
                            class="text-blue-600 hover:text-blue-700 hover:underline transition-colors">
                            Email
                        </a>

                        <span class="mx-2 text-gray-300">·</span>



                            <!-- Bookmark icon -->
                            <a
                                v-if="!msg.isBookmarked"
                                href="#"
                                @click.prevent="openBookmarkModal(msg)"
                                class="text-blue-600 hover:text-blue-700 hover:underline transition-colors">
                                Bookmark
                            </a>

                            <a
                                v-if="msg.isBookmarked"
                                href="#"
                                @click.prevent="removeBookmark(msg)"
                                class="text-red-600 hover:text-red-700 hover:underline transition-colors">
                                Remove Bookmark
                            </a>

                        <div v-if="showBookmarkModal"
                          class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">

                          <div class="bg-base w-full max-w-md rounded-2xl shadow-xl p-5">

                              <!-- Title -->
                              <div class="text-lg font-semibold mb-3">
                                  Save Bookmark
                              </div>

                              <!-- Input -->
                              <input
                                  v-model="bookmarkText"
                                  placeholder="Enter bookmark name"
                                  class="w-full border rounded-xl px-3 py-2 outline-none focus:ring-2 focus:ring-black mb-4" />

                              <!-- Buttons -->
                              <div class="flex justify-end gap-2">

                                  <button
                                      @click="showBookmarkModal = false"
                                      class="px-4 py-2 rounded-xl border text-gray-600 hover:bg-panel">
                                      Cancel
                                  </button>

                                  <button
                                      @click="saveBookmark"
                                      class="px-4 py-2 rounded-xl bg-user hover:opacity-90">
                                      Save
                                  </button>
                              </div>
                          </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- anchor -->
        <div ref="bottomRef"></div>

    </div>
<div class="shrink-0 border-t border-soft bg-base p-3 pb-[max(12px,env(safe-area-inset-bottom))]">

    <div class="flex gap-2">

        <!-- INPUT -->
        <input v-if="chat.viewMode === 'chat'"
            ref="questionInput"
            v-model="question"
            @keyup.enter="sendQuestion"
            :disabled="loading"
            :placeholder="loading
                ? 'Please wait. Fetching the answer...'
                : 'Ask anything'"
            class="flex-1 border-soft rounded-2xl px-4 py-3 outline-none bg-panel focus:ring-1 focus:ring-[var(--border)] disabled:bg-panel transition" />

        <!-- BUTTON -->
        <button v-if="chat.viewMode === 'chat'"
            @click="sendQuestion"
            :disabled="loading"
            class="px-4 py-3 rounded-2xl bg-user text-white disabled:opacity-50 transition whitespace-nowrap">

            {{ loading ? "..." : "Send" }}

        </button>

    </div>

</div>



</div>
<!-- =============================================
REPLACE EXISTING MODAL WITH THIS VERSION
Toolbar removed from top
Actions moved to bottom as premium links
============================================= -->

<div
    v-if="modalVisible"
    class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">

    <div
        class="bg-base rounded-2xl shadow-xl w-full max-w-5xl max-h-[88vh] flex flex-col">

        <!-- HEADER -->
        <div
            class="p-4 border-b flex items-center justify-between">

            <div class="font-semibold text-lg">
                {{ modalTitle }}
            </div>

            <button
                @click="modalVisible = false"
                class="text-gray-500 hover:text-black text-xl">
                ×
            </button>

        </div>

        <!-- BODY -->
        <div
            class="p-4 overflow-auto flex-1">

            <pre
                class="text-sm whitespace-pre-wrap break-words font-mono">{{ modalContent }}</pre>

        </div>

        <!-- FOOTER -->
        <div
            class="p-4 border-t flex flex-wrap items-center justify-between gap-y-2">

            <!-- LEFT LINKS -->
            <div class="flex flex-wrap items-center text-sm text-gray-500 gap-y-1">

                <!-- COPY -->
                <a
                    href="#"
                    @click.prevent="copyContent"
                    class="text-blue-600 hover:text-blue-700 hover:underline">
                    Copy
                </a>

                <!-- separator -->
                <span class="mx-2 text-gray-300">·</span>

                <!-- DOWNLOAD -->
                <a
                    href="#"
                    @click.prevent="downloadContent"
                    class="text-blue-600 hover:text-blue-700 hover:underline">
                    Download
                </a>

                <!-- EXCEL -->
                <template v-if="modalType === 'data'">
                    <span class="mx-2 text-gray-300">·</span>

                    <a
                        href="#"
                        @click.prevent="downloadExcel"
                        class="text-blue-600 hover:text-blue-700 hover:underline">
                        Download Excel
                    </a>
                </template>

            </div>

            <!-- RIGHT BUTTON -->
            <button
                @click="modalVisible = false"
                class="px-4 py-2 rounded-xl bg-user">
                Close
            </button>

        </div>

    </div>

</div>

<!-- =============================================
EMAIL MODAL
Place below existing SQL/Data modal
============================================= -->

<div
    v-if="emailModalVisible"
    class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">

    <div
        class="bg-base rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] flex flex-col">

        <!-- HEADER -->
        <div
            class="p-4 border-b flex items-center justify-between">

            <div class="font-semibold text-lg">
                Email Result
            </div>

            <button
                @click="emailModalVisible = false"
                class="text-gray-500 hover:text-black text-xl">
                ×
            </button>

        </div>

        <!-- BODY -->
        <div class="p-4 overflow-auto flex-1 space-y-4">

            <!-- FROM -->
            <div>
                <label class="block text-sm text-gray-600 mb-1">
                    From
                </label>

                <input
                    v-model="emailFrom"
                    type="email"
                    class="w-full border rounded-xl px-4 py-3 outline-none"
                    placeholder="from@company.com" />
            </div>

            <!-- TO -->
            <div>
                <label class="block text-sm text-gray-600 mb-1">
                    To
                </label>

                <input
                    v-model="emailTo"
                    type="text"
                    class="w-full border rounded-xl px-4 py-3 outline-none"
                    placeholder="to@company.com" />
            </div>

            <!-- CC -->
            <div>
                <label class="block text-sm text-gray-600 mb-1">
                    CC
                </label>

                <input
                    v-model="emailCc"
                    type="text"
                    class="w-full border rounded-xl px-4 py-3 outline-none"
                    placeholder="cc@company.com" />
            </div>

            <!-- SUBJECT -->
            <div>
                <label class="block text-sm text-gray-600 mb-1">
                    Subject
                </label>

                <input
                    v-model="emailSubject"
                    type="text"
                    class="w-full border rounded-xl px-4 py-3 outline-none" />
            </div>

            <!-- BODY -->
            <div>
                <label class="block text-sm text-gray-600 mb-1">
                    Body
                </label>

                <textarea
                    v-model="emailBody"
                    rows="12"
                    class="w-full border rounded-xl px-4 py-3 outline-none resize-none"></textarea>
            </div>

        </div>

        <!-- FOOTER -->
        <div
            class="p-4 border-t flex items-center justify-between">

            <div class="text-sm text-gray-500">
                Send current response by email
            </div>

            <div class="flex gap-2">

                <button
                    @click="emailModalVisible = false"
                    class="px-4 py-2 rounded-xl border">
                    Cancel
                </button>

                <button
                    @click="sendEmail"
                    class="px-4 py-2 rounded-xl bg-user">
                    <span v-if="!emailSending">
                        Send
                    </span>

                    <span
                        v-else
                        class="flex items-center justify-center gap-2">

                        <!-- Spinner -->
                        <svg
                            class="animate-spin h-4 w-4"
                            viewBox="0 0 24 24"
                            fill="none">

                            <circle
                                class="opacity-25"
                                cx="12"
                                cy="12"
                                r="10"
                                stroke="currentColor"
                                stroke-width="4">
                            </circle>

                            <path
                                class="opacity-75"
                                fill="currentColor"
                                d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z">
                            </path>

                        </svg>

                        Sending...

                    </span>

                </button>

            </div>

        </div>

    </div>

</div>

</template>

<script setup lang="ts">

import { ref, nextTick, watch, onMounted, computed, onBeforeUnmount } from "vue";
import { useChatStore } from "@/stores/chat";

// ------------------------------------------
// MODAL STATE
// ------------------------------------------
const modalVisible = ref(false);
const modalTitle = ref("");
const modalContent = ref("");

const modalType = ref(""); // sql / data

const chat = useChatStore();

const question = ref("");
const loading = ref(false);

const chatBody = ref<HTMLElement | null>(null);
const bottomRef = ref<HTMLElement | null>(null);
const questionInput = ref<HTMLInputElement | null>(null);

const BASE_URL = "http://localhost:3000";

const bookmarkText = ref("")
const bookmarkMessageId = ref<number | null>(null)
const showBookmarkModal = ref(false)

import { useThemeStore } from '@/stores/theme'

const theme = useThemeStore()

function openBookmarkModal(msg:any) {
  console.log("Opening bookmark modal")
    bookmarkMessageId.value = msg.id
    bookmarkText.value = getQuestion(msg) // msg.messageText   // default question
    showBookmarkModal.value = true
}

async function saveBookmark() {
    await fetch(
        `${BASE_URL}/addbookmark/${bookmarkMessageId.value}?text=${encodeURIComponent(bookmarkText.value)}`,
        {
            credentials: 'include',
            headers: authHeader()
        }
    )
    chat.messages.find((m:any)=>m.id===bookmarkMessageId.value).isBookmarked=true;
    showBookmarkModal.value = false
}

async function removeBookmark(msg:any) {
    await fetch(
        `${BASE_URL}/removebookmark/${msg.id}`,
        {
          credentials: 'include',
          headers: authHeader()
        }
    )
    msg.isBookmarked=false
}

// ==========================================
// Auto Scroll
// ==========================================
async function scrollToBottom() {
    await nextTick();

    bottomRef.value?.scrollIntoView({
        behavior: "smooth",
        block: "end"
    });
}

// Scroll to bottom when the message length change

watch(
    () => chat.messages.length,
    async () => {
        await nextTick();
        await scrollToBottom();
        questionInput.value?.focus();
    }
);

// ==========================================
// New Chat
// ==========================================
function newChat() {
    if (loading.value) return;

    chat.messages = [];
    chat.selectedSessionId = null;
}
const wasNewSession = !chat.selectedSessionId;
// ==========================================
// Send Question
// ==========================================
async function sendQuestion() {

    if (!question.value.trim() || loading.value)
        return;

    const userQuestion = question.value;
    const now = new Date().toLocaleString();

    // Add user msg
    chat.messages.push({
        id: Date.now(),
        role: "user",
        messageText: userQuestion,
        createdOn: now,
        SessionId: chat.selectedSessionId,
        canShowChart: false,
        canShowData : false,
        canShowSql : false
    });

    question.value = "";
    loading.value = true;

    await scrollToBottom();

    try {

        const token =
            localStorage.getItem("token") || "";

        const res = await fetch(
            `${BASE_URL}/ask`,
            {
                method: "POST",
                credentials: 'include',
                headers: {
                    "Content-Type": "application/json",
                    credentials: 'include',
                    "Authorization": `Bearer ${token}`
                },
                body: JSON.stringify({
                    SessionId: chat.selectedSessionId,
                    Question: userQuestion
                })
            });

        const raw = await res.text();

        let text = raw;
        let msgid = Date.now() + 1;
        try {
            const json = JSON.parse(raw);
            console.log ("Received data from Agent", json);
            try {
                text =
                    json.response ||
                    json.answer ||
                    json.message ||
                    json.messageText ||
                    raw;
            }
            catch {
                text = raw;
            }
            if (json.sessionId) {
                chat.selectedSessionId = json.sessionId;
            }
            if (json.id) {
                msgid=json.id;
            }
            if (wasNewSession || json?.sessionId) {
                await chat.loadSessions();
            }
            chat.messages.push({
                id: msgid,
                sessionId: chat.selectedSessionId,
                role: "assistant",
                messageText: text,
                createdOn: json.createdOn || new Date().toLocaleString(),

                canShowSql: json.canShowSql || false,
                canShowData: json.canShowData || false,
                canShowChart: json.canShowChart || false
            });

        }
        catch {
            text = raw;
        }

        await scrollToBottom();
    }
    catch (ex) {
        console.log("Exception in SendQuestion", ex)
        chat.messages.push({
            id: Date.now() + 2,
            sessionId: chat.selectedSessionId,
            role: "assistant",
            messageText: "Error connecting to server.",
            createdOn: new Date().toLocaleString()
        });

        await scrollToBottom();
    }
    finally {
        loading.value = false;
        questionInput.value?.focus();
    }
}

function isUser(role: string) {
    const r = (role || "").toLowerCase().trim();

    return r === "user";
}

function authHeader() {
    const token =
        localStorage.getItem("token") || "";

    return {
        "Authorization": `Bearer ${token}`
    };
}

// ------------------------------------------
// SHOW SQL
// ------------------------------------------
async function showSql(msg: any) {

    modalType.value = "sql";

    try {
        modalTitle.value = "SQL";
        modalContent.value = "Loading...";
        modalVisible.value = true;

        const res = await fetch(
            `${BASE_URL}/messagesql/${msg.id}`,
            {
              credentials: 'include',
                headers: authHeader()
            });

        const json = await res.json();

        modalContent.value =
            json.sql || "";
    }
    catch {
        modalContent.value =
            "Unable to load SQL.";
    }
}


// ------------------------------------------
// SHOW DATA
// ------------------------------------------
async function showData(msg: any) {

    modalType.value = "data";

    try {
        modalTitle.value = "Data";
        modalContent.value = "Loading...";
        modalVisible.value = true;

        const res = await fetch(
            `${BASE_URL}/messagedata/${msg.id}`,

            {
              credentials: 'include',
                headers: authHeader()
            });

        const json = await res.json();

        modalContent.value =
            JSON.stringify(json, null, 2);
    }
    catch {
        modalContent.value =
            "Unable to load Data.";
    }
}

// ------------------------------------------
// COPY
// ------------------------------------------
async function copyContent() {

    await navigator.clipboard.writeText(
        modalContent.value
    );
}

// ------------------------------------------
// DOWNLOAD TXT / JSON / SQL
// ------------------------------------------
function downloadContent() {

    let ext = "txt";

    if (modalType.value === "sql")
        ext = "sql";

    if (modalType.value === "data")
        ext = "json";

    const blob = new Blob(
        [modalContent.value],
        { type: "text/plain" }
    );

    const url =
        URL.createObjectURL(blob);

    const a =
        document.createElement("a");

    a.href = url;
    a.download =
        `${modalType.value}.${ext}`;

    a.click();

    URL.revokeObjectURL(url);
}

// ------------------------------------------
// DOWNLOAD EXCEL (CSV)
// ------------------------------------------
function downloadExcel() {

    try {

        const rows =
            JSON.parse(modalContent.value);

        if (!rows || !rows.length)
            return;

        const headers =
            Object.keys(rows[0]);

        const csv = [
            headers.join(","),
            ...rows.map((row: any) =>
                headers.map(h =>
                    `"${String(row[h] ?? "").replace(/"/g, '""')}"`
                ).join(","))
        ].join("\n");

        const blob = new Blob(
            [csv],
            { type: "text/csv;charset=utf-8;" }
        );

        const url =
            URL.createObjectURL(blob);

        const a =
            document.createElement("a");

        a.href = url;
        a.download = "data.csv";
        a.click();

        URL.revokeObjectURL(url);
    }
    catch {
        alert("Unable to export Excel.");
    }
}

function showChart(msg: any) {
    alert(`Load Chart for Message Id: ${msg.id}`);
}

const themeBtnClass = (name: string) => {
    return [
        "px-3 py-1 rounded-lg text-sm transition",
        theme.currentTheme === name
            ? "bg-white shadow text-black"
            : "text-gray-600 hover:bg-white/50"
    ]
}

const emailModalVisible = ref(false);

const emailFrom = ref("");
const emailTo = ref("");
const emailCc = ref("");

const emailSubject = ref("");
const emailBody = ref("");

const emailMessageId = ref(0);
const emailSending = ref(false);

function getQuestion(msg: any):string {
    const index =
        chat.messages.findIndex(
            (x: any) => x.id === msg.id
        );

    if (index > 0) {

        for (let i = index - 1; i >= 0; i--) {

            if (
                chat.messages[i].role.toLowerCase() === "user"
            ) {
                return chat.messages[i].messageText;
            }
        }
    }
    return "InsightChat Result";
}

// ------------------------------------------
// Open Email Modal
// msg = assistant message clicked
// ------------------------------------------
function emailResult(msg: any) {


    emailMessageId.value = msg.id;

    // default from
    emailFrom.value =
        localStorage.getItem("username") || "";

    emailTo.value = "";
    emailCc.value = "";

    // Find previous user question in chat
    const question = getQuestion(msg); //"InsightChat Result";

    emailSubject.value = question;

    // default body = current answer
    emailBody.value = msg.messageText || "";

    emailModalVisible.value = true;
}

// ------------------------------------------
// Send Email
// Connect backend later
// ------------------------------------------
async function sendEmail() {


    if (!emailFrom.value || !emailTo.value) {
        alert("Please enter From and To.");
        return;
    }

    if (emailSending.value)
        return;
    try {
        emailSending.value = true;

        const result = await fetch(`${BASE_URL}/emailresult`, {
            method: "POST",
            credentials: 'include',
            headers: {
                "Content-Type":"application/json",
                ...authHeader()
            },
            body: JSON.stringify({
                messageId: emailMessageId.value,
                from: emailFrom.value,
                to: emailTo.value,
                cc: emailCc.value,
                subject: emailSubject.value,
                body: emailBody.value
            })
        });

        console.log(result);
        alert("Email sent successfully.");

        emailModalVisible.value = false;
    }

    catch (err: any) {

        alert(
            err?.message ||
            "Unable to send email."
        );
    }
    finally {
        emailSending.value = false;
    }
}

function startFromBookmark(questionText: string) {

    chat.viewMode = 'chat'
    chat.messages = []
    chat.selectedSessionId = null

    question.value = questionText

    nextTick(() => {
        questionInput.value?.focus()
    })
}


async function exportExcel(msg: any) {

    const token =
        localStorage.getItem("token") || "";
console.log("Exporting Excel for Message Id: ", msg.id)
    const res =
        await fetch(
            `${BASE_URL}/exportdata/${msg.id}`,
            {
              credentials: 'include',
              headers: {
                "Authorization": `Bearer ${token}`
              }
            });

    if (!res.ok) {
        alert("Unable to export Excel");
        return;
    }

    const blob =
        await res.blob();

    const url =
        window.URL.createObjectURL(blob);

    const a =
        document.createElement("a");

    a.href = url;
    a.download = `export_${msg.id}.xlsx`;

    a.click();

    window.URL.revokeObjectURL(url);
}

const databases = ref<any[]>([])
const activeDbName = ref('')
const activeDbDescription = ref('')
const showDbMenu = ref(false)

onMounted(() => {
    loadDatabases()
    loadLlms()
})

function toggleDbMenu() {
    showDbMenu.value = !showDbMenu.value
}

async function loadDatabases() {

    const res = await fetch(
        `${BASE_URL}/databases`,
        {
            credentials: 'include',
            headers: authHeader()
        });

    const json = await res.json();

    databases.value = json.databases;
    activeDbName.value = json.activeDb;
    activeDbDescription.value = json.activeDbDescription;

    if (databases.value.length > 0 && !json.activeDb) {

        const current = databases.value[0];

        activeDbName.value = current.name;
        activeDbDescription.value = current.description;
    }

    chat.setSelectedDatabases( databases.value.map((x: any) => x.name))
}

document.addEventListener('click', (e) => {
    if (!(e.target as HTMLElement).closest('.relative')) {
        showDbMenu.value = false
    }
})

async function selectDb(db: any) {

    const res = await fetch(
        `${BASE_URL}/databases/select`,
        {
            method: 'POST',
            credentials: 'include',
            headers: {
                ...authHeader(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(db.name)
        });

    if (!res.ok) {
        alert('Failed to change database');
        return;
    }

    activeDbName.value = db.name;
    activeDbDescription.value = db.description;

    showDbMenu.value = false;

    // Optional cleanup
    chat.messages = [];
    chat.selectedSessionId = null;
}

const llms = ref<any[]>([])

const activeProvider = ref('')
const activeModel = ref('')

const showLlmMenu = ref(false)

async function loadLlms() {

    const res = await fetch(
        `${BASE_URL}/llms`,
        {
            credentials: 'include',
            headers: authHeader()
        });

    const json = await res.json()

    llms.value = json.providers

    activeProvider.value = json.selectedProvider

    activeModel.value = json.selectedModel

}

async function selectLlm(provider: string, model: string) {
    console.log("Llm changed to", provider, model)
    const body:string = JSON.stringify({
                provider,
                model
            })
    await fetch(
        `${BASE_URL}/llms/select`,
        {
            method: 'POST',
            credentials: 'include',
            headers: {
                ...authHeader(),
                'Content-Type': 'application/json'
            },
            body: body
        });

    activeProvider.value = provider;
    activeModel.value = model;

    showLlmMenu.value = false;
}

const showDbFilter = ref(false)

//const selectedDatabases = ref<string[]>([])

const allSelected = computed(() => {
    return chat.selectedDatabases.length === databases.value.length
})

function toggleAllDatabases() {

    if (allSelected.value) {

        chat.setSelectedDatabases([activeDbName.value])
    }
    else {

        chat.setSelectedDatabases(databases.value.map((x: any) => x.name))
    }
}

function toggleDatabase(dbName: string) {

    // Current active DB cannot be unchecked
    if (dbName === activeDbName.value)
        return

    if (chat.selectedDatabases.includes(dbName)) {
        chat.selectedDatabases = chat.selectedDatabases
            .filter(x => x !== dbName)
    }
    else {

        chat.selectedDatabases.push(dbName)
    }
}

async function applyDatabaseFilter() {

    console.log("Applying DB filter", chat.selectedDatabases)

    // -----------------------------
    // FILTER SESSIONS
    // -----------------------------
    const sessionRes = await fetch(
        `${BASE_URL}/sessions/filter`,
        {
            method: 'POST',
            credentials: 'include',
            headers: {
                ...authHeader(),
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                databases: chat.selectedDatabases
            })
        })

    const sessions =
        await sessionRes.json()

    chat.sessions = sessions

    const exists = sessions.some( (x:any) => x.id === chat.selectedSessionId)

    if (!exists) {

        chat.selectedSessionId = null
        chat.messages = []
    }
    // -----------------------------
    // FILTER CURRENT SESSION MSGS
    // -----------------------------
    if (chat.selectedSessionId) {

        const msgRes = await fetch(
            `${BASE_URL}/message/filter/${chat.selectedSessionId}`,
            {
                method: 'POST',
                credentials: 'include',
                headers: {
                    ...authHeader(),
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    databases: chat.selectedDatabases
                })
            })

        const msgs =
            await msgRes.json()

        chat.messages = msgs
    }
}

watch(
    () => chat.selectedDatabases,
    async () => {

        await applyDatabaseFilter()
    },
    {
        deep: true
    })

watch(activeDbName, (db) => {

    if (!db)
        return

    if (!chat.selectedDatabases.includes(db)) {

        chat.selectedDatabases.push(db)
    }
})

const dbFilterRef = ref()

onMounted(() => {

    document.addEventListener('click', handleOutsideClick)
})

onBeforeUnmount(() => {

    document.removeEventListener('click', handleOutsideClick)
})

function handleOutsideClick(e: any) {

    if (!dbFilterRef.value?.contains(e.target)) {

        showDbFilter.value = false
    }
}

</script>
