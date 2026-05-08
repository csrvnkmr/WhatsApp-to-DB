<template>
<LoginView v-if="!auth.isLoggedIn" />
    <router-view v-else />
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/components/LoginView.vue'
const auth = useAuthStore()
</script>
