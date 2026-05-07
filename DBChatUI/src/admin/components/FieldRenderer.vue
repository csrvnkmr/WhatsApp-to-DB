<template>

<div>

    <label class="block text-sm mb-1 opacity-70">
        {{ field.label }}
    </label>

    <!-- TEXT -->
    <input
        v-if="field.type === 'text'"
        :value="modelValue"
        @input="$emit('update:modelValue', $event.target.value)"
        class="w-full rounded-xl border border-soft bg-base px-3 py-2" />

    <!-- TEXTAREA -->
    <textarea
        v-else-if="field.type === 'textarea'"
        :value="modelValue"
        @input="$emit('update:modelValue', $event.target.value)"
        rows="5"
        class="w-full rounded-xl border border-soft bg-base px-3 py-2" />

    <!-- SELECT -->
    <select
        v-else-if="field.type === 'select'"
        :value="modelValue"
        @change="$emit('update:modelValue', $event.target.value)"
        class="w-full rounded-xl border border-soft bg-base px-3 py-2">

        <option
            v-for="o in field.options"
            :key="o"
            :value="o">
            {{ o }}
        </option>

    </select>

</div>

</template>
