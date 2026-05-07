<template>

<div class="w-72 border-r border-soft bg-panel overflow-auto shrink-0">

    <div class="p-4 border-b border-soft font-bold text-xl">
        Admin
    </div>

    <!-- MAIN -->

    <div class="p-2">

        <RouterLink
            to="/admin/databases"
            class="menu-item">
            Databases
        </RouterLink>

        <RouterLink
            to="/admin/llms"
            class="menu-item">
            LLMs
        </RouterLink>

        <RouterLink
            to="/admin/users"
            class="menu-item">
            Users
        </RouterLink>

        <RouterLink
            to="/admin/defaultsettings"
            class="menu-item">
            Default Settings
        </RouterLink>

    </div>

    <!-- DATABASE SUB MENUS -->

    <div class="px-2 pb-4">

        <div
            v-for="db in databases"
            :key="db.name"
            class="mb-4">

            <div class="px-3 py-2 text-sm font-semibold opacity-70">
                {{ db.name }}
            </div>

            <div class="pl-2 flex flex-col gap-1">

                <RouterLink
                    :to="`/admin/database/${db.name}/schema`"
                    class="submenu-item">
                    Schema
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.name}/roles`"
                    class="submenu-item">
                    Roles
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.name}/systemprompt`"
                    class="submenu-item">
                    Prompt
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.name}/plugins`"
                    class="submenu-item">
                    Plugins
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.name}/extensions`"
                    class="submenu-item">
                    Extensions
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.name}/mailsettings`"
                    class="submenu-item">
                    Mail Settings
                </RouterLink>

            </div>

        </div>

    </div>

</div>

</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'

const databases = ref<any[]>([])

onMounted(async () => {

    const res = await fetch(
        'http://localhost:3000/admin/api/data/databases')

    databases.value = await res.json()
})
</script>

