<template>

<div class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">

    <div class="w-full max-w-3xl bg-panel rounded-2xl p-6 border border-soft max-h-[90vh] overflow-auto">

        <div class="text-xl font-bold mb-6">
            Edit
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

            <FieldRenderer
                v-for="field in processedFields"
                :key="field.name"
                :field="field"
                :modelValue="getModelValue(field.name)"
                @update:modelValue="val => setModelValue(field.name, val)"
                :model="localModel"
                :database="props.database"
                :class="{'md:col-span-2': field.fulllength}" />

        </div>

        <div class="flex justify-end gap-2 mt-6">

            <button
                @click="$emit('cancel')"
                class="px-4 py-2 rounded-xl border border-soft">
                Cancel
            </button>

            <button
                @click="save"
                class="px-4 py-2 rounded-xl bg-user text-white">
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

const localModel = ref(JSON.parse(JSON.stringify(props.model || {})))

watch(() => props.model, (newVal) => {
    localModel.value = JSON.parse(JSON.stringify(newVal || {}))
}, { deep: true })

function getModelValue(name: string) {
    const parts = name.split(/[.,]/);
    let current = localModel.value;
    for (let i = 0; i < parts.length; i++) {
        if (current === undefined || current === null) return undefined;
        current = current[parts[i]];
    }
    return current;
}

function setModelValue(name: string, value: any) {
    const parts = name.split(/[.,]/);
    let current = localModel.value;
    for (let i = 0; i < parts.length - 1; i++) {
        if (current[parts[i]] === undefined || current[parts[i]] === null) {
            current[parts[i]] = {};
        }
        current = current[parts[i]];
    }
    current[parts[parts.length - 1]] = value;
}

function evaluateConditions(conditions: any) {
    if (!conditions) return true;
    for (const key in conditions) {
        const targetValue = getModelValue(key);
        const condition = conditions[key];
        
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
    if (!props.metadata || !props.metadata.fields) return [];
    
    return props.metadata.fields.filter((field: any) => {
        return evaluateConditions(field.showIf);
    }).map((field: any) => {
        let isRequired = field.required;
        if (field.requiredIf) {
            isRequired = evaluateConditions(field.requiredIf);
        }
        return { ...field, required: isRequired };
    });
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
