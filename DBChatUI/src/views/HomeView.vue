<!-- =============================================
src/views/HomeView.vue
FIXES:
1. Mobile header always visible
2. Input bar no longer half hidden
3. Sidebar auto closes when session clicked
============================================= -->

<template>
<div class="h-[100dvh] bg-base flex overflow-hidden">

    <!-- MOBILE OVERLAY -->
    <div
        v-if="sidebarOpen"
        @click="sidebarOpen = false"
        class="fixed inset-0 bg-black/40 z-40 md:hidden">
    </div>

    <!-- SIDEBAR -->
    <aside
        :class="[
            'fixed md:static inset-y-0 left-0 z-50 w-72 bg-slate-100 transform transition-transform duration-300 flex flex-col',
            sidebarOpen
                ? 'translate-x-0'
                : '-translate-x-full md:translate-x-0'
        ]">

        <!-- emits closeMobile when chat clicked -->
        <Sidebar
            @closeMobile="sidebarOpen = false" />

    </aside>

    <!-- MAIN -->
    <div class="flex-1 flex flex-col min-w-0 h-full">

        <!-- MOBILE HEADER ALWAYS VISIBLE -->
        <header
            class="md:hidden shrink-0 h-14 border-b bg-base flex items-center justify-between px-4">

            <button
                @click="sidebarOpen = true"
                class="text-xl px-2 py-1">
                ☰
            </button>

            <div class="font-semibold truncate">
                InsightChat
            </div>

            <div class="w-8"></div>

        </header>

        <!-- CHAT AREA -->
        <main class="flex-1 min-h-0 overflow-hidden">
            <ChatWindow />
        </main>

    </div>

</div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import Sidebar from "@/components/Sidebar.vue";
import ChatWindow from "@/components/ChatWindow.vue";

const sidebarOpen = ref(false);
</script>
