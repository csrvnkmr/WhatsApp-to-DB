import { defineStore } from 'pinia'
import { ref } from 'vue'
import { login } from '@/services/api'

export const useAuthStore = defineStore('auth', () => {

    const token =
        ref(localStorage.getItem('token') || '')

    const userName =
        ref(localStorage.getItem('username') || '')

    const message =
        ref('')

    const isLoggedIn =
        ref(!!token.value)

    async function doLogin(
        username: string,
        password: string
    ) {
        const result =
            await login(username, password)

        token.value =
            result.token || ''

        message.value =
            result.message || ''

        isLoggedIn.value =
            !!token.value

        userName.value =
            isLoggedIn.value
                ? username
                : ''

        localStorage.setItem(
            'token',
            token.value
        )

        localStorage.setItem(
            'username',
            userName.value
        )
    }

    function logout() {

        token.value = ''
        userName.value = ''
        isLoggedIn.value = false

        localStorage.removeItem('token')
        localStorage.removeItem('username')
    }

    return {
        token,
        userName,
        message,
        isLoggedIn,
        doLogin,
        logout
    }
})