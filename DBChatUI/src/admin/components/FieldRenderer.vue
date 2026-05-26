<template>

<div>

    <div class="flex items-center justify-between mb-1">
        <label class="block text-sm opacity-70">
            {{ field.label }}
            <span v-if="field.required" class="text-red-500 ml-1">*</span>
        </label>
        <button
            v-if="field.wizard && (field.type === 'text' || field.type === 'textarea')"
            type="button"
            :disabled="!isWizardActive"
            @click="openWizard"
            class="text-xs px-2.5 py-1 rounded-xl bg-soft hover:bg-hover border border-soft transition disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-1 font-medium">
            <span>🔧 Connection Wizard</span>
        </button>
    </div>

    <!-- TEXT -->
    <div v-if="field.type === 'text'" class="relative w-full">
        <input
            :value="modelValue"
            :disabled="isDisabled"
            @input="(e) => { $emit('update:modelValue', (e.target as HTMLInputElement).value); showAutocomplete = true; autocompleteIndex = -1 }"
            @focus="showAutocomplete = true"
            @blur="handleBlur"
            @keydown="handleKeydown"
            class="w-full rounded-xl border border-soft bg-base px-3 py-2 disabled:opacity-50 disabled:cursor-not-allowed" />
            
        <div v-if="showAutocomplete && field.references && filteredOptions.length > 0" 
             class="absolute z-10 w-full mt-1 bg-panel border border-soft rounded-xl shadow-lg max-h-48 overflow-y-auto">
            <div 
                v-for="(opt, idx) in filteredOptions" 
                :key="opt"
                :id="'autocomplete-item-' + idx"
                @mousedown.prevent="selectAutocomplete(opt)"
                :class="['px-3 py-2 cursor-pointer', autocompleteIndex === idx ? 'bg-user text-white' : 'hover:bg-hover']">
                {{ opt }}
            </div>
        </div>
    </div>

    <!-- PASSWORD -->
    <div v-else-if="field.type === 'password'" class="relative w-full">
        <input
            :type="showPassword ? 'text' : 'password'"
            :value="modelValue"
            @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
            class="w-full rounded-xl border border-soft bg-base px-3 py-2 pr-16" />
        <button
            type="button"
            @click.prevent="showPassword = !showPassword"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-xs opacity-60 hover:opacity-100 transition">
            {{ showPassword ? 'Hide' : 'Show' }}
        </button>
    </div>

    <!-- TEXTAREA -->
    <textarea
        v-else-if="field.type === 'textarea'"
        :value="modelValue"
        @input="$emit('update:modelValue', ($event.target as HTMLTextAreaElement).value)"
        rows="5"
        class="w-full rounded-xl border border-soft bg-base px-3 py-2" />

    <!-- NUMBER -->
    <input
        v-else-if="field.type === 'number'"
        type="number"
        v-model="numberVal"
        :disabled="isDisabled"
        class="w-full rounded-xl border border-soft bg-base px-3 py-2 disabled:opacity-50 disabled:cursor-not-allowed" />

    <!-- DATE -->
    <input
        v-else-if="field.type === 'date'"
        type="date"
        v-model="dateVal"
        :disabled="isDisabled"
        class="w-full rounded-xl border border-soft bg-base px-3 py-2 disabled:opacity-50 disabled:cursor-not-allowed" />

    <!-- FILE -->
    <div v-else-if="field.type === 'file'" class="flex gap-2 w-full">
        <input
            type="text"
            :value="modelValue"
            @input="$emit('update:modelValue', ($event.target as HTMLInputElement).value)"
            class="flex-1 rounded-xl border border-soft bg-base px-3 py-2"
            placeholder="Type filename..." />
        <input
            type="file"
            @change="handleFileChange"
            class="hidden"
            ref="fileInputRef" />
        <button
            @click.prevent="triggerFileSelect"
            class="px-4 py-2 rounded-xl bg-soft hover:bg-hover transition border border-soft whitespace-nowrap">
            Browse
        </button>
    </div>

    <!-- SELECT -->
    <select
        v-else-if="field.type === 'select'"
        :value="modelValue"
        @change="$emit('update:modelValue', ($event.target as HTMLSelectElement).value)"
        class="w-full rounded-xl border border-soft bg-base px-3 py-2">

        <option
            v-for="o in field.options"
            :key="o"
            :value="o">
            {{ o }}
        </option>

    </select>

    <!-- BOOLEAN (SWITCH) -->
    <label
        v-else-if="field.type === 'boolean'"
        class="relative inline-flex items-center cursor-pointer mt-1">

        <input
            type="checkbox"
            class="sr-only peer"
            :checked="!!modelValue"
            @change="$emit('update:modelValue', ($event.target as HTMLInputElement).checked)" />

        <div class="w-11 h-6 bg-red-400 border border-red-500 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-green-400 peer-checked:border-green-500"></div>

    </label>

    <!-- OBJECT WITH PROPERTIES -->
    <div v-else-if="field.type === 'object' && field.properties && field.properties.length > 0" class="border border-soft rounded-xl p-4 bg-hover/5 w-full">
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <FieldRenderer
                v-for="subField in processedSubFields"
                :key="subField.name"
                :field="subField"
                :modelValue="safeGetSubValue(subField.name)"
                @update:modelValue="val => updateSingleObjectSubValue(subField.name, val)"
                @row-editing-change="val => $emit('row-editing-change', val)"
                :model="modelValue || {}"
                :database="props.database"
                :class="{'md:col-span-2': subField.fulllength}" />
        </div>
    </div>

    <!-- OBJECT (GENERIC / JSON TEXTAREA) -->
    <div v-else-if="field.type === 'object'" class="w-full">
        <textarea
            :value="objectJsonString"
            @input="handleObjectInput"
            @blur="handleObjectBlur"
            rows="4"
            class="w-full rounded-xl border border-soft bg-base px-3 py-2 font-mono text-xs"
            placeholder="{}" />
        <div v-if="jsonError" class="text-xs text-red-500 mt-1">
            {{ jsonError }}
        </div>
    </div>

    <!-- ARRAY OF OBJECTS -->
    <div v-else-if="field.type === 'array<object>'" class="w-full">
        <div class="text-xs text-red-500 bg-red-100 dark:bg-red-950 p-2 mb-2 rounded font-mono">
            DEBUG: isAddingItem={{ isAddingItem }}, editingItemIdx={{ editingItemIdx }}
        </div>
        <!-- Form for Add/Edit -->
        <div v-if="isAddingItem || editingItemIdx !== null" key="array-edit-form-container" class="border border-soft rounded-xl p-4 bg-hover/10 mb-3">
            <div class="text-xs font-bold mb-3 uppercase tracking-wider opacity-70">
                {{ editingItemIdx !== null ? 'Edit Item' : 'Add Item' }}
            </div>
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 mb-4">
                <FieldRenderer
                    v-for="subField in processedSubFields"
                    :key="subField.name"
                    :field="subField"
                    :modelValue="editingItem[subField.name]"
                    @update:modelValue="val => updateSubFieldValue(subField.name, val)"
                    @row-editing-change="val => $emit('row-editing-change', val)"
                    :model="editingItem"
                    :database="props.database"
                    :class="{'md:col-span-2': subField.fulllength}" />
            </div>

            <div class="flex justify-end gap-2 text-xs">
                <button
                    @click.prevent="cancelEditItem"
                    class="px-3 py-1.5 rounded-xl border border-soft">
                    Cancel
                </button>
                <button
                    @click.prevent="saveItem"
                    class="px-3 py-1.5 rounded-xl bg-user text-white font-medium">
                    {{ editingItemIdx !== null ? 'Update' : 'Add' }}
                </button>
            </div>
        </div>

        <!-- Add item button and list of existing items -->
        <div v-if="!isAddingItem && editingItemIdx === null" key="array-list-container" :class="{ 'hidden': isAddingItem || editingItemIdx !== null }">
            <!-- Add item button -->
            <div class="mb-2">
                <button
                    @click.prevent="startAddItem"
                    :disabled="isDisabled"
                    class="px-3 py-1.5 rounded-xl bg-soft hover:bg-hover border border-soft text-xs transition flex items-center gap-1">
                    <span>+ Add Item</span>
                </button>
            </div>

            <!-- Table of existing items -->
            <div v-if="Array.isArray(modelValue) && modelValue.length > 0" class="overflow-x-auto border border-soft rounded-xl mb-3 bg-panel">
                <table class="w-full text-xs text-left">
                    <thead>
                        <tr class="border-b border-soft bg-hover/10">
                            <th class="p-2 w-8 text-center"></th>
                            <th v-for="subField in nestedFields" :key="subField.name" class="p-2 font-semibold whitespace-nowrap">
                                {{ subField.label || subField.name }}
                            </th>
                            <th class="p-2 w-24 text-right">Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        <tr
                            v-for="(item, idx) in modelValue"
                            :key="getRowKey(item, idx)"
                            draggable="true"
                            @dragstart="onDragStart($event, idx)"
                            @dragover="onDragOver($event, idx)"
                            @dragend="onDragEnd"
                            :class="[
                                'border-b border-soft hover:bg-hover/20 last:border-b-0 transition-colors',
                                draggedIdx === idx ? 'bg-hover/40 opacity-50' : ''
                            ]">
                            <td 
                                @mousedown="dragHandleActive = true"
                                @touchstart="dragHandleActive = true"
                                class="p-2 text-center align-middle drag-handle cursor-grab active:cursor-grabbing text-muted select-none w-8">
                                <svg class="w-4 h-4 inline-block opacity-40 hover:opacity-100 transition" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <circle cx="9" cy="12" r="1" />
                                    <circle cx="9" cy="5" r="1" />
                                    <circle cx="9" cy="19" r="1" />
                                    <circle cx="15" cy="12" r="1" />
                                    <circle cx="15" cy="5" r="1" />
                                    <circle cx="15" cy="19" r="1" />
                                </svg>
                            </td>
                            <td v-for="subField in nestedFields" :key="subField.name" class="p-2 font-mono text-[11px] max-w-[200px] truncate">
                                {{ formatSubFieldValue(item[subField.name], subField.type) }}
                            </td>
                            <td class="p-2 text-right whitespace-nowrap">
                                <button
                                    @click.stop.prevent="startEditItem(idx)"
                                    class="text-user hover:underline mr-2"
                                    title="Edit item">
                                    Edit
                                </button>
                                <button
                                    @click.stop.prevent="removeObjectItem(idx)"
                                    class="text-red-500 hover:underline"
                                    title="Delete item">
                                    Delete
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>

    <!-- ARRAY -->
    <div v-else-if="field.type && field.type.startsWith('array<')" class="w-full">
        <div class="flex flex-wrap gap-2 mb-2">
            <div
                v-for="(item, idx) in (Array.isArray(modelValue) ? modelValue : [])"
                :key="idx"
                class="flex items-center gap-1 bg-soft px-2 py-1 rounded-lg text-sm">
                <span>{{ item }}</span>
                <button
                    @click.prevent="removeArrayItem(idx)"
                    class="opacity-50 hover:opacity-100 hover:text-red-500 transition px-1"
                    title="Remove item">
                    &times;
                </button>
            </div>
        </div>
        <div class="flex gap-2 relative">
            <input
                v-model="newItemText"
                :disabled="isDisabled"
                @keydown="handleKeydown"
                @focus="showAutocomplete = true"
                @input="showAutocomplete = true; autocompleteIndex = -1"
                @blur="handleBlur"
                :type="field.type.includes('date') ? 'date' : (field.type.includes('number') ? 'number' : 'text')"
                class="flex-1 rounded-xl border border-soft bg-base px-3 py-2 text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                placeholder="Type and press Enter to add..." />
            <button
                @click.prevent="addArrayItem"
                :disabled="isDisabled"
                class="px-3 py-2 rounded-xl bg-user text-white text-sm disabled:opacity-50 disabled:cursor-not-allowed">
                Add
            </button>
            <div v-if="showAutocomplete && field.references && filteredOptions.length > 0" 
                 class="absolute top-full left-0 z-10 w-[calc(100%-4.5rem)] mt-1 bg-panel border border-soft rounded-xl shadow-lg max-h-48 overflow-y-auto">
                <div 
                    v-for="(opt, idx) in filteredOptions" 
                    :key="opt"
                    :id="'autocomplete-item-' + idx"
                    @mousedown.prevent="selectAutocomplete(opt)"
                    :class="['px-3 py-2 cursor-pointer text-sm', autocompleteIndex === idx ? 'bg-user text-white' : 'hover:bg-hover']">
                    {{ opt }}
                </div>
            </div>
        </div>
    </div>

    <!-- DESCRIPTION / HINT -->
    <div v-if="field.description" class="text-xs opacity-50 mt-1">
        {{ field.description }}
    </div>

    <!-- Teleport Wizard Modal to body -->
    <Teleport to="body" v-if="showWizardModal">
        <div class="fixed inset-0 bg-black/55 flex items-center justify-center z-[100] backdrop-blur-sm">
            <div class="w-full max-w-lg bg-panel border border-soft rounded-2xl p-6 shadow-2xl max-h-[95vh] overflow-y-auto">
                <div class="flex items-center justify-between mb-4 border-b border-soft pb-3">
                    <h3 class="text-base font-bold flex items-center gap-2">
                        <span>🔧 Connection Wizard</span>
                        <span class="text-xs font-normal opacity-50">({{ currentProviderName }})</span>
                    </h3>
                    <button 
                        @click="closeWizard" 
                        class="text-lg opacity-60 hover:opacity-100 transition px-2">
                        &times;
                    </button>
                </div>

                <div class="space-y-4 my-4">
                    <div v-for="wField in wizardFields" :key="wField.key || wField.name">
                        <label class="block text-xs font-semibold mb-1 opacity-70">
                            {{ wField.label || wField.key || wField.name }}
                            <span v-if="wField.isRequired || wField.required" class="text-red-500 ml-1">*</span>
                        </label>
                        
                        <!-- WIZARD INPUT: TEXT -->
                        <input
                            v-if="wField.type === 'text'"
                            type="text"
                            v-model="wizardModel[wField.key || wField.name]"
                            class="w-full rounded-xl border border-soft bg-base px-3 py-2 text-xs" />

                        <!-- WIZARD INPUT: PASSWORD -->
                        <div v-else-if="wField.type === 'password'" class="relative w-full">
                            <input
                                :type="showWizardPassword[wField.key || wField.name] ? 'text' : 'password'"
                                v-model="wizardModel[wField.key || wField.name]"
                                class="w-full rounded-xl border border-soft bg-base px-3 py-2 pr-12 text-xs" />
                            <button
                                type="button"
                                @click.prevent="showWizardPassword[wField.key || wField.name] = !showWizardPassword[wField.key || wField.name]"
                                class="absolute right-3 top-1/2 -translate-y-1/2 text-[10px] opacity-60 hover:opacity-100 transition">
                                {{ showWizardPassword[wField.key || wField.name] ? 'Hide' : 'Show' }}
                            </button>
                        </div>

                        <!-- WIZARD INPUT: NUMBER -->
                        <input
                            v-else-if="wField.type === 'number'"
                            type="number"
                            v-model.number="wizardModel[wField.key || wField.name]"
                            class="w-full rounded-xl border border-soft bg-base px-3 py-2 text-xs" />

                        <!-- WIZARD INPUT: BOOLEAN (SWITCH) -->
                        <label
                            v-else-if="wField.type === 'boolean' || wField.type === 'checkbox'"
                            class="relative inline-flex items-center cursor-pointer mt-1">
                            <input
                                type="checkbox"
                                class="sr-only peer"
                                v-model="wizardModel[wField.key || wField.name]" />
                            <div class="w-9 h-5 bg-red-400 border border-red-500 peer-focus:outline-none rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-4 after:w-4 after:transition-all peer-checked:bg-green-400 peer-checked:border-green-500"></div>
                        </label>
                        
                        <!-- WIZARD INPUT: SELECT -->
                        <select
                            v-else-if="wField.type === 'select'"
                            v-model="wizardModel[wField.key || wField.name]"
                            class="w-full rounded-xl border border-soft bg-base px-3 py-2 text-xs">
                            <option v-for="o in wField.options" :key="o" :value="o">
                                {{ o }}
                            </option>
                        </select>

                        <div v-if="wField.description" class="text-[10px] opacity-50 mt-0.5">
                            {{ wField.description }}
                        </div>
                    </div>
                </div>

                <div v-if="wizardError" class="text-xs text-red-500 bg-red-100 dark:bg-red-950/40 p-2.5 rounded-xl border border-red-200 dark:border-red-900/50 mb-4">
                    {{ wizardError }}
                </div>

                <div class="flex justify-end gap-2 text-xs pt-3 border-t border-soft">
                    <button
                        @click="closeWizard"
                        class="px-3 py-1.5 rounded-xl border border-soft">
                        Cancel
                    </button>
                    <button
                        @click="generateConnectionString"
                        :disabled="isGeneratingString"
                        class="px-3 py-1.5 rounded-xl bg-user text-white font-medium disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-1.5">
                        <span v-if="isGeneratingString" class="w-3 h-3 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
                        Apply &amp; Generate
                    </button>
                </div>
            </div>
        </div>
    </Teleport>

</div>

</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch, nextTick } from 'vue'
import { getData, compileMetadataString } from '@/services/api'

// Global counter for debugging component instances
let instanceCounter = (window as any).__fieldRendererInstanceCounter || 0;
(window as any).__fieldRendererInstanceCounter = instanceCounter + 1;
const instanceId = (window as any).__fieldRendererInstanceCounter;

const props = defineProps<{
    field: any
    modelValue: any
    database?: string
    model?: any
}>()

console.log(`[FieldRenderer debug instance ${instanceId}] Created for field: ${props.field?.name}, type: ${props.field?.type}`)

onUnmounted(() => {
    console.log(`[FieldRenderer debug instance ${instanceId}] Unmounted for field: ${props.field?.name}`)
})

const emit = defineEmits(['update:modelValue', 'row-editing-change'])

const newItemText = ref('')
const fileInputRef = ref<HTMLInputElement | null>(null)
const showPassword = ref(false)

// Computed bindings for number and date inputs to ensure clean reactivity and type conversion
const numberVal = computed({
    get: () => {
        const val = props.modelValue
        if (val === null || val === undefined || (val as any) === '') {
            return ''
        }
        return Number(val)
    },
    set: (val) => {
        emit('update:modelValue', val === null || val === undefined || (val as any) === '' ? null : Number(val))
    }
})

const dateVal = computed({
    get: () => {
        if (props.modelValue === null || props.modelValue === undefined) {
            return ''
        }
        return props.modelValue
    },
    set: (val) => {
        emit('update:modelValue', val === null || val === undefined || (val as any) === '' ? null : val)
    }
})

// Debug log for field changes
watch(() => props.modelValue, (newVal) => {
    console.log(`[FieldRenderer debug] Field: ${props.field?.name}, Type: ${props.field?.type}, modelValue:`, newVal, typeof newVal)
}, { immediate: true })

// Autocomplete State
const referenceDataRaw = ref<any[]>([])
const showAutocomplete = ref(false)
const autocompleteIndex = ref(-1)

watch(showAutocomplete, (val) => {
    if (!val) {
        autocompleteIndex.value = -1
    }
})

async function loadReferenceData() {
    if (props.field.references && props.field.references.includes('.')) {
        const [refObj, refField] = props.field.references.split('.')
        const scopeField = props.field.sourceScope || props.field.sourcescope
        let targetDb = props.database
        if (scopeField && props.model?.[scopeField]) {
            targetDb = props.model[scopeField]
        }
        try {
            const data = await getData(refObj, targetDb)
            if (Array.isArray(data)) {
                referenceDataRaw.value = data
            }
        } catch (e) {
            console.error("Failed to load reference data for", refObj, e)
        }
    }
}

onMounted(() => {
    loadReferenceData()
})

watch(() => {
    const scopeField = props.field?.sourceScope || props.field?.sourcescope
    return scopeField ? props.model?.[scopeField] : null
}, (newVal, oldVal) => {
    if (newVal !== oldVal) {
        loadReferenceData()
    }
})

const isDisabled = computed(() => {
    if (props.field.filterby && props.field.filterby.sourcefield) {
        const sourceVal = props.model?.[props.field.filterby.sourcefield]
        if (!sourceVal) return true
    }
    return false
})

const referenceOptions = computed(() => {
    if (!props.field.references || !props.field.references.includes('.')) return []
    
    const [, refField] = props.field.references.split('.')
    let data = referenceDataRaw.value

    if (props.field.filterby && props.field.filterby.sourcefield && props.field.filterby.targetField) {
        const sourceVal = props.model?.[props.field.filterby.sourcefield]
        if (!sourceVal) return []
        
        let targetField = props.field.filterby.targetField
        if (targetField.includes('.')) {
            targetField = targetField.split('.').pop()
        }
        
        data = data.filter(item => item[targetField] === sourceVal)
    }

    const uniqueValues = new Set<string>()
    for (const item of data) {
        const val = item[refField]
        if (Array.isArray(val)) {
            for (const v of val) {
                if (v !== undefined && v !== null) {
                    uniqueValues.add(String(v))
                }
            }
        } else if (val !== undefined && val !== null) {
            uniqueValues.add(String(val))
        }
    }
    return Array.from(uniqueValues)
})

const filteredOptions = computed(() => {
    let text = ''
    let selectedValues: string[] = []

    if (props.field.type === 'text') {
        text = String(props.modelValue || '').toLowerCase()
        selectedValues = [String(props.modelValue || '')]
    } else if (props.field.type && props.field.type.startsWith('array<')) {
        text = String(newItemText.value || '').toLowerCase()
        if (Array.isArray(props.modelValue)) {
            selectedValues = props.modelValue.map(String)
        }
    }
    
    // Exclude already selected values
    let opts = referenceOptions.value.filter(opt => !selectedValues.includes(opt))

    if (!text) return opts
    return opts.filter(opt => opt.toLowerCase().includes(text))
})

function handleKeydown(e: KeyboardEvent) {
    if (!showAutocomplete.value || !props.field.references || filteredOptions.value.length === 0) {
        // If it's an array input and enter is pressed, fallback to addArrayItem
        if (e.key === 'Enter' && props.field.type && props.field.type.startsWith('array<')) {
            e.preventDefault()
            addArrayItem()
        }
        return
    }

    if (e.key === 'ArrowDown') {
        e.preventDefault()
        if (autocompleteIndex.value < filteredOptions.value.length - 1) {
            autocompleteIndex.value++
            scrollToActiveItem()
        }
    } else if (e.key === 'ArrowUp') {
        e.preventDefault()
        if (autocompleteIndex.value > 0) {
            autocompleteIndex.value--
            scrollToActiveItem()
        }
    } else if (e.key === 'Enter') {
        e.preventDefault()
        if (autocompleteIndex.value >= 0 && autocompleteIndex.value < filteredOptions.value.length) {
            selectAutocomplete(filteredOptions.value[autocompleteIndex.value]!)
        } else if (props.field.type && props.field.type.startsWith('array<')) {
            addArrayItem()
        } else {
            showAutocomplete.value = false
        }
    } else if (e.key === 'Escape') {
        showAutocomplete.value = false
    }
}

function scrollToActiveItem() {
    nextTick(() => {
        const el = document.getElementById('autocomplete-item-' + autocompleteIndex.value)
        if (el) {
            el.scrollIntoView({ block: 'nearest' })
        }
    })
}

function selectAutocomplete(option: string) {
    if (props.field.type === 'text') {
        emit('update:modelValue', option)
    } else if (props.field.type && props.field.type.startsWith('array<')) {
        newItemText.value = option
        addArrayItem()
    }
    showAutocomplete.value = false
}

function handleBlur() {
    // Delay hiding so clicks on the dropdown register first
    setTimeout(() => {
        showAutocomplete.value = false
        // Validate single reference
        if (props.field.type === 'text' && props.field.references && props.modelValue) {
            if (!referenceOptions.value.includes(props.modelValue)) {
                alert(`Invalid reference. Must be one of: ${referenceOptions.value.join(', ')}`)
            }
        }
    }, 200)
}

function handleFileChange(event: Event) {
    const target = event.target as HTMLInputElement;
    if (target.files && target.files.length > 0) {
        // Some desktop wrappers like Electron provide a non-standard .path property.
        // Standard web browsers hide the full path for security reasons.
        const file = target.files[0] as any;
        console.log("Selected file", file.path, file.name);
        emit('update:modelValue', file.path || file.name);
        target.value = ''; // Reset so the same file can be selected again
    }
}

function triggerFileSelect() {
    // Check if it's an array of refs (happens in some v-for contexts, though not here usually)
    const el = Array.isArray(fileInputRef.value) ? fileInputRef.value[0] : fileInputRef.value;
    el?.click();
}

function addArrayItem() {
    if (!newItemText.value || !String(newItemText.value).trim()) return

    const currentArray = Array.isArray(props.modelValue) ? [...props.modelValue] : []

    let valToAdd: any = String(newItemText.value).trim()

    if (props.field.type === 'array<numbers>' || props.field.type === 'array<number>') {
        valToAdd = Number(valToAdd)
        if (isNaN(valToAdd)) {
            alert('Please enter a valid number')
            return
        }
    }

    if (props.field.references && props.field.references.includes('.')) {
        if (!referenceOptions.value.includes(String(valToAdd))) {
            alert(`Invalid reference. Must be one of: ${referenceOptions.value.join(', ')}`)
            return
        }
    }

    if (currentArray.includes(valToAdd)) {
        alert('This item is already added.')
        return
    }

    currentArray.push(valToAdd)
    emit('update:modelValue', currentArray)
    newItemText.value = ''
}

function removeArrayItem(index: number) {
    if (confirm('Are you sure you want to remove this item?')) {
        const currentArray = [...props.modelValue]
        currentArray.splice(index, 1)
        emit('update:modelValue', currentArray)
    }
}

// -------------------------------------------------------------
// Nested Fields, Generic Object, and Array<Object> support code
// -------------------------------------------------------------

const nestedFields = computed(() => {
    const propsList = props.field.properties || []
    const headers = propsList.filter((f: any) => f.showinheader)
    return headers.length > 0 ? headers : propsList
})

const processedSubFields = computed(() => {
    if (!props.field || !props.field.properties) return []
    const targetModel = editingItem.value || props.modelValue || {}
    return props.field.properties.filter((subField: any) => {
        return evaluateSubConditions(subField.showIf, targetModel)
    }).map((subField: any) => {
        let isRequired = subField.required
        if (subField.requiredIf) {
            isRequired = evaluateSubConditions(subField.requiredIf, targetModel)
        }
        return { ...subField, required: isRequired }
    })
})

function evaluateSubConditions(conditions: any, localModel: any) {
    if (!conditions) return true
    
    let parsedConditions = conditions
    if (typeof conditions === 'string') {
        try {
            parsedConditions = JSON.parse(conditions)
        } catch (e) {
            console.warn("Condition string is not valid JSON:", conditions)
            return true
        }
    }

    for (const key in parsedConditions) {
        const parts = key.split(/[.,]/)
        
        let targetValue: any = localModel
        let foundInLocal = true
        for (const part of parts) {
            if (targetValue === undefined || targetValue === null || typeof targetValue !== 'object' || !(part in targetValue)) {
                foundInLocal = false
                break
            }
            targetValue = targetValue[part]
        }
        
        if (!foundInLocal && props.model) {
            targetValue = props.model
            for (const part of parts) {
                if (targetValue === undefined || targetValue === null || typeof targetValue !== 'object') {
                    targetValue = undefined
                    break
                }
                targetValue = targetValue[part]
            }
        }

        const condition = parsedConditions[key]
        
        if (condition && typeof condition === 'object' && !Array.isArray(condition) && 'op' in condition) {
            const { op, value } = condition
            switch(op) {
                case '==':
                case '=': if (targetValue != value) return false; break
                case '!=':
                case '<>': if (targetValue != value) return false; break
                case '>': if (targetValue <= value) return false; break
                case '>=': if (targetValue < value) return false; break
                case '<': if (targetValue >= value) return false; break
                case '<=': if (targetValue > value) return false; break
                case 'contains': 
                    if (!targetValue || typeof targetValue.includes !== 'function' || !targetValue.includes(value)) return false
                    break
                case 'in':
                    if (!Array.isArray(value) || !value.includes(targetValue)) return false
                    break
                case 'not in':
                    if (Array.isArray(value) && value.includes(targetValue)) return false
                    break
                case 'empty':
                    if (targetValue !== undefined && targetValue !== null && targetValue !== '' && !(typeof targetValue === 'string' && targetValue.trim() === '')) return false
                    break
                case 'not empty':
                    if (targetValue === undefined || targetValue === null || targetValue === '' || (typeof targetValue === 'string' && targetValue.trim() === '')) return false
                    break
                default:
                    return false
            }
        } else {
            if (targetValue !== condition) return false
        }
    }
    return true
}

function formatSubFieldValue(val: any, type: string) {
    if (val === undefined || val === null) return '-'
    if (type === 'boolean') return val ? 'Yes' : 'No'
    if (typeof val === 'object') {
        try {
            return JSON.stringify(val)
        } catch (e) {
            return String(val)
        }
    }
    return String(val)
}

// Object Field State (JSON Textarea)
const objectJsonString = ref('')
const jsonError = ref('')

watch(() => props.modelValue, (newVal) => {
    if (props.field.type === 'object' && (!props.field.properties || props.field.properties.length === 0)) {
        if (newVal === undefined || newVal === null) {
            objectJsonString.value = ''
        } else {
            try {
                const parsed = JSON.parse(objectJsonString.value)
                if (JSON.stringify(parsed) !== JSON.stringify(newVal)) {
                    objectJsonString.value = JSON.stringify(newVal, null, 2)
                }
            } catch (e) {
                objectJsonString.value = JSON.stringify(newVal, null, 2)
            }
        }
    }
}, { immediate: true, deep: true })

function handleObjectInput(e: Event) {
    const val = (e.target as HTMLTextAreaElement).value
    objectJsonString.value = val
    try {
        if (!val.trim()) {
            jsonError.value = ''
            emit('update:modelValue', null)
            return
        }
        const parsed = JSON.parse(val)
        jsonError.value = ''
        emit('update:modelValue', parsed)
    } catch (err: any) {
        jsonError.value = 'Invalid JSON: ' + err.message
    }
}

function handleObjectBlur() {
    if (!objectJsonString.value.trim()) {
        jsonError.value = ''
        emit('update:modelValue', null)
        return
    }
    try {
        const parsed = JSON.parse(objectJsonString.value)
        jsonError.value = ''
        emit('update:modelValue', parsed)
        objectJsonString.value = JSON.stringify(parsed, null, 2)
    } catch (err: any) {
        // Keep error
    }
}

// Single Object with Properties Helpers
function safeGetSubValue(name: string) {
    const obj = props.modelValue || {}
    const parts = name.split(/[.,]/)
    let current = obj
    for (const part of parts) {
        if (current === undefined || current === null) return undefined
        current = current[part]
    }
    return current
}

function updateSingleObjectSubValue(name: string, value: any) {
    const obj = JSON.parse(JSON.stringify(props.modelValue || {}))
    const parts = name.split(/[.,]/)
    let current = obj
    for (let i = 0; i < parts.length - 1; i++) {
        const key = parts[i] as string
        if (current[key] === undefined || current[key] === null) {
            current[key] = {}
        }
        current = current[key]
    }
    const lastKey = parts[parts.length - 1] as string
    current[lastKey] = value
    emit('update:modelValue', obj)
}

// Array of Objects state
const editingItem = ref<any>(null)
const editingItemIdx = ref<number | null>(null)
const isAddingItem = ref(false)

function startAddItem() {
    console.log(`[FieldRenderer debug instance ${instanceId}] startAddItem called`)
    editingItem.value = {}
    editingItemIdx.value = null
    isAddingItem.value = true
    emit('row-editing-change', true)
}

function startEditItem(index: number) {
    console.log(`[FieldRenderer debug instance ${instanceId}] startEditItem called for index: ${index}`)
    const arr = Array.isArray(props.modelValue) ? props.modelValue : []
    editingItem.value = JSON.parse(JSON.stringify(arr[index] || {}))
    editingItemIdx.value = index
    isAddingItem.value = false
    emit('row-editing-change', true)
}

function updateSubFieldValue(name: string, value: any) {
    console.log(`[FieldRenderer debug instance ${instanceId}] updateSubFieldValue called: ${name} =`, value)
    if (!editingItem.value) return
    const parts = name.split(/[.,]/)
    let current = editingItem.value
    for (let i = 0; i < parts.length - 1; i++) {
        const key = parts[i] as string
        if (current[key] === undefined || current[key] === null) {
            current[key] = {}
        }
        current = current[key]
    }
    const lastKey = parts[parts.length - 1] as string
    current[lastKey] = value
}

function saveItem() {
    console.log(`[FieldRenderer debug instance ${instanceId}] saveItem called. editingItemIdx = ${editingItemIdx.value}`)
    if (!editingItem.value) return
    
    // Validate required subfields
    if (props.field.properties) {
        for (const subField of props.field.properties) {
            const isRequired = subField.required || (subField.requiredIf && evaluateSubConditions(subField.requiredIf, editingItem.value))
            if (isRequired) {
                const parts = subField.name.split(/[.,]/)
                let current = editingItem.value
                for (const part of parts) {
                    if (current === undefined || current === null) break
                    current = current[part]
                }
                if (current === undefined || current === null || current === '') {
                    alert(`${subField.label || subField.name} is required.`)
                    return
                }
            }
        }
    }
    
    const currentArray = Array.isArray(props.modelValue) ? [...props.modelValue] : []
    if (editingItemIdx.value !== null) {
        currentArray[editingItemIdx.value] = editingItem.value
    } else {
        currentArray.push(editingItem.value)
    }
    
    emit('update:modelValue', currentArray)
    cancelEditItem()
}

function cancelEditItem() {
    console.log(`[FieldRenderer debug instance ${instanceId}] cancelEditItem called`)
    editingItem.value = null
    editingItemIdx.value = null
    isAddingItem.value = false
    emit('row-editing-change', false)
}

function removeObjectItem(index: number) {
    if (confirm('Are you sure you want to remove this item?')) {
        const currentArray = Array.isArray(props.modelValue) ? [...props.modelValue] : []
        currentArray.splice(index, 1)
        emit('update:modelValue', currentArray)
        if (editingItemIdx.value === index) {
            cancelEditItem()
        }
    }
}

// Drag and drop support for object arrays
const draggedIdx = ref<number | null>(null)
const dragHandleActive = ref(false)
const rowKeys = new WeakMap<any, number>()
let nextKey = 1

function getRowKey(item: any, index: number) {
    if (item && typeof item === 'object') {
        if (!rowKeys.has(item)) {
            rowKeys.set(item, nextKey++)
        }
        return rowKeys.get(item)
    }
    return index
}

function resetDragHandle() {
    dragHandleActive.value = false
}

onMounted(() => {
    window.addEventListener('mouseup', resetDragHandle)
    window.addEventListener('touchend', resetDragHandle)
})

onUnmounted(() => {
    window.removeEventListener('mouseup', resetDragHandle)
    window.removeEventListener('touchend', resetDragHandle)
})

function onDragStart(event: DragEvent, index: number) {
    if (!dragHandleActive.value) {
        event.preventDefault()
        return
    }
    dragHandleActive.value = false
    draggedIdx.value = index
    if (event.dataTransfer) {
        event.dataTransfer.effectAllowed = 'move'
        event.dataTransfer.dropEffect = 'move'
    }
}

function onDragOver(event: DragEvent, index: number) {
    event.preventDefault()
    if (draggedIdx.value === null || draggedIdx.value === index) return

    const currentArray = Array.isArray(props.modelValue) ? [...props.modelValue] : []
    const draggedItem = currentArray[draggedIdx.value]
    
    // Remove from old position and insert at new position
    currentArray.splice(draggedIdx.value, 1)
    currentArray.splice(index, 0, draggedItem)
    
    draggedIdx.value = index
    emit('update:modelValue', currentArray)
}

function onDragEnd() {
    draggedIdx.value = null
}

// Wizard state and logic
const showWizardModal = ref(false)
const wizardModel = ref<Record<string, any>>({})
const showWizardPassword = ref<Record<string, boolean>>({})
const isGeneratingString = ref(false)
const wizardError = ref('')

const isWizardActive = computed(() => {
    if (!props.field.wizard) return false
    const dependsOnField = props.field.wizard.dependsOn
    if (!dependsOnField) return true
    const val = getDependencyValue(dependsOnField)
    return !!val
})

const currentProviderName = computed(() => {
    if (!props.field.wizard) return ''
    const val = getDependencyValue(props.field.wizard.dependsOn)
    if (!val) return ''
    const config = getProviderConfig(val)
    return config ? config.name : String(val)
})

const wizardFields = computed(() => {
    if (!props.field.wizard || !props.field.wizard.providers) return []
    const providerVal = getDependencyValue(props.field.wizard.dependsOn)
    if (!providerVal) return []
    const config = getProviderConfig(providerVal)
    return config ? config.fields : []
})

function safeGetModelValue(name: string) {
    const obj = props.model || {}
    const parts = name.split(/[.,]/)
    let current = obj
    for (const part of parts) {
        if (current === undefined || current === null) return undefined
        current = current[part]
    }
    return current
}

function getDependencyValue(dependsOnField: string | undefined) {
    if (!dependsOnField || !props.model) return undefined
    
    // 1. Direct nested lookup
    const directVal = safeGetModelValue(dependsOnField)
    if (directVal !== undefined) return directVal
    
    // 2. Case-insensitive Suffix Search in model keys (e.g. "DbProvider" matches "Provider")
    const lowerTarget = dependsOnField.toLowerCase()
    const keys = Object.keys(props.model)
    
    // Exact case-insensitive match
    for (const key of keys) {
        if (key.toLowerCase() === lowerTarget) {
            return props.model[key]
        }
    }
    
    // Suffix match
    for (const key of keys) {
        if (key.toLowerCase().endsWith(lowerTarget)) {
            return props.model[key]
        }
    }
    
    // Contains match
    for (const key of keys) {
        if (key.toLowerCase().includes(lowerTarget)) {
            return props.model[key]
        }
    }
    
    return undefined
}

function getProviderConfig(providerValue: any) {
    if (!props.field.wizard || !props.field.wizard.providers || !providerValue) return null
    
    const providers = props.field.wizard.providers
    const lowerVal = String(providerValue).toLowerCase()
    
    // Direct case-insensitive match
    for (const key of Object.keys(providers)) {
        if (key.toLowerCase() === lowerVal) {
            return { name: key, fields: providers[key] }
        }
    }
    
    // Alias/Synonym mapping
    const mappings: Record<string, string> = {
        'mssql': 'sqlserver',
        'sqlserver': 'sqlserver',
        'postgres': 'postgresql',
        'postgresql': 'postgresql',
        'sqlite': 'sqlite'
    }
    
    const standardVal = mappings[lowerVal] || lowerVal
    
    for (const key of Object.keys(providers)) {
        const standardKey = mappings[key.toLowerCase()] || key.toLowerCase()
        if (standardKey === standardVal) {
            return { name: key, fields: providers[key] }
        }
    }
    
    return null
}

function parseConnectionString(connStr: string): Record<string, string> {
    const result: Record<string, string> = {}
    if (!connStr) return result
    
    // Split by semicolon, but handle possible trailing empty entries
    const pairs = connStr.split(';')
    for (const pair of pairs) {
        if (!pair.trim()) continue
        const eqIdx = pair.indexOf('=')
        if (eqIdx !== -1) {
            const key = pair.substring(0, eqIdx).trim()
            const val = pair.substring(eqIdx + 1).trim()
            result[key] = val
        }
    }
    return result
}

function openWizard() {
    wizardError.value = ''
    
    const parsedConnStr = parseConnectionString(String(props.modelValue || ''))
    const model: Record<string, any> = {}
    const pwShow: Record<string, boolean> = {}
    
    for (const wf of wizardFields.value) {
        const fieldKey = wf.key || wf.name
        const defaultValue = wf.defaultValue !== undefined ? wf.defaultValue : wf.default
        
        // Find in parsed connection string case-insensitively
        let parsedVal: string | undefined = undefined
        const lowerFieldKey = fieldKey.toLowerCase()
        
        for (const [pk, pv] of Object.entries(parsedConnStr)) {
            if (pk.toLowerCase() === lowerFieldKey) {
                parsedVal = pv
                break
            }
        }
        
        if (parsedVal !== undefined) {
            // Convert to target datatype
            if (wf.type === 'number') {
                const num = Number(parsedVal)
                model[fieldKey] = isNaN(num) ? (defaultValue !== undefined ? defaultValue : '') : num
            } else if (wf.type === 'boolean' || wf.type === 'checkbox') {
                const lowerVal = parsedVal.toLowerCase()
                model[fieldKey] = lowerVal === 'true' || lowerVal === '1' || lowerVal === 'yes'
            } else {
                model[fieldKey] = parsedVal
            }
        } else {
            // Fallback to default values or empty
            if (defaultValue !== undefined) {
                model[fieldKey] = defaultValue
            } else if (wf.type === 'boolean' || wf.type === 'checkbox') {
                model[fieldKey] = false
            } else {
                model[fieldKey] = ''
            }
        }
        
        if (wf.type === 'password') {
            pwShow[fieldKey] = false
        }
    }
    
    wizardModel.value = model
    showWizardPassword.value = pwShow
    showWizardModal.value = true
}

function closeWizard() {
    showWizardModal.value = false
    wizardError.value = ''
}

async function generateConnectionString() {
    // Validate required fields
    for (const wf of wizardFields.value) {
        const fieldKey = wf.key || wf.name
        const isRequired = wf.isRequired !== undefined ? wf.isRequired : wf.required
        if (isRequired && (wizardModel.value[fieldKey] === undefined || wizardModel.value[fieldKey] === null || wizardModel.value[fieldKey] === '')) {
            alert(`${wf.label || fieldKey} is required.`)
            return
        }
    }
    
    isGeneratingString.value = true
    wizardError.value = ''
    try {
        const providerVal = getDependencyValue(props.field.wizard.dependsOn)
        
        // Convert all parameter values to strings as expected by the backend Dictionary<string, string>
        const stringifiedParameters: Record<string, string> = {}
        for (const [key, val] of Object.entries(wizardModel.value)) {
            if (val === undefined || val === null) {
                stringifiedParameters[key] = ""
            } else {
                stringifiedParameters[key] = String(val)
            }
        }

        const response = await compileMetadataString(providerVal, stringifiedParameters)
        if (response && response.resultString !== undefined) {
            emit('update:modelValue', response.resultString)
            closeWizard()
        } else {
            throw new Error("Invalid API response format. Expected 'resultString'.")
        }
    } catch (e: any) {
        console.error("Failed to generate connection string", e)
        wizardError.value = e.message || "Failed to generate connection string"
    } finally {
        isGeneratingString.value = false
    }
}
</script>
