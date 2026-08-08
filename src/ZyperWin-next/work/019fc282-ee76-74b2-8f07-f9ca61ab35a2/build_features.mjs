import fs from "node:fs/promises";
import { SpreadsheetFile, Workbook } from "@oai/artifact-tool";

const xmlPath = "E:/ZyperWin-next/ZyperWin++4.1/Release/Bin/ZyperData.xml";
const outputDir = "E:/ZyperWin-next/outputs/019fc282-ee76-74b2-8f07-f9ca61ab35a2";
const outputPath = `${outputDir}/ZyperWin++当前功能统计.xlsx`;

const categoryNames = {
  explorer: "外观/资源管理器",
  xingneng: "性能优化设置",
  safe: "安全设置",
  edge: "Edge优化设置",
  system: "系统设置",
  update: "更新设置",
  yinsi: "隐私设置",
};

function decodeXml(value) {
  return value
    .replaceAll("&quot;", '"')
    .replaceAll("&apos;", "'")
    .replaceAll("&lt;", "<")
    .replaceAll("&gt;", ">")
    .replaceAll("&amp;", "&");
}

const xml = await fs.readFile(xmlPath, "utf8");
const categories = [];
const features = [];
let globalIndex = 1;

for (const match of xml.matchAll(/<Configuration\s+category="([^"]+)">([\s\S]*?)<\/Configuration>/g)) {
  const code = match[1];
  const body = match[2];
  const names = [...body.matchAll(/<Item\s+name="([^"]+)"/g)].map((item) => decodeXml(item[1]));
  const display = categoryNames[code] ?? code;
  categories.push({ code, display, count: names.length });
  for (const rawName of names) {
    const numberMatch = rawName.match(/^(\d+)、(.*)$/s);
    features.push([
      globalIndex++,
      display,
      numberMatch ? Number(numberMatch[1]) : "",
      numberMatch ? numberMatch[2] : rawName,
    ]);
  }
}

const workbook = Workbook.create();
const sheet = workbook.worksheets.add("功能统计");
sheet.showGridLines = false;

sheet.getRange("A1:D1").merge();
sheet.getRange("A1").values = [["ZyperWin++ 当前功能统计"]];
sheet.getRange("A1:D1").format = {
  fill: "#1F4E78",
  font: { bold: true, color: "#FFFFFF", size: 16 },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
sheet.getRange("A1:D1").format.rowHeight = 34;

sheet.getRange("A2:D2").merge();
sheet.getRange("A2").values = [["统计来源：当前运行版 Bin/ZyperData.xml　生成日期：2026-08-02"]];
sheet.getRange("A2:D2").format = {
  fill: "#D9EAF7",
  font: { color: "#405466", size: 10 },
  horizontalAlignment: "left",
  verticalAlignment: "center",
};
sheet.getRange("A2:D2").format.rowHeight = 24;

sheet.getRange("A4:B4").values = [["分类", "功能数量"]];
sheet.getRange(`A5:B${4 + categories.length}`).values = categories.map((item) => [item.display, item.count]);
const totalRow = 5 + categories.length;
sheet.getRange(`A${totalRow}`).values = [["合计"]];
sheet.getRange(`B${totalRow}`).formulas = [[`=SUM(B5:B${totalRow - 1})`]];

sheet.getRange(`A4:B${totalRow}`).format.borders = {
  insideHorizontal: { style: "thin", color: "#D9E2F3" },
  outside: { style: "thin", color: "#A6B8CC" },
};
sheet.getRange("A4:B4").format = {
  fill: "#5B9BD5",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};
sheet.getRange(`A${totalRow}:B${totalRow}`).format = {
  fill: "#E2F0D9",
  font: { bold: true, color: "#375623" },
};
sheet.getRange(`B5:B${totalRow}`).format.numberFormat = "0";
sheet.getRange(`B5:B${totalRow}`).format.horizontalAlignment = "center";

const listHeaderRow = totalRow + 3;
sheet.getRange(`A${listHeaderRow}:D${listHeaderRow}`).values = [["总序号", "分类", "分类序号", "功能名称"]];
sheet.getRange(`A${listHeaderRow}:D${listHeaderRow}`).format = {
  fill: "#4472C4",
  font: { bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
};

const firstDataRow = listHeaderRow + 1;
const lastDataRow = firstDataRow + features.length - 1;
sheet.getRange(`A${firstDataRow}:D${lastDataRow}`).values = features;
sheet.getRange(`A${firstDataRow}:C${lastDataRow}`).format.horizontalAlignment = "center";
sheet.getRange(`D${firstDataRow}:D${lastDataRow}`).format.horizontalAlignment = "left";
sheet.getRange(`A${listHeaderRow}:D${lastDataRow}`).format.borders = {
  insideHorizontal: { style: "thin", color: "#E7EAF0" },
  outside: { style: "thin", color: "#A6B8CC" },
};

const table = sheet.tables.add(`A${listHeaderRow}:D${lastDataRow}`, true, "FeatureListTable");
table.style = "TableStyleMedium2";
table.showFilterButton = true;
table.showBandedRows = true;

sheet.freezePanes.freezeRows(listHeaderRow);
sheet.getRange("A:A").format.columnWidth = 10;
sheet.getRange("B:B").format.columnWidth = 22;
sheet.getRange("C:C").format.columnWidth = 11;
sheet.getRange("D:D").format.columnWidth = 52;
sheet.getRange(`A${listHeaderRow}:D${lastDataRow}`).format.rowHeight = 22;
sheet.getRange(`D${firstDataRow}:D${lastDataRow}`).format.wrapText = true;

await fs.mkdir(outputDir, { recursive: true });
const preview = await workbook.render({
  sheetName: "功能统计",
  range: `A1:D${Math.min(lastDataRow, 36)}`,
  scale: 1,
  format: "png",
});
await fs.writeFile(`${outputDir}/功能统计预览.png`, new Uint8Array(await preview.arrayBuffer()));

const inspection = await workbook.inspect({
  kind: "region",
  sheetId: "功能统计",
  range: `A1:D${Math.min(lastDataRow, 25)}`,
  maxChars: 5000,
});
console.log(inspection.ndjson ?? inspection);
console.log(JSON.stringify({ categoryCount: categories.length, featureCount: features.length, totalRow, listHeaderRow, lastDataRow }));

const xlsx = await SpreadsheetFile.exportXlsx(workbook);
await xlsx.save(outputPath);
console.log(outputPath);
