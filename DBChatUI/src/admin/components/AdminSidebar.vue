<template>

<div class="w-full md:w-72 h-full border-r border-soft bg-panel overflow-auto shrink-0 flex flex-col">

    <!-- WELCOME & LOGOUT -->
    <div class="p-4 border-b border-soft shrink-0">

        <div class="flex items-center justify-between">

            <!-- Left: Welcome + Username inline -->
            <div class="flex items-center gap-2 min-w-0">

                <span class="text-sm text-gray-500">
                    Welcome
                </span>

                <span class="font-semibold text-base truncate max-w-[140px]">
                    {{ userName }}
                </span>

            </div>

            <!-- Right: Logout -->
            <button
                @click="logout"
                class="text-xs px-2 py-1 rounded-md text-gray-500 hover:text-black hover:bg-hover transition">
                Logout
            </button>

        </div>

    </div>

    <div class="p-2 text-sm shrink-0">

        <RouterLink
            to="/admin/databases"
            @click="emit('closeMobile')"
            class="menu-item flex items-center gap-2">
            <span>🗄️</span> Databases
        </RouterLink>

        <RouterLink
            to="/admin/llms"
            @click="emit('closeMobile')"
            class="menu-item flex items-center gap-2">
            <span>🤖</span> LLMs
        </RouterLink>

        <RouterLink
            to="/admin/users"
            @click="emit('closeMobile')"
            class="menu-item flex items-center gap-2">
            <span>👥</span> Users
        </RouterLink>

        <RouterLink
            to="/admin/defaultsettings"
            @click="emit('closeMobile')"
            class="menu-item flex items-center gap-2">
            <span>⚙️</span> Default Settings
        </RouterLink>

        <RouterLink
            to="/admin/defaultfolders"
            @click="emit('closeMobile')"
            class="menu-item flex items-center gap-2">
            <span>📁</span> Default Folders
        </RouterLink>

    </div>

    <!-- DATABASE SUB MENUS -->

    <div class="px-2 pb-4 flex-1 overflow-auto">

        <div
            v-for="db in databases"
            :key="db.Name"
            class="mb-4">

            <div
                class="px-3 py-1.5 text-sm font-semibold opacity-70 cursor-pointer flex justify-between items-center hover:opacity-100 transition"
                @click="toggleDb(db.Name)">
                <span class="flex items-center gap-2">📂 {{ db.Name }}</span>
                <svg
                    class="w-3.5 h-3.5 transition-transform duration-200"
                    :class="{ 'rotate-180': expandedDbs[db.Name] }"
                    fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
                </svg>
            </div>

            <div v-show="expandedDbs[db.Name]" class="pl-5 flex flex-col gap-0.5">

                <div class="px-1 py-1 font-bold opacity-50 text-[10px] uppercase tracking-wider mt-1 flex items-center gap-2">
                    🛠️ Schema
                </div>

                <div class="pl-3 flex flex-col gap-0.5">
                    <RouterLink
                        :to="`/admin/database/${db.Name}/modules`"
                        @click="emit('closeMobile')"
                        class="submenu-item flex items-center gap-2 text-xs py-1">
                        <span>🧩</span> Modules
                    </RouterLink>

                    <RouterLink
                        :to="`/admin/database/${db.Name}/tables`"
                        @click="emit('closeMobile')"
                        class="submenu-item flex items-center gap-2 text-xs py-1">
                        <span>📊</span> Tables
                    </RouterLink>

                    <RouterLink
                        :to="`/admin/database/${db.Name}/tablejoins`"
                        @click="emit('closeMobile')"
                        class="submenu-item flex items-center gap-2 text-xs py-1">
                        <span>🔗</span> Table Joins
                    </RouterLink>
                  </div>

                <RouterLink
                    :to="`/admin/database/${db.Name}/roles`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>🔑</span> Roles
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.Name}/systemprompt`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>📝</span> Prompt
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.Name}/plugins`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>🔌</span> Plugins
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.Name}/extensions`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>➕</span> Extensions
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.Name}/mailsettings`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>📧</span> Mail Settings
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.Name}/fewshotqueries`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>💡</span> Few Shot Queries
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.Name}/vectorconfigurations`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>📐</span> Vector Configurations
                </RouterLink>

                <RouterLink
                    :to="`/admin/database/${db.Name}/vectordbsettings`"
                    @click="emit('closeMobile')"
                    class="submenu-item flex items-center gap-2 text-xs py-1">
                    <span>⚙️</span> Vector DB Settings
                </RouterLink>

            </div>

        </div>

    </div>

</div>

</template>

<script setup lang="ts">
import { getDatabases } from '@/services/api'
import { onMounted, ref, computed } from 'vue'
import { useAuthStore } from '@/stores/auth'

const emit = defineEmits(["closeMobile"])
const auth = useAuthStore()
const BASE_URL = "http://localhost:3000"

const userName = computed(() =>
    localStorage.getItem("username")
    || auth.userName
    || "User"
)

const databases = ref<any[]>([])
const expandedDbs = ref<Record<string, boolean>>({})

function toggleDb(name: string) {
    expandedDbs.value[name] = !expandedDbs.value[name]
}

async function loadDatabases() {
    console.log("Loading databases")
    databases.value = await getDatabases()
    console.log(databases.value)
}

onMounted(async () => {
    loadDatabases()
})

function authHeader() {
    const token = localStorage.getItem("token") || ""
    return {
        "Authorization": `Bearer ${token}`
    }
}

async function logout() {
    await fetch(
        `${BASE_URL}/logout`,
        {
            credentials: 'include',
            method: "POST",
            headers: authHeader()
        }
    )

    localStorage.removeItem("token")
    localStorage.removeItem("username")

    location.reload()
}

defineExpose({
    loadDatabases
})
</script>

