export function initNexusBalanceReport(reportJson) {
    const reportData = JSON.parse(reportJson);
    const metricLabels = {
        firstContactActivityRate: 'First-contact activity',
        attackerWinRate: 'Attacker win rate',
        attackerDamagePerTrial: 'Attacker damage per trial',
        defenderDamagePerTrial: 'Defender damage per trial',
        netDamageSwing: 'Net damage swing',
        attackerControlRate: 'Attacker control rate',
        contestedRate: 'Contested rate',
        attackerExpectedSurvivorCost: 'Attacker expected survivor cost',
        survivorCostDelta: 'Survivor cost delta'
    };

    const scenarioSelect = document.getElementById('scenario-select');
    const metricSelect = document.getElementById('metric-select');
    const attackerSelect = document.getElementById('attacker-select');
    const defenderSelect = document.getElementById('defender-select');

    let selectedScenarioId = reportData.scenarios[0].id;
    let selectedMetric = 'attackerWinRate';
    let selectedAttackerId = reportData.profiles[0].id;
    let selectedDefenderId = reportData.profiles[0].id;

    const profileMap = new Map(reportData.profiles.map(profile => [profile.id, profile]));
    const scenarioMap = new Map(reportData.scenarios.map(scenario => [scenario.id, scenario]));
    const phaseSummaryMap = new Map();

    function getSelectedScenario() {
        return scenarioMap.get(selectedScenarioId);
    }

    function getSelectedScenarioProfiles() {
        const scenario = getSelectedScenario();
        return (scenario?.profileIds ?? [])
            .map(profileId => profileMap.get(profileId))
            .filter(profile => profile);
    }

    function syncSelectedProfilesToScenario() {
        const scenarioProfiles = getSelectedScenarioProfiles();

        if (scenarioProfiles.length === 0)
            return;

        if (!scenarioProfiles.some(profile => profile.id === selectedAttackerId))
            selectedAttackerId = scenarioProfiles[0].id;

        if (!scenarioProfiles.some(profile => profile.id === selectedDefenderId))
            selectedDefenderId = scenarioProfiles[0].id;
    }

    for (const summary of reportData.phaseSummaries) {
        const key = `${summary.scenarioId}|${summary.attackerProfileId}|${summary.defenderProfileId}`;
        const current = phaseSummaryMap.get(key) ?? {
            attackerDamagePerTrial: 0,
            defenderDamagePerTrial: 0,
            attackerKillsPerTrial: 0,
            defenderKillsPerTrial: 0
        };

        if (summary.side === 'attacker') {
            current.attackerDamagePerTrial += Number(summary.hitsPerTrial ?? 0);
            current.attackerKillsPerTrial += Number(summary.killsPerTrial ?? 0);
        } else {
            current.defenderDamagePerTrial += Number(summary.hitsPerTrial ?? 0);
            current.defenderKillsPerTrial += Number(summary.killsPerTrial ?? 0);
        }

        phaseSummaryMap.set(key, current);
    }

    function getScenarioMatchups() {
        return reportData.matchups
            .filter(matchup => matchup.scenarioId === selectedScenarioId)
            .map(matchup => {
                const phaseSummary = phaseSummaryMap.get(
                    `${matchup.scenarioId}|${matchup.attackerProfileId}|${matchup.defenderProfileId}`
                ) ?? {
                    attackerDamagePerTrial: 0,
                    defenderDamagePerTrial: 0,
                    attackerKillsPerTrial: 0,
                    defenderKillsPerTrial: 0
                };

                return {
                    ...matchup,
                    attackerLabel: profileMap.get(matchup.attackerProfileId).label,
                    defenderLabel: profileMap.get(matchup.defenderProfileId).label,
                    survivorCostDelta: matchup.attackerExpectedSurvivorCost - matchup.defenderExpectedSurvivorCost,
                    attackerDamagePerTrial: phaseSummary.attackerDamagePerTrial,
                    defenderDamagePerTrial: phaseSummary.defenderDamagePerTrial,
                    netDamageSwing: phaseSummary.attackerDamagePerTrial - phaseSummary.defenderDamagePerTrial,
                    attackerKillsPerTrial: phaseSummary.attackerKillsPerTrial,
                    defenderKillsPerTrial: phaseSummary.defenderKillsPerTrial
                };
            });
    }

    function getSelectedMatchup() {
        return getScenarioMatchups().find(matchup =>
            matchup.attackerProfileId === selectedAttackerId && matchup.defenderProfileId === selectedDefenderId
        );
    }

    function getSelectedPhaseRows() {
        return reportData.phaseSummaries.filter(summary =>
            summary.scenarioId === selectedScenarioId &&
            summary.attackerProfileId === selectedAttackerId &&
            summary.defenderProfileId === selectedDefenderId
        );
    }

    function getSelectedSurvivors() {
        return reportData.survivorSummaries.filter(summary =>
            summary.scenarioId === selectedScenarioId &&
            summary.attackerProfileId === selectedAttackerId &&
            summary.defenderProfileId === selectedDefenderId
        );
    }

    function populateControls() {
        const scenarioProfiles = getSelectedScenarioProfiles();
        scenarioSelect.innerHTML = reportData.scenarios
            .map(scenario => `<option value="${scenario.id}">${scenario.label}</option>`)
            .join('');
        metricSelect.innerHTML = Object.entries(metricLabels)
            .map(([value, label]) => `<option value="${value}">${label}</option>`)
            .join('');
        const options = scenarioProfiles
            .map(profile => `<option value="${profile.id}">${profile.label}</option>`)
            .join('');
        attackerSelect.innerHTML = options;
        defenderSelect.innerHTML = options;
        scenarioSelect.value = selectedScenarioId;
        metricSelect.value = selectedMetric;
        attackerSelect.value = selectedAttackerId;
        defenderSelect.value = selectedDefenderId;
    }

    function renderHeatmap() {
        const profiles = getSelectedScenarioProfiles();
        const values = getScenarioMatchups();
        const metricValues = values.map(value => Number(value[selectedMetric] ?? 0));
        const minValue = Math.min(...metricValues);
        const maxValue = Math.max(...metricValues);
        const columns = profiles.length + 1;
        const rows = [];

        rows.push('<div class="ui-balance-heatmap-corner">Attacker / Defender</div>');
        for (const profile of profiles)
            rows.push(`<div class="ui-balance-heatmap-col-header">${escapeHtml(profile.label)}</div>`);

        for (const attacker of profiles) {
            rows.push(`<div class="ui-balance-heatmap-row-header">${escapeHtml(attacker.label)}</div>`);

            for (const defender of profiles) {
                const matchup = values.find(candidate =>
                    candidate.attackerProfileId === attacker.id && candidate.defenderProfileId === defender.id
                );

                if (!matchup) {
                    rows.push('<div class="ui-balance-heatmap-cell is-empty">No data</div>');
                    continue;
                }

                const metricValue = Number(matchup[selectedMetric] ?? 0);
                const normalized = maxValue === minValue ? 0.5 : (metricValue - minValue) / (maxValue - minValue);
                const background = `background: ${colorForScale(normalized)};`;
                const selectedClass =
                    matchup.attackerProfileId === selectedAttackerId && matchup.defenderProfileId === selectedDefenderId
                        ? 'is-selected'
                        : '';
                const formattedMetric = formatMetric(selectedMetric, metricValue);
                const subvalue = `Control ${formatPercent(matchup.attackerControlRate)} | ${phaseLabel(matchup.dominantKillPhase)}`;

                rows.push(`
                    <button
                      type="button"
                      class="ui-balance-heatmap-cell ${selectedClass}"
                      data-attacker-id="${matchup.attackerProfileId}"
                      data-defender-id="${matchup.defenderProfileId}"
                      title="${escapeHtml(matchup.attackerLabel)} vs ${escapeHtml(matchup.defenderLabel)}"
                      style="${background}">
                      <span class="ui-balance-heatmap-value">${formattedMetric}</span>
                      <span class="ui-balance-heatmap-subvalue">${escapeHtml(subvalue)}</span>
                    </button>
                `);
            }
        }

        const heatmap = document.getElementById('heatmap');
        heatmap.className = 'ui-balance-heatmap-grid';
        heatmap.style.gridTemplateColumns = `180px repeat(${columns - 1}, 132px)`;
        heatmap.innerHTML = rows.join('');

        heatmap.querySelectorAll('.ui-balance-heatmap-cell[data-attacker-id]').forEach(cell => {
            cell.addEventListener('click', () => {
                selectedAttackerId = cell.dataset.attackerId;
                selectedDefenderId = cell.dataset.defenderId;
                attackerSelect.value = selectedAttackerId;
                defenderSelect.value = selectedDefenderId;
                rerender();
            });
        });
    }

    function renderMatchupDetails() {
        const matchup = getSelectedMatchup();
        const attacker = profileMap.get(selectedAttackerId);
        const defender = profileMap.get(selectedDefenderId);
        const scenario = scenarioMap.get(selectedScenarioId);

        document.getElementById('matchup-meta').innerHTML = `
            <div><strong>${attacker.label}</strong> vs <strong>${defender.label}</strong> in <strong>${scenario.label}</strong></div>
            <div class="ui-balance-pill-row">
              ${attacker.tags.map(tag => `<span class="ui-balance-pill">attacker ${tag}</span>`).join('')}
              ${defender.tags.map(tag => `<span class="ui-balance-pill">defender ${tag}</span>`).join('')}
            </div>
            <div>Attacker cost ${attacker.totalCost} | Defender cost ${defender.totalCost} | Simulated rounds ${scenario.maxRounds} | Dominant kill phase ${phaseLabel(matchup.dominantKillPhase)}</div>
        `;

        const statRows = [
            ['First-contact activity', formatPercent(matchup.firstContactActivityRate)],
            ['Attacker win rate', formatPercent(matchup.attackerWinRate)],
            ['Defender win rate', formatPercent(matchup.defenderWinRate)],
            ['Attacker damage / trial', matchup.attackerDamagePerTrial.toFixed(2)],
            ['Defender damage / trial', matchup.defenderDamagePerTrial.toFixed(2)],
            ['Net damage swing', formatSigned(matchup.netDamageSwing)],
            ['Contested rate', formatPercent(matchup.contestedRate)],
            ['Attacker control rate', formatPercent(matchup.attackerControlRate)],
            ['Defender control rate', formatPercent(matchup.defenderControlRate)],
            ['Attacker survivor cost', matchup.attackerExpectedSurvivorCost.toFixed(2)],
            ['Defender survivor cost', matchup.defenderExpectedSurvivorCost.toFixed(2)],
            ['Mutual destruction', formatPercent(matchup.mutualDestructionRate)]
        ];
        document.getElementById('matchup-stats').innerHTML = statRows
            .map(([label, value]) => `<div class="ui-balance-stat"><span>${label}</span><strong>${value}</strong></div>`)
            .join('');

        renderPhaseChart();
        renderSurvivorChart();
        renderCounterTable();
    }

    function renderPhaseChart() {
        const rows = getSelectedPhaseRows();
        const maxKills = Math.max(0.01, ...rows.map(row => row.killsPerTrial));
        const phaseChart = document.getElementById('phase-chart');

        if (rows.length === 0) {
            phaseChart.innerHTML = '<div class="ui-empty-state">No phase data for the selected matchup.</div>';
            return;
        }

        phaseChart.innerHTML = `<div class="ui-balance-bar-list">${rows
            .map(row => {
                const width = Math.max(4, (row.killsPerTrial / maxKills) * 100);
                return `
                    <div class="ui-balance-bar-row">
                      <div class="ui-type-muted">${phaseLabel(row.phase)} ${capitalize(row.side)}</div>
                      <div class="ui-balance-bar-track">
                        <div class="ui-balance-bar-fill-${row.side}" style="width:${width}%"></div>
                      </div>
                      <div class="ui-type-muted">${row.killsPerTrial.toFixed(2)} kills</div>
                    </div>
                `;
            })
            .join('')}</div>`;
    }

    function renderSurvivorChart() {
        const rows = getSelectedSurvivors();
        const maxCount = Math.max(0.01, ...rows.map(row => row.expectedCount));
        const survivorChart = document.getElementById('survivor-chart');

        if (rows.length === 0) {
            survivorChart.innerHTML = '<div class="ui-empty-state">No survivors recorded for the selected matchup.</div>';
            return;
        }

        survivorChart.innerHTML = `<div class="ui-balance-bar-list">${rows
            .map(row => {
                const width = Math.max(4, (row.expectedCount / maxCount) * 100);
                return `
                    <div class="ui-balance-bar-row">
                      <div class="ui-type-muted">${capitalize(row.side)} ${escapeHtml(row.unitType)}</div>
                      <div class="ui-balance-bar-track">
                        <div class="ui-balance-bar-fill-${row.side}" style="width:${width}%"></div>
                      </div>
                      <div class="ui-type-muted">${row.expectedCount.toFixed(2)}</div>
                    </div>
                `;
            })
            .join('')}</div>`;
    }

    function renderCounterTable() {
        const rows = getScenarioMatchups()
            .filter(matchup => matchup.attackerProfileId === selectedAttackerId)
            .sort((left, right) => (right[selectedMetric] ?? 0) - (left[selectedMetric] ?? 0))
            .map(matchup => `
                <tr class="border-b border-white/5 last:border-0">
                  <td class="py-2 pr-4">${profileMap.get(matchup.defenderProfileId).label}</td>
                  <td class="py-2 pr-4">${formatPercent(matchup.attackerWinRate)}</td>
                  <td class="py-2 pr-4">${formatPercent(matchup.attackerControlRate)}</td>
                  <td class="py-2 pr-4">${(matchup.attackerExpectedSurvivorCost - matchup.defenderExpectedSurvivorCost).toFixed(2)}</td>
                  <td class="py-2">${phaseLabel(matchup.dominantKillPhase)}</td>
                </tr>
            `)
            .join('');
        document.getElementById('counter-table-body').innerHTML = rows;
    }

    function phaseLabel(phase) {
        if (typeof phase === 'string')
            return phase;

        switch (phase) {
            case 1: return 'Screen';
            case 2: return 'Engage';
            case 3: return 'Bombard';
            case 4: return 'Assault';
            default: return `Phase ${phase}`;
        }
    }

    function formatPercent(value) {
        return new Intl.NumberFormat('en-US', { style: 'percent', maximumFractionDigits: 1 }).format(value);
    }

    function formatMetric(metric, value) {
        if (metric.endsWith('Rate'))
            return formatPercent(value);

        if (metric === 'netDamageSwing' || metric === 'survivorCostDelta')
            return formatSigned(value);

        return Number(value).toFixed(2);
    }

    function formatSigned(value) {
        return `${value >= 0 ? '+' : ''}${Number(value).toFixed(2)}`;
    }

    function colorForScale(normalized) {
        const clamped = Math.max(0, Math.min(1, normalized));
        const start = { r: 15, g: 23, b: 42 };   // slate-900
        const end = { r: 194, g: 65, b: 12 };     // orange-700
        const r = Math.round(start.r + (end.r - start.r) * clamped);
        const g = Math.round(start.g + (end.g - start.g) * clamped);
        const b = Math.round(start.b + (end.b - start.b) * clamped);
        return `rgb(${r}, ${g}, ${b})`;
    }

    function capitalize(value) {
        return value.charAt(0).toUpperCase() + value.slice(1);
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll('&', '&amp;')
            .replaceAll('<', '&lt;')
            .replaceAll('>', '&gt;')
            .replaceAll('"', '&quot;')
            .replaceAll("'", '&#39;');
    }

    function rerender() {
        syncSelectedProfilesToScenario();
        populateControls();
        renderHeatmap();
        renderMatchupDetails();
    }

    rerender();

    scenarioSelect.addEventListener('change', event => {
        selectedScenarioId = event.target.value;
        rerender();
    });
    metricSelect.addEventListener('change', event => {
        selectedMetric = event.target.value;
        rerender();
    });
    attackerSelect.addEventListener('change', event => {
        selectedAttackerId = event.target.value;
        rerender();
    });
    defenderSelect.addEventListener('change', event => {
        selectedDefenderId = event.target.value;
        rerender();
    });
}
