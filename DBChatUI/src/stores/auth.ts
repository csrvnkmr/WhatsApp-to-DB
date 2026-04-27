import { defineStore } from 'pinia'
import { ref } from 'vue'
import { login } from '@/services/api'

export const useAuthStore = defineStore('auth', () => {
    const token = ref(localStorage.getItem('token') || '')
    const message = ref('')
    const isLoggedIn = ref(!!token.value)

    async function doLogin(username: string, password: string) {
        const result = await login(username, password)
        token.value = result.token || ''
        message.value = result.message || ''
        isLoggedIn.value = !!token.value
        localStorage.setItem('token', token.value)
        let user = "";
        if (isLoggedIn.value) {
            user = username;
        }
        localStorage.setItem('username', user)
    }

    function logout() {
        token.value = ''
        isLoggedIn.value = false
        localStorage.removeItem('username')
        localStorage.removeItem('token')
    }

    return { token, message, isLoggedIn, doLogin, logout }
})