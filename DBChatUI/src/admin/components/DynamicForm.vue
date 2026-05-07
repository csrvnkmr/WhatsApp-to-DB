<template>

<div class="fixed inset-0 bg-black/40 flex items-center justify-center z-50">

    <div class="w-full max-w-3xl bg-panel rounded-2xl p-6 border border-soft max-h-[90vh] overflow-auto">

        <div class="text-xl font-bold mb-6">
            Edit
        </div>

        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">

            <FieldRenderer
                v-for="field in metadata.fields"
                :key="field.name"
                :field="field"
                v-model="localModel[field.name]" />

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
