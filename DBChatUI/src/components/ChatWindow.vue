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
    <div class="p-4 flex flex-col lg:flex-row lg:items-center justify-between border-b border-soft bg-base gap-4">

        <!-- LEFT: Title + DB Description -->
        <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-gradient-to-tr from-blue-500 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-blue-500/20 text-lg shrink-0">
                💬
            </div>
            <div class="flex flex-col min-w-0">
                <h1 class="font-bold text-base flex items-center leading-tight">
                    <span class="bg-gradient-to-r from-blue-500 to-indigo-500 dark:from-blue-400 dark:to-indigo-400 bg-clip-text text-transparent">Insight Chat</span>
                </h1>
                <div class="text-[10px] opacity-70 truncate max-w-[200px] sm:max-w-[300px]">
                    {{ activeDbDescription }}
                </div>
            </div>
        </div>

        <!-- RIGHT SIDE -->
        <div class="flex flex-wrap items-center gap-2 sm:gap-3 w-full lg:w-auto">

            <!-- Admin Link -->
            <router-link
                to="/admin/databases"
                class="px-3 py-1.5 rounded-xl border border-soft hover:bg-hover transition text-sm font-medium bg-panel">
                ⚙️ Admin
            </router-link>

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
                      class="absolute left-0 sm:left-auto sm:right-0 mt-2 w-72 max-w-[90vw] bg-panel border border-soft rounded-2xl shadow-xl z-50 overflow-hidden">

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
                  class="absolute left-0 sm:left-auto sm:right-0 mt-2 w-72 max-w-[90vw] bg-panel border border-soft rounded-xl shadow-lg z-50">

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
                    class="absolute left-0 sm:left-auto sm:right-0 mt-2 w-56 max-w-[90vw] bg-panel border border-soft rounded-xl shadow-lg z-50">

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
                    {{ parseMessage(msg).text }}


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
                        <template v-if="msg.canShowChart || parseMessage(msg).canShowChart">
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
            @keydown.up.prevent="navigateHistory('up')"
            @keydown.down.prevent="navigateHistory('down')"
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
            class="p-0 overflow-auto flex-1 relative">

            <template v-if="modalType === 'data' && modalData && modalData.length">
                <table class="w-full text-sm text-left border-collapse">
                    <thead class="bg-panel border-b sticky top-0 shadow-sm z-10">
                        <tr>
                            <th v-for="key in Object.keys(modalData[0])" :key="key" class="p-3 font-semibold whitespace-nowrap">
                                {{ key }}
                            </th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr v-for="(row, idx) in modalData" :key="idx" class="border-b border-soft/50 hover:bg-hover">
                            <td v-for="key in Object.keys(modalData[0])" :key="key" class="p-3">
                                {{ row[key] }}
                            </td>
                        </tr>
                    </tbody>
                </table>
            </template>
            <pre v-else
                class="p-4 text-sm whitespace-pre-wrap break-words font-mono">{{ modalContent }}</pre>

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
    class="fixed inset-0 bg-black/40 z-[60] flex items-center justify-center p-4">

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

<!-- =============================================
CHART MODAL
============================================= -->

<div
    v-if="chartModalVisible"
    class="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">

    <div
        ref="chartModalRef"
        :class="['bg-base shadow-xl flex flex-col overflow-hidden transition-all duration-200', chartIsMaximized ? 'fixed inset-0 w-full h-full rounded-none' : 'w-full max-w-5xl max-h-[90vh] rounded-2xl']">

        <!-- HEADER -->
        <div
            class="p-4 border-b flex items-center justify-between">

            <div class="font-semibold text-lg">
                {{ chartConfig?.Title || 'Chart View' }}
            </div>

            <button
                @click="chartModalVisible = false"
                class="text-gray-500 hover:text-black text-xl">
                ×
            </button>

        </div>

        <!-- BODY -->
        <div class="p-4 overflow-auto flex-1 min-h-[50vh] bg-base">

            <AiChartView 
                ref="aiChartRef"
                :chartConfig="chartConfig" 
                :chartData="chartData" 
            />

        </div>

        <!-- FOOTER -->
        <div
            class="p-4 border-t flex flex-wrap items-center justify-between gap-y-2 bg-base">

            <!-- LEFT LINKS -->
            <div class="flex flex-wrap items-center text-sm text-gray-500 gap-y-1">
                <a
                    href="#"
                    @click.prevent="downloadChartImage"
                    class="text-blue-600 hover:text-blue-700 hover:underline">
                    Download Image
                </a>
                <span class="mx-2 text-gray-300">·</span>
                <a
                    href="#"
                    @click.prevent="emailChart"
                    class="text-blue-600 hover:text-blue-700 hover:underline">
                    Email
                </a>
                <span class="mx-2 text-gray-300">·</span>
                <a
                    href="#"
                    @click.prevent="toggleMaximizeChart"
                    class="text-blue-600 hover:text-blue-700 hover:underline">
                    {{ chartIsMaximized ? 'Restore' : 'Maximize' }}
                </a>
                <span class="mx-2 text-gray-300">·</span>
                <a
                    href="#"
                    @click.prevent="requestFullscreenChart"
                    class="text-blue-600 hover:text-blue-700 hover:underline">
                    Fullscreen
                </a>
                <span class="mx-2 text-gray-300">·</span>
                <a
                    href="#"
                    @click.prevent="openChartInNewTab"
                    class="text-blue-600 hover:text-blue-700 hover:underline">
                    Open in New Tab
                </a>
            </div>

            <!-- RIGHT BUTTON -->
            <button
                @click="chartModalVisible = false"
                class="px-4 py-2 rounded-xl bg-user text-white">
                Close
            </button>

        </div>

    </div>

</div>

</template>

<script setup lang="ts">
import { ref, nextTick, watch, onMounted, computed, onBeforeUnmount } from "vue";
import { useChatStore } from "@/stores/chat";
import { useThemeStore } from "@/stores/theme";
import { useMessageModals } from "@/composables/useMessageModals";
import { useEmailResult } from "@/composables/useEmailResult";
import AiChartView from "./AiChartView.vue";
import {
    getDatabasesList,
    selectActiveDatabase,
    getLlmsList,
    selectActiveLlm,
    filterSessions,
    getMessagesDatabases,
    addBookmark,
    removeBookmark as apiRemoveBookmark,
    exportExcelFile,
    askQuestion
} from "@/services/api";

const chat = useChatStore();
const theme = useThemeStore();

// Composable for message visual actions (SQL / Data view modals)
const {
    modalVisible,
    modalTitle,
    modalContent,
    modalType,
    modalData,
    chartModalVisible,
    chartConfig,
    chartData,
    activeChartMsg,
    showSql,
    showData,
    showChart,
    copyContent,
    downloadContent,
    downloadExcel
} = useMessageModals();

const aiChartRef = ref<any>(null);
const chartIsMaximized = ref(false);
const chartModalRef = ref<HTMLElement | null>(null);

function parseMessage(msg: any) {
    if (msg._parsed) return msg._parsed;
    msg._parsed = { isJson: false, text: msg.messageText, canShowChart: false };
    if (typeof msg.messageText === 'string' && msg.messageText.trim().startsWith('{')) {
        try {
            const parsed = JSON.parse(msg.messageText);
            if (parsed.analysis_text) {
                msg._parsed.isJson = true;
                msg._parsed.text = parsed.analysis_text;

                if (parsed.chart_config && parsed.chart_data) {
                    const config = typeof parsed.chart_config === 'string' ? JSON.parse(parsed.chart_config) : parsed.chart_config;
                    if (config && config.ChartType && config.ChartType.toLowerCase() !== 'none') {
                        msg._parsed.chartConfig = config;
                        msg._parsed.chartData = typeof parsed.chart_data === 'string' ? JSON.parse(parsed.chart_data) : parsed.chart_data;
                        msg._parsed.canShowChart = true;
                    }
                }
            }
        } catch (e) {
            // fallback to original text if JSON parse fails
        }
    }
    return msg._parsed;
}

function downloadChartImage() {
    if (!aiChartRef.value) return;
    const url = aiChartRef.value.getDataURL();
    if (!url) {
        alert("Cannot download image for this chart type.");
        return;
    }
    const a = document.createElement("a");
    a.href = url;
    a.download = "chart.png";
    a.click();
}

function emailChart() {
    if (!aiChartRef.value || !activeChartMsg.value) return;
    const url = aiChartRef.value.getDataURL();
    if (url) {
        emailResult(activeChartMsg.value, url);
    } else {
        emailResult(activeChartMsg.value);
    }
}

function toggleMaximizeChart() {
    chartIsMaximized.value = !chartIsMaximized.value;
}

async function requestFullscreenChart() {
    if (!document.fullscreenElement && chartModalRef.value) {
        await chartModalRef.value.requestFullscreen().catch(err => {
            alert(`Error attempting to enable fullscreen mode: ${err.message}`);
        });
    } else if (document.fullscreenElement) {
        await document.exitFullscreen();
    }
}

function openChartInNewTab() {
    if (!aiChartRef.value || !aiChartRef.value.getOption) return;
    const option = aiChartRef.value.getOption();
    if (!option) return;
    
    const html = `
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8" />
        <title>${chartConfig.value?.Title || 'Chart Viewer'}</title>
        <scr` + `ipt src="https://cdn.jsdelivr.net/npm/echarts@5.6.0/dist/echarts.min.js"></scr` + `ipt>
        <style>body, html { width: 100%; height: 100%; margin: 0; padding: 0; background: #fff; } #chart { width: 100%; height: 100%; }</style>
    </head>
    <body>
        <div id="chart"></div>
        <scr` + `ipt>
            var chart = echarts.init(document.getElementById('chart'));
            chart.setOption(${JSON.stringify(option)});
            window.addEventListener('resize', function() { chart.resize(); });
        </scr` + `ipt>
    </body>
    </html>
    `;
    
    const blob = new Blob([html], { type: 'text/html' });
    const url = URL.createObjectURL(blob);
    window.open(url, '_blank');
}

// Composable for email result popups
const {
    emailModalVisible,
    emailFrom,
    emailTo,
    emailCc,
    emailSubject,
    emailBody,
    emailSending,
    emailResult,
    sendEmail,
    getQuestion
} = useEmailResult();

const question = ref("");
const loading = ref(false);

const chatBody = ref<HTMLElement | null>(null);
const bottomRef = ref<HTMLElement | null>(null);
const questionInput = ref<HTMLInputElement | null>(null);

const bookmarkText = ref("");
const bookmarkMessageId = ref<number | null>(null);
const showBookmarkModal = ref(false);

function openBookmarkModal(msg: any) {
    console.log("Opening bookmark modal");
    bookmarkMessageId.value = msg.id;
    bookmarkText.value = getQuestion(msg); // default question text
    showBookmarkModal.value = true;
}

async function saveBookmark() {
    if (bookmarkMessageId.value === null) return;

    try {
        await addBookmark(bookmarkMessageId.value, bookmarkText.value);
        const m = chat.messages.find((m: any) => m.id === bookmarkMessageId.value);
        if (m) {
            m.isBookmarked = true;
        }
        showBookmarkModal.value = false;
    } catch (err) {
        console.error("Failed to save bookmark:", err);
    }
}

async function removeBookmark(msg: any) {
    try {
        await apiRemoveBookmark(msg.id);
        msg.isBookmarked = false;
    } catch (err) {
        console.error("Failed to remove bookmark:", err);
    }
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

// ==========================================
// Question History Navigation
// ==========================================
const questionHistory = ref<string[]>([]);
const historyIndex = ref(-1);
const unsentQuestion = ref("");

function navigateHistory(direction: "up" | "down") {
    if (questionHistory.value.length === 0) return;

    if (direction === "up") {
        if (historyIndex.value === -1) {
            unsentQuestion.value = question.value;
            historyIndex.value = questionHistory.value.length - 1;
            question.value = questionHistory.value[historyIndex.value];
        } else if (historyIndex.value > 0) {
            historyIndex.value--;
            question.value = questionHistory.value[historyIndex.value];
        }
    } else if (direction === "down") {
        if (historyIndex.value !== -1) {
            if (historyIndex.value < questionHistory.value.length - 1) {
                historyIndex.value++;
                question.value = questionHistory.value[historyIndex.value];
            } else if (historyIndex.value === questionHistory.value.length - 1) {
                historyIndex.value = -1;
                question.value = unsentQuestion.value;
            }
        }
    }
}

watch(
    () => chat.messages.length,
    async () => {
        await nextTick();
        await scrollToBottom();
        questionInput.value?.focus();

        const userMsgs = chat.messages
            .filter((m: any) => isUser(m.role) && m.messageText)
            .map((m: any) => m.messageText);
        questionHistory.value = userMsgs;
        historyIndex.value = -1;
        unsentQuestion.value = "";
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

// ==========================================
// Send Question
// ==========================================
async function sendQuestion() {
    if (!question.value.trim() || loading.value)
        return;

    const userQuestion = question.value;
    const now = new Date().toLocaleString();
    const wasNewSession = !chat.selectedSessionId;

    // Add user msg
    chat.messages.push({
        id: Date.now(),
        role: "user",
        messageText: userQuestion,
        createdOn: now,
        SessionId: chat.selectedSessionId,
        canShowChart: false,
        canShowData: false,
        canShowSql: false
    });

    question.value = "";
    loading.value = true;

    await scrollToBottom();

    try {
        const raw = await askQuestion(userQuestion, chat.selectedSessionId);

        let text = raw;
        let msgid = Date.now() + 1;
        try {
            const json = JSON.parse(raw);
            console.log("Received data from Agent", json);
            try {
                text =
                    json.response ||
                    json.answer ||
                    json.message ||
                    json.messageText ||
                    raw;
            } catch {
                text = raw;
            }
            if (json.sessionId) {
                chat.selectedSessionId = json.sessionId;
            }
            if (json.id) {
                msgid = json.id;
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
        } catch {
            text = raw;
        }

        await scrollToBottom();
    } catch (ex) {
        console.log("Exception in SendQuestion", ex);
        chat.messages.push({
            id: Date.now() + 2,
            sessionId: chat.selectedSessionId,
            role: "assistant",
            messageText: "Error connecting to server.",
            createdOn: new Date().toLocaleString()
        });

        await scrollToBottom();
    } finally {
        loading.value = false;
        questionInput.value?.focus();
    }
}

function isUser(role: string) {
    const r = (role || "").toLowerCase().trim();
    return r === "user";
}

const themeBtnClass = (name: string) => {
    return [
        "px-3 py-1 rounded-lg text-sm transition",
        theme.currentTheme === name
            ? "bg-white shadow text-black"
            : "text-gray-600 hover:bg-white/50"
    ];
};

function startFromBookmark(questionText: string) {
    chat.viewMode = 'chat';
    chat.messages = [];
    chat.selectedSessionId = null;

    question.value = questionText;

    nextTick(() => {
        questionInput.value?.focus();
    });
}

async function exportExcel(msg: any) {
    try {
        console.log("Exporting Excel for Message Id: ", msg.id);
        const blob = await exportExcelFile(msg.id);
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = `export_${msg.id}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
    } catch (err) {
        alert("Unable to export Excel");
    }
}

const databases = ref<any[]>([]);
const activeDbName = ref('');
const activeDbDescription = ref('');
const showDbMenu = ref(false);

onMounted(() => {
    loadDatabases();
    loadLlms();
});

function toggleDbMenu() {
    showDbMenu.value = !showDbMenu.value;
}

async function loadDatabases() {
    try {
        const json = await getDatabasesList();
        databases.value = json.databases;
        activeDbName.value = json.activeDb;
        activeDbDescription.value = json.activeDbDescription;

        if (databases.value.length > 0 && !json.activeDb) {
            const current = databases.value[0];
            activeDbName.value = current.name;
            activeDbDescription.value = current.description;
        }

        chat.setSelectedDatabases(databases.value.map((x: any) => x.name));
    } catch (err) {
        console.error("Failed to load databases:", err);
    }
}

async function selectDb(db: any) {
    try {
        const res = await selectActiveDatabase(db.name);
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
    } catch (err) {
        console.error("Failed to select database:", err);
    }
}

const llms = ref<any[]>([]);
const activeProvider = ref('');
const activeModel = ref('');
const showLlmMenu = ref(false);

async function loadLlms() {
    try {
        const json = await getLlmsList();
        llms.value = json.providers;
        activeProvider.value = json.selectedProvider;
        activeModel.value = json.selectedModel;
    } catch (err) {
        console.error("Failed to load LLMs:", err);
    }
}

async function selectLlm(provider: string, model: string) {
    try {
        console.log("Llm changed to", provider, model);
        await selectActiveLlm(provider, model);
        activeProvider.value = provider;
        activeModel.value = model;
        showLlmMenu.value = false;
    } catch (err) {
        console.error("Failed to select LLM:", err);
    }
}

const showDbFilter = ref(false);

const allSelected = computed(() => {
    return chat.selectedDatabases.length === databases.value.length;
});

function toggleAllDatabases() {
    if (allSelected.value) {
        chat.setSelectedDatabases([activeDbName.value]);
    } else {
        chat.setSelectedDatabases(databases.value.map((x: any) => x.name));
    }
}

function toggleDatabase(dbName: string) {
    // Current active DB cannot be unchecked
    if (dbName === activeDbName.value)
        return;

    if (chat.selectedDatabases.includes(dbName)) {
        chat.selectedDatabases = chat.selectedDatabases
            .filter(x => x !== dbName);
    } else {
        chat.selectedDatabases.push(dbName);
    }
}

async function applyDatabaseFilter() {
    console.log("Applying DB filter", chat.selectedDatabases);
    try {
        const sessions = await filterSessions(chat.selectedDatabases);
        chat.sessions = sessions;

        const exists = sessions.some((x: any) => x.id === chat.selectedSessionId);
        if (!exists) {
            chat.selectedSessionId = null;
            chat.messages = [];
        }

        if (chat.selectedSessionId) {
            const msgs = await getMessagesDatabases(chat.selectedSessionId, chat.selectedDatabases);
            chat.messages = msgs;
        }
    } catch (err) {
        console.error("Failed to apply database filter:", err);
    }
}

watch(
    () => chat.selectedDatabases,
    async () => {
        await applyDatabaseFilter();
    },
    {
        deep: true
    }
);

watch(activeDbName, (db) => {
    if (!db)
        return;

    if (!chat.selectedDatabases.includes(db)) {
        chat.selectedDatabases.push(db);
    }
});

const dbFilterRef = ref();

onMounted(() => {
    document.addEventListener('click', handleOutsideClick);
});

onBeforeUnmount(() => {
    document.removeEventListener('click', handleOutsideClick);
});

function handleOutsideClick(e: any) {
    if (!dbFilterRef.value?.contains(e.target)) {
        showDbFilter.value = false;
    }
}
</script>
