import fs from 'node:fs';
import path from 'node:path';

// Emits an apply_patch document; does not modify the source save or write files.
const savePath = process.argv[2];
const save = JSON.parse(fs.readFileSync(savePath, 'utf8'));
const prefix = savePath.slice(0, -5);
const eventSegments = ['00-09', '10-19'].filter(segment => fs.existsSync(`${prefix}__Live Events__H1_${segment}.jsonl`));
const events = eventSegments.flatMap(segment =>
  fs.readFileSync(`${prefix}__Live Events__H1_${segment}.jsonl`, 'utf8')
    .trim().split('\n').map(line => JSON.parse(line))).sort((a, b) => a.sequenceNumber - b.sequenceNumber);
const decisions = eventSegments.flatMap(segment =>
  JSON.parse(fs.readFileSync(`${prefix}__AI Decisions__H1_${segment}.json`, 'utf8')).decisions);
const seconds = clock => clock.split(':').reduce((a, b) => a * 60 + Number(b), 0);
const windows = ['05:15', '07:32', '08:10', '08:34', '10:00', '10:20', '10:30', '11:10', '11:30', '12:53', '13:26', '14:24', '14:59'];
const selectedDecisionIds = process.argv[3]?.startsWith('decisions:') ? process.argv[3].slice(10).split(',').map(Number) : null;
const selectedWindows = selectedDecisionIds ? ['selected-decisions'] : process.argv[3] ? windows.filter(clock => clock === process.argv[3]) : windows;
// Reported times came from the review. Match named incidents to recorded decision
// IDs where the review time differs from the in-game clock stored by the trace.
const incidentDecisions = {'05:15': [17, 18, 19, 20], '07:32': [63, 64, 65, 66],
  '08:10': [83, 84, 85, 86], '08:34': [96, 97, 98, 102, 103], '10:30': [161, 162, 163, 164, 165]};
function movementReplay(earlier, instruction) {
  if (instruction?.manager !== 'MovementPhaseManager') return null;
  const prompt = instruction.instructionText?.match(/forfeit (\d+) remaining moves of (Attacking|Defending|2f2)/);
  if (!prompt) return null;
  const phaseStart = earlier.filter(item => item.kind === 'snapshot.action_boundary'
    || item.kind === 'action.selected' && item.result?.action === 'movement').at(-1);
  if (!phaseStart) throw new Error('Cannot reconstruct movement ownership without a phase boundary');
  const phaseEvents = earlier.filter(item => item.sequenceNumber > phaseStart.sequenceNumber
    && item.currentGameState === 'MovementPhase');
  const movedTokenKeys = [...new Set(phaseEvents.filter(item => item.kind === 'token.move'
    && item.sourceHex && item.targetHex && (item.sourceHex.x !== item.targetHex.x || item.sourceHex.z !== item.targetHex.z))
    .map(item => item.actorTokenKey).filter(Boolean))];
  const lastPurposefulTouchTokenKey = earlier.filter(item => item.kind === 'snapshot.action_boundary' && item.actorTokenKey).at(-1)?.actorTokenKey ?? null;
  return {phaseStartSequence: phaseStart.sequenceNumber, section: prompt[2], remainingMoves: Number(prompt[1]), movedTokenKeys, lastPurposefulTouchTokenKey,
    note: 'Moved-token ownership reconstructed from movement events since the last action boundary/activation; not a full manager runtime save.'};
}
const fixtures = selectedWindows.map(clock => {
  const nearby = decisions.map(decision => ({decision, event: events.find(event => event.sequenceNumber === decision.precedingGameplaySequence)}))
    .filter(item => item.event && (selectedDecisionIds ? selectedDecisionIds.includes(item.decision.decisionNumber)
      : !incidentDecisions[clock] || incidentDecisions[clock].includes(item.decision.decisionNumber)))
    .sort((a, b) => Math.abs(seconds(a.event.clockDisplay) - seconds(clock)) - Math.abs(seconds(b.event.clockDisplay) - seconds(clock)))
    .slice(0, selectedDecisionIds?.length ?? incidentDecisions[clock]?.length ?? 4).sort((a, b) => a.decision.decisionNumber - b.decision.decisionNumber);
  return {reportedClock: clock, decisions: nearby.map(({decision, event}) => {
    const earlier = events.filter(item => item.sequenceNumber <= event.sequenceNumber);
    const boundary = earlier.filter(item => item.snapshot?.tokens).at(-1);
    const tokens = structuredClone(boundary.snapshot.tokens);
    let ball = structuredClone(boundary.snapshot.ballHex);
    for (const item of earlier.filter(item => item.sequenceNumber > boundary.sequenceNumber)) {
      if (item.kind === 'token.move' && item.targetHex) {
        const token = tokens.find(token => token.tokenKey === item.actorTokenKey);
        if (token) token.hex = item.targetHex;
      }
      if (item.kind === 'ball.move' && item.targetHex) ball = item.targetHex;
    }
    const instruction = earlier.filter(item => item.instruction).at(-1)?.instruction;
    return {decisionNumber: decision.decisionNumber, sequenceNumber: event.sequenceNumber, clock: event.clockDisplay,
      currentState: decision.currentState, manager: decision.manager, expectedTeam: decision.expectedTeam,
      teamInAttack: event.teamInAttack ?? decision.teamInAttack,
      attackHasPossession: event.attackHasPossession ?? decision.attackHasPossession, ball,
      homeTeamDirection: 'LeftToRight', awayTeamDirection: 'RightToLeft',
      tokens: tokens.map(token => {const [side, jersey] = token.tokenKey.split(':');
        const roster = save.rosters[side.toLowerCase()][jersey];
        return {...roster, playerName: roster.name, tokenKey: token.tokenKey, teamSide: side,
          jerseyNumber: Number(jersey), currentHex: token.hex, isPlaying: token.status.split(',').includes('playing'),
          isGoalKeeper: token.status.split(',').includes('gk'), isBooked: token.status.includes('booked')};}),
      recordedSelection: decision.selectedAction, recordedReason: decision.selectedReason,
      instruction, movementReplay: movementReplay(earlier, instruction),
      alternatives: decision.alternatives.filter((candidate, index) => candidate.selected || index < 3)
        .map(candidate => ({label: candidate.label, actionType: candidate.actionType, step: candidate.step,
          actorTokenKey: candidate.actorTokenKey, targetHex: candidate.targetHex, key: candidate.key,
          score: candidate.score, selected: candidate.selected, reasons: candidate.reasons,
          expectedGoals: candidate.expectedGoals, expectedGoalsAgainst: candidate.expectedGoalsAgainst,
          tackle: candidate.isTackleDecision ? {fromBehind: candidate.tackleFromBehind,
            success: candidate.tackleSuccessProbability, foul: candidate.tackleFoulProbability,
            sendOff: candidate.tackleSendOffProbability, retained: candidate.dribblerRetainsProbability,
            repositionHex: candidate.dribblerBestRepositionHex, repositionXg: candidate.dribblerBestRepositionExpectedGoals,
            repositionProgress: candidate.dribblerBestRepositionProgress} : null,
          goalkeeper: candidate.goalkeeperCurrentMaximumExpectedGoals >= 0 ? {
            currentXg: candidate.goalkeeperCurrentMaximumExpectedGoals, shooter: candidate.goalkeeperWorstThreatTokenKey,
            target: candidate.worstShotTarget, blockers: candidate.goalkeeperCurrentOutfieldBlockerCount} : null}))};
  })};
});
const document = {schemaVersion: 1, sourceSave: path.basename(savePath),
  note: 'Positions reconstructed from the last full snapshot plus token.move and ball.move events before each recorded decision; directions verified against initial setup. Retained alternatives are evidence, not the complete legal action set.', fixtures};
const lines = ['{', `  "schemaVersion": 1,`, `  "sourceSave": ${JSON.stringify(document.sourceSave)},`,
  `  "note": ${JSON.stringify(document.note)},`, '  "fixtures": [',
  ...fixtures.map((fixture, i) => '    ' + JSON.stringify(fixture) + (i < fixtures.length - 1 ? ',' : '')), '  ]', '}'];
const fixtureName = process.argv[4] ?? (process.argv[3] ? `ReportedMatch_${process.argv[3].replace(':', '-')}.json` : 'ReportedMatchDecisions.json');
process.stdout.write(`*** Begin Patch\n*** Add File: Assets/Editor/Fixtures/${fixtureName}\n` + lines.map(line => '+' + line).join('\n') + '\n*** End Patch\n');
