<template>
<div class="p-6 max-w-[1600px] mx-auto flex flex-col h-[calc(100vh-100px)] overflow-hidden">
  
  <!-- HEADER BANNER -->
  <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-soft shrink-0">
    <div class="flex items-center gap-3">
      <router-link 
        :to="`/admin/databases`"
        class="p-2 rounded-xl border border-soft hover:bg-hover transition shrink-0"
        title="Back to Databases"
      >
        ⬅️
      </router-link>
      <div>
        <span class="text-[10px] font-bold tracking-wider uppercase opacity-50">Database Schema Manager</span>
        <h2 class="text-2xl font-bold flex items-center gap-2">
          <span>🗃️</span> {{ props.database }}
        </h2>
      </div>
    </div>
    
    <!-- SAVE BUTTON -->
    <div class="flex items-center gap-3">
      <div v-if="notification" :class="notificationClass">
        {{ notification.message }}
      </div>
      
      <button
        @click="saveConfiguration"
        :disabled="saving"
        class="px-5 py-2.5 rounded-xl bg-user text-white shadow-md shadow-blue-500/10 hover:opacity-95 disabled:opacity-50 disabled:cursor-not-allowed transition flex items-center gap-2 text-sm font-semibold"
      >
        <span v-if="saving" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        <span>💾 Save Configuration</span>
      </button>
    </div>
  </div>

  <!-- MAIN TWO-COLUMN CONTENT -->
  <div v-if="loading" class="flex-1 flex flex-col items-center justify-center gap-3">
    <span class="w-12 h-12 border-4 border-user border-t-transparent rounded-full animate-spin"></span>
    <span class="text-sm opacity-70">Loading database schema...</span>
  </div>

  <div v-else class="flex-1 grid grid-cols-1 lg:grid-cols-2 gap-6 pt-6 overflow-hidden min-h-0">
    
    <!-- LEFT PANEL: TABLE / COLUMN CURATION -->
    <div class="flex flex-col h-full overflow-hidden border border-soft rounded-2xl bg-panel">
      <div class="p-4 border-b border-soft shrink-0 flex items-center justify-between gap-4">
        <h3 class="font-bold text-base flex items-center gap-2">
          📊 AI Table & Column Exposure
        </h3>
        <input 
          v-model="searchQuery"
          type="text"
          placeholder="Search tables..."
          class="w-48 sm:w-64 bg-base border border-soft rounded-xl px-3 py-1.5 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition"
        />
      </div>

      <!-- Table Controls Toolbar -->
      <div class="px-4 py-2 border-b border-soft bg-panel/30 flex flex-wrap items-center justify-between gap-3 text-xs shrink-0 select-none">
        <div class="flex items-center gap-2">
          <button 
            @click="selectAllTables"
            class="px-2 py-1 rounded bg-hover hover:bg-hover/80 text-[11px] font-semibold text-user transition"
            title="Expose all visible tables"
          >
            ☑️ Select All
          </button>
          <button 
            @click="deselectAllTables"
            class="px-2 py-1 rounded bg-hover hover:bg-hover/80 text-[11px] font-semibold opacity-80 hover:opacity-100 transition"
            title="Hide all visible tables"
          >
            ⬜ Deselect All
          </button>
        </div>

        <label class="flex items-center gap-2 cursor-pointer font-medium hover:text-user transition">
          <input 
            type="checkbox"
            v-model="tableShowCuratedOnly"
            class="w-3.5 h-3.5 rounded border-soft text-user focus:ring-user cursor-pointer"
          />
          <span>Show Curated Only</span>
        </label>
      </div>

      <div class="flex-1 overflow-auto p-4 space-y-1">
        <div 
          v-for="table in filteredTables" 
          :key="table"
          class="border border-soft rounded-xl bg-base overflow-hidden transition"
          :class="{ 'ring-1 ring-user/30': expandedTable === table }"
        >
          <!-- Table Row Header -->
          <div class="flex items-center justify-between py-2 px-3.5 select-none bg-panel/30 hover:bg-hover/20 transition">
            <div class="flex items-center gap-3 min-w-0 flex-1">
              <input 
                type="checkbox"
                v-model="curatedTablesMap[table]!.curated"
                @change="toggleTableCuration(table)"
                class="w-4 h-4 rounded border-soft text-user focus:ring-user"
              />
              <span 
                @click="onTableExpand(table)"
                class="font-semibold text-sm cursor-pointer truncate flex-1 hover:text-user"
              >
                {{ table }}
              </span>
            </div>
            
            <button 
              @click="onTableExpand(table)"
              class="p-1 hover:bg-hover rounded-lg transition"
            >
              <svg 
                class="w-4 h-4 transform transition-transform duration-200" 
                :class="{ 'rotate-180': expandedTable === table }"
                fill="none" stroke="currentColor" viewBox="0 0 24 24"
              >
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"></path>
              </svg>
            </button>
          </div>

          <!-- Table Curation Details (Expanded) -->
          <div v-if="expandedTable === table" class="p-4 border-t border-soft space-y-4 bg-panel/10">
            <!-- Table Details / Description -->
            <div>
              <label class="block text-[11px] font-bold uppercase opacity-55 mb-1.5">
                Table Details / Purpose (Exposed to AI)
              </label>
              <textarea
                v-model="curatedTablesMap[table]!.description"
                placeholder="e.g. Master list of store customers, contains contact details and credit limits."
                rows="2"
                class="w-full bg-base border border-soft rounded-xl p-3 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition"
              ></textarea>
            </div>

            <!-- Column Selection List -->
            <div>
              <div class="flex justify-between items-center mb-2">
                <span class="text-[11px] font-bold uppercase opacity-55">
                  Column Metadata
                </span>
                <span class="text-[10px] opacity-60">
                  Select which columns the AI can view and describe their purpose
                </span>
              </div>

              <!-- Columns Loader -->
              <div v-if="tableColumns[table]?.loading" class="py-6 flex justify-center">
                <span class="w-6 h-6 border-2 border-user border-t-transparent rounded-full animate-spin"></span>
              </div>
              
              <!-- Columns Error -->
              <div v-else-if="tableColumns[table]?.error" class="p-3 bg-red-500/10 text-red-500 rounded-xl text-xs">
                Error loading columns: {{ tableColumns[table].error }}
              </div>

              <!-- Columns list -->
              <div v-else class="space-y-2.5">
                <!-- Column Controls Toolbar -->
                <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-panel/30 border border-soft p-3 rounded-xl text-xs">
                  <!-- Column Search Input -->
                  <div class="relative flex-1 min-w-[200px]">
                    <span class="absolute left-3 top-1/2 -translate-y-1/2 opacity-50">🔍</span>
                    <input 
                      v-model="columnSearch[table]"
                      type="text"
                      placeholder="Search columns by name or description..."
                      class="w-full bg-base border border-soft rounded-lg pl-8 pr-3 py-1.5 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition"
                    />
                  </div>

                  <!-- Select All / Deselect All / Filter Exposed -->
                  <div class="flex flex-wrap items-center gap-4 select-none">
                    <div class="flex items-center gap-2 border-r border-soft pr-3 mr-1">
                      <button 
                        @click="selectAllColumns(table)"
                        class="px-2 py-1 rounded bg-hover hover:bg-hover/80 text-[11px] font-semibold text-user transition"
                        title="Select all visible columns"
                      >
                        ☑️ Select All
                      </button>
                      <button 
                        @click="deselectAllColumns(table)"
                        class="px-2 py-1 rounded bg-hover hover:bg-hover/80 text-[11px] font-semibold opacity-80 hover:opacity-100 transition"
                        title="Deselect all visible columns"
                      >
                        ⬜ Deselect All
                      </button>
                    </div>

                    <label class="flex items-center gap-2 cursor-pointer font-medium hover:text-user transition">
                      <input 
                        type="checkbox"
                        v-model="columnExposedOnly[table]"
                        class="w-3.5 h-3.5 rounded border-soft text-user focus:ring-user cursor-pointer"
                      />
                      <span>Show Curated Only</span>
                    </label>
                  </div>
                </div>

                <!-- Column Table -->
                <div class="border border-soft rounded-xl overflow-hidden bg-base text-xs max-h-[400px] overflow-y-auto">
                  <table class="w-full border-collapse">
                    <thead class="sticky top-0 bg-panel border-b border-soft z-10">
                      <tr class="text-left">
                        <th class="p-2.5 w-16 text-center">Expose</th>
                        <th class="p-2.5 w-1/3">Column Name</th>
                        <th class="p-2.5">AI Synonyms & Descriptions</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr 
                        v-for="col in getFilteredColumns(table)" 
                        :key="col"
                        class="border-b border-soft last:border-b-0 hover:bg-hover/10 transition-colors duration-150"
                      >
                        <td class="p-2 text-center">
                          <input 
                            type="checkbox"
                            v-model="columnMetadata[table]![col]!.exposed"
                            class="w-3.5 h-3.5 rounded border-soft text-user focus:ring-user"
                          />
                        </td>
                        <td class="p-2 font-mono text-xs font-semibold">
                          {{ col }}
                        </td>
                        <td class="p-1.5">
                          <input 
                            type="text"
                            v-model="columnMetadata[table]![col]!.description"
                            placeholder="e.g. Unique client identifier, customer number"
                            class="w-full bg-base border border-soft rounded-lg px-2 py-1 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition"
                          />
                        </td>
                      </tr>
                      <tr v-if="getFilteredColumns(table).length === 0">
                        <td colspan="3" class="text-center py-6 opacity-50">
                          No columns found matching the filters.
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="filteredTables.length === 0" class="text-center py-10 text-xs opacity-50">
          No tables found matching "{{ searchQuery }}".
        </div>
      </div>
    </div>

    <!-- RIGHT PANEL: JOIN MANAGEMENT -->
    <div class="flex flex-col h-full overflow-hidden space-y-6">
      
      <!-- ACTIVE JOINS LIST -->
      <div 
        class="flex flex-col overflow-hidden border border-soft rounded-2xl bg-panel transition-all duration-300"
        :class="activeJoinsExpanded ? 'flex-1' : 'shrink-0'"
      >
        <div class="p-4 border-b border-soft shrink-0 flex items-center justify-between">
          <div 
            @click="activeJoinsExpanded = !activeJoinsExpanded" 
            class="flex items-center gap-2 cursor-pointer select-none group"
          >
            <h3 class="font-bold text-base flex items-center gap-2 group-hover:text-user transition">
              🔗 Active Joins ({{ curatedJoins.length }})
            </h3>
            <svg 
              class="w-4 h-4 transform transition-transform duration-200 opacity-60 group-hover:opacity-100" 
              :class="{ 'rotate-180': !activeJoinsExpanded }"
              fill="none" stroke="currentColor" viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"></path>
            </svg>
          </div>
          <button 
            @click="openModalForCreate"
            class="px-3 py-1.5 rounded-xl bg-hover border border-soft hover:bg-hover/80 text-xs font-semibold transition text-user flex items-center gap-1.5 cursor-pointer"
            title="Create new join group"
          >
            <span>➕ Add Custom Join</span>
          </button>
        </div>

        <div v-show="activeJoinsExpanded" class="flex-1 flex flex-col overflow-hidden min-h-0">
          <!-- Active Joins Search Toolbar -->
          <div class="px-4 py-2 border-b border-soft bg-panel/30 flex items-center justify-between gap-3 text-xs shrink-0 select-none">
            <div class="relative flex-1">
              <span class="absolute left-3 top-1/2 -translate-y-1/2 opacity-50">🔍</span>
              <input 
                v-model="activeJoinsSearch"
                type="text"
                placeholder="Search active joins by description or condition..."
                class="w-full bg-base border border-soft rounded-lg pl-8 pr-3 py-1.5 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition"
              />
            </div>
          </div>

          <div class="flex-1 overflow-auto p-4 space-y-3">
            <div 
              v-for="join in filteredActiveJoins" 
              :key="join.originalIndex"
              class="p-4 border border-soft rounded-xl bg-base flex justify-between items-start gap-4 hover:shadow-sm transition"
            >
              <div class="min-w-0 flex-1 space-y-2">
                <div 
                  @click="openModalForEdit(join.originalIndex)"
                  class="font-semibold text-xs text-user uppercase tracking-wider cursor-pointer hover:underline hover:opacity-80 transition inline-block"
                  title="Click to edit this join group in modal"
                >
                  {{ join.Description }}
                </div>
                <div class="flex flex-wrap gap-1.5">
                  <div 
                    v-for="(cond, cIdx) in getFilteredConditions(join.JoinConditions)" 
                    :key="cIdx"
                    class="inline-flex items-center gap-2 font-mono text-[11px] bg-panel border border-soft rounded px-2.5 py-0.5"
                  >
                    <span>{{ cond }}</span>
                  </div>
                </div>
              </div>
              
              <button
                @click="deleteJoin(join.originalIndex)"
                class="p-1.5 rounded-lg text-red-500 bg-red-500/10 hover:bg-red-500/20 transition self-start shrink-0 cursor-pointer"
                title="Delete entire join group"
              >
                🗑️
              </button>
            </div>

            <div v-if="filteredActiveJoins.length === 0" class="text-center py-12 text-xs opacity-50">
              <span v-if="activeJoinsSearch.trim()">No active joins found matching "{{ activeJoinsSearch }}".</span>
              <span v-else>No joins defined yet. Expose tables on the left and define joins here.</span>
            </div>
          </div>
        </div>
      </div>

      <!-- AUTO-DISCOVERED JOINS -->
      <div 
        class="flex flex-col overflow-hidden border border-soft rounded-2xl bg-panel transition-all duration-300"
        :class="autoDiscoveredExpanded ? (activeJoinsExpanded ? 'h-64 shrink-0' : 'flex-1') : 'shrink-0'"
      >
        <div 
          @click="autoDiscoveredExpanded = !autoDiscoveredExpanded"
          class="p-4 border-b border-soft shrink-0 flex items-center justify-between cursor-pointer select-none group"
        >
          <h3 class="font-bold text-sm flex items-center gap-2 group-hover:text-user transition">
            💡 Auto-discovered Relationships ({{ autoDiscoveredJoins.length }})
          </h3>
          <svg 
            class="w-4 h-4 transform transition-transform duration-200 opacity-60 group-hover:opacity-100" 
            :class="{ 'rotate-180': !autoDiscoveredExpanded }"
            fill="none" stroke="currentColor" viewBox="0 0 24 24"
          >
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M19 9l-7 7-7-7"></path>
          </svg>
        </div>
        <div v-show="autoDiscoveredExpanded" class="flex-1 overflow-auto p-4 space-y-2">
          <div 
            v-for="(j, idx) in autoDiscoveredJoins" 
            :key="idx"
            class="p-3 border border-soft rounded-xl bg-base flex justify-between items-center gap-4 hover:bg-hover/5 transition"
          >
            <div class="min-w-0 flex-1">
              <div class="font-semibold text-xs opacity-85 mb-1">
                {{ j.description }}
              </div>
              <div class="flex flex-wrap gap-1 font-mono text-[10px] opacity-70">
                <span 
                  v-for="(cond, cIdx) in j.conditions" 
                  :key="cIdx"
                  class="bg-panel border border-soft px-1.5 py-0.5 rounded"
                >
                  {{ cond }}
                </span>
              </div>
            </div>

            <button
              @click="addAutoDiscoverJoin(j)"
              class="px-2.5 py-1.5 rounded-lg bg-user text-white text-[11px] font-semibold hover:opacity-90 shrink-0 transition cursor-pointer"
            >
              + Add
            </button>
          </div>
          <div v-if="autoDiscoveredJoins.length === 0" class="text-center py-10 text-xs opacity-50 font-medium">
            No autogenerated joins available or all the auto generated joins are active
          </div>
        </div>
      </div>

    </div>

    <!-- EDIT/CREATE JOIN GROUP MODAL -->
    <div v-if="showModal" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-panel border border-soft rounded-2xl max-w-2xl w-full max-h-[90vh] flex flex-col shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
        
        <!-- Modal Header -->
        <div class="p-4 border-b border-soft flex items-center justify-between bg-panel/30">
          <h3 class="font-bold text-base flex items-center gap-2">
            <span>{{ editingJoinIndex !== null ? '✏️ Edit Join Group' : '➕ Create Custom Join Group' }}</span>
          </h3>
          <button 
            @click="closeModal" 
            class="p-1 hover:bg-hover rounded-lg transition text-lg leading-none cursor-pointer"
          >
            ✕
          </button>
        </div>

        <!-- Modal Body -->
        <div class="p-6 overflow-y-auto space-y-6 flex-1 min-h-0">
          
          <!-- Join Group Description (Textbox) -->
          <div class="space-y-2">
            <label class="block text-xs font-bold uppercase opacity-55">Join Group Description (Header)</label>
            <input 
              v-model="modalJoinDescription"
              type="text"
              placeholder="e.g. Track joins"
              class="w-full bg-base border border-soft rounded-xl px-3 py-2 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition"
            />
          </div>

          <!-- Add New Condition Form (Top) -->
          <div class="border border-soft rounded-xl p-4 bg-base/50 space-y-4">
            <h4 class="text-xs font-bold uppercase opacity-55 flex items-center gap-2">
              <span>➕ Add New Condition to Group</span>
            </h4>

            <!-- Table Selectors -->
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-[10px] font-bold uppercase opacity-55 mb-1">Table A</label>
                <select
                  v-model="customJoin.tableA"
                  class="w-full bg-base border border-soft rounded-xl px-3 py-2 text-xs focus:outline-none focus:border-user transition"
                >
                  <option value="">-- Select --</option>
                  <option 
                    v-for="t in curatedTablesList" 
                    :key="t" 
                    :value="t"
                  >
                    {{ t }}
                  </option>
                </select>
              </div>
              <div>
                <label class="block text-[10px] font-bold uppercase opacity-55 mb-1">Table B</label>
                <select
                  v-model="customJoin.tableB"
                  class="w-full bg-base border border-soft rounded-xl px-3 py-2 text-xs focus:outline-none focus:border-user transition"
                >
                  <option value="">-- Select --</option>
                  <option 
                    v-for="t in curatedTablesList" 
                    :key="t" 
                    :value="t"
                  >
                    {{ t }}
                  </option>
                </select>
              </div>
            </div>

            <!-- Conditions Builder -->
            <div v-if="customJoin.tableA && customJoin.tableB" class="space-y-2">
              <div class="text-[10px] font-bold uppercase opacity-55">
                Conditions ({{ customJoin.conditions.length }})
              </div>
              
              <div 
                v-for="(c, cIdx) in customJoin.conditions" 
                :key="cIdx"
                class="flex items-center gap-2"
              >
                <select
                  v-model="c.colA"
                  class="flex-1 bg-base border border-soft rounded-lg px-2 py-1.5 text-xs focus:outline-none transition font-mono"
                >
                  <option value="">-- {{ customJoin.tableA }} Columns --</option>
                  <option 
                    v-for="col in tableColumns[customJoin.tableA]?.all" 
                    :key="col" 
                    :value="col"
                  >
                    {{ col }}
                  </option>
                </select>
                
                <span class="text-xs opacity-50">=</span>
                
                <!-- Table B Selector or Custom Value Input -->
                <div class="flex-1 flex gap-1 items-center">
                  <select
                    v-if="!c.isCustomB"
                    v-model="c.colB"
                    class="flex-1 bg-base border border-soft rounded-lg px-2 py-1.5 text-xs focus:outline-none transition font-mono"
                  >
                    <option value="">-- {{ customJoin.tableB }} Columns --</option>
                    <option 
                      v-for="col in tableColumns[customJoin.tableB]?.all" 
                      :key="col" 
                      :value="col"
                    >
                      {{ col }}
                    </option>
                  </select>
                  <input
                    v-else
                    v-model="c.customValB"
                    type="text"
                    placeholder="e.g. '17' or ObjType"
                    class="flex-1 bg-base border border-soft rounded-lg px-2 py-1.5 text-xs focus:outline-none focus:border-user focus:ring-1 focus:ring-user transition font-mono"
                  />
                  
                  <button
                    @click="c.isCustomB = !c.isCustomB"
                    class="px-2 py-1.5 rounded hover:bg-hover border border-soft text-[11px] cursor-pointer"
                    title="Toggle column vs custom text"
                  >
                    {{ c.isCustomB ? '📋' : '✍️' }}
                  </button>
                </div>
                
                <button
                  @click="removeCustomCondition(cIdx)"
                  :disabled="customJoin.conditions.length === 1"
                  class="p-1 rounded hover:bg-hover disabled:opacity-30 text-red-500 text-xs transition cursor-pointer"
                  title="Remove condition"
                >
                  ✖️
                </button>
              </div>

              <button
                @click="addCustomCondition"
                class="text-xs text-user hover:underline font-semibold flex items-center gap-1 mt-1 cursor-pointer"
              >
                ➕ Add Join Field Condition
              </button>
            </div>

            <div class="flex justify-end">
              <button
                @click="addConditionToModalGroup"
                :disabled="!customJoin.tableA || !customJoin.tableB"
                class="px-4 py-2 rounded-xl bg-user text-white hover:opacity-90 disabled:opacity-50 disabled:cursor-not-allowed text-xs font-semibold transition cursor-pointer"
              >
                ➕ Add Condition to List
              </button>
            </div>
          </div>

          <!-- Existing Conditions in Group (Bottom) -->
          <div class="space-y-2">
            <h4 class="text-xs font-bold uppercase opacity-55">
              🔗 Conditions in this Group ({{ modalJoinConditions.length }})
            </h4>
            
            <div class="border border-soft rounded-xl overflow-hidden bg-base text-xs divide-y divide-soft">
              <div 
                v-for="(cond, cIdx) in modalJoinConditions" 
                :key="cIdx"
                class="p-3 flex justify-between items-center hover:bg-hover/10 transition"
              >
                <span class="font-mono text-xs">{{ cond }}</span>
                <button 
                  @click="deleteModalGroupCondition(cIdx)"
                  class="px-2 py-1 rounded text-red-500 bg-red-500/10 hover:bg-red-500/20 text-[10px] font-semibold transition cursor-pointer"
                  title="Delete this condition"
                >
                  ✕ Delete
                </button>
              </div>
              <div v-if="modalJoinConditions.length === 0" class="text-center py-8 opacity-50">
                No conditions in this group yet. Select tables and fields above to add one.
              </div>
            </div>
          </div>

        </div>

        <!-- Modal Footer -->
        <div class="p-4 border-t border-soft bg-panel/30 flex justify-end gap-2 shrink-0">
          <button
            @click="closeModal"
            class="px-4 py-2 rounded-xl border border-soft hover:bg-hover text-xs font-semibold transition cursor-pointer"
          >
            Cancel
          </button>
          <button
            @click="saveModalGroup"
            class="px-4 py-2 rounded-xl bg-user text-white hover:opacity-90 text-xs font-semibold transition cursor-pointer"
          >
            Save Group
          </button>
        </div>

      </div>
    </div>

  </div>
</div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import {
  getDatabaseTables,
  getTableColumns,
  getTableForeignKeys,
  getCuratedTables,
  saveCuratedTables,
  getCuratedTableJoins,
  saveCuratedTableJoins,
  type CuratedTable,
  type CuratedTableJoin
} from './schemaApi'

const props = defineProps<{
  database: string
}>()

const router = useRouter()

// App Curation State
const dbTables = ref<string[]>([])
const curatedTables = ref<CuratedTable[]>([])
const curatedJoins = ref<CuratedTableJoin[]>([])

// Loading states
const loading = ref(true)
const saving = ref(false)
const searchQuery = ref('')
const expandedTable = ref<string | null>(null)

// Column filter/search states per table
const columnSearch = ref<Record<string, string>>({})
const columnExposedOnly = ref<Record<string, boolean>>({})

// Notification states
const notification = ref<{ message: string; type: 'success' | 'error' } | null>(null)

// UI curation mappings
// Maps table name -> { curated: boolean, description: string }
const curatedTablesMap = ref<Record<string, { curated: boolean; description: string }>>({})

// Maps table name -> { allColumns: string[], loading, error }
const tableColumns = ref<Record<string, { all: string[]; loading: boolean; error?: string }>>({})

// Maps table name -> { referencedTable -> { currentTableCol -> referencedTableCol } }
const tableForeignKeys = ref<Record<string, { all: Record<string, Record<string, string>>; loading: boolean }>>({})

// Column curation detail: { [table]: { [column]: { exposed: boolean, description: string } } }
const columnMetadata = ref<Record<string, Record<string, { exposed: boolean; description: string }>>>({})

// Custom Join Builder state (used within modal)
const customJoin = ref({
  description: '',
  tableA: '',
  tableB: '',
  conditions: [{ colA: '', colB: '', isCustomB: false, customValB: '' }]
})

const editingJoinIndex = ref<number | null>(null)

// Modal Visibility and Data refs
const showModal = ref(false)
const modalJoinDescription = ref("")
const modalJoinConditions = ref<string[]>([])

// Panel expansion states
const activeJoinsExpanded = ref(true)
const autoDiscoveredExpanded = ref(true)

// Active Joins Search state
const activeJoinsSearch = ref('')

const filteredActiveJoins = computed(() => {
  const query = activeJoinsSearch.value.trim().toLowerCase()
  return curatedJoins.value
    .map((join, originalIndex) => {
      const Description = join.Description ?? (join as any).description ?? ''
      const JoinConditions = join.JoinConditions ?? (join as any).joinConditions ?? []
      return { Description, JoinConditions, originalIndex }
    })
    .filter(join => {
      if (!query) return true
      const matchDesc = join.Description.toLowerCase().includes(query)
      const matchCond = join.JoinConditions.some(cond => cond.toLowerCase().includes(query))
      return matchDesc || matchCond
    })
})

// Compute curated tables list
const curatedTablesList = computed(() => {
  return dbTables.value.filter(t => curatedTablesMap.value[t]?.curated)
})

// Computed notifications styling
const notificationClass = computed(() => {
  if (!notification.value) return ''
  return [
    'px-3 py-1.5 rounded-xl text-xs font-medium border transition duration-300',
    notification.value.type === 'success' 
      ? 'bg-green-500/10 border-green-500/20 text-green-600 dark:text-green-400' 
      : 'bg-red-500/10 border-red-500/20 text-red-600 dark:text-red-400'
  ]
})

function getFilteredConditions(conditions: string[]): string[] {
  const query = activeJoinsSearch.value.trim().toLowerCase()
  if (!query) return conditions
  return conditions.filter(cond => cond && cond.toLowerCase().includes(query))
}

// Helpers to parse columns details
function parseColumnString(colStr: string): { name: string; description: string } {
  const match = colStr.match(/^([^(]+)(?:\(([^)]+)\))?$/)
  if (match) {
    const name = (match[1] || '').trim()
    const description = match[2] ? match[2].trim() : ''
    return { name, description }
  }
  return { name: colStr.trim(), description: '' }
}

function formatColumnString(name: string, description: string): string {
  if (description && description.trim()) {
    return `${name} (${description.trim()})`
  }
  return name
}

function splitColumns(colStr: string): string[] {
  const result: string[] = []
  let current = ''
  let parenDepth = 0
  for (let i = 0; i < colStr.length; i++) {
    const char = colStr[i]
    if (char === '(') parenDepth++
    else if (char === ')') parenDepth--
    
    if (char === ',' && parenDepth === 0) {
      result.push(current.trim())
      current = ''
    } else {
      current += char
    }
  }
  if (current.trim()) {
    result.push(current.trim())
  }
  return result
}

// Watch table A and B to load columns if needed
watch(() => customJoin.value.tableA, (newVal) => {
  if (newVal) loadTableColumns(newVal)
})

watch(() => customJoin.value.tableB, (newVal) => {
  if (newVal) loadTableColumns(newVal)
})

// Table search filter state
const tableShowCuratedOnly = ref(false)

// Filtered tables search query
const filteredTables = computed(() => {
  let tables = dbTables.value
  if (searchQuery.value.trim()) {
    const query = searchQuery.value.toLowerCase()
    tables = tables.filter(t => t.toLowerCase().includes(query))
  }
  if (tableShowCuratedOnly.value) {
    tables = tables.filter(t => curatedTablesMap.value[t]?.curated)
  }
  return tables
})

// Select all visible tables
function selectAllTables() {
  filteredTables.value.forEach(table => {
    if (curatedTablesMap.value[table]) {
      curatedTablesMap.value[table].curated = true
      toggleTableCuration(table)
    }
  })
}

// Deselect all visible tables
function deselectAllTables() {
  filteredTables.value.forEach(table => {
    if (curatedTablesMap.value[table]) {
      curatedTablesMap.value[table].curated = false
    }
  })
}

// Filtered columns for a given table
function getFilteredColumns(tableName: string): string[] {
  const cols = tableColumns.value[tableName]?.all || []
  const query = (columnSearch.value[tableName] || '').trim().toLowerCase()
  const exposedOnly = !!columnExposedOnly.value[tableName]

  return cols.filter(col => {
    const meta = columnMetadata.value[tableName]?.[col]
    const matchesSearch = !query || 
      col.toLowerCase().includes(query) || 
      (meta?.description || '').toLowerCase().includes(query)
    
    const matchesExposed = !exposedOnly || !!meta?.exposed
    
    return matchesSearch && matchesExposed
  })
}

// Select all visible columns for a table
function selectAllColumns(tableName: string) {
  const cols = getFilteredColumns(tableName)
  if (!columnMetadata.value[tableName]) {
    columnMetadata.value[tableName] = {}
  }
  const meta = columnMetadata.value[tableName]
  if (meta) {
    cols.forEach(col => {
      if (!meta[col]) {
        meta[col] = { exposed: true, description: '' }
      } else {
        meta[col].exposed = true
      }
    })
  }
}

// Deselect all visible columns for a table
function deselectAllColumns(tableName: string) {
  const cols = getFilteredColumns(tableName)
  if (!columnMetadata.value[tableName]) {
    columnMetadata.value[tableName] = {}
  }
  const meta = columnMetadata.value[tableName]
  if (meta) {
    cols.forEach(col => {
      if (!meta[col]) {
        meta[col] = { exposed: false, description: '' }
      } else {
        meta[col].exposed = false
      }
    })
  }
}

// Toggle curation of table
function toggleTableCuration(tableName: string) {
  const isCurated = curatedTablesMap.value[tableName]?.curated
  if (isCurated) {
    loadTableColumns(tableName, true) // auto-curate all its columns by default
  }
}

// Expand Accordion and fetch database details
async function onTableExpand(tableName: string) {
  if (expandedTable.value === tableName) {
    expandedTable.value = null
    return
  }
  expandedTable.value = tableName
  await loadTableColumns(tableName)
  await loadTableForeignKeys(tableName)
}

// Fetch Columns dynamically
async function loadTableColumns(tableName: string, autoExposeAll = false) {
  if (!tableColumns.value[tableName]) {
    tableColumns.value[tableName] = { all: [], loading: true }
  } else if (tableColumns.value[tableName].all.length > 0 && !autoExposeAll) {
    return // already loaded
  }

  tableColumns.value[tableName].loading = true
  tableColumns.value[tableName].error = undefined
  
  try {
    const cols = await getTableColumns(props.database, tableName)
    tableColumns.value[tableName].all = cols
    
    if (!columnMetadata.value[tableName]) {
      columnMetadata.value[tableName] = {}
    }

    const meta = columnMetadata.value[tableName]
    if (meta) {
      cols.forEach(col => {
        if (!meta[col]) {
          meta[col] = {
            exposed: autoExposeAll,
            description: ''
          }
        } else if (autoExposeAll) {
          meta[col].exposed = true
        }
      })
    }
  } catch (err: any) {
    tableColumns.value[tableName].error = err.message || 'Failed to load columns'
  } finally {
    tableColumns.value[tableName].loading = false
  }
}

// Fetch foreign keys
async function loadTableForeignKeys(tableName: string) {
  if (tableForeignKeys.value[tableName]) return // already loaded

  tableForeignKeys.value[tableName] = { all: {}, loading: true }
  try {
    const fks = await getTableForeignKeys(props.database, tableName)
    tableForeignKeys.value[tableName].all = fks
  } catch (err) {
    console.error(`Failed to load foreign keys for ${tableName}:`, err)
  } finally {
    tableForeignKeys.value[tableName].loading = false
  }
}

// Auto discovered joins computed property
const autoDiscoveredJoins = computed(() => {
  const joins: Array<{
    description: string
    conditions: string[]
    alreadyAdded: boolean
  }> = []

  for (const srcTable of Object.keys(tableForeignKeys.value)) {
    if (!curatedTablesMap.value[srcTable]?.curated) continue

    const fkData = tableForeignKeys.value[srcTable]?.all
    if (!fkData) continue
    for (const tgtTable of Object.keys(fkData)) {
      if (!curatedTablesMap.value[tgtTable]?.curated) continue

      const mapping = fkData[tgtTable]
      if (!mapping) continue
      const conditions: string[] = []
      for (const [srcCol, tgtCol] of Object.entries(mapping)) {
        conditions.push(`${srcTable}.${srcCol}=${tgtTable}.${tgtCol}`)
      }

      if (conditions.length > 0) {
        const description = `Link from ${srcTable} to ${tgtTable}`
        const alreadyAdded = curatedJoins.value.some(cj => 
          isRelationshipSubset(conditions, cj.JoinConditions)
        )

        joins.push({
          description,
          conditions,
          alreadyAdded
        })
      }
    }
  }
  return joins.filter(j => !j.alreadyAdded)
})

function getCanonicalConditions(condStrings: string[]): string[] {
  const flatConditions: string[] = []
  condStrings.forEach(condStr => {
    // Split by ' and ' (case insensitive)
    const parts = condStr.split(/\s+and\s+/i)
    parts.forEach(part => {
      const eqIdx = part.indexOf('=')
      if (eqIdx === -1) {
        flatConditions.push(part.trim().toLowerCase().replace(/\s+/g, ''))
      } else {
        const left = part.slice(0, eqIdx).trim().toLowerCase().replace(/\s+/g, '')
        const right = part.slice(eqIdx + 1).trim().toLowerCase().replace(/\s+/g, '')
        const sortedOperands = [left, right].sort()
        flatConditions.push(`${sortedOperands[0]}=${sortedOperands[1]}`)
      }
    })
  })
  return flatConditions.sort()
}

function isRelationshipSubset(relationshipConds: string[], curatedConds: string[]): boolean {
  const canonRel = getCanonicalConditions(relationshipConds)
  const canonCurated = getCanonicalConditions(curatedConds)
  return canonRel.every(cond => canonCurated.includes(cond))
}

function areConditionsEqual(condsA: string[], condsB: string[]): boolean {
  const canonA = getCanonicalConditions(condsA)
  const canonB = getCanonicalConditions(condsB)
  if (canonA.length !== canonB.length) return false
  return canonA.every((val, idx) => val === canonB[idx])
}

function normalizeCondition(cond: string): string {
  return cond.replace(/\s+/g, '').toLowerCase()
}

// Add auto discovered join
function addAutoDiscoverJoin(join: { description: string; conditions: string[] }) {
  const desc = prompt("Enter a description for this join:", join.description)
  if (desc === null) return // cancelled
  
  const finalDesc = desc.trim() || join.description
  const existing = curatedJoins.value.find(cj => cj.Description.toLowerCase() === finalDesc.toLowerCase())
  
  if (existing) {
    join.conditions.forEach(cond => {
      const alreadyExists = isRelationshipSubset([cond], existing.JoinConditions)
      if (!alreadyExists) {
        existing.JoinConditions.push(cond)
      }
    })
  } else {
    curatedJoins.value.push({
      Description: finalDesc,
      JoinConditions: [...join.conditions]
    })
  }
}

// Custom Join Builders (used within modal)
function addCustomCondition() {
  customJoin.value.conditions.push({ colA: '', colB: '', isCustomB: false, customValB: '' })
}

function removeCustomCondition(idx: number) {
  customJoin.value.conditions.splice(idx, 1)
}

// Modal actions
function openModalForEdit(idx: number) {
  const join = curatedJoins.value[idx]
  if (!join) return
  editingJoinIndex.value = idx
  modalJoinDescription.value = join.Description
  modalJoinConditions.value = [...join.JoinConditions]
  
  // Reset builder state
  customJoin.value = {
    description: '',
    tableA: '',
    tableB: '',
    conditions: [{ colA: '', colB: '', isCustomB: false, customValB: '' }]
  }
  showModal.value = true
}

function openModalForCreate() {
  editingJoinIndex.value = null
  modalJoinDescription.value = ""
  modalJoinConditions.value = []
  
  // Reset builder state
  customJoin.value = {
    description: '',
    tableA: '',
    tableB: '',
    conditions: [{ colA: '', colB: '', isCustomB: false, customValB: '' }]
  }
  showModal.value = true
}

function closeModal() {
  showModal.value = false
  editingJoinIndex.value = null
  modalJoinDescription.value = ""
  modalJoinConditions.value = []
}

function addConditionToModalGroup() {
  const j = customJoin.value
  if (!j.tableA || !j.tableB) {
    alert("Please select both Table A and Table B.")
    return
  }
  
  // Validate conditions
  for (const c of j.conditions) {
    if (!c.colA) {
      alert("Please select Table A column for all conditions.")
      return
    }
    if (c.isCustomB) {
      if (!c.customValB.trim()) {
        alert("Please enter a custom value/literal for the condition.")
        return
      }
    } else {
      if (!c.colB) {
        alert("Please select Table B column for all conditions.")
        return
      }
    }
  }

  // Format conditions array
  const condStrings = j.conditions.map(c => {
    if (c.isCustomB) {
      return `${j.tableA}.${c.colA}=${c.customValB.trim()}`
    } else {
      return `${j.tableA}.${c.colA}=${j.tableB}.${c.colB}`
    }
  })

  const combinedCondition = condStrings.join(' and ')
  
  // Check if duplicate condition in this group
  const canonCombined = getCanonicalConditions([combinedCondition])
  const alreadyExists = modalJoinConditions.value.some(jc => {
    const canonJc = getCanonicalConditions([jc])
    if (canonJc.length !== canonCombined.length) return false
    return canonJc.every((val, idx) => val === canonCombined[idx])
  })

  if (alreadyExists) {
    alert("This condition is already present in this group.")
    return
  }

  modalJoinConditions.value.push(combinedCondition)

  // Reset conditions fields, keeping tables to easily add another condition
  customJoin.value.conditions = [{ colA: '', colB: '', isCustomB: false, customValB: '' }]
}

function deleteModalGroupCondition(cIdx: number) {
  modalJoinConditions.value.splice(cIdx, 1)
}

function saveModalGroup() {
  const desc = modalJoinDescription.value.trim()
  if (!desc) {
    alert("Please enter a join group description.")
    return
  }
  if (modalJoinConditions.value.length === 0) {
    alert("Please define at least one join condition for the group.")
    return
  }

  if (editingJoinIndex.value !== null) {
    // Edit mode
    const idx = editingJoinIndex.value
    const joinObj = curatedJoins.value[idx]
    if (joinObj) {
      joinObj.Description = desc
      joinObj.JoinConditions = [...modalJoinConditions.value]
    }
  } else {
    // Create mode
    const existing = curatedJoins.value.find(cj => cj.Description.toLowerCase() === desc.toLowerCase())
    if (existing) {
      // Merge conditions
      modalJoinConditions.value.forEach(cond => {
        const canonCond = getCanonicalConditions([cond])
        const alreadyExists = existing.JoinConditions.some(jc => {
          const canonJc = getCanonicalConditions([jc])
          if (canonJc.length !== canonCond.length) return false
          return canonJc.every((val, idx) => val === canonCond[idx])
        })
        if (!alreadyExists) {
          existing.JoinConditions.push(cond)
        }
      })
    } else {
      curatedJoins.value.push({
        Description: desc,
        JoinConditions: [...modalJoinConditions.value]
      })
    }
  }
  
  closeModal()
}

// Delete Active Join Group
function deleteJoin(idx: number) {
  if (confirm("Are you sure you want to delete this entire join relationship group?")) {
    curatedJoins.value.splice(idx, 1)
  }
}

// Save all configurations to the backend
async function saveConfiguration() {
  saving.value = true
  showNotification('Saving schema configurations...', 'success')
  
  try {
    // 1. Build curated tables payload
    const tablesPayload: CuratedTable[] = []
    
    for (const table of dbTables.value) {
      const mapItem = curatedTablesMap.value[table]
      if (mapItem?.curated) {
        const columnsList: string[] = []
        
        if (tableColumns.value[table]?.all && tableColumns.value[table].all.length > 0) {
          // Use loaded checked column states
          for (const col of tableColumns.value[table].all) {
            const colMeta = columnMetadata.value[table]?.[col]
            if (colMeta?.exposed) {
              columnsList.push(formatColumnString(col, colMeta.description))
            }
          }
        } else {
          // Table was curated but never expanded. Use originally parsed columns.
          const original = curatedTables.value.find(t => t.Name === table)
          if (original) {
            for (const colStr of original.Columns) {
              const actualCols = splitColumns(colStr)
              actualCols.forEach(actualCol => {
                const { name } = parseColumnString(actualCol)
                const colMeta = columnMetadata.value[table]?.[name]
                if (colMeta?.exposed) {
                  columnsList.push(formatColumnString(name, colMeta.description))
                }
              })
            }
          }
        }

        tablesPayload.push({
          Name: table,
          Description: mapItem.description || '',
          Columns: columnsList
        })
      }
    }
    
    // 2. Build curated joins payload
    const joinsPayload: CuratedTableJoin[] = curatedJoins.value.map(cj => ({
      Description: cj.Description,
      JoinConditions: cj.JoinConditions
    }))

    // Save both simultaneously
    await Promise.all([
      saveCuratedTables(props.database, tablesPayload),
      saveCuratedTableJoins(props.database, joinsPayload)
    ])

    // Update local base configuration references
    curatedTables.value = JSON.parse(JSON.stringify(tablesPayload))
    
    showNotification('Configuration saved successfully!', 'success')
  } catch (err: any) {
    console.error('Failed to save schema configs:', err)
    showNotification(err.message || 'Failed to save schema configurations.', 'error')
  } finally {
    saving.value = false
  }
}

// Toast helper
function showNotification(message: string, type: 'success' | 'error') {
  notification.value = { message, type }
  if (type === 'success' && !saving.value) {
    setTimeout(() => {
      if (notification.value?.message === message) {
        notification.value = null
      }
    }, 4000)
  }
}

// Initial Data Fetch
async function initializeData() {
  loading.value = true
  try {
    // 1. Fetch DB tables list
    const [allDbTables, fetchedCuratedTables, fetchedCuratedJoins] = await Promise.all([
      getDatabaseTables(props.database),
      getCuratedTables(props.database),
      getCuratedTableJoins(props.database)
    ])
    
    dbTables.value = allDbTables
    curatedTables.value = fetchedCuratedTables
    curatedJoins.value = fetchedCuratedJoins

    // 2. Initialize curation maps
    allDbTables.forEach(table => {
      const existingCurated = fetchedCuratedTables.find(t => t.Name === table)
      curatedTablesMap.value[table] = {
        curated: !!existingCurated,
        description: existingCurated ? existingCurated.Description : ''
      }

      // Initialize columns registry
      columnMetadata.value[table] = {}
      const meta = columnMetadata.value[table]

      if (existingCurated && meta) {
        // Pre-parse and register previously curated columns
        existingCurated.Columns.forEach(colStr => {
          const actualCols = splitColumns(colStr)
          actualCols.forEach(actualCol => {
            const { name, description } = parseColumnString(actualCol)
            meta[name] = {
              exposed: true,
              description
            }
          })
        })
      }
    })

    // 3. For already curated tables, fetch their foreign keys to build suggestions
    await Promise.all(
      fetchedCuratedTables.map(t => loadTableForeignKeys(t.Name))
    )

  } catch (err: any) {
    console.error('Failed to load database schema data:', err)
    showNotification('Error loading schema data. Please make sure the backend is active.', 'error')
  } finally {
    loading.value = false
  }
}

onMounted(() => {
  initializeData()
})
</script>

<style scoped>
/* Scoped overrides if needed, using standard variables */
.bg-base {
  background: var(--bg);
}
.bg-panel {
  background: var(--panel);
}
.border-soft {
  border-color: var(--border);
}
.text-user {
  color: var(--user-bg);
}
.bg-user {
  background: var(--user-bg);
}
</style>
