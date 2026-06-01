<template>

<div class="flex flex-col h-full overflow-hidden">

    <!-- Top control bar -->
    <div class="flex flex-wrap items-center justify-between gap-4 p-4 bg-panel border border-soft rounded-2xl mb-4 shrink-0">

        <!-- Search and filters -->
        <div class="flex flex-wrap items-center gap-3 flex-1 min-w-[300px]">

            <div class="relative w-64 md:w-80 flex items-center">
                <span class="absolute left-3 text-gray-400 select-none text-xs pointer-events-none z-10">🔍</span>
                <input
                    v-model="searchQuery"
                    type="text"
                    placeholder="Search module, question, or query..."
                    class="w-full bg-base border border-soft rounded-xl pr-3 py-2 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition"
                    style="padding-left: 2.25rem;" />
            </div>

            <!-- Action buttons -->
            <div class="flex items-center gap-1.5 border-l border-soft pl-3">
                <button
                    @click="selectAll"
                    class="px-2.5 py-1.5 rounded-lg bg-hover border border-soft hover:bg-hover/80 text-[11px] font-semibold transition cursor-pointer"
                    title="Select all visible questions">
                    ☑️ Select All
                </button>

                <button
                    @click="deselectAll"
                    class="px-2.5 py-1.5 rounded-lg bg-hover border border-soft hover:bg-hover/80 text-[11px] font-semibold transition cursor-pointer"
                    title="Deselect all visible questions">
                    ⬜ Deselect All
                </button>

                <button
                    @click="revertSelection"
                    class="px-2.5 py-1.5 rounded-lg bg-hover border border-soft hover:bg-hover/80 text-[11px] font-semibold transition cursor-pointer"
                    title="Invert selection of visible questions">
                    🔄 Revert
                </button>
            </div>

            <!-- Toggle selected only -->
            <button
                @click="showOnlySelected = !showOnlySelected"
                :class="[
                    'px-3 py-1.5 rounded-lg border text-[11px] font-semibold transition cursor-pointer flex items-center gap-1.5',
                    showOnlySelected 
                        ? 'bg-user/10 text-user border-user/30' 
                        : 'bg-hover/30 text-gray-500 border-soft hover:bg-hover/60'
                ]">
                <span>⭐</span>
                {{ showOnlySelected ? 'Showing Selected Only' : 'Show Selected Only' }}
            </button>

        </div>

        <!-- Start Eval button -->
        <div class="flex items-center gap-3">
            <span class="text-xs text-gray-500 font-medium">
                Selected: <strong class="text-user">{{ selectedSet.size }}</strong> / {{ props.queries.length }}
            </span>
            <button
                @click="startEval"
                :disabled="selectedSet.size === 0"
                class="px-5 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-500 text-white font-bold shadow-md shadow-emerald-500/10 hover:shadow-emerald-500/20 disabled:opacity-40 disabled:pointer-events-none transition flex items-center gap-2 text-xs cursor-pointer">
                <span>🚀</span> Start Evaluation ({{ selectedSet.size }})
            </button>
        </div>

    </div>

    <!-- Table Container -->
    <div class="flex-1 min-h-0 border border-soft rounded-2xl bg-panel flex flex-col overflow-hidden">

        <!-- Table Header (Static/Sticky) -->
        <div class="overflow-x-auto flex-1 min-h-0">
            <table class="w-full text-sm border-collapse table-fixed">
                <thead class="sticky top-0 bg-panel border-b border-soft z-10 shadow-[0_1px_0_0_rgba(0,0,0,0.05)]">
                    <tr class="text-left text-gray-500 dark:text-gray-400 text-xs font-semibold uppercase tracking-wider">
                        <th class="w-14 text-center p-3 select-none">Sel</th>
                        <th class="w-36 p-3">Module</th>
                        <th class="w-80 p-3">Question</th>
                        <th class="p-3">SQL / Target Query</th>
                    </tr>
                </thead>
                <tbody class="divide-y divide-soft">
                    <tr
                        v-for="(item, index) in filteredQueries"
                        :key="index"
                        :class="[
                            'hover:bg-hover/20 transition-colors duration-150 group cursor-pointer',
                            selectedSet.has(item) ? 'bg-user/5' : ''
                        ]"
                        @click="toggleItem(item)">
                        
                        <!-- Checkbox -->
                        <td class="w-14 text-center p-3 align-top" @click.stop>
                            <input
                                type="checkbox"
                                :checked="selectedSet.has(item)"
                                @change="toggleItem(item)"
                                class="w-4 h-4 rounded border-soft text-user focus:ring-user cursor-pointer transition" />
                        </td>

                        <!-- Module -->
                        <td class="w-36 p-3 align-top truncate">
                            <span class="inline-block bg-user/5 text-user border border-user/10 px-2.5 py-0.5 rounded-lg text-[10px] font-bold tracking-wider uppercase">
                                {{ item.Module || 'General' }}
                            </span>
                        </td>

                        <!-- Question -->
                        <td class="w-80 p-3 align-top font-semibold text-xs text-gray-800 dark:text-gray-200 leading-relaxed">
                            {{ item.Question }}
                        </td>

                        <!-- Query -->
                        <td class="p-3 align-top font-mono text-[11px] text-gray-600 dark:text-gray-400">
                            <div class="bg-base/30 border border-soft rounded-lg px-3 py-1.5 overflow-x-auto max-h-24 whitespace-pre-wrap break-all leading-normal group-hover:bg-base/60 transition-colors">
                                {{ item.Query }}
                            </div>
                        </td>

                    </tr>

                    <tr v-if="filteredQueries.length === 0">
                        <td colspan="4" class="text-center py-16 text-gray-500 opacity-60">
                            <span class="text-2xl mb-2 block">📭</span>
                            No test suite queries found matching the active filters.
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>

        <!-- Footer status bar -->
        <div class="px-4 py-2 border-t border-soft bg-panel/30 text-[10px] text-gray-500 flex justify-between shrink-0 select-none">
            <span>Showing {{ filteredQueries.length }} of {{ props.queries.length }} queries</span>
            <span v-if="searchQuery.trim()">Filtered search active</span>
        </div>

    </div>

</div>

</template>

<script setup lang="ts">
import { ref, computed } from 'vue'

interface TestSuiteQuery {
    Question: string;
    Module: string;
    Query: string;
}

const props = defineProps<{
    queries: TestSuiteQuery[]
    database: string
}>()

const emit = defineEmits<{
    (e: 'start', selected: TestSuiteQuery[]): void
}>()

const searchQuery = ref('')
const showOnlySelected = ref(false)
const selectedSet = ref(new Set<TestSuiteQuery>())

// Filtered list based on search term and showOnlySelected toggle
const filteredQueries = computed(() => {
    const q = searchQuery.value.toLowerCase().trim()
    return props.queries.filter(item => {
        const matchesSearch = !q || 
            item.Question.toLowerCase().includes(q) || 
            item.Module.toLowerCase().includes(q) || 
            item.Query.toLowerCase().includes(q)
            
        const matchesSelection = !showOnlySelected.value || selectedSet.value.has(item)
        
        return matchesSearch && matchesSelection
    })
})

function toggleItem(item: TestSuiteQuery) {
    if (selectedSet.value.has(item)) {
        selectedSet.value.delete(item)
    } else {
        selectedSet.value.add(item)
    }
}

// Select All currently filtered visible items
function selectAll() {
    filteredQueries.value.forEach(item => {
        selectedSet.value.add(item)
    })
}

// Deselect All currently filtered visible items
function deselectAll() {
    filteredQueries.value.forEach(item => {
        selectedSet.value.delete(item)
    })
}

// Revert selection for all currently filtered visible items
function revertSelection() {
    filteredQueries.value.forEach(item => {
        if (selectedSet.value.has(item)) {
            selectedSet.value.delete(item)
        } else {
            selectedSet.value.add(item)
        }
    })
}

function startEval() {
    emit('start', Array.from(selectedSet.value))
}
</script>

<style scoped>
</style>
