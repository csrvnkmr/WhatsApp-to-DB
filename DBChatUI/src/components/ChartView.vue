<template>
<VChart class="h-80" :option="option" autoresize />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import VChart from 'vue-echarts'

const props = defineProps<{ rows:any[], type:string }>()

const option = computed(() => ({
  xAxis: { type: 'category', data: props.rows.map(x => x.name) },
  yAxis: { type: 'value' },
  series: [{ type: props.type, data: props.rows.map(x => x.value), smooth:true }]
}))
</script>