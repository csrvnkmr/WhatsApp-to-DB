<template>

<div class="flex flex-col h-full overflow-hidden">

    <!-- Top control panel -->
    <div class="flex flex-col gap-4 p-5 bg-panel border border-soft rounded-2xl mb-4 shrink-0 shadow-xs">

        <!-- Controls Row -->
        <div class="flex flex-wrap items-center justify-between gap-4">

            <!-- Model selector & Actions -->
            <div class="flex flex-wrap items-center gap-3 flex-1 min-w-[300px]">
                
                <span class="text-xs font-semibold text-gray-500">Target Models:</span>

                <!-- Multi-select Combo -->
                <div class="relative w-64 md:w-80" ref="dropdownRef">
                    <button
                        @click="isDropdownOpen = !isDropdownOpen"
                        :disabled="isRunning"
                        class="w-full bg-base border border-soft rounded-xl px-3 py-2 text-xs text-left focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition flex justify-between items-center disabled:opacity-50 disabled:cursor-not-allowed select-none">
                        <span class="truncate font-medium text-gray-700 dark:text-gray-300">
                            {{ selectedModels.length === 0 ? 'Select Models...' : `${selectedModels.length} Model(s) Selected` }}
                        </span>
                        <svg class="w-3.5 h-3.5 opacity-60" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
                        </svg>
                    </button>
                    <div v-if="isDropdownOpen" class="absolute left-0 right-0 mt-1 bg-panel border border-soft rounded-xl shadow-xl z-30 max-h-60 overflow-y-auto p-2 space-y-0.5">
                        <div
                            v-for="opt in modelOptions"
                            :key="opt.label"
                            @click="toggleModel(opt)"
                            class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-hover/40 cursor-pointer transition text-xs select-none">
                            <input
                                type="checkbox"
                                :checked="isModelSelected(opt)"
                                class="w-3.5 h-3.5 rounded border-soft text-user focus:ring-user"
                                @click.stop="toggleModel(opt)" />
                            <span class="font-medium text-gray-700 dark:text-gray-300">{{ opt.label }}</span>
                        </div>
                    </div>
                </div>

                <!-- Execute & Stop Buttons -->
                <button
                    @click="runEvaluation"
                    :disabled="isRunning || selectedModels.length === 0"
                    class="px-5 py-2 rounded-xl bg-user text-white font-bold hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed text-xs transition cursor-pointer select-none">
                    ⚡ Execute
                </button>

                <button
                    @click="handleStop"
                    :disabled="!isRunning"
                    class="px-5 py-2 rounded-xl bg-red-600 text-white font-bold hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed text-xs transition cursor-pointer select-none">
                    🛑 Stop
                </button>

            </div>

            <!-- Back to Selection button -->
            <button
                @click="emit('back')"
                :disabled="isRunning"
                class="px-4 py-2 rounded-xl border border-soft hover:bg-hover text-xs font-semibold transition disabled:opacity-50 disabled:cursor-not-allowed select-none">
                ↩️ Back to Selection
            </button>

        </div>

        <hr class="border-soft my-1" />

        <!-- Status Summary Row -->
        <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-4 text-xs font-semibold text-gray-600 dark:text-gray-400">
            
            <div class="flex flex-col gap-1">
                <span class="text-[10px] uppercase tracking-wider opacity-60">Overall Status</span>
                <span :class="[
                    'text-sm font-bold',
                    isRunning ? 'text-blue-500' : statusMessage === 'Execution completed' ? 'text-green-500' : 'text-gray-500'
                ]">{{ statusMessage }}</span>
            </div>

            <div class="flex flex-col gap-1">
                <span class="text-[10px] uppercase tracking-wider opacity-60">Time Period</span>
                <span class="text-sm font-bold text-gray-800 dark:text-gray-200">
                    {{ startTime || '--:--' }} <span class="opacity-50">to</span> {{ endTime || '--:--' }}
                </span>
            </div>

            <div class="flex flex-col gap-1">
                <span class="text-[10px] uppercase tracking-wider opacity-60">Results Summary</span>
                <div class="flex gap-2 text-sm">
                    <span class="text-green-500">Pass: {{ comparisonStats.Pass }}</span>
                    <span class="text-red-500">Failed: {{ comparisonStats.Failed }}</span>
                </div>
            </div>

            <!-- Progress Bar -->
            <div class="flex flex-col gap-1 justify-center">
                <div class="flex justify-between text-[10px] uppercase tracking-wider opacity-60 mb-0.5">
                    <span>Progress</span>
                    <span>{{ progressPercent }}%</span>
                </div>
                <div class="w-full bg-base border border-soft rounded-full h-2 overflow-hidden">
                    <div
                        class="bg-user h-full transition-all duration-300"
                        :style="{ width: `${progressPercent}%` }"></div>
                </div>
            </div>

        </div>

    </div>

    <!-- Table Details -->
    <div class="flex-1 min-h-0 border border-soft rounded-2xl bg-panel flex flex-col overflow-hidden">

        <div class="overflow-x-auto flex-1 min-h-0">
            <table class="w-full text-sm border-collapse table-fixed">
                <thead class="sticky top-0 bg-panel border-b border-soft z-10 shadow-[0_1px_0_0_rgba(0,0,0,0.05)]">
                    <tr class="text-left text-gray-500 dark:text-gray-400 text-xs font-semibold uppercase tracking-wider select-none">
                        <th class="w-36 p-3">Module</th>
                        <th class="p-3">Question & Run Details</th>
                        <th class="w-24 p-3 text-center">Details</th>
                        <th class="w-56 p-3 text-right">Status</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-soft">
                    <tr
                        v-for="item in props.selectedQueries"
                        :key="item.Question"
                        class="hover:bg-hover/10 transition-colors duration-150">

                        <!-- Module -->
                        <td class="w-36 p-3 align-top">
                            <span class="inline-block bg-user/5 text-user border border-user/10 px-2.5 py-0.5 rounded-lg text-[10px] font-bold tracking-wider uppercase">
                                {{ item.Module || 'General' }}
                            </span>
                        </td>

                        <!-- Question & Run Details -->
                        <td class="p-3 align-top">
                            <div class="font-bold text-gray-800 dark:text-gray-200 text-xs mb-2">
                                {{ item.Question }}
                            </div>

                            <!-- SSE Live Progress monitor (Only show for active question) -->
                            <div
                                v-if="activeQuestion === item.Question && activeProgressEvents.length > 0"
                                class="progress-feed mt-3 space-y-1.5 border border-blue-500/15 rounded-xl p-3 bg-blue-500/5 max-h-40 overflow-y-auto">
                                <div class="text-[9px] font-bold uppercase tracking-wider text-blue-500 mb-1 flex items-center gap-1.5 animate-pulse">
                                    <span class="w-1.5 h-1.5 bg-blue-500 rounded-full"></span>
                                    AI Thinking Process (Model: {{ activeModel }})
                                </div>
                                <div
                                    v-for="(evt, i) in activeProgressEvents"
                                    :key="i"
                                    :class="['progress-event', `phase-${(evt.Phase || evt.phase || '').toLowerCase()}`]">
                                    <span class="message text-[11px] font-medium">{{ evt.Message || evt.message }}</span>
                                    <span v-if="evt.Detail || evt.detail" class="detail text-[10px] block opacity-85 mt-0.5 leading-relaxed bg-base/50 p-1.5 rounded font-mono border border-soft/50">{{ evt.Detail || evt.detail }}</span>
                                </div>
                            </div>



                        </td>

                        <!-- Details Link Column -->
                        <td class="w-24 p-3 align-top text-center">
                            <button
                                v-if="getStatus(item.Question).startsWith('Completed')"
                                @click="openDetails(item)"
                                class="text-xs font-bold text-user hover:underline cursor-pointer">
                                🔍 Details
                            </button>
                            <span v-else class="text-xs text-gray-400 select-none">-</span>
                        </td>

                        <!-- Status Column -->
                        <td class="w-56 p-3 align-top text-right">
                            <span :class="[
                                'inline-block px-2.5 py-1 rounded-lg font-bold text-[10px] uppercase border select-none',
                                getStatusClass(item.Question)
                            ]">
                                {{ getStatus(item.Question) }}
                            </span>
                        </td>

                    </tr>
                </tbody>
            </table>
        </div>

    </div>

</div>

<!-- Modal Backdrop -->
<div v-if="isModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center z-50 p-4" @click.self="isModalOpen = false">
    <!-- Modal Container -->
    <div class="bg-panel border border-soft rounded-2xl max-w-4xl w-full max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-fade-in">
        
        <!-- Modal Header -->
        <div class="flex justify-between items-center px-6 py-4 border-b border-soft bg-base/30 shrink-0">
            <div class="flex flex-col gap-1 pr-6 min-w-0">
                <span class="text-[10px] font-bold uppercase tracking-wider text-user">Test Execution Details</span>
                <h3 class="text-sm font-bold text-gray-800 dark:text-gray-200 truncate select-all">{{ modalData?.question }}</h3>
            </div>
            <button @click="isModalOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition text-2xl font-semibold leading-none cursor-pointer">
                &times;
            </button>
        </div>

        <!-- Modal Body (Scrollable) -->
        <div class="flex-1 overflow-y-auto p-6 space-y-6">
            
            <!-- Database Ground Truth Result -->
            <div class="flex flex-col gap-2">
                <div class="flex items-center gap-1.5 text-xs font-bold text-gray-700 dark:text-gray-300">
                    <span>🗄️</span> Common Database Result (Source of Truth)
                </div>
                <div class="text-xs font-mono bg-base/50 border border-soft rounded-xl p-4 max-h-48 overflow-y-auto whitespace-pre-wrap break-all leading-normal text-gray-600 dark:text-gray-400 select-all">
                    {{ modalData?.databaseResult || '(No database results available)' }}
                </div>
            </div>

            <!-- Repeated LLM Comparisons -->
            <div class="space-y-6">
                <div class="text-xs font-bold uppercase tracking-wider text-gray-500 border-b border-soft pb-1">
                    🤖 LLM Performance & Judgments
                </div>

                <div 
                    v-for="run in modalData?.runs" 
                    :key="run.modelName"
                    class="border border-soft rounded-xl bg-base/10 overflow-hidden shadow-xs">
                    
                    <!-- LLM Title bar -->
                    <div class="bg-base/30 px-4 py-2.5 border-b border-soft flex justify-between items-center text-xs font-bold text-gray-700 dark:text-gray-300">
                        <span>🤖 {{ run.modelName }}</span>
                        <div class="flex gap-2">
                            <span :class="[
                                'px-2 py-0.5 rounded text-[10px] font-extrabold uppercase tracking-wider border',
                                run.comparison === 'Pass' ? 'bg-green-500/10 text-green-600 border-green-500/20' : 'bg-red-500/10 text-red-500 border-red-500/20'
                            ]">{{ run.comparison || 'Failed' }}</span>
                            
                            <span :class="[
                                'px-2 py-0.5 rounded text-[10px] font-extrabold uppercase tracking-wider border',
                                isAnswered(run.answersthequestion) ? 'bg-green-500/10 text-green-600 border-green-500/20' : 'bg-red-500/10 text-red-500 border-red-500/20'
                            ]">
                                Answered: {{ isAnswered(run.answersthequestion) ? 'Yes' : 'No' }}
                            </span>
                        </div>
                    </div>

                    <!-- Metrics Details -->
                    <div class="p-4 grid grid-cols-1 md:grid-cols-2 gap-4">
                        
                        <!-- LLM Output -->
                        <div class="flex flex-col gap-1.5">
                            <span class="text-[10px] font-bold uppercase tracking-wider text-gray-500">LLM Generated Output</span>
                            <div class="flex-1 text-xs font-mono bg-panel border border-soft/50 rounded-lg p-3 max-h-48 overflow-y-auto whitespace-pre-wrap break-all leading-normal text-gray-600 dark:text-gray-400 select-all">
                                {{ run.llmResult || '(No LLM response generated)' }}
                            </div>
                        </div>

                        <!-- Diff & Reason -->
                        <div class="flex flex-col gap-4">
                            
                            <!-- Differences -->
                            <div class="flex flex-col gap-1">
                                <span class="text-[10px] font-bold uppercase tracking-wider text-gray-500">Differences Detected</span>
                                <div class="text-xs font-mono bg-panel border border-soft/50 rounded-lg p-3 max-h-24 overflow-y-auto whitespace-pre-wrap text-red-500 leading-normal">
                                    {{ run.difference || 'None' }}
                                </div>
                            </div>

                            <!-- Reason -->
                            <div class="flex flex-col gap-1">
                                <span class="text-[10px] font-bold uppercase tracking-wider text-gray-500">Judge Reason / Rationale</span>
                                <div class="text-xs bg-panel border border-soft/50 rounded-lg p-3 max-h-24 overflow-y-auto leading-relaxed text-gray-600 dark:text-gray-400 font-sans leading-normal">
                                    {{ run.reason || 'No explanation provided.' }}
                                </div>
                            </div>

                        </div>

                    </div>

                </div>
            </div>

        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3.5 border-t border-soft bg-base/30 flex justify-end shrink-0">
            <button @click="isModalOpen = false" class="px-4 py-2 bg-user hover:opacity-90 text-white font-bold rounded-xl text-xs transition cursor-pointer select-none">
                Close
            </button>
        </div>

    </div>
</div>

</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import { getLlmsList, selectActiveLlm, selectActiveDatabase, evalQuestion, executeDatabaseQuery, compareResults, stopEvaluation, startEvalRun, getMessageData, endEvalRun, BASE_URL } from '@/services/api'

interface TestSuiteQuery {
    Question: string;
    Module: string;
    Query: string;
}

interface ModelOption {
    provider: string;
    model: string;
    label: string;
}

interface RunResult {
    provider: string;
    modelName: string;
    llmResult: string;
    startTime: Date;
    endTime: Date | null;
    sseEvents: any[];
    isLoading: boolean;
    comparison?: string;
    answersthequestion?: any;
    difference?: string;
    reason?: string;
}

interface QuestionState {
    status: string;
    runs: RunResult[];
    dbResult?: string;
    dbStartTime?: string;
    dbEndTime?: string;
}

const props = defineProps<{
    selectedQueries: TestSuiteQuery[]
    database: string
}>()

const emit = defineEmits<{
    (e: 'back'): void
}>()

// UI state
const isDropdownOpen = ref(false)
const dropdownRef = ref<HTMLElement | null>(null)
const isRunning = ref(false)
const statusMessage = ref("Not started")
const startTime = ref<string | null>(null)
const endTime = ref<string | null>(null)
const progressPercent = ref(0)
const comparisonStats = ref({ Pass: 0, Failed: 0 })

// Dropdown options
const modelOptions = ref<ModelOption[]>([])
const selectedModels = ref<ModelOption[]>([])

// Running state tracking
const testResults = ref<Record<string, QuestionState>>({})
const activeQuestion = ref<string | null>(null)
const activeModel = ref<string | null>(null)
const activeProgressEvents = ref<any[]>([])

let stopRequested = false

// Toggle individual model selection
function toggleModel(opt: ModelOption) {
    const index = selectedModels.value.findIndex(m => m.label === opt.label)
    if (index === -1) {
        selectedModels.value.push(opt)
    } else {
        selectedModels.value.splice(index, 1)
    }
    sessionStorage.setItem('testrunner_selected_models', JSON.stringify(selectedModels.value))
}

function isModelSelected(opt: ModelOption) {
    return selectedModels.value.some(m => m.label === opt.label)
}

// Helpers for reading results in template
function getStatus(questionText: string) {
    return testResults.value[questionText]?.status || 'Pending'
}

function getStatusClass(questionText: string) {
    const stat = getStatus(questionText)
    if (stat === 'Pending') return 'bg-hover/40 text-gray-500 border-soft'
    if (stat.startsWith('Executing') || stat === 'Comparing Results') return 'bg-blue-500/10 text-blue-600 border-blue-500/20 animate-pulse'
    if (stat.startsWith('Completed')) {
        if (stat.includes('Failed')) return 'bg-red-500/10 text-red-500 border-red-500/20'
        return 'bg-green-500/10 text-green-600 border-green-500/20'
    }
    return 'bg-hover text-gray-500 border-soft'
}

function getRunsForQuestion(questionText: string) {
    return testResults.value[questionText]?.runs || []
}

function getDbResultForQuestion(questionText: string) {
    return testResults.value[questionText]?.dbResult || ''
}

function getDbStartTimeForQuestion(questionText: string) {
    return testResults.value[questionText]?.dbStartTime || ''
}

function getDbEndTimeForQuestion(questionText: string) {
    return testResults.value[questionText]?.dbEndTime || ''
}

function formatTime(val: Date | null) {
    if (!val) return ''
    return val.toLocaleTimeString()
}

const isModalOpen = ref(false)
const modalData = ref<{
    question: string;
    databaseResult: string;
    runs: RunResult[];
} | null>(null)

function openDetails(item: TestSuiteQuery) {
    const qState = testResults.value[item.Question]
    if (!qState) return
    modalData.value = {
        question: item.Question,
        databaseResult: qState.dbResult || '',
        runs: qState.runs
    }
    isModalOpen.value = true
}

function isAnswered(val: any): boolean {
    if (val === true || val === 'true' || val === 'Yes' || val === 'yes' || val === 1 || val === '1') {
        return true
    }
    return false
}

// Click outside handler for dropdown
function handleClickOutside(event: MouseEvent) {
    if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
        isDropdownOpen.value = false
    }
}

async function loadModels() {
    try {
        const response = await getLlmsList()
        const options: ModelOption[] = []
        if (response && Array.isArray(response.providers)) {
            response.providers.forEach((prov: any) => {
                const isEnabled = prov.Enabled !== undefined ? prov.Enabled : prov.enabled;
                const modelsList = prov.Models || prov.models;
                const providerName = prov.Provider || prov.provider || '';

                if (isEnabled && Array.isArray(modelsList)) {
                    modelsList.forEach((mod: string) => {
                        options.push({
                            provider: providerName,
                            model: mod,
                            label: `${providerName}: ${mod}`
                        })
                    })
                }
            })
        }
        modelOptions.value = options
        
        // Retrieve previously selected models from session storage
        const saved = sessionStorage.getItem('testrunner_selected_models')
        if (saved) {
            try {
                const parsed = JSON.parse(saved) as ModelOption[]
                // Only select models that are actually available in loaded options
                selectedModels.value = options.filter(opt => 
                    parsed.some(savedOpt => savedOpt.label === opt.label)
                )
            } catch (e) {
                console.error("Failed to parse saved models from sessionStorage", e)
            }
        }
    } catch (e) {
        console.error("Failed to load models list", e)
    }
}

// Execution logic
async function runEvaluation() {
    if (selectedModels.value.length === 0) {
        alert("Please select at least one LLM model for evaluation.")
        return
    }

    isRunning.value = true
    stopRequested = false
    startTime.value = new Date().toLocaleTimeString()
    endTime.value = null
    progressPercent.value = 0
    comparisonStats.value = { Pass: 0, Failed: 0 }

    // Initialize eval run on the backend
    try {
        const startPayload = {
            Database: props.database,
            Questions: props.selectedQueries.map(q => q.Question),
            Models: selectedModels.value.map(m => ({
                Provider: m.provider,
                Model: m.model
            }))
        }
        await startEvalRun(startPayload)
    } catch (err: any) {
        console.error("Failed to call startEvalRun:", err)
        alert(`Failed to initialize evaluation run on the server: ${err.message}`)
        isRunning.value = false
        return
    }

    // Set initial Pending state
    const results: Record<string, QuestionState> = {}
    props.selectedQueries.forEach(q => {
        results[q.Question] = {
            status: "Pending",
            runs: []
        }
    })
    testResults.value = results

    const totalQuestions = props.selectedQueries.length
    const totalModels = selectedModels.value.length
    const totalSteps = totalQuestions * totalModels
    let completedSteps = 0

    for (let i = 0; i < totalQuestions; i++) {
        if (stopRequested) break

        const q = props.selectedQueries[i]
        if (!q) continue

        const qState = results[q.Question]
        if (!qState) continue

        qState.status = "Running"

        // 1. Loop through selected models
        for (let m = 0; m < totalModels; m++) {
            if (stopRequested) break

            const modelOpt = selectedModels.value[m]
            if (!modelOpt) continue

            // Update status text
            qState.status = `Executing LLM ${modelOpt.model} (${m + 1} of ${totalModels})`
            statusMessage.value = `Executing ${i + 1} of ${totalQuestions} (${Math.round((completedSteps / totalSteps) * 100)}% complete)`

            const runInfo: RunResult = {
                provider: modelOpt.provider,
                modelName: modelOpt.model,
                llmResult: "",
                startTime: new Date(),
                endTime: null,
                sseEvents: [],
                isLoading: true
            }
            qState.runs.push(runInfo)

            // Set active states for live updates
            activeQuestion.value = q.Question
            activeModel.value = modelOpt.model
            activeProgressEvents.value = []

            try {
                // Select active database on backend
                await selectActiveDatabase(props.database)

                // Select LLM on backend
                await selectActiveLlm(modelOpt.provider, modelOpt.model)

                // Call /eval endpoint
                const askRes = await evalQuestion(q.Question, null)
                let requestId = ""
                try {
                    const json = JSON.parse(askRes)
                    requestId = json.requestId || json.RequestId || ""
                } catch {
                    throw new Error("Invalid response from evalQuestion")
                }

                if (!requestId) {
                    throw new Error("No requestId returned")
                }

                // Listen to SSE progress events
                await new Promise<void>((resolve) => {
                    const sseUrl = `${BASE_URL}/stream/${requestId}`
                    const sse = new EventSource(sseUrl)

                    sse.addEventListener("progress", (event: MessageEvent) => {
                        try {
                            const evt = JSON.parse(event.data)
                            const phase = (evt.Phase || evt.phase || "").toLowerCase()

                            if (phase === "done") {
                                const detail = evt.Detail || evt.detail || ""
                                let finalMsgText = detail
                                let messageId: number | null = null
                                let canShowData = false
                                try {
                                    const msgDto = JSON.parse(detail)
                                    finalMsgText = msgDto.messageText || msgDto.MessageText || detail
                                    messageId = msgDto.id || msgDto.Id || null
                                    canShowData = msgDto.canShowData || msgDto.CanShowData || false
                                } catch {}

                                if (canShowData && messageId) {
                                    getMessageData(messageId).then(data => {
                                        runInfo.llmResult = typeof data === 'string' ? data : JSON.stringify(data, null, 2)
                                        runInfo.endTime = new Date()
                                        runInfo.isLoading = false
                                        sse.close()
                                        resolve()
                                    }).catch(err => {
                                        console.error("Failed to fetch message data:", err)
                                        runInfo.llmResult = finalMsgText
                                        runInfo.endTime = new Date()
                                        runInfo.isLoading = false
                                        sse.close()
                                        resolve()
                                    })
                                } else {
                                    runInfo.llmResult = finalMsgText
                                    runInfo.endTime = new Date()
                                    runInfo.isLoading = false
                                    sse.close()
                                    resolve()
                                }
                                return
                            }

                            if (phase === "error") {
                                runInfo.llmResult = evt.Message || evt.message || "Error generating response"
                                runInfo.endTime = new Date()
                                runInfo.isLoading = false
                                sse.close()
                                resolve()
                                return
                            }

                            if (phase !== "done_step") {
                                runInfo.sseEvents.push(evt)
                                if (activeQuestion.value === q.Question && activeModel.value === modelOpt.model) {
                                    activeProgressEvents.value = [...runInfo.sseEvents]
                                }
                            }
                        } catch (err) {
                            console.error("SSE progress parse error", err)
                        }
                    })

                    sse.onerror = () => {
                        runInfo.llmResult = "Progress stream disconnected."
                        runInfo.endTime = new Date()
                        runInfo.isLoading = false
                        sse.close()
                        resolve()
                    }
                })
            } catch (err: any) {
                runInfo.llmResult = `Error: ${err.message}`
                runInfo.endTime = new Date()
                runInfo.isLoading = false
            }

            completedSteps++
            progressPercent.value = Math.round((completedSteps / totalSteps) * 100)
        }

        if (stopRequested) break

        // 2. Call DB Execution endpoint
        qState.status = "Executing Database Query"
        const dbStartTime = new Date().toLocaleTimeString()
        let dbResult = ""
        try {
            dbResult = await executeDatabaseQuery(props.database, q.Question)
        } catch (err: any) {
            dbResult = `Database Error: ${err.message}`
        }
        const dbEndTime = new Date().toLocaleTimeString()

        qState.dbResult = dbResult
        qState.dbStartTime = dbStartTime
        qState.dbEndTime = dbEndTime

        if (stopRequested) break

        // 3. Compare LLM and DB results for each run
        qState.status = "Comparing Results"
        let questionComparisonSummary = ""
        for (let m = 0; m < totalModels; m++) {
            if (stopRequested) break

            const runInfo = qState.runs[m]
            if (!runInfo) continue

            try {
                const compPayload = {
                    questiontext: q.Question,
                    llmresult: runInfo.llmResult,
                    databaseresult: dbResult,
                    provider: runInfo.provider,
                    modelname: runInfo.modelName,
                    starttime: runInfo.startTime,
                    endtime: runInfo.endTime || new Date()
                }

                const compRes = await compareResults(props.database, compPayload)
                runInfo.comparison = compRes.comparison || "Failed"
                runInfo.answersthequestion = compRes.answersthequestion !== undefined ? compRes.answersthequestion : compRes.answersTheQuestion
                runInfo.difference = compRes.difference || compRes.differences || ""
                runInfo.reason = compRes.reason || ""

                if (runInfo.comparison === "Pass") {
                    comparisonStats.value.Pass++
                } else {
                    comparisonStats.value.Failed++
                }
            } catch (err) {
                console.error("Comparison execution failed", err)
                runInfo.comparison = "Failed"
                comparisonStats.value.Failed++
            }

            questionComparisonSummary += `${runInfo.comparison} (${runInfo.modelName}) | `
        }

        if (stopRequested) break

        // 4. Update the row's final status
        const cleanSummary = questionComparisonSummary.replace(/ \| $/, "")
        qState.status = `Completed: ${cleanSummary}`
    }

    // Call endEvalRun to persist completion stats on the server
    console.log("[TestSuiteExecution] Calling endEvalRun()...");
    try {
        const res = await endEvalRun()
        console.log("[TestSuiteExecution] endEvalRun() response received:", res)
    } catch (err) {
        console.error("[TestSuiteExecution] Failed to call endEvalRun:", err)
    }

    // Finished
    isRunning.value = false
    activeQuestion.value = null
    activeModel.value = null
    activeProgressEvents.value = []

    if (stopRequested) {
        statusMessage.value = "Execution canceled"
    } else {
        statusMessage.value = "Execution completed"
        progressPercent.value = 100
        endTime.value = new Date().toLocaleTimeString()
    }
}

async function handleStop() {
    stopRequested = true
    try {
        await stopEvaluation()
    } catch (e) {
        console.error("Stop execution API error", e)
    }
    statusMessage.value = "Execution canceled"
    isRunning.value = false
    activeQuestion.value = null
    activeModel.value = null
    activeProgressEvents.value = []
}

onMounted(() => {
    loadModels()
    document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside)
})
</script>

<style scoped>
.progress-feed {
    border-color: rgba(59, 130, 246, 0.15);
}
.progress-event {
    font-size: 0.85rem;
    display: flex;
    flex-direction: column;
    gap: 1px;
}
.phase-thinking {
    color: #2563eb;
}
.phase-planning {
    color: #d97706;
}
.phase-executing {
    color: #059669;
}
.phase-done {
    color: #4b5563;
}
.phase-error {
    color: #dc2626;
}
</style>
