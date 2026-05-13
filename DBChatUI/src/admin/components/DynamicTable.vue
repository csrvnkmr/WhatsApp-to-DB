<template>

<div>

    <div class="flex items-center justify-between mb-4">

        <div class="text-2xl font-bold">
            {{ metadata?.title }}
        </div>

        <button
            v-if="!metadata?.maxrecords || rows.length < metadata.maxrecords"
            @click="addNew"
            class="px-4 py-2 rounded-xl bg-user text-white">
            + Add
        </button>

    </div>

    <!-- TABLE -->

    <div class="overflow-auto border border-soft rounded-2xl bg-panel">

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

                    <th class="w-32"></th>

                </tr>
            </thead>

            <tbody>

                <tr
                    v-for="(row, index) in rows"
                    :key="index"
                    class="border-b border-soft hover:bg-hover transition">

                    <template v-for="f in metadata?.fields" :key="f.name">
                        <td
                            v-if="f.showinheader"
                            class="p-3">

                            {{ row[f.name] }}

                        </td>
                    </template>

                    <td class="p-3">

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

</div>

</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import DynamicForm from './DynamicForm.vue'
import { getMetadata, getData, saveData } from '@/services/api'

const props = defineProps<{
    entity: string
    database?: string
}>()

const emit = defineEmits(['data-changed'])

const metadata = ref<any>(null)
const rows = ref<any[]>([])
const editing = ref<any>(null)
const isNew = ref(false)
const originalRow = ref<any>(null)

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

onMounted(() => {
    loadData()
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
