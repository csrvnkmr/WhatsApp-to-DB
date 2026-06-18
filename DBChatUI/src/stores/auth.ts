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
        message.value = ''
        try {
            const result =
                await login(username, password)

            token.value =
                result.token || result.Token || ''

            message.value =
                result.message || result.Message || ''

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
        } catch (err: any) {
            token.value = ''
            message.value = err.message || 'An unexpected error occurred.'
            isLoggedIn.value = false
            userName.value = ''
            role.value = ''

            localStorage.removeItem('token')
            localStorage.removeItem('username')
            localStorage.removeItem('role')
        }
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