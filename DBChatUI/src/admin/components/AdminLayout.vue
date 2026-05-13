<template>

<div class="h-screen flex bg-base text-text overflow-hidden">

    <AdminSidebar ref="sidebarRef" />

    <div class="flex-1 flex flex-col overflow-hidden">
        
        <!-- TOP HEADER -->
        <header class="p-4 flex items-center justify-between border-b border-soft bg-base shrink-0">
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-xl bg-gradient-to-tr from-blue-500 to-indigo-600 flex items-center justify-center text-white shadow-md shadow-blue-500/20 text-lg shrink-0">
                    🛡️
                </div>
                <div class="flex flex-col min-w-0">
                    <span class="text-[10px] font-bold tracking-wider uppercase opacity-50 leading-none mb-0.5">Management Portal</span>
                    <h1 class="text-base font-bold flex items-center gap-1.5 leading-tight">
                        <span class="bg-gradient-to-r from-blue-500 to-indigo-500 dark:from-blue-400 dark:to-indigo-400 bg-clip-text text-transparent">Insight Chat</span> 
                        <span class="font-normal text-xs opacity-70">Admin Panel</span>
                    </h1>
                </div>
            </div>
            
            <div class="flex items-center gap-4">
                <!-- Chat View Link -->
                <router-link to="/" class="px-3 py-1.5 rounded-xl border border-soft hover:bg-hover transition text-sm font-medium">
                    💬 Chat
                </router-link>

                <!-- Theme Toggle -->
                <div class="flex items-center gap-1 bg-panel rounded-xl p-1 border border-soft">
                    <button
                        @click="theme.currentTheme = 'light'"
                        :class="themeBtnClass('light')">
                        ☀️
                    </button>
                    <button
                        @click="theme.currentTheme = 'dark'"
                        :class="themeBtnClass('dark')">
                        🌙
                    </button>
                    <button
                        @click="theme.currentTheme = 'corporate'"
                        :class="themeBtnClass('corporate')">
                        🏢
                    </button>
                </div>
            </div>
        </header>

        <div class="flex-1 overflow-auto">

            <router-view :key="$route.path" @data-changed="onDataChanged" />

        </div>

    </div>

</div>

</template>

<script setup lang="ts">
import { ref } from 'vue'
import AdminSidebar from './AdminSidebar.vue'
import { useThemeStore } from '@/stores/theme'

const theme = useThemeStore()

const themeBtnClass = (name: string) => {
    return [
        "px-3 py-1 rounded-lg text-sm transition",
        theme.currentTheme === name
            ? "bg-white shadow text-black"
            : "text-gray-600 hover:bg-white/50"
    ]
}

const sidebarRef = ref<InstanceType<typeof AdminSidebar> | null>(null)

function onDataChanged(entity: string) {
    if (entity === 'databases' && sidebarRef.value) {
        sidebarRef.value.loadDatabases()
    }
}
</script>
