// Read-only audit of serialized Room stat references, without opening a match.
import { readFileSync } from "node:fs";

const scene = readFileSync(new URL("../Assets/Scenes/Room.unity", import.meta.url), "utf8");
const parse = yaml => new Map([...yaml.matchAll(/^--- !u!\d+ &(\d+).*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)]
  .map(match => [match[1], match[2]]));
const documents = parse(scene);
const statPrefabGuid = "ce2b0ab6120e645e2910e04655208564";
const statPrefab = parse(readFileSync(new URL("../Assets/Resources/UI/HUD/StatRow.prefab", import.meta.url), "utf8"));
const labels = [...readFileSync(new URL("../Assets/Resources/UI/MatchStatsTemplate.asset", import.meta.url), "utf8")
  .matchAll(/^    centerLabel: (.*)$/gm)].map(match => match[1]);
const ref = (doc, field) => doc?.match(new RegExp(`^  ${field}: \\{fileID: (\\d+)`, "m"))?.[1];
const name = id => documents.get(id)?.match(/^  m_Name: (.*)$/m)?.[1];
const gameObject = id => ref(documents.get(id), "m_GameObject");
const transforms = new Map([...documents].filter(([, doc]) => /^(RectTransform|Transform):/m.test(doc))
  .map(([id, doc]) => [gameObject(id), { id, parent: ref(doc, "m_Father") }]));
const parent = id => gameObject(transforms.get(id)?.parent);
const errors = [];
const reports = [];
const statCell = id => {
  const doc = documents.get(id);
  const source = doc?.match(/m_CorrespondingSourceObject: \{fileID: (\d+), guid: (\w+)/);
  if (!source || source[2] !== statPrefabGuid) return null;
  const instance = ref(doc, "m_PrefabInstance");
  const instanceDoc = documents.get(instance);
  const component = statPrefab.get(source[1]);
  const go = statPrefab.get(ref(component, "m_GameObject"));
  const cellName = go?.match(/^  m_Name: (.*)$/m)?.[1];
  const rowName = instanceDoc?.match(/propertyPath: m_Name\n      value: (.*)/)?.[1];
  const text = instanceDoc?.match(new RegExp(`target: \\{fileID: ${source[1]},[^\\n]*\\n      propertyPath: m_text\\n      value: (.*)`))?.[1];
  return { instance, cellName, rowName, text };
};
for (const [id, doc] of documents) {
  if (!/^  homeStatsTexts:/m.test(doc)) continue;
  const columns = {};
  for (const field of ["homeStatsTexts", "statTitleTexts", "awayStatsTexts"]) {
    const block = doc.match(new RegExp(`^  ${field}:\\n((?:  - \\{fileID: \\d+\\}\\n)+)`, "m"))?.[1] ?? "";
    columns[field] = [...block.matchAll(/fileID: (\d+)/g)].map(match => match[1]);
    if (columns[field].length !== 21 || new Set(columns[field]).size !== 21)
      errors.push(`${id}/${field}: expected 21 distinct references`);
    if (columns[field].some(target => !documents.has(target) || target === "0"))
      errors.push(`${id}/${field}: missing component reference`);
  }
  // The live MatchStatsUI uses row objects. EndGamePanel's legacy columns
  // have a separate hierarchy, so validate its array integrity independently.
  const isMatchStats = /guid: c3a0663be634a436fb9e8f38e14d3922/.test(doc);
  if (isMatchStats) {
    for (let row = 0; row < 21; row++) {
      const ids = Object.values(columns).map(column => column[row]);
      const prefabCells = ids.map(statCell);
      const cells = ids.map(gameObject);
      const parents = cells.map(parent);
      const validPrefab = prefabCells.every(cell => cell?.rowName === `StatRow (${row + 1})`)
        && new Set(prefabCells.map(cell => cell?.instance)).size === 1;
      const validScene = new Set(parents).size === 1 && name(parents[0]) === `StatRow (${row + 1})`;
      if (!validPrefab && !validScene)
        errors.push(`MatchStats row ${row + 1}: cells reference different/wrong rows`);
      const names = validPrefab ? prefabCells.map(cell => cell.cellName) : cells.map(name);
      if (names.join(",") !== "HomeStat,StatTitle,AwayStat")
        errors.push(`MatchStats row ${row + 1}: wrong cell names/order`);
      if (validPrefab && prefabCells[1].text !== labels[row])
        errors.push(`MatchStats row ${row + 1}: label does not match the stats template`);
    }
    for (const field of ["homeTeamNameText", "awayTeamNameText", "scoreText", "homeScorersText", "awayScorersText"])
      if (!documents.has(ref(doc, field))) errors.push(`MatchStats ${field}: missing reference`);
  }
  reports.push({ component: isMatchStats ? "MatchStatsUI" : "EndGamePanel", id,
    rows: columns.homeStatsTexts.length, distinctCells: new Set(Object.values(columns).flat()).size });
  if (new Set(Object.values(columns).flat()).size !== 63) errors.push(`${id}: expected 63 distinct cells across columns`);
}
if (reports.length !== 2) errors.push(`Expected two stats components, found ${reports.length}`);
if (labels.length !== 21) errors.push(`Expected 21 template labels, found ${labels.length}`);
console.log(JSON.stringify({ passed: errors.length === 0, reports, errors }, null, 2));
process.exitCode = errors.length ? 1 : 0;
