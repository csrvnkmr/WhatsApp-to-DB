import { defineStore } from 'pinia'
import { ref } from 'vue'
import { login } from '@/services/api'

export const useAuthStore = defineStore('auth', () => {

    const token =
        ref(localStorage.getItem('token') || '')

    const userName =
        ref(localStorage.getItem('username') || '')

    const role =
        ref(localStorage.getItem('role') || '')

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

        role.value =
            isLoggedIn.value
                ? (result.role || result.Role || '')
                : ''

        localStorage.setItem(
            'token',
            token.value
        )

        localStorage.setItem(
            'username',
            userName.value
        )

        localStorage.setItem(
            'role',
            role.value
        )
    }

    function logout() {

        token.value = ''
        userName.value = ''
        role.value = ''
        isLoggedIn.value = false

        localStorage.removeItem('token')
        localStorage.removeItem('username')
        localStorage.removeItem('role')
    }

    return {
        token,
        userName,
        role,
        message,
        isLoggedIn,
        doLogin,
        logout
    }
})