<template>
<div class="h-screen flex items-center justify-center bg-base p-4">
  <div class="panel p-8 rounded-2xl w-full max-w-md space-y-6 shadow-xl border border-soft bg-panel">
    <!-- Header -->
    <div class="text-center space-y-1">
      <div class="text-2xl font-bold bg-gradient-to-r from-blue-500 to-indigo-500 dark:from-blue-400 dark:to-indigo-400 bg-clip-text text-transparent">InsightChat</div>
      <div class="text-xs opacity-75">
        {{ mode === 'login' ? 'Sign in to query your databases' : 'Create a Temporary User' }}
      </div>
    </div>
    
    <!-- Inputs -->
    <div class="space-y-4">
      <div>
        <label class="block text-xs font-semibold mb-1 opacity-75">Username</label>
        <input 
          v-model="username" 
          :disabled="loading"
          class="w-full border border-soft p-3 rounded-xl bg-base text-main outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition disabled:opacity-60" 
          placeholder="Enter username" 
        />
      </div>
      <div>
        <label class="block text-xs font-semibold mb-1 opacity-75">Password</label>
        <input 
          v-model="password" 
          type="password" 
          :disabled="loading"
          class="w-full border border-soft p-3 rounded-xl bg-base text-main outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-500 transition disabled:opacity-60" 
          placeholder="Enter password" 
          @keyup.enter="submit"
        />
      </div>
    </div>

    <!-- Submit Button -->
    <button 
      :disabled="loading || !username.trim() || !password.trim()" 
      @click="submit" 
      class="w-full bg-user text-white p-3 rounded-xl font-semibold hover:opacity-90 disabled:opacity-50 transition-all duration-200 flex items-center justify-center gap-2 shadow-md shadow-blue-500/10 cursor-pointer disabled:cursor-not-allowed"
    >
      <!-- Loading Spinner -->
      <svg v-if="loading" class="animate-spin h-5 w-5 text-white" fill="none" viewBox="0 0 24 24">
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
      </svg>
      <span>
        {{ mode === 'login' 
            ? (loading ? 'Signing in...' : 'Sign In') 
            : (loading ? 'Creating User...' : 'Create Temporary User') 
        }}
      </span>
    </button>

    <!-- Error/Success Messages -->
    <div v-if="auth.message && mode === 'login'" class="text-red-500 text-sm text-center font-medium bg-red-500/10 p-3 rounded-xl border border-red-500/20">
      ⚠️ {{ auth.message }}
    </div>

    <div v-if="errorMessage && mode === 'temp'" class="text-red-500 text-sm text-center font-medium bg-red-500/10 p-3 rounded-xl border border-red-500/20">
      ⚠️ {{ errorMessage }}
    </div>

    <div v-if="successMessage" class="text-emerald-500 text-sm text-center font-medium bg-emerald-500/10 p-3 rounded-xl border border-emerald-500/20">
      ✅ {{ successMessage }}
    </div>

    <!-- Toggle Mode Link -->
    <div class="text-center pt-2">
      <button 
        @click="toggleMode"
        :disabled="loading"
        class="text-xs text-blue-500 hover:underline font-semibold cursor-pointer disabled:opacity-50 bg-transparent border-none outline-none"
      >
        {{ mode === 'login' ? 'Create Temporary User' : 'Back to Sign In' }}
      </button>
    </div>
  </div>
</div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { addTempUser } from '@/services/api'

const auth = useAuthStore()
const username = ref('')
const password = ref('')
const loading = ref(false)
const mode = ref<'login' | 'temp'>('login')
const errorMessage = ref('')
const successMessage = ref('')

function toggleMode() {
  mode.value = mode.value === 'login' ? 'temp' : 'login'
  errorMessage.value = ''
  successMessage.value = ''
  username.value = ''
  password.value = ''
  auth.message = ''
}

async function submit() {
  if (!username.value.trim() || !password.value.trim() || loading.value) return
  loading.value = true
  errorMessage.value = ''
  successMessage.value = ''
  auth.message = ''
  
  try {
    if (mode.value === 'login') {
      await auth.doLogin(username.value, password.value)
    } else {
      const res = await addTempUser(username.value, password.value)
      if (res.success || res.Message || res.message) {
        successMessage.value = res.message || res.Message || 'Temporary user created successfully. Please sign in.'
        username.value = ''
        password.value = ''
        mode.value = 'login'
      }
    }
  } catch (err: any) {
    if (mode.value === 'login') {
      // doLogin sets auth.message inside auth store
    } else {
      errorMessage.value = err.message || 'An error occurred while creating temporary user.'
    }
  } finally {
    loading.value = false
  }
}
</script>
