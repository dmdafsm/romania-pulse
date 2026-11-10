// Romania Pulse — the only place charts are configured.
// The server sends each chart as a <canvas data-chart="{json}">; this file draws it.
(() => {
    'use strict';

    const token = (name) => getComputedStyle(document.body).getPropertyValue(name).trim();
    const reducedMotion = matchMedia('(prefers-reduced-motion: reduce)').matches;

    const format = (value, payload) => {
        if (value === null || value === undefined) {
            return '–';
        }
        const text = value.toLocaleString('en-GB', {
            minimumFractionDigits: payload.decimals,
            maximumFractionDigits: payload.decimals,
        });
        return text.replace('-', '−') + payload.suffix;
    };

    // Breaks a long category name into lines so it is never clipped on narrow cards.
    const wrap = (label, width) => {
        const lines = [];
        label.split(' ').forEach((word) => {
            const last = lines[lines.length - 1];
            if (last !== undefined && `${last} ${word}`.length <= width) {
                lines[lines.length - 1] = `${last} ${word}`;
            } else {
                lines.push(word);
            }
        });
        return lines.length > 1 ? lines : label;
    };

    // Vertical guide at the hovered period, so values across series line up visually.
    const crosshair = {
        id: 'crosshair',
        afterDatasetsDraw(chart) {
            const active = chart.tooltip?.getActiveElements?.() ?? [];
            if (chart.options.indexAxis === 'y' || active.length === 0) {
                return;
            }
            const { ctx, chartArea } = chart;
            const x = active[0].element.x;
            ctx.save();
            ctx.beginPath();
            ctx.moveTo(x, chartArea.top);
            ctx.lineTo(x, chartArea.bottom);
            ctx.lineWidth = 1;
            ctx.strokeStyle = token('--chart-ink');
            ctx.globalAlpha = 0.5;
            ctx.stroke();
            ctx.restore();
        },
    };

    // Value printed at the end of each horizontal bar, so breakdowns read without hovering.
    const barLabels = {
        id: 'barLabels',
        afterDatasetsDraw(chart, _args, options) {
            if (chart.options.indexAxis !== 'y') {
                return;
            }
            const { ctx } = chart;
            ctx.save();
            ctx.font = `500 12px ${token('--font') || 'sans-serif'}`;
            ctx.fillStyle = token('--on-surface');
            ctx.textBaseline = 'middle';
            chart.getDatasetMeta(0).data.forEach((bar, i) => {
                const value = chart.data.datasets[0].data[i];
                if (value === null) {
                    return;
                }
                ctx.textAlign = value < 0 ? 'right' : 'left';
                ctx.fillText(format(value, options.payload), bar.x + (value < 0 ? -6 : 6), bar.y);
            });
            ctx.restore();
        },
    };

    function draw(canvas) {
        const payload = JSON.parse(canvas.dataset.chart);
        const horizontal = payload.kind === 'hbar';
        const isLine = payload.kind === 'line';
        const colors = [1, 2, 3, 4].map((n) => token(`--series-${n}`));
        const ink = token('--chart-ink');
        const grid = token('--chart-grid');
        const surface = token('--surface-container-low');
        const font = { family: token('--font') || 'sans-serif', size: 12 };

        if (horizontal) {
            canvas.parentElement.style.height = `${payload.labels.length * 38 + 40}px`;
        }

        // Axis ticks stay terse: the unit is in the card's subtitle, only "%" is repeated.
        const axisTick = (v) =>
            v.toLocaleString('en-GB', { maximumFractionDigits: 2 }).replace('-', '\u2212') + (payload.suffix === '%' ? '%' : '');
        const labels = horizontal ? payload.labels.map((label) => wrap(label, 20)) : payload.labels;

        const datasets = payload.datasets.map((dataset, i) => ({
            label: dataset.label,
            data: dataset.data,
            borderColor: colors[i % colors.length],
            backgroundColor: colors[i % colors.length],
            borderWidth: isLine ? 2 : 0,
            pointRadius: 0,
            pointHoverRadius: 5,
            pointHoverBorderWidth: 2,
            pointHoverBorderColor: surface,
            spanGaps: true,
            borderRadius: 4,
            maxBarThickness: horizontal ? 18 : 28,
        }));

        const valueAxis = {
            beginAtZero: !isLine,
            border: { display: false },
            grid: { color: grid },
            ticks: { color: ink, font, maxTicksLimit: 6, callback: axisTick },
        };
        const categoryAxis = {
            border: { color: grid },
            grid: { display: false },
            ticks: { color: horizontal ? token('--on-surface') : ink, font, maxRotation: 0, autoSkip: !horizontal, maxTicksLimit: horizontal ? undefined : 7 },
        };

        // Leave room on the value side for the direct labels of horizontal bars.
        const values = payload.datasets[0].data.filter((v) => v !== null);
        const longest = horizontal ? Math.max(...values.map((v) => format(v, payload).length)) : 0;

        return new Chart(canvas, {
            type: isLine ? 'line' : 'bar',
            data: { labels, datasets },
            plugins: [crosshair, barLabels],
            options: {
                indexAxis: horizontal ? 'y' : 'x',
                responsive: true,
                maintainAspectRatio: false,
                animation: reducedMotion ? false : { duration: 250 },
                interaction: { mode: 'index', intersect: false, axis: horizontal ? 'y' : 'x' },
                layout: { padding: { right: horizontal ? longest * 7 + 10 : 4 } },
                scales: horizontal ? { x: valueAxis, y: categoryAxis } : { x: categoryAxis, y: valueAxis },
                plugins: {
                    barLabels: { payload },
                    legend: {
                        display: datasets.length > 1,
                        position: 'top',
                        align: 'start',
                        labels: { color: token('--on-surface'), font, usePointStyle: true, pointStyle: 'line', boxWidth: 24 },
                    },
                    tooltip: {
                        backgroundColor: token('--inverse-surface'),
                        titleColor: token('--inverse-on-surface'),
                        bodyColor: token('--inverse-on-surface'),
                        titleFont: font,
                        bodyFont: font,
                        padding: 10,
                        displayColors: datasets.length > 1,
                        callbacks: {
                            title: (items) => payload.labels[items[0].dataIndex],
                            label: (item) => {
                                const text = format(item.raw, payload);
                                return datasets.length > 1 ? ` ${item.dataset.label}: ${text}` : text;
                            },
                        },
                    },
                },
            },
        });
    }

    function drawAll(root) {
        // Charts whose canvas was swapped out by htmx are released first.
        Object.values(Chart.instances).forEach((chart) => {
            if (!chart.canvas || !chart.canvas.isConnected) {
                chart.destroy();
            }
        });
        root.querySelectorAll('canvas[data-chart]').forEach((canvas) => {
            Chart.getChart(canvas)?.destroy();
            draw(canvas);
        });
    }

    // htmx:load fires for the initial page and for every swapped-in fragment.
    document.addEventListener('htmx:load', (event) => drawAll(event.detail.elt));

    document.getElementById('theme-toggle')?.addEventListener('click', () => {
        const dark = !document.body.classList.contains('dark');
        document.body.classList.toggle('dark', dark);
        document.body.classList.toggle('light', !dark);
        try {
            localStorage.setItem('theme', dark ? 'dark' : 'light');
        } catch {
            // Storage can be blocked; the choice then lasts for this page only.
        }
        drawAll(document.body);
    });
})();
