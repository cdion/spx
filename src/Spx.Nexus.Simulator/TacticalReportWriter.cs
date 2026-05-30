using System.Text.Json;
using System.Text.Json.Serialization;

namespace Spx.Nexus.Simulator;

public sealed class TacticalReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<SimulatorRunResult> WriteAsync(
        string outputDirectory,
        TacticalReportData reportData,
        CancellationToken cancellationToken
    )
    {
        Directory.CreateDirectory(outputDirectory);

        var summaryPath = Path.Combine(outputDirectory, "summary.json");
        var reportPath = Path.Combine(outputDirectory, "report.html");
        var reportJson = JsonSerializer.Serialize(reportData, JsonOptions);

        await File.WriteAllTextAsync(summaryPath, reportJson, cancellationToken);
        await File.WriteAllTextAsync(reportPath, BuildHtml(reportJson), cancellationToken);

        return new SimulatorRunResult(reportPath, summaryPath);
    }

    private static string BuildHtml(string reportJson)
    {
        const string template = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Nexus Tactical Balance Report</title>
  <style>
    :root {
      color-scheme: light;
      --bg: #f3f0e8;
      --panel: #fffaf0;
      --ink: #1f2328;
      --muted: #5c6057;
      --line: #d7cfbf;
      --accent: #b24c2c;
      --accent-soft: #f2d7cd;
    }
    body {
      margin: 0;
      font-family: "Iowan Old Style", "Palatino Linotype", "Book Antiqua", serif;
      background: radial-gradient(circle at top, #fffaf0 0%, var(--bg) 55%, #e7dfcf 100%);
      color: var(--ink);
    }
    main {
      max-width: 1400px;
      margin: 0 auto;
      padding: 32px 24px 48px;
    }
    h1, h2 {
      margin: 0;
      font-weight: 600;
      letter-spacing: 0.02em;
    }
    p {
      color: var(--muted);
    }
    .hero {
      display: grid;
      gap: 12px;
      margin-bottom: 24px;
    }
    .controls,
    .panel,
    .stats {
      background: color-mix(in srgb, var(--panel) 92%, white 8%);
      border: 1px solid var(--line);
      border-radius: 18px;
      box-shadow: 0 12px 28px rgba(66, 48, 19, 0.08);
    }
    .controls {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
      padding: 18px;
      margin-bottom: 24px;
    }
    .control {
      display: grid;
      gap: 6px;
      min-width: 180px;
    }
    label {
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: var(--muted);
    }
    select {
      border: 1px solid var(--line);
      border-radius: 10px;
      padding: 10px 12px;
      background: white;
      color: var(--ink);
      font: inherit;
    }
    .layout {
      display: grid;
      gap: 24px;
    }
    .panel {
      padding: 18px;
    }
    .charts {
      display: grid;
      grid-template-columns: minmax(0, 2fr) minmax(320px, 1fr);
      gap: 24px;
    }
    .stack {
      display: grid;
      gap: 24px;
    }
    .stats {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
      gap: 12px;
      padding: 18px;
    }
    .stat {
      padding: 12px;
      border-radius: 14px;
      background: color-mix(in srgb, white 78%, var(--accent-soft) 22%);
    }
    .stat strong {
      display: block;
      font-size: 1.6rem;
      margin-bottom: 4px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th,
    td {
      padding: 10px 12px;
      border-bottom: 1px solid var(--line);
      text-align: left;
      font-size: 0.95rem;
    }
    th {
      font-size: 0.78rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: var(--muted);
    }
    .matchup-meta {
      display: grid;
      gap: 10px;
      margin-bottom: 16px;
    }
    .pill-row {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
    }
    .pill {
      border-radius: 999px;
      background: var(--accent-soft);
      color: var(--accent);
      padding: 6px 10px;
      font-size: 0.82rem;
      letter-spacing: 0.04em;
      text-transform: uppercase;
    }
    .heatmap-shell {
      overflow-x: auto;
    }
    .heatmap-grid {
      display: grid;
      gap: 6px;
      align-items: stretch;
      min-width: max-content;
    }
    .heatmap-corner,
    .heatmap-column,
    .heatmap-row,
    .heatmap-cell {
      border-radius: 10px;
      min-height: 56px;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 8px;
      text-align: center;
      box-sizing: border-box;
    }
    .heatmap-corner,
    .heatmap-column,
    .heatmap-row {
      background: rgba(255, 255, 255, 0.82);
      border: 1px solid var(--line);
      font-size: 0.8rem;
      text-transform: uppercase;
      letter-spacing: 0.08em;
      color: var(--muted);
    }
    .heatmap-column {
      min-width: 132px;
      font-size: 0.74rem;
    }
    .heatmap-row {
      justify-content: flex-start;
      min-width: 180px;
      padding-left: 12px;
      font-size: 0.74rem;
    }
    .heatmap-cell {
      min-width: 132px;
      border: 1px solid rgba(135, 92, 32, 0.16);
      color: #20160d;
      cursor: pointer;
      flex-direction: column;
      gap: 4px;
    }
    .heatmap-cell:hover {
      outline: 2px solid color-mix(in srgb, var(--accent) 45%, white 55%);
      outline-offset: 1px;
    }
    .heatmap-cell.is-selected {
      outline: 3px solid var(--accent);
      outline-offset: 1px;
    }
    .heatmap-value {
      font-size: 1rem;
      font-weight: 700;
    }
    .heatmap-subvalue {
      font-size: 0.72rem;
      color: rgba(32, 22, 13, 0.76);
    }
    .bar-list {
      display: grid;
      gap: 10px;
    }
    .bar-row {
      display: grid;
      grid-template-columns: 88px minmax(0, 1fr) 88px;
      gap: 12px;
      align-items: center;
    }
    .bar-label,
    .bar-value {
      font-size: 0.9rem;
      color: var(--muted);
    }
    .bar-track {
      position: relative;
      height: 18px;
      border-radius: 999px;
      background: rgba(92, 96, 87, 0.12);
      overflow: hidden;
    }
    .bar-fill {
      position: absolute;
      inset: 0 auto 0 0;
      border-radius: 999px;
    }
    .bar-fill.attacker {
      background: linear-gradient(90deg, #c9653f 0%, #b24c2c 100%);
    }
    .bar-fill.defender {
      background: linear-gradient(90deg, #66859f 0%, #405d72 100%);
    }
    .empty-state {
      padding: 18px;
      border: 1px dashed var(--line);
      border-radius: 12px;
      color: var(--muted);
      background: rgba(255, 255, 255, 0.55);
    }
    @media (max-width: 1000px) {
      .charts {
        grid-template-columns: 1fr;
      }
    }
  </style>
</head>
<body>
  <main>
    <section class="hero">
      <h1>Nexus Tactical Balance Report</h1>
      <p>Bootstrap report for one-system balance using the live Nexus combat resolver. Scenario slices separate space duels from invasion/control profiles and compare 1, 2, and 3 round horizons.</p>
    </section>

    <section class="controls">
      <div class="control">
        <label for="scenario-select">Scenario</label>
        <select id="scenario-select"></select>
      </div>
      <div class="control">
        <label for="metric-select">Heatmap Metric</label>
        <select id="metric-select"></select>
      </div>
      <div class="control">
        <label for="attacker-select">Attacker Profile</label>
        <select id="attacker-select"></select>
      </div>
      <div class="control">
        <label for="defender-select">Defender Profile</label>
        <select id="defender-select"></select>
      </div>
    </section>

    <section class="layout">
      <section class="panel">
        <h2>Matchup Heatmap</h2>
        <p>Compare profile-vs-profile tactical performance for the selected scenario and metric.</p>
        <div class="heatmap-shell">
          <div id="heatmap"></div>
        </div>
      </section>

      <section class="stats" id="matchup-stats"></section>

      <section class="charts">
        <section class="panel stack">
          <div>
            <h2>Selected Matchup</h2>
            <div class="matchup-meta" id="matchup-meta"></div>
          </div>
          <div>
            <h2>Phase Breakdown</h2>
            <div id="phase-chart"></div>
          </div>
          <div>
            <h2>Expected Survivors</h2>
            <div id="survivor-chart"></div>
          </div>
        </section>

        <section class="panel">
          <h2>Counter Table</h2>
          <p>All defender outcomes against the selected attacker, sorted by the active heatmap metric.</p>
          <table>
            <thead>
              <tr>
                <th>Defender</th>
                <th>Attacker Win</th>
                <th>Attacker Control</th>
                <th>Survivor Cost Delta</th>
                <th>Dominant Phase</th>
              </tr>
            </thead>
            <tbody id="counter-table-body"></tbody>
          </table>
        </section>
      </section>
    </section>
  </main>

  <script>
    const reportData = __REPORT_DATA__;
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

      rows.push('<div class="heatmap-corner">Attacker / Defender</div>');
      for (const profile of profiles)
        rows.push(`<div class="heatmap-column">${escapeHtml(profile.label)}</div>`);

      for (const attacker of profiles) {
        rows.push(`<div class="heatmap-row">${escapeHtml(attacker.label)}</div>`);

        for (const defender of profiles) {
          const matchup = values.find(candidate =>
            candidate.attackerProfileId === attacker.id && candidate.defenderProfileId === defender.id
          );

          if (!matchup) {
            rows.push('<div class="heatmap-cell empty-state">No data</div>');
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
              class="heatmap-cell ${selectedClass}"
              data-attacker-id="${matchup.attackerProfileId}"
              data-defender-id="${matchup.defenderProfileId}"
              title="${escapeHtml(matchup.attackerLabel)} vs ${escapeHtml(matchup.defenderLabel)}"
              style="${background}">
              <span class="heatmap-value">${formattedMetric}</span>
              <span class="heatmap-subvalue">${escapeHtml(subvalue)}</span>
            </button>
          `);
        }
      }

      const heatmap = document.getElementById('heatmap');
      heatmap.className = 'heatmap-grid';
      heatmap.style.gridTemplateColumns = `180px repeat(${columns - 1}, 132px)`;
      heatmap.innerHTML = rows.join('');

      heatmap.querySelectorAll('.heatmap-cell[data-attacker-id]').forEach(cell => {
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
        <div class="pill-row">
          ${attacker.tags.map(tag => `<span class="pill">attacker ${tag}</span>`).join('')}
          ${defender.tags.map(tag => `<span class="pill">defender ${tag}</span>`).join('')}
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
        .map(([label, value]) => `<div class="stat"><span>${label}</span><strong>${value}</strong></div>`)
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
        phaseChart.innerHTML = '<div class="empty-state">No phase data for the selected matchup.</div>';
        return;
      }

      phaseChart.innerHTML = `<div class="bar-list">${rows
        .map(row => {
          const width = Math.max(4, (row.killsPerTrial / maxKills) * 100);
          return `
            <div class="bar-row">
              <div class="bar-label">${phaseLabel(row.phase)} ${capitalize(row.side)}</div>
              <div class="bar-track">
                <div class="bar-fill ${row.side}" style="width:${width}%"></div>
              </div>
              <div class="bar-value">${row.killsPerTrial.toFixed(2)} kills</div>
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
        survivorChart.innerHTML = '<div class="empty-state">No survivors recorded for the selected matchup.</div>';
        return;
      }

      survivorChart.innerHTML = `<div class="bar-list">${rows
        .map(row => {
          const width = Math.max(4, (row.expectedCount / maxCount) * 100);
          return `
            <div class="bar-row">
              <div class="bar-label">${capitalize(row.side)} ${escapeHtml(row.unitType)}</div>
              <div class="bar-track">
                <div class="bar-fill ${row.side}" style="width:${width}%"></div>
              </div>
              <div class="bar-value">${row.expectedCount.toFixed(2)}</div>
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
          <tr>
            <td>${profileMap.get(matchup.defenderProfileId).label}</td>
            <td>${formatPercent(matchup.attackerWinRate)}</td>
            <td>${formatPercent(matchup.attackerControlRate)}</td>
            <td>${(matchup.attackerExpectedSurvivorCost - matchup.defenderExpectedSurvivorCost).toFixed(2)}</td>
            <td>${phaseLabel(matchup.dominantKillPhase)}</td>
          </tr>
        `)
        .join('');
      document.getElementById('counter-table-body').innerHTML = rows;
    }

    function phaseLabel(phase) {
      if (typeof phase === 'string')
        return phase;

      switch (phase) {
        case 1:
          return 'Screen';
        case 2:
          return 'Engage';
        case 3:
          return 'Bombard';
        case 4:
          return 'Assault';
        default:
          return `Phase ${phase}`;
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
      const start = { r: 248, g: 236, b: 218 };
      const end = { r: 178, g: 76, b: 44 };
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
  </script>
</body>
</html>
""";

        return template.Replace("__REPORT_DATA__", reportJson);
    }
}
