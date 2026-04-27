<!-- ============================================= -->
<!-- src/views/HomeView.vue -->
<!-- ============================================= -->
<template>
<div class="h-screen flex">

    <!-- Desktop Sidebar -->
    <div class="hidden md:block">
        <Sidebar />
    </div>

    <!-- Mobile Sidebar -->
    <div class="md:hidden fixed z-50">
        <Sidebar />
    </div>

    <ChatWindow />

</div>
</template>

<script setup lang="ts">
import Sidebar from "@/components/Sidebar.vue";
import ChatWindow from "@/components/ChatWindow.vue";
</script>