import { defineStore } from 'pinia'
import { ref, watch } from 'vue'

export const useThemeStore = defineStore('theme', () => {
    const currentTheme = ref(localStorage.getItem('theme') || 'light')

    watch(currentTheme, (v) => {
        document.documentElement.setAttribute('data-theme', v)
        localStorage.setItem('theme', v)
    }, { immediate: true })

    return { currentTheme }
})