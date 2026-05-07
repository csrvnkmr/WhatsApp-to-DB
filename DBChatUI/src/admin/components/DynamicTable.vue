<template>

<div>

    <div class="flex items-center justify-between mb-4">

        <div class="text-2xl font-bold">
            {{ metadata?.title }}
        </div>

        <button
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

                    <th
                        v-for="f in metadata?.fields"
                        :key="f.name"
                        class="text-left p-3">

                        {{ f.label }}

                    </th>

                    <th class="w-32"></th>

                </tr>
            </thead>

            <tbody>

                <tr
                    v-for="row in rows"
                    :key="row.id"
                    class="border-b border-soft hover:bg-hover transition">

                    <td
                        v-for="f in metadata?.fields"
                        :key="f.name"
                        class="p-3">

                        {{ row[f.name] }}

                    </td>

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
        @save="save"
        @cancel="editing = null" />

</div>

</template>
