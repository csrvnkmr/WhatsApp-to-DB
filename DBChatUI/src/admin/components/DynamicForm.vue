<template>

<div class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">

    <div class="w-full max-w-3xl bg-panel rounded-2xl p-6 border border-soft max-h-[90vh] overflow-auto">

        <div class="text-xl font-bold mb-6">
            {{ isNewRecord ? 'Add' : 'Edit' }} {{ metadata?.title || 'Item' }}
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

            <FieldRenderer
                v-for="field in processedFields"
                :key="field.name"
                :field="field"
                :modelValue="getModelValue(field.name)"
                @update:modelValue="val => setModelValue(field.name, val)"
                @row-editing-change="val => activeRowEditors[field.name] = val"
                :model="localModel"
                :database="props.database"
                :class="{'md:col-span-2': field.fulllength}" />

        </div>

        <div class="flex justify-end gap-2 mt-6">

            <button
                @click="$emit('cancel')"
                :disabled="isAnyRowEditing"
                class="px-4 py-2 rounded-xl border border-soft disabled:opacity-40 disabled:cursor-not-allowed transition">
                Cancel
            </button>

            <button
                @click="save"
                :disabled="isAnyRowEditing"
                class="px-4 py-2 rounded-xl bg-user text-white disabled:opacity-40 disabled:cursor-not-allowed transition">
                Save
            </button>

        </div>

    </div>

</div>

</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import FieldRenderer from './FieldRenderer.vue'

const props = defineProps<{
    metadata: any
    model: any
    database?: string
}>()

const emit = defineEmits(['save', 'cancel'])

const isNewRecord = computed(() => {
    return !props.model || Object.keys(props.model).length === 0
})

const activeRowEditors = ref<Record<string, boolean>>({})
const isAnyRowEditing = computed(() => {
    return Object.values(activeRowEditors.value).some(val => val)
})

const localModel = ref(JSON.parse(JSON.stringify(props.model || {})))

watch(() => props.model, (newVal) => {
    localModel.value = JSON.parse(JSON.stringify(newVal || {}))
}, { deep: true })

function getModelValue(name: string) {
    const parts = name.split(/[.,]/);
    let current = localModel.value;
    for (let i = 0; i < parts.length; i++) {
        if (current === undefined || current === null) return undefined;
        const key = parts[i] as string;
        current = current[key];
    }
    return current;
}

function setModelValue(name: string, value: any) {
    const parts = name.split(/[.,]/);
    let current = localModel.value;
    for (let i = 0; i < parts.length - 1; i++) {
        const key = parts[i] as string;
        if (current[key] === undefined || current[key] === null) {
            current[key] = {};
        }
        current = current[key];
    }
    const lastKey = parts[parts.length - 1] as string;
    current[lastKey] = value;
}

function evaluateConditions(conditions: any) {
    if (!conditions) return true;
    
    let parsedConditions = conditions;
    if (typeof conditions === 'string') {
        try {
            parsedConditions = JSON.parse(conditions);
        } catch (e) {
            console.warn("Condition string is not valid JSON:", conditions);
            return true;
        }
    }
    
    for (const key in parsedConditions) {
        const targetValue = getModelValue(key);
        const condition = parsedConditions[key];
        
        if (condition && typeof condition === 'object' && !Array.isArray(condition) && 'op' in condition) {
            const { op, value } = condition;
            switch(op) {
                case '==':
                case '=': if (targetValue != value) return false; break;
                case '!=':
                case '<>': if (targetValue != value) return false; break;
                case '>': if (targetValue <= value) return false; break;
                case '>=': if (targetValue < value) return false; break;
                case '<': if (targetValue >= value) return false; break;
                case '<=': if (targetValue > value) return false; break;
                case 'contains': 
                    if (!targetValue || typeof targetValue.includes !== 'function' || !targetValue.includes(value)) return false; 
                    break;
                case 'in':
                    if (!Array.isArray(value) || !value.includes(targetValue)) return false;
                    break;
                case 'not in':
                    if (Array.isArray(value) && value.includes(targetValue)) return false;
                    break;
                case 'empty':
                    if (targetValue !== undefined && targetValue !== null && targetValue !== '' && !(typeof targetValue === 'string' && targetValue.trim() === '')) return false;
                    break;
                case 'not empty':
                    if (targetValue === undefined || targetValue === null || targetValue === '' || (typeof targetValue === 'string' && targetValue.trim() === '')) return false;
                    break;
                default:
                    return false;
            }
        } else {
            // direct equality
            if (targetValue !== condition) return false;
        }
    }
    return true; // All conditions passed
}

const processedFields = computed(() => {
    if (!props.metadata || !props.metadata.fields) {
        console.log("[DynamicForm debug] No metadata or fields found:", props.metadata);
        return [];
    }
    
    console.log("[DynamicForm debug] All metadata fields:", props.metadata.fields);
    console.log("[DynamicForm debug] Current model:", props.model);

    const filtered = props.metadata.fields.filter((field: any) => {
        const show = evaluateConditions(field.showIf);
        console.log(`[DynamicForm debug] Field: ${field.name}, showIf:`, field.showIf, "-> show:", show);
        return show;
    }).map((field: any) => {
        let isRequired = field.required;
        if (field.requiredIf) {
            isRequired = evaluateConditions(field.requiredIf);
        }
        return { ...field, required: isRequired };
    });

    console.log("[DynamicForm debug] Processed/rendered fields:", filtered.map((f: any) => f.name));
    return filtered;
});

function save() {
    for (const field of processedFields.value) {
        if (field.required) {
            const val = getModelValue(field.name);
            if (val === undefined || val === null || val === '') {
                alert(`${field.label} is required.`);
                return;
            }
        }
    }

    emit('save', localModel.value)
}
</script>
