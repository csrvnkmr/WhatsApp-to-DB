<template>
<div class="h-screen flex items-center justify-center">
  <div class="panel p-8 rounded-2xl w-full max-w-md space-y-4">
    <div class="text-2xl font-bold">InsightChat Login</div>
    <input v-model="username" class="w-full border p-3 rounded-xl" placeholder="Username" />
    <input v-model="password" type="password" class="w-full border p-3 rounded-xl" placeholder="Password" />
    <button @click="submit" class="w-full bg-user p-3 rounded-xl">Login</button>
    <div class="text-red-500 text-sm">{{ auth.message }}</div>
  </div>
</div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const username = ref('')
const password = ref('')

async function submit(){
  await auth.doLogin(username.value, password.value)
}
</script>
