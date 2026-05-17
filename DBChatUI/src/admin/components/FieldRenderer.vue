<template>

<div>

    <label class="block text-sm mb-1 opacity-70">
        {{ field.label }}
        <span v-if="field.required" class="text-red-500 ml-1">*</span>
    </label>

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

</div>

</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch, nextTick } from 'vue'
import { getData } from '@/services/api'

const props = defineProps<{
    field: any
    modelValue: any
    database?: string
    model?: any
}>()

const emit = defineEmits(['update:modelValue'])

const newItemText = ref('')
const fileInputRef = ref<HTMLInputElement | null>(null)
const showPassword = ref(false)

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
</script>
