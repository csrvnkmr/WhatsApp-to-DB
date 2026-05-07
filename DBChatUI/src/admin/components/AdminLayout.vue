<template>

<div class="h-screen flex bg-base text-text overflow-hidden">

    <AdminSidebar />

    <div class="flex-1 overflow-auto">

        <router-view />

    </div>

</div>

</template>

<script setup lang="ts">
import AdminSidebar from './AdminSidebar.vue'
</script>
