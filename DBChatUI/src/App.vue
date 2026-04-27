<template>
<LoginView v-if="!auth.isLoggedIn" />
<HomeView v-else />
</template>

<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import LoginView from '@/components/LoginView.vue'
import HomeView from './views/HomeView.vue'
const auth = useAuthStore()
</script>