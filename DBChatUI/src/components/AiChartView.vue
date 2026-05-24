<template>
  <div class="w-full h-full relative flex flex-col">
    <!-- Chart Mode -->
    <VChart 
      v-if="chartType !== 'table'" 
      ref="chartRef"
      class="flex-1 w-full min-h-[500px]" 
      :option="option" 
      autoresize 
    />
    
    <!-- Table Mode -->
    <div v-else class="flex-1 overflow-auto border border-soft rounded-lg bg-panel p-2">
      <table class="w-full text-sm">
        <thead>
          <tr class="border-b border-soft">
            <th class="p-2 text-left font-semibold sticky top-0 bg-panel">{{ xKey }}</th>
            <th class="p-2 text-right font-semibold sticky top-0 bg-panel">{{ yKey }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(row, idx) in chartData" :key="idx" class="border-b border-soft/50 hover:bg-hover">
            <td class="p-2">{{ row[xKey] }}</td>
            <td class="p-2 text-right">{{ row[yKey] }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import { BarChart, LineChart, PieChart, ScatterChart } from 'echarts/charts'
import { TitleComponent, TooltipComponent, LegendComponent, GridComponent } from 'echarts/components'
import VChart from 'vue-echarts'

use([
  CanvasRenderer,
  BarChart,
  LineChart,
  PieChart,
  ScatterChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent
])

const props = defineProps<{
  chartConfig: any
  chartData: any[]
}>()

const chartRef = ref<any>(null)

const chartType = computed(() => {
  const t = props.chartConfig?.ChartType?.toLowerCase() || 'bar'
  return t
})

const xKey = computed(() => props.chartConfig?.XKey || '')
const yKey = computed(() => props.chartConfig?.YKey || '')
const seriesKey = computed(() => props.chartConfig?.SeriesKey || '')

const option = computed(() => {
  const type = chartType.value
  const title = props.chartConfig?.Title || ''
  const data = props.chartData || []

  const baseOption: any = {
    color: [
      '#5470c6', '#91cc75', '#fac858', '#ee6666', '#73c0de', 
      '#3ba272', '#fc8452', '#9a60b4', '#ea7ccc', '#ff9f7f'
    ],
    title: {
      text: title,
      left: 'center',
      textStyle: {
        fontSize: 16,
        fontWeight: 'bold'
      }
    },
    tooltip: {
      trigger: type === 'pie' ? 'item' : 'axis',
      textStyle: {
        fontSize: 14
      }
    },
    grid: {
      left: '5%',
      right: '5%',
      bottom: '15%',
      containLabel: true
    }
  }

  // Multi-series logic
  if (seriesKey.value && ['line_multi', 'grouped_bar', 'stacked_bar'].includes(type)) {
    // Collect unique X values to form the category axis
    const uniqueXSet = new Set<string>();
    data.forEach(d => uniqueXSet.add(d[xKey.value]));
    const uniqueX = Array.from(uniqueXSet);

    // Collect unique Series values
    const uniqueSeriesSet = new Set<string>();
    data.forEach(d => uniqueSeriesSet.add(d[seriesKey.value]));
    const uniqueSeries = Array.from(uniqueSeriesSet);

    // Build quick lookup table: lookup[seriesValue][xValue] = yValue
    const lookup: Record<string, Record<string, number>> = {};
    uniqueSeries.forEach(s => {
      if (s) lookup[s] = {};
    });
    data.forEach(d => {
      const sVal = d[seriesKey.value];
      const xVal = d[xKey.value];
      if (sVal && xVal && lookup[sVal]) {
        lookup[sVal]![xVal] = d[yKey.value];
      }
    });

    const seriesArray = uniqueSeries.map(s => {
      const sData = uniqueX.map(x => {
        if (s && x) {
          const entry = lookup[s];
          if (entry) {
            return entry[x] ?? 0;
          }
        }
        return 0;
      });
      return {
        name: s,
        type: type === 'line_multi' ? 'line' : 'bar',
        stack: type === 'stacked_bar' ? 'total' : undefined,
        data: sData,
        smooth: type === 'line_multi',
        emphasis: { focus: 'series' }
      };
    });

    return {
      ...baseOption,
      legend: {
        type: 'scroll',
        top: 30,
        textStyle: { fontSize: 12 }
      },
      grid: {
        ...baseOption.grid,
        top: 70
      },
      xAxis: {
        type: 'category',
        data: uniqueX,
        axisLabel: {
          rotate: 45,
          hideOverlap: true,
          fontSize: 11,
          formatter: function (value: string) {
            if (!value) return '';
            return value.length > 20 ? value.substring(0, 20) + '...' : value;
          }
        }
      },
      yAxis: {
        type: 'value',
        axisLabel: { fontSize: 12 }
      },
      series: seriesArray
    }
  }

  // Single-series logic
  let xData = []
  let yData = []
  let pieData = []

  for (const row of data) {
    xData.push(row[xKey.value])
    yData.push(row[yKey.value])
    pieData.push({ name: row[xKey.value], value: row[yKey.value] })
  }

  if (type === 'pie') {
    return {
      ...baseOption,
      series: [
        {
          type: 'pie',
          radius: '60%',
          data: pieData,
          colorBy: 'data',
          label: {
            fontSize: 14
          },
          emphasis: {
            itemStyle: {
              shadowBlur: 10,
              shadowOffsetX: 0,
              shadowColor: 'rgba(0, 0, 0, 0.5)'
            }
          }
        }
      ]
    }
  }

  // Bar, Line, Scatter (single series)
  return {
    ...baseOption,
    xAxis: {
      type: 'category',
      data: xData,
      axisLabel: {
        rotate: 45,
        hideOverlap: true,
        fontSize: 11,
        formatter: function (value: string) {
          if (!value) return '';
          return value.length > 20 ? value.substring(0, 20) + '...' : value;
        }
      }
    },
    yAxis: {
      type: 'value',
      axisLabel: {
        fontSize: 12
      }
    },
    series: [
      {
        type: type === 'scatter' ? 'scatter' : (type === 'line' ? 'line' : 'bar'),
        data: yData,
        smooth: type === 'line',
        colorBy: type === 'bar' ? 'data' : 'series',
        itemStyle: {
          borderRadius: type === 'bar' ? [4, 4, 0, 0] : 0
        }
      }
    ]
  }
})

// Expose a method to get data URL for downloading
function getDataURL() {
  if (chartType.value === 'table') {
    // For table, we cannot natively export image via echarts.
    // Return empty or handle gracefully.
    return null;
  }
  return chartRef.value?.getDataURL({
    type: 'png',
    pixelRatio: 2,
    backgroundColor: '#fff'
  })
}

function getOption() {
  return option.value;
}

defineExpose({
  getDataURL,
  getOption
})
</script>
