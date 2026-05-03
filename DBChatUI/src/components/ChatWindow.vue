<!-- ============================================= -->
<!-- src/components/ChatWindow.vue
UPDATED:
1. Disable textbox + Send while loading
2. Placeholder changes while loading
3. Auto-scroll to latest message
-->
<template>
<!--<div class="flex-1 h-screen flex flex-col">-->
    
<div class="h-full flex flex-col bg-white">

    <!-- HEADER -->
    <div class="p-4 flex items-center justify-between">
        <div class="font-bold text-xl">
            InsightChat
        </div>

        <button
            @click="newChat"
            :disabled="loading"
            class="bg-black text-white px-4 py-2 rounded-xl text-sm disabled:opacity-50">
            + New Chat
        </button>
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

        <div
            v-for="msg in chat.messages"
            :key="msg.id">

            <!-- USER -->
            <div
                v-if="isUser(msg.role)"
                class="flex justify-end">

                <div class="bg-black text-white px-4 py-3 rounded-2xl max-w-[85%] md:max-w-[70%] lg:max-w-[60%]">
                    <div>{{ msg.messageText }}</div>
                    <div class="text-xs opacity-70 mt-1">
                        {{ msg.createdOn }}
                    </div>
                </div>
            </div>
            <!-- AI -->
            <div
                v-else
                class="flex justify-start">

                <div class="bg-gray-100 px-4 py-3 rounded-2xl max-w-[90%] md:max-w-[75%] lg:max-w-[75%] whitespace-pre-wrap break-words">

                    <div>{{ msg.messageText }}</div>

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

                    </div>
                </div>

            </div>
        </div>

        <!-- anchor -->
        <div ref="bottomRef"></div>

    </div>

    <!-- INPUT 
    <div class="border-t p-4 flex gap-3">

        <input
            ref="questionInput"
            v-model="question"
            @keyup.enter="sendQuestion"
            :disabled="loading"
            :placeholder="loading
                ? 'Please wait. Fetching the answer...'
                : 'Ask anything'"
            class="flex-1 border rounded-2xl px-4 py-3 outline-none disabled:bg-gray-100 disabled:text-gray-500" />

        <button
            @click="sendQuestion"
            :disabled="loading"
            class="bg-black text-white px-5 rounded-2xl disabled:opacity-50">

            {{ loading ? "..." : "Send" }}

        </button>

    </div> -->
    <div
        class="shrink-0 border-t bg-white p-3 pb-[max(12px,env(safe-area-inset-bottom))]">

        <div class="flex gap-2">

            <input
                ref="questionInput"
                v-model="question"
                @keyup.enter="sendQuestion"
                :disabled="loading"
                :placeholder="loading
                    ? 'Please wait. Fetching the answer...'
                    : 'Ask anything'"
                class="flex-1 border rounded-2xl px-4 py-3 outline-none disabled:bg-gray-100" />

            <button
                @click="sendQuestion"
                :disabled="loading"
                class="px-4 rounded-2xl bg-black text-white disabled:opacity-50">

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
        class="bg-white rounded-2xl shadow-xl w-full max-w-5xl max-h-[88vh] flex flex-col">

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
                class="px-4 py-2 rounded-xl bg-black text-white">
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
        class="bg-white rounded-2xl shadow-xl w-full max-w-2xl max-h-[90vh] flex flex-col">

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
                    class="px-4 py-2 rounded-xl bg-black text-white">
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

import { ref, nextTick, watch } from "vue";
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
                headers: {
                    "Content-Type": "application/json",
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


const emailModalVisible = ref(false);

const emailFrom = ref("");
const emailTo = ref("");
const emailCc = ref("");

const emailSubject = ref("");
const emailBody = ref("");

const emailMessageId = ref(0);
const emailSending = ref(false);
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
    let question = "InsightChat Result";

    const index =
        chat.messages.findIndex(
            (x: any) => x.id === msg.id
        );

    if (index > 0) {

        for (let i = index - 1; i >= 0; i--) {

            if (
                (chat.messages[i].role || "")
                    .toLowerCase() === "user"
            ) {
                question =
                    chat.messages[i].messageText;
                break;
            }
        }
    }

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
        
        var result = await fetch(`${BASE_URL}/emailresult`, {
            method: "POST",
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
</script>