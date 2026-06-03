<template>
<div class="p-6 max-w-[1600px] mx-auto flex flex-col gap-6 text-text">
        <!-- HEADER -->
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-soft shrink-0 mb-6">
            <div>
                <span class="text-[10px] font-bold tracking-wider uppercase opacity-50">Test Suite Reports</span>
                <h2 class="text-2xl font-bold flex items-center gap-2">
                    📊 {{ activeReport }}
                </h2>
            </div>
            <div class="flex items-center gap-2">
                <button 
                    @click="loadDashboardData" 
                    class="px-3 py-1.5 rounded-xl border border-soft hover:bg-hover transition text-sm font-medium flex items-center gap-1.5"
                    :disabled="loading">
                    <span :class="{ 'animate-spin inline-block': loading }">🔄</span> Refresh
                </button>
            </div>
        </div>

        <!-- LOADING / ERROR STATES -->
        <div v-if="loading && !dashboardData" class="flex flex-col items-center justify-center py-20 gap-3">
            <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
            <span class="text-sm opacity-70">Loading report metrics...</span>
        </div>

        <div v-else-if="error" class="p-4 bg-red-500/10 text-red-500 rounded-xl border border-red-500/20 text-sm mb-6">
            ❌ {{ error }}
        </div>

        <!-- RUN SUMMARY REPORT -->
        <div v-else-if="activeReport === 'Run Summary'" class="space-y-6">
            <!-- KPI METRICS GRID -->
            <div class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-8 gap-3">
                <!-- Card 1 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Total Runs</span>
                        <h3 class="text-xl font-extrabold mt-1 text-blue-500">{{ summary.totalRuns }}</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">📋</span>
                </div>

                <!-- Card 2 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Successful</span>
                        <h3 class="text-xl font-extrabold mt-1 text-emerald-500">{{ summary.successfulRuns }}</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">✅</span>
                </div>

                <!-- Card 3 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Failed Runs</span>
                        <h3 class="text-xl font-extrabold mt-1 text-rose-500">{{ summary.failedRuns }}</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">❌</span>
                </div>

                <!-- Card 4 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Fail Rate (Runs)</span>
                        <h3 class="text-xl font-extrabold mt-1 text-amber-500">{{ summary.failureRate }}%</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">📉</span>
                </div>

                <!-- Card 5 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Total Cases</span>
                        <h3 class="text-xl font-extrabold mt-1 text-violet-500">{{ summary.totalCases }}</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">🧪</span>
                </div>

                <!-- Card 6 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Pass Inferences</span>
                        <h3 class="text-xl font-extrabold mt-1 text-teal-500">{{ summary.totalSuccessfulInferences }}</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">👍</span>
                </div>

                <!-- Card 7 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Fail Inferences</span>
                        <h3 class="text-xl font-extrabold mt-1 text-red-500">{{ summary.totalFailedInferences }}</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">👎</span>
                </div>

                <!-- Card 8 -->
                <div class="bg-panel border border-soft rounded-xl p-3 flex items-center justify-between hover:shadow-md transition relative overflow-hidden group">
                    <div class="flex flex-col min-w-0">
                        <span class="text-[10px] font-semibold opacity-60 uppercase tracking-wider truncate">Fail Rate (Inf)</span>
                        <h3 class="text-xl font-extrabold mt-1 text-orange-500">{{ summary.failureRateInferences }}%</h3>
                    </div>
                    <span class="text-xl opacity-20 shrink-0 select-none ml-2">⚠️</span>
                </div>
            </div>

            <!-- FILTERS BAR -->
            <div class="bg-panel border border-soft rounded-2xl p-4 flex flex-wrap gap-4 items-center">
                <!-- Database filter -->
                <div class="flex flex-col gap-1 min-w-[200px]">
                    <label class="text-[10px] font-bold uppercase opacity-55">Database</label>
                    <select 
                        v-model="filterDb" 
                        class="px-3 py-1.5 rounded-xl border border-soft bg-base text-sm focus:outline-none focus:border-user transition">
                        <option value="">All Databases</option>
                        <option v-for="db in dbList" :key="db.Name" :value="db.Name">{{ db.Name }}</option>
                    </select>
                </div>

                <!-- Status filter -->
                <div class="flex flex-col gap-1 min-w-[150px]">
                    <label class="text-[10px] font-bold uppercase opacity-55">Status</label>
                    <select 
                        v-model="filterStatus" 
                        class="px-3 py-1.5 rounded-xl border border-soft bg-base text-sm focus:outline-none focus:border-user transition">
                        <option value="">All Statuses</option>
                        <option value="Completed">Completed</option>
                        <option value="Running">Running</option>
                        <option value="Aborted">Aborted</option>
                    </select>
                </div>

                <!-- Compare Runs option -->
                <!-- Compare Runs option -->
                <div v-if="selectedCompareRuns.length === 2" class="flex flex-col gap-1">
                    <label class="text-[10px] font-bold uppercase opacity-55">Run Comparison</label>
                    <button 
                        @click="openRunComparisonModal"
                        class="px-4 py-1.5 rounded-xl bg-user text-white hover:opacity-90 shadow-md shadow-user/20 transition text-sm font-bold flex items-center gap-1.5 h-[34px] cursor-pointer">
                        ⚖️ Compare Runs (#{{ selectedCompareRuns[0] }} vs #{{ selectedCompareRuns[1] }})
                    </button>
                </div>

                <!-- Module Performance option -->
                <div v-if="selectedCompareRuns.length >= 1" class="flex flex-col gap-1">
                    <label class="text-[10px] font-bold uppercase opacity-55">Module Analysis</label>
                    <button 
                        @click="openModulePerformanceModal"
                        class="px-4 py-1.5 rounded-xl bg-teal-600 text-white hover:opacity-90 shadow-md shadow-teal-600/20 transition text-sm font-bold flex items-center gap-1.5 h-[34px] cursor-pointer">
                        🧩 Check Module Performance ({{ selectedCompareRuns.length }} selected)
                    </button>
                </div>

                <!-- LLM Performance option -->
                <div v-if="selectedCompareRuns.length >= 1" class="flex flex-col gap-1">
                    <label class="text-[10px] font-bold uppercase opacity-55">LLM Analysis</label>
                    <button 
                        @click="openLlmPerformanceModal"
                        class="px-4 py-1.5 rounded-xl bg-indigo-600 text-white hover:opacity-90 shadow-md shadow-indigo-600/20 transition text-sm font-bold flex items-center gap-1.5 h-[34px] cursor-pointer">
                        🤖 Check LLM Performance ({{ selectedCompareRuns.length }} selected)
                    </button>
                </div>

                <!-- Case Details option -->
                <div v-if="selectedCompareRuns.length >= 1" class="flex flex-col gap-1">
                    <label class="text-[10px] font-bold uppercase opacity-55">Case Analysis</label>
                    <button 
                        @click="openCaseDetailsModal"
                        class="px-4 py-1.5 rounded-xl bg-violet-600 text-white hover:opacity-90 shadow-md shadow-violet-600/20 transition text-sm font-bold flex items-center gap-1.5 h-[34px] cursor-pointer">
                        🔍 Show Case Details ({{ selectedCompareRuns.length }} selected)
                    </button>
                </div>

                <!-- Failure Analysis option -->
                <div v-if="selectedCompareRuns.length >= 1" class="flex flex-col gap-1">
                    <label class="text-[10px] font-bold uppercase opacity-55">Failure Analysis</label>
                    <button 
                        @click="openFailureAnalysisModal"
                        class="px-4 py-1.5 rounded-xl bg-rose-600 text-white hover:opacity-90 shadow-md shadow-rose-600/20 transition text-sm font-bold flex items-center gap-1.5 h-[34px] cursor-pointer">
                        ⚠️ Check Failure Analysis ({{ selectedCompareRuns.length }} selected)
                    </button>
                </div>

                <!-- Pass Trend option -->
                <div v-if="selectedCompareRuns.length >= 1" class="flex flex-col gap-1">
                    <label class="text-[10px] font-bold uppercase opacity-55">Trend Analysis</label>
                    <button 
                        @click="openPassTrendModal"
                        class="px-4 py-1.5 rounded-xl bg-emerald-600 text-white hover:opacity-90 shadow-md shadow-emerald-600/20 transition text-sm font-bold flex items-center gap-1.5 h-[34px] cursor-pointer">
                        📈 Pass Trend ({{ selectedCompareRuns.length }} selected)
                    </button>
                </div>

                <div class="flex-1"></div>

                <!-- Showing count info -->
                <div class="text-xs opacity-60 self-end mb-1">
                    Showing {{ filteredRuns.length }} runs
                </div>
            </div>

            <!-- LIST OF RUNS TABLE -->
            <div class="bg-panel border border-soft rounded-2xl overflow-hidden shadow-xs">
                <div class="overflow-x-auto">
                    <table class="w-full text-left border-collapse">
                        <thead>
                            <tr class="border-b border-soft bg-hover/30 text-xs font-semibold opacity-70">
                                <th class="w-12 p-4 text-center">Compare</th>
                                <th class="p-4">Run Id</th>
                                <th class="p-4">Database</th>
                                <th class="p-4">Started At</th>
                                <th class="p-4">Ended At</th>
                                <th class="p-4 text-center">Cases/Inferences</th>
                                <th class="p-4 text-center">Pass</th>
                                <th class="p-4 text-center">Fail</th>
                                <th class="p-4">Pass Rate</th>
                                <th class="p-4">Status</th>
                            </tr>
                        </thead>
                        <tbody class="divide-y divide-soft text-sm">
                            <tr v-if="paginatedRuns.length === 0" class="hover:bg-hover/20 transition">
                                <td colspan="10" class="p-8 text-center opacity-60">No runs match the selected filters.</td>
                            </tr>
                            <template v-for="run in paginatedRuns" :key="run.runId">
                                <tr class="hover:bg-hover/30 transition duration-150">
                                    <td class="p-4 text-center select-none relative group">
                                        <input 
                                            type="checkbox" 
                                            :value="run.runId" 
                                            v-model="selectedCompareRuns"
                                            :disabled="!filterDb"
                                            class="w-4 h-4 rounded border-soft text-user focus:ring-user" />
                                        
                                        <!-- Tooltip overlay -->
                                        <div v-if="!filterDb" class="absolute bottom-full left-1/2 -translate-x-1/2 mb-1 hidden group-hover:block bg-black/90 text-white text-[10px] px-2 py-1 rounded shadow-lg whitespace-nowrap z-30 pointer-events-none transition-all duration-200">
                                            Only one database should be selected for selecting a run
                                        </div>
                                    </td>
                                    <td class="p-4 font-bold text-user">
                                        <button 
                                            @click="toggleRun(run.runId)" 
                                            class="hover:underline text-left font-bold text-user cursor-pointer">
                                            #{{ run.runId }}
                                        </button>
                                    </td>
                                    <td class="p-4 font-semibold">{{ run.database }}</td>
                                    <td class="p-4 text-xs opacity-75">{{ run.startedAt }}</td>
                                    <td class="p-4 text-xs opacity-75">{{ run.endedAt || '-' }}</td>
                                    <td class="p-4 text-center font-medium">{{ run.totalCases }}</td>
                                    <td class="p-4 text-center text-emerald-500 font-semibold">{{ run.passCount }}</td>
                                    <td class="p-4 text-center text-rose-500 font-semibold">{{ run.failCount + run.errorCount }}</td>
                                    <td class="p-4">
                                        <div class="flex items-center gap-2">
                                            <div class="w-20 bg-soft rounded-full h-1.5 overflow-hidden shrink-0">
                                                <div 
                                                    class="bg-gradient-to-r from-emerald-500 to-teal-400 h-full rounded-full transition-all duration-300"
                                                    :style="{ width: getPassRatePercent(run) + '%' }">
                                                </div>
                                            </div>
                                            <span class="font-bold text-xs">{{ getPassRatePercent(run) }}%</span>
                                        </div>
                                    </td>
                                    <td class="p-4">
                                        <span :class="getStatusBadgeClass(run.status)">
                                            {{ run.status }}
                                        </span>
                                    </td>
                                </tr>

                                <!-- NESTED ROW FOR DETAILS -->
                                <tr v-if="selectedRunId === run.runId" class="bg-hover/5">
                                    <td colspan="10" class="p-4">
                                        <!-- LOADING STATE FOR RUN DETAILS -->
                                        <div v-if="detailsLoading" class="flex flex-col items-center justify-center p-6 gap-2">
                                            <span class="w-6 h-6 border-2 border-user border-t-transparent rounded-full animate-spin"></span>
                                            <span class="text-xs opacity-75">Loading Run #{{ selectedRunId }} details...</span>
                                        </div>

                                        <!-- DETAILED RUN QUESTIONS SECTION -->
                                        <div v-else-if="runDetails" class="space-y-4">
                                            <div class="flex items-center justify-between border-b border-soft pb-2">
                                                <h4 class="text-xs font-bold text-gray-800 dark:text-gray-200">
                                                    ❓ Questions & Inferences for Run #{{ selectedRunId }}
                                                </h4>
                                                <button 
                                                    @click="closeRunDetails" 
                                                    class="text-[10px] px-2 py-1 rounded border border-soft hover:bg-hover hover:text-red-500 transition font-medium select-none cursor-pointer">
                                                    ✖ Close Section
                                                </button>
                                            </div>

                                            <!-- Cases List Table -->
                                            <div class="overflow-x-auto border border-soft rounded-xl bg-panel">
                                                <table class="w-full text-xs border-collapse table-fixed">
                                                    <thead>
                                                        <tr class="text-left text-gray-500 bg-hover/20 text-[10px] font-semibold uppercase tracking-wider select-none">
                                                            <th class="w-36 p-3">Module</th>
                                                            <th class="p-3">Question & Run Details</th>
                                                            <th class="w-24 p-3 text-center">Details</th>
                                                            <th class="w-48 p-3 text-right">Status</th>
                                                        </tr>
                                                    </thead>
                                                    <tbody class="divide-y divide-soft">
                                                        <tr v-for="item in runDetails" :key="item.caseId" class="hover:bg-hover/10 transition-colors duration-150">
                                                            <!-- Module -->
                                                            <td class="w-36 p-3 align-top">
                                                                <span class="inline-block bg-user/5 text-user border border-user/10 px-2 py-0.5 rounded text-[9px] font-bold tracking-wider uppercase">
                                                                    {{ item.module || 'General' }}
                                                                </span>
                                                            </td>

                                                            <!-- Question & Runs -->
                                                            <td class="p-3 align-top">
                                                                <div class="font-bold text-gray-800 dark:text-gray-200 mb-2">
                                                                    {{ item.question }}
                                                                </div>
                                                                <div class="text-[10px] text-gray-500 space-y-1">
                                                                    <div v-for="inf in item.runs" :key="inf.modelName" class="flex items-center gap-2">
                                                                        <span>🤖 {{ inf.modelName }} ({{ inf.providerName }})</span>
                                                                        <span :class="[
                                                                            'px-1.5 py-0.5 rounded text-[8px] font-extrabold uppercase border',
                                                                            inf.comparison === 'Pass' ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20' : 'bg-rose-500/10 text-rose-500 border-rose-500/20'
                                                                        ]">{{ inf.comparison }}</span>
                                                                    </div>
                                                                </div>
                                                            </td>

                                                            <!-- Details Link Column -->
                                                            <td class="w-24 p-3 align-top text-center">
                                                                <button
                                                                    @click="openDetails(item)"
                                                                    class="font-bold text-user hover:underline cursor-pointer">
                                                                    🔍 Details
                                                                </button>
                                                            </td>

                                                            <!-- Status Badge Column -->
                                                            <td class="w-48 p-3 align-top text-right">
                                                                <span :class="[
                                                                    'inline-block px-2 py-0.5 rounded font-bold text-[9px] uppercase border select-none',
                                                                    getCaseStatusClass(item)
                                                                ]">
                                                                    {{ getCaseStatusText(item) }}
                                                                </span>
                                                            </td>
                                                        </tr>
                                                    </tbody>
                                                </table>
                                            </div>
                                        </div>
                                    </td>
                                </tr>
                            </template>
                        </tbody>
                    </table>
                </div>

                <!-- PAGINATION CONTROLS -->
                <div class="p-4 border-t border-soft flex items-center justify-between gap-4 text-xs bg-hover/10">
                    <span class="opacity-60">
                        Page {{ currentPage }} of {{ totalPages || 1 }} (Items {{ paginationStart }}-{{ paginationEnd }} of {{ filteredRuns.length }})
                    </span>
                    <div class="flex items-center gap-1.5">
                        <button 
                            @click="currentPage > 1 && currentPage--" 
                            :disabled="currentPage <= 1"
                            class="px-2.5 py-1.5 rounded-lg border border-soft hover:bg-hover transition font-medium disabled:opacity-40 disabled:hover:bg-transparent">
                            ⬅️ Previous
                        </button>
                        <button 
                            @click="currentPage < totalPages && currentPage++" 
                            :disabled="currentPage >= totalPages"
                            class="px-2.5 py-1.5 rounded-lg border border-soft hover:bg-hover transition font-medium disabled:opacity-40 disabled:hover:bg-transparent">
                            Next ➡️
                        </button>
                    </div>
                </div>
            </div>
        </div>

        <!-- OTHER REPORTS (PLACEHOLDERS) -->
        <div v-else class="flex flex-col items-center justify-center py-24 gap-4 bg-panel border border-soft rounded-2xl shadow-xs text-center max-w-lg mx-auto">
            <span class="text-5xl">🛠️</span>
            <h3 class="text-xl font-bold">Report In Development</h3>
            <p class="text-sm opacity-60 px-6">
                The "{{ activeReport }}" analytical view is currently scheduled for deployment. Check back soon for detailed comparisons and charts.
            </p>
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
                                isAnswered(run.answersQuestion) ? 'bg-green-500/10 text-green-600 border-green-500/20' : 'bg-red-500/10 text-red-500 border-red-500/20'
                            ]">
                                Answered: {{ isAnswered(run.answersQuestion) ? 'Yes' : 'No' }}
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

<!-- Run vs Run Comparison Modal -->
<div v-if="isCompareModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center z-50 p-4" @click.self="isCompareModalOpen = false">
    <!-- Modal Container -->
    <div class="bg-panel border border-soft rounded-2xl max-w-5xl w-full max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-fade-in">
        
        <!-- Modal Header -->
        <div class="flex justify-between items-center px-6 py-4 border-b border-soft bg-base/30 shrink-0">
            <div class="flex flex-col gap-1 pr-6 min-w-0">
                <span class="text-[10px] font-bold uppercase tracking-wider text-user">Comparative Run Analysis</span>
                <h3 class="text-sm font-bold text-gray-800 dark:text-gray-200">
                    ⚖️ Comparison: Run #{{ selectedCompareRuns[0] }} vs Run #{{ selectedCompareRuns[1] }}
                </h3>
            </div>
            <button @click="isCompareModalOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition text-2xl font-semibold leading-none cursor-pointer">
                &times;
            </button>
        </div>

        <!-- Modal Body -->
        <div class="flex-1 overflow-y-auto p-6">
            
            <div v-if="compareLoading" class="flex flex-col items-center justify-center py-20 gap-3">
                <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
                <span class="text-sm opacity-70">Running comparison analysis...</span>
            </div>

            <div v-else-if="compareError" class="p-4 bg-red-500/10 text-red-500 rounded-xl border border-red-500/20 text-sm">
                ❌ {{ compareError }}
            </div>

            <!-- Comparison Table -->
            <div v-else class="overflow-x-auto border border-soft rounded-xl bg-panel">
                <table class="w-full text-xs border-collapse">
                    <thead>
                        <tr class="text-left text-gray-500 bg-hover/20 text-[10px] font-semibold uppercase tracking-wider sticky top-0 bg-panel border-b border-soft shadow-[0_1px_0_0_rgba(0,0,0,0.05)] select-none">
                            <th class="w-32 p-3">Module</th>
                            <th class="p-3">Question</th>
                            <th class="w-48 p-3">Model</th>
                            <th class="w-32 p-3 text-center">Run #{{ selectedCompareRuns[0] }}</th>
                            <th class="w-32 p-3 text-center">Run #{{ selectedCompareRuns[1] }}</th>
                            <th class="w-24 p-3 text-center">Status</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-soft text-sm">
                        <tr v-for="(row, idx) in comparisonData" :key="idx" 
                            :class="['hover:bg-hover/10 transition-colors duration-150', 
                                     row.verdictRunA !== row.verdictRunB ? 'bg-amber-500/5' : '']">
                            <td class="p-3 align-middle">
                                <span class="inline-block bg-user/5 text-user border border-user/10 px-2 py-0.5 rounded text-[9px] font-bold tracking-wider uppercase">
                                    {{ row.moduleName || 'General' }}
                                </span>
                            </td>
                            <td class="p-3 align-middle text-gray-800 dark:text-gray-200 font-medium whitespace-normal break-words">
                                {{ row.inputText }}
                            </td>
                            <td class="p-3 align-middle text-gray-500">
                                🤖 {{ row.modelName }} <span class="text-[9px] opacity-60 block">{{ row.providerName }}</span>
                            </td>
                            <td class="p-3 align-middle text-center">
                                <span :class="getVerdictBadgeClass(row.verdictRunA)">
                                    {{ row.verdictRunA || '-' }}
                                </span>
                            </td>
                            <td class="p-3 align-middle text-center">
                                <span :class="getVerdictBadgeClass(row.verdictRunB)">
                                    {{ row.verdictRunB || '-' }}
                                </span>
                            </td>
                            <td class="p-3 align-middle text-center select-none">
                                <span v-if="row.verdictRunA !== row.verdictRunB" class="inline-block px-1.5 py-0.5 rounded text-[8px] font-extrabold uppercase border bg-amber-500/10 text-amber-500 border-amber-500/20">
                                    Changed
                                </span>
                                <span v-else class="inline-block px-1.5 py-0.5 rounded text-[8px] font-extrabold uppercase border bg-soft text-gray-500 border-soft opacity-45">
                                    Unchanged
                                </span>
                            </td>
                        </tr>
                        <tr v-if="comparisonData.length === 0">
                            <td colspan="6" class="p-8 text-center text-gray-500 opacity-60">No test cases are shared between these runs for comparison.</td>
                        </tr>
                    </tbody>
                </table>
            </div>

        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3.5 border-t border-soft bg-base/30 flex justify-end shrink-0">
            <button @click="isCompareModalOpen = false" class="px-4 py-2 bg-user hover:opacity-90 text-white font-bold rounded-xl text-xs transition cursor-pointer select-none">
                Close
            </button>
        </div>

    </div>
</div>

<!-- Module Performance Modal -->
<div v-if="isModuleModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center z-50 p-4" @click.self="isModuleModalOpen = false">
    <!-- Modal Container -->
    <div class="bg-panel border border-soft rounded-2xl max-w-4xl w-full max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-fade-in">
        
        <!-- Modal Header -->
        <div class="flex justify-between items-center px-6 py-4 border-b border-soft bg-base/30 shrink-0">
            <div class="flex flex-col gap-1 pr-6 min-w-0">
                <span class="text-[10px] font-bold uppercase tracking-wider text-user">Module Execution Statistics</span>
                <h3 class="text-sm font-bold text-gray-800 dark:text-gray-200">
                    🧩 Module Performance for Selected Runs: {{ selectedCompareRuns.join(', ') }}
                </h3>
            </div>
            <button @click="isModuleModalOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition text-2xl font-semibold leading-none cursor-pointer">
                &times;
            </button>
        </div>

        <!-- Modal Body -->
        <div class="flex-1 overflow-y-auto p-6">
            
            <div v-if="moduleLoading" class="flex flex-col items-center justify-center py-20 gap-3">
                <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
                <span class="text-sm opacity-70">Running module analysis...</span>
            </div>

            <div v-else-if="moduleError" class="p-4 bg-red-500/10 text-red-500 rounded-xl border border-red-500/20 text-sm">
                ❌ {{ moduleError }}
            </div>

            <!-- Performance Table -->
            <div v-else class="overflow-x-auto border border-soft rounded-xl bg-panel">
                <table class="w-full text-xs border-collapse">
                    <thead>
                        <tr class="text-left text-gray-500 bg-hover/20 text-[10px] font-semibold uppercase tracking-wider sticky top-0 bg-panel border-b border-soft shadow-[0_1px_0_0_rgba(0,0,0,0.05)] select-none">
                            <th class="p-3">Module Name</th>
                            <th class="w-24 p-3 text-center">Total Cases</th>
                            <th class="w-24 p-3 text-center">Passed</th>
                            <th class="w-24 p-3 text-center">Failed</th>
                            <th class="w-24 p-3 text-center">Errors</th>
                            <th class="w-48 p-3 text-center">Pass Rate</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-soft text-sm">
                        <tr v-for="row in modulePerformanceData" :key="row.moduleName" class="hover:bg-hover/10 transition-colors duration-150">
                            <td class="p-3 align-middle font-bold text-gray-700 dark:text-gray-300">
                                {{ row.moduleName || 'General' }}
                            </td>
                            <td class="p-3 align-middle text-center font-medium">
                                {{ row.totalCases }}
                            </td>
                            <td class="p-3 align-middle text-center text-emerald-500 font-bold">
                                {{ row.passed }}
                            </td>
                            <td class="p-3 align-middle text-center text-rose-500 font-bold">
                                {{ row.failed }}
                            </td>
                            <td class="p-3 align-middle text-center text-amber-500 font-bold">
                                {{ row.errors }}
                            </td>
                            <td class="p-3 align-middle">
                                <div class="flex items-center gap-2 justify-center">
                                    <div class="w-24 bg-soft rounded-full h-1.5 overflow-hidden shrink-0">
                                        <div 
                                            class="bg-gradient-to-r from-emerald-500 to-teal-400 h-full rounded-full transition-all duration-300"
                                            :style="{ width: row.passRatePct + '%' }">
                                        </div>
                                    </div>
                                    <span class="font-extrabold text-xs w-10 text-right">{{ row.passRatePct }}%</span>
                                </div>
                            </td>
                        </tr>
                        <tr v-if="modulePerformanceData.length === 0">
                            <td colspan="6" class="p-8 text-center text-gray-500 opacity-60">No records found.</td>
                        </tr>
                    </tbody>
                </table>
            </div>

        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3.5 border-t border-soft bg-base/30 flex justify-end shrink-0">
            <button @click="isModuleModalOpen = false" class="px-4 py-2 bg-user hover:opacity-90 text-white font-bold rounded-xl text-xs transition cursor-pointer select-none">
                Close
            </button>
        </div>

    </div>
</div>

<!-- LLM Performance Modal -->
<div v-if="isLlmModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center z-50 p-4" @click.self="isLlmModalOpen = false">
    <!-- Modal Container -->
    <div class="bg-panel border border-soft rounded-2xl max-w-5xl w-full max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-fade-in">
        
        <!-- Modal Header -->
        <div class="flex justify-between items-center px-6 py-4 border-b border-soft bg-base/30 shrink-0">
            <div class="flex flex-col gap-1 pr-6 min-w-0">
                <span class="text-[10px] font-bold uppercase tracking-wider text-user">LLM Execution Statistics</span>
                <h3 class="text-sm font-bold text-gray-800 dark:text-gray-200">
                    🤖 LLM Performance for Selected Runs: {{ selectedCompareRuns.join(', ') }}
                </h3>
            </div>
            <button @click="isLlmModalOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition text-2xl font-semibold leading-none cursor-pointer">
                &times;
            </button>
        </div>

        <!-- Modal Body -->
        <div class="flex-1 overflow-y-auto p-6">
            
            <div v-if="llmLoading" class="flex flex-col items-center justify-center py-20 gap-3">
                <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
                <span class="text-sm opacity-70">Running LLM analysis...</span>
            </div>

            <div v-else-if="llmError" class="p-4 bg-red-500/10 text-red-500 rounded-xl border border-red-500/20 text-sm">
                ❌ {{ llmError }}
            </div>

            <!-- Performance Table -->
            <div v-else class="overflow-x-auto border border-soft rounded-xl bg-panel">
                <table class="w-full text-xs border-collapse">
                    <thead>
                        <tr class="text-left text-gray-500 bg-hover/20 text-[10px] font-semibold uppercase tracking-wider sticky top-0 bg-panel border-b border-soft shadow-[0_1px_0_0_rgba(0,0,0,0.05)] select-none">
                            <th class="p-3">Provider</th>
                            <th class="p-3">Model</th>
                            <th class="w-20 p-3 text-center">Cases</th>
                            <th class="w-20 p-3 text-center">Passed</th>
                            <th class="w-20 p-3 text-center">Failed</th>
                            <th class="w-20 p-3 text-center">Errors</th>
                            <th class="w-40 p-3 text-center">Pass Rate</th>
                            <th class="w-28 p-3 text-center">Avg Latency</th>
                            <th class="w-32 p-3 text-center">Prompt Tokens</th>
                            <th class="w-32 p-3 text-center">Completion Tokens</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-soft text-sm">
                        <tr v-for="row in llmPerformanceData" :key="row.providerName + '-' + row.modelName" class="hover:bg-hover/10 transition-colors duration-150">
                            <td class="p-3 align-middle font-semibold text-gray-600 dark:text-gray-400">
                                {{ row.providerName }}
                            </td>
                            <td class="p-3 align-middle font-bold text-gray-800 dark:text-gray-200">
                                {{ row.modelName }}
                            </td>
                            <td class="p-3 align-middle text-center font-medium">
                                {{ row.totalCases }}
                            </td>
                            <td class="p-3 align-middle text-center text-emerald-500 font-bold">
                                {{ row.passed }}
                            </td>
                            <td class="p-3 align-middle text-center text-rose-500 font-bold">
                                {{ row.failed }}
                            </td>
                            <td class="p-3 align-middle text-center text-amber-500 font-bold">
                                {{ row.errors }}
                            </td>
                            <td class="p-3 align-middle">
                                <div class="flex items-center gap-2 justify-center">
                                    <div class="w-20 bg-soft rounded-full h-1.5 overflow-hidden shrink-0">
                                        <div 
                                            class="bg-gradient-to-r from-emerald-500 to-teal-400 h-full rounded-full transition-all duration-300"
                                            :style="{ width: row.passRatePct + '%' }">
                                        </div>
                                    </div>
                                    <span class="font-extrabold text-xs w-10 text-right">{{ row.passRatePct }}%</span>
                                </div>
                            </td>
                            <td class="p-3 align-middle text-center">
                                {{ row.avgLatencyMs.toLocaleString() }} ms
                            </td>
                            <td class="p-3 align-middle text-center">
                                {{ row.totalPromptTokens.toLocaleString() }}
                            </td>
                            <td class="p-3 align-middle text-center">
                                {{ row.totalCompletionTokens.toLocaleString() }}
                            </td>
                        </tr>
                        <tr v-if="llmPerformanceData.length === 0">
                            <td colspan="10" class="p-8 text-center text-gray-500 opacity-60">No records found.</td>
                        </tr>
                    </tbody>
                </table>
            </div>

        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3.5 border-t border-soft bg-base/30 flex justify-end shrink-0">
            <button @click="isLlmModalOpen = false" class="px-4 py-2 bg-user hover:opacity-90 text-white font-bold rounded-xl text-xs transition cursor-pointer select-none">
                Close
            </button>
        </div>

    </div>
</div>

<!-- Case Details Modal -->
<div v-if="isCaseDetailsModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center z-50 p-4" @click.self="isCaseDetailsModalOpen = false">
    <!-- Modal Container -->
    <div class="bg-panel border border-soft rounded-2xl max-w-6xl w-full max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-fade-in">
        
        <!-- Modal Header -->
        <div class="flex justify-between items-center px-6 py-4 border-b border-soft bg-base/30 shrink-0">
            <div class="flex flex-col gap-1 pr-6 min-w-0">
                <span class="text-[10px] font-bold uppercase tracking-wider text-user">
                    {{ isFailureOnlyMode ? 'Failure Analysis Statistics' : 'Detailed Case Execution Log' }}
                </span>
                <h3 class="text-sm font-bold text-gray-800 dark:text-gray-200">
                    {{ isFailureOnlyMode ? '⚠️ Failure Analysis for Selected Runs: ' : '🔍 Show Case Details for Selected Runs: ' }} {{ selectedCompareRuns.join(', ') }}
                </h3>
            </div>
            <button @click="isCaseDetailsModalOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition text-2xl font-semibold leading-none cursor-pointer">
                &times;
            </button>
        </div>

        <!-- Modal Body -->
        <div class="flex-1 overflow-y-auto p-6">
            
            <div v-if="caseDetailsLoading" class="flex flex-col items-center justify-center py-20 gap-3">
                <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
                <span class="text-sm opacity-70">Loading case details...</span>
            </div>

            <div v-else-if="caseDetailsError" class="p-4 bg-red-500/10 text-red-500 rounded-xl border border-red-500/20 text-sm">
                ❌ {{ caseDetailsError }}
            </div>

            <!-- Case Details Table -->
            <div v-else class="overflow-x-auto border border-soft rounded-xl bg-panel">
                <table class="w-full text-xs border-collapse">
                    <thead>
                        <tr class="text-left text-gray-500 bg-hover/20 text-[10px] font-semibold uppercase tracking-wider sticky top-0 bg-panel border-b border-soft shadow-[0_1px_0_0_rgba(0,0,0,0.05)] select-none">
                            <th class="w-12 p-3 text-center">Seq</th>
                            <th class="w-32 p-3">Module</th>
                            <th class="p-3">Input Text</th>
                            <th class="w-40 p-3">Model</th>
                            <th class="w-20 p-3 text-center">Verdict</th>
                            <th class="w-20 p-3 text-center">Latency</th>
                            <th class="w-20 p-3 text-center">Action</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-soft text-sm">
                        <template v-for="(row, idx) in caseDetailsData" :key="idx">
                            <tr class="hover:bg-hover/10 transition-colors duration-150 cursor-pointer" @click="toggleExpandRow(idx)">
                                <td class="p-3 align-middle text-center font-bold text-gray-400">
                                    #{{ row.sequence }}
                                </td>
                                <td class="p-3 align-middle">
                                    <span class="inline-block bg-user/5 text-user border border-user/10 px-2 py-0.5 rounded text-[9px] font-bold tracking-wider uppercase">
                                        {{ row.module || 'General' }}
                                    </span>
                                </td>
                                <td class="p-3 align-middle font-medium text-gray-800 dark:text-gray-200 truncate max-w-xs">
                                    {{ row.inputText }}
                                </td>
                                <td class="p-3 align-middle text-gray-500">
                                    🤖 {{ row.modelName }}
                                    <span class="text-[9px] opacity-60 block">{{ row.providerName }}</span>
                                </td>
                                <td class="p-3 align-middle text-center">
                                    <span :class="getVerdictBadgeClass(row.verdict)">
                                        {{ row.verdict || '-' }}
                                    </span>
                                </td>
                                <td class="p-3 align-middle text-center font-medium">
                                    {{ row.latencyMs ? row.latencyMs + ' ms' : '-' }}
                                </td>
                                <td class="p-3 align-middle text-center select-none">
                                    <button class="text-user hover:underline font-bold text-xs">
                                        {{ expandedRow === idx ? 'Collapse ▲' : 'Expand ▼' }}
                                    </button>
                                </td>
                            </tr>
                            <!-- Expanded detail drawer -->
                            <tr v-if="expandedRow === idx" class="bg-hover/5">
                                <td colspan="7" class="p-4 border-t border-b border-soft">
                                    <div class="grid grid-cols-1 md:grid-cols-2 gap-4 text-xs">
                                        <!-- Left Side: Case Info & Reasoning -->
                                        <div class="flex flex-col gap-3">
                                            <div>
                                                <span class="font-bold text-[10px] uppercase opacity-60 block mb-1">Full Input Question</span>
                                                <p class="text-gray-800 dark:text-gray-200 bg-base/30 p-2.5 rounded-lg border border-soft select-all">
                                                    {{ row.inputText }}
                                                </p>
                                            </div>
                                            <div v-if="row.judgeReasoning">
                                                <span class="font-bold text-[10px] uppercase opacity-60 block mb-1">Judge Reasoning</span>
                                                <p class="text-gray-700 dark:text-gray-300 bg-base/30 p-2.5 rounded-lg border border-soft">
                                                    {{ row.judgeReasoning }}
                                                </p>
                                            </div>
                                            <div v-if="row.differences">
                                                <span class="font-bold text-[10px] uppercase opacity-60 block mb-1 text-rose-500">Differences / Error Details</span>
                                                <pre class="whitespace-pre-wrap text-rose-500 bg-rose-500/5 p-2.5 rounded-lg border border-rose-500/10 font-mono text-[11px]">{{ row.differences }}</pre>
                                            </div>
                                        </div>

                                        <!-- Right Side: SQL & Execution Metadata -->
                                        <div class="flex flex-col gap-3">
                                            <div>
                                                <span class="font-bold text-[10px] uppercase opacity-60 block mb-1">Generated SQL</span>
                                                <pre class="bg-neutral-900 text-neutral-100 p-2.5 rounded-lg font-mono text-[11px] overflow-x-auto border border-neutral-800 select-all">{{ row.generatedSql || '-- No SQL generated' }}</pre>
                                            </div>
                                            <div class="grid grid-cols-2 gap-3 bg-base/20 p-3 rounded-lg border border-soft">
                                                <div>
                                                    <span class="opacity-60 text-[9px] uppercase font-bold block">Confidence Level</span>
                                                    <span class="font-bold text-gray-700 dark:text-gray-300">{{ row.judgeConfidence || '-' }}</span>
                                                </div>
                                                <div>
                                                    <span class="opacity-60 text-[9px] uppercase font-bold block">Answers Question</span>
                                                    <span :class="['font-bold', row.answersQuestion === 1 ? 'text-emerald-500' : 'text-rose-500']">
                                                        {{ row.answersQuestion === 1 ? 'Yes' : 'No' }}
                                                    </span>
                                                </div>
                                                <div>
                                                    <span class="opacity-60 text-[9px] uppercase font-bold block">Was Overridden</span>
                                                    <span :class="['font-bold', row.wasOverridden === 1 ? 'text-amber-500' : 'text-gray-500']">
                                                        {{ row.wasOverridden === 1 ? 'Yes' : 'No' }}
                                                    </span>
                                                </div>
                                                <div>
                                                    <span class="opacity-60 text-[9px] uppercase font-bold block">Execution Time</span>
                                                    <span class="font-bold text-gray-700 dark:text-gray-300">{{ row.latencyMs ? row.latencyMs + ' ms' : '-' }}</span>
                                                </div>
                                                <div class="col-span-2">
                                                    <span class="opacity-60 text-[9px] uppercase font-bold block">Timeline</span>
                                                    <span class="text-gray-500 text-[10px]">
                                                        Start: {{ row.startedAt || '-' }} <br>
                                                        End: {{ row.completedAt || '-' }}
                                                    </span>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </td>
                            </tr>
                        </template>
                        <tr v-if="caseDetailsData.length === 0">
                            <td colspan="7" class="p-8 text-center text-gray-500 opacity-60">No records found.</td>
                        </tr>
                    </tbody>
                </table>
            </div>

        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3.5 border-t border-soft bg-base/30 flex justify-end shrink-0">
            <button @click="isCaseDetailsModalOpen = false" class="px-4 py-2 bg-user hover:opacity-90 text-white font-bold rounded-xl text-xs transition cursor-pointer select-none">
                Close
            </button>
        </div>

    </div>
</div>

<!-- Pass Trend Modal -->
<div v-if="isPassTrendModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-xs flex items-center justify-center z-50 p-4" @click.self="isPassTrendModalOpen = false">
    <!-- Modal Container -->
    <div class="bg-panel border border-soft rounded-2xl max-w-4xl w-full max-h-[90vh] flex flex-col overflow-hidden shadow-2xl animate-fade-in">
        
        <!-- Modal Header -->
        <div class="flex justify-between items-center px-6 py-4 border-b border-soft bg-base/30 shrink-0">
            <div class="flex flex-col gap-1 pr-6 min-w-0">
                <span class="text-[10px] font-bold uppercase tracking-wider text-user">Pass Rate Historical Trend</span>
                <h3 class="text-sm font-bold text-gray-800 dark:text-gray-200">
                    📈 Pass Trend for Selected Runs: {{ selectedCompareRuns.join(', ') }}
                </h3>
            </div>
            <button @click="isPassTrendModalOpen = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200 transition text-2xl font-semibold leading-none cursor-pointer">
                &times;
            </button>
        </div>

        <!-- Modal Body -->
        <div class="flex-1 overflow-y-auto p-6">
            
            <div v-if="passTrendLoading" class="flex flex-col items-center justify-center py-20 gap-3">
                <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
                <span class="text-sm opacity-70">Loading pass trend data...</span>
            </div>

            <div v-else-if="passTrendError" class="p-4 bg-red-500/10 text-red-500 rounded-xl border border-red-500/20 text-sm">
                ❌ {{ passTrendError }}
            </div>

            <!-- Pass Trend Table -->
            <div v-else class="overflow-x-auto border border-soft rounded-xl bg-panel">
                <table class="w-full text-xs border-collapse">
                    <thead>
                        <tr class="text-left text-gray-500 bg-hover/20 text-[10px] font-semibold uppercase tracking-wider sticky top-0 bg-panel border-b border-soft shadow-[0_1px_0_0_rgba(0,0,0,0.05)] select-none">
                            <th class="w-20 p-3 text-center">Run ID</th>
                            <th class="w-40 p-3">Started At</th>
                            <th class="p-3">Module</th>
                            <th class="p-3">Model</th>
                            <th class="w-24 p-3 text-center">Total Cases</th>
                            <th class="w-48 p-3 text-center">Pass Rate</th>
                        </tr>
                    </thead>
                    <tbody class="divide-y divide-soft text-sm">
                        <tr v-for="row in passTrendData" :key="row.evalRunId + '-' + row.module + '-' + row.modelName" class="hover:bg-hover/10 transition-colors duration-150">
                            <td class="p-3 align-middle text-center font-bold text-user">
                                #{{ row.evalRunId }}
                            </td>
                            <td class="p-3 align-middle text-gray-500">
                                {{ row.startedAt }}
                            </td>
                            <td class="p-3 align-middle">
                                <span class="inline-block bg-user/5 text-user border border-user/10 px-2 py-0.5 rounded text-[9px] font-bold tracking-wider uppercase">
                                    {{ row.module || 'General' }}
                                </span>
                            </td>
                            <td class="p-3 align-middle text-gray-700 dark:text-gray-300">
                                🤖 {{ row.modelName }}
                                <span class="text-[9px] opacity-60 block">{{ row.providerName }}</span>
                            </td>
                            <td class="p-3 align-middle text-center font-medium">
                                {{ row.totalCases }}
                            </td>
                            <td class="p-3 align-middle">
                                <div class="flex items-center gap-2 justify-center">
                                    <div class="w-24 bg-soft rounded-full h-1.5 overflow-hidden shrink-0">
                                        <div 
                                            class="bg-gradient-to-r from-emerald-500 to-teal-400 h-full rounded-full transition-all duration-300"
                                            :style="{ width: row.passRatePct + '%' }">
                                        </div>
                                    </div>
                                    <span class="font-extrabold text-xs w-10 text-right">{{ row.passRatePct }}%</span>
                                </div>
                            </td>
                        </tr>
                        <tr v-if="passTrendData.length === 0">
                            <td colspan="6" class="p-8 text-center text-gray-500 opacity-60">No records found.</td>
                        </tr>
                    </tbody>
                </table>
            </div>

        </div>

        <!-- Modal Footer -->
        <div class="px-6 py-3.5 border-t border-soft bg-base/30 flex justify-end shrink-0">
            <button @click="isPassTrendModalOpen = false" class="px-4 py-2 bg-user hover:opacity-90 text-white font-bold rounded-xl text-xs transition cursor-pointer select-none">
                Close
            </button>
        </div>

    </div>
</div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { getDatabases, getDashboardRuns, getDashboardRunDetails, getRunVsRun, getModulePerformance, getLlmPerformance, getCaseDetails, getFailureAnalysis, getPassTrend } from '@/services/api'

const props = defineProps<{
    database: string
}>()

const activeReport = ref('Run Summary')
const loading = ref(false)
const error = ref<string | null>(null)

// Sidebar structure
const reports = [
    { id: 'Run Summary', label: 'Run Summary', icon: '📊' },
    { id: 'Run vs Run', label: 'Run vs Run', icon: '⚖️', disabled: true },
    { id: 'Module Performance', label: 'Module Performance', icon: '🧩', disabled: true },
    { id: 'Model Comparison', label: 'Model Comparison', icon: '🤖', disabled: true },
    { id: 'Case Detail', label: 'Case Detail', icon: '🔍', disabled: true },
    { id: 'Failure Analysis', label: 'Failure Analysis', icon: '⚠️', disabled: true },
    { id: 'Override Analysis', label: 'Override Analysis', icon: '✍️', disabled: true },
    { id: 'Historical Trend', label: 'Historical Trend', icon: '📈', disabled: true },
]

// Backend data store
const dashboardData = ref<any>(null)
const dbList = ref<any[]>([])

// Filter selections
const filterDb = ref(props.database || "")
const filterStatus = ref("")

// Pagination state
const currentPage = ref(1)
const pageSize = 20

// Load databases on mount
async function loadDatabases() {
    try {
        dbList.value = await getDatabases()
    } catch (e) {
        console.error("Failed to load databases list", e)
    }
}

// Fetch dashboard data
async function loadDashboardData() {
    loading.value = true
    error.value = null
    try {
        // Query the backend endpoint
        const res = await getDashboardRuns(filterDb.value, filterStatus.value)
        dashboardData.value = res
        currentPage.value = 1
    } catch (e: any) {
        console.error("Failed to load dashboard data", e)
        error.value = "Failed to load dashboard reports. Please ensure the backend server is running."
    } finally {
        loading.value = false
    }
}

// Summary statistics getters
const summary = computed(() => {
    if (!dashboardData.value || !dashboardData.value.summary) {
        return {
            totalRuns: 0,
            successfulRuns: 0,
            failedRuns: 0,
            failureRate: 0,
            totalCases: 0,
            totalSuccessfulInferences: 0,
            totalFailedInferences: 0,
            failureRateInferences: 0
        }
    }
    return dashboardData.value.summary
})

// Runs list matching current filters
const filteredRuns = computed<any[]>(() => {
    if (!dashboardData.value || !Array.isArray(dashboardData.value.runs)) {
        return []
    }
    return dashboardData.value.runs
})

// Pagination getters
const totalPages = computed(() => {
    return Math.ceil(filteredRuns.value.length / pageSize)
})

const paginationStart = computed(() => {
    if (filteredRuns.value.length === 0) return 0
    return (currentPage.value - 1) * pageSize + 1
})

const paginationEnd = computed(() => {
    const end = currentPage.value * pageSize
    return end > filteredRuns.value.length ? filteredRuns.value.length : end
})

const paginatedRuns = computed(() => {
    const start = (currentPage.value - 1) * pageSize
    const end = start + pageSize
    return filteredRuns.value.slice(start, end)
})

// Computes pass rate
function getPassRatePercent(run: any) {
    if (!run || !run.totalCases) return 0
    const passRate = (run.passCount / run.totalCases) * 100
    return Math.round(passRate)
}

// Returns appropriate classes for status pill
function getStatusBadgeClass(status: string) {
    const base = "px-2.5 py-0.5 rounded-full text-xs font-bold border "
    if (status === 'Completed') {
        return base + "bg-emerald-500/10 text-emerald-500 border-emerald-500/20"
    } else if (status === 'Running') {
        return base + "bg-amber-500/10 text-amber-500 border-amber-500/20"
    } else {
        return base + "bg-rose-500/10 text-rose-500 border-rose-500/20"
    }
}

// Reload data when filters change
watch([filterDb, filterStatus], () => {
    loadDashboardData()
})

// Reload when database prop changes
watch(() => props.database, (newDb) => {
    filterDb.value = newDb || ""
})

onMounted(() => {
    loadDatabases()
    loadDashboardData()
})

const selectedRunId = ref<number | null>(null)
const runDetails = ref<any[] | null>(null)
const detailsLoading = ref(false)

const isModalOpen = ref(false)
const modalData = ref<{
    question: string;
    databaseResult: string;
    runs: any[];
} | null>(null)

async function selectRun(runId: number) {
    selectedRunId.value = runId
    runDetails.value = null
    detailsLoading.value = true
    try {
        const details = await getDashboardRunDetails(runId)
        runDetails.value = details
    } catch (e) {
        console.error("Failed to load run details", e)
        alert("Failed to load run details.")
    } finally {
        detailsLoading.value = false
    }
}

function toggleRun(runId: number) {
    if (selectedRunId.value === runId) {
        closeRunDetails()
    } else {
        selectRun(runId)
    }
}

function closeRunDetails() {
    selectedRunId.value = null
    runDetails.value = null
}

function openDetails(item: any) {
    modalData.value = {
        question: item.question,
        databaseResult: item.databaseResult || '',
        runs: item.runs
    }
    isModalOpen.value = true
}

function isAnswered(val: any): boolean {
    if (val === true || val === 'true' || val === 'Yes' || val === 'yes' || val === 1 || val === '1') {
        return true
    }
    return false
}

function getCaseStatusText(item: any) {
    if (!item.runs || item.runs.length === 0) return "Pending"
    const hasFail = item.runs.some((r: any) => r.comparison !== 'Pass')
    return hasFail ? "Completed: Failed" : "Completed: Pass"
}

function getCaseStatusClass(item: any) {
    const text = getCaseStatusText(item)
    if (text === "Pending") return "bg-hover/40 text-gray-500 border-soft"
    if (text === "Completed: Failed") return "bg-red-500/10 text-red-500 border-red-500/20"
    return "bg-green-500/10 text-green-600 border-green-500/20"
}

const selectedCompareRuns = ref<number[]>([])
const isCompareModalOpen = ref(false)
const compareLoading = ref(false)
const compareError = ref<string | null>(null)
const comparisonData = ref<any[]>([])

async function openRunComparisonModal() {
    if (selectedCompareRuns.value.length !== 2) return
    isCompareModalOpen.value = true
    compareLoading.value = true
    compareError.value = null
    comparisonData.value = []
    try {
        const data = await getRunVsRun(selectedCompareRuns.value[0]!, selectedCompareRuns.value[1]!)
        comparisonData.value = data
    } catch (e: any) {
        console.error("Failed to load run comparison", e)
        compareError.value = "Failed to load run comparisons. Make sure both runs are complete."
    } finally {
        compareLoading.value = false
    }
}

function getVerdictBadgeClass(verdict: string) {
    const base = "px-1.5 py-0.5 rounded text-[9px] font-extrabold uppercase border "
    if (verdict === 'Pass') {
        return base + "bg-emerald-500/10 text-emerald-500 border-emerald-500/20"
    } else if (verdict === 'Failed' || verdict === 'Fail') {
        return base + "bg-rose-500/10 text-rose-500 border-rose-500/20"
    }
    return base + "bg-soft text-gray-500 border-soft"
}

// Clear selected comparison runs when data reloads
watch(filteredRuns, () => {
    selectedCompareRuns.value = []
})

const isModuleModalOpen = ref(false)
const moduleLoading = ref(false)
const moduleError = ref<string | null>(null)
const modulePerformanceData = ref<any[]>([])

async function openModulePerformanceModal() {
    if (selectedCompareRuns.value.length === 0) return
    isModuleModalOpen.value = true
    moduleLoading.value = true
    moduleError.value = null
    modulePerformanceData.value = []
    try {
        const data = await getModulePerformance(selectedCompareRuns.value)
        modulePerformanceData.value = data
    } catch (e: any) {
        console.error("Failed to load module performance", e)
        moduleError.value = "Failed to load module performance metrics."
    } finally {
        moduleLoading.value = false
    }
}

const isLlmModalOpen = ref(false)
const llmLoading = ref(false)
const llmError = ref<string | null>(null)
const llmPerformanceData = ref<any[]>([])

async function openLlmPerformanceModal() {
    if (selectedCompareRuns.value.length === 0) return
    isLlmModalOpen.value = true
    llmLoading.value = true
    llmError.value = null
    llmPerformanceData.value = []
    try {
        const data = await getLlmPerformance(selectedCompareRuns.value)
        llmPerformanceData.value = data
    } catch (e: any) {
        console.error("Failed to load LLM performance", e)
        llmError.value = "Failed to load LLM performance metrics."
    } finally {
        llmLoading.value = false
    }
}

const isCaseDetailsModalOpen = ref(false)
const caseDetailsLoading = ref(false)
const caseDetailsError = ref<string | null>(null)
const caseDetailsData = ref<any[]>([])
const expandedRow = ref<number | null>(null)
const isFailureOnlyMode = ref(false)

async function openCaseDetailsModal() {
    if (selectedCompareRuns.value.length === 0) return
    isFailureOnlyMode.value = false
    isCaseDetailsModalOpen.value = true
    caseDetailsLoading.value = true
    caseDetailsError.value = null
    caseDetailsData.value = []
    expandedRow.value = null
    try {
        const data = await getCaseDetails(selectedCompareRuns.value)
        caseDetailsData.value = data
    } catch (e: any) {
        console.error("Failed to load case details", e)
        caseDetailsError.value = "Failed to load case details."
    } finally {
        caseDetailsLoading.value = false
    }
}

async function openFailureAnalysisModal() {
    if (selectedCompareRuns.value.length === 0) return
    isFailureOnlyMode.value = true
    isCaseDetailsModalOpen.value = true
    caseDetailsLoading.value = true
    caseDetailsError.value = null
    caseDetailsData.value = []
    expandedRow.value = null
    try {
        const data = await getFailureAnalysis(selectedCompareRuns.value)
        caseDetailsData.value = data
    } catch (e: any) {
        console.error("Failed to load failure analysis details", e)
        caseDetailsError.value = "Failed to load failure analysis details."
    } finally {
        caseDetailsLoading.value = false
    }
}

function toggleExpandRow(idx: number) {
    if (expandedRow.value === idx) {
        expandedRow.value = null
    } else {
        expandedRow.value = idx
    }
}

const isPassTrendModalOpen = ref(false)
const passTrendLoading = ref(false)
const passTrendError = ref<string | null>(null)
const passTrendData = ref<any[]>([])

async function openPassTrendModal() {
    if (selectedCompareRuns.value.length === 0) return
    isPassTrendModalOpen.value = true
    passTrendLoading.value = true
    passTrendError.value = null
    passTrendData.value = []
    try {
        const data = await getPassTrend(selectedCompareRuns.value)
        passTrendData.value = data
    } catch (e: any) {
        console.error("Failed to load pass trend", e)
        passTrendError.value = "Failed to load pass trend metrics."
    } finally {
        passTrendLoading.value = false
    }
}

</script>

<style scoped>
/* Glassmorphic border classes custom styling */
.border-soft {
    border-color: var(--border-color, rgba(229, 231, 235, 0.1));
}
</style>
