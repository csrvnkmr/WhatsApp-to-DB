<template>

<div class="p-6 max-w-[1600px] mx-auto flex flex-col h-[calc(100vh-100px)] overflow-hidden">

    <!-- Header section -->
    <div class="flex items-center justify-between gap-4 pb-4 border-b border-soft shrink-0 mb-4">

        <div class="flex items-center gap-3">

            <RouterLink
                to="/admin/databases"
                class="p-2 rounded-xl border border-soft hover:bg-hover transition shrink-0"
                title="Back to Databases">
                ⬅️
            </RouterLink>

            <div>
                <span class="text-[10px] font-bold tracking-wider uppercase opacity-50">
                    Database Test Suite
                </span>

                <h2 class="text-2xl font-bold flex items-center gap-2">
                    🧪 Test Suite: {{ props.database }}
                </h2>
            </div>

        </div>

    </div>

    <!-- Main Content Area -->
    <div class="flex-1 min-h-0 overflow-hidden relative">

        <!-- Loading state -->
        <div v-if="loading" class="absolute inset-0 flex flex-col items-center justify-center gap-3 bg-panel/50 backdrop-blur-xs z-20">
            <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
            <span class="text-sm opacity-70">Loading test suite queries...</span>
        </div>

        <!-- Error state -->
        <div v-else-if="error" class="p-4 bg-red-500/10 text-red-500 rounded-xl border border-red-500/20 text-sm">
            ❌ {{ error }}
        </div>

        <!-- Selection Screen -->
        <TestSuiteSelection
            v-else-if="currentScreen === 'selection'"
            :queries="queries"
            :database="props.database"
            @start="handleStartEval" />

        <!-- Execution Screen -->
        <TestSuiteExecution
            v-else-if="currentScreen === 'execution'"
            :selected-queries="selectedQueries"
            :database="props.database"
            @back="currentScreen = 'selection'" />

    </div>

</div>

</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { getData } from '@/services/api'
import TestSuiteSelection from './TestSuiteSelection.vue'
import TestSuiteExecution from './TestSuiteExecution.vue'

const props = defineProps<{
    database: string
}>()

interface TestSuiteQuery {
    Question: string;
    Module: string;
    Query: string;
}

const currentScreen = ref<'selection' | 'execution'>('selection')
const queries = ref<TestSuiteQuery[]>([])
const selectedQueries = ref<TestSuiteQuery[]>([])
const loading = ref(true)
const error = ref<string | null>(null)

async function loadQueries() {
    loading.value = true
    error.value = null
    try {
        const rawData = await getData('testsuitequeries', props.database)
        if (Array.isArray(rawData)) {
            queries.value = rawData.map(item => ({
                Question: item.Question || item.question || '',
                Module: item.Module || item.module || '',
                Query: item.Query || item.query || ''
            }))
        } else {
            queries.value = []
        }
    } catch (e: any) {
        console.error("Failed to load test suite queries", e)
        error.value = "Failed to load test suite queries. Please make sure the backend is active."
    } finally {
        loading.value = false
    }
}

function handleStartEval(selected: TestSuiteQuery[]) {
    selectedQueries.value = selected
    currentScreen.value = 'execution'
}

onMounted(() => {
    loadQueries()
})

watch(() => props.database, () => {
    currentScreen.value = 'selection'
    loadQueries()
})
</script>

<style scoped>
</style>
