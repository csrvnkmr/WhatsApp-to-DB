<template>

<div>

    <div v-if="!editing && !isEditingMetadata" class="flex items-center justify-between mb-4">

        <div class="flex items-center gap-6">
            <div class="text-2xl font-bold">
                {{ metadata?.title }}
            </div>
            
            <button
                v-if="metadata"
                @click="startEditMetadata"
                class="text-user hover:underline text-sm font-medium transition">
                Edit Metadata
            </button>
            
            <div v-if="actions.length > 0" class="flex items-center gap-4 text-sm mt-1">
                <button
                    v-for="(act, i) in actions"
                    :key="i"
                    @click="onActionClick(act)"
                    :disabled="isExecutingAction"
                    class="text-user hover:underline disabled:opacity-50 disabled:cursor-not-allowed font-medium transition flex items-center gap-1">
                    <span v-if="isExecutingAction" class="w-3 h-3 border-2 border-user border-t-transparent rounded-full animate-spin"></span>
                    {{ act.label }}: {{ act.value }}
                </button>
            </div>
        </div>

        <button
            v-if="!metadata?.maxrecords || rows.length < metadata.maxrecords"
            @click="addNew"
            class="px-4 py-2 rounded-xl bg-user text-white shadow-sm hover:opacity-90 transition">
            + Add
        </button>

    </div>

    <!-- TABLE -->

    <div v-if="!editing && !isEditingMetadata" class="overflow-auto border border-soft rounded-2xl bg-panel">

        <table class="w-full text-sm">

            <thead>
                <tr class="border-b border-soft">

                    <template v-for="f in metadata?.fields" :key="f.name">
                        <th
                            v-if="f.showinheader"
                            class="text-left p-3">

                            {{ f.label }}

                        </th>
                    </template>

                    <th class="w-32 sticky right-0 bg-panel z-10 shadow-[-4px_0_10px_-4px_rgba(0,0,0,0.05)]"></th>

                </tr>
            </thead>

            <tbody>

                <tr
                    v-for="(row, index) in rows"
                    :key="index"
                    class="group border-b border-soft hover:bg-hover transition">

                    <template v-for="f in metadata?.fields" :key="f.name">
                        <td
                            v-if="f.showinheader"
                            class="p-3">

                            {{ getRowValue(row, f.name) }}

                        </td>
                    </template>

                    <td class="p-3 sticky right-0 bg-panel group-hover:bg-hover z-10 shadow-[-4px_0_10px_-4px_rgba(0,0,0,0.05)]">

                        <div class="flex gap-2">

                            <button
                                @click="edit(row)"
                                class="px-2 py-1 rounded bg-hover text-xs">
                                Edit
                            </button>

                            <button
                                @click="remove(row)"
                                class="px-2 py-1 rounded bg-hover text-xs">
                                Delete
                            </button>

                        </div>

                    </td>

                </tr>

            </tbody>

        </table>

    </div>

    <!-- FORM -->

    <DynamicForm
        v-if="editing"
        :metadata="metadata"
        :model="editing"
        :database="props.database"
        @save="save"
        @cancel="editing = null" />

    <!-- METADATA FORM -->

    <DynamicForm
        v-if="isEditingMetadata"
        :metadata="metadataSchema"
        :model="metadataModel"
        @save="saveMetadataForm"
        @cancel="isEditingMetadata = false" />

</div>

</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import DynamicForm from './DynamicForm.vue'
import { getMetadata, getData, saveData, getActions, executeAction, saveMetadata } from '@/services/api'

const props = defineProps<{
    entity: string
    database?: string
}>()

const emit = defineEmits(['data-changed'])

function getRowValue(row: any, name: string) {
    if (!row || !name) return '';
    const parts = name.split(/[.,]/);
    let current = row;
    for (const part of parts) {
        if (current === undefined || current === null) return '';
        current = current[part];
    }
    return current;
}

const metadata = ref<any>(null)
const rows = ref<any[]>([])
const editing = ref<any>(null)
const isNew = ref(false)
const originalRow = ref<any>(null)

const actions = ref<any[]>([])
const isExecutingAction = ref(false)

const isEditingMetadata = ref(false)
const metadataSchema = ref<any>(null)
const metadataModel = ref<any>(null)

async function startEditMetadata() {
    try {
        metadataSchema.value = await getMetadata('metadata')
        metadataModel.value = await getMetadata(props.entity)
        isEditingMetadata.value = true
    } catch (e) {
        console.error("Failed to load metadata for editing", e)
        alert("Failed to load metadata details.")
    }
}

async function saveMetadataForm(updatedMetadata: any) {
    try {
        await saveMetadata(props.entity, updatedMetadata)
        isEditingMetadata.value = false
        await loadData()
        await loadActions()
    } catch (e) {
        console.error("Failed to save metadata", e)
        alert("Failed to save metadata.")
    }
}

async function loadData() {
    try {
        console.log("LoadData on entity page for", props.entity)
        metadata.value = await getMetadata(props.entity)
        rows.value = await getData(props.entity, props.database)
        if (!Array.isArray(rows.value)) {
            rows.value = []
        }
    } catch (e) {
        console.error("Failed to load data", e)
    }
}

async function loadActions() {
    if (!props.database || !props.entity) return;
    try {
        const fetchedActions = await getActions(props.database, props.entity);
        if (Array.isArray(fetchedActions)) {
            actions.value = fetchedActions;
        } else {
            actions.value = [];
        }
    } catch (e) {
        console.error("Failed to load actions", e);
        actions.value = [];
    }
}

async function onActionClick(actionItem: any) {
    if (isExecutingAction.value) return;
    isExecutingAction.value = true;
    try {
        let finalActionUrl = actionItem.action;
        if (props.database) {
            finalActionUrl = finalActionUrl.replace('{database}', props.database);
        }
        await executeAction(finalActionUrl, actionItem.method);
    } catch (e) {
        console.error("Action execution failed", e);
        alert("Failed to execute action: " + (e as Error).message);
    } finally {
        setTimeout(async () => {
            await loadActions();
            isExecutingAction.value = false;
        }, 500);
    }
}

onMounted(() => {
    loadData()
    loadActions()
})

function addNew() {
    if (metadata.value?.maxrecords && rows.value.length >= metadata.value.maxrecords) {
        alert(`Maximum of ${metadata.value.maxrecords} records allowed.`)
        return
    }
    editing.value = {}
    isNew.value = true
    originalRow.value = null
}

function edit(row: any) {
    editing.value = { ...row }
    originalRow.value = row
    isNew.value = false
}

async function remove(row: any) {
    if (!confirm('Are you sure you want to delete this item?')) return

    const index = rows.value.indexOf(row)
    if (index !== -1) {
        rows.value.splice(index, 1)
        await saveData(props.entity, rows.value, props.database)
        emit('data-changed', props.entity)
    }
}

async function save(updatedRow: any) {
    // Validate uniqueness constraint
    if (metadata.value?.fields) {
        for (const field of metadata.value.fields) {
            if (field.unique) {
                const val = getRowValue(updatedRow, field.name);
                if (val !== undefined && val !== null && val !== '') {
                    // Check if any other row has this exact value
                    const duplicate = rows.value.find(r => r !== originalRow.value && getRowValue(r, field.name) === val);
                    if (duplicate) {
                        alert(`The value for ${field.label || field.name} must be unique. "${val}" is already in use.`);
                        return; // Abort save, keep form open
                    }
                }
            }
        }
    }

    if (isNew.value) {
        rows.value.push(updatedRow)
    } else {
        const index = rows.value.indexOf(originalRow.value)
        if (index !== -1) {
            rows.value[index] = updatedRow
        }
    }

    try {
        await saveData(props.entity, rows.value, props.database)
        emit('data-changed', props.entity)
    } catch (e) {
        console.error("Failed to save data", e)
        alert("Failed to save data")
    }
    editing.value = null
    originalRow.value = null
}
</script>
